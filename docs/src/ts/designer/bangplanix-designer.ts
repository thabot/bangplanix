/**
 * Visual Designer Web Component (<bangplanix-designer>) for Docs
 */
// @ts-ignore
import { BangplanixDesignerCore } from './designer-core.js';

const BaseElement: { new(): any } = typeof HTMLElement !== 'undefined' ? HTMLElement : class {
  attachShadow() { return {}; }
  dispatchEvent() { return true; }
};

export class BangplanixDesigner extends BaseElement {
  public core: any;
  public serverUrl: string = 'http://localhost:9545';
  public readOnly: boolean = false;
  public initialReport: any = null;
  public activeBandName: string = 'Detail';
  public isFullscreen: boolean = false;
  public mouseX: number = 0;
  public mouseY: number = 0;

  constructor() {
    super();
    try {
      if (typeof this.attachShadow === 'function') {
        this.attachShadow({ mode: 'open' });
      }
    } catch {}
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

  public resolveBandFromY(y: number): { bandName: string; offsetY: number } {
    const bands = this.core.report?.bands || {};
    const bandEntries = Object.entries(bands);
    if (bandEntries.length === 0) return { bandName: 'Detail', offsetY: Math.max(0, Math.round(y)) };
    let accumulatedHeight = 0;
    for (const [name, band] of bandEntries) {
      const h = (band as any).height || 40;
      if (y >= accumulatedHeight && y < accumulatedHeight + h) {
        return { bandName: name, offsetY: Math.max(0, Math.round(y - accumulatedHeight)) };
      }
      accumulatedHeight += h;
    }
    const lastBand = bandEntries[bandEntries.length - 1];
    return { bandName: lastBand[0], offsetY: Math.max(0, Math.round(y - accumulatedHeight + ((lastBand[1] as any).height || 40))) };
  }

  public addComponentToBand(type: string, bandName: string = this.activeBandName || 'Detail', customProps: any = {}): any {
    if (!this.core.report?.bands?.[bandName]) {
      bandName = 'Detail';
    }
    const band = this.core.report?.bands?.[bandName];
    const existingCount = band?.elements?.length || 0;
    const defaultX = 20;
    const defaultY = Math.min(Math.max(5, (band?.height || 60) - 30), Math.max(5, existingCount * 25));

    const el = this.core.addElement(bandName, {
      type,
      x: customProps.x !== undefined ? customProps.x : defaultX,
      y: customProps.y !== undefined ? customProps.y : defaultY,
      width: customProps.width || (type === 'Chart' ? 300 : (type === 'Barcode' ? 180 : (type === 'Table' ? 400 : 120))),
      height: customProps.height || (type === 'Chart' ? 180 : (type === 'Barcode' ? 60 : (type === 'Table' ? 120 : 25))),
      text: customProps.text !== undefined ? customProps.text : (type === 'Text' ? 'Label Text' : (type === 'Barcode' ? '123456789' : '')),
      expression: customProps.expression || '',
      style: {
        fontSize: 12,
        fontFamily: 'inherit',
        color: '#0f172a',
        ...customProps.style
      }
    });

    if (el?.id) {
      this.core.selectElement(el.id);
    }
    this.core.pushHistory(`Add ${type} to ${bandName}`);
    this.dispatchChange();
    this.requestUpdate();
    return el;
  }

  public printDocument(): void {
    window.print();
  }

  public toggleFullscreen(): void {
    if (!document.fullscreenElement) {
      this.requestFullscreen?.().catch?.(() => {
        document.documentElement.requestFullscreen?.().catch?.(() => {});
      });
      this.isFullscreen = true;
    } else {
      document.exitFullscreen?.().catch?.(() => {});
      this.isFullscreen = false;
    }
    this.requestUpdate();
  }

  private handleToolDragStart(e: DragEvent, type: string): void {
    if (e.dataTransfer) {
      e.dataTransfer.setData('text/plain', type);
      e.dataTransfer.setData('bpx-tool', type);
      e.dataTransfer.effectAllowed = 'copy';
    }
  }

  private handleFieldDragStart(e: DragEvent, fieldName: string): void {
    if (e.dataTransfer) {
      e.dataTransfer.setData('text/plain', `{Fields.${fieldName}}`);
      e.dataTransfer.setData('bpx-field', fieldName);
      e.dataTransfer.effectAllowed = 'copy';
    }
  }

  private handleCanvasDrop(e: DragEvent, targetBandName?: string): void {
    e.preventDefault();
    if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';

    const canvasEl = this.shadowRoot?.querySelector('.bpx-page-canvas') as HTMLElement | null;
    const rect = (canvasEl || e.currentTarget as HTMLElement).getBoundingClientRect();
    const zoom = (this.core.zoomLevel || 100) / 100;
    const rawX = (e.clientX - rect.left) / zoom;
    const rawY = (e.clientY - rect.top) / zoom;

    const fieldName = e.dataTransfer?.getData('bpx-field');
    const rawType = e.dataTransfer?.getData('bpx-tool') || e.dataTransfer?.getData('text/plain') || 'Text';

    let bandName = targetBandName;
    let offsetY = rawY;
    if (!bandName) {
      const resolved = this.resolveBandFromY(rawY);
      bandName = resolved.bandName;
      offsetY = resolved.offsetY;
    }

    if (fieldName) {
      this.core.addElement(bandName, {
        type: 'Expression',
        x: Math.max(0, Math.round(rawX)),
        y: Math.max(0, Math.round(offsetY)),
        width: 140,
        height: 25,
        expression: `Fields.${fieldName}`,
        text: '',
        style: { fontSize: 11, color: '#2563eb' }
      });
    } else {
      const isExpression = rawType.startsWith('{') || rawType === 'Expression';
      const type = isExpression ? 'Expression' : rawType;
      this.core.addElement(bandName, {
        type,
        x: Math.max(0, Math.round(rawX)),
        y: Math.max(0, Math.round(offsetY)),
        width: type === 'Chart' ? 300 : (type === 'Barcode' ? 180 : (type === 'Table' ? 400 : 120)),
        height: type === 'Chart' ? 180 : (type === 'Barcode' ? 60 : (type === 'Table' ? 120 : 25)),
        text: type === 'Text' ? 'Label Text' : (type === 'Barcode' ? '123456789' : ''),
        expression: isExpression ? (rawType.startsWith('{') ? rawType.replace(/[{}]/g, '').trim() : 'Fields.Amount') : ''
      });
    }
    this.core.pushHistory(`Drop on ${bandName}`);
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

    if (!info.element.style) info.element.style = {};

    if (key === 'text') info.element.text = value;
    else if (key === 'expression') info.element.expression = value;
    else if (key === 'width') info.element.width = Number(value);
    else if (key === 'height') info.element.height = Number(value);
    else if (key === 'x') info.element.x = Number(value);
    else if (key === 'y') info.element.y = Number(value);
    else if (key === 'fontSize') info.element.style.fontSize = Number(value) || 12;
    else if (key === 'fontFamily') info.element.style.fontFamily = value;
    else if (key === 'fontWeight') info.element.style.fontWeight = value;
    else if (key === 'fontStyle') info.element.style.fontStyle = value;
    else if (key === 'color') info.element.style.color = value;
    else if (key === 'alignment') info.element.style.alignment = value;
    else if (key === 'backgroundColor') info.element.style.backgroundColor = value;
    else if (key === 'borderWidth') info.element.style.borderWidth = Number(value) || 0;
    else if (key === 'borderStyle') info.element.style.borderStyle = value;
    else if (key === 'borderColor') info.element.style.borderColor = value;
    else if (key === 'borderRadius') info.element.style.borderRadius = Number(value) || 0;

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
      } else if (key === 'fontSize') {
        elDom.style.fontSize = `${Number(value) || 12}px`;
      } else if (key === 'fontFamily') {
        elDom.style.fontFamily = value;
      } else if (key === 'fontWeight') {
        elDom.style.fontWeight = value;
      } else if (key === 'fontStyle') {
        elDom.style.fontStyle = value;
      } else if (key === 'color') {
        elDom.style.color = value;
      } else if (key === 'alignment') {
        elDom.style.justifyContent = String(value).toLowerCase() === 'right' ? 'flex-end' : (String(value).toLowerCase() === 'center' ? 'center' : 'flex-start');
        elDom.style.textAlign = String(value).toLowerCase();
      } else if (key === 'backgroundColor') {
        elDom.style.backgroundColor = value;
      } else if (key === 'borderWidth') {
        elDom.style.borderWidth = `${Number(value) || 0}px`;
      } else if (key === 'borderStyle') {
        elDom.style.borderStyle = value;
      } else if (key === 'borderColor') {
        elDom.style.borderColor = value;
      } else if (key === 'borderRadius') {
        elDom.style.borderRadius = `${Number(value) || 0}px`;
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

    const isPreview = this.core.viewMode === 'preview';

    const bandsHtml = Object.entries(bands).map(([bandName, band]: [string, any]) => {
      const isActiveBand = this.activeBandName === bandName;
      const elementsHtml = (band.elements || []).map((el: any) => {
        const isSelected = this.core.selectedElementIds.includes(el.id);
        const style = el.style || {};
        const fontSize = style.fontSize || 12;
        const fontFamily = style.fontFamily || 'inherit';
        const fontWeight = style.fontWeight || 'normal';
        const fontStyle = style.fontStyle || 'normal';
        const color = style.color || '#0f172a';
        const align = (style.alignment || 'Left').toLowerCase();
        const justify = align === 'right' ? 'flex-end' : (align === 'center' ? 'center' : 'flex-start');

        const backgroundColor = style.backgroundColor || 'transparent';
        const borderWidth = style.borderWidth !== undefined ? `${style.borderWidth}px` : (isPreview ? '0px' : (isSelected ? '1.5px' : '1px'));
        const borderStyle = style.borderStyle || (isPreview ? 'none' : (isSelected ? 'solid' : 'dashed'));
        const borderColor = style.borderColor || (isPreview ? 'transparent' : (isSelected ? '#2563eb' : 'transparent'));
        const borderRadius = style.borderRadius ? `${style.borderRadius}px` : '0px';
        const outline = (!isPreview && isSelected) ? '2px solid #2563eb' : 'none';

        const displayContent = el.type === 'Chart' 
          ? `📊 [Chart: ${el.chart?.title || el.chart?.chartType || 'Column'}]` 
          : (el.type === 'Barcode' || el.type === 'QrCode'
            ? `▦ [${el.type}: ${el.text || '123456'}]`
            : (el.expression 
              ? (isPreview ? `INV-2026-001` : `<span style="color: #2563eb; font-style: italic;">{ ${el.expression} }</span>`)
              : (el.text || el.type || '')));

        return `
          <div 
            class="bpx-element-view ${isSelected ? 'selected' : ''}"
            data-el-id="${el.id}"
            title="Double-click to edit text inline, or drag to move"
            style="left: ${el.x || 0}px; top: ${el.y || 0}px; width: ${el.width || 100}px; height: ${el.height || 24}px; font-size: ${fontSize}px; font-family: ${fontFamily}; font-weight: ${fontWeight}; font-style: ${fontStyle}; color: ${color}; text-align: ${align}; justify-content: ${justify}; background-color: ${backgroundColor}; border-width: ${borderWidth}; border-style: ${borderStyle}; border-color: ${borderColor}; border-radius: ${borderRadius}; outline: ${outline}; outline-offset: 1px; z-index: 2;"
          >
            <span class="bpx-el-content" style="width: 100%; text-align: inherit;">${displayContent}</span>
          </div>
        `;
      }).join('');

      return `
        <div class="bpx-band-container ${isActiveBand ? 'active-band' : ''}" data-band="${bandName}" style="height: ${band.height || 60}px; position: relative;">
          ${!isPreview ? `<div class="bpx-band-header ${isActiveBand ? 'active' : ''}" data-band="${bandName}" title="Active Band for new components">${bandName} ${isActiveBand ? '★' : ''}</div>` : ''}
          ${elementsHtml}
        </div>
      `;
    }).join('');

    const totalBandsHeight = Object.values(bands).reduce((sum: number, b: any) => sum + (b.height || 0), 0);
    const pageCount = Math.max(1, Math.ceil(totalBandsHeight / pageSetup.height));

    let pageSheetsHtml = '';
    for (let p = 0; p < pageCount; p++) {
      const topY = p * pageSetup.height;
      pageSheetsHtml += `
        <div class="bpx-page-sheet" data-sheet-index="${p + 1}" style="position: absolute; top: ${topY}px; left: 0; width: ${pageSetup.width}px; height: ${pageSetup.height}px; pointer-events: none; border-bottom: ${p < pageCount - 1 && !isPreview ? '2px dashed #2563eb' : 'none'}; box-sizing: border-box;">
          ${!isPreview ? `
            <div class="bpx-page-sheet-header" style="position: absolute; top: -22px; left: 0; font-size: 10px; font-weight: 700; color: #38bdf8; display: flex; align-items: center; gap: 8px; white-space: nowrap; pointer-events: auto;">
              <span style="background: #1e293b; padding: 2px 8px; border-radius: 4px; border: 1px solid #334155;">
                📄 Sheet ${p + 1} of ${pageCount} (${pageSetup.paperKind || (Math.abs(pageSetup.width - 595.28) < 1 ? 'A4' : 'Custom')} ${Math.round(pageSetup.width)} × ${Math.round(pageSetup.height)} pt)
              </span>
              <span style="color: #64748b; font-weight: 500;">Margins: ${marginTop}pt Top/Bottom, ${marginLeft}pt Left/Right</span>
            </div>
            <div class="bpx-margin-guide" style="position: absolute; top: ${marginTop}px; left: ${marginLeft}px; right: ${marginRight}px; bottom: ${marginBottom}px; border: 1px dashed #cbd5e1; pointer-events: none;"></div>
          ` : ''}
          ${p > 0 && !isPreview ? `
            <div class="bpx-page-break-badge" style="position: absolute; top: -11px; right: 12px; z-index: 10; pointer-events: none;">
              <span style="background: #2563eb; color: #ffffff; font-size: 10px; font-weight: 700; padding: 2px 8px; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.2);">
                ✂ Page Break • Sheet ${p + 1} of ${pageCount}
              </span>
            </div>
          ` : ''}
        </div>
      `;
    }

    const toolboxItems = [
      { type: 'Text', icon: '📝', label: 'Text' },
      { type: 'Expression', icon: '⚡', label: 'Expression' },
      { type: 'Table', icon: '📑', label: 'Table' },
      { type: 'Chart', icon: '📊', label: 'Chart' },
      { type: 'Barcode', icon: '▦', label: 'Barcode' },
      { type: 'QrCode', icon: '🔲', label: 'QrCode' },
      { type: 'Image', icon: '🖼', label: 'Image' },
      { type: 'Shape', icon: '⬛', label: 'Shape' }
    ];

    const toolboxHtml = `
      <div class="bpx-toolbox-list">
        ${toolboxItems.map(item => `
          <div class="bpx-tool-item" draggable="true" data-tool="${item.type}" title="Drag to paper or click [＋] to insert into ${this.activeBandName || 'Detail'}">
            <span class="bpx-tool-icon">${item.icon}</span>
            <span class="bpx-tool-label">${item.label}</span>
            <button type="button" class="bpx-tool-add-btn" data-tool="${item.type}" title="Insert into ${this.activeBandName || 'Detail'}">＋</button>
          </div>
        `).join('')}
      </div>
    `;

    const datasetsHtml = (report.datasets || []).map((ds: any) => `
      <div style="font-weight: 600; color: #38bdf8; margin-bottom: 6px; font-size: 11px;">📊 ${ds.name}</div>
      <div style="display: flex; flex-direction: column; gap: 2px; margin-bottom: 12px;">
        ${(ds.fields || []).map((f: any) => `
          <div class="bpx-field-item" draggable="true" data-field="${f.name}" title="Drag to paper or click [＋] to insert expression {Fields.${f.name}}">
            <span style="color: #cbd5e1; font-size: 11px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">🏷 ${f.name} <span style="font-size: 9.5px; color: #64748b;">(${f.type})</span></span>
            <button type="button" class="bpx-field-add-btn" data-field="${f.name}" title="Insert {Fields.${f.name}}">＋</button>
          </div>
        `).join('')}
      </div>
    `).join('');

    const currentStyle = selectedInfo?.element?.style || {};
    const standardSwatches = ['#0f172a', '#475569', '#2563eb', '#0284c7', '#059669', '#ef4444', '#ea580c', '#7c3aed'];
    const swatchesHtml = standardSwatches.map(color => `
      <button
        type="button"
        class="bpx-color-swatch"
        data-color="${color}"
        title="${color}"
        style="background-color: ${color}; width: 14px; height: 14px; border-radius: 3px; border: 1px solid rgba(255,255,255,0.25); cursor: pointer; padding: 0;"
      ></button>
    `).join('');

    const normalizeHex = (c: string | undefined): string => {
      if (!c) return '#0f172a';
      if (/^#[0-9A-Fa-f]{6}$/.test(c)) return c;
      if (/^#[0-9A-Fa-f]{3}$/.test(c)) return '#' + c[1] + c[1] + c[2] + c[2] + c[3] + c[3];
      return '#0f172a';
    };
    const currentHexColor = normalizeHex(currentStyle.color);
    const currentBgHex = normalizeHex(currentStyle.backgroundColor || '#ffffff');
    const currentBorderHex = normalizeHex(currentStyle.borderColor || '#000000');

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

        <div style="font-weight: 600; color: #38bdf8; margin: 12px 0 6px 0; border-top: 1px solid #334155; padding-top: 8px;">Typography & Text Color</div>
        <div class="bpx-prop-row">
          <label>Font Family</label>
          <select class="bpx-input" id="prop-fontFamily">
            <option value="Sarabun, sans-serif" ${(currentStyle.fontFamily || '').includes('Sarabun') ? 'selected' : ''}>Sarabun (TH)</option>
            <option value="-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif" ${(currentStyle.fontFamily || '').includes('apple-system') ? 'selected' : ''}>System Sans</option>
            <option value="Arial, Helvetica, sans-serif" ${(currentStyle.fontFamily || '').includes('Arial') ? 'selected' : ''}>Arial</option>
            <option value="Tahoma, sans-serif" ${(currentStyle.fontFamily || '').includes('Tahoma') ? 'selected' : ''}>Tahoma</option>
            <option value="'Times New Roman', Times, serif" ${(currentStyle.fontFamily || '').includes('Times') ? 'selected' : ''}>Times New Roman</option>
            <option value="'Courier New', Courier, monospace" ${(currentStyle.fontFamily || '').includes('Courier') ? 'selected' : ''}>Courier New</option>
          </select>
        </div>
        <div class="bpx-prop-row">
          <label>Font Size (pt)</label>
          <input class="bpx-input" type="number" id="prop-fontSize" min="6" max="72" value="${currentStyle.fontSize || 12}" />
        </div>
        <div class="bpx-prop-row">
          <label>Font Weight</label>
          <select class="bpx-input" id="prop-fontWeight">
            <option value="normal" ${(currentStyle.fontWeight || 'normal') === 'normal' ? 'selected' : ''}>Regular</option>
            <option value="bold" ${(currentStyle.fontWeight || '') === 'bold' || (currentStyle.fontWeight || '') === 'Bold' ? 'selected' : ''}>Bold</option>
            <option value="600" ${(currentStyle.fontWeight || '') === '600' ? 'selected' : ''}>Semi-Bold</option>
          </select>
        </div>
        <div class="bpx-prop-row">
          <label>Font Style</label>
          <select class="bpx-input" id="prop-fontStyle">
            <option value="normal" ${(currentStyle.fontStyle || 'normal') === 'normal' ? 'selected' : ''}>Normal</option>
            <option value="italic" ${(currentStyle.fontStyle || '') === 'italic' ? 'selected' : ''}>Italic</option>
          </select>
        </div>
        <div class="bpx-prop-row" style="flex-direction: column; align-items: stretch; gap: 6px;">
          <div style="display: flex; justify-content: space-between; align-items: center;">
            <label>Text Color</label>
            <div style="display: flex; gap: 4px; align-items: center;">
              <input type="color" id="prop-color-picker" value="${currentHexColor}" title="Color Picker (วงล้อสี/จานสี)" style="width: 26px; height: 26px; padding: 0; border: 1px solid #475569; border-radius: 4px; cursor: pointer; background: none;" />
              <input class="bpx-input" type="text" id="prop-color" value="${currentStyle.color || '#0f172a'}" style="width: 72px; padding: 3px 6px; font-family: monospace; font-size: 11px;" placeholder="#000000" />
              <button type="button" class="bpx-btn" id="prop-color-eyedropper" title="EyeDropper (ดูดสีจากหน้าจอ)" style="padding: 3px 6px; font-size: 12px;">💉</button>
            </div>
          </div>
          <div class="bpx-color-swatches" style="display: flex; gap: 4px; justify-content: flex-end; align-items: center;">
            <span style="font-size: 10px; color: #64748b; margin-right: 2px;">Quick:</span>
            ${swatchesHtml}
          </div>
        </div>
        <div class="bpx-prop-row">
          <label>Alignment</label>
          <select class="bpx-input" id="prop-alignment">
            <option value="Left" ${(currentStyle.alignment || 'Left').toLowerCase() === 'left' ? 'selected' : ''}>Left</option>
            <option value="Center" ${(currentStyle.alignment || '').toLowerCase() === 'center' ? 'selected' : ''}>Center</option>
            <option value="Right" ${(currentStyle.alignment || '').toLowerCase() === 'right' ? 'selected' : ''}>Right</option>
          </select>
        </div>

        <div style="font-weight: 600; color: #38bdf8; margin: 12px 0 6px 0; border-top: 1px solid #334155; padding-top: 8px;">Border & Background Fill</div>
        <div class="bpx-prop-row">
          <label>Background Fill</label>
          <div style="display: flex; gap: 4px; align-items: center;">
            <input type="color" id="prop-bg-picker" value="${currentBgHex}" style="width: 26px; height: 26px; padding: 0; border: 1px solid #475569; border-radius: 4px; cursor: pointer; background: none;" />
            <input class="bpx-input" type="text" id="prop-backgroundColor" value="${currentStyle.backgroundColor || ''}" style="width: 72px; padding: 3px 6px; font-family: monospace; font-size: 11px;" placeholder="transparent" />
          </div>
        </div>
        <div class="bpx-prop-row">
          <label>Border Width (px)</label>
          <input class="bpx-input" type="number" id="prop-borderWidth" min="0" max="20" value="${currentStyle.borderWidth || 0}" />
        </div>
        <div class="bpx-prop-row">
          <label>Border Style</label>
          <select class="bpx-input" id="prop-borderStyle">
            <option value="none" ${(currentStyle.borderStyle || 'none') === 'none' ? 'selected' : ''}>None</option>
            <option value="solid" ${(currentStyle.borderStyle || '') === 'solid' ? 'selected' : ''}>Solid</option>
            <option value="dashed" ${(currentStyle.borderStyle || '') === 'dashed' ? 'selected' : ''}>Dashed</option>
            <option value="dotted" ${(currentStyle.borderStyle || '') === 'dotted' ? 'selected' : ''}>Dotted</option>
            <option value="double" ${(currentStyle.borderStyle || '') === 'double' ? 'selected' : ''}>Double</option>
          </select>
        </div>
        <div class="bpx-prop-row">
          <label>Border Color</label>
          <div style="display: flex; gap: 4px; align-items: center;">
            <input type="color" id="prop-border-color-picker" value="${currentBorderHex}" style="width: 26px; height: 26px; padding: 0; border: 1px solid #475569; border-radius: 4px; cursor: pointer; background: none;" />
            <input class="bpx-input" type="text" id="prop-borderColor" value="${currentStyle.borderColor || '#000000'}" style="width: 72px; padding: 3px 6px; font-family: monospace; font-size: 11px;" placeholder="#000000" />
          </div>
        </div>
        <div class="bpx-prop-row">
          <label>Border Radius (px)</label>
          <input class="bpx-input" type="number" id="prop-borderRadius" min="0" max="50" value="${currentStyle.borderRadius || 0}" />
        </div>

        <button type="button" id="prop-delete-btn" class="bpx-btn" style="width: 100%; margin-top: 14px; justify-content: center; background: #dc2626; border-color: #ef4444; color: white; padding: 6px 12px; font-weight: 600;">🗑 Delete Element</button>
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
          height: 40px;
          background-color: #1e293b;
          border-bottom: 1px solid #334155;
          padding: 0 10px;
          gap: 6px;
          user-select: none;
        }

        .bpx-toolbar-group {
          display: flex;
          align-items: center;
          gap: 4px;
        }

        .bpx-btn {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          padding: 4px 8px;
          background-color: #334155;
          color: #e2e8f0;
          border: 1px solid #475569;
          border-radius: 4px;
          font-size: 11px;
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
          font-size: 14px;
          letter-spacing: -0.5px;
          color: #38bdf8;
          display: flex;
          align-items: center;
          gap: 6px;
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
          width: 165px;
          background-color: #1e293b;
          border-right: 1px solid #334155;
          display: flex;
          flex-direction: column;
          overflow: hidden;
        }

        .bpx-panel-header {
          padding: 6px 10px;
          font-size: 11px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.5px;
          color: #94a3b8;
          border-bottom: 1px solid #334155;
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .bpx-toolbox-list {
          display: flex;
          flex-direction: column;
          gap: 3px;
          padding: 6px;
          overflow-y: auto;
        }

        .bpx-tool-item {
          background-color: #0f172a;
          border: 1px solid #334155;
          border-radius: 4px;
          height: 25px;
          padding: 0 6px;
          display: flex;
          align-items: center;
          justify-content: space-between;
          cursor: grab;
          font-size: 11px;
          color: #e2e8f0;
          transition: all 0.15s ease;
          user-select: none;
        }

        .bpx-tool-item:hover {
          border-color: #38bdf8;
          background-color: #1e293b;
          transform: translateX(1px);
        }

        .bpx-tool-icon {
          font-size: 13px;
          line-height: 1;
          margin-right: 6px;
        }

        .bpx-tool-label {
          flex: 1;
          font-weight: 500;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .bpx-tool-add-btn {
          background: transparent;
          border: none;
          color: #64748b;
          cursor: pointer;
          font-size: 13px;
          padding: 0 4px;
          line-height: 1;
          border-radius: 3px;
        }

        .bpx-tool-add-btn:hover {
          color: #38bdf8;
          background-color: rgba(56, 189, 248, 0.15);
        }

        .bpx-field-item {
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 3px 6px;
          background-color: #0f172a;
          border: 1px solid #334155;
          border-radius: 4px;
          cursor: grab;
          transition: all 0.15s ease;
          user-select: none;
        }

        .bpx-field-item:hover {
          border-color: #38bdf8;
          background-color: #1e293b;
        }

        .bpx-field-add-btn {
          background: transparent;
          border: none;
          color: #64748b;
          cursor: pointer;
          font-size: 13px;
          padding: 0 4px;
          line-height: 1;
          border-radius: 3px;
        }

        .bpx-field-add-btn:hover {
          color: #38bdf8;
          background-color: rgba(56, 189, 248, 0.15);
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

        .bpx-band-container.active-band {
          background-color: rgba(37, 99, 235, 0.02);
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
          cursor: pointer;
          pointer-events: auto;
          transition: all 0.15s ease;
        }

        .bpx-band-header:hover, .bpx-band-header.active {
          color: #38bdf8;
          background-color: rgba(56, 189, 248, 0.1);
          border-radius: 3px;
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

        /* Live Preview Mode Overrides */
        .preview-mode .bpx-band-header { display: none !important; }
        .preview-mode .bpx-margin-guide { display: none !important; }
        .preview-mode .bpx-band-container { border-bottom: none !important; }
        .preview-mode .bpx-page-sheet { border-bottom: none !important; }
        .preview-mode .bpx-page-sheet-header { display: none !important; }
        .preview-mode .bpx-page-break-badge { display: none !important; }

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

        .bpx-color-swatch:hover {
          transform: scale(1.2);
        }

        /* Bottom Editor Area */
        .bpx-bottom-editor {
          height: 150px;
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
          padding: 4px 10px;
          font-size: 11px;
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
          <div style="width: 1px; height: 20px; background: #334155; margin: 0 4px;"></div>
          <button class="bpx-btn" id="btn-quick-print" title="Quick Print (Ctrl+P)">🖨️ Print</button>
          <button class="bpx-btn" id="btn-fullscreen" title="Toggle Fullscreen">⛶ Fullscreen</button>
          <button class="bpx-btn" id="btn-delete" title="Delete Selected" style="color: #ef4444;">🗑</button>
        </div>
      </div>

      <!-- Main Workspace -->
      <div class="bpx-workspace">
        <!-- Left Toolbox & Data Explorer -->
        <div class="bpx-sidebar-left">
          <div class="bpx-panel-header">Components</div>
          ${toolboxHtml}

          <div class="bpx-panel-header" style="margin-top: 6px;">Datasets & Fields</div>
          <div style="padding: 6px 8px; font-size: 12px; overflow-y: auto; flex: 1;">
            ${datasetsHtml || '<div style="color: #64748b; font-size: 11px;">No active datasets</div>'}
          </div>
        </div>

        <!-- Center Canvas -->
        <div class="bpx-canvas-container ${isPreview ? 'preview-mode' : ''}" id="canvas-container">
          ${isPreview ? `
            <div style="position: absolute; top: 12px; right: 12px; background: rgba(16, 185, 129, 0.9); color: white; padding: 4px 10px; border-radius: 6px; font-size: 11px; font-weight: 600; display: flex; align-items: center; gap: 6px; z-index: 50; box-shadow: 0 4px 12px rgba(0,0,0,0.3); pointer-events: none;">
              👁️ Live Print Preview
            </div>
          ` : ''}
          <div class="bpx-page-canvas" style="width: ${pageSetup.width}px; min-height: ${Math.max(pageSetup.height, pageCount * pageSetup.height)}px; transform: scale(${this.core.zoomLevel / 100}); transform-origin: top center; position: relative;">
            ${pageSheetsHtml}
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
    root.getElementById('btn-mode-design')?.addEventListener('click', () => {
      this.core.setViewMode('design');
      this.dispatchEvent(new CustomEvent('view-mode-change', { detail: { mode: 'design' }, bubbles: true, composed: true }));
      this.requestUpdate();
    });
    root.getElementById('btn-mode-preview')?.addEventListener('click', () => {
      this.core.setViewMode('preview');
      this.dispatchEvent(new CustomEvent('view-mode-change', { detail: { mode: 'preview' }, bubbles: true, composed: true }));
      this.requestUpdate();
    });
    root.getElementById('btn-mode-code')?.addEventListener('click', () => {
      this.core.setViewMode('code');
      this.core.setBottomEditorTab('bpxJson');
      this.dispatchEvent(new CustomEvent('view-mode-change', { detail: { mode: 'code' }, bubbles: true, composed: true }));
      this.requestUpdate();
    });

    // Quick Print & Fullscreen
    root.getElementById('btn-quick-print')?.addEventListener('click', () => this.printDocument());
    root.getElementById('btn-fullscreen')?.addEventListener('click', () => this.toggleFullscreen());

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

    // Active band selection
    root.querySelectorAll('.bpx-band-header').forEach((header: any) => {
      header.addEventListener('click', (e: MouseEvent) => {
        e.stopPropagation();
        this.activeBandName = header.dataset.band || 'Detail';
        this.requestUpdate();
      });
    });

    // Compact Toolbox items drag & click
    root.querySelectorAll('.bpx-tool-item').forEach((item: any) => {
      item.addEventListener('dragstart', (e: DragEvent) => {
        this.handleToolDragStart(e, item.dataset.tool || 'Text');
      });
      item.addEventListener('click', (e: MouseEvent) => {
        if ((e.target as HTMLElement)?.classList?.contains('bpx-tool-add-btn')) return;
        this.addComponentToBand(item.dataset.tool || 'Text');
      });
    });

    root.querySelectorAll('.bpx-tool-add-btn').forEach((btn: any) => {
      btn.addEventListener('click', (e: MouseEvent) => {
        e.stopPropagation();
        this.addComponentToBand(btn.dataset.tool || 'Text');
      });
    });

    // Datasets & Fields drag & click
    root.querySelectorAll('.bpx-field-item').forEach((item: any) => {
      item.addEventListener('dragstart', (e: DragEvent) => {
        this.handleFieldDragStart(e, item.dataset.field || '');
      });
      item.addEventListener('click', (e: MouseEvent) => {
        if ((e.target as HTMLElement)?.classList?.contains('bpx-field-add-btn')) return;
        this.addComponentToBand('Expression', this.activeBandName, { expression: `Fields.${item.dataset.field}`, text: '' });
      });
    });

    root.querySelectorAll('.bpx-field-add-btn').forEach((btn: any) => {
      btn.addEventListener('click', (e: MouseEvent) => {
        e.stopPropagation();
        this.addComponentToBand('Expression', this.activeBandName, { expression: `Fields.${btn.dataset.field}`, text: '' });
      });
    });

    // Band dragover / drop
    root.querySelectorAll('.bpx-band-container').forEach((bandEl: any) => {
      bandEl.addEventListener('dragover', (e: DragEvent) => {
        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
      });
      bandEl.addEventListener('drop', (e: DragEvent) => {
        this.handleCanvasDrop(e, bandEl.dataset.band || 'Detail');
      });
    });

    // Canvas universal drop
    const canvasEl = root.querySelector('.bpx-page-canvas') as HTMLElement | null;
    const canvasContainer = root.getElementById('canvas-container');
    [canvasContainer, canvasEl].forEach((container: any) => {
      container?.addEventListener('dragover', (e: any) => {
        e.preventDefault();
        if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
      });
    });
    canvasEl?.addEventListener('drop', (e: any) => {
      this.handleCanvasDrop(e);
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

    // Standard Property inputs
    ['x', 'y', 'width', 'height', 'text', 'expression', 'fontSize', 'fontFamily', 'fontWeight', 'fontStyle', 'alignment'].forEach(key => {
      const input = root.getElementById(`prop-${key}`) as HTMLInputElement | HTMLSelectElement | null;
      input?.addEventListener('input', (e: any) => {
        this.updateSelectedProp(key, e.target.value);
      });
      input?.addEventListener('change', (e: any) => {
        this.updateSelectedProp(key, e.target.value);
        this.core.pushHistory(`Update ${key}`);
      });
    });

    // Text Color Suite
    const colorPicker = root.getElementById('prop-color-picker') as HTMLInputElement | null;
    const colorText = root.getElementById('prop-color') as HTMLInputElement | null;
    colorPicker?.addEventListener('input', (e: any) => {
      if (colorText) colorText.value = e.target.value;
      this.updateSelectedProp('color', e.target.value);
    });
    colorPicker?.addEventListener('change', () => {
      this.core.pushHistory('Update Text Color');
    });

    colorText?.addEventListener('input', (e: any) => {
      const val = e.target.value.trim();
      if (/^#[0-9A-Fa-f]{6}$/.test(val)) {
        if (colorPicker) colorPicker.value = val;
      }
      this.updateSelectedProp('color', val);
    });
    colorText?.addEventListener('change', () => {
      this.core.pushHistory('Update Text Color');
    });

    root.getElementById('prop-color-eyedropper')?.addEventListener('click', async () => {
      if ((window as any).EyeDropper) {
        try {
          const eyeDropper = new (window as any).EyeDropper();
          const result = await eyeDropper.open();
          if (result?.sRGBHex) {
            const hex = result.sRGBHex;
            if (colorPicker) colorPicker.value = hex;
            if (colorText) colorText.value = hex;
            this.updateSelectedProp('color', hex);
            this.core.pushHistory('Pick Color with EyeDropper');
          }
        } catch {
          // User canceled
        }
      } else {
        alert('EyeDropper API is supported in Chromium browsers (Chrome, Edge). You can use the Color Picker box or Hex text box.');
      }
    });

    root.querySelectorAll('.bpx-color-swatch').forEach((swatch: any) => {
      swatch.addEventListener('click', () => {
        const col = swatch.dataset.color;
        if (!col) return;
        if (colorPicker) colorPicker.value = col;
        if (colorText) colorText.value = col;
        this.updateSelectedProp('color', col);
        this.core.pushHistory(`Set Color to ${col}`);
      });
    });

    // Background Fill
    const bgPicker = root.getElementById('prop-bg-picker') as HTMLInputElement | null;
    const bgText = root.getElementById('prop-backgroundColor') as HTMLInputElement | null;
    bgPicker?.addEventListener('input', (e: any) => {
      if (bgText) bgText.value = e.target.value;
      this.updateSelectedProp('backgroundColor', e.target.value);
    });
    bgPicker?.addEventListener('change', () => this.core.pushHistory('Update Background Color'));
    bgText?.addEventListener('input', (e: any) => {
      if (/^#[0-9A-Fa-f]{6}$/.test(e.target.value.trim()) && bgPicker) {
        bgPicker.value = e.target.value.trim();
      }
      this.updateSelectedProp('backgroundColor', e.target.value);
    });
    bgText?.addEventListener('change', () => this.core.pushHistory('Update Background Color'));

    // Border properties
    const borderColorPicker = root.getElementById('prop-border-color-picker') as HTMLInputElement | null;
    const borderColorText = root.getElementById('prop-borderColor') as HTMLInputElement | null;
    borderColorPicker?.addEventListener('input', (e: any) => {
      if (borderColorText) borderColorText.value = e.target.value;
      this.updateSelectedProp('borderColor', e.target.value);
    });
    borderColorPicker?.addEventListener('change', () => this.core.pushHistory('Update Border Color'));
    borderColorText?.addEventListener('input', (e: any) => {
      if (/^#[0-9A-Fa-f]{6}$/.test(e.target.value.trim()) && borderColorPicker) {
        borderColorPicker.value = e.target.value.trim();
      }
      this.updateSelectedProp('borderColor', e.target.value);
    });
    borderColorText?.addEventListener('change', () => this.core.pushHistory('Update Border Color'));

    ['borderWidth', 'borderStyle', 'borderRadius'].forEach(key => {
      const input = root.getElementById(`prop-${key}`) as HTMLInputElement | HTMLSelectElement | null;
      input?.addEventListener('input', (e: any) => {
        this.updateSelectedProp(key, e.target.value);
      });
      input?.addEventListener('change', (e: any) => {
        this.updateSelectedProp(key, e.target.value);
        this.core.pushHistory(`Update ${key}`);
      });
    });

    // Delete Element button in Inspector
    root.getElementById('prop-delete-btn')?.addEventListener('click', () => {
      this.core.removeSelectedElements();
      this.dispatchChange();
      this.requestUpdate();
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
  customElements.define('bangplanix-designer', BangplanixDesigner as unknown as CustomElementConstructor);
}
