import { test, describe } from 'node:test';
import assert from 'node:assert';
import { BangplanixEventBus } from '../dist/core/event-bus.js';

describe('Bangplanix Event Bus Tests', () => {
  test('should subscribe and receive published events', () => {
    const bus = new BangplanixEventBus();
    let received = null;

    bus.subscribe('test:event', (data) => {
      received = data;
    });

    bus.publish('test:event', { foo: 'bar' });
    assert.deepStrictEqual(received, { foo: 'bar' });
  });

  test('should allow unsubscribing cleanly', () => {
    const bus = new BangplanixEventBus();
    let callCount = 0;

    const unsub = bus.subscribe('counter', () => {
      callCount++;
    });

    bus.publish('counter', null);
    assert.strictEqual(callCount, 1);

    unsub();
    bus.publish('counter', null);
    assert.strictEqual(callCount, 1);
  });

  test('should handle errors in handlers gracefully without breaking bus', () => {
    const bus = new BangplanixEventBus();
    let normalHandlerRan = false;

    bus.subscribe('error:test', () => {
      throw new Error('Explosion');
    });

    bus.subscribe('error:test', () => {
      normalHandlerRan = true;
    });

    assert.doesNotThrow(() => {
      bus.publish('error:test', {});
    });
    assert.strictEqual(normalHandlerRan, true);
  });
});
