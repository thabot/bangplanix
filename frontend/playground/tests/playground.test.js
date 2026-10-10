import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import { BangplanixWasmEngine } from '../src/wasm-engine.js';
import { BangplanixPlayground } from '../src/playground.js';

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

  describe('Bangplanix Playground Client-Side Document Exporters', () => {
    test('generateXlsxDocument should produce a valid OpenXML spreadsheet ZIP archive', () => {
      const headers = ['Item Description', 'Qty', 'Unit Price', 'Total'];
      const rows = [
        { 'Item Description': 'Cloud License', 'Qty': 2, 'Unit Price': 15000, 'Total': 30000 },
        { 'Item Description': 'Support SLA', 'Qty': 1, 'Unit Price': 5000, 'Total': 5000 }
      ];

      const xlsxBytes = BangplanixPlayground.generateXlsxDocument('Tax Invoice', headers, rows);

      assert.ok(xlsxBytes instanceof Uint8Array);
      assert.ok(xlsxBytes.length > 500);

      // Verify ZIP standard signature 0x50 0x4B 0x03 0x04 ('PK\x03\x04')
      assert.equal(xlsxBytes[0], 0x50);
      assert.equal(xlsxBytes[1], 0x4B);
      assert.equal(xlsxBytes[2], 0x03);
      assert.equal(xlsxBytes[3], 0x04);

      // Convert to string to check OpenXML contents
      const decoded = new TextDecoder().decode(xlsxBytes);
      assert.ok(decoded.includes('[Content_Types].xml'));
      assert.ok(decoded.includes('xl/workbook.xml'));
      assert.ok(decoded.includes('xl/worksheets/sheet1.xml'));
      assert.ok(decoded.includes('Cloud License'));
      assert.ok(decoded.includes('30000'));
    });

    test('generatePdfDocument should produce a compliant PDF 1.4 binary stream', () => {
      const title = 'Commercial Tax Invoice';
      const textLines = [
        'Customer: Acme Enterprise Ltd.',
        'Invoice No: INV-2026-0901',
        'Total Amount: 35,000.00 THB'
      ];

      const pdfBytes = BangplanixPlayground.generatePdfDocument(title, textLines, 595.28, 841.89);

      assert.ok(pdfBytes instanceof Uint8Array);
      assert.ok(pdfBytes.length > 200);

      const decoded = new TextDecoder().decode(pdfBytes);
      // Verify PDF 1.4 header
      assert.ok(decoded.startsWith('%PDF-1.4'));
      // Verify PDF structure elements
      assert.ok(decoded.includes('/Type /Catalog'));
      assert.ok(decoded.includes('/Type /Pages'));
      assert.ok(decoded.includes('/Type /Page'));
      assert.ok(decoded.includes('/Type /Font'));
      assert.ok(decoded.includes('/BaseFont /Helvetica'));
      assert.ok(decoded.includes('Commercial Tax Invoice'));
      assert.ok(decoded.includes('INV-2026-0901'));
      assert.ok(decoded.includes('xref'));
      assert.ok(decoded.includes('trailer'));
      assert.ok(decoded.includes('startxref'));
      assert.ok(decoded.endsWith('%%EOF\n') || decoded.includes('%%EOF'));
    });

    test('generateCsvDocument should format tabular data into standard quoted CSV', () => {
      const headers = ['SKU', 'Product Name', 'Price'];
      const rows = [
        { SKU: 'PROD-01', 'Product Name': 'High-Speed Engine "Pro"', Price: 45000 },
        { SKU: 'PROD-02', 'Product Name': 'Support SLA', Price: 5000 }
      ];

      const csvBytes = BangplanixPlayground.generateCsvDocument(headers, rows);
      assert.ok(csvBytes instanceof Uint8Array);

      const decoded = new TextDecoder().decode(csvBytes);
      assert.ok(decoded.includes('"SKU","Product Name","Price"'));
      assert.ok(decoded.includes('"PROD-01","High-Speed Engine ""Pro""","45000"'));
      assert.ok(decoded.includes('"PROD-02","Support SLA","5000"'));
    });

    test('generateJsonDocument should format clean JSON data stream with metadata and timestamps', () => {
      const template = { metadata: { title: 'POS Receipt' }, parameters: [{ name: 'Store', defaultValue: 'Main' }] };
      const rows = [{ item: 'Espresso', qty: 2, total: 180 }];

      const jsonBytes = BangplanixPlayground.generateJsonDocument('POS Receipt', template, rows);
      assert.ok(jsonBytes instanceof Uint8Array);

      const parsed = JSON.parse(new TextDecoder().decode(jsonBytes));
      assert.equal(parsed.title, 'POS Receipt');
      assert.equal(parsed.metadata.title, 'POS Receipt');
      assert.equal(parsed.datasets.items[0].item, 'Espresso');
      assert.ok(parsed.exportedAt);
    });

    test('generateHtmlDocument should wrap report SVG in a valid standalone HTML5 shell', () => {
      const title = 'Commercial Invoice Report';
      const svg = '<svg><text>Sample Invoice Output</text></svg>';

      const htmlBytes = BangplanixPlayground.generateHtmlDocument(title, svg);
      assert.ok(htmlBytes instanceof Uint8Array);

      const decoded = new TextDecoder().decode(htmlBytes);
      assert.ok(decoded.includes('<!DOCTYPE html>'));
      assert.ok(decoded.includes('<title>Commercial Invoice Report</title>'));
      assert.ok(decoded.includes('<svg><text>Sample Invoice Output</text></svg>'));
    });
  });
});


