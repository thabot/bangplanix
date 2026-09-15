/**
 * Interactive Visual Designer Application Bootstrap (docs/designer.html)
 */
import { BpxReportSchema } from '../core/types.js';
import { globalStorage } from '../core/browser-storage.js';
import { CodeSyncBridge } from './code-sync-bridge.js';
import { TemplateGalleryManager } from './template-gallery.js';
import { SdkCodeGenerator } from './sdk-code-generator.js';
import { MockDataManager } from './mock-data-manager.js';
import { ShortcutManager } from './shortcut-manager.js';
import { InstantPdfExporter } from './pdf-exporter.js';

export class BangplanixDesignerApp {
  private syncBridge: CodeSyncBridge;
  private galleryManager: TemplateGalleryManager;
  private sdkGenerator: SdkCodeGenerator;
  private mockManager: MockDataManager;
  private shortcutManager: ShortcutManager;
  private pdfExporter: InstantPdfExporter;
  private currentReport: BpxReportSchema;

  constructor() {
    this.syncBridge = new CodeSyncBridge();
    this.galleryManager = new TemplateGalleryManager();
    this.sdkGenerator = new SdkCodeGenerator();
    this.mockManager = new MockDataManager();
    this.shortcutManager = new ShortcutManager();
    this.pdfExporter = new InstantPdfExporter();

    // Load initial template (default to A4 Commercial Invoice)
    const initialTpl = this.galleryManager.getTemplateById('a4_commercial_invoice') || this.galleryManager.getTemplateById('us_commercial_invoice');
    this.currentReport = initialTpl ? initialTpl.schema : this.getDefaultSchema();
  }

  private getDefaultSchema(): BpxReportSchema {
    return {
      version: '1.0',
      metadata: { title: 'Untitled Report' },
      pageSetup: { width: 595.28, height: 841.89, paperKind: 'A4' },
      bands: {
        Detail: { height: 60, elements: [] }
      }
    };
  }

  init(): void {
    this.setupShortcuts();
    this.checkSavedDraft();
  }

  private setupShortcuts(): void {
    this.shortcutManager.register('save', () => this.saveDraft());
    this.shortcutManager.register('export_pdf', () => this.exportPdf());
    
    if (typeof window !== 'undefined') {
      window.addEventListener('keydown', (e) => {
        this.shortcutManager.handleKeyDown(e);
      });
    }
  }

  private checkSavedDraft(): void {
    const draft = globalStorage.loadDraft();
    if (draft && draft.schema) {
      console.log(`[DesignerApp] Loaded auto-saved draft: ${draft.name} (${draft.savedAt})`);
    }
  }

  saveDraft(): boolean {
    const ok = globalStorage.autoSaveDraft(this.currentReport);
    if (ok) {
      this.showToast('Draft auto-saved successfully (BrowserStorage)');
    }
    return ok;
  }

  exportPdf(): void {
    this.pdfExporter.exportPdf(this.currentReport);
    this.showToast('Generating instant high-resolution PDF...');
  }

  loadPreset(templateId: string): BpxReportSchema | null {
    const tpl = this.galleryManager.getTemplateById(templateId);
    if (tpl) {
      this.currentReport = tpl.schema;
      this.saveDraft();
      return this.currentReport;
    }
    return null;
  }

  getSdkSnippet(lang: any): string {
    return this.sdkGenerator.generate(lang, this.currentReport);
  }

  showToast(message: string): void {
    if (typeof document === 'undefined') return;
    const toast = document.getElementById('bpx-toast');
    if (toast) {
      toast.textContent = message;
      toast.classList.add('show');
      setTimeout(() => toast.classList.remove('show'), 3000);
    }
  }

  getReport(): BpxReportSchema {
    return this.currentReport;
  }

  setReport(report: BpxReportSchema): void {
    this.currentReport = report;
  }
}
