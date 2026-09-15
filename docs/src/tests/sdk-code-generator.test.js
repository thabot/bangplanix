import { test, describe } from 'node:test';
import assert from 'node:assert';
import { SdkCodeGenerator } from '../dist/designer/sdk-code-generator.js';

describe('Bangplanix Polyglot SDK Code Generator Tests', () => {
  const dummySchema = {
    version: '1.0',
    metadata: { title: 'Order' },
    pageSetup: { width: 612, height: 792 },
    bands: { Detail: { height: 30, elements: [] } }
  };

  test('should generate clean executable code in 5 languages', () => {
    const gen = new SdkCodeGenerator();

    const csharp = gen.generate('csharp', dummySchema);
    assert.ok(csharp.includes('BangplanixEngine.RenderAsync'));
    assert.ok(csharp.includes('using Bangplanix.Engine;'));

    const nodejs = gen.generate('nodejs', dummySchema);
    assert.ok(nodejs.includes('@bangplanix/client'));
    assert.ok(nodejs.includes('await client.render'));

    const python = gen.generate('python', dummySchema);
    assert.ok(python.includes('from bangplanix import Client'));
    assert.ok(python.includes('client.render(template'));

    const go = gen.generate('go', dummySchema);
    assert.ok(go.includes('bangplanix.NewClient'));
    assert.ok(go.includes('package main'));

    const java = gen.generate('java', dummySchema);
    assert.ok(java.includes('io.bangplanix.client.BangplanixClient'));
    assert.ok(java.includes('BangplanixClient.builder()'));
  });
});
