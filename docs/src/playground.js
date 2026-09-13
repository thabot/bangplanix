import { BangplanixWasmEngine } from './wasm-engine.js';

const SAMPLES = {
  invoice: {
    template: {
      "$schema": "https://bangplanix.io/schemas/v1/bangplanix.schema.json",
      "version": "1.0.0",
      "metadata": { "title": "Commercial Tax Invoice", "author": "Bangplanix Billing Engine" },
      "pageSetup": { "paperKind": "A4", "orientation": "Portrait", "margins": { "top": 30, "bottom": 30, "left": 30, "right": 30 } },
      "parameters": [
        { "name": "Company", "type": "string", "defaultValue": "Bangplanix Enterprise Co., Ltd." },
        { "name": "InvoiceNo", "type": "string", "defaultValue": "INV-2026-0901" }
      ],
      "datasets": [{ "name": "items", "source": "json" }],
      "bands": {
        "title": {
          "height": 70,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 300, "height": 24 }, "expression": "=Parameters!Company.Value", "fontSize": 16, "fontWeight": "Bold", "textColor": "#1e293b" },
            { "type": "text", "bounds": { "x": 300, "y": 0, "width": 235, "height": 24 }, "content": "TAX INVOICE / RECEIPT", "fontSize": 14, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#2563eb" },
            { "type": "text", "bounds": { "x": 300, "y": 26, "width": 235, "height": 18 }, "expression": "=\"Invoice No: \" + Parameters!InvoiceNo.Value", "fontSize": 11, "textAlign": "Right", "textColor": "#64748b" },
            { "type": "line", "bounds": { "x": 0, "y": 55, "width": 535, "height": 1 }, "strokeColor": "#e2e8f0", "strokeWidth": 1.5 }
          ]
        },
        "pageHeader": {
          "height": 30,
          "elements": [
            { "type": "rectangle", "bounds": { "x": 0, "y": 0, "width": 535, "height": 26 }, "fillColor": "#f8fafc", "borderColor": "#e2e8f0" },
            { "type": "text", "bounds": { "x": 10, "y": 6, "width": 240, "height": 16 }, "content": "Item Description", "fontSize": 10, "fontWeight": "Bold", "textColor": "#475569" },
            { "type": "text", "bounds": { "x": 260, "y": 6, "width": 60, "height": 16 }, "content": "Qty", "fontSize": 10, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#475569" },
            { "type": "text", "bounds": { "x": 330, "y": 6, "width": 90, "height": 16 }, "content": "Unit Price", "fontSize": 10, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#475569" },
            { "type": "text", "bounds": { "x": 430, "y": 6, "width": 95, "height": 16 }, "content": "Total Amount", "fontSize": 10, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#475569" }
          ]
        },
        "detail": {
          "dataset": "items",
          "height": 28,
          "elements": [
            { "type": "text", "bounds": { "x": 10, "y": 6, "width": 240, "height": 16 }, "expression": "=Fields!name.Value", "fontSize": 10, "textColor": "#0f172a" },
            { "type": "text", "bounds": { "x": 260, "y": 6, "width": 60, "height": 16 }, "expression": "=Fields!qty.Value", "fontSize": 10, "textAlign": "Right" },
            { "type": "text", "bounds": { "x": 330, "y": 6, "width": 90, "height": 16 }, "expression": "=Fields!price.Value", "fontSize": 10, "textAlign": "Right", "format": "N2" },
            { "type": "text", "bounds": { "x": 430, "y": 6, "width": 95, "height": 16 }, "expression": "=Fields!total.Value", "fontSize": 10, "textAlign": "Right", "fontWeight": "Bold", "format": "N2" },
            { "type": "line", "bounds": { "x": 0, "y": 27, "width": 535, "height": 1 }, "strokeColor": "#f1f5f9", "strokeWidth": 1 }
          ]
        },
        "pageFooter": {
          "height": 50,
          "elements": [
            { "type": "line", "bounds": { "x": 0, "y": 5, "width": 535, "height": 1 }, "strokeColor": "#cbd5e1", "strokeWidth": 1 },
            { "type": "barcode", "bounds": { "x": 0, "y": 12, "width": 180, "height": 32 }, "expression": "=Parameters!InvoiceNo.Value", "symbology": "Code128" },
            { "type": "text", "bounds": { "x": 250, "y": 18, "width": 285, "height": 16 }, "expression": "=\"Thank you for your business! Generated on \" + Globals!ExecutionTime", "fontSize": 8.5, "textAlign": "Right", "textColor": "#94a3b8" }
          ]
        }
      }
    },
    data: {
      items: [
        { name: "Enterprise High-Performance Engine License", qty: 1, price: 45000, total: 45000 },
        { name: "Dedicated 24/7 SLA Support Package", qty: 12, price: 5000, total: 60000 },
        { name: "Custom Legacy Report Adapter Migration", qty: 5, price: 8000, total: 40000 }
      ]
    }
  },
  thai_etax: {
    template: {
      "$schema": "https://bangplanix.io/schemas/v1/bangplanix.schema.json",
      "version": "1.0.0",
      "metadata": { "title": "ใบกำกับภาษีเต็มรูป (e-Tax Invoice)", "standard": "ETDA ER3-2560" },
      "pageSetup": { "paperKind": "A4", "orientation": "Portrait", "margins": { "top": 25, "bottom": 25, "left": 25, "right": 25 } },
      "parameters": [
        { "name": "SellerName", "type": "string", "defaultValue": "บริษัท บางพลานิกซ์ เทคโนโลยี จำกัด" },
        { "name": "TaxDocNo", "type": "string", "defaultValue": "TAX-2026-9901" }
      ],
      "datasets": [{ "name": "items", "source": "json" }],
      "bands": {
        "title": {
          "height": 75,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 300, "height": 22 }, "expression": "=Parameters!SellerName.Value", "fontSize": 14, "fontWeight": "Bold", "textColor": "#0f172a" },
            { "type": "text", "bounds": { "x": 0, "y": 22, "width": 300, "height": 16 }, "content": "เลขประจำตัวผู้เสียภาษี: 0105567012345 (สำนักงานใหญ่)", "fontSize": 9.5, "textColor": "#475569" },
            { "type": "text", "bounds": { "x": 300, "y": 0, "width": 235, "height": 24 }, "content": "ใบกำกับภาษี / ใบเสร็จรับเงิน", "fontSize": 14, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#1e3a8a" },
            { "type": "text", "bounds": { "x": 300, "y": 24, "width": 235, "height": 16 }, "expression": "=\"เลขที่: \" + Parameters!TaxDocNo.Value", "fontSize": 10, "textAlign": "Right", "fontWeight": "Bold" },
            { "type": "line", "bounds": { "x": 0, "y": 60, "width": 535, "height": 1 }, "strokeColor": "#cbd5e1", "strokeWidth": 1.5 }
          ]
        },
        "detail": {
          "dataset": "items",
          "height": 26,
          "elements": [
            { "type": "text", "bounds": { "x": 10, "y": 5, "width": 280, "height": 16 }, "expression": "=Fields!name.Value", "fontSize": 10 },
            { "type": "text", "bounds": { "x": 300, "y": 5, "width": 40, "height": 16 }, "expression": "=Fields!qty.Value", "fontSize": 10, "textAlign": "Center" },
            { "type": "text", "bounds": { "x": 350, "y": 5, "width": 85, "height": 16 }, "expression": "=Fields!price.Value", "fontSize": 10, "textAlign": "Right" },
            { "type": "text", "bounds": { "x": 440, "y": 5, "width": 85, "height": 16 }, "expression": "=Fields!total.Value", "fontSize": 10, "textAlign": "Right", "fontWeight": "Bold" }
          ]
        },
        "pageFooter": {
          "height": 45,
          "elements": [
            { "type": "line", "bounds": { "x": 0, "y": 2, "width": 535, "height": 1 }, "strokeColor": "#94a3b8", "strokeWidth": 1 },
            { "type": "text", "bounds": { "x": 0, "y": 10, "width": 300, "height": 18 }, "content": "ยอดรวมทั้งสิ้น: หนึ่งแสนหนึ่งหมื่นเจ็ดพันเจ็ดร้อยบาทถ้วน", "fontSize": 10, "fontWeight": "Bold", "textColor": "#1e3a8a" },
            { "type": "text", "bounds": { "x": 320, "y": 10, "width": 205, "height": 18 }, "content": "รวมสุทธิ: 117,700.00 บาท", "fontSize": 11, "fontWeight": "Bold", "textAlign": "Right" }
          ]
        }
      }
    },
    data: {
      items: [
        { name: "Bangplanix Enterprise Engine Core License", qty: 1, price: "85,000.00", total: "85,000.00" },
        { name: "e-Tax Invoice Cryptographic Signing Module", qty: 1, price: "25,000.00", total: "25,000.00" }
      ]
    }
  },
  pos_receipt: {
    template: {
      "$schema": "https://bangplanix.io/schemas/v1/bangplanix.schema.json",
      "version": "1.0.0",
      "metadata": { "title": "POS Thermal Receipt 80mm", "standard": "ESC/POS" },
      "pageSetup": { "paperKind": "Custom", "width": 80, "height": 180, "margins": { "top": 5, "bottom": 5, "left": 5, "right": 5 } },
      "parameters": [
        { "name": "Store", "type": "string", "defaultValue": "BANGPLANIX COFFEE" },
        { "name": "OrderNo", "type": "string", "defaultValue": "ORD-9402" }
      ],
      "datasets": [{ "name": "items", "source": "json" }],
      "bands": {
        "title": {
          "height": 45,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 70, "height": 16 }, "expression": "=Parameters!Store.Value", "fontSize": 11, "fontWeight": "Bold", "textAlign": "Center" },
            { "type": "text", "bounds": { "x": 0, "y": 16, "width": 70, "height": 12 }, "expression": "=\"Order: \" + Parameters!OrderNo.Value", "fontSize": 8, "textAlign": "Center", "textColor": "#64748b" },
            { "type": "line", "bounds": { "x": 0, "y": 36, "width": 70, "height": 1 }, "strokeColor": "#000000", "strokeWidth": 1 }
          ]
        },
        "detail": {
          "dataset": "items",
          "height": 16,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 2, "width": 45, "height": 12 }, "expression": "=Fields!name.Value", "fontSize": 7.5 },
            { "type": "text", "bounds": { "x": 45, "y": 2, "width": 10, "height": 12 }, "expression": "=\"x\" + Fields!qty.Value", "fontSize": 7.5, "textAlign": "Center" },
            { "type": "text", "bounds": { "x": 55, "y": 2, "width": 15, "height": 12 }, "expression": "=Fields!price.Value", "fontSize": 7.5, "textAlign": "Right" }
          ]
        },
        "pageFooter": {
          "height": 40,
          "elements": [
            { "type": "line", "bounds": { "x": 0, "y": 2, "width": 70, "height": 1 }, "strokeColor": "#000000", "strokeWidth": 1 },
            { "type": "text", "bounds": { "x": 0, "y": 6, "width": 40, "height": 14 }, "content": "TOTAL:", "fontSize": 9, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 40, "y": 6, "width": 30, "height": 14 }, "content": "345.00", "fontSize": 9, "fontWeight": "Bold", "textAlign": "Right" },
            { "type": "barcode", "bounds": { "x": 10, "y": 22, "width": 50, "height": 16 }, "expression": "=Parameters!OrderNo.Value", "symbology": "Code128" }
          ]
        }
      }
    },
    data: {
      items: [
        { name: "Iced Caramel Macchiato", qty: 1, price: "125.00" },
        { name: "Hot Americano (Medium)", qty: 1, price: "95.00" },
        { name: "Almond Croissant", qty: 1, price: "125.00" }
      ]
    }
  },
  shipping_label: {
    template: {
      "$schema": "https://bangplanix.io/schemas/v1/bangplanix.schema.json",
      "version": "1.0.0",
      "metadata": { "title": "Logistics & Shipping Label 4x6", "standard": "GS1 / Zebra ZPL II" },
      "pageSetup": { "paperKind": "Custom", "width": 101.6, "height": 152.4, "margins": { "top": 5, "bottom": 5, "left": 5, "right": 5 } },
      "parameters": [
        { "name": "TrackingNo", "type": "string", "defaultValue": "BPX-TH-882910482" },
        { "name": "Recipient", "type": "string", "defaultValue": "Somchai Tech Solutions Co., Ltd." }
      ],
      "bands": {
        "title": {
          "height": 130,
          "elements": [
            { "type": "rectangle", "bounds": { "x": 0, "y": 0, "width": 91.6, "height": 20 }, "fillColor": "#0f172a" },
            { "type": "text", "bounds": { "x": 4, "y": 3, "width": 50, "height": 14 }, "content": "EXPRESS NEXT-DAY", "fontSize": 10, "fontWeight": "Bold", "textColor": "#ffffff" },
            { "type": "text", "bounds": { "x": 55, "y": 3, "width": 32, "height": 14 }, "content": "BKK-01", "fontSize": 9, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#38bdf8" },
            { "type": "text", "bounds": { "x": 2, "y": 26, "width": 88, "height": 12 }, "content": "SHIP TO:", "fontSize": 8, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 2, "y": 38, "width": 88, "height": 14 }, "expression": "=Parameters!Recipient.Value", "fontSize": 9, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 2, "y": 52, "width": 88, "height": 12 }, "content": "88/12 Sukhumvit 71 Road, Phra Khanong, Bangkok 10110", "fontSize": 7.5 },
            { "type": "line", "bounds": { "x": 0, "y": 70, "width": 91.6, "height": 1.5 }, "strokeColor": "#000000", "strokeWidth": 1.5 },
            { "type": "barcode", "bounds": { "x": 10, "y": 78, "width": 71.6, "height": 30 }, "expression": "=Parameters!TrackingNo.Value", "symbology": "Code128" },
            { "type": "text", "bounds": { "x": 0, "y": 112, "width": 91.6, "height": 12 }, "expression": "=Parameters!TrackingNo.Value", "fontSize": 8.5, "textAlign": "Center", "fontWeight": "Bold" }
          ]
        }
      }
    },
    data: {}
  },
  payslip: {
    template: {
      "$schema": "https://bangplanix.io/schemas/v1/bangplanix.schema.json",
      "version": "1.0.0",
      "metadata": { "title": "ใบจ่ายเงินเดือนพนักงาน (Payslip)", "standard": "HR-PAYSLIP" },
      "pageSetup": { "paperKind": "A5", "orientation": "Landscape", "margins": { "top": 15, "bottom": 15, "left": 15, "right": 15 } },
      "parameters": [
        { "name": "Company", "type": "string", "defaultValue": "บริษัท บางพลานิกซ์ เทคโนโลยี จำกัด" },
        { "name": "EmpName", "type": "string", "defaultValue": "นายสมศักดิ์ นวัตกรรม" }
      ],
      "bands": {
        "title": {
          "height": 90,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 100, "height": 18 }, "expression": "=Parameters!Company.Value", "fontSize": 12, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 110, "y": 0, "width": 70, "height": 18 }, "content": "PAYSLIP (กันยายน 2569)", "fontSize": 10, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#1e3a8a" },
            { "type": "line", "bounds": { "x": 0, "y": 24, "width": 180, "height": 1 }, "strokeColor": "#cbd5e1", "strokeWidth": 1 },
            { "type": "text", "bounds": { "x": 0, "y": 30, "width": 180, "height": 12 }, "expression": "=\"พนักงาน: \" + Parameters!EmpName.Value + \" (Senior Systems Architect)\"", "fontSize": 9, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 0, "y": 50, "width": 80, "height": 14 }, "content": "เงินเดือนพื้นฐาน: 95,000.00", "fontSize": 8.5 },
            { "type": "text", "bounds": { "x": 90, "y": 50, "width": 90, "height": 14 }, "content": "หักภาษี + ปกส.: 9,200.00", "fontSize": 8.5 },
            { "type": "line", "bounds": { "x": 0, "y": 70, "width": 180, "height": 1 }, "strokeColor": "#0f172a", "strokeWidth": 1.5 },
            { "type": "text", "bounds": { "x": 0, "y": 74, "width": 90, "height": 14 }, "content": "ยอดเงินรับสุทธิ (NET):", "fontSize": 9.5, "fontWeight": "Bold", "textColor": "#1e3a8a" },
            { "type": "text", "bounds": { "x": 90, "y": 74, "width": 90, "height": 14 }, "content": "85,800.00 บาท", "fontSize": 10, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#16a34a" }
          ]
        }
      }
    },
    data: {}
  }
};

export class BangplanixPlayground {
  constructor() {
    this.engine = new BangplanixWasmEngine();
    this.currentSample = 'invoice';
    this.init();
  }

  init() {
    const editor = document.getElementById('bpx-editor');
    const previewContainer = document.getElementById('preview-canvas-wrapper');
    const sampleSelect = document.getElementById('sample-select');
    const themeBtn = document.getElementById('theme-toggle-btn');
    const exportBtn = document.getElementById('export-svg-btn');
    const errorBanner = document.getElementById('error-banner');

    if (editor) {
      editor.value = JSON.stringify(SAMPLES.invoice.template, null, 2);
      editor.addEventListener('input', () => this.render());
    }

    if (sampleSelect) {
      sampleSelect.addEventListener('change', (e) => {
        const key = e.target.value;
        if (SAMPLES[key]) {
          this.currentSample = key;
          editor.value = JSON.stringify(SAMPLES[key].template, null, 2);
          this.render();
        }
      });
    }

    if (themeBtn) {
      themeBtn.addEventListener('click', () => {
        const isLight = document.body.getAttribute('data-theme') === 'light';
        document.body.setAttribute('data-theme', isLight ? 'dark' : 'light');
        themeBtn.textContent = isLight ? '🌙 Dark' : '☀️ Light';
      });
    }

    if (exportBtn) {
      exportBtn.addEventListener('click', () => this.downloadSvg());
    }

    this.render();
  }

  render() {
    const editor = document.getElementById('bpx-editor');
    const previewContainer = document.getElementById('preview-canvas-wrapper');
    const errorBanner = document.getElementById('error-banner');

    if (!editor || !previewContainer) return;

    try {
      const template = JSON.parse(editor.value);
      const data = SAMPLES[this.currentSample]?.data || {};
      const svg = this.engine.renderSvg(template, data);

      previewContainer.innerHTML = svg;
      if (errorBanner) errorBanner.style.display = 'none';
    } catch (err) {
      if (errorBanner) {
        errorBanner.textContent = `Error: ${err.message}`;
        errorBanner.style.display = 'block';
      }
    }
  }

  downloadSvg() {
    const svgEl = document.querySelector('#preview-canvas-wrapper svg');
    if (!svgEl) return;
    const blob = new Blob([svgEl.outerHTML], { type: 'image/svg+xml;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${this.currentSample}-preview.svg`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
