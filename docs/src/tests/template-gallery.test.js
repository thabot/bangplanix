import { test, describe } from 'node:test';
import assert from 'node:assert';
import { US_PRESET_TEMPLATES, TemplateGalleryManager } from '../dist/designer/template-gallery.js';

describe('Bangplanix US Standards Template Gallery Tests', () => {
  test('should provide exactly 5 US standard templates in English', () => {
    const mgr = new TemplateGalleryManager();
    const templates = mgr.getTemplates();
    assert.strictEqual(templates.length, 5);

    const ids = templates.map(t => t.id);
    assert.ok(ids.includes('us_commercial_invoice'));
    assert.ok(ids.includes('usps_shipping_label'));
    assert.ok(ids.includes('us_pos_receipt'));
    assert.ok(ids.includes('us_payroll_paystub'));
    assert.ok(ids.includes('executive_financial_summary'));
  });

  test('all templates must strictly adhere to US dimensions and bands schema', () => {
    US_PRESET_TEMPLATES.forEach(t => {
      assert.ok(t.schema.version);
      assert.ok(t.schema.pageSetup);
      assert.ok(t.schema.bands);
      assert.ok(t.schema.bands.Detail, `Template ${t.id} must contain Detail band`);
      assert.ok(['Letter', 'Custom'].includes(t.schema.pageSetup.paperKind));
    });
  });

  test('getTemplateById should return the requested template accurately', () => {
    const mgr = new TemplateGalleryManager();
    const invoice = mgr.getTemplateById('us_commercial_invoice');
    assert.ok(invoice !== undefined);
    assert.strictEqual(invoice.name, 'US Commercial Tax Invoice');
    assert.strictEqual(invoice.category, 'Billing');
  });
});
