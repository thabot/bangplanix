import { LitElement, html, css } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { BangplanixViewerCore } from './viewer-core.js';

export interface ParameterOption {
  label: string;
  value: any;
}

export interface ParameterDefinition {
  name: string;
  label?: string;
  type: string;
  defaultValue?: any;
  isRequired?: boolean;
  multiSelect?: boolean;
  cascadingParent?: string;
  validationPattern?: string;
  minValue?: number;
  maxValue?: number;
  availableValues?: ParameterOption[];
}

@customElement('bangplanix-viewer')
export class BangplanixViewer extends LitElement {
  @property({ type: String }) src: string = '';
  @property({ type: String }) serverUrl: string = 'http://localhost:9545';
  @property({ type: String }) template: string = '';
  @property({ type: String }) data: string = '';
  @property({ type: Array }) parameterDefs: ParameterDefinition[] = [];

  @state() private pdfUrl: string | null = null;
  @state() private isLoading: boolean = false;
  @state() private errorMessage: string | null = null;
  @state() private zoomLevel: number = 100;
  @state() private zoomMode: string = 'manual';

  // Search
  @state() private searchOpen: boolean = false;
  @state() private searchQuery: string = '';
  @state() private searchTotal: number = 0;
  @state() private searchCurrent: number = 0;
  @state() private isFullscreen: boolean = false;

  // Mobile State
  @state() private mobileActionsOpen: boolean = false;

  // Parameters
  @state() private parameterPanelOpen: boolean = false;
  @state() private parameters: Record<string, any> = {};
  @state() private parameterOptionsMap: Record<string, ParameterOption[]> = {};
  @state() private validationErrors: string[] = [];

  // Touch & Pinch Zoom Tracking
  private initialPinchDistance: number = 0;
  private initialZoomLevel: number = 100;
  private lastTapTime: number = 0;

  private core: BangplanixViewerCore = new BangplanixViewerCore();

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      width: 100%;
      height: 100%;
      min-height: 500px;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
      background-color: #1e1e2d;
      color: #ffffff;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 4px 24px rgba(0, 0, 0, 0.35);
      position: relative;
      touch-action: pan-x pan-y;
      -webkit-tap-highlight-color: transparent;
    }

    :host([fullscreen]) {
      position: fixed;
      inset: 0;
      z-index: 999999;
      border-radius: 0;
      height: 100vh;
      width: 100vw;
    }

    .toolbar {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      padding: 8px 16px;
      background: linear-gradient(135deg, #181824 0%, #252538 100%);
      border-bottom: 1px solid #32324d;
      gap: 10px;
      user-select: none;
      z-index: 2;
    }

    .toolbar-group {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .brand-title {
      font-weight: 700;
      font-size: 14px;
      color: #38bdf8;
      letter-spacing: 0.5px;
      display: flex;
      align-items: center;
      gap: 6px;
      margin-right: 8px;
    }

    button {
      background-color: #2b2b42;
      color: #e2e8f0;
      border: 1px solid #3f3f5a;
      border-radius: 6px;
      padding: 6px 10px;
      font-size: 12px;
      font-weight: 500;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 4px;
      min-height: 32px;
      transition: all 0.2s ease;
    }

    button:hover:not(:disabled) {
      background-color: #3b3b5c;
      border-color: #38bdf8;
      color: #ffffff;
    }

    button:active:not(:disabled) {
      transform: scale(0.97);
    }

    button:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

    button.primary {
      background-color: #0284c7;
      border-color: #0369a1;
      color: #ffffff;
    }

    button.primary:hover:not(:disabled) {
      background-color: #0369a1;
    }

    button.active {
      background-color: #0369a1;
      border-color: #38bdf8;
      color: #ffffff;
    }

    .zoom-select {
      background-color: #2b2b42;
      color: #e2e8f0;
      border: 1px solid #3f3f5a;
      border-radius: 6px;
      padding: 5px 8px;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      outline: none;
      min-height: 32px;
    }

    /* Main Container & Parameter Drawer */
    .main-layout {
      display: flex;
      flex: 1;
      position: relative;
      overflow: hidden;
    }

    .parameter-drawer {
      width: 280px;
      background: #181824;
      border-right: 1px solid #32324d;
      display: flex;
      flex-direction: column;
      padding: 16px;
      gap: 14px;
      overflow-y: auto;
      -webkit-overflow-scrolling: touch;
      animation: slideRight 0.2s ease;
      z-index: 20;
    }

    @keyframes slideRight {
      from { transform: translateX(-100%); }
      to { transform: translateX(0); }
    }

    .drawer-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-weight: 700;
      font-size: 13px;
      color: #38bdf8;
      border-bottom: 1px solid #2d2d42;
      padding-bottom: 8px;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .form-group label {
      font-size: 12px;
      font-weight: 600;
      color: #cbd5e1;
    }

    .form-group input,
    .form-group select {
      background-color: #0f172a;
      color: #ffffff;
      border: 1px solid #334155;
      border-radius: 6px;
      padding: 8px 10px;
      font-size: 13px;
      outline: none;
      min-height: 38px;
    }

    .form-group input:focus,
    .form-group select:focus {
      border-color: #38bdf8;
    }

    .param-error {
      color: #ef4444;
      font-size: 11px;
    }

    .drawer-actions {
      display: flex;
      gap: 8px;
      margin-top: 8px;
    }

    /* Search Bar */
    .search-panel {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 16px;
      background-color: #1f293d;
      border-bottom: 1px solid #38bdf844;
      animation: slideDown 0.2s ease;
      z-index: 10;
    }

    @keyframes slideDown {
      from { transform: translateY(-100%); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }

    .search-input-group {
      display: flex;
      align-items: center;
      background: #0f172a;
      border: 1px solid #334155;
      border-radius: 6px;
      padding: 2px 8px;
      flex: 1;
      max-width: 400px;
    }

    .search-input-group input {
      background: transparent;
      border: none;
      color: #f1f5f9;
      font-size: 13px;
      outline: none;
      padding: 6px 4px;
      flex: 1;
    }

    .search-counter {
      font-size: 11px;
      color: #94a3b8;
      font-weight: 600;
      margin-left: 6px;
    }

    .content-viewport {
      flex: 1;
      position: relative;
      display: flex;
      justify-content: center;
      align-items: center;
      background-color: #0f172a;
      overflow: auto;
      -webkit-overflow-scrolling: touch;
      padding: 16px;
      touch-action: pan-x pan-y pinch-zoom;
    }

    iframe {
      width: 100%;
      height: 100%;
      border: none;
      background-color: #ffffff;
      border-radius: 4px;
      transition: transform 0.15s ease-out;
    }

    .loading-overlay {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: rgba(15, 23, 42, 0.85);
      z-index: 10;
      gap: 12px;
    }

    .spinner {
      width: 36px;
      height: 36px;
      border: 3px solid rgba(56, 189, 248, 0.2);
      border-top-color: #38bdf8;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .error-banner {
      background-color: #ef4444;
      color: #ffffff;
      padding: 12px 16px;
      border-radius: 6px;
      font-size: 13px;
      max-width: 80%;
      text-align: center;
    }

    /* Mobile Action Overflow Popup */
    .mobile-actions-menu {
      position: absolute;
      top: 50px;
      right: 12px;
      background: #181824;
      border: 1px solid #3f3f5a;
      border-radius: 8px;
      box-shadow: 0 8px 30px rgba(0, 0, 0, 0.5);
      display: flex;
      flex-direction: column;
      padding: 8px;
      gap: 6px;
      z-index: 40;
      min-width: 180px;
      animation: fadeIn 0.15s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-8px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .mobile-actions-menu button {
      width: 100%;
      justify-content: flex-start;
      padding: 10px 12px;
      font-size: 13px;
    }

    .mobile-only {
      display: none;
    }

    .desktop-only {
      display: flex;
    }

    /* 📱 Responsive Mobile & Tablet Styles */
    @media (max-width: 768px) {
      .desktop-only {
        display: none !important;
      }

      .mobile-only {
        display: inline-flex !important;
      }

      .toolbar {
        padding: 8px 12px;
        gap: 6px;
      }

      .brand-title span {
        display: none;
      }

      button {
        padding: 8px 10px;
        min-height: 38px;
        font-size: 13px;
      }

      .parameter-drawer {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        border-right: none;
        z-index: 50;
        padding: 20px 16px;
        background: #14141f;
      }

      .search-panel {
        padding: 8px 12px;
        flex-wrap: wrap;
      }

      .search-input-group {
        max-width: 100%;
        width: 100%;
      }

      .content-viewport {
        padding: 8px;
      }
    }

    @media (max-width: 480px) {
      .toolbar-group {
        gap: 4px;
      }

      button {
        padding: 6px 8px;
        font-size: 12px;
      }
    }
  `;

  async connectedCallback() {
    super.connectedCallback();
    this.core = new BangplanixViewerCore({
      serverUrl: this.serverUrl,
      template: this.template,
      data: this.data,
      src: this.src,
      parameterDefs: this.parameterDefs
    });

    if (this.parameterDefs.length > 0) {
      this.core.setParameterDefinitions(this.parameterDefs);
      this.parameters = { ...this.core.parameters };
      this.parameterOptionsMap = { ...this.core.parameterOptionsMap };
    }

    if (this.src) {
      this.pdfUrl = this.src;
    } else if (this.template) {
      await this.loadReport();
    }

    window.addEventListener('keydown', this.handleGlobalKeyDown.bind(this));
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.removeEventListener('keydown', this.handleGlobalKeyDown.bind(this));
  }

  private handleGlobalKeyDown(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') {
      e.preventDefault();
      this.toggleSearch();
    }
  }

  // 📱 Mobile Touch & Pinch Gestures
  private handleTouchStart(e: TouchEvent) {
    if (e.touches.length === 2) {
      // 2 fingers pinch start
      const dx = e.touches[0].clientX - e.touches[1].clientX;
      const dy = e.touches[0].clientY - e.touches[1].clientY;
      this.initialPinchDistance = Math.hypot(dx, dy);
      this.initialZoomLevel = this.zoomLevel;
    } else if (e.touches.length === 1) {
      // Check for double tap
      const now = Date.now();
      if (now - this.lastTapTime < 300) {
        e.preventDefault();
        this.handleDoubleTap();
      }
      this.lastTapTime = now;
    }
  }

  private handleTouchMove(e: TouchEvent) {
    if (e.touches.length === 2 && this.initialPinchDistance > 0) {
      const dx = e.touches[0].clientX - e.touches[1].clientX;
      const dy = e.touches[0].clientY - e.touches[1].clientY;
      const currentDistance = Math.hypot(dx, dy);
      this.zoomLevel = this.core.calculatePinchZoom(this.initialPinchDistance, currentDistance, this.initialZoomLevel);
      this.zoomMode = this.core.zoomMode;
    }
  }

  private handleTouchEnd() {
    this.initialPinchDistance = 0;
  }

  private handleDoubleTap() {
    const viewport = this.shadowRoot?.querySelector('.content-viewport');
    const width = viewport ? viewport.clientWidth : 400;
    this.zoomLevel = this.core.handleDoubleTapZoom(width, 794);
    this.zoomMode = this.core.zoomMode;
  }

  toggleMobileActions() {
    this.mobileActionsOpen = this.core.toggleMobileActionsMenu();
  }

  closeMobileActions() {
    this.mobileActionsOpen = this.core.toggleMobileActionsMenu(false);
  }

  async loadReport() {
    this.isLoading = true;
    this.errorMessage = null;

    try {
      const response = await fetch(`${this.serverUrl}/api/v1/report/render`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          templatePath: this.template,
          dataJson: this.data || '{}',
          parameters: this.parameters,
          format: 'pdf'
        })
      });

      if (!response.ok) {
        throw new Error(`Report render failed: ${response.status} ${response.statusText}`);
      }

      const blob = await response.blob();
      if (this.pdfUrl) {
        URL.revokeObjectURL(this.pdfUrl);
      }
      this.pdfUrl = URL.createObjectURL(blob);
      this.dispatchEvent(new CustomEvent('report-loaded', { detail: { template: this.template, parameters: this.parameters } }));
    } catch (err: any) {
      this.errorMessage = err.message || 'Unknown error occurred while rendering report';
    } finally {
      this.isLoading = false;
    }
  }

  async exportExcel() {
    this.closeMobileActions();
    try {
      const response = await fetch(`${this.serverUrl}/api/v1/report/render`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          templatePath: this.template,
          dataJson: this.data || '{}',
          parameters: this.parameters,
          format: 'xlsx'
        })
      });

      if (!response.ok) {
        throw new Error(`Excel export failed: ${response.status}`);
      }

      const blob = await response.blob();
      const downloadUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = downloadUrl;
      a.download = `${this.template.replace(/[/\\?%*:|"<>]/g, '_') || 'report'}.xlsx`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(downloadUrl);
      this.dispatchEvent(new CustomEvent('export-completed', { detail: { format: 'xlsx' } }));
    } catch (err: any) {
      alert(`Export XLSX Error: ${err.message}`);
    }
  }

  downloadPdf() {
    this.closeMobileActions();
    if (!this.pdfUrl) return;
    const a = document.createElement('a');
    a.href = this.pdfUrl;
    a.download = `${this.template.replace(/[/\\?%*:|"<>]/g, '_') || 'report'}.pdf`;
    document.body.appendChild(a);
    a.click();
    a.remove();
  }

  // Parameter Handling
  toggleParameterDrawer() {
    this.closeMobileActions();
    this.parameterPanelOpen = this.core.toggleParameterPanel();
  }

  handleParameterChange(name: string, value: any) {
    this.core.setParameter(name, value);
    this.parameters = { ...this.core.parameters };
    this.parameterOptionsMap = { ...this.core.parameterOptionsMap };
    this.dispatchEvent(new CustomEvent('parameter-change', { detail: { name, value, allParameters: this.parameters } }));
  }

  async applyParameters() {
    const valResult = this.core.validateParameters();
    this.validationErrors = valResult.errors;

    if (!valResult.isValid) {
      return;
    }

    if (window.innerWidth <= 768) {
      this.parameterPanelOpen = false;
    }

    await this.loadReport();
  }

  resetParameters() {
    this.parameters = { ...this.core.resetParametersToDefaults() };
    this.parameterOptionsMap = { ...this.core.parameterOptionsMap };
    this.validationErrors = [];
  }

  // Zoom Handling
  handleZoomIn() {
    this.zoomLevel = this.core.zoomIn(25);
    this.zoomMode = this.core.zoomMode;
  }

  handleZoomOut() {
    this.zoomLevel = this.core.zoomOut(25);
    this.zoomMode = this.core.zoomMode;
  }

  handleZoomReset() {
    this.zoomLevel = this.core.resetZoom();
    this.zoomMode = this.core.zoomMode;
  }

  handleFitWidth() {
    const viewport = this.shadowRoot?.querySelector('.content-viewport');
    const width = viewport ? viewport.clientWidth : 1000;
    this.zoomLevel = this.core.fitToWidth(width, 794);
    this.zoomMode = this.core.zoomMode;
  }

  handleFitPage() {
    const viewport = this.shadowRoot?.querySelector('.content-viewport');
    const height = viewport ? viewport.clientHeight : 800;
    this.zoomLevel = this.core.fitToPage(height, 1123);
    this.zoomMode = this.core.zoomMode;
  }

  handleZoomSelect(e: Event) {
    const target = e.target as HTMLSelectElement;
    const val = target.value;
    if (val === 'fit-width') {
      this.handleFitWidth();
    } else if (val === 'fit-page') {
      this.handleFitPage();
    } else {
      this.zoomLevel = this.core.setZoom(parseInt(val, 10));
      this.zoomMode = this.core.zoomMode;
    }
  }

  // Search Handling
  toggleSearch() {
    this.closeMobileActions();
    this.searchOpen = this.core.toggleSearch();
    if (this.searchOpen) {
      setTimeout(() => {
        const input = this.shadowRoot?.querySelector('.search-input-group input') as HTMLInputElement;
        input?.focus();
      }, 50);
    }
  }

  handleSearchInput(e: Event) {
    const input = e.target as HTMLInputElement;
    this.searchQuery = input.value;
    const res = this.core.executeSearch(this.searchQuery, []);
    this.searchTotal = res.total;
    this.searchCurrent = res.current;
  }

  handleSearchNext() {
    const res = this.core.nextSearchResult();
    if (res) {
      this.searchCurrent = res.current;
    }
  }

  handleSearchPrev() {
    const res = this.core.previousSearchResult();
    if (res) {
      this.searchCurrent = res.current;
    }
  }

  // Fullscreen
  toggleFullscreen() {
    this.closeMobileActions();
    this.isFullscreen = this.core.toggleFullscreen();
    if (this.isFullscreen) {
      this.setAttribute('fullscreen', '');
    } else {
      this.removeAttribute('fullscreen');
    }
  }

  handlePrint() {
    this.closeMobileActions();
    const iframe = this.shadowRoot?.querySelector('iframe');
    if (iframe && iframe.contentWindow) {
      iframe.contentWindow.focus();
      iframe.contentWindow.print();
    }
  }

  render() {
    return html`
      <div class="toolbar">
        <div class="toolbar-group">
          <span class="brand-title">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
              <path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
            </svg>
            <span>Bangplanix</span>
          </span>
          <button @click=${this.toggleParameterDrawer} class=${this.parameterPanelOpen ? 'active' : ''} title="Toggle Parameters / Filter Panel">
            ⚙️ Filters ${this.parameterDefs.length > 0 ? `(${this.parameterDefs.length})` : ''}
          </button>
          <button @click=${this.toggleSearch} class=${this.searchOpen ? 'active' : ''} title="Search Document (Ctrl+F)">
            🔍 <span class="desktop-only">Search</span>
          </button>
        </div>

        <div class="toolbar-group">
          <button @click=${this.handleZoomOut} ?disabled=${this.zoomLevel <= 25} title="Zoom Out">-</button>
          <select class="zoom-select" .value=${this.zoomMode === 'manual' ? `${this.zoomLevel}%` : this.zoomMode} @change=${this.handleZoomSelect}>
            <option value="50">50%</option>
            <option value="75">75%</option>
            <option value="100">100%</option>
            <option value="125">125%</option>
            <option value="150">150%</option>
            <option value="200">200%</option>
            <option value="300">300%</option>
            <option value="fit-width">Fit Width</option>
            <option value="fit-page">Fit Page</option>
          </select>
          <button @click=${this.handleZoomIn} ?disabled=${this.zoomLevel >= 500} title="Zoom In">+</button>
          <button @click=${this.handleFitWidth} class="desktop-only ${this.zoomMode === 'fit-width' ? 'active' : ''}" title="Fit to Width">Fit Width</button>
          <button @click=${this.handleFitPage} class="desktop-only ${this.zoomMode === 'fit-page' ? 'active' : ''}" title="Fit to Page">Fit Page</button>
        </div>

        <div class="toolbar-group desktop-only">
          <button @click=${this.handlePrint} ?disabled=${!this.pdfUrl} title="Print Document">
            Print
          </button>
          <button @click=${this.downloadPdf} ?disabled=${!this.pdfUrl} title="Download PDF Document">
            Download PDF
          </button>
          <button @click=${this.exportExcel} class="primary" title="Export to Excel XLSX">
            Export XLSX
          </button>
          <button @click=${this.toggleFullscreen} title="Toggle Fullscreen">
            ${this.isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}
          </button>
          <button @click=${this.loadReport} title="Refresh / Reload">
            Refresh
          </button>
        </div>

        <!-- 📱 Mobile Overflow Actions Button -->
        <div class="toolbar-group mobile-only">
          <button @click=${this.toggleMobileActions} class=${this.mobileActionsOpen ? 'active' : ''} title="More Actions">
            ⋮
          </button>
        </div>
      </div>

      <!-- 📱 Mobile Actions Dropdown Menu -->
      ${this.mobileActionsOpen ? html`
        <div class="mobile-actions-menu">
          <button @click=${this.downloadPdf} ?disabled=${!this.pdfUrl}>📥 Download PDF</button>
          <button @click=${this.exportExcel} class="primary">📊 Export XLSX</button>
          <button @click=${this.handlePrint} ?disabled=${!this.pdfUrl}>🖨️ Print</button>
          <button @click=${this.handleFitWidth}>↔️ Fit to Width</button>
          <button @click=${this.handleFitPage}>↕️ Fit to Page</button>
          <button @click=${this.toggleFullscreen}>⛶ ${this.isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}</button>
          <button @click=${() => { this.closeMobileActions(); this.loadReport(); }}>🔄 Refresh</button>
        </div>
      ` : ''}

      ${this.searchOpen ? html`
        <div class="search-panel">
          <div class="search-input-group">
            <input
              type="text"
              placeholder="Find in document..."
              .value=${this.searchQuery}
              @input=${this.handleSearchInput}
              @keydown=${(e: KeyboardEvent) => { if (e.key === 'Enter') { e.shiftKey ? this.handleSearchPrev() : this.handleSearchNext(); } }}
            />
            <span class="search-counter">${this.searchTotal > 0 ? `${this.searchCurrent} of ${this.searchTotal}` : (this.searchQuery ? '0 found' : '')}</span>
          </div>
          <button @click=${this.handleSearchPrev} ?disabled=${this.searchTotal === 0} title="Previous Match (Shift+Enter)">▲</button>
          <button @click=${this.handleSearchNext} ?disabled=${this.searchTotal === 0} title="Next Match (Enter)">▼</button>
          <button @click=${this.toggleSearch} title="Close Search">✕</button>
        </div>
      ` : ''}

      <div class="main-layout">
        ${this.parameterPanelOpen ? html`
          <div class="parameter-drawer">
            <div class="drawer-header">
              <span>Report Parameters</span>
              <button @click=${this.toggleParameterDrawer} title="Close Panel">✕</button>
            </div>

            ${this.validationErrors.map(err => html`
              <div class="param-error">⚠️ ${err}</div>
            `)}

            ${this.parameterDefs.map(p => {
              const options = this.parameterOptionsMap[p.name] || p.availableValues || [];
              const val = this.parameters[p.name] !== undefined ? this.parameters[p.name] : (p.defaultValue || '');

              return html`
                <div class="form-group">
                  <label>${p.label || p.name} ${p.isRequired ? '*' : ''}</label>
                  ${options.length > 0 ? html`
                    <select
                      .value=${val}
                      @change=${(e: Event) => this.handleParameterChange(p.name, (e.target as HTMLSelectElement).value)}
                    >
                      <option value="">-- Select --</option>
                      ${options.map(opt => html`
                        <option value=${opt.value} ?selected=${opt.value === val}>${opt.label}</option>
                      `)}
                    </select>
                  ` : (p.type === 'DateTime' ? html`
                    <input
                      type="date"
                      .value=${val}
                      @input=${(e: Event) => this.handleParameterChange(p.name, (e.target as HTMLInputElement).value)}
                    />
                  ` : (p.type === 'Number' ? html`
                    <input
                      type="number"
                      min=${p.minValue !== undefined ? p.minValue : ''}
                      max=${p.maxValue !== undefined ? p.maxValue : ''}
                      .value=${val}
                      @input=${(e: Event) => this.handleParameterChange(p.name, Number((e.target as HTMLInputElement).value))}
                    />
                  ` : html`
                    <input
                      type="text"
                      .value=${val}
                      @input=${(e: Event) => this.handleParameterChange(p.name, (e.target as HTMLInputElement).value)}
                    />
                  `))}
                </div>
              `;
            })}

            <div class="drawer-actions">
              <button @click=${this.applyParameters} class="primary" style="flex: 1;">Apply Filters</button>
              <button @click=${this.resetParameters} style="flex: 1;">Reset</button>
            </div>
          </div>
        ` : ''}

        <div
          class="content-viewport"
          @touchstart=${this.handleTouchStart}
          @touchmove=${this.handleTouchMove}
          @touchend=${this.handleTouchEnd}
        >
          ${this.isLoading ? html`
            <div class="loading-overlay">
              <div class="spinner"></div>
              <span>Rendering Report in Native AOT...</span>
            </div>
          ` : ''}

          ${this.errorMessage ? html`
            <div class="error-banner">
              <strong>Error:</strong> ${this.errorMessage}
            </div>
          ` : ''}

          ${this.pdfUrl && !this.errorMessage ? html`
            <iframe
              src="${this.pdfUrl}#zoom=${this.zoomLevel}"
              style="transform: scale(${this.zoomLevel / 100}); transform-origin: top center;"
              title="Bangplanix Report Document"
            ></iframe>
          ` : (!this.isLoading && !this.errorMessage ? html`
            <div style="color: #64748b; font-size: 14px;">No report loaded. Specify 'src' or 'template' attribute.</div>
          ` : '')}
        </div>
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'bangplanix-viewer': BangplanixViewer;
  }
}