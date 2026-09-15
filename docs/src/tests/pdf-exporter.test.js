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
});
