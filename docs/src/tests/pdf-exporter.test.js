import { test, describe } from 'node:test';
import assert from 'node:assert';
import { InstantPdfExporter } from '../dist/designer/pdf-exporter.js';

describe('Bangplanix Instant PDF Exporter Tests', () => {
  const dummySchema = {
    version: '1.0',
    metadata: { title: 'Executive Report 2026' },
    pageSetup: { width: 612, height: 792 },
    bands: { Detail: { height: 50, elements: [] } }
  };

  test('should safely handle Node.js headless environment without crashing', () => {
    const exporter = new InstantPdfExporter();
    const res = exporter.exportPdf(dummySchema, 'test.pdf');
    assert.strictEqual(typeof res.success, 'boolean');
    assert.strictEqual(res.filename, 'test.pdf');
  });

  test('generatePrintHtml should generate complete printable document with accurate vector bands and styles', () => {
    const exporter = new InstantPdfExporter();
    const richReport = {
      version: '1.0',
      metadata: { title: 'Tax Invoice A4' },
      pageSetup: { width: 595.28, height: 841.89, paperKind: 'A4' },
      bands: {
        ReportHeader: {
          height: 60,
          elements: [
            { id: 't1', type: 'Text', x: 0, y: 10, width: 300, height: 25, text: 'BANGPLANIX CORP', style: { fontSize: 16, fontWeight: 'bold', color: '#2563eb' } }
          ]
        },
        Detail: {
          height: 30,
          elements: [
            { id: 'd1', type: 'Text', x: 0, y: 5, width: 200, height: 20, text: 'Item 1 Description', style: { fontSize: 11 } },
            { id: 'b1', type: 'Barcode', x: 210, y: 5, width: 100, height: 25, text: 'INV-12345' }
          ]
        }
      }
    };

    const html = exporter.generatePrintHtml(richReport);
    assert.ok(html.includes('<!DOCTYPE html>'));
    assert.ok(html.includes('@page'));
    assert.ok(html.includes('A4 portrait'));
    assert.ok(html.includes('BANGPLANIX CORP'));
    assert.ok(html.includes('Item 1 Description'));
    assert.ok(html.includes('<svg')); // Barcode SVG vector
    assert.ok(html.includes('INV-12345'));
    assert.ok(html.includes('window.print()'));
  });
});
