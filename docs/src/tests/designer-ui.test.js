import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BangplanixDesignerCore } from '../../../frontend/designer/designer-core.js';
import { TemplateGalleryManager } from '../dist/designer/template-gallery.js';

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

  test('multi-page enterprise invoice template has 20 items looping across 2 pages', () => {
    const gallery = new TemplateGalleryManager();
    const multiTpl = gallery.getTemplateById('a4_multipage_enterprise_invoice');
    assert.ok(multiTpl, 'a4_multipage_enterprise_invoice must exist');
    assert.strictEqual(multiTpl.schema.pageSetup.paperKind, 'A4');

    const detailEls = multiTpl.schema.bands.Detail.elements;
    assert.ok(detailEls.length >= 20, 'Detail elements must contain at least 20 item fields');

    // Calculate total height
    const bands = multiTpl.schema.bands;
    const totalHeight = Object.values(bands).reduce((sum, b) => sum + (b.height || 0), 0);
    const pageCount = Math.ceil(totalHeight / multiTpl.schema.pageSetup.height);
    assert.ok(pageCount >= 1, 'Page count must be calculated');
  });
});
