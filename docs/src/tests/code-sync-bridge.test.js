import { test, describe } from 'node:test';
import assert from 'node:assert';
import { CodeSyncBridge } from '../dist/designer/code-sync-bridge.js';

describe('Bangplanix 2-Way Code Sync Bridge Tests', () => {
  const sampleReport = {
    version: '1.0',
    metadata: { title: 'Invoice' },
    pageSetup: { width: 612, height: 792 },
    bands: {
      Detail: {
        height: 50,
        elements: [
          { id: 'el_1', type: 'Text', x: 20, y: 30, width: 100, height: 20, text: 'Hello' }
        ]
      }
    }
  };

  test('canvasToJson should serialize report correctly into JSON', () => {
    const bridge = new CodeSyncBridge();
    const json = bridge.canvasToJson(sampleReport);
    assert.ok(json.includes('"title": "Invoice"'));
    assert.ok(json.includes('"x": 20'));
  });

  test('jsonToCanvas should validate and parse valid JSON schema', () => {
    const bridge = new CodeSyncBridge();
    const validJson = JSON.stringify(sampleReport);
    const res = bridge.jsonToCanvas(validJson);
    assert.strictEqual(res.valid, true);
    assert.strictEqual(res.schema.metadata.title, 'Invoice');
    assert.strictEqual(res.schema.bands.Detail.elements[0].x, 20);
  });

  test('jsonToCanvas should strip markdown json blocks cleanly', () => {
    const bridge = new CodeSyncBridge();
    const mdJson = '```json\n' + JSON.stringify(sampleReport) + '\n```';
    const res = bridge.jsonToCanvas(mdJson);
    assert.strictEqual(res.valid, true);
    assert.strictEqual(res.schema.version, '1.0');
  });

  test('jsonToCanvas should reject invalid JSON and missing bands gracefully', () => {
    const bridge = new CodeSyncBridge();
    const badJson = '{ title: broken, }';
    const res1 = bridge.jsonToCanvas(badJson);
    assert.strictEqual(res1.valid, false);
    assert.ok(res1.error);

    const missingBands = '{"version":"1.0"}';
    const res2 = bridge.jsonToCanvas(missingBands);
    assert.strictEqual(res2.valid, false);
    assert.ok(res2.error.includes('Missing required "bands"'));
  });
});
