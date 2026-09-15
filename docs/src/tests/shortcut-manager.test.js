import { test, describe } from 'node:test';
import assert from 'node:assert';
import { ShortcutManager } from '../dist/designer/shortcut-manager.js';

describe('Bangplanix Keyboard Shortcuts Manager Tests', () => {
  test('should trigger registered callbacks upon matching key events', () => {
    const mgr = new ShortcutManager();
    let saved = false;
    let undone = false;

    mgr.register('save', () => { saved = true; });
    mgr.register('undo', () => { undone = true; });

    // Mock Ctrl+S
    const sEvent = { key: 's', ctrlKey: true, metaKey: false, shiftKey: false, preventDefault: () => {} };
    mgr.handleKeyDown(sEvent);
    assert.strictEqual(saved, true);

    // Mock Ctrl+Z
    const zEvent = { key: 'z', ctrlKey: true, metaKey: false, shiftKey: false, preventDefault: () => {} };
    mgr.handleKeyDown(zEvent);
    assert.strictEqual(undone, true);
  });
});
