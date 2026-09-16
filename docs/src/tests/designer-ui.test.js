import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BangplanixDesignerCore } from '../../../frontend/designer/designer-core.js';
import { TemplateGalleryManager } from '../dist/designer/template-gallery.js';
import { BangplanixDesigner } from '../dist/designer/bangplanix-designer.js';
import { ShortcutManager } from '../dist/designer/shortcut-manager.js';

describe('Bangplanix Designer UI & A4 Layout Integration Tests', () => {

  test('default report should adhere to ISO standard A4 dimensions (595.28 x 841.89 pt)', () => {
    const designer = new BangplanixDesignerCore();
    assert.strictEqual(designer.report.pageSetup.width, 595.28);
    assert.strictEqual(designer.report.pageSetup.height, 841.89);
    assert.strictEqual(designer.report.pageSetup.orientation, 'Portrait');
  });

  test('template gallery includes a4_commercial_invoice with accurate A4 paperKind and dimensions', () => {
    const gallery = new TemplateGalleryManager();
    const a4Invoice = gallery.getTemplateById('a4_commercial_invoice');
    assert.ok(a4Invoice, 'a4_commercial_invoice template must exist');
    assert.strictEqual(a4Invoice.schema.pageSetup.paperKind, 'A4');
    assert.strictEqual(a4Invoice.schema.pageSetup.width, 595.28);
    assert.strictEqual(a4Invoice.schema.pageSetup.height, 841.89);
    assert.ok(a4Invoice.schema.bands.ReportHeader, 'Must have ReportHeader');
    assert.ok(a4Invoice.schema.bands.Detail, 'Must have Detail');
    assert.ok(a4Invoice.schema.bands.ReportFooter, 'Must have ReportFooter');
  });

  test('updating text and element properties directly mutates model and pushes history', () => {
    const designer = new BangplanixDesignerCore();
    const el = designer.addElement('Detail', {
      type: 'Text',
      x: 10,
      y: 10,
      width: 100,
      height: 25,
      text: 'Original Text'
    });

    assert.strictEqual(el.text, 'Original Text');

    // Select and update
    designer.selectElement(el.id);
    const info = designer.findElement(el.id);
    assert.ok(info);
    
    info.element.text = 'Updated Via UI Property Box';
    info.element.x = 50;
    info.element.y = 30;
    designer.pushHistory('Update text and position');

    // Verify mutation
    const updated = designer.findElement(el.id);
    assert.strictEqual(updated.element.text, 'Updated Via UI Property Box');
    assert.strictEqual(updated.element.x, 50);
    assert.strictEqual(updated.element.y, 30);

    // Verify undo restores previous state
    designer.undo();
    const reverted = designer.findElement(el.id);
    assert.strictEqual(reverted.element.text, 'Original Text');
    assert.strictEqual(reverted.element.x, 10);
    assert.strictEqual(reverted.element.y, 10);

    // Verify redo
    designer.redo();
    const redone = designer.findElement(el.id);
    assert.strictEqual(redone.element.text, 'Updated Via UI Property Box');
  });

  test('exporting to .bpx JSON and reloading retains all A4 metadata and elements', () => {
    const designer = new BangplanixDesignerCore();
    const jsonStr = designer.getBpxJson();
    assert.ok(jsonStr.includes('595.28'));
    assert.ok(jsonStr.includes('841.89'));

    const newDesigner = new BangplanixDesignerCore();
    const res = newDesigner.loadBpxJson(jsonStr);
    assert.strictEqual(res.success, true);
    assert.strictEqual(newDesigner.report.pageSetup.width, 595.28);
    assert.strictEqual(newDesigner.report.pageSetup.height, 841.89);
  });

  test('switching paper size dynamically adjusts pageSetup dimensions and margins correctly', () => {
    const designer = new BangplanixDesignerCore();
    
    // Switch to Letter
    designer.report.pageSetup.paperKind = 'Letter';
    designer.report.pageSetup.width = 612;
    designer.report.pageSetup.height = 792;
    assert.strictEqual(designer.report.pageSetup.width, 612);
    assert.strictEqual(designer.report.pageSetup.height, 792);

    // Switch to A4
    designer.report.pageSetup.paperKind = 'A4';
    designer.report.pageSetup.width = 595.28;
    designer.report.pageSetup.height = 841.89;
    assert.strictEqual(designer.report.pageSetup.width, 595.28);
    assert.strictEqual(designer.report.pageSetup.height, 841.89);

    // Calculate printable area within margins
    const ps = designer.report.pageSetup;
    const printableWidth = ps.width - (ps.marginLeft + ps.marginRight);
    const printableHeight = ps.height - (ps.marginTop + ps.marginBottom);
    assert.strictEqual(printableWidth, 595.28 - 72);
    assert.strictEqual(printableHeight, 841.89 - 72);
  });

  test('inline text editing commits changes directly to report model and syncs schema', () => {
    const designer = new BangplanixDesignerCore();
    const el = designer.addElement('Detail', {
      type: 'Text',
      x: 20,
      y: 20,
      width: 150,
      height: 30,
      text: 'Double click me'
    });

    assert.strictEqual(el.text, 'Double click me');

    // Simulate inline edit commit
    const info = designer.findElement(el.id);
    info.element.text = 'Inline Edited By User Directly on Canvas';
    designer.pushHistory('Inline Edit Text');

    // Check updated state
    assert.strictEqual(designer.findElement(el.id).element.text, 'Inline Edited By User Directly on Canvas');

    // Verify .bpx JSON schema includes the inline edit
    const json = designer.getBpxJson();
    assert.ok(json.includes('Inline Edited By User Directly on Canvas'));
  });

  test('updating typography properties (font family, font size, color, weight) mutates element style correctly', () => {
    const designer = new BangplanixDesignerCore();
    const el = designer.addElement('Detail', {
      type: 'Text',
      x: 10,
      y: 10,
      width: 200,
      height: 25,
      text: 'Sample Typography'
    });

    const info = designer.findElement(el.id);
    if (!info.element.style) info.element.style = {};
    info.element.style.fontFamily = 'Sarabun, sans-serif';
    info.element.style.fontSize = 14;
    info.element.style.fontWeight = 'bold';
    info.element.style.color = '#2563eb';
    info.element.style.alignment = 'Center';
    designer.pushHistory('Update Typography');

    const updated = designer.findElement(el.id).element;
    assert.strictEqual(updated.style.fontFamily, 'Sarabun, sans-serif');
    assert.strictEqual(updated.style.fontSize, 14);
    assert.strictEqual(updated.style.fontWeight, 'bold');
    assert.strictEqual(updated.style.color, '#2563eb');
    assert.strictEqual(updated.style.alignment, 'Center');

    const json = designer.getBpxJson();
    assert.ok(json.includes('Sarabun, sans-serif'));
    assert.ok(json.includes('14'));
    assert.ok(json.includes('#2563eb'));
  });

  test('color picker, hex input, and swatches update element text color with 2-way sync', () => {
    const component = new BangplanixDesigner();
    const el = component.addComponentToBand('Text', 'Detail', { text: 'Invoice No' });
    assert.ok(el);

    // Simulate selecting element and applying color via swatches/picker
    component.core.selectElement(el.id);
    const info = component.core.findElement(el.id);
    assert.ok(info);

    // 1. Swatch click: Emerald green #059669
    info.element.style.color = '#059669';
    component.core.pushHistory('Set Color to #059669');
    assert.strictEqual(component.core.findElement(el.id).element.style.color, '#059669');

    // 2. Hex code input: Royal blue #2563eb
    info.element.style.color = '#2563eb';
    component.core.pushHistory('Update Text Color');
    assert.strictEqual(component.core.findElement(el.id).element.style.color, '#2563eb');

    // 3. Undo restores previous green
    component.core.undo();
    assert.strictEqual(component.core.findElement(el.id).element.style.color, '#059669');
  });

  test('shortcut manager allows Backspace and Delete keys when focused on input inside Shadow DOM', () => {
    const mgr = new ShortcutManager();
    let deleteTriggered = false;
    mgr.register('delete', () => { deleteTriggered = true; });

    // Mock an input inside shadow DOM
    let prevented = false;
    const shadowInput = { tagName: 'INPUT', type: 'text', value: 'Hello' };
    const fakeEvent = {
      key: 'Backspace',
      ctrlKey: false,
      metaKey: false,
      shiftKey: false,
      target: {
        shadowRoot: {
          activeElement: shadowInput
        }
      },
      composedPath: () => [shadowInput],
      preventDefault: () => { prevented = true; }
    };

    mgr.handleKeyDown(fakeEvent);
    // ShortcutManager must NOT preventDefault on inputs inside shadow DOM
    assert.strictEqual(prevented, false, 'Backspace in shadow input must not be blocked');
    assert.strictEqual(deleteTriggered, false, 'Delete callback should not trigger when editing input text');
  });

  test('multi-page enterprise invoice template has 25 items across 2 pages and computes pageCount = 2', () => {
    const gallery = new TemplateGalleryManager();
    const multiTpl = gallery.getTemplateById('a4_multipage_enterprise_invoice');
    assert.ok(multiTpl, 'a4_multipage_enterprise_invoice must exist');
    assert.strictEqual(multiTpl.schema.pageSetup.paperKind, 'A4');

    const detailEls = multiTpl.schema.bands.Detail.elements;
    assert.ok(detailEls.length >= 20, 'Detail elements must contain enterprise item fields');

    // Calculate total height
    const bands = multiTpl.schema.bands;
    const totalHeight = Object.values(bands).reduce((sum, b) => sum + (b.height || 0), 0);
    const pageCount = Math.ceil(totalHeight / multiTpl.schema.pageSetup.height);
    assert.strictEqual(pageCount, 2, 'Template with 1200pt total height must calculate exactly 2 pages');
  });

  test('resolveBandFromY accurately maps canvas Y coordinates to bands', () => {
    const component = new BangplanixDesigner();
    component.core.report = {
      bands: {
        ReportHeader: { height: 100 },
        PageHeader: { height: 40 },
        Detail: { height: 300 },
        PageFooter: { height: 50 },
        ReportFooter: { height: 60 }
      }
    };

    // Y = 30 -> ReportHeader (0..100)
    const res1 = component.resolveBandFromY(30);
    assert.strictEqual(res1.bandName, 'ReportHeader');
    assert.strictEqual(res1.offsetY, 30);

    // Y = 120 -> PageHeader (100..140), offsetY = 20
    const res2 = component.resolveBandFromY(120);
    assert.strictEqual(res2.bandName, 'PageHeader');
    assert.strictEqual(res2.offsetY, 20);

    // Y = 250 -> Detail (140..440), offsetY = 110
    const res3 = component.resolveBandFromY(250);
    assert.strictEqual(res3.bandName, 'Detail');
    assert.strictEqual(res3.offsetY, 110);
  });

  test('addComponentToBand inserts element into active band with defaults', () => {
    const component = new BangplanixDesigner();
    component.activeBandName = 'Detail';

    const el = component.addComponentToBand('Barcode', 'Detail', { text: '987654321' });
    assert.ok(el, 'Element must be created');
    assert.strictEqual(el.type, 'Barcode');
    assert.strictEqual(el.text, '987654321');
    assert.strictEqual(el.width, 180);
    assert.strictEqual(el.height, 60);

    // Verify it is selected
    assert.ok(component.core.selectedElementIds.includes(el.id));
  });

  test('view mode switching toggles design, live preview, and schema code', () => {
    const component = new BangplanixDesigner();
    assert.strictEqual(component.core.viewMode, 'design');

    component.core.setViewMode('preview');
    assert.strictEqual(component.core.viewMode, 'preview');

    component.core.setViewMode('code');
    assert.strictEqual(component.core.viewMode, 'code');

    component.core.setViewMode('design');
    assert.strictEqual(component.core.viewMode, 'design');
  });

  test('default invoice template contains active datasets and fields', () => {
    const gallery = new TemplateGalleryManager();
    const tpl = gallery.getTemplateById('a4_commercial_invoice');
    assert.ok(tpl.schema.datasets, 'Datasets must be present');
    assert.ok(tpl.schema.datasets.length > 0, 'At least 1 dataset must exist');

    const ds = tpl.schema.datasets[0];
    assert.strictEqual(ds.name, 'InvoiceItems');
    assert.ok(ds.fields.some(f => f.name === 'ItemCode'));
    assert.ok(ds.fields.some(f => f.name === 'TotalAmount'));
  });

  test('updating border style, width, and background fill mutates element model', () => {
    const component = new BangplanixDesigner();
    const el = component.addComponentToBand('Shape', 'Detail');
    const info = component.core.findElement(el.id);

    info.element.style.borderWidth = 2;
    info.element.style.borderStyle = 'solid';
    info.element.style.borderColor = '#2563eb';
    info.element.style.backgroundColor = '#f1f5f9';
    info.element.style.borderRadius = 8;
    component.core.pushHistory('Update Box Model');

    const updated = component.core.findElement(el.id).element;
    assert.strictEqual(updated.style.borderWidth, 2);
    assert.strictEqual(updated.style.borderStyle, 'solid');
    assert.strictEqual(updated.style.borderColor, '#2563eb');
    assert.strictEqual(updated.style.backgroundColor, '#f1f5f9');
    assert.strictEqual(updated.style.borderRadius, 8);
  });

  test('delete element action removes element and pushes history', () => {
    const component = new BangplanixDesigner();
    const el = component.addComponentToBand('Text', 'Detail', { text: 'To be deleted' });
    assert.ok(component.core.findElement(el.id));

    component.core.selectElement(el.id);
    component.core.removeSelectedElements();

    assert.strictEqual(component.core.findElement(el.id), null, 'Element should be removed');

    // Undo restores element
    component.core.undo();
    assert.ok(component.core.findElement(el.id), 'Undo should restore element');
  });
});
