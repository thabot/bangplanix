/**
 * 2-Way Real-time Code-Canvas Synchronization Bridge
 * Connects Bangplanix Visual Canvas with live Monaco/Textarea Code Editor
 */
import { BpxReportSchema } from '../core/types.js';
import { globalEventBus } from '../core/event-bus.js';

export interface SyncParseResult {
  valid: boolean;
  schema?: BpxReportSchema;
  error?: string;
}

export class CodeSyncBridge {
  private debounceTimer: any = null;
  private debounceMs: number;
  private isUpdatingInternally = false;

  constructor(debounceMs = 300) {
    this.debounceMs = debounceMs;
  }

  /**
   * Serializes canvas report object into pretty-printed .bpx JSON string
   */
  canvasToJson(report: BpxReportSchema): string {
    return JSON.stringify(report, null, 2);
  }

  /**
   * Validates and parses raw JSON string into BpxReportSchema safely
   */
  jsonToCanvas(rawJson: string): SyncParseResult {
    if (!rawJson || !rawJson.trim()) {
      return { valid: false, error: 'Empty JSON payload' };
    }

    try {
      let cleaned = rawJson.trim();
      // Support markdown code blocks stripping
      cleaned = cleaned.replace(/^```(?:json)?/i, '').replace(/```$/, '').trim();
      
      const parsed = JSON.parse(cleaned);

      if (typeof parsed !== 'object' || parsed === null) {
        return { valid: false, error: 'Schema root must be an object' };
      }

      if (!parsed.bands || typeof parsed.bands !== 'object') {
        return { valid: false, error: 'Missing required "bands" definition' };
      }

      // Fill essential defaults if omitted
      if (!parsed.version) parsed.version = '1.0';
      if (!parsed.pageSetup) parsed.pageSetup = { width: 612, height: 792 };

      return { valid: true, schema: parsed as BpxReportSchema };
    } catch (err: any) {
      return { valid: false, error: err.message || 'Invalid JSON syntax' };
    }
  }

  /**
   * Triggered when code editor content changes. Debounces and notifies listeners if valid.
   */
  onCodeEditorInput(rawJson: string, callback: (result: SyncParseResult) => void): void {
    if (this.isUpdatingInternally) return;

    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }

    this.debounceTimer = setTimeout(() => {
      const res = this.jsonToCanvas(rawJson);
      if (res.valid && res.schema) {
        globalEventBus.publish('schema:updated_from_code', res.schema);
      }
      callback(res);
    }, this.debounceMs);
  }

  /**
   * Triggered when visual canvas elements are modified.
   */
  onCanvasModified(report: BpxReportSchema, updateEditorCallback: (jsonStr: string) => void): void {
    this.isUpdatingInternally = true;
    const jsonStr = this.canvasToJson(report);
    updateEditorCallback(jsonStr);
    globalEventBus.publish('schema:updated_from_canvas', report);
    this.isUpdatingInternally = false;
  }
}
