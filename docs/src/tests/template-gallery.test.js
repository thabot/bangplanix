import { test, describe } from 'node:test';
import assert from 'node:assert';
import { US_PRESET_TEMPLATES, TemplateGalleryManager } from '../dist/designer/template-gallery.js';

describe('Bangplanix Standard & US Standards Template Gallery Tests', () => {
  test('should provide standard templates including international A4 and US formats', () => {
    const mgr = new TemplateGalleryManager();
    const templates = mgr.getTemplates();
    assert.strictEqual(templates.length, 6);

    const ids = templates.map(t => t.id);
    assert.ok(ids.includes('a4_commercial_invoice'));
    assert.ok(ids.includes('us_commercial_invoice'));
    assert.ok(ids.includes('usps_shipping_label'));
    assert.ok(ids.includes('us_pos_receipt'));
    assert.ok(ids.includes('us_payroll_paystub'));
    assert.ok(ids.includes('executive_financial_summary'));
  });

  test('all templates must strictly adhere to dimensions and bands schema', () => {
    US_PRESET_TEMPLATES.forEach(t => {
      assert.ok(t.schema.version);
      assert.ok(t.schema.pageSetup);
      assert.ok(t.schema.bands);
      assert.ok(t.schema.bands.Detail, `Template ${t.id} must contain Detail band`);
      assert.ok(['A4', 'Letter', 'Custom'].includes(t.schema.pageSetup.paperKind));
    });
  });

  test('getTemplateById should return the requested template accurately', () => {
    const mgr = new TemplateGalleryManager();
    const a4 = mgr.getTemplateById('a4_commercial_invoice');
    assert.ok(a4 !== undefined);
    assert.strictEqual(a4.name, 'A4 Commercial Tax Invoice');
    assert.strictEqual(a4.category, 'Billing');
    assert.strictEqual(a4.schema.pageSetup.paperKind, 'A4');

    const invoice = mgr.getTemplateById('us_commercial_invoice');
    assert.ok(invoice !== undefined);
    assert.strictEqual(invoice.name, 'US Commercial Tax Invoice');
    assert.strictEqual(invoice.category, 'Billing');
  });
});
