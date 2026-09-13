import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import { BangplanixWasmEngine } from '../src/wasm-engine.js';

describe('Bangplanix WASM In-Browser Engine Tests', () => {
  const engine = new BangplanixWasmEngine();

  const sampleBpx = {
    version: '1.0.0',
    metadata: { title: 'Test Invoice' },
    pageSetup: {
      paperKind: 'A4',
      orientation: 'Portrait',
      margins: { top: 20, bottom: 20, left: 20, right: 20 }
    },
    parameters: [
      { name: 'CustomerName', defaultValue: 'Acme Corp' }
    ],
    bands: {
      title: {
        height: 50,
        elements: [
          { type: 'text', bounds: { x: 0, y: 0, width: 200, height: 20 }, content: 'TAX INVOICE', fontSize: 16, fontWeight: 'Bold' },
          { type: 'text', bounds: { x: 0, y: 25, width: 200, height: 20 }, expression: '=Parameters!CustomerName.Value' }
        ]
      },
      detail: {
        dataset: 'items',
        height: 25,
        elements: [
          { type: 'text', bounds: { x: 0, y: 0, width: 150, height: 20 }, expression: '=Fields!name.Value' },
          { type: 'text', bounds: { x: 160, y: 0, width: 80, height: 20 }, expression: '=Fields!price.Value', format: 'N2', textAlign: 'Right' }
        ]
      },
      pageFooter: {
        height: 30,
        elements: [
          { type: 'barcode', bounds: { x: 0, y: 0, width: 100, height: 30 }, content: '12345678', symbology: 'Code128' }
        ]
      }
    }
  };

  test('parseTemplate should parse valid JSON string and objects', () => {
    const parsedObj = engine.parseTemplate(sampleBpx);
    assert.equal(parsedObj.version, '1.0.0');

    const parsedJson = engine.parseTemplate(JSON.stringify(sampleBpx));
    assert.equal(parsedJson.version, '1.0.0');

    assert.throws(() => engine.parseTemplate('invalid json {'), /Invalid \.bpx JSON/);
  });

  test('evaluateExpression should evaluate parameters, fields and globals correctly', () => {
    const context = {
      Parameters: { CustomerName: 'John Doe' },
      Fields: { amount: 1500 },
      Globals: { PageNumber: 1 }
    };

    assert.equal(engine.evaluateExpression('=Parameters!CustomerName.Value', context), 'John Doe');
    assert.equal(engine.evaluateExpression('=Fields!amount.Value * 2', context), 3000);
    assert.equal(engine.evaluateExpression('Static Text', context), 'Static Text');
  });

  test('renderSvg should render complete SVG document with bands and rows', () => {
    const data = {
      items: [
        { name: 'Cloud Server Hosting', price: 1200 },
        { name: 'SSL Certificate', price: 300 }
      ]
    };

    const svg = engine.renderSvg(sampleBpx, data);

    assert.ok(svg.includes('<svg class="bangplanix-report-svg"'));
    assert.ok(svg.includes('TAX INVOICE'));
    assert.ok(svg.includes('Acme Corp'));
    assert.ok(svg.includes('Cloud Server Hosting'));
    assert.ok(svg.includes('SSL Certificate'));
    assert.ok(svg.includes('Code128'));
    assert.ok(svg.includes('1,200.00'));
  });

  test('renderSvg should respect custom parameters override', () => {
    const svg = engine.renderSvg(sampleBpx, {}, { CustomerName: 'Custom Enterprise Inc.' });
    assert.ok(svg.includes('Custom Enterprise Inc.'));
  });

  test('renderSvg with empty data should still render header and footer cleanly', () => {
    const svg = engine.renderSvg(sampleBpx, { items: [] });
    assert.ok(svg.includes('<svg class="bangplanix-report-svg"'));
    assert.ok(svg.includes('TAX INVOICE'));
    assert.ok(svg.includes('12345678'));
  });
});
