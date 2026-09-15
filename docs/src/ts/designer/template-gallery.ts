/**
 * US Standards Preset Template Gallery (100% English)
 * Standard business formats: US Letter 8.5"x11", 4"x6" Shipping, 80mm POS
 */
import { BpxReportSchema } from '../core/types.js';

export interface GalleryTemplateItem {
  id: string;
  name: string;
  category: 'Billing' | 'Logistics' | 'Retail' | 'HR' | 'Executive';
  paperSize: string;
  description: string;
  schema: BpxReportSchema;
}

export const US_PRESET_TEMPLATES: GalleryTemplateItem[] = [
  {
    id: 'us_commercial_invoice',
    name: 'US Commercial Tax Invoice',
    category: 'Billing',
    paperSize: 'US Letter (8.5" x 11")',
    description: 'Corporate US Invoice with EIN, Net 30 Payment Terms, Sales Tax & Itemized SKU Table.',
    schema: {
      version: '1.0',
      metadata: { title: 'Commercial Invoice', author: 'Bangplanix US Billing Suite' },
      pageSetup: { paperKind: 'Letter', width: 612, height: 792, orientation: 'Portrait', margins: { top: 36, bottom: 36, left: 36, right: 36 } },
      parameters: [
        { name: 'InvoiceNo', type: 'string', defaultValue: 'INV-2026-8801' },
        { name: 'Company', type: 'string', defaultValue: 'BANGPLANIX ENTERPRISE LLC' },
        { name: 'EIN', type: 'string', defaultValue: 'XX-XXXXXXX' }
      ],
      bands: {
        ReportHeader: {
          height: 110,
          elements: [
            { id: 'h_title', type: 'Text', x: 0, y: 0, width: 320, height: 28, text: 'BANGPLANIX ENTERPRISE LLC', style: { fontSize: 18, fontWeight: 'Bold', color: '#0f172a' } },
            { id: 'h_tagline', type: 'Text', x: 0, y: 30, width: 320, height: 18, text: '500 Howard Street, Suite 400 • San Francisco, CA 94105', style: { fontSize: 10, color: '#64748b' } },
            { id: 'h_ein', type: 'Text', x: 0, y: 48, width: 250, height: 18, text: 'Federal Tax ID (EIN): 82-4192831', style: { fontSize: 10, color: '#64748b' } },
            { id: 'h_inv_badge', type: 'Text', x: 340, y: 0, width: 200, height: 28, text: 'COMMERCIAL INVOICE', style: { fontSize: 16, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'h_inv_no', type: 'Text', x: 340, y: 30, width: 200, height: 18, text: 'Invoice #: INV-2026-8801', style: { fontSize: 11, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'h_terms', type: 'Text', x: 340, y: 48, width: 200, height: 18, text: 'Payment Terms: Net 30 Days', style: { fontSize: 10, color: '#64748b', alignment: 'Right' } },
            { id: 'h_due_date', type: 'Text', x: 340, y: 66, width: 200, height: 18, text: 'Due Date: October 15, 2026', style: { fontSize: 10, color: '#ef4444', fontWeight: 'Bold', alignment: 'Right' } }
          ]
        },
        PageHeader: {
          height: 32,
          elements: [
            { id: 'ph_desc', type: 'Text', x: 0, y: 8, width: 260, height: 18, text: 'ITEM DESCRIPTION', style: { fontSize: 10, fontWeight: 'Bold', color: '#475569' } },
            { id: 'ph_sku', type: 'Text', x: 260, y: 8, width: 80, height: 18, text: 'SKU / CODE', style: { fontSize: 10, fontWeight: 'Bold', color: '#475569' } },
            { id: 'ph_qty', type: 'Text', x: 340, y: 8, width: 50, height: 18, text: 'QTY', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'ph_rate', type: 'Text', x: 400, y: 8, width: 65, height: 18, text: 'RATE ($)', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'ph_total', type: 'Text', x: 470, y: 8, width: 70, height: 18, text: 'AMOUNT ($)', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } }
          ]
        },
        Detail: {
          height: 30,
          elements: [
            { id: 'd_desc', type: 'Text', x: 0, y: 6, width: 260, height: 18, text: 'Bangplanix High-Speed Native AOT Cluster License', style: { fontSize: 10 } },
            { id: 'd_sku', type: 'Text', x: 260, y: 6, width: 80, height: 18, text: 'BPX-AOT-01', style: { fontSize: 10, color: '#64748b' } },
            { id: 'd_qty', type: 'Text', x: 340, y: 6, width: 50, height: 18, text: '1', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'd_rate', type: 'Text', x: 400, y: 6, width: 65, height: 18, text: '$1,999.00', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'd_amount', type: 'Text', x: 470, y: 6, width: 70, height: 18, text: '$1,999.00', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right' } }
          ]
        },
        ReportFooter: {
          height: 120,
          elements: [
            { id: 'rf_subtotal_lbl', type: 'Text', x: 340, y: 10, width: 100, height: 18, text: 'Subtotal:', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'rf_subtotal_val', type: 'Text', x: 450, y: 10, width: 90, height: 18, text: '$1,999.00', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'rf_tax_lbl', type: 'Text', x: 340, y: 28, width: 100, height: 18, text: 'Estimated Tax (0%):', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'rf_tax_val', type: 'Text', x: 450, y: 28, width: 90, height: 18, text: '$0.00', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'rf_total_lbl', type: 'Text', x: 320, y: 50, width: 120, height: 24, text: 'Total Due (USD):', style: { fontSize: 13, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'rf_total_val', type: 'Text', x: 450, y: 50, width: 90, height: 24, text: '$1,999.00', style: { fontSize: 14, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'rf_barcode', type: 'Barcode', x: 0, y: 35, width: 180, height: 35, text: 'INV-2026-8801' },
            { id: 'rf_notes', type: 'Text', x: 0, y: 80, width: 540, height: 20, text: 'Remit Wire / ACH to: Silicon Valley Bank, Routing # 121140399, Acct # 9940128419', style: { fontSize: 9, color: '#94a3b8' } }
          ]
        }
      }
    }
  },
  {
    id: 'usps_shipping_label',
    name: 'USPS / FedEx Shipping Label (4" x 6")',
    category: 'Logistics',
    paperSize: 'Thermal 4" x 6"',
    description: 'Direct thermal label with GS1-128 routing barcode, 2D QR tracking, and US ZIP+4.',
    schema: {
      version: '1.0',
      metadata: { title: 'Shipping Label', author: 'Bangplanix Logistics Engine' },
      pageSetup: { paperKind: 'Custom', width: 288, height: 432, orientation: 'Portrait', margins: { top: 12, bottom: 12, left: 12, right: 12 } },
      bands: {
        ReportHeader: {
          height: 80,
          elements: [
            { id: 'lbl_class', type: 'Text', x: 0, y: 0, width: 180, height: 24, text: 'PRIORITY MAIL 2-DAY®', style: { fontSize: 13, fontWeight: 'Bold' } },
            { id: 'lbl_postage', type: 'Text', x: 190, y: 0, width: 74, height: 20, text: 'U.S. POSTAGE PAID', style: { fontSize: 7.5, alignment: 'Right' } },
            { id: 'lbl_from', type: 'Text', x: 0, y: 28, width: 264, height: 36, text: 'SHIP FROM:\nBangplanix Logistics Center\n1200 Logistics Blvd, Dock 4\nMemphis, TN 38118', style: { fontSize: 8.5, color: '#475569' } }
          ]
        },
        Detail: {
          height: 140,
          elements: [
            { id: 'lbl_ship_to', type: 'Text', x: 0, y: 10, width: 264, height: 60, text: 'SHIP TO:\nJOHNATHAN DOE\n450 RIVERTOWN WAY, SUITE 8B\nSEATTLE, WA 98101-4421', style: { fontSize: 12, fontWeight: 'Bold' } },
            { id: 'lbl_zip_barcode', type: 'Barcode', x: 15, y: 75, width: 234, height: 50, text: '981014421' }
          ]
        },
        ReportFooter: {
          height: 150,
          elements: [
            { id: 'lbl_track_no', type: 'Text', x: 0, y: 10, width: 264, height: 16, text: 'USPS TRACKING # 9405 5112 0621 8840 1928 01', style: { fontSize: 8.5, fontWeight: 'Bold', alignment: 'Center' } },
            { id: 'lbl_main_barcode', type: 'Barcode', x: 12, y: 32, width: 240, height: 65, text: '9405511206218840192801' },
            { id: 'lbl_qr_track', type: 'QrCode', x: 100, y: 105, width: 40, height: 40, text: 'https://tools.usps.com/track?9405511206218840192801' }
          ]
        }
      }
    }
  },
  {
    id: 'us_pos_receipt',
    name: 'US Retail POS Thermal Receipt (80mm)',
    category: 'Retail',
    paperSize: '80mm (3 1/8")',
    description: 'Point-of-sale thermal receipt with California state tax, Visa authorization code, and UPC barcode.',
    schema: {
      version: '1.0',
      metadata: { title: 'POS Thermal Receipt', author: 'Bangplanix POS Service' },
      pageSetup: { paperKind: 'Custom', width: 226, height: 420, orientation: 'Portrait', margins: { top: 10, bottom: 10, left: 10, right: 10 } },
      bands: {
        ReportHeader: {
          height: 65,
          elements: [
            { id: 'pos_store', type: 'Text', x: 0, y: 0, width: 206, height: 18, text: 'PACIFIC BLUE COFFEE CO.', style: { fontSize: 12, fontWeight: 'Bold', alignment: 'Center' } },
            { id: 'pos_addr', type: 'Text', x: 0, y: 20, width: 206, height: 14, text: 'Market St & 4th, San Francisco, CA', style: { fontSize: 8.5, alignment: 'Center', color: '#64748b' } },
            { id: 'pos_reg', type: 'Text', x: 0, y: 36, width: 206, height: 14, text: 'REG: 04 • CLERK: ALEX • 09/15/2026 09:30 AM', style: { fontSize: 7.5, alignment: 'Center', color: '#64748b' } }
          ]
        },
        Detail: {
          height: 48,
          elements: [
            { id: 'pos_item1', type: 'Text', x: 0, y: 4, width: 140, height: 14, text: 'Iced Cold Brew (Venti)', style: { fontSize: 9 } },
            { id: 'pos_p1', type: 'Text', x: 140, y: 4, width: 66, height: 14, text: '$6.25', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'pos_item2', type: 'Text', x: 0, y: 22, width: 140, height: 14, text: 'Avocado Sourdough Toast', style: { fontSize: 9 } },
            { id: 'pos_p2', type: 'Text', x: 140, y: 22, width: 66, height: 14, text: '$12.50', style: { fontSize: 9, alignment: 'Right' } }
          ]
        },
        ReportFooter: {
          height: 120,
          elements: [
            { id: 'pos_sub', type: 'Text', x: 0, y: 8, width: 120, height: 14, text: 'Subtotal:', style: { fontSize: 9 } },
            { id: 'pos_sub_v', type: 'Text', x: 120, y: 8, width: 86, height: 14, text: '$18.75', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'pos_tax', type: 'Text', x: 0, y: 24, width: 120, height: 14, text: 'CA Sales Tax (8.625%):', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'pos_tax_v', type: 'Text', x: 120, y: 24, width: 86, height: 14, text: '$1.62', style: { fontSize: 8.5, alignment: 'Right' } },
            { id: 'pos_tot', type: 'Text', x: 0, y: 42, width: 100, height: 18, text: 'TOTAL:', style: { fontSize: 12, fontWeight: 'Bold' } },
            { id: 'pos_tot_v', type: 'Text', x: 100, y: 42, width: 106, height: 18, text: '$20.37', style: { fontSize: 12, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'pos_auth', type: 'Text', x: 0, y: 64, width: 206, height: 14, text: 'VISA CREDIT • AUTH: 049281 • AID: A0000000031010', style: { fontSize: 7, alignment: 'Center', color: '#94a3b8' } },
            { id: 'pos_qr', type: 'QrCode', x: 83, y: 82, width: 40, height: 40, text: 'https://bangplanix.io/r/049281' }
          ]
        }
      }
    }
  },
  {
    id: 'us_payroll_paystub',
    name: 'US Corporate Earnings Statement (Paystub)',
    category: 'HR',
    paperSize: 'US Letter (8.5" x 11")',
    description: 'Bi-weekly payroll statement with Federal Income Tax (FIT), FICA, Medicare, and 401(k) deductions.',
    schema: {
      version: '1.0',
      metadata: { title: 'Earnings Statement', author: 'Bangplanix Payroll Engine' },
      pageSetup: { paperKind: 'Letter', width: 612, height: 792, orientation: 'Portrait', margins: { top: 36, bottom: 36, left: 36, right: 36 } },
      bands: {
        ReportHeader: {
          height: 80,
          elements: [
            { id: 'ps_corp', type: 'Text', x: 0, y: 0, width: 300, height: 22, text: 'ACME TECHNOLOGIES CORP', style: { fontSize: 14, fontWeight: 'Bold' } },
            { id: 'ps_title', type: 'Text', x: 320, y: 0, width: 220, height: 22, text: 'EARNINGS STATEMENT', style: { fontSize: 13, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'ps_emp', type: 'Text', x: 0, y: 28, width: 260, height: 40, text: 'EMPLOYEE: DAVID S. MILLER\nSSN: XXX-XX-4912 • ID: EMP-8821\nJOB: SENIOR SYSTEMS ARCHITECT', style: { fontSize: 9.5, color: '#475569' } },
            { id: 'ps_period', type: 'Text', x: 300, y: 28, width: 240, height: 40, text: 'PAY PERIOD: 09/01/2026 - 09/14/2026\nPAY DATE: 09/18/2026\nFREQUENCY: BI-WEEKLY', style: { fontSize: 9.5, alignment: 'Right', color: '#475569' } }
          ]
        },
        Detail: {
          height: 90,
          elements: [
            { id: 'ps_gross_lbl', type: 'Text', x: 0, y: 10, width: 180, height: 16, text: 'Regular Base Earnings (80.00 hrs):', style: { fontSize: 10 } },
            { id: 'ps_gross_val', type: 'Text', x: 190, y: 10, width: 80, height: 16, text: '$6,250.00', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'ps_fit_lbl', type: 'Text', x: 300, y: 10, width: 150, height: 16, text: 'Federal Withholding (FIT):', style: { fontSize: 9.5, color: '#ef4444' } },
            { id: 'ps_fit_val', type: 'Text', x: 460, y: 10, width: 80, height: 16, text: '-$1,125.00', style: { fontSize: 9.5, alignment: 'Right' } },
            { id: 'ps_fica_lbl', type: 'Text', x: 300, y: 30, width: 150, height: 16, text: 'Social Security (FICA 6.2%):', style: { fontSize: 9.5, color: '#ef4444' } },
            { id: 'ps_fica_val', type: 'Text', x: 460, y: 30, width: 80, height: 16, text: '-$387.50', style: { fontSize: 9.5, alignment: 'Right' } },
            { id: 'ps_med_lbl', type: 'Text', x: 300, y: 50, width: 150, height: 16, text: 'Medicare (1.45%):', style: { fontSize: 9.5, color: '#ef4444' } },
            { id: 'ps_med_val', type: 'Text', x: 460, y: 50, width: 80, height: 16, text: '-$90.62', style: { fontSize: 9.5, alignment: 'Right' } },
            { id: 'ps_401k_lbl', type: 'Text', x: 300, y: 70, width: 150, height: 16, text: '401(k) Pre-tax Retirement (6%):', style: { fontSize: 9.5, color: '#ef4444' } },
            { id: 'ps_401k_val', type: 'Text', x: 460, y: 70, width: 80, height: 16, text: '-$375.00', style: { fontSize: 9.5, alignment: 'Right' } }
          ]
        },
        ReportFooter: {
          height: 60,
          elements: [
            { id: 'ps_net_lbl', type: 'Text', x: 280, y: 16, width: 160, height: 24, text: 'NET PAY DISTRIBUTION:', style: { fontSize: 12, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'ps_net_val', type: 'Text', x: 450, y: 16, width: 90, height: 24, text: '$4,271.88', style: { fontSize: 14, fontWeight: 'Bold', color: '#16a34a', alignment: 'Right' } }
          ]
        }
      }
    }
  },
  {
    id: 'executive_financial_summary',
    name: 'Executive Financial Summary Report',
    category: 'Executive',
    paperSize: 'US Letter Landscape',
    description: 'Boardroom executive performance report featuring revenue KPI metrics, profit charts, and division breakdown.',
    schema: {
      version: '1.0',
      metadata: { title: 'Executive Summary', author: 'Bangplanix Business Intelligence' },
      pageSetup: { paperKind: 'Letter', width: 792, height: 612, orientation: 'Landscape', margins: { top: 36, bottom: 36, left: 36, right: 36 } },
      bands: {
        ReportHeader: {
          height: 70,
          elements: [
            { id: 'ex_title', type: 'Text', x: 0, y: 0, width: 450, height: 28, text: 'GLOBAL ENTERPRISE PERFORMANCE — Q3 2026', style: { fontSize: 16, fontWeight: 'Bold', color: '#0f172a' } },
            { id: 'ex_subtitle', type: 'Text', x: 0, y: 30, width: 450, height: 18, text: 'CONFIDENTIAL • PREPARED FOR THE BOARD OF DIRECTORS', style: { fontSize: 9.5, color: '#64748b' } }
          ]
        },
        Detail: {
          height: 180,
          elements: [
            { id: 'ex_kpi1', type: 'Text', x: 0, y: 10, width: 220, height: 50, text: 'TOTAL REVENUE\n$48.2M (+18.4% YoY)', style: { fontSize: 12, fontWeight: 'Bold', color: '#2563eb' } },
            { id: 'ex_kpi2', type: 'Text', x: 240, y: 10, width: 220, height: 50, text: 'OPERATING PROFIT\n$14.8M (30.7% Margin)', style: { fontSize: 12, fontWeight: 'Bold', color: '#16a34a' } },
            { id: 'ex_kpi3', type: 'Text', x: 480, y: 10, width: 220, height: 50, text: 'NET RETENTION RATE\n124.6% NRR', style: { fontSize: 12, fontWeight: 'Bold', color: '#9333ea' } },
            { id: 'ex_chart', type: 'Chart', x: 0, y: 70, width: 700, height: 100, chart: { chartType: 'Column', title: 'Q1-Q3 Revenue vs Target' } }
          ]
        },
        ReportFooter: {
          height: 40,
          elements: [
            { id: 'ex_footer', type: 'Text', x: 0, y: 10, width: 720, height: 18, text: 'Bangplanix Document Intelligence Engine • Auto-generated Sub-millisecond Report', style: { fontSize: 8.5, alignment: 'Center', color: '#94a3b8' } }
          ]
        }
      }
    }
  }
];

export class TemplateGalleryManager {
  getTemplates(): GalleryTemplateItem[] {
    return US_PRESET_TEMPLATES;
  }

  getTemplateById(id: string): GalleryTemplateItem | undefined {
    return US_PRESET_TEMPLATES.find(t => t.id === id);
  }
}
