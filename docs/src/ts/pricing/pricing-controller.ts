/**
 * Bangplanix Commercial Pricing Controller
 * Implements Head-to-Head QuestPDF Pricing:
 * Community ($0), Pro ($699/yr, $59/mo), Enterprise ($1,999/yr, $199/mo), OEM ($3,999/yr)
 */
import { PricingTierData } from '../core/types.js';

export const PRICING_TIERS: PricingTierData[] = [
  {
    id: 'community',
    name: 'Community',
    tagline: 'For developers, indie hackers & businesses with < $1M annual revenue.',
    priceAnnualUsd: 0,
    priceMonthlyUsd: 0,
    priceAnnualThb: 0,
    priceMonthlyThb: 0,
    coresQuota: 4,
    features: [
      'Unwatermarked 100% Production Output',
      'Web Visual Designer (<bangplanix-designer>)',
      'Sub-millisecond Native AOT Server',
      'All 5 SDKs (C#, Node.js, Python, Go, Java)',
      'HarfBuzz Thai & Global Complex Typography',
      'MiniExcel XLSX Big Data Connectors',
      'Up to 4 CPU Cores',
      'Community GitHub Discussions'
    ],
    checkoutUrlAnnual: 'https://github.com/thabot/bangplanix',
    checkoutUrlMonthly: 'https://github.com/thabot/bangplanix'
  },
  {
    id: 'professional',
    name: 'Professional',
    tagline: 'For high-velocity engineering teams requiring priority bursting.',
    priceAnnualUsd: 699,
    priceMonthlyUsd: 59,
    priceAnnualThb: 24500,
    priceMonthlyThb: 2050,
    coresQuota: 16,
    isPopular: true,
    features: [
      'Everything in Community Edition',
      'Up to 16 CPU Cores / Pods',
      'Scheduled Multi-Channel Report Bursting',
      'Direct S3 / MinIO / SFTP Object Delivery',
      'Commercial Non-Open-Source Indemnity',
      'Royalty-Free Runtime Redistribution',
      'Email Support (24-hour SLA)'
    ],
    checkoutUrlAnnual: 'https://bangplanix.lemonsqueezy.com/checkout/buy/99f67d3d-d04f-40de-be6f-a28c06c6624e?discount=0',
    checkoutUrlMonthly: 'https://bangplanix.lemonsqueezy.com/checkout/buy/99f67d3d-d04f-40de-be6f-a28c06c6624e?discount=0'
  },
  {
    id: 'enterprise',
    name: 'Enterprise Sovereign',
    tagline: 'For fintech, healthcare, banking & cloud platforms needing e-Tax and PAdES.',
    priceAnnualUsd: 1999,
    priceMonthlyUsd: 199,
    priceAnnualThb: 70000,
    priceMonthlyThb: 6950,
    coresQuota: 'Unlimited',
    features: [
      'Everything in Professional',
      'Unlimited CPU Cores & Kubernetes Pods',
      'Thai e-Tax Invoice & RFC 3161 TSA Timestamps',
      'PKCS#7 PAdES Cryptographic Signatures',
      'True Vector Redaction (Sanitization)',
      'Legacy SSRS & Crystal Reports Migration',
      'Formal Thai Tax Invoice (ภ.พ.20 & WHT 3%)',
      'Dedicated Slack / Teams Channel (4-hour SLA)'
    ],
    checkoutUrlAnnual: 'https://bangplanix.lemonsqueezy.com/checkout/buy/e585b5a6-a630-4eeb-ab80-db0553bf35b4',
    checkoutUrlMonthly: 'https://bangplanix.lemonsqueezy.com/checkout/buy/e585b5a6-a630-4eeb-ab80-db0553bf35b4'
  },
  {
    id: 'oem',
    name: 'OEM Sovereign',
    tagline: 'For commercial software vendors (ISVs), defense & air-gapped appliances.',
    priceAnnualUsd: 3999,
    priceMonthlyUsd: 399,
    priceAnnualThb: 140000,
    priceMonthlyThb: 14000,
    coresQuota: 'Unlimited',
    features: [
      'Everything in Enterprise Sovereign',
      'White-Label Unbranded Embedding Rights',
      '100% Offline Air-Gapped Verification',
      'NIST FIPS 204 ML-DSA-65 (Dilithium) Quantum Signature',
      'Source Code Escrow Agreement Option',
      'Direct Core Engineering Escalation (1-hour SLA)'
    ],
    checkoutUrlAnnual: 'mailto:thabot47@gmail.com?subject=Bangplanix%20OEM%20Licensing%20Inquiry',
    checkoutUrlMonthly: 'mailto:thabot47@gmail.com?subject=Bangplanix%20OEM%20Licensing%20Inquiry'
  }
];

export class BangplanixPricingController {
  private billingCycle: 'annual' | 'monthly' = 'annual';

  setBillingCycle(cycle: 'annual' | 'monthly'): void {
    this.billingCycle = cycle;
  }

  getBillingCycle(): 'annual' | 'monthly' {
    return this.billingCycle;
  }

  getTiers(): PricingTierData[] {
    return PRICING_TIERS;
  }

  getDisplayPrice(tier: PricingTierData): { usd: number; thb: number; period: string } {
    if (this.billingCycle === 'annual') {
      return {
        usd: tier.priceAnnualUsd,
        thb: tier.priceAnnualThb,
        period: '/ year'
      };
    } else {
      return {
        usd: tier.priceMonthlyUsd,
        thb: tier.priceMonthlyThb,
        period: '/ month'
      };
    }
  }
}
