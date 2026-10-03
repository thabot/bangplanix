import { test, describe } from 'node:test';
import assert from 'node:assert';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const docsDir = path.resolve(__dirname, '../../');

describe('Bangplanix Global Navigation & Cross-Linking Integrity Tests', () => {
  test('index.html (Demo Playground) contains all essential cross-page navigation links', () => {
    const indexPath = path.join(docsDir, 'index.html');
    const content = fs.readFileSync(indexPath, 'utf-8');

    assert.ok(content.includes('href="index.html"'), 'Must link to Demo Playground');
    assert.ok(content.includes('href="designer.html"'), 'Must link to Visual Designer');
    assert.ok(content.includes('href="pricing.html"'), 'Must link to Pricing & Buy License');
    assert.ok(content.includes('href="https://bangplanix.95459654.xyz/lookup"'), 'Must link to License Lookup Portal');
  });

  test('designer.html (Visual Designer) contains all essential cross-page navigation links', () => {
    const designerPath = path.join(docsDir, 'designer.html');
    const content = fs.readFileSync(designerPath, 'utf-8');

    assert.ok(content.includes('href="index.html"'), 'Must link to Demo Playground');
    assert.ok(content.includes('href="designer.html"'), 'Must link to Visual Designer');
    assert.ok(content.includes('href="pricing.html"'), 'Must link to Pricing & Buy License');
    assert.ok(content.includes('href="https://bangplanix.95459654.xyz/lookup"'), 'Must link to License Lookup Portal');
  });

  test('pricing.html (Pricing & Buy) contains all essential cross-page navigation links and checkout triggers', () => {
    const pricingPath = path.join(docsDir, 'pricing.html');
    const content = fs.readFileSync(pricingPath, 'utf-8');

    assert.ok(content.includes('href="index.html"'), 'Must link to Demo Playground');
    assert.ok(content.includes('href="designer.html"'), 'Must link to Visual Designer');
    assert.ok(content.includes('href="pricing.html"'), 'Must link to Pricing & Buy License');
    assert.ok(content.includes('href="https://bangplanix.95459654.xyz/lookup"'), 'Must link to License Lookup Portal');
    assert.ok(content.includes('openModal'), 'Must support online checkout modal');
    assert.ok(content.includes('createCheckoutSession'), 'Must connect to checkout session creation');
  });
});
