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
      e.dataTransfer.setData('text/plain', type);
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

      <!-- Bottom Monaco Editor / Code Area -->
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
    `;
  }
}
