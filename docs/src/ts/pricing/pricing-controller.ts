/**
 * Bangplanix Commercial Pricing Controller
 * Implements Head-to-Head QuestPDF Pricing:
 * Community ($0), Pro ($699/yr, $59/mo), Enterprise ($1,999/yr, $199/mo), OEM ($3,999/yr)
 */
import { PricingTierData } from '../core/types.js';
import { PRICING_TRANSLATIONS, PricingTranslation } from './pricing-translations.js';

export { PRICING_TRANSLATIONS, PricingTranslation };

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
    checkoutUrlAnnual: '#checkout-pro',
    checkoutUrlMonthly: '#checkout-pro'
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
    checkoutUrlAnnual: '#checkout-enterprise',
    checkoutUrlMonthly: '#checkout-enterprise'
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

export type SupportedLanguage = 'en' | 'th' | 'zh' | 'ja' | 'es';

export interface CheckoutResult {
  success: boolean;
  checkoutUrl?: string;
  invoiceId?: string;
  paymentId?: string | number;
  paymentRecordId?: string;
  payAddress?: string;
  payAmount?: number;
  payCurrency?: string;
  amountUSD?: number;
  currency?: string;
  error?: string;
}

export class BangplanixPricingController {
  private billingCycle: 'annual' | 'monthly' = 'annual';
  private currentLanguage: SupportedLanguage = 'en';

  setBillingCycle(cycle: 'annual' | 'monthly'): void {
    this.billingCycle = cycle;
  }

  getBillingCycle(): 'annual' | 'monthly' {
    return this.billingCycle;
  }

  setLanguage(lang: SupportedLanguage): void {
    if (PRICING_TRANSLATIONS[lang]) {
      this.currentLanguage = lang;
    }
  }

  getLanguage(): SupportedLanguage {
    return this.currentLanguage;
  }

  getTranslation(lang?: SupportedLanguage): PricingTranslation {
    const target = lang || this.currentLanguage;
    return PRICING_TRANSLATIONS[target] || PRICING_TRANSLATIONS.en;
  }

  getTiers(): PricingTierData[] {
    return PRICING_TIERS;
  }

  getDisplayPrice(tier: PricingTierData): { usd: number; thb: number; period: string } {
    const t = this.getTranslation();
    if (this.billingCycle === 'annual') {
      return {
        usd: tier.priceAnnualUsd,
        thb: tier.priceAnnualThb,
        period: t.perYear
      };
    } else {
      return {
        usd: tier.priceMonthlyUsd,
        thb: tier.priceMonthlyThb,
        period: t.perMonth
      };
    }
  }

  async createCheckoutSession(
    tierId: string,
    customerEmail: string,
    customerName: string = '',
    payCurrency: string = 'usdttrc20',
    apiBase: string = 'https://bangplanix.95459654.xyz'
  ): Promise<CheckoutResult> {
    const cleanEmail = customerEmail.trim().toLowerCase();
    if (!cleanEmail || !cleanEmail.includes('@')) {
      return { success: false, error: 'Please enter a valid customer email address.' };
    }

    const tierCode = tierId === 'enterprise' ? 'ENTERPRISE' : 'PRO';
    const durationDays = this.billingCycle === 'annual' ? 365 : 30;

    try {
      const res = await fetch(`${apiBase}/api/v1/checkout/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tier: tierCode,
          customerEmail: cleanEmail,
          customerName: customerName.trim(),
          durationDays,
          payCurrency: payCurrency.trim().toLowerCase(),
          successUrl: typeof window !== 'undefined' ? `${window.location.origin}/lookup?payment=success` : undefined,
          cancelUrl: typeof window !== 'undefined' ? `${window.location.origin}/pricing.html` : undefined
        })
      });

      if (!res.ok) {
        const errText = await res.text();
        return { success: false, error: `Checkout initialization failed: ${errText}` };
      }

      const data: any = await res.json();
      return {
        success: true,
        checkoutUrl: data.checkoutUrl,
        invoiceId: data.invoiceId,
        paymentId: data.paymentId,
        paymentRecordId: data.paymentRecordId,
        payAddress: data.payAddress,
        payAmount: data.payAmount,
        payCurrency: data.payCurrency,
        amountUSD: data.amountUSD,
        currency: data.currency
      };
    } catch (err: any) {
      return { success: false, error: err.message || 'Network error connecting to payment gateway.' };
    }
  }
}


