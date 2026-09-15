/**
 * Core Type Definitions for Bangplanix Docs & Web Designer
 */

export interface PageSetup {
  paperKind?: string;
  width?: number;
  height?: number;
  orientation?: 'Portrait' | 'Landscape';
  margins?: {
    top: number;
    bottom: number;
    left: number;
    right: number;
  };
}

export interface ReportElement {
  id: string;
  type: string;
  x: number;
  y: number;
  width: number;
  height: number;
  text?: string;
  expression?: string;
  style?: Record<string, any>;
  chart?: any;
  barcode?: any;
  imageSource?: string;
}

export interface ReportBand {
  height: number;
  elements: ReportElement[];
}

export interface BpxReportSchema {
  $schema?: string;
  version: string;
  metadata?: {
    title?: string;
    author?: string;
    createdAt?: string;
    description?: string;
  };
  pageSetup: PageSetup;
  parameters?: Array<{ name: string; type: string; defaultValue?: any }>;
  datasets?: Array<{ name: string; source: string; query?: string; fields?: any[] }>;
  bands: Record<string, ReportBand>;
}

export interface StorageItem {
  id: string;
  name: string;
  savedAt: string;
  schema: BpxReportSchema;
}

export type SdkLanguage = 'csharp' | 'nodejs' | 'python' | 'go' | 'java';

export interface MockScenario {
  id: string;
  title: string;
  description: string;
  data: Record<string, any>;
}

export interface ShortcutItem {
  key: string;
  macKey: string;
  action: string;
  description: string;
}

export interface PricingTierData {
  id: string;
  name: string;
  tagline: string;
  priceAnnualUsd: number;
  priceMonthlyUsd: number;
  priceAnnualThb: number;
  priceMonthlyThb: number;
  coresQuota: number | 'Unlimited';
  features: string[];
  isPopular?: boolean;
  checkoutUrlAnnual: string;
  checkoutUrlMonthly: string;
}
