import * as fs from 'node:fs/promises';
import { createWriteStream } from 'node:fs';
import { pipeline } from 'node:stream/promises';
import { Readable } from 'node:stream';
import { randomUUID } from 'node:crypto';

/**
 * Official Bangplanix Client SDK for Node.js / TypeScript (Enhanced)
 */
export class BangplanixClient {
  constructor(options = {}) {
    this.serverUrl = (options.serverUrl || 'http://localhost:9545').replace(/\/+$/, '');
    this.timeout = options.timeout || 30000;
    this.apiKey = options.apiKey || null;
    this.maxRetries = options.maxRetries !== undefined ? options.maxRetries : 3;
    this.retryDelayMs = options.retryDelayMs || 200;
    this.customFetch = options.fetch || globalThis.fetch;
  }

  async renderReport(request) {
    if (!request.templatePath && !request.templateJson) {
      throw new Error('Either templatePath or templateJson must be provided.');
    }

    const payload = {
      templatePath: request.templatePath,
      templateJson: request.templateJson,
      dataJson: typeof request.data === 'string' ? request.data : JSON.stringify(request.data || {}),
      parameters: request.parameters || {},
      format: (request.format || 'pdf').toLowerCase()
    };

    const correlationId = request.correlationId || randomUUID();
    const headers = {
      'Content-Type': 'application/json',
      'X-Correlation-ID': correlationId
    };

    if (this.apiKey) {
      headers['Authorization'] = `Bearer ${this.apiKey}`;
    }

    const startTime = Date.now();
    let attempt = 0;
    let lastError = null;

    while (attempt <= this.maxRetries) {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), this.timeout);

      try {
        const endpoint = `${this.serverUrl}/api/v1/report/render`;
        const response = await this.customFetch(endpoint, {
          method: 'POST',
          headers,
          body: JSON.stringify(payload),
          signal: controller.signal
        });

        if (!response.ok) {
          const isTransient = [429, 502, 503, 504].includes(response.status);
          const errorText = await response.text().catch(() => '');
          const err = new Error(`Bangplanix render error [HTTP ${response.status}]: ${errorText || response.statusText}`);
          err.status = response.status;

          if (isTransient && attempt < this.maxRetries) {
            attempt++;
            const backoff = this.retryDelayMs * Math.pow(2, attempt - 1) + Math.random() * 50;
            await new Promise(res => setTimeout(res, backoff));
            continue;
          }
          throw err;
        }

        const buffer = await response.arrayBuffer();
        const contentType = response.headers.get('content-type') || (payload.format === 'xlsx' ? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' : 'application/pdf');

        const uint8Data = new Uint8Array(buffer);
        return {
          data: uint8Data,
          format: payload.format,
          contentType,
          length: buffer.byteLength,
          correlationId,
          durationMs: Date.now() - startTime,
          toBase64() {
            return Buffer.from(uint8Data).toString('base64');
          },
          toStream() {
            return Readable.from(Buffer.from(uint8Data));
          }
        };
      } catch (err) {
        lastError = err;
        if (attempt < this.maxRetries && (!err.status || [429, 502, 503, 504].includes(err.status))) {
          attempt++;
          const backoff = this.retryDelayMs * Math.pow(2, attempt - 1) + Math.random() * 50;
          await new Promise(res => setTimeout(res, backoff));
          continue;
        }
        throw lastError;
      } finally {
        clearTimeout(timer);
      }
    }

    throw lastError;
  }

  async renderToFile(request, outputPath) {
    const result = await this.renderReport(request);
    await fs.writeFile(outputPath, result.data);
    return {
      ...result,
      filePath: outputPath
    };
  }

  async renderToBase64(request) {
    const result = await this.renderReport(request);
    return {
      ...result,
      base64: result.toBase64()
    };
  }

  async renderToStream(request) {
    const result = await this.renderReport(request);
    return result.toStream();
  }

  async renderBatch(requests, concurrencyLimit = 4) {
    const results = [];
    const queue = [...requests];
    const workers = Array.from({ length: Math.min(concurrencyLimit, requests.length) }, async () => {
      while (queue.length > 0) {
        const req = queue.shift();
        if (req) {
          try {
            const res = await this.renderReport(req);
            results.push({ success: true, result: res });
          } catch (err) {
            results.push({ success: false, error: err.message, request: req });
          }
        }
      }
    });

    await Promise.all(workers);
    return results;
  }

  async validateTemplate(templateJsonOrPath) {
    const isJson = templateJsonOrPath.trim().startsWith('{');
    const payload = isJson ? { templateJson: templateJsonOrPath } : { templatePath: templateJsonOrPath };

    const response = await this.customFetch(`${this.serverUrl}/api/v1/template/validate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      return { isValid: false, errors: [`HTTP ${response.status}: ${response.statusText}`] };
    }

    return await response.json();
  }

  async healthCheck() {
    const response = await this.customFetch(`${this.serverUrl}/health`);
    if (!response.ok) {
      return { status: 'unhealthy', httpStatus: response.status };
    }
    return await response.json();
  }
}