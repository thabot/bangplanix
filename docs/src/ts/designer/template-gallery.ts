/**
 * Standard Preset Template Gallery
 * Formats: Standard A4 (210x297mm), US Letter 8.5"x11", 4"x6" Shipping, 80mm POS
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
    id: 'a4_commercial_invoice',
    name: 'A4 Commercial Tax Invoice',
    category: 'Billing',
    paperSize: 'ISO A4 (210mm x 297mm)',
    description: 'International standard A4 commercial invoice with VAT breakdown, itemized SKU table & bank details.',
    schema: {
      version: '1.0',
      metadata: { title: 'Commercial Tax Invoice (A4)', author: 'Bangplanix Global Billing Engine' },
      pageSetup: { paperKind: 'A4', width: 595.28, height: 841.89, orientation: 'Portrait', margins: { top: 36, bottom: 36, left: 36, right: 36 } },
      parameters: [
        { name: 'InvoiceNo', type: 'string', defaultValue: 'INV-2026-9901' },
        { name: 'Company', type: 'string', defaultValue: 'BANGPLANIX ENTERPRISE CORP' },
        { name: 'TaxId', type: 'string', defaultValue: '0105559876543' }
      ],
      datasets: [
        {
          name: 'InvoiceItems',
          source: 'InMemory',
          fields: [
            { name: 'ItemCode', type: 'string' },
            { name: 'Description', type: 'string' },
            { name: 'Quantity', type: 'number' },
            { name: 'UnitPrice', type: 'number' },
            { name: 'TotalAmount', type: 'number' }
          ]
        }
      ],
      bands: {
        ReportHeader: {
          height: 115,
          elements: [
            { id: 'h_title', type: 'Text', x: 0, y: 0, width: 320, height: 28, text: 'BANGPLANIX ENTERPRISE CORP', style: { fontSize: 18, fontWeight: 'Bold', color: '#0f172a' } },
            { id: 'h_tagline', type: 'Text', x: 0, y: 30, width: 320, height: 18, text: 'Empire Tower, 47th Fl., South Sathorn Rd., Bangkok 10120', style: { fontSize: 10, color: '#64748b' } },
            { id: 'h_taxid', type: 'Text', x: 0, y: 48, width: 280, height: 18, text: 'Tax ID / เลขประจำตัวผู้เสียภาษี: 0105559876543 (Head Office)', style: { fontSize: 10, color: '#64748b' } },
            { id: 'h_inv_badge', type: 'Text', x: 320, y: 0, width: 200, height: 28, text: 'TAX INVOICE / RECEIPT', style: { fontSize: 15, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'h_inv_no', type: 'Text', x: 320, y: 30, width: 200, height: 18, text: 'Invoice #: INV-2026-9901', style: { fontSize: 11, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'h_date', type: 'Text', x: 320, y: 48, width: 200, height: 18, text: 'Date: September 15, 2026', style: { fontSize: 10, color: '#64748b', alignment: 'Right' } },
            { id: 'h_due_date', type: 'Text', x: 320, y: 66, width: 200, height: 18, text: 'Due Date: October 15, 2026', style: { fontSize: 10, color: '#ef4444', fontWeight: 'Bold', alignment: 'Right' } }
          ]
        },
        PageHeader: {
          height: 32,
          elements: [
            { id: 'ph_desc', type: 'Text', x: 0, y: 8, width: 260, height: 18, text: 'ITEM DESCRIPTION', style: { fontSize: 10, fontWeight: 'Bold', color: '#475569' } },
            { id: 'ph_sku', type: 'Text', x: 260, y: 8, width: 80, height: 18, text: 'SKU / CODE', style: { fontSize: 10, fontWeight: 'Bold', color: '#475569' } },
            { id: 'ph_qty', type: 'Text', x: 340, y: 8, width: 45, height: 18, text: 'QTY', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'ph_rate', type: 'Text', x: 395, y: 8, width: 60, height: 18, text: 'PRICE', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'ph_total', type: 'Text', x: 460, y: 8, width: 63, height: 18, text: 'AMOUNT', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } }
          ]
        },
        Detail: {
          height: 30,
          elements: [
            { id: 'd_desc', type: 'Text', x: 0, y: 6, width: 260, height: 18, text: 'Bangplanix High-Speed Native AOT Cluster License', style: { fontSize: 10 } },
            { id: 'd_sku', type: 'Text', x: 260, y: 6, width: 80, height: 18, text: 'BPX-AOT-01', style: { fontSize: 10, color: '#64748b' } },
            { id: 'd_qty', type: 'Text', x: 340, y: 6, width: 45, height: 18, text: '1', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'd_rate', type: 'Text', x: 395, y: 6, width: 60, height: 18, text: '69,900.00', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'd_amount', type: 'Text', x: 460, y: 6, width: 63, height: 18, text: '69,900.00', style: { fontSize: 10, fontWeight: 'Bold', alignment: 'Right' } }
          ]
        },
        ReportFooter: {
          height: 140,
          elements: [
            { id: 'rf_subtotal_lbl', type: 'Text', x: 320, y: 10, width: 100, height: 18, text: 'Subtotal:', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'rf_subtotal_val', type: 'Text', x: 430, y: 10, width: 93, height: 18, text: '69,900.00 THB', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'rf_tax_lbl', type: 'Text', x: 320, y: 28, width: 100, height: 18, text: 'VAT (7%):', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'rf_tax_val', type: 'Text', x: 430, y: 28, width: 93, height: 18, text: '4,893.00 THB', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'rf_total_lbl', type: 'Text', x: 300, y: 50, width: 120, height: 24, text: 'Grand Total (รวมทั้งสิ้น):', style: { fontSize: 12, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'rf_total_val', type: 'Text', x: 430, y: 50, width: 93, height: 24, text: '74,793.00 THB', style: { fontSize: 13, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'rf_bahttext', type: 'Text', x: 0, y: 52, width: 300, height: 20, text: '(=เจ็ดหมื่นสี่พันเจ็ดร้อยเก้าสิบสามบาทถ้วน=)', style: { fontSize: 9.5, fontWeight: 'Bold', color: '#1e293b' } },
            { id: 'rf_barcode', type: 'Barcode', x: 0, y: 80, width: 180, height: 35, text: 'INV-2026-9901' },
            { id: 'rf_notes', type: 'Text', x: 200, y: 85, width: 323, height: 35, text: 'Payment via PromptPay / SCB Bank A/C: 111-394821-9\nThank you for choosing Bangplanix Enterprise Engine!', style: { fontSize: 8.5, color: '#64748b' } }
          ]
        }
      }
    }
  },
  {
    id: 'a4_multipage_enterprise_invoice',
    name: 'A4 Multi-Page Enterprise Invoice',
    category: 'Billing',
    paperSize: 'ISO A4 (210mm x 297mm • 2 Pages)',
    description: 'Multi-page commercial invoice with 25 itemized SKU rows looping across 2 pages with running totals & page breaks.',
    schema: {
      version: '1.0',
      metadata: { title: 'Enterprise Tax Invoice (Multi-Page)', author: 'Bangplanix Global Billing Engine' },
      pageSetup: { paperKind: 'A4', width: 595.28, height: 841.89, orientation: 'Portrait', margins: { top: 36, bottom: 36, left: 36, right: 36 } },
      parameters: [
        { name: 'InvoiceNo', type: 'string', defaultValue: 'INV-2026-MULTI' },
        { name: 'Customer', type: 'string', defaultValue: 'Global Megacorp Logistics LLC' }
      ],
      datasets: [
        {
          name: 'InvoiceItems',
          source: 'InMemory',
          fields: [
            { name: 'ItemCode', type: 'string' },
            { name: 'Description', type: 'string' },
            { name: 'Quantity', type: 'number' },
            { name: 'UnitPrice', type: 'number' },
            { name: 'TotalAmount', type: 'number' }
          ]
        }
      ],
      bands: {
        ReportHeader: {
          height: 100,
          elements: [
            { id: 'mp_title', type: 'Text', x: 0, y: 0, width: 340, height: 28, text: 'BANGPLANIX ENTERPRISE CORP', style: { fontSize: 18, fontWeight: 'Bold', color: '#0f172a' } },
            { id: 'mp_sub', type: 'Text', x: 0, y: 30, width: 340, height: 18, text: 'Bangkok / Singapore / Tokyo Global Cloud Delivery', style: { fontSize: 10, color: '#64748b' } },
            { id: 'mp_tax', type: 'Text', x: 0, y: 48, width: 280, height: 18, text: 'Tax ID: 0105559876543 • Customer: Global Megacorp Logistics', style: { fontSize: 10, color: '#64748b' } },
            { id: 'mp_inv_badge', type: 'Text', x: 340, y: 0, width: 180, height: 28, text: 'ENTERPRISE INVOICE', style: { fontSize: 14, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'mp_inv_no', type: 'Text', x: 340, y: 30, width: 180, height: 18, text: 'Invoice #: INV-2026-MULTI', style: { fontSize: 11, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'mp_date', type: 'Text', x: 340, y: 48, width: 180, height: 18, text: 'Pages: 2 Pages Continuous', style: { fontSize: 10, color: '#64748b', alignment: 'Right' } }
          ]
        },
        PageHeader: {
          height: 30,
          elements: [
            { id: 'mp_ph_desc', type: 'Text', x: 0, y: 6, width: 250, height: 18, text: 'ITEM DESCRIPTION', style: { fontSize: 9.5, fontWeight: 'Bold', color: '#475569' } },
            { id: 'mp_ph_sku', type: 'Text', x: 250, y: 6, width: 80, height: 18, text: 'SKU / CODE', style: { fontSize: 9.5, fontWeight: 'Bold', color: '#475569' } },
            { id: 'mp_ph_qty', type: 'Text', x: 330, y: 6, width: 50, height: 18, text: 'QTY', style: { fontSize: 9.5, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'mp_ph_rate', type: 'Text', x: 390, y: 6, width: 65, height: 18, text: 'RATE', style: { fontSize: 9.5, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } },
            { id: 'mp_ph_total', type: 'Text', x: 460, y: 6, width: 63, height: 18, text: 'AMOUNT', style: { fontSize: 9.5, fontWeight: 'Bold', alignment: 'Right', color: '#475569' } }
          ]
        },
        Detail: {
          height: 950,
          elements: [
            { id: 'd_item_0_desc', type: 'Text', x: 0, y: 4, width: 250, height: 18, text: '#1. Industrial Cloud Node - Rack 1001', style: { fontSize: 9 } },
            { id: 'd_item_0_sku', type: 'Text', x: 250, y: 4, width: 80, height: 18, text: 'BPX-CL-1001', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_0_qty', type: 'Text', x: 330, y: 4, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_0_rate', type: 'Text', x: 390, y: 4, width: 65, height: 18, text: '1,250.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_0_amt', type: 'Text', x: 460, y: 4, width: 63, height: 18, text: '1,250.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_1_desc', type: 'Text', x: 0, y: 34, width: 250, height: 18, text: '#2. Industrial Cloud Node - Rack 1002', style: { fontSize: 9 } },
            { id: 'd_item_1_sku', type: 'Text', x: 250, y: 34, width: 80, height: 18, text: 'BPX-CL-1002', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_1_qty', type: 'Text', x: 330, y: 34, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_1_rate', type: 'Text', x: 390, y: 34, width: 65, height: 18, text: '1,400.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_1_amt', type: 'Text', x: 460, y: 34, width: 63, height: 18, text: '2,800.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_2_desc', type: 'Text', x: 0, y: 64, width: 250, height: 18, text: '#3. Edge Computing Gateway Unit', style: { fontSize: 9 } },
            { id: 'd_item_2_sku', type: 'Text', x: 250, y: 64, width: 80, height: 18, text: 'BPX-GW-201', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_2_qty', type: 'Text', x: 330, y: 64, width: 50, height: 18, text: '4', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_2_rate', type: 'Text', x: 390, y: 64, width: 65, height: 18, text: '850.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_2_amt', type: 'Text', x: 460, y: 64, width: 63, height: 18, text: '3,400.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_3_desc', type: 'Text', x: 0, y: 94, width: 250, height: 18, text: '#4. High-Throughput NVMe Storage Array', style: { fontSize: 9 } },
            { id: 'd_item_3_sku', type: 'Text', x: 250, y: 94, width: 80, height: 18, text: 'BPX-ST-500', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_3_qty', type: 'Text', x: 330, y: 94, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_3_rate', type: 'Text', x: 390, y: 94, width: 65, height: 18, text: '3,200.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_3_amt', type: 'Text', x: 460, y: 94, width: 63, height: 18, text: '6,400.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_4_desc', type: 'Text', x: 0, y: 124, width: 250, height: 18, text: '#5. Realtime Roslyn Script Evaluator', style: { fontSize: 9 } },
            { id: 'd_item_4_sku', type: 'Text', x: 250, y: 124, width: 80, height: 18, text: 'BPX-EV-09', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_4_qty', type: 'Text', x: 330, y: 124, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_4_rate', type: 'Text', x: 390, y: 124, width: 65, height: 18, text: '4,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_4_amt', type: 'Text', x: 460, y: 124, width: 63, height: 18, text: '4,500.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_5_desc', type: 'Text', x: 0, y: 154, width: 250, height: 18, text: '#6. 2D Datamatrix & Aztec Barcode Pack', style: { fontSize: 9 } },
            { id: 'd_item_5_sku', type: 'Text', x: 250, y: 154, width: 80, height: 18, text: 'BPX-BC-2D', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_5_qty', type: 'Text', x: 330, y: 154, width: 50, height: 18, text: '5', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_5_rate', type: 'Text', x: 390, y: 154, width: 65, height: 18, text: '350.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_5_amt', type: 'Text', x: 460, y: 154, width: 63, height: 18, text: '1,750.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_6_desc', type: 'Text', x: 0, y: 184, width: 250, height: 18, text: '#7. Enterprise Multi-Tenant Billing Gateway', style: { fontSize: 9 } },
            { id: 'd_item_6_sku', type: 'Text', x: 250, y: 184, width: 80, height: 18, text: 'BPX-GW-MT', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_6_qty', type: 'Text', x: 330, y: 184, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_6_rate', type: 'Text', x: 390, y: 184, width: 65, height: 18, text: '8,900.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_6_amt', type: 'Text', x: 460, y: 184, width: 63, height: 18, text: '8,900.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_7_desc', type: 'Text', x: 0, y: 214, width: 250, height: 18, text: '#8. AOT Native Linux Shared Library Pack', style: { fontSize: 9 } },
            { id: 'd_item_7_sku', type: 'Text', x: 250, y: 214, width: 80, height: 18, text: 'BPX-SO-LINUX', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_7_qty', type: 'Text', x: 330, y: 214, width: 50, height: 18, text: '3', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_7_rate', type: 'Text', x: 390, y: 214, width: 65, height: 18, text: '1,200.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_7_amt', type: 'Text', x: 460, y: 214, width: 63, height: 18, text: '3,600.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_8_desc', type: 'Text', x: 0, y: 244, width: 250, height: 18, text: '#9. AOT Native macOS Universal dylib Pack', style: { fontSize: 9 } },
            { id: 'd_item_8_sku', type: 'Text', x: 250, y: 244, width: 80, height: 18, text: 'BPX-DY-MAC', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_8_qty', type: 'Text', x: 330, y: 244, width: 50, height: 18, text: '3', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_8_rate', type: 'Text', x: 390, y: 244, width: 65, height: 18, text: '1,200.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_8_amt', type: 'Text', x: 460, y: 244, width: 63, height: 18, text: '3,600.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_9_desc', type: 'Text', x: 0, y: 274, width: 250, height: 18, text: '#10. Automated Cluster Failover Watchdog', style: { fontSize: 9 } },
            { id: 'd_item_9_sku', type: 'Text', x: 250, y: 274, width: 80, height: 18, text: 'BPX-HA-DOG', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_9_qty', type: 'Text', x: 330, y: 274, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_9_rate', type: 'Text', x: 390, y: 274, width: 65, height: 18, text: '2,100.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_9_amt', type: 'Text', x: 460, y: 274, width: 63, height: 18, text: '4,200.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_10_desc', type: 'Text', x: 0, y: 304, width: 250, height: 18, text: '#11. PDF/A-3b Archival Conformance Engine', style: { fontSize: 9 } },
            { id: 'd_item_10_sku', type: 'Text', x: 250, y: 304, width: 80, height: 18, text: 'BPX-PDFA-3B', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_10_qty', type: 'Text', x: 330, y: 304, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_10_rate', type: 'Text', x: 390, y: 304, width: 65, height: 18, text: '3,800.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_10_amt', type: 'Text', x: 460, y: 304, width: 63, height: 18, text: '3,800.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_11_desc', type: 'Text', x: 0, y: 334, width: 250, height: 18, text: '#12. Thai PromptPay EMV-QR Generator Plug', style: { fontSize: 9 } },
            { id: 'd_item_11_sku', type: 'Text', x: 250, y: 334, width: 80, height: 18, text: 'BPX-QR-PP', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_11_qty', type: 'Text', x: 330, y: 334, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_11_rate', type: 'Text', x: 390, y: 334, width: 65, height: 18, text: '1,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_11_amt', type: 'Text', x: 460, y: 334, width: 63, height: 18, text: '1,500.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_12_desc', type: 'Text', x: 0, y: 364, width: 250, height: 18, text: '#13. Enterprise SLA 24x7 Mission Critical', style: { fontSize: 9 } },
            { id: 'd_item_12_sku', type: 'Text', x: 250, y: 364, width: 80, height: 18, text: 'BPX-SLA-247', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_12_qty', type: 'Text', x: 330, y: 364, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_12_rate', type: 'Text', x: 390, y: 364, width: 65, height: 18, text: '12,000.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_12_amt', type: 'Text', x: 460, y: 364, width: 63, height: 18, text: '12,000.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            // Additional Items 14-25 flowing across Page 2
            { id: 'd_item_13_desc', type: 'Text', x: 0, y: 450, width: 250, height: 18, text: '#14. Distributed Job Orchestrator Worker Node', style: { fontSize: 9 } },
            { id: 'd_item_13_sku', type: 'Text', x: 250, y: 450, width: 80, height: 18, text: 'BPX-JOB-01', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_13_qty', type: 'Text', x: 330, y: 450, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_13_rate', type: 'Text', x: 390, y: 450, width: 65, height: 18, text: '2,400.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_13_amt', type: 'Text', x: 460, y: 450, width: 63, height: 18, text: '4,800.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_14_desc', type: 'Text', x: 0, y: 480, width: 250, height: 18, text: '#15. WebAssembly Canvas Renderer Engine Pack', style: { fontSize: 9 } },
            { id: 'd_item_14_sku', type: 'Text', x: 250, y: 480, width: 80, height: 18, text: 'BPX-WASM-02', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_14_qty', type: 'Text', x: 330, y: 480, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_14_rate', type: 'Text', x: 390, y: 480, width: 65, height: 18, text: '5,000.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_14_amt', type: 'Text', x: 460, y: 480, width: 63, height: 18, text: '5,000.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_15_desc', type: 'Text', x: 0, y: 510, width: 250, height: 18, text: '#16. Skia Vector Graphics Acceleration GPU Unit', style: { fontSize: 9 } },
            { id: 'd_item_15_sku', type: 'Text', x: 250, y: 510, width: 80, height: 18, text: 'BPX-SKIA-GPU', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_15_qty', type: 'Text', x: 330, y: 510, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_15_rate', type: 'Text', x: 390, y: 510, width: 65, height: 18, text: '9,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_15_amt', type: 'Text', x: 460, y: 510, width: 63, height: 18, text: '9,500.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_16_desc', type: 'Text', x: 0, y: 540, width: 250, height: 18, text: '#17. Thai Tax e-Invoice & e-Receipt XML Signer', style: { fontSize: 9 } },
            { id: 'd_item_16_sku', type: 'Text', x: 250, y: 540, width: 80, height: 18, text: 'BPX-ETAX-TH', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_16_qty', type: 'Text', x: 330, y: 540, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_16_rate', type: 'Text', x: 390, y: 540, width: 65, height: 18, text: '7,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_16_amt', type: 'Text', x: 460, y: 540, width: 63, height: 18, text: '7,500.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_17_desc', type: 'Text', x: 0, y: 570, width: 250, height: 18, text: '#18. High-Res Thermal Barcode Print Optimizer', style: { fontSize: 9 } },
            { id: 'd_item_17_sku', type: 'Text', x: 250, y: 570, width: 80, height: 18, text: 'BPX-THERM-OPT', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_17_qty', type: 'Text', x: 330, y: 570, width: 50, height: 18, text: '3', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_17_rate', type: 'Text', x: 390, y: 570, width: 65, height: 18, text: '850.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_17_amt', type: 'Text', x: 460, y: 570, width: 63, height: 18, text: '2,550.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_18_desc', type: 'Text', x: 0, y: 600, width: 250, height: 18, text: '#19. Real-Time Kafka Stream Reporting Connector', style: { fontSize: 9 } },
            { id: 'd_item_18_sku', type: 'Text', x: 250, y: 600, width: 80, height: 18, text: 'BPX-KAFKA-CON', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_18_qty', type: 'Text', x: 330, y: 600, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_18_rate', type: 'Text', x: 390, y: 600, width: 65, height: 18, text: '6,200.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_18_amt', type: 'Text', x: 460, y: 600, width: 63, height: 18, text: '6,200.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_19_desc', type: 'Text', x: 0, y: 630, width: 250, height: 18, text: '#20. PostgreSQL Partitioned Timeseries Plug', style: { fontSize: 9 } },
            { id: 'd_item_19_sku', type: 'Text', x: 250, y: 630, width: 80, height: 18, text: 'BPX-PG-TS', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_19_qty', type: 'Text', x: 330, y: 630, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_19_rate', type: 'Text', x: 390, y: 630, width: 65, height: 18, text: '4,800.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_19_amt', type: 'Text', x: 460, y: 630, width: 63, height: 18, text: '4,800.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_20_desc', type: 'Text', x: 0, y: 660, width: 250, height: 18, text: '#21. Zero-Copy Shared Memory Cluster IPC', style: { fontSize: 9 } },
            { id: 'd_item_20_sku', type: 'Text', x: 250, y: 660, width: 80, height: 18, text: 'BPX-IPC-SHM', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_20_qty', type: 'Text', x: 330, y: 660, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_20_rate', type: 'Text', x: 390, y: 660, width: 65, height: 18, text: '3,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_20_amt', type: 'Text', x: 460, y: 660, width: 63, height: 18, text: '7,000.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_21_desc', type: 'Text', x: 0, y: 690, width: 250, height: 18, text: '#22. Automated Load Balancer Health Probe Unit', style: { fontSize: 9 } },
            { id: 'd_item_21_sku', type: 'Text', x: 250, y: 690, width: 80, height: 18, text: 'BPX-LB-PROBE', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_21_qty', type: 'Text', x: 330, y: 690, width: 50, height: 18, text: '4', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_21_rate', type: 'Text', x: 390, y: 690, width: 65, height: 18, text: '600.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_21_amt', type: 'Text', x: 460, y: 690, width: 63, height: 18, text: '2,400.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_22_desc', type: 'Text', x: 0, y: 720, width: 250, height: 18, text: '#23. Cloud Security Encryption HSM Connector', style: { fontSize: 9 } },
            { id: 'd_item_22_sku', type: 'Text', x: 250, y: 720, width: 80, height: 18, text: 'BPX-SEC-HSM', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_22_qty', type: 'Text', x: 330, y: 720, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_22_rate', type: 'Text', x: 390, y: 720, width: 65, height: 18, text: '8,400.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_22_amt', type: 'Text', x: 460, y: 720, width: 63, height: 18, text: '8,400.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_23_desc', type: 'Text', x: 0, y: 750, width: 250, height: 18, text: '#24. Realtime Webhook Dispatcher Worker', style: { fontSize: 9 } },
            { id: 'd_item_23_sku', type: 'Text', x: 250, y: 750, width: 80, height: 18, text: 'BPX-HOOK-DISP', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_23_qty', type: 'Text', x: 330, y: 750, width: 50, height: 18, text: '2', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_23_rate', type: 'Text', x: 390, y: 750, width: 65, height: 18, text: '1,500.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_23_amt', type: 'Text', x: 460, y: 750, width: 63, height: 18, text: '3,000.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } },

            { id: 'd_item_24_desc', type: 'Text', x: 0, y: 780, width: 250, height: 18, text: '#25. Multi-Region Geo-Redundant Hot Standby', style: { fontSize: 9 } },
            { id: 'd_item_24_sku', type: 'Text', x: 250, y: 780, width: 80, height: 18, text: 'BPX-GEO-STAND', style: { fontSize: 8.5, color: '#64748b' } },
            { id: 'd_item_24_qty', type: 'Text', x: 330, y: 780, width: 50, height: 18, text: '1', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_24_rate', type: 'Text', x: 390, y: 780, width: 65, height: 18, text: '15,000.00', style: { fontSize: 9, alignment: 'Right' } },
            { id: 'd_item_24_amt', type: 'Text', x: 460, y: 780, width: 63, height: 18, text: '15,000.00', style: { fontSize: 9, fontWeight: 'Bold', alignment: 'Right' } }
          ]
        },
        ReportFooter: {
          height: 120,
          elements: [
            { id: 'mp_rf_sub_lbl', type: 'Text', x: 320, y: 10, width: 100, height: 18, text: 'Subtotal (25 Items):', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'mp_rf_sub_val', type: 'Text', x: 430, y: 10, width: 93, height: 18, text: '136,800.00 THB', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'mp_rf_tax_lbl', type: 'Text', x: 320, y: 28, width: 100, height: 18, text: 'VAT (7%):', style: { fontSize: 10, alignment: 'Right', color: '#64748b' } },
            { id: 'mp_rf_tax_val', type: 'Text', x: 430, y: 28, width: 93, height: 18, text: '9,576.00 THB', style: { fontSize: 10, alignment: 'Right' } },
            { id: 'mp_rf_tot_lbl', type: 'Text', x: 300, y: 50, width: 120, height: 24, text: 'Grand Total:', style: { fontSize: 12, fontWeight: 'Bold', alignment: 'Right' } },
            { id: 'mp_rf_tot_val', type: 'Text', x: 430, y: 50, width: 93, height: 24, text: '146,376.00 THB', style: { fontSize: 13, fontWeight: 'Bold', color: '#2563eb', alignment: 'Right' } },
            { id: 'mp_rf_barcode', type: 'Barcode', x: 0, y: 60, width: 180, height: 35, text: 'INV-2026-MULTI' }
          ]
        }
      }
    }
  },
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
