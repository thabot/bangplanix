import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';
import { BangplanixClient } from '../src/index.js';

describe('Bangplanix Node.js Client SDK Tests', () => {
  test('should throw error when neither templatePath nor templateJson is provided', async () => {
    const client = new BangplanixClient();
    await assert.rejects(
      async () => await client.renderReport({}),
      /Either templatePath or templateJson must be provided/
    );
  });

  test('renderReport should correctly structure request and return Uint8Array data and telemetry', async () => {
    const mockBytes = new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]); // %PDF-1.4
    let capturedCorrelationId = null;
    const mockFetch = async (url, options) => {
      assert.equal(url, 'http://localhost:9545/api/v1/report/render');
      assert.equal(options.method, 'POST');
      capturedCorrelationId = options.headers['X-Correlation-ID'];
      assert.ok(capturedCorrelationId, 'X-Correlation-ID must be injected');

      const body = JSON.parse(options.body);
      assert.equal(body.templatePath, 'schema/v1/samples/invoice.bpx');
      assert.equal(body.format, 'pdf');
      return {
        ok: true,
        status: 200,
        headers: new Map([['content-type', 'application/pdf']]),
        arrayBuffer: async () => mockBytes.buffer
      };
    };

    const client = new BangplanixClient({ fetch: mockFetch });
    const result = await client.renderReport({
      templatePath: 'schema/v1/samples/invoice.bpx',
      data: [{ item: 'Service A', amount: 500 }],
      format: 'pdf'
    });

    assert.equal(result.format, 'pdf');
    assert.equal(result.contentType, 'application/pdf');
    assert.equal(result.length, 8);
    assert.equal(result.correlationId, capturedCorrelationId);
    assert.ok(result.durationMs >= 0);
  });

  test('renderReport should auto retry on 503 transient error and succeed', async () => {
    const mockBytes = new Uint8Array([0x25, 0x50, 0x44, 0x46]);
    let callCount = 0;
    const mockFetch = async () => {
      callCount++;
      if (callCount === 1) {
        return {
          ok: false,
          status: 503,
          statusText: 'Service Temporarily Unavailable',
          text: async () => 'Service Overloaded'
        };
      }
      return {
        ok: true,
        status: 200,
        headers: new Map([['content-type', 'application/pdf']]),
        arrayBuffer: async () => mockBytes.buffer
      };
    };

    const client = new BangplanixClient({ fetch: mockFetch, maxRetries: 2, retryDelayMs: 10 });
    const result = await client.renderReport({
      templatePath: 'schema/v1/samples/invoice.bpx',
      format: 'pdf'
    });

    assert.equal(callCount, 2, 'Should have retried once and succeeded on second attempt');
    assert.equal(result.length, 4);
  });

  test('renderToFile should write output file to disk', async () => {
    const mockBytes = new Uint8Array([0x50, 0x4B, 0x03, 0x04]); // XLSX Zip header
    const mockFetch = async () => ({
      ok: true,
      status: 200,
      headers: new Map([['content-type', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet']]),
      arrayBuffer: async () => mockBytes.buffer
    });

    const client = new BangplanixClient({ fetch: mockFetch });
    const __dirname = path.dirname(fileURLToPath(import.meta.url));
    const outPath = path.join(__dirname, 'temp_test_out.xlsx');
    const res = await client.renderToFile({
      templatePath: 'schema/v1/samples/invoice.bpx',
      format: 'xlsx'
    }, outPath);

    assert.equal(res.filePath, outPath);
    const fileExists = await fs.stat(outPath).then(() => true).catch(() => false);
    assert.ok(fileExists);
    await fs.unlink(outPath); // cleanup
  });

  test('renderBatch should execute multiple reports concurrently with queue limit', async () => {
    const mockBytes = new Uint8Array([0x25, 0x50, 0x44, 0x46]);
    let activeCalls = 0;
    let maxActiveCalls = 0;

    const mockFetch = async () => {
      activeCalls++;
      maxActiveCalls = Math.max(maxActiveCalls, activeCalls);
      await new Promise(r => setTimeout(r, 20));
      activeCalls--;
      return {
        ok: true,
        status: 200,
        headers: new Map([['content-type', 'application/pdf']]),
        arrayBuffer: async () => mockBytes.buffer
      };
    };

    const client = new BangplanixClient({ fetch: mockFetch });
    const requests = Array.from({ length: 6 }, (_, i) => ({
      templatePath: `template_${i}.bpx`,
      format: 'pdf'
    }));

    const batchResults = await client.renderBatch(requests, 2);
    assert.equal(batchResults.length, 6);
    assert.ok(batchResults.every(r => r.success));
    assert.ok(maxActiveCalls <= 2, `Max concurrent calls should be <= 2, was ${maxActiveCalls}`);
  });

  test('validateTemplate and healthCheck should function properly', async () => {
    const mockFetch = async (url) => {
      if (url.includes('/validate')) {
        return { ok: true, status: 200, json: async () => ({ isValid: true }) };
      }
      return { ok: true, status: 200, json: async () => ({ status: 'Healthy' }) };
    };

    const client = new BangplanixClient({ fetch: mockFetch });
    const valRes = await client.validateTemplate('schema/v1/samples/invoice.bpx');
    const healthRes = await client.healthCheck();

    assert.equal(valRes.isValid, true);
    assert.equal(healthRes.status, 'Healthy');
  });

  test('renderReport should throw Error on 400 Bad Request without retry', async () => {
    let callCount = 0;
    const mockFetch = async () => {
      callCount++;
      return {
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        text: async () => 'Invalid parameters'
      };
    };

    const client = new BangplanixClient({ fetch: mockFetch, maxRetries: 3 });
    await assert.rejects(
      async () => await client.renderReport({ templatePath: 'invalid.bpx' }),
      /Bangplanix render error \[HTTP 400\]: Invalid parameters/
    );
    assert.equal(callCount, 1, 'Should fail immediately without retrying non-transient errors');
  });

  test('renderReport and helpers should support Base64 and Stream return formats', async () => {
    const sampleBytes = new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37]); // %PDF-1.7
    const mockFetch = async () => ({
      ok: true,
      status: 200,
      headers: new Map([['content-type', 'application/pdf']]),
      arrayBuffer: async () => sampleBytes.buffer
    });

    const client = new BangplanixClient({ fetch: mockFetch });
    const result = await client.renderReport({ templatePath: 'schema/v1/samples/invoice.bpx' });

    // Test toBase64()
    const base64 = result.toBase64();
    assert.equal(base64, Buffer.from(sampleBytes).toString('base64'));

    // Test renderToBase64()
    const b64Result = await client.renderToBase64({ templatePath: 'schema/v1/samples/invoice.bpx' });
    assert.equal(b64Result.base64, base64);

    // Test toStream()
    const stream = result.toStream();
    const chunks = [];
    for await (const chunk of stream) {
      chunks.push(chunk);
    }
    const streamedBuffer = Buffer.concat(chunks);
    assert.deepEqual(streamedBuffer, Buffer.from(sampleBytes));
  });
});