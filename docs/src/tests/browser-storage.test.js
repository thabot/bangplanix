import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BrowserStorageRepository } from '../dist/core/browser-storage.js';

describe('Bangplanix BrowserStorage Repository Tests', () => {
  const dummySchema = {
    version: '1.0',
    metadata: { title: 'Test Invoice' },
    pageSetup: { width: 612, height: 792 },
    bands: {
      Detail: { height: 40, elements: [] }
    }
  };

  test('should fallback to in-memory store in Node.js environment', () => {
    const storage = new BrowserStorageRepository('test_node:');
    storage.setItem('key1', 'val1');
    assert.strictEqual(storage.getItem('key1'), 'val1');

    storage.removeItem('key1');
    assert.strictEqual(storage.getItem('key1'), null);
  });

  test('should auto-save and restore draft successfully', () => {
    const storage = new BrowserStorageRepository('test_draft:');
    const ok = storage.autoSaveDraft(dummySchema);
    assert.strictEqual(ok, true);

    const draft = storage.loadDraft();
    assert.ok(draft !== null);
    assert.strictEqual(draft.name, 'Test Invoice');
    assert.strictEqual(draft.schema.version, '1.0');

    storage.clearDraft();
    assert.strictEqual(storage.loadDraft(), null);
  });

  test('should manage named template catalog (save, list, delete)', () => {
    const storage = new BrowserStorageRepository('test_catalog:');
    assert.deepStrictEqual(storage.getTemplateList(), []);

    storage.saveNamedTemplate('Invoice v1', dummySchema);
    const list = storage.getTemplateList();
    assert.strictEqual(list.length, 1);
    assert.strictEqual(list[0].name, 'Invoice v1');

    const item = storage.loadNamedTemplate(list[0].id);
    assert.ok(item !== null);
    assert.strictEqual(item.name, 'Invoice v1');

    storage.deleteNamedTemplate(list[0].id);
    assert.strictEqual(storage.getTemplateList().length, 0);
  });
});
