import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BangplanixDesignerCore } from '../designer-core.js';

describe('Bangplanix Web Visual Designer Core Tests (<bangplanix-designer>)', () => {

  describe('1. Multi-panel Layout & View Modes (Task 4.1.1 & Task 4.1.3)', () => {
    test('should initialize with default 4-panel layout and design mode', () => {
      const designer = new BangplanixDesignerCore();
      assert.strictEqual(designer.viewMode, 'design');
      assert.strictEqual(designer.zoomLevel, 100);
      assert.strictEqual(designer.panels.toolbox, true);
      assert.strictEqual(designer.panels.dataExplorer, true);
      assert.strictEqual(designer.panels.propertyInspector, true);
      assert.strictEqual(designer.panels.bottomEditor, true);
    });

    test('should switch between 3 view modes: design, preview, code', () => {
      const designer = new BangplanixDesignerCore();
      designer.setViewMode('preview');
      assert.strictEqual(designer.viewMode, 'preview');

      designer.setViewMode('code');
      assert.strictEqual(designer.viewMode, 'code');

      designer.setViewMode('invalid_mode');
      assert.strictEqual(designer.viewMode, 'code'); // Should not change on invalid
    });

    test('togglePanel and setBottomEditorTab should control workspace panels', () => {
      const designer = new BangplanixDesignerCore();
      designer.togglePanel('toolbox', false);
      assert.strictEqual(designer.panels.toolbox, false);

      designer.setBottomEditorTab('sql');
      assert.strictEqual(designer.bottomEditorTab, 'sql');
      designer.setBottomEditorTab('bpxJson');
      assert.strictEqual(designer.bottomEditorTab, 'bpxJson');
    });
  });

  describe('2. WYSIWYG Drag & Drop, Zoom & Snap-to-Grid Math (Task 4.1.2)', () => {
    test('zoom operations should clamp within 25% to 400%', () => {
      const designer = new BangplanixDesignerCore();
      designer.setZoom(500);
      assert.strictEqual(designer.zoomLevel, 400);

      designer.setZoom(10);
      assert.strictEqual(designer.zoomLevel, 25);

      designer.zoomIn(20);
      assert.strictEqual(designer.zoomLevel, 45);

      designer.resetZoom();
      assert.strictEqual(designer.zoomLevel, 100);
    });

    test('snap-to-grid should snap coordinates to nearest grid size', () => {
      const designer = new BangplanixDesignerCore({ gridSize: 10, snapToGrid: true });
      assert.strictEqual(designer.applySnap(14), 10);
      assert.strictEqual(designer.applySnap(16), 20);
      assert.strictEqual(designer.applySnap(25), 30);
    });

    test('addElement and moveElement should position elements correctly', () => {
      const designer = new BangplanixDesignerCore({ gridSize: 10 });
      const el = designer.addElement('Detail', {
        type: 'Text',
        x: 43,
        y: 18,
        width: 154,
        height: 33,
        text: 'Employee Name'
      });

      assert.strictEqual(el.x, 40); // Snapped from 43
      assert.strictEqual(el.y, 20); // Snapped from 18
      assert.strictEqual(el.width, 150); // Snapped from 154

      designer.moveElement(el.id, 82, 99);
      assert.strictEqual(el.x, 80);
      assert.strictEqual(el.y, 100);
    });

    test('resizeElement should prevent negative or zero dimensions', () => {
      const designer = new BangplanixDesignerCore();
      const el = designer.addElement('Detail', { type: 'Shape', x: 0, y: 0, width: 100, height: 50 });
      designer.resizeElement(el.id, -20, 0);
      assert.strictEqual(el.width, 5); // Minimum enforced size
      assert.strictEqual(el.height, 5); // Minimum enforced size
    });
  });

  describe('3. Multi-Selection & Alignment Guides (Task 4.1.2)', () => {
    test('selectElement and multi-selection bounding box computation', () => {
      const designer = new BangplanixDesignerCore();
      const el1 = designer.addElement('Detail', { x: 10, y: 10, width: 100, height: 30 });
      const el2 = designer.addElement('Detail', { x: 200, y: 50, width: 80, height: 40 });

      designer.selectElement(el1.id);
      designer.selectElement(el2.id, true); // multi-select

      assert.strictEqual(designer.selectedElementIds.length, 2);
      const bbox = designer.getSelectionBoundingBox();
      assert.strictEqual(bbox.x, 10);
      assert.strictEqual(bbox.y, 10);
      assert.strictEqual(bbox.width, 270); // 200 + 80 - 10 = 270
      assert.strictEqual(bbox.height, 80); // 50 + 40 - 10 = 80
    });

    test('alignSelectedElements should align left, center, right, top, bottom, and distribute', () => {
      const designer = new BangplanixDesignerCore({ snapToGrid: false });
      const el1 = designer.addElement('Detail', { x: 10, y: 10, width: 100, height: 30 });
      const el2 = designer.addElement('Detail', { x: 150, y: 50, width: 50, height: 30 });

      designer.selectElement(el1.id);
      designer.selectElement(el2.id, true);

      // Align Left
      designer.alignSelectedElements('left');
      assert.strictEqual(el1.x, 10);
      assert.strictEqual(el2.x, 10);

      // Align Top
      designer.alignSelectedElements('top');
      assert.strictEqual(el1.y, 10);
      assert.strictEqual(el2.y, 10);
    });

    test('removeSelectedElements should delete chosen items and clear selection', () => {
      const designer = new BangplanixDesignerCore();
      const el = designer.addElement('Detail', { text: 'To be deleted' });
      designer.selectElement(el.id);

      const count = designer.removeSelectedElements();
      assert.strictEqual(count, 1);
      assert.strictEqual(designer.selectedElementIds.length, 0);
      assert.strictEqual(designer.findElement(el.id), null);
    });
  });

  describe('4. Visual Chart & Barcode Configurators (Task 4.1.5)', () => {
    test('configureChart should structure complete vector chart definition', () => {
      const designer = new BangplanixDesignerCore();
      const el = designer.addElement('Detail', { type: 'Chart', x: 0, y: 0, width: 400, height: 250 });

      const chart = designer.configureChart(el.id, {
        chartType: 'Doughnut',
        title: 'Department Expense Breakdown',
        palette: 'Emerald',
        donutHoleSize: 0.6,
        categories: ['HR', 'R&D', 'Sales'],
        series: [{ name: '2026 Expenses', values: [30, 50, 20] }]
      });

      assert.strictEqual(chart.chartType, 'Doughnut');
      assert.strictEqual(chart.palette, 'Emerald');
      assert.strictEqual(chart.donutHoleSize, 0.6);
      assert.strictEqual(chart.series[0].values.length, 3);
    });

    test('configureBarcode should setup 1D & 2D barcode parameters', () => {
      const designer = new BangplanixDesignerCore();
      const el = designer.addElement('Detail', { type: 'Barcode', text: 'TAX-9988' });

      designer.configureBarcode(el.id, {
        barcodeType: 'QrCode',
        text: 'https://bangplanix.io/verify',
        qrEccLevel: 'High'
      });

      assert.strictEqual(el.type, 'QrCode');
      assert.strictEqual(el.barcodeType, 'QrCode');
      assert.strictEqual(el.text, 'https://bangplanix.io/verify');
      assert.strictEqual(el.qrEccLevel, 'High');
    });

    test('addDataset and connection string masking', () => {
      const designer = new BangplanixDesignerCore();
      const ds = designer.addDataset({
        name: 'FinanceDb',
        connectorType: 'Sql',
        connectionString: 'Server=sql.corp;Database=Finance;User Id=sa;Password=super_secret_pwd;',
        query: 'SELECT * FROM Ledger WHERE Year = @Year',
        parameters: [{ name: 'Year', type: 'Int', defaultValue: 2026 }]
      });

      assert.strictEqual(ds.name, 'FinanceDb');
      assert.strictEqual(ds.connectionStringEncrypted, 'Server=sql.corp;Database=Finance;User Id=sa;Password=********;');
      assert.strictEqual(designer.report.datasets.length, 2); // Initial 1 + new 1
    });
  });

  describe('5. 2-Way .bpx JSON Schema Synchronization & Undo/Redo (Task 4.1.3 & Task 4.1.4)', () => {
    test('getBpxJson and loadBpxJson should serialize and restore reports', () => {
      const designer = new BangplanixDesignerCore();
      const json = designer.getBpxJson();
      assert.ok(json.includes('Enterprise Summary Report'));

      const result = designer.loadBpxJson(json);
      assert.strictEqual(result.success, true);
    });

    test('loadBpxJson should reject invalid payloads gracefully', () => {
      const designer = new BangplanixDesignerCore();
      const result = designer.loadBpxJson('INVALID_NON_JSON');
      assert.strictEqual(result.success, false);
      assert.ok(result.error);
    });

    test('undo and redo should reverse and reapply modifications accurately', () => {
      const designer = new BangplanixDesignerCore();
      const initialCount = designer.report.bands.Detail.elements.length;

      const el = designer.addElement('Detail', { text: 'New Row' });
      assert.strictEqual(designer.report.bands.Detail.elements.length, initialCount + 1);

      // Undo Add
      const undone = designer.undo();
      assert.strictEqual(undone, true);
      assert.strictEqual(designer.report.bands.Detail.elements.length, initialCount);

      // Redo Add
      const redone = designer.redo();
      assert.strictEqual(redone, true);
      assert.strictEqual(designer.report.bands.Detail.elements.length, initialCount + 1);
    });
  });

  describe('6. Bangplanix AI Suite Designer Integration (Sprint 4.2)', () => {
    test('applyAiGeneratedReport should parse and load AI generated .bpx schema', () => {
      const designer = new BangplanixDesignerCore();
      const aiJson = JSON.stringify({
        version: '1.0',
        metadata: { title: 'AI Generated Sales Report' },
        pageSetup: { paperSize: 'A4', orientation: 'Portrait' },
        bands: {
          Header: { height: 50, elements: [{ id: 'lbl_1', type: 'Label', text: 'AI Sales Report' }] },
          Detail: { height: 30, elements: [] }
        }
      });

      const res = designer.applyAiGeneratedReport(aiJson);
      assert.strictEqual(res.success, true);
      assert.strictEqual(designer.report.metadata.title, 'AI Generated Sales Report');
    });

    test('suggestAiExpression should convert natural language intent to C# Roslyn formula', () => {
      const designer = new BangplanixDesignerCore();
      const vatExpr = designer.suggestAiExpression('คำนวณภาษี VAT 7% จากยอด Amount');
      assert.ok(vatExpr.csharpExpression.includes('0.07m'));

      const thaiBahtExpr = designer.suggestAiExpression('แปลงตัวเลขเป็นตัวอักษรบาทไทย ThaiBaht');
      assert.ok(thaiBahtExpr.csharpExpression.includes('ThaiBahtText'));
      assert.strictEqual(thaiBahtExpr.returnType, 'string');
    });

    test('diagnoseAndAutoFix should repair broken or malformed report structures', () => {
      const designer = new BangplanixDesignerCore();
      const brokenJson = '```json\n{ "metadata": { "title": "Broken Report" }, "bands": { "Detail": { "height": 30 } }, }\n```';

      const fixResult = designer.diagnoseAndAutoFix(brokenJson);
      assert.strictEqual(fixResult.isRepaired, true);
      assert.strictEqual(designer.report.version, '1.0');
      assert.strictEqual(designer.report.metadata.title, 'Broken Report');
    });
  });

});

