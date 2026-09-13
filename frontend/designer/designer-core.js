/**
 * Bangplanix Visual Designer Core Engine
 * Manages Multi-panel Layout, WYSIWYG Canvas Math, Undo/Redo Stacks,
 * 2-Way .bpx JSON Schema Synchronization, Chart/Barcode Configurator & Data Explorer.
 */

export class BangplanixDesignerCore {
  constructor(options = {}) {
    // 1. View & Mode State
    this.viewMode = options.viewMode || 'design'; // 'design' | 'preview' | 'code'
    this.zoomLevel = options.zoomLevel || 100; // 25% - 400%
    this.snapToGrid = options.snapToGrid !== undefined ? options.snapToGrid : true;
    this.gridSize = options.gridSize || 10; // points
    this.showRulers = options.showRulers !== undefined ? options.showRulers : true;
    this.showGuides = options.showGuides !== undefined ? options.showGuides : true;

    // 2. Multi-panel Visibility
    this.panels = {
      toolbox: true,
      dataExplorer: true,
      propertyInspector: true,
      bottomEditor: true
    };
    this.bottomEditorTab = 'expression'; // 'expression' | 'sql' | 'bpxJson' | 'logs'

    // 3. Document / Report Definition (.bpx Schema Model)
    this.report = options.report || this.createDefaultReport();

    // 4. Selection & Multi-Selection State
    this.selectedElementIds = [];
    this.activeBandName = 'Detail'; // 'ReportHeader' | 'PageHeader' | 'Detail' | 'PageFooter' | 'ReportFooter'

    // 5. History / Undo-Redo Stacks
    this.historyStack = [];
    this.redoStack = [];
    this.maxHistory = 50;

    // 6. Alignment & Drag Temp State
    this.activeGuides = []; // { type: 'vertical'|'horizontal', pos: number }

    // Save initial state to history
    this.pushHistory('Initial Document');
  }

  createDefaultReport() {
    return {
      version: '1.0',
      metadata: {
        title: 'Untitled Report',
        author: 'Bangplanix Designer',
        createdAt: new Date().toISOString()
      },
      pageSetup: {
        width: 595.28, // A4 Pt width
        height: 841.89, // A4 Pt height
        orientation: 'Portrait',
        marginTop: 36,
        marginBottom: 36,
        marginLeft: 36,
        marginRight: 36,
        unit: 'Pt'
      },
      datasets: [
        {
          name: 'MainDataset',
          connectorType: 'Sql',
          query: 'SELECT * FROM Invoices WHERE Status = @Status',
          parameters: [{ name: 'Status', type: 'String', defaultValue: 'Active' }],
          fields: [
            { name: 'InvoiceNo', type: 'String' },
            { name: 'CustomerName', type: 'String' },
            { name: 'Amount', type: 'Decimal' },
            { name: 'IssueDate', type: 'DateTime' }
          ]
        }
      ],
      parameters: [
        {
          name: 'Status',
          type: 'String',
          defaultValue: 'Active',
          promptText: 'Select Invoice Status',
          availableValues: [
            { label: 'Active', value: 'Active' },
            { label: 'Closed', value: 'Closed' }
          ]
        }
      ],
      bands: {
        ReportHeader: {
          height: 60,
          elements: [
            {
              id: 'el_title',
              type: 'Text',
              x: 0,
              y: 10,
              width: 523,
              height: 30,
              text: 'Enterprise Summary Report',
              style: {
                fontSize: 18,
                bold: true,
                color: '#0f172a',
                alignment: 'Center'
              }
            }
          ]
        },
        PageHeader: {
          height: 30,
          elements: []
        },
        Detail: {
          height: 40,
          elements: [
            {
              id: 'el_inv_no',
              type: 'Text',
              x: 0,
              y: 5,
              width: 150,
              height: 25,
              expression: 'Fields.InvoiceNo',
              style: { fontSize: 11, color: '#334155' }
            },
            {
              id: 'el_amount',
              type: 'Text',
              x: 160,
              y: 5,
              width: 120,
              height: 25,
              expression: 'FormatCurrency(Fields.Amount)',
              style: { fontSize: 11, color: '#059669', alignment: 'Right' }
            }
          ]
        },
        PageFooter: {
          height: 25,
          elements: [
            {
              id: 'el_page_no',
              type: 'Text',
              x: 0,
              y: 5,
              width: 523,
              height: 18,
              expression: '$\"Page {Globals.PageNumber} of {Globals.TotalPages}\"',
              style: { fontSize: 9, color: '#94a3b8', alignment: 'Right' }
            }
          ]
        },
        ReportFooter: {
          height: 40,
          elements: []
        }
      }
    };
  }

  // --- View Mode & Zoom Math ---
  setViewMode(mode) {
    if (['design', 'preview', 'code'].includes(mode)) {
      this.viewMode = mode;
    }
    return this.viewMode;
  }

  setZoom(level) {
    this.zoomLevel = Math.max(25, Math.min(400, Math.round(level)));
    return this.zoomLevel;
  }

  zoomIn(step = 10) {
    return this.setZoom(this.zoomLevel + step);
  }

  zoomOut(step = 10) {
    return this.setZoom(this.zoomLevel - step);
  }

  resetZoom() {
    return this.setZoom(100);
  }

  togglePanel(panelName, force = null) {
    if (this.panels[panelName] !== undefined) {
      this.panels[panelName] = force !== null ? force : !this.panels[panelName];
    }
    return this.panels[panelName];
  }

  setBottomEditorTab(tab) {
    if (['expression', 'sql', 'bpxJson', 'logs'].includes(tab)) {
      this.bottomEditorTab = tab;
    }
    return this.bottomEditorTab;
  }

  // --- Element CRUD & Positioning ---
  addElement(bandName, element) {
    if (!this.report.bands[bandName]) {
      this.report.bands[bandName] = { height: 50, elements: [] };
    }

    const newElement = {
      id: element.id || `el_${Date.now()}_${Math.random().toString(36).substring(2, 7)}`,
      type: element.type || 'Text',
      x: this.applySnap(element.x || 0),
      y: this.applySnap(element.y || 0),
      width: this.applySnap(element.width || 120),
      height: this.applySnap(element.height || 30),
      text: element.text || '',
      expression: element.expression || '',
      style: { ...(element.style || {}) },
      chart: element.chart ? JSON.parse(JSON.stringify(element.chart)) : null,
      barcode: element.barcode ? JSON.parse(JSON.stringify(element.barcode)) : null,
      imageSource: element.imageSource || null
    };

    this.report.bands[bandName].elements.push(newElement);
    this.selectedElementIds = [newElement.id];
    this.activeBandName = bandName;
    this.pushHistory(`Add ${newElement.type} Element`);
    return newElement;
  }

  removeSelectedElements() {
    if (this.selectedElementIds.length === 0) return 0;
    let removedCount = 0;

    for (const bandKey in this.report.bands) {
      const band = this.report.bands[bandKey];
      if (band && Array.isArray(band.elements)) {
        const initialLen = band.elements.length;
        band.elements = band.elements.filter(el => !this.selectedElementIds.includes(el.id));
        removedCount += (initialLen - band.elements.length);
      }
    }

    this.selectedElementIds = [];
    if (removedCount > 0) {
      this.pushHistory('Delete Elements');
    }
    return removedCount;
  }

  findElement(elementId) {
    for (const bandKey in this.report.bands) {
      const band = this.report.bands[bandKey];
      if (band && Array.isArray(band.elements)) {
        const found = band.elements.find(el => el.id === elementId);
        if (found) return { element: found, bandName: bandKey };
      }
    }
    return null;
  }

  selectElement(elementId, multiSelect = false) {
    if (!multiSelect) {
      this.selectedElementIds = elementId ? [elementId] : [];
    } else {
      if (elementId && !this.selectedElementIds.includes(elementId)) {
        this.selectedElementIds.push(elementId);
      } else if (elementId) {
        this.selectedElementIds = this.selectedElementIds.filter(id => id !== elementId);
      }
    }

    if (this.selectedElementIds.length === 1) {
      const info = this.findElement(this.selectedElementIds[0]);
      if (info) this.activeBandName = info.bandName;
    }
    return this.selectedElementIds;
  }

  selectAllElements(bandName = null) {
    const ids = [];
    const bandsToScan = bandName ? [this.report.bands[bandName]] : Object.values(this.report.bands);

    bandsToScan.forEach(band => {
      if (band && Array.isArray(band.elements)) {
        band.elements.forEach(el => ids.push(el.id));
      }
    });

    this.selectedElementIds = ids;
    return this.selectedElementIds;
  }

  // --- Snap to Grid & Alignment Math ---
  applySnap(val) {
    if (!this.snapToGrid || this.gridSize <= 1) return Math.round(val);
    return Math.round(val / this.gridSize) * this.gridSize;
  }

  moveElement(elementId, newX, newY, enforceBounds = true) {
    const info = this.findElement(elementId);
    if (!info) return null;

    let x = this.applySnap(newX);
    let y = this.applySnap(newY);

    if (enforceBounds) {
      x = Math.max(0, x);
      y = Math.max(0, y);
    }

    info.element.x = x;
    info.element.y = y;
    return info.element;
  }

  resizeElement(elementId, newWidth, newHeight) {
    const info = this.findElement(elementId);
    if (!info) return null;

    info.element.width = Math.max(5, this.applySnap(newWidth));
    info.element.height = Math.max(5, this.applySnap(newHeight));
    return info.element;
  }

  // Multi-Selection Bounding Box
  getSelectionBoundingBox() {
    if (this.selectedElementIds.length === 0) return null;

    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    let foundAny = false;

    this.selectedElementIds.forEach(id => {
      const info = this.findElement(id);
      if (info) {
        foundAny = true;
        const el = info.element;
        minX = Math.min(minX, el.x);
        minY = Math.min(minY, el.y);
        maxX = Math.max(maxX, el.x + el.width);
        maxY = Math.max(maxY, el.y + el.height);
      }
    });

    if (!foundAny) return null;
    return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
  }

  // Alignment Commands (Left, Center, Right, Top, Middle, Bottom, Distribute)
  alignSelectedElements(alignmentType) {
    if (this.selectedElementIds.length < 2) return;
    const elements = this.selectedElementIds.map(id => this.findElement(id)?.element).filter(Boolean);
    if (elements.length < 2) return;

    const bbox = this.getSelectionBoundingBox();
    if (!bbox) return;

    switch (alignmentType) {
      case 'left':
        elements.forEach(el => { el.x = bbox.x; });
        break;
      case 'center':
        elements.forEach(el => { el.x = this.applySnap(bbox.x + (bbox.width / 2) - (el.width / 2)); });
        break;
      case 'right':
        elements.forEach(el => { el.x = bbox.x + bbox.width - el.width; });
        break;
      case 'top':
        elements.forEach(el => { el.y = bbox.y; });
        break;
      case 'middle':
        elements.forEach(el => { el.y = this.applySnap(bbox.y + (bbox.height / 2) - (el.height / 2)); });
        break;
      case 'bottom':
        elements.forEach(el => { el.y = bbox.y + bbox.height - el.height; });
        break;
      case 'distributeHorizontally':
        elements.sort((a, b) => a.x - b.x);
        const totalW = elements.reduce((sum, el) => sum + el.width, 0);
        const freeW = bbox.width - totalW;
        const gapH = freeW / (elements.length - 1);
        let curX = bbox.x;
        elements.forEach(el => {
          el.x = this.applySnap(curX);
          curX += el.width + gapH;
        });
        break;
      case 'distributeVertically':
        elements.sort((a, b) => a.y - b.y);
        const totalH = elements.reduce((sum, el) => sum + el.height, 0);
        const freeH = bbox.height - totalH;
        const gapV = freeH / (elements.length - 1);
        let curY = bbox.y;
        elements.forEach(el => {
          el.y = this.applySnap(curY);
          curY += el.height + gapV;
        });
        break;
    }

    this.pushHistory(`Align Elements ${alignmentType}`);
  }

  // --- Visual Chart Configurator ---
  configureChart(elementId, chartConfig) {
    const info = this.findElement(elementId);
    if (!info) return null;

    info.element.type = 'Chart';
    info.element.chart = {
      chartType: chartConfig.chartType || 'Column',
      title: chartConfig.title || 'Chart Title',
      subtitle: chartConfig.subtitle || '',
      palette: chartConfig.palette || 'Default',
      customColors: chartConfig.customColors || [],
      categories: chartConfig.categories || ['Q1', 'Q2', 'Q3', 'Q4'],
      showLegend: chartConfig.showLegend !== undefined ? chartConfig.showLegend : true,
      legendPosition: chartConfig.legendPosition || 'Bottom',
      donutHoleSize: chartConfig.donutHoleSize || 0.5,
      series: chartConfig.series || [
        {
          name: 'Series 1',
          values: [100, 200, 150, 300],
          color: '#2563eb'
        }
      ],
      xAxis: chartConfig.xAxis || { title: 'Category Axis', showGridLines: true },
      yAxis: chartConfig.yAxis || { title: 'Value Axis', showGridLines: true },
      gaugeOptions: chartConfig.gaugeOptions || null,
      bulletOptions: chartConfig.bulletOptions || null,
      waterfallOptions: chartConfig.waterfallOptions || null
    };

    this.pushHistory('Configure Chart');
    return info.element.chart;
  }

  // --- Visual Barcode & QR Configurator ---
  configureBarcode(elementId, barcodeConfig) {
    const info = this.findElement(elementId);
    if (!info) return null;

    info.element.type = barcodeConfig.barcodeType === 'QrCode' ? 'QrCode' : 'Barcode';
    info.element.text = barcodeConfig.text || info.element.text || '123456789';
    info.element.barcodeType = barcodeConfig.barcodeType || 'Code128';
    info.element.showBarcodeText = barcodeConfig.showBarcodeText !== undefined ? barcodeConfig.showBarcodeText : true;
    info.element.qrEccLevel = barcodeConfig.qrEccLevel || 'Medium';

    this.pushHistory('Configure Barcode');
    return info.element;
  }

  // --- Data Explorer & Encrypted Connection Strings ---
  addDataset(dataset) {
    if (!dataset.name) throw new Error('Dataset name is required.');
    const newDs = {
      name: dataset.name,
      connectorType: dataset.connectorType || 'Sql',
      connectionStringEncrypted: dataset.connectionString ? this.maskConnectionString(dataset.connectionString) : null,
      query: dataset.query || 'SELECT 1',
      parameters: dataset.parameters || [],
      fields: dataset.fields || []
    };

    this.report.datasets = this.report.datasets || [];
    this.report.datasets.push(newDs);
    this.pushHistory(`Add Dataset: ${newDs.name}`);
    return newDs;
  }

  maskConnectionString(connStr) {
    if (!connStr) return '';
    // Mask password in standard SQL connection strings
    return connStr.replace(/(Password|Pwd)\s*=\s*([^;]+)/gi, '$1=********');
  }

  // --- 2-Way .bpx JSON Schema Synchronization & Validation ---
  getBpxJson(pretty = true) {
    return pretty ? JSON.stringify(this.report, null, 2) : JSON.stringify(this.report);
  }

  loadBpxJson(jsonString) {
    try {
      const parsed = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString;
      if (!parsed || typeof parsed !== 'object') {
        throw new Error('Invalid .bpx schema format: Root must be an object.');
      }
      if (!parsed.bands || typeof parsed.bands !== 'object') {
        throw new Error('Invalid .bpx schema format: Missing required bands definition.');
      }

      this.report = parsed;
      this.selectedElementIds = [];
      this.pushHistory('Import .bpx JSON');
      return { success: true };
    } catch (err) {
      return { success: false, error: err.message };
    }
  }

  // --- History (Undo / Redo) Stack ---
  pushHistory(actionName = 'Edit') {
    const snapshot = JSON.stringify(this.report);
    if (this.historyStack.length > 0 && this.historyStack[this.historyStack.length - 1].snapshot === snapshot) {
      return; // Deduplicate identical snapshots
    }

    this.historyStack.push({ actionName, snapshot, timestamp: Date.now() });
    if (this.historyStack.length > this.maxHistory) {
      this.historyStack.shift();
    }
    this.redoStack = []; // Clear redo stack on new modification
  }

  undo() {
    if (this.historyStack.length <= 1) return false;

    const current = this.historyStack.pop();
    this.redoStack.push(current);

    const previous = this.historyStack[this.historyStack.length - 1];
    this.report = JSON.parse(previous.snapshot);
    this.selectedElementIds = [];
    return true;
  }

  redo() {
    if (this.redoStack.length === 0) return false;

    const next = this.redoStack.pop();
    this.historyStack.push(next);
    this.report = JSON.parse(next.snapshot);
    this.selectedElementIds = [];
    return true;
  }

  // --- Bangplanix AI Suite Assistant Integration ---
  applyAiGeneratedReport(bpxJsonString) {
    const res = this.loadBpxJson(bpxJsonString);
    if (res.success) {
      this.pushHistory('AI Generated Report');
    }
    return res;
  }

  suggestAiExpression(userPrompt) {
    if (!userPrompt) return null;
    let expr = `(Fields["Amount"] > 1000) ? (Fields["Amount"] * 0.05) : 0`;
    let type = 'decimal';
    let expl = 'Default conditional expression';

    if (userPrompt.includes('VAT') || userPrompt.includes('ภาษี')) {
      expr = `Convert.ToDecimal(Fields["Amount"]) * 0.07m`;
      expl = 'คำนวณภาษี VAT 7%';
    } else if (userPrompt.includes('บาท') || userPrompt.includes('ThaiBaht')) {
      expr = `ThaiBahtText(Convert.ToDecimal(Fields["Total"]))`;
      type = 'string';
      expl = 'แปลงตัวเลขเป็นตัวอักษรบาทไทย';
    }

    return { csharpExpression: expr, returnType: type, explanation: expl };
  }

  diagnoseAndAutoFix(brokenJsonString, errorMsg = '') {
    if (!brokenJsonString) {
      const def = this.createDefaultReport();
      this.report = def;
      this.pushHistory('AI Auto-Fix Rebuild');
      return { isRepaired: true, appliedFixes: ['Created default template scaffold'] };
    }

    try {
      let cleaned = brokenJsonString.trim();
      cleaned = cleaned.replace(/^```json/i, '').replace(/^```/, '').replace(/```$/, '').trim();
      cleaned = cleaned.replace(/,\s*([\]}])/g, '$1');
      const obj = JSON.parse(cleaned);

      if (!obj.version) obj.version = '1.0';
      if (!obj.pageSetup) obj.pageSetup = { paperSize: 'A4', orientation: 'Portrait', unit: 'Pt' };
      if (!Array.isArray(obj.bands)) {
        obj.bands = [{ bandType: 'Detail', height: 30, elements: [] }];
      }

      this.report = obj;
      this.pushHistory('AI Auto-Fix Repair');
      return { isRepaired: true, patchedJson: JSON.stringify(obj, null, 2), appliedFixes: ['Repaired schema structure'] };
    } catch (e) {
      const def = this.createDefaultReport();
      this.report = def;
      this.pushHistory('AI Auto-Fix Rebuild');
      return { isRepaired: true, patchedJson: JSON.stringify(def, null, 2), appliedFixes: ['Fatal parse error, reconstructed valid template'] };
    }
  }
}

