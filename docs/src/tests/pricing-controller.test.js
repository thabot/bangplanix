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

  test('multi-language translation support should cover all 5 documentation languages', () => {
    const ctrl = new BangplanixPricingController();
    const pro = ctrl.getTiers().find(t => t.id === 'professional');

    // 1. English (default)
    assert.strictEqual(ctrl.getLanguage(), 'en');
    let t = ctrl.getTranslation();
    assert.ok(t.heroTitle.includes('Head-to-Head'));
    assert.strictEqual(t.perYear, '/ year');
    assert.strictEqual(ctrl.getDisplayPrice(pro).period, '/ year');

    // 2. Thai
    ctrl.setLanguage('th');
    assert.strictEqual(ctrl.getLanguage(), 'th');
    t = ctrl.getTranslation();
    assert.ok(t.heroTitle.includes('ความคุ้มค่า'));
    assert.strictEqual(t.perYear, '/ ปี');
    assert.strictEqual(ctrl.getDisplayPrice(pro).period, '/ ปี');

    // 3. Simplified Chinese
    ctrl.setLanguage('zh');
    assert.strictEqual(ctrl.getLanguage(), 'zh');
    t = ctrl.getTranslation();
    assert.ok(t.heroTitle.includes('企业级性价比'));
    assert.strictEqual(t.perYear, '/ 年');
    assert.strictEqual(ctrl.getDisplayPrice(pro).period, '/ 年');

    // 4. Japanese
    ctrl.setLanguage('ja');
    assert.strictEqual(ctrl.getLanguage(), 'ja');
    t = ctrl.getTranslation();
    assert.ok(t.heroTitle.includes('QuestPDF に匹敵する'));
    assert.strictEqual(t.perYear, '/ 年');
    assert.strictEqual(ctrl.getDisplayPrice(pro).period, '/ 年');

    // 5. Spanish
    ctrl.setLanguage('es');
    assert.strictEqual(ctrl.getLanguage(), 'es');
    t = ctrl.getTranslation();
    assert.ok(t.heroTitle.includes('Valor Empresarial'));
    assert.strictEqual(t.perYear, '/ año');
    assert.strictEqual(ctrl.getDisplayPrice(pro).period, '/ año');
  });
});

