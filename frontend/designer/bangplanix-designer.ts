import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { BangplanixDesignerCore } from './designer-core.js';

@customElement('bangplanix-designer')
export class BangplanixDesigner extends LitElement {
  @property({ type: Object }) initialReport: any = null;
  @property({ type: String }) serverUrl: string = 'http://localhost:9545';
  @property({ type: Boolean }) readOnly: boolean = false;

  @state() private core: BangplanixDesignerCore = new BangplanixDesignerCore();
  @state() private activeTab: string = 'design';
  @state() private isDragging: boolean = false;
  @state() private dragElementId: string | null = null;
  @state() private dragStartPos = { x: 0, y: 0 };
  @state() private livePreviewSvg: string = '';

  static override styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100vh;
      width: 100%;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Sarabun", sans-serif;
      background-color: #0f172a;
      color: #f8fafc;
      overflow: hidden;
      box-sizing: border-box;
    }

    *, *::before, *::after {
      box-sizing: border-box;
    }

    /* Top Ribbon Toolbar */
    .bpx-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 48px;
      background-color: #1e293b;
      border-bottom: 1px solid #334155;
      padding: 0 12px;
      gap: 8px;
      user-select: none;
    }

    .bpx-toolbar-group {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .bpx-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      background-color: #334155;
      color: #e2e8f0;
      border: 1px solid #475569;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .bpx-btn:hover {
      background-color: #475569;
      color: #ffffff;
    }

    .bpx-btn.active {
      background-color: #2563eb;
      border-color: #3b82f6;
      color: #ffffff;
    }

    .bpx-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .bpx-brand {
      font-weight: 700;
      font-size: 15px;
      letter-spacing: -0.5px;
      color: #38bdf8;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    /* Main Workspace Layout */
    .bpx-workspace {
      display: flex;
      flex: 1;
      overflow: hidden;
      position: relative;
    }

    /* Left Sidebar: Toolbox & Data Explorer */
    .bpx-sidebar-left {
      width: 260px;
      background-color: #1e293b;
      border-right: 1px solid #334155;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .bpx-panel-header {
      padding: 10px 14px;
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #94a3b8;
      border-bottom: 1px solid #334155;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .bpx-toolbox-items {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
      padding: 12px;
      overflow-y: auto;
    }

    .bpx-tool-card {
      background-color: #0f172a;
      border: 1px solid #334155;
      border-radius: 6px;
      padding: 10px 8px;
      text-align: center;
      cursor: grab;
      font-size: 12px;
      transition: all 0.15s ease;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .bpx-tool-card:hover {
      border-color: #38bdf8;
      background-color: #1e293b;
      transform: translateY(-1px);
    }

    /* Center Canvas Area */
    .bpx-canvas-container {
      flex: 1;
      background-color: #0b0f19;
      overflow: auto;
      position: relative;
      padding: 32px;
      display: flex;
      justify-content: center;
      align-items: flex-start;
    }

    .bpx-page-canvas {
      background-color: #ffffff;
      color: #0f172a;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.5), 0 8px 10px -6px rgba(0, 0, 0, 0.5);
      border-radius: 4px;
      position: relative;
      user-select: none;
      transition: transform 0.1s ease;
    }

    .bpx-band-container {
      border-bottom: 1px dashed #cbd5e1;
      position: relative;
    }

    .bpx-band-header {
      position: absolute;
      left: -90px;
      top: 0;
      width: 85px;
      font-size: 10px;
      font-weight: 700;
      color: #94a3b8;
      text-align: right;
      padding-right: 6px;
      pointer-events: none;
    }

    .bpx-element-view {
      position: absolute;
      border: 1px dashed transparent;
      cursor: move;
      overflow: hidden;
      display: flex;
      align-items: center;
      padding: 2px 4px;
    }

    .bpx-element-view:hover {
      border-color: #38bdf8;
    }

    .bpx-element-view.selected {
      border: 1.5px solid #2563eb;
      background-color: rgba(37, 99, 235, 0.08);
    }

    /* Right Property Inspector */
    .bpx-sidebar-right {
      width: 300px;
      background-color: #1e293b;
      border-left: 1px solid #334155;
      display: flex;
      flex-direction: column;
      overflow-y: auto;
    }

    .bpx-prop-section {
      padding: 12px 14px;
      border-bottom: 1px solid #334155;
    }

    .bpx-prop-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 8px;
      font-size: 12px;
    }

    .bpx-prop-row label {
      color: #94a3b8;
    }

    .bpx-input {
      background-color: #0f172a;
      border: 1px solid #475569;
      color: #f8fafc;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      width: 140px;
    }

    /* Bottom Editor Area */
    .bpx-bottom-editor {
      height: 180px;
      background-color: #0f172a;
      border-top: 1px solid #334155;
      display: flex;
      flex-direction: column;
    }

    .bpx-editor-tabs {
      display: flex;
      background-color: #1e293b;
      border-bottom: 1px solid #334155;
    }

    .bpx-tab {
      padding: 6px 14px;
      font-size: 12px;
      font-weight: 500;
      color: #94a3b8;
      cursor: pointer;
      border-right: 1px solid #334155;
    }

    .bpx-tab.active {
      color: #38bdf8;
      background-color: #0f172a;
      border-bottom: 2px solid #38bdf8;
    }

    .bpx-editor-body {
      flex: 1;
      padding: 10px;
      font-family: monospace;
      font-size: 12px;
      overflow: auto;
    }
  `;

  override connectedCallback() {
    super.connectedCallback();
    if (this.initialReport) {
      this.core.loadBpxJson(this.initialReport);
    }
  }

  // --- Handlers ---
  private handleToolDragStart(e: DragEvent, type: string) {
    if (e.dataTransfer) {
  @state() private showAiModal: boolean = false;
  @state() private showAiSettingsModal: boolean = false;
  @state() private showHistoryDrawer: boolean = false;
  @state() private aiPrompt: string = '';
  @state() private aiLanguage: string = 'th-TH';
  @state() private isListening: boolean = false;
  @state() private isAiGenerating: boolean = false;
  @state() private aiLoadingStep: string = '';
  @state() private aiError: string | null = null;
  @state() private aiGeneratedBpx: string = '';
  @state() private abortController: AbortController | null = null;
  @state() private aiHistoryList: any[] = [];

  // BYOK Settings State
  @state() private aiProvider: string = 'gemini';
  @state() private aiModelName: string = 'gemini-2.5-flash';
  @state() private aiApiKey: string = '';
  @state() private aiEndpointUrl: string = '';
  @state() private testPingStatus: string = '';
  @state() private isPinging: boolean = false;

  private recognition: any = null;

  override connectedCallback() {
    super.connectedCallback();
    this.loadSavedAiSettings();
    this.initSpeechRecognition();
  }

  private loadSavedAiSettings() {
    try {
      this.aiProvider = localStorage.getItem('bpx_ai_provider') || 'gemini';
      this.aiModelName = localStorage.getItem('bpx_ai_model') || 'gemini-2.5-flash';
      this.aiApiKey = localStorage.getItem('bpx_ai_key') || '';
      this.aiEndpointUrl = localStorage.getItem('bpx_ai_endpoint') || '';
      this.aiLanguage = localStorage.getItem('bpx_ai_voice_lang') || 'th-TH';
    } catch (_) {}
  }

  private saveAiSettings() {
    try {
      localStorage.setItem('bpx_ai_provider', this.aiProvider);
      localStorage.setItem('bpx_ai_model', this.aiModelName);
      localStorage.setItem('bpx_ai_key', this.aiApiKey);
      localStorage.setItem('bpx_ai_endpoint', this.aiEndpointUrl);
      localStorage.setItem('bpx_ai_voice_lang', this.aiLanguage);
      this.showAiSettingsModal = false;
      this.requestUpdate();
    } catch (_) {}
  }

  private initSpeechRecognition() {
    const SpeechRec = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (SpeechRec) {
      this.recognition = new SpeechRec();
      this.recognition.continuous = false;
      this.recognition.interimResults = true;

      this.recognition.onresult = (event: any) => {
        let transcript = '';
        for (let i = event.resultIndex; i < event.results.length; ++i) {
          transcript += event.results[i][0].transcript;
        }
        if (transcript) {
          this.aiPrompt = transcript;
          this.requestUpdate();
        }
      };

      this.recognition.onerror = () => {
        this.isListening = false;
        this.requestUpdate();
      };

      this.recognition.onend = () => {
        this.isListening = false;
        this.requestUpdate();
      };
    }
  }

  private toggleVoiceRecognition() {
    if (!this.recognition) {
      alert('Speech Recognition is not supported in this browser. Please use Chrome/Edge or type your prompt.');
      return;
    }

    if (this.isListening) {
      this.recognition.stop();
      this.isListening = false;
    } else {
      this.recognition.lang = this.aiLanguage;
      this.recognition.start();
      this.isListening = true;
    }
    this.requestUpdate();
  }

  private getStarterPrompts() {
    const lang = this.aiLanguage.toLowerCase();
    if (lang.startsWith('th')) {
      return [
        { icon: '🧾', title: 'ใบเสร็จรับเงิน', text: 'สร้างรายงานใบเสร็จรับเงิน มีรหัสสินค้า ชื่อสินค้า จำนวน ราคา ยอดรวม พร้อม QR Code PromptPay และยอดเงินบาทถ้วน' },
        { icon: '📊', title: 'แดชบอร์ดสรุปยอดขาย', text: 'สร้างแดชบอร์ดสรุปยอดขายรายเดือน พร้อมกราฟแท่ง Column Chart และการเปรียบเทียบ KPI' },
        { icon: '🏷️', title: 'ใบปะหน้าพัสดุ', text: 'สร้างฉลากติดกล่องพัสดุและบาร์โค้ด Code 128 พร้อมที่อยู่ผู้รับและผู้ส่ง' },
        { icon: '📑', title: 'ใบกำกับภาษีเต็มรูป', text: 'สร้างใบกำกับภาษี/ใบส่งของ มีชื่อผู้ซื้อ-ผู้ขาย เลขประจำตัวผู้เสียภาษี และคำนวณภาษี VAT 7%' }
      ];
    } else if (lang.startsWith('zh')) {
      return [
        { icon: '🧾', title: '销售收据', text: '生成包含商品明细表与二维码的销售收据' },
        { icon: '📊', title: '销售仪表盘', text: '创建月度销售执行仪表盘，包含柱状图与关键指标' }
      ];
    } else if (lang.startsWith('ja')) {
      return [
        { icon: '🧾', title: '領収書', text: '商品明細表とQRコード付きの領収書・レシートを作成' },
        { icon: '📊', title: '売上ダッシュボード', text: '売上カラムチャートとKPIカードを含む月次ダッシュボード作成' }
      ];
    } else if (lang.startsWith('es')) {
      return [
        { icon: '🧾', title: 'Recibo de Venta', text: 'Generar recibo de venta con tabla de productos y código QR' },
        { icon: '📊', title: 'Panel de Ventas', text: 'Crear panel de ventas mensual con gráfico de columnas' }
      ];
    }
    return [
      { icon: '🧾', title: 'Sales Receipt', text: 'Generate a sales receipt with payment QR, itemized table, and tax summary.' },
      { icon: '📊', title: 'Executive Dashboard', text: 'Create monthly executive sales dashboard with revenue column chart and KPIs.' },
      { icon: '🏷️', title: 'Shipping Label', text: 'Generate shipping logistics label with Code 128 barcode and recipient address.' },
      { icon: '📑', title: 'Commercial Invoice', text: 'Create commercial VAT invoice with subtotal, tax 7%, and total amount.' }
    ];
  }

  private async executeAiGeneration() {
    if (!this.aiPrompt.trim()) return;

    this.isAiGenerating = true;
    this.aiError = null;
    this.aiGeneratedBpx = '';
    this.aiLoadingStep = 'กำลังวิเคราะห์คำสั่งและโครงสร้างรายงาน...';
    this.abortController = new AbortController();

    const snapshot = this.core.getBpxJson();

    try {
      setTimeout(() => {
        if (this.isAiGenerating) this.aiLoadingStep = 'กำลังจัดวางองค์ประกอบและคำนวณ Bands...';
      }, 1000);

      setTimeout(() => {
        if (this.isAiGenerating) this.aiLoadingStep = 'กำลังตรวจสอบความปลอดภัยของ SQL & Roslyn expressions...';
      }, 2000);

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'X-Bangplanix-Ai-Provider': this.aiProvider,
        'X-Bangplanix-Ai-Model': this.aiModelName
      };
      if (this.aiApiKey) headers['X-Bangplanix-Ai-Key'] = this.aiApiKey;
      if (this.aiEndpointUrl) headers['X-Bangplanix-Ai-Endpoint'] = this.aiEndpointUrl;

      const res = await fetch(`${this.serverUrl}/api/v1/ai/generate`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          prompt: this.aiPrompt,
          originalBpxSnapshot: snapshot
        }),
        signal: this.abortController.signal
      });

      const data = await res.json();
      if (res.ok && data.success) {
        this.aiGeneratedBpx = data.bpxJson;
        this.aiLoadingStep = 'สร้างสำเร็จ พร้อมตรวจสอบก่อน Apply!';
      } else {
        this.aiError = data.error || 'Failed to generate report schema.';
      }
    } catch (err: any) {
      if (err.name === 'AbortError') {
        this.aiError = 'การประมวลผลถูกยกเลิก (Cancelled by user)';
      } else {
        this.aiError = `Error: ${err.message}`;
      }
    } finally {
      this.isAiGenerating = false;
      this.abortController = null;
      this.requestUpdate();
    }
  }

  private cancelAiGeneration() {
    if (this.abortController) {
      this.abortController.abort();
    }
  }

  private applyGeneratedSchema() {
    if (!this.aiGeneratedBpx) return;
    this.core.applyAiGeneratedReport(this.aiGeneratedBpx);
    this.showAiModal = false;
    this.requestUpdate();
  }

  private async testAiConnection() {
    this.isPinging = true;
    this.testPingStatus = 'Testing connection...';
    try {
      const res = await fetch(`${this.serverUrl}/api/v1/ai/test-connection`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          provider: this.aiProvider,
          apiKey: this.aiApiKey,
          modelName: this.aiModelName,
          endpointUrl: this.aiEndpointUrl
        })
      });
      const data = await res.json();
      if (res.ok && data.success) {
        this.testPingStatus = `✓ Connected successfully! Latency: ${data.latencyMs.toFixed(1)} ms`;
      } else {
        this.testPingStatus = `✗ Connection failed: ${data.error || 'Check key & endpoint'}`;
      }
    } catch (err: any) {
      this.testPingStatus = `✗ Error: ${err.message}`;
    } finally {
      this.isPinging = false;
      this.requestUpdate();
    }
  }

  private async loadAiHistory() {
    try {
      const res = await fetch(`${this.serverUrl}/api/v1/ai/history?limit=30`);
      if (res.ok) {
        this.aiHistoryList = await res.json();
      }
    } catch (_) {}
    this.showHistoryDrawer = true;
    this.requestUpdate();
  }

  private async rollbackHistory(historyId: number) {
    try {
      const res = await fetch(`${this.serverUrl}/api/v1/ai/history/undo`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ historyId })
      });
      const data = await res.json();
      if (res.ok && data.success && data.restoredBpxJson) {
        this.core.loadBpxJson(data.restoredBpxJson);
        this.showHistoryDrawer = false;
        this.requestUpdate();
      } else {
        alert(data.error || 'Failed to rollback.');
      }
    } catch (err: any) {
      alert(`Error: ${err.message}`);
    }
  }

  private handleCanvasDrop(e: DragEvent, bandName: string) {
    e.preventDefault();
    const type = e.dataTransfer?.getData('text/plain') || 'Text';
    const canvasRect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const x = (e.clientX - canvasRect.left) / (this.core.zoomLevel / 100);
    const y = (e.clientY - canvasRect.top) / (this.core.zoomLevel / 100);

    this.core.addElement(bandName, {
      type,
      x,
      y,
      width: type === 'Chart' ? 300 : (type === 'Barcode' ? 180 : 120),
      height: type === 'Chart' ? 180 : (type === 'Barcode' ? 60 : 25),
      text: type === 'Text' ? 'Label Text' : (type === 'Barcode' ? '123456789' : '')
    });
    this.requestUpdate();
  }

  private handleElementClick(e: MouseEvent, id: string) {
    e.stopPropagation();
    this.core.selectElement(id, e.shiftKey || e.ctrlKey);
    this.requestUpdate();
  }

  private updateSelectedProp(key: string, value: any) {
    if (this.core.selectedElementIds.length === 0) return;
    const info = this.core.findElement(this.core.selectedElementIds[0]);
    if (!info) return;

    if (key === 'text') info.element.text = value;
    else if (key === 'expression') info.element.expression = value;
    else if (key === 'width') info.element.width = Number(value);
    else if (key === 'height') info.element.height = Number(value);
    else if (key === 'x') info.element.x = Number(value);
    else if (key === 'y') info.element.y = Number(value);

    this.core.pushHistory(`Update ${key}`);
    this.requestUpdate();
  }

  private handleToolDragStart(e: DragEvent, type: string) {
    if (e.dataTransfer) {
      e.dataTransfer.setData('text/plain', type);
    }
  }

  override render() {
    const report = this.core.report;
    const selectedInfo = this.core.selectedElementIds.length === 1 
      ? this.core.findElement(this.core.selectedElementIds[0]) 
      : null;

    return html`
      <!-- Top Ribbon Toolbar -->
      <div class="bpx-toolbar">
        <div class="bpx-toolbar-group">
          <div class="bpx-brand">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
            Bangplanix Designer
          </div>
          <div style="width: 1px; height: 20px; background: #334155; margin: 0 6px;"></div>
          <button class="bpx-btn ${this.core.viewMode === 'design' ? 'active' : ''}" @click=${() => { this.core.setViewMode('design'); this.requestUpdate(); }}>Design</button>
          <button class="bpx-btn ${this.core.viewMode === 'preview' ? 'active' : ''}" @click=${() => { this.core.setViewMode('preview'); this.requestUpdate(); }}>Live Preview</button>
          <button class="bpx-btn ${this.core.viewMode === 'code' ? 'active' : ''}" @click=${() => { this.core.setViewMode('code'); this.requestUpdate(); }}>Schema Code</button>
        </div>

        <!-- AI Assistant & Actions -->
        <div class="bpx-toolbar-group">
          <button class="bpx-btn" style="background: linear-gradient(135deg, #4f46e5, #7c3aed); border-color: #6366f1; color: white;" @click=${() => { this.showAiModal = true; this.requestUpdate(); }}>
            ✨ AI Assistant
          </button>
          <button class="bpx-btn" style="background: #1e293b; border-color: #3b82f6; color: #38bdf8;" @click=${() => { this.loadAiHistory(); }}>
            📜 AI History
          </button>
          <button class="bpx-btn" @click=${() => { this.showAiSettingsModal = true; this.requestUpdate(); }} title="Configure AI Provider & API Key">
            ⚙️ AI Settings
          </button>
        </div>

        <div class="bpx-toolbar-group">
          <button class="bpx-btn" @click=${() => { this.core.undo(); this.requestUpdate(); }} title="Undo (Ctrl+Z)">↶ Undo</button>
          <button class="bpx-btn" @click=${() => { this.core.redo(); this.requestUpdate(); }} title="Redo (Ctrl+Y)">↷ Redo</button>
          <div style="width: 1px; height: 20px; background: #334155; margin: 0 4px;"></div>
          <button class="bpx-btn" @click=${() => { this.core.zoomOut(); this.requestUpdate(); }}>−</button>
          <span style="font-size: 12px; font-weight: 600; min-width: 44px; text-align: center;">${this.core.zoomLevel}%</span>
          <button class="bpx-btn" @click=${() => { this.core.zoomIn(); this.requestUpdate(); }}>+</button>
          <button class="bpx-btn" @click=${() => { this.core.resetZoom(); this.requestUpdate(); }}>100%</button>
        </div>

        <div class="bpx-toolbar-group">
          <button class="bpx-btn" @click=${() => { this.core.alignSelectedElements('left'); this.requestUpdate(); }} title="Align Left">⇤</button>
          <button class="bpx-btn" @click=${() => { this.core.alignSelectedElements('center'); this.requestUpdate(); }} title="Align Center">⇹</button>
          <button class="bpx-btn" @click=${() => { this.core.alignSelectedElements('right'); this.requestUpdate(); }} title="Align Right">⇥</button>
          <button class="bpx-btn" @click=${() => { this.core.removeSelectedElements(); this.requestUpdate(); }} title="Delete Selected" style="color: #ef4444;">🗑</button>
        </div>
      </div>

      <!-- Main Workspace -->
      <div class="bpx-workspace">
        <!-- Left Toolbox & Data Explorer -->
        <div class="bpx-sidebar-left">
          <div class="bpx-panel-header">Components Toolbox</div>
          <div class="bpx-toolbox-items">
            ${['Text', 'Expression', 'Table', 'Chart', 'Barcode', 'QrCode', 'Image', 'Shape'].map(tool => html`
              <div class="bpx-tool-card" draggable="true" @dragstart=${(e: DragEvent) => this.handleToolDragStart(e, tool)}>
                <span>📦</span>
                <span>${tool}</span>
              </div>
            `)}
          </div>

          <div class="bpx-panel-header" style="margin-top: 10px;">Datasets & Fields</div>
          <div style="padding: 10px 14px; font-size: 12px; overflow-y: auto; flex: 1;">
            ${(report.datasets || []).map((ds: any) => html`
              <div style="font-weight: 600; color: #38bdf8; margin-bottom: 4px;">📊 ${ds.name}</div>
              <div style="padding-left: 12px; margin-bottom: 10px;">
                ${(ds.fields || []).map((f: any) => html`
                  <div style="color: #94a3b8; padding: 2px 0;">🏷 ${f.name} <span style="font-size: 10px; color: #64748b;">(${f.type})</span></div>
                `)}
              </div>
            `)}
          </div>
        </div>

        <!-- Center Canvas -->
        <div class="bpx-canvas-container" @click=${() => { this.core.selectElement(null); this.requestUpdate(); }}>
          <div class="bpx-page-canvas" style="width: ${report.pageSetup.width}px; min-height: ${report.pageSetup.height}px; transform: scale(${this.core.zoomLevel / 100}); transform-origin: top center;">
            ${Object.entries(report.bands || {}).map(([bandName, band]: [string, any]) => html`
              <div class="bpx-band-container" style="height: ${band.height}px;" @dragover=${(e: DragEvent) => e.preventDefault()} @drop=${(e: DragEvent) => this.handleCanvasDrop(e, bandName)}>
                <div class="bpx-band-header">${bandName}</div>
                ${(band.elements || []).map((el: any) => html`
                  <div 
                    class="bpx-element-view ${this.core.selectedElementIds.includes(el.id) ? 'selected' : ''}"
                    style="left: ${el.x}px; top: ${el.y}px; width: ${el.width}px; height: ${el.height}px; font-size: ${el.style?.fontSize || 12}px; color: ${el.style?.color || '#0f172a'};"
                    @click=${(e: MouseEvent) => this.handleElementClick(e, el.id)}
                  >
                    ${el.type === 'Chart' ? html`📊 [Chart: ${el.chart?.title || el.chart?.chartType || 'Column'}]` : 
                      (el.type === 'Barcode' || el.type === 'QrCode' ? html`▦ [${el.type}: ${el.text || '123456'}]` :
                      (el.expression ? html`<span style="color: #2563eb; font-style: italic;">{ ${el.expression} }</span>` : (el.text || el.type)))}
                  </div>
                `)}
              </div>
            `)}
          </div>
        </div>

        <!-- Right Property Inspector -->
        <div class="bpx-sidebar-right">
          <div class="bpx-panel-header">Property Inspector</div>
          ${selectedInfo ? html`
            <div class="bpx-prop-section">
              <div style="font-weight: 600; color: #38bdf8; margin-bottom: 8px;">Selected: ${selectedInfo.element.type}</div>
              <div class="bpx-prop-row">
                <label>X Position</label>
                <input class="bpx-input" type="number" .value=${selectedInfo.element.x} @input=${(e: any) => this.updateSelectedProp('x', e.target.value)} />
              </div>
              <div class="bpx-prop-row">
                <label>Y Position</label>
                <input class="bpx-input" type="number" .value=${selectedInfo.element.y} @input=${(e: any) => this.updateSelectedProp('y', e.target.value)} />
              </div>
              <div class="bpx-prop-row">
                <label>Width</label>
                <input class="bpx-input" type="number" .value=${selectedInfo.element.width} @input=${(e: any) => this.updateSelectedProp('width', e.target.value)} />
              </div>
              <div class="bpx-prop-row">
                <label>Height</label>
                <input class="bpx-input" type="number" .value=${selectedInfo.element.height} @input=${(e: any) => this.updateSelectedProp('height', e.target.value)} />
              </div>
              <div class="bpx-prop-row">
                <label>Static Text</label>
                <input class="bpx-input" type="text" .value=${selectedInfo.element.text || ''} @input=${(e: any) => this.updateSelectedProp('text', e.target.value)} />
              </div>
              <div class="bpx-prop-row">
                <label>C# Expression</label>
                <input class="bpx-input" type="text" .value=${selectedInfo.element.expression || ''} @input=${(e: any) => this.updateSelectedProp('expression', e.target.value)} />
              </div>
            </div>
          ` : html`
            <div style="padding: 24px; text-align: center; color: #64748b; font-size: 13px;">
              Select an element on canvas to inspect and modify properties.
            </div>
          `}
        </div>
      </div>

      <!-- Bottom Editor Area -->
      <div class="bpx-bottom-editor">
        <div class="bpx-editor-tabs">
          <div class="bpx-tab ${this.core.bottomEditorTab === 'expression' ? 'active' : ''}" @click=${() => { this.core.setBottomEditorTab('expression'); this.requestUpdate(); }}>Expression Assist</div>
          <div class="bpx-tab ${this.core.bottomEditorTab === 'sql' ? 'active' : ''}" @click=${() => { this.core.setBottomEditorTab('sql'); this.requestUpdate(); }}>SQL Query Console</div>
          <div class="bpx-tab ${this.core.bottomEditorTab === 'bpxJson' ? 'active' : ''}" @click=${() => { this.core.setBottomEditorTab('bpxJson'); this.requestUpdate(); }}>.bpx JSON Schema</div>
        </div>
        <div class="bpx-editor-body">
          ${this.core.bottomEditorTab === 'bpxJson' 
            ? html`<pre style="margin: 0; color: #38bdf8;">${this.core.getBpxJson()}</pre>`
            : (this.core.bottomEditorTab === 'sql' 
                ? html`<pre style="margin: 0; color: #a7f3d0;">${report.datasets?.[0]?.query || 'No SQL Query'}</pre>`
                : html`<div style="color: #cbd5e1;">💡 Type C# formulas: e.g. <span style="color: #38bdf8;">FormatCurrency(Fields.Amount * (1.0 - Fields.Discount))</span></div>`)}
        </div>
      </div>

      <!-- AI Assistant Modal Dialog -->
      ${this.showAiModal ? html`
        <div style="position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; align-items: center; justify-content: center; z-index: 1000;">
          <div style="background: #1e293b; border: 1px solid #475569; border-radius: 12px; width: 720px; max-width: 95vw; display: flex; flex-direction: column; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.5);">
            <div style="padding: 16px 20px; background: #0f172a; border-bottom: 1px solid #334155; display: flex; justify-content: space-between; align-items: center;">
              <div style="font-weight: 700; font-size: 16px; color: #38bdf8; display: flex; align-items: center; gap: 8px;">
                <span>✨ Bangplanix AI Report Assistant</span>
              </div>
              <button class="bpx-btn" @click=${() => { this.showAiModal = false; }}>✕</button>
            </div>

            <div style="padding: 20px; display: flex; flex-direction: column; gap: 14px; max-height: 75vh; overflow-y: auto;">
              <!-- Starter Prompt Cards -->
              <div>
                <div style="font-size: 12px; font-weight: 600; color: #94a3b8; margin-bottom: 6px;">💡 Quick Starter Prompts (เลือกคำสั่งสำเร็จรูป):</div>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px;">
                  ${this.getStarterPrompts().map(p => html`
                    <div 
                      style="background: #0f172a; border: 1px solid #334155; border-radius: 6px; padding: 8px 10px; cursor: pointer; transition: all 0.15s ease; font-size: 12px;"
                      @click=${() => { this.aiPrompt = p.text; this.requestUpdate(); }}
                    >
                      <div style="font-weight: 600; color: #e2e8f0; margin-bottom: 2px;">${p.icon} ${p.title}</div>
                      <div style="color: #64748b; font-size: 11px; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;">${p.text}</div>
                    </div>
                  `)}
                </div>
              </div>

              <!-- Prompt Input Area with Mic -->
              <div>
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                  <label style="font-size: 12px; font-weight: 600; color: #94a3b8;">Describe your report in Thai or English:</label>
                  <div style="display: flex; align-items: center; gap: 6px;">
                    <select class="bpx-input" style="width: 110px; padding: 2px 4px; font-size: 11px;" .value=${this.aiLanguage} @change=${(e: any) => { this.aiLanguage = e.target.value; this.requestUpdate(); }}>
                      <option value="th-TH">🇹🇭 ไทย (Thai)</option>
                      <option value="en-US">🇺🇸 English (US)</option>
                      <option value="zh-CN">🇨🇳 中文 (Chinese)</option>
                      <option value="ja-JP">🇯🇵 日本語 (Japanese)</option>
                      <option value="es-ES">🇪🇸 Español</option>
                      <option value="de-DE">🇩🇪 Deutsch</option>
                      <option value="fr-FR">🇫🇷 Français</option>
                      <option value="ar-SA">🇸🇦 العربية</option>
                    </select>
                    <button 
                      class="bpx-btn ${this.isListening ? 'active' : ''}" 
                      style="${this.isListening ? 'background: #ef4444; border-color: #dc2626; animation: pulse 1s infinite;' : ''}"
                      @click=${() => this.toggleVoiceRecognition()}
                      title="Speech-to-Text Microphone"
                    >
                      ${this.isListening ? '⏹️ Listening...' : '🎙️ Mic'}
                    </button>
                  </div>
                </div>
                <textarea 
                  class="bpx-input" 
                  style="width: 100%; height: 90px; resize: vertical; font-family: inherit; font-size: 13px;" 
                  placeholder="e.g. สร้างรายงานสรุปยอดขายรายสัปดาห์ มีตารางสินค้า ยอดเงิน และ QR PromptPay..."
                  .value=${this.aiPrompt}
                  @input=${(e: any) => { this.aiPrompt = e.target.value; }}
                ></textarea>
              </div>

              <!-- Loading & Cancellation State -->
              ${this.isAiGenerating ? html`
                <div style="background: #0f172a; border: 1px solid #3b82f6; border-radius: 8px; padding: 14px; display: flex; align-items: center; justify-content: space-between;">
                  <div style="display: flex; align-items: center; gap: 10px;">
                    <div style="width: 18px; height: 18px; border: 2px solid #38bdf8; border-top-color: transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>
                    <span style="font-size: 13px; color: #38bdf8;">${this.aiLoadingStep}</span>
                  </div>
                  <button class="bpx-btn" style="background: #ef4444; border-color: #dc2626; color: white;" @click=${() => this.cancelAiGeneration()}>
                    ⏹️ Cancel Generation
                  </button>
                </div>
              ` : ''}

              <!-- Error Alert -->
              ${this.aiError ? html`
                <div style="background: rgba(239, 68, 68, 0.1); border: 1px solid #ef4444; border-radius: 8px; padding: 12px; color: #fca5a5; font-size: 13px;">
                  ⚠️ ${this.aiError}
                </div>
              ` : ''}

              <!-- Generated Preview Result -->
              ${this.aiGeneratedBpx ? html`
                <div style="background: #0f172a; border: 1px solid #22c55e; border-radius: 8px; padding: 12px;">
                  <div style="font-size: 12px; font-weight: 700; color: #4ade80; margin-bottom: 6px;">✓ Generated .bpx Schema Ready:</div>
                  <pre style="margin: 0; max-height: 120px; overflow-y: auto; font-size: 11px; color: #94a3b8;">${this.aiGeneratedBpx}</pre>
                </div>
              ` : ''}
            </div>

            <div style="padding: 14px 20px; background: #0f172a; border-top: 1px solid #334155; display: flex; justify-content: flex-end; gap: 8px;">
              <button class="bpx-btn" @click=${() => { this.showAiModal = false; }}>Close</button>
              ${this.aiGeneratedBpx ? html`
                <button class="bpx-btn" style="background: #22c55e; border-color: #16a34a; color: white; font-weight: 600;" @click=${() => this.applyGeneratedSchema()}>
                  ✓ Apply to Canvas
                </button>
              ` : html`
                <button class="bpx-btn active" ?disabled=${this.isAiGenerating || !this.aiPrompt.trim()} @click=${() => this.executeAiGeneration()}>
                  🚀 Generate Report
                </button>
              `}
            </div>
          </div>
        </div>
      ` : ''}

      <!-- AI Settings Modal (BYOK & Free-Text Model) -->
      ${this.showAiSettingsModal ? html`
        <div style="position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; align-items: center; justify-content: center; z-index: 1000;">
          <div style="background: #1e293b; border: 1px solid #475569; border-radius: 12px; width: 520px; max-width: 95vw; overflow: hidden; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.5);">
            <div style="padding: 16px 20px; background: #0f172a; border-bottom: 1px solid #334155; display: flex; justify-content: space-between; align-items: center;">
              <div style="font-weight: 700; font-size: 15px; color: #38bdf8;">⚙️ AI Provider & BYOK Settings</div>
              <button class="bpx-btn" @click=${() => { this.showAiSettingsModal = false; }}>✕</button>
            </div>

            <div style="padding: 20px; display: flex; flex-direction: column; gap: 12px;">
              <div class="bpx-prop-row">
                <label>Provider</label>
                <select class="bpx-input" style="width: 220px;" .value=${this.aiProvider} @change=${(e: any) => { this.aiProvider = e.target.value; this.requestUpdate(); }}>
                  <option value="gemini">Google Gemini</option>
                  <option value="openai">OpenAI / ChatGPT</option>
                  <option value="azure">Azure OpenAI</option>
                  <option value="claude">Anthropic Claude</option>
                  <option value="ollama">Ollama (Local / On-Premise)</option>
                </select>
              </div>

              <!-- Free-Text Model Input with Datalist -->
              <div class="bpx-prop-row">
                <label>Model Name</label>
                <input 
                  list="bpx-model-suggestions"
                  class="bpx-input" 
                  style="width: 220px;" 
                  type="text" 
                  placeholder="e.g. gemini-2.5-flash, gpt-4o"
                  .value=${this.aiModelName}
                  @input=${(e: any) => { this.aiModelName = e.target.value; }}
                />
                <datalist id="bpx-model-suggestions">
                  <option value="gemini-2.5-flash"></option>
                  <option value="gemini-2.5-pro"></option>
                  <option value="gpt-4o"></option>
                  <option value="gpt-4o-mini"></option>
                  <option value="claude-3-5-sonnet-20241022"></option>
                  <option value="deepseek-r1"></option>
                  <option value="llama3.3"></option>
                  <option value="qwen2.5"></option>
                </datalist>
              </div>

              <div class="bpx-prop-row">
                <label>API Key (BYOK)</label>
                <input 
                  class="bpx-input" 
                  style="width: 220px;" 
                  type="password" 
                  placeholder="AI Key (Saved locally)"
                  .value=${this.aiApiKey}
                  @input=${(e: any) => { this.aiApiKey = e.target.value; }}
                />
              </div>

              <div class="bpx-prop-row">
                <label>Custom Endpoint (Optional)</label>
                <input 
                  class="bpx-input" 
                  style="width: 220px;" 
                  type="text" 
                  placeholder="http://localhost:11434"
                  .value=${this.aiEndpointUrl}
                  @input=${(e: any) => { this.aiEndpointUrl = e.target.value; }}
                />
              </div>

              ${this.testPingStatus ? html`
                <div style="font-size: 12px; padding: 8px 10px; border-radius: 6px; background: #0f172a; color: ${this.testPingStatus.startsWith('✓') ? '#4ade80' : '#f87171'};">
                  ${this.testPingStatus}
                </div>
              ` : ''}
            </div>

            <div style="padding: 14px 20px; background: #0f172a; border-top: 1px solid #334155; display: flex; justify-content: space-between; align-items: center;">
              <button class="bpx-btn" ?disabled=${this.isPinging} @click=${() => this.testAiConnection()}>
                ${this.isPinging ? 'Pinging...' : '⚡ Test Connection'}
              </button>
              <div style="display: flex; gap: 8px;">
                <button class="bpx-btn" @click=${() => { this.showAiSettingsModal = false; }}>Cancel</button>
                <button class="bpx-btn active" @click=${() => this.saveAiSettings()}>Save Settings</button>
              </div>
            </div>
          </div>
        </div>
      ` : ''}

      <!-- AI History & Undo Drawer -->
      ${this.showHistoryDrawer ? html`
        <div style="position: fixed; inset: 0; background: rgba(0,0,0,0.6); display: flex; justify-content: flex-end; z-index: 1000;">
          <div style="background: #1e293b; border-left: 1px solid #475569; width: 440px; max-width: 90vw; height: 100%; display: flex; flex-direction: column;">
            <div style="padding: 16px; background: #0f172a; border-bottom: 1px solid #334155; display: flex; justify-content: space-between; align-items: center;">
              <div style="font-weight: 700; color: #38bdf8;">📜 SQLite AI Prompt History</div>
              <button class="bpx-btn" @click=${() => { this.showHistoryDrawer = false; }}>✕</button>
            </div>

            <div style="flex: 1; overflow-y: auto; padding: 12px; display: flex; flex-direction: column; gap: 8px;">
              ${this.aiHistoryList.length === 0 ? html`
                <div style="text-align: center; color: #64748b; padding: 30px;">No AI history recorded in SQLite yet.</div>
              ` : this.aiHistoryList.map(h => html`
                <div style="background: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 10px;">
                  <div style="display: flex; justify-content: space-between; font-size: 11px; color: #64748b; margin-bottom: 4px;">
                    <span>${h.provider} (${h.modelName})</span>
                    <span>${new Date(h.createdAtUtc).toLocaleTimeString()}</span>
                  </div>
                  <div style="font-size: 13px; color: #e2e8f0; margin-bottom: 8px;">"${h.prompt}"</div>
                  <div style="display: flex; justify-content: flex-end; gap: 6px;">
                    <button class="bpx-btn" style="font-size: 11px; padding: 2px 8px;" @click=${() => { this.aiPrompt = h.prompt; this.showAiModal = true; this.showHistoryDrawer = false; }}>
                      Re-run
                    </button>
                    ${h.hasSnapshot ? html`
                      <button class="bpx-btn" style="font-size: 11px; padding: 2px 8px; color: #38bdf8; border-color: #0284c7;" @click=${() => this.rollbackHistory(h.id)}>
                        ↺ Rollback Snapshot
                      </button>
                    ` : ''}
                  </div>
                </div>
              `)}
            </div>
          </div>
        </div>
      ` : ''}
    `;
  }
}

