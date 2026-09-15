/**
 * Visual Designer Web Component (<bangplanix-designer>) for Docs
 */
// @ts-ignore
import { BangplanixDesignerCore } from './designer-core.js';

export class BangplanixDesigner extends HTMLElement {
  public core: any;
  public serverUrl: string = 'http://localhost:9545';
  public readOnly: boolean = false;
  public initialReport: any = null;

  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
    this.core = new BangplanixDesignerCore();
  }

  static get observedAttributes(): string[] {
    return ['server-url', 'read-only'];
  }

  attributeChangedCallback(name: string, oldValue: string | null, newValue: string | null): void {
    if (oldValue === newValue) return;
    if (name === 'server-url') this.serverUrl = newValue || '';
    if (name === 'read-only') this.readOnly = newValue !== null && newValue !== 'false';
  }

  connectedCallback(): void {
    this.render();
  }

  requestUpdate(): void {
    this.render();
  }

  private handleToolDragStart(e: DragEvent, type: string): void {
    if (e.dataTransfer) {
      e.dataTransfer.setData('text/plain', type);
    }
  }

  private handleCanvasDrop(e: DragEvent, bandName: string): void {
    e.preventDefault();
    const type = e.dataTransfer?.getData('text/plain') || 'Text';
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const x = (e.clientX - rect.left) / (this.core.zoomLevel / 100);
    const y = (e.clientY - rect.top) / (this.core.zoomLevel / 100);

    this.core.addElement(bandName, {
      type,
      x: Math.round(x),
      y: Math.round(y),
      width: type === 'Chart' ? 300 : (type === 'Barcode' ? 180 : 120),
      height: type === 'Chart' ? 180 : (type === 'Barcode' ? 60 : 25),
      text: type === 'Text' ? 'Label Text' : (type === 'Barcode' ? '123456789' : '')
    });
    this.dispatchChange();
    this.requestUpdate();
  }

  public dispatchChange(): void {
    this.dispatchEvent(new CustomEvent('report-change', {
      detail: { report: this.core.report },
      bubbles: true,
      composed: true
    }));
  }

  public setPaperSize(kind: string): void {
    if (!this.core.report) return;
    if (!this.core.report.pageSetup) {
      this.core.report.pageSetup = {};
    }
    const ps = this.core.report.pageSetup;
    ps.paperKind = kind;
    if (kind === 'A4') {
      ps.width = 595.28;
      ps.height = 841.89;
    } else if (kind === 'Letter') {
      ps.width = 612;
      ps.height = 792;
    } else if (kind === 'Legal') {
      ps.width = 612;
      ps.height = 1008;
    } else if (kind === 'POS80') {
      ps.width = 226.77;
      ps.height = 600;
    }
    this.core.pushHistory(`Change Paper Size to ${kind}`);
    this.dispatchChange();
    this.requestUpdate();
  }

  private handleElementClick(e: MouseEvent, id: string): void {
    e.stopPropagation();
    this.core.selectElement(id, e.shiftKey || e.ctrlKey);
    this.requestUpdate();
  }

  private handleElementDblClick(e: MouseEvent, id: string): void {
    e.stopPropagation();
    const info = this.core.findElement(id);
    if (!info) return;

    const elDom = this.shadowRoot?.querySelector(`.bpx-element-view[data-el-id="${id}"]`) as HTMLElement | null;
    if (!elDom) return;

    if (elDom.querySelector('.bpx-inline-editor')) return;

    const currentText = info.element.text || '';
    const inlineInput = document.createElement('input');
    inlineInput.type = 'text';
    inlineInput.className = 'bpx-inline-editor';
    inlineInput.value = currentText;
    inlineInput.style.cssText = `
      width: 100%;
      height: 100%;
      border: 2px solid #2563eb;
      background: #ffffff;
      color: #0f172a;
      font-size: ${info.element.style?.fontSize || 12}px;
      font-family: inherit;
      padding: 0 4px;
      outline: none;
      box-sizing: border-box;
      border-radius: 2px;
      z-index: 100;
    `;

    let committed = false;
    const finishEdit = () => {
      if (committed) return;
      committed = true;
      const newText = inlineInput.value;
      info.element.text = newText;
      this.core.pushHistory('Inline Edit Text');
      
      const propTextInput = this.shadowRoot?.getElementById('prop-text') as HTMLInputElement | null;
      if (propTextInput && this.core.selectedElementIds.includes(id)) {
        propTextInput.value = newText;
      }
      
      this.dispatchChange();
      this.requestUpdate();
    };

    inlineInput.addEventListener('blur', finishEdit);
    inlineInput.addEventListener('keydown', (ke: KeyboardEvent) => {
      if (ke.key === 'Enter') {
        ke.preventDefault();
        inlineInput.blur();
      } else if (ke.key === 'Escape') {
        inlineInput.value = currentText;
        inlineInput.blur();
      }
    });

    elDom.innerHTML = '';
    elDom.appendChild(inlineInput);
    inlineInput.focus();
    inlineInput.select();
  }

  private handleElementMouseDown(e: MouseEvent, id: string): void {
    if (e.button !== 0) return;
    const target = e.target as HTMLElement;
    if (target.tagName === 'INPUT') return;

    const info = this.core.findElement(id);
    if (!info) return;

    if (!this.core.selectedElementIds.includes(id)) {
      this.core.selectElement(id);
      this.requestUpdate();
    }

    const startX = e.clientX;
    const startY = e.clientY;
    const initialElX = info.element.x || 0;
    const initialElY = info.element.y || 0;
    const zoom = (this.core.zoomLevel || 100) / 100;
    let moved = false;

    const onMouseMove = (moveEvent: MouseEvent) => {
      const dx = (moveEvent.clientX - startX) / zoom;
      const dy = (moveEvent.clientY - startY) / zoom;
      if (Math.abs(dx) > 1 || Math.abs(dy) > 1) {
        moved = true;
        const newX = Math.max(0, Math.round(initialElX + dx));
        const newY = Math.max(0, Math.round(initialElY + dy));
        info.element.x = newX;
        info.element.y = newY;

        const elDom = this.shadowRoot?.querySelector(`.bpx-element-view[data-el-id="${id}"]`) as HTMLElement | null;
        if (elDom) {
          elDom.style.left = `${newX}px`;
          elDom.style.top = `${newY}px`;
        }

        const propX = this.shadowRoot?.getElementById('prop-x') as HTMLInputElement | null;
        const propY = this.shadowRoot?.getElementById('prop-y') as HTMLInputElement | null;
        if (propX) propX.value = String(newX);
        if (propY) propY.value = String(newY);
      }
    };

    const onMouseUp = () => {
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseup', onMouseUp);
      if (moved) {
        this.core.pushHistory('Move Element');
        this.dispatchChange();
      }
    };

    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
  }

  private updateSelectedProp(key: string, value: any): void {
    if (this.core.selectedElementIds.length === 0) return;
    const info = this.core.findElement(this.core.selectedElementIds[0]);
    if (!info) return;

    if (key === 'text') info.element.text = value;
    else if (key === 'expression') info.element.expression = value;
    else if (key === 'width') info.element.width = Number(value);
    else if (key === 'height') info.element.height = Number(value);
    else if (key === 'x') info.element.x = Number(value);
    else if (key === 'y') info.element.y = Number(value);

    // Update canvas DOM directly without destroying focused input element
    const elDom = this.shadowRoot?.querySelector(`.bpx-element-view[data-el-id="${info.element.id}"]`) as HTMLElement | null;
    if (elDom) {
      if (key === 'text' || key === 'expression') {
        const contentSpan = elDom.querySelector('.bpx-el-content');
        const displayContent = info.element.type === 'Chart' 
          ? `📊 [Chart: ${info.element.chart?.title || info.element.chart?.chartType || 'Column'}]` 
          : (info.element.type === 'Barcode' || info.element.type === 'QrCode'
            ? `▦ [${info.element.type}: ${info.element.text || '123456'}]`
            : (info.element.expression 
              ? `<span style="color: #2563eb; font-style: italic;">{ ${info.element.expression} }</span>`
              : (info.element.text || info.element.type || '')));
        if (contentSpan) {
          contentSpan.innerHTML = displayContent;
        } else {
          elDom.innerHTML = `<span class="bpx-el-content">${displayContent}</span>`;
        }
      } else if (key === 'x') {
        elDom.style.left = `${Number(value) || 0}px`;
      } else if (key === 'y') {
        elDom.style.top = `${Number(value) || 0}px`;
      } else if (key === 'width') {
        elDom.style.width = `${Number(value) || 0}px`;
      } else if (key === 'height') {
        elDom.style.height = `${Number(value) || 0}px`;
      }
    }

    if (this.core.bottomEditorTab === 'bpxJson') {
      const codePre = this.shadowRoot?.querySelector('.bpx-editor-body pre');
      if (codePre) codePre.textContent = this.core.getBpxJson();
    }

    this.dispatchChange();
  }

  public render(): void {
    if (!this.shadowRoot) return;

    const report = this.core.report || { pageSetup: { width: 595.28, height: 841.89, paperKind: 'A4', marginTop: 36, marginBottom: 36, marginLeft: 36, marginRight: 36 }, bands: {} };
    const selectedInfo = this.core.selectedElementIds.length === 1
      ? this.core.findElement(this.core.selectedElementIds[0])
      : null;

    const pageSetup = report.pageSetup || { width: 595.28, height: 841.89, paperKind: 'A4', marginTop: 36, marginBottom: 36, marginLeft: 36, marginRight: 36 };
    const marginTop = pageSetup.marginTop ?? 36;
    const marginBottom = pageSetup.marginBottom ?? 36;
    const marginLeft = pageSetup.marginLeft ?? 36;
    const marginRight = pageSetup.marginRight ?? 36;
    const bands = report.bands || {};

    const bandsHtml = Object.entries(bands).map(([bandName, band]: [string, any]) => {
      const elementsHtml = (band.elements || []).map((el: any) => {
        const isSelected = this.core.selectedElementIds.includes(el.id);
        const displayContent = el.type === 'Chart' 
          ? `📊 [Chart: ${el.chart?.title || el.chart?.chartType || 'Column'}]` 
          : (el.type === 'Barcode' || el.type === 'QrCode'
            ? `▦ [${el.type}: ${el.text || '123456'}]`
            : (el.expression 
              ? `<span style="color: #2563eb; font-style: italic;">{ ${el.expression} }</span>`
              : (el.text || el.type || '')));

        return `
          <div 
            class="bpx-element-view ${isSelected ? 'selected' : ''}"
            data-el-id="${el.id}"
            title="Double-click to edit text inline, or drag to move"
            style="left: ${el.x || 0}px; top: ${el.y || 0}px; width: ${el.width || 100}px; height: ${el.height || 24}px; font-size: ${el.style?.fontSize || 12}px; color: ${el.style?.color || '#0f172a'}; z-index: 2;"
          >
            <span class="bpx-el-content">${displayContent}</span>
          </div>
        `;
      }).join('');

      return `
        <div class="bpx-band-container" data-band="${bandName}" style="height: ${band.height || 60}px;">
          <div class="bpx-band-header">${bandName}</div>
          ${elementsHtml}
        </div>
      `;
    }).join('');

    const datasetsHtml = (report.datasets || []).map((ds: any) => `
      <div style="font-weight: 600; color: #38bdf8; margin-bottom: 4px;">📊 ${ds.name}</div>
      <div style="padding-left: 12px; margin-bottom: 10px;">
        ${(ds.fields || []).map((f: any) => `
          <div style="color: #94a3b8; padding: 2px 0;">🏷 ${f.name} <span style="font-size: 10px; color: #64748b;">(${f.type})</span></div>
        `).join('')}
      </div>
    `).join('');

    const propInspectorHtml = selectedInfo ? `
      <div class="bpx-prop-section">
        <div style="font-weight: 600; color: #38bdf8; margin-bottom: 8px;">Selected: ${selectedInfo.element.type}</div>
        <div class="bpx-prop-row">
          <label>X Position</label>
          <input class="bpx-input" type="number" id="prop-x" value="${selectedInfo.element.x || 0}" />
        </div>
        <div class="bpx-prop-row">
          <label>Y Position</label>
          <input class="bpx-input" type="number" id="prop-y" value="${selectedInfo.element.y || 0}" />
        </div>
        <div class="bpx-prop-row">
          <label>Width</label>
          <input class="bpx-input" type="number" id="prop-width" value="${selectedInfo.element.width || 0}" />
        </div>
        <div class="bpx-prop-row">
          <label>Height</label>
          <input class="bpx-input" type="number" id="prop-height" value="${selectedInfo.element.height || 0}" />
        </div>
        <div class="bpx-prop-row">
          <label>Static Text</label>
          <input class="bpx-input" type="text" id="prop-text" value="${(selectedInfo.element.text || '').replace(/"/g, '&quot;')}" />
        </div>
        <div class="bpx-prop-row">
          <label>C# Expression</label>
          <input class="bpx-input" type="text" id="prop-expression" value="${(selectedInfo.element.expression || '').replace(/"/g, '&quot;')}" />
        </div>
      </div>
    ` : `
      <div style="padding: 24px; text-align: center; color: #64748b; font-size: 13px;">
        Select an element on canvas to inspect and modify properties.
      </div>
    `;

    const bottomEditorContent = this.core.bottomEditorTab === 'bpxJson'
      ? `<pre style="margin: 0; color: #38bdf8;">${this.core.getBpxJson()}</pre>`
      : (this.core.bottomEditorTab === 'sql'
        ? `<pre style="margin: 0; color: #a7f3d0;">${report.datasets?.[0]?.query || 'No SQL Query'}</pre>`
        : `<div style="color: #cbd5e1;">💡 Type C# formulas: e.g. <span style="color: #38bdf8;">FormatCurrency(Fields.Amount * (1.0 - Fields.Discount))</span></div>`);

    this.shadowRoot.innerHTML = `
      <style>
        :host {
          display: flex;
          flex-direction: column;
          height: 100%;
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
          color: #e2e8f0;
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
      </style>

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
          <button class="bpx-btn ${this.core.viewMode === 'design' ? 'active' : ''}" id="btn-mode-design">Design</button>
          <button class="bpx-btn ${this.core.viewMode === 'preview' ? 'active' : ''}" id="btn-mode-preview">Live Preview</button>
          <button class="bpx-btn ${this.core.viewMode === 'code' ? 'active' : ''}" id="btn-mode-code">Schema Code</button>
        </div>

        <div class="bpx-toolbar-group">
          <button class="bpx-btn" id="btn-undo" title="Undo (Ctrl+Z)">↶ Undo</button>
          <button class="bpx-btn" id="btn-redo" title="Redo (Ctrl+Y)">↷ Redo</button>
          <div style="width: 1px; height: 20px; background: #334155; margin: 0 4px;"></div>
          <button class="bpx-btn" id="btn-zoom-out">−</button>
          <span style="font-size: 12px; font-weight: 600; min-width: 44px; text-align: center;">${this.core.zoomLevel}%</span>
          <button class="bpx-btn" id="btn-zoom-in">+</button>
          <button class="bpx-btn" id="btn-zoom-reset">100%</button>
        </div>

        <div class="bpx-toolbar-group">
          <button class="bpx-btn" id="btn-align-left" title="Align Left">⇤</button>
          <button class="bpx-btn" id="btn-align-center" title="Align Center">⇹</button>
          <button class="bpx-btn" id="btn-align-right" title="Align Right">⇥</button>
          <button class="bpx-btn" id="btn-delete" title="Delete Selected" style="color: #ef4444;">🗑</button>
        </div>
      </div>

      <!-- Main Workspace -->
      <div class="bpx-workspace">
        <!-- Left Toolbox & Data Explorer -->
        <div class="bpx-sidebar-left">
          <div class="bpx-panel-header">Components Toolbox</div>
          <div class="bpx-toolbox-items">
            <div class="bpx-tool-card" draggable="true" data-tool="Text"><span>📝</span><span>Text</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Expression"><span>⚡</span><span>Expression</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Table"><span>📑</span><span>Table</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Chart"><span>📊</span><span>Chart</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Barcode"><span>▦</span><span>Barcode</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="QrCode"><span>🔲</span><span>QrCode</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Image"><span>🖼</span><span>Image</span></div>
            <div class="bpx-tool-card" draggable="true" data-tool="Shape"><span>⬛</span><span>Shape</span></div>
          </div>

          <div class="bpx-panel-header" style="margin-top: 10px;">Datasets & Fields</div>
          <div style="padding: 10px 14px; font-size: 12px; overflow-y: auto; flex: 1;">
            ${datasetsHtml || '<div style="color: #64748b;">No active datasets</div>'}
          </div>
        </div>

        <!-- Center Canvas -->
        <div class="bpx-canvas-container" id="canvas-container">
          <div class="bpx-page-canvas" style="width: ${pageSetup.width}px; min-height: ${pageSetup.height}px; transform: scale(${this.core.zoomLevel / 100}); transform-origin: top center; position: relative;">
            <div class="bpx-canvas-page-header" style="position: absolute; top: -24px; left: 0; font-size: 11px; color: #94a3b8; display: flex; align-items: center; gap: 8px; white-space: nowrap;">
              <span style="background: #1e293b; padding: 2px 8px; border-radius: 4px; border: 1px solid #334155; color: #38bdf8; font-weight: 600;">
                📄 ${pageSetup.paperKind || (Math.abs(pageSetup.width - 595.28) < 1 ? 'A4' : 'Custom')} (${Math.round(pageSetup.width)} × ${Math.round(pageSetup.height)} pt)
              </span>
              <span style="color: #64748b;">Margins: ${marginTop}pt Top/Bottom, ${marginLeft}pt Left/Right</span>
            </div>
            <!-- Margin Guideline Overlay -->
            <div class="bpx-margin-guide" style="position: absolute; top: ${marginTop}px; left: ${marginLeft}px; right: ${marginRight}px; bottom: ${marginBottom}px; border: 1px dashed #cbd5e1; pointer-events: none; z-index: 1;"></div>
            ${bandsHtml}
          </div>
        </div>

        <!-- Right Property Inspector -->
        <div class="bpx-sidebar-right">
          <div class="bpx-panel-header">Property Inspector</div>
          ${propInspectorHtml}
        </div>
      </div>

      <!-- Bottom Monaco Editor / Code Area -->
      <div class="bpx-bottom-editor">
        <div class="bpx-editor-tabs">
          <div class="bpx-tab ${this.core.bottomEditorTab === 'expression' ? 'active' : ''}" data-tab="expression">Expression Assist</div>
          <div class="bpx-tab ${this.core.bottomEditorTab === 'sql' ? 'active' : ''}" data-tab="sql">SQL Query Console</div>
          <div class="bpx-tab ${this.core.bottomEditorTab === 'bpxJson' ? 'active' : ''}" data-tab="bpxJson">.bpx JSON Schema</div>
        </div>
        <div class="bpx-editor-body">
          ${bottomEditorContent}
        </div>
      </div>
    `;

    this.attachEventListeners();
  }

  private attachEventListeners(): void {
    const root = this.shadowRoot;
    if (!root) return;

    // View modes
    root.getElementById('btn-mode-design')?.addEventListener('click', () => { this.core.setViewMode('design'); this.requestUpdate(); });
    root.getElementById('btn-mode-preview')?.addEventListener('click', () => { this.core.setViewMode('preview'); this.requestUpdate(); });
    root.getElementById('btn-mode-code')?.addEventListener('click', () => { this.core.setViewMode('code'); this.requestUpdate(); });

    // Undo / Redo / Zoom
    root.getElementById('btn-undo')?.addEventListener('click', () => { this.core.undo(); this.dispatchChange(); this.requestUpdate(); });
    root.getElementById('btn-redo')?.addEventListener('click', () => { this.core.redo(); this.dispatchChange(); this.requestUpdate(); });
    root.getElementById('btn-zoom-out')?.addEventListener('click', () => { this.core.zoomOut(); this.requestUpdate(); });
    root.getElementById('btn-zoom-in')?.addEventListener('click', () => { this.core.zoomIn(); this.requestUpdate(); });
    root.getElementById('btn-zoom-reset')?.addEventListener('click', () => { this.core.resetZoom(); this.requestUpdate(); });

    // Align / Delete
    root.getElementById('btn-align-left')?.addEventListener('click', () => { this.core.alignSelectedElements('left'); this.dispatchChange(); this.requestUpdate(); });
    root.getElementById('btn-align-center')?.addEventListener('click', () => { this.core.alignSelectedElements('center'); this.dispatchChange(); this.requestUpdate(); });
    root.getElementById('btn-align-right')?.addEventListener('click', () => { this.core.alignSelectedElements('right'); this.dispatchChange(); this.requestUpdate(); });
    root.getElementById('btn-delete')?.addEventListener('click', () => { this.core.removeSelectedElements(); this.dispatchChange(); this.requestUpdate(); });

    // Deselect click
    root.getElementById('canvas-container')?.addEventListener('click', (e: any) => {
      if (e.target.id === 'canvas-container' || e.target.classList?.contains('bpx-page-canvas')) {
        this.core.selectElement(null);
        this.requestUpdate();
      }
    });

    // Tool drag start
    root.querySelectorAll('.bpx-tool-card').forEach((card: any) => {
      card.addEventListener('dragstart', (e: DragEvent) => {
        this.handleToolDragStart(e, card.dataset.tool || 'Text');
      });
    });

    // Band dragover / drop
    root.querySelectorAll('.bpx-band-container').forEach((bandEl: any) => {
      bandEl.addEventListener('dragover', (e: DragEvent) => e.preventDefault());
      bandEl.addEventListener('drop', (e: DragEvent) => {
        this.handleCanvasDrop(e, bandEl.dataset.band || 'Detail');
      });
    });

    // Element selection & interactions
    root.querySelectorAll('.bpx-element-view').forEach((el: any) => {
      el.addEventListener('click', (e: MouseEvent) => {
        this.handleElementClick(e, el.dataset.elId);
      });
      el.addEventListener('dblclick', (e: MouseEvent) => {
        this.handleElementDblClick(e, el.dataset.elId);
      });
      el.addEventListener('mousedown', (e: MouseEvent) => {
        this.handleElementMouseDown(e, el.dataset.elId);
      });
    });

    // Property inputs
    ['x', 'y', 'width', 'height', 'text', 'expression'].forEach(key => {
      const input = root.getElementById(`prop-${key}`) as HTMLInputElement | null;
      input?.addEventListener('input', (e: any) => {
        this.updateSelectedProp(key, e.target.value);
      });
      input?.addEventListener('change', () => {
        this.core.pushHistory(`Update ${key}`);
      });
    });

    // Bottom editor tabs
    root.querySelectorAll('.bpx-tab').forEach((tab: any) => {
      tab.addEventListener('click', () => {
        this.core.setBottomEditorTab(tab.dataset.tab);
        this.requestUpdate();
      });
    });
  }
}

if (typeof customElements !== 'undefined' && !customElements.get('bangplanix-designer')) {
  customElements.define('bangplanix-designer', BangplanixDesigner);
}
