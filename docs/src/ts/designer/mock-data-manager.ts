/**
 * Mock Data Scenario Switcher for Bangplanix Designer
 * Allows developers to test multi-page pagination, large orders, and edge cases
 */
import { MockScenario } from '../core/types.js';

export const MOCK_SCENARIOS: MockScenario[] = [
  {
    id: 'standard_order',
    title: '1. Standard Order (Single Page)',
    description: 'Clean 3-item order fitting comfortably within a single US Letter page.',
    data: {
      InvoiceNo: 'INV-2026-8801',
      Customer: 'Acme Corporation',
      items: [
        { name: 'Bangplanix Developer Pro Annual License', qty: 1, rate: 699.00, amount: 699.00 },
        { name: 'Priority Integration Support Pack', qty: 2, rate: 250.00, amount: 500.00 },
        { name: 'Custom ZPL Thermal Driver Setup', qty: 1, rate: 350.00, amount: 350.00 }
      ],
      subtotal: 1549.00,
      tax: 0.00,
      total: 1549.00
    }
  },
  {
    id: 'multipage_enterprise',
    title: '2. Multi-Page Enterprise (50+ Line Items)',
    description: 'Stress-test automatic pagination, running totals, and repeated page headers.',
    data: {
      InvoiceNo: 'INV-2026-9900',
      Customer: 'Global Megacorp Logistics LLC',
      items: Array.from({ length: 50 }, (_, i) => ({
        name: `Industrial IoT Sensor Node Unit #${1000 + i}`,
        qty: (i % 5) + 1,
        rate: 89.50,
        amount: ((i % 5) + 1) * 89.50
      })),
      subtotal: 13425.00,
      tax: 1141.12,
      total: 14566.12
    }
  },
  {
    id: 'us_tax_exempt',
    title: '3. US Tax-Exempt / Government Order',
    description: 'Government contract with 501(c)(3) Federal Tax Exemption certificate.',
    data: {
      InvoiceNo: 'INV-GOV-2026',
      Customer: 'State University Research Department',
      TaxExemptID: 'EXEMPT-948102',
      items: [
        { name: 'Bangplanix Enterprise Sovereign Perpetual Site License', qty: 1, rate: 1999.00, amount: 1999.00 }
      ],
      subtotal: 1999.00,
      tax: 0.00,
      total: 1999.00
    }
  }
];

export class MockDataManager {
  getScenarios(): MockScenario[] {
    return MOCK_SCENARIOS;
  }

  getScenarioById(id: string): MockScenario | undefined {
    return MOCK_SCENARIOS.find(s => s.id === id);
  }
}
