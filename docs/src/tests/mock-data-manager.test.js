import { test, describe } from 'node:test';
import assert from 'node:assert';
import { MockDataManager, MOCK_SCENARIOS } from '../dist/designer/mock-data-manager.js';

describe('Bangplanix Mock Data Manager Tests', () => {
  test('should provide 3 essential enterprise testing scenarios', () => {
    const mgr = new MockDataManager();
    const scenarios = mgr.getScenarios();
    assert.strictEqual(scenarios.length, 3);

    const multipage = mgr.getScenarioById('multipage_enterprise');
    assert.ok(multipage !== undefined);
    assert.strictEqual(multipage.data.items.length, 50);
  });
});
