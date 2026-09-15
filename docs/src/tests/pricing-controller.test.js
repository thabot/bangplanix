import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BangplanixPricingController, PRICING_TIERS } from '../dist/pricing/pricing-controller.js';

describe('Bangplanix Pricing Controller & QuestPDF Parity Tests', () => {
  test('should provide exactly 4 tiers matching QuestPDF price points', () => {
    const ctrl = new BangplanixPricingController();
    const tiers = ctrl.getTiers();
    assert.strictEqual(tiers.length, 4);

    const pro = tiers.find(t => t.id === 'professional');
    assert.strictEqual(pro.priceAnnualUsd, 699);
    assert.strictEqual(pro.priceMonthlyUsd, 59);

    const ent = tiers.find(t => t.id === 'enterprise');
    assert.strictEqual(ent.priceAnnualUsd, 1999);
    assert.strictEqual(ent.priceMonthlyUsd, 199);

    const oem = tiers.find(t => t.id === 'oem');
    assert.strictEqual(oem.priceAnnualUsd, 3999);
  });

  test('billing toggle should accurately alternate between annual and monthly display', () => {
    const ctrl = new BangplanixPricingController();
    const pro = ctrl.getTiers().find(t => t.id === 'professional');

    // Default Annual
    const annualPrice = ctrl.getDisplayPrice(pro);
    assert.strictEqual(annualPrice.usd, 699);
    assert.strictEqual(annualPrice.period, '/ year');

    // Switch Monthly
    ctrl.setBillingCycle('monthly');
    const monthlyPrice = ctrl.getDisplayPrice(pro);
    assert.strictEqual(monthlyPrice.usd, 59);
    assert.strictEqual(monthlyPrice.period, '/ month');
  });
});
