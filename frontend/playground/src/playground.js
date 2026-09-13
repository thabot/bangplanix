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
      "pageSetup": { "paperKind": "Custom", "width": 280, "height": 480, "margins": { "top": 15, "bottom": 15, "left": 15, "right": 15 } },
      "parameters": [
        { "name": "Store", "type": "string", "defaultValue": "BANGPLANIX COFFEE" },
        { "name": "OrderNo", "type": "string", "defaultValue": "ORD-9402" }
      ],
      "datasets": [{ "name": "items", "source": "json" }],
      "bands": {
        "title": {
          "height": 48,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 250, "height": 18 }, "expression": "=Parameters!Store.Value", "fontSize": 13, "fontWeight": "Bold", "textAlign": "Center" },
            { "type": "text", "bounds": { "x": 0, "y": 20, "width": 250, "height": 14 }, "expression": "=\"Order: \" + Parameters!OrderNo.Value", "fontSize": 9, "textAlign": "Center", "textColor": "#64748b" },
            { "type": "line", "bounds": { "x": 0, "y": 40, "width": 250, "height": 1 }, "strokeColor": "#000000", "strokeWidth": 1 }
          ]
        },
        "detail": {
          "dataset": "items",
          "height": 20,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 3, "width": 150, "height": 14 }, "expression": "=Fields!name.Value", "fontSize": 8.5 },
            { "type": "text", "bounds": { "x": 150, "y": 3, "width": 35, "height": 14 }, "expression": "=\"x\" + Fields!qty.Value", "fontSize": 8.5, "textAlign": "Center" },
            { "type": "text", "bounds": { "x": 185, "y": 3, "width": 65, "height": 14 }, "expression": "=Fields!price.Value", "fontSize": 8.5, "textAlign": "Right" }
          ]
        },
        "pageFooter": {
          "height": 65,
          "elements": [
            { "type": "line", "bounds": { "x": 0, "y": 2, "width": 250, "height": 1 }, "strokeColor": "#000000", "strokeWidth": 1 },
            { "type": "text", "bounds": { "x": 0, "y": 8, "width": 100, "height": 16 }, "content": "TOTAL:", "fontSize": 11, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 110, "y": 8, "width": 140, "height": 16 }, "content": "345.00", "fontSize": 11, "fontWeight": "Bold", "textAlign": "Right" },
            { "type": "barcode", "bounds": { "x": 25, "y": 30, "width": 200, "height": 32 }, "expression": "=Parameters!OrderNo.Value", "symbology": "Code128" }
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
      "pageSetup": { "paperKind": "Custom", "width": 288, "height": 432, "margins": { "top": 15, "bottom": 15, "left": 15, "right": 15 } },
      "parameters": [
        { "name": "TrackingNo", "type": "string", "defaultValue": "BPX-TH-882910482" },
        { "name": "Recipient", "type": "string", "defaultValue": "Somchai Tech Solutions Co., Ltd." }
      ],
      "bands": {
        "title": {
          "height": 200,
          "elements": [
            { "type": "rectangle", "bounds": { "x": 0, "y": 0, "width": 258, "height": 28 }, "fillColor": "#0f172a" },
            { "type": "text", "bounds": { "x": 10, "y": 6, "width": 140, "height": 16 }, "content": "EXPRESS NEXT-DAY", "fontSize": 11, "fontWeight": "Bold", "textColor": "#ffffff" },
            { "type": "text", "bounds": { "x": 155, "y": 6, "width": 93, "height": 16 }, "content": "BKK-01", "fontSize": 11, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#38bdf8" },
            { "type": "text", "bounds": { "x": 5, "y": 36, "width": 248, "height": 14 }, "content": "SHIP TO:", "fontSize": 9, "fontWeight": "Bold", "textColor": "#64748b" },
            { "type": "text", "bounds": { "x": 5, "y": 52, "width": 248, "height": 18 }, "expression": "=Parameters!Recipient.Value", "fontSize": 11, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 5, "y": 72, "width": 248, "height": 16 }, "content": "88/12 Sukhumvit 71 Road, Phra Khanong, Bangkok 10110", "fontSize": 9.5 },
            { "type": "line", "bounds": { "x": 0, "y": 98, "width": 258, "height": 1.5 }, "strokeColor": "#000000", "strokeWidth": 1.5 },
            { "type": "barcode", "bounds": { "x": 14, "y": 110, "width": 230, "height": 55 }, "expression": "=Parameters!TrackingNo.Value", "symbology": "Code128", "showBarcodeText": false },
            { "type": "text", "bounds": { "x": 0, "y": 172, "width": 258, "height": 16 }, "expression": "=Parameters!TrackingNo.Value", "fontSize": 11, "textAlign": "Center", "fontWeight": "Bold" }
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
      "pageSetup": { "paperKind": "A5", "orientation": "Landscape", "margins": { "top": 25, "bottom": 25, "left": 25, "right": 25 } },
      "parameters": [
        { "name": "Company", "type": "string", "defaultValue": "บริษัท บางพลานิกซ์ เทคโนโลยี จำกัด" },
        { "name": "EmpName", "type": "string", "defaultValue": "นายสมศักดิ์ นวัตกรรม" }
      ],
      "bands": {
        "title": {
          "height": 170,
          "elements": [
            { "type": "text", "bounds": { "x": 0, "y": 0, "width": 320, "height": 24 }, "expression": "=Parameters!Company.Value", "fontSize": 14, "fontWeight": "Bold" },
            { "type": "text", "bounds": { "x": 320, "y": 0, "width": 225, "height": 24 }, "content": "PAYSLIP (กันยายน 2569)", "fontSize": 12, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#1e3a8a" },
            { "type": "line", "bounds": { "x": 0, "y": 30, "width": 545, "height": 1 }, "strokeColor": "#cbd5e1", "strokeWidth": 1 },
            { "type": "text", "bounds": { "x": 0, "y": 38, "width": 545, "height": 18 }, "expression": "=\"พนักงาน: \" + Parameters!EmpName.Value + \" (Senior Systems Architect)\"", "fontSize": 11, "fontWeight": "Bold" },
            { "type": "rectangle", "bounds": { "x": 0, "y": 64, "width": 545, "height": 42 }, "fillColor": "#f8fafc", "borderColor": "#e2e8f0" },
            { "type": "text", "bounds": { "x": 15, "y": 76, "width": 240, "height": 18 }, "content": "เงินเดือนพื้นฐาน: 95,000.00 บาท", "fontSize": 10 },
            { "type": "text", "bounds": { "x": 275, "y": 76, "width": 255, "height": 18 }, "content": "หักภาษี + ปกส.: 9,200.00 บาท", "fontSize": 10, "textAlign": "Right", "textColor": "#dc2626" },
            { "type": "line", "bounds": { "x": 0, "y": 118, "width": 545, "height": 1.5 }, "strokeColor": "#0f172a", "strokeWidth": 1.5 },
            { "type": "text", "bounds": { "x": 0, "y": 130, "width": 250, "height": 20 }, "content": "ยอดเงินรับสุทธิ (NET):", "fontSize": 13, "fontWeight": "Bold", "textColor": "#1e3a8a" },
            { "type": "text", "bounds": { "x": 275, "y": 130, "width": 270, "height": 20 }, "content": "85,800.00 บาท", "fontSize": 13, "fontWeight": "Bold", "textAlign": "Right", "textColor": "#16a34a" }
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
    this.zoomLevel = 100;
    this.zoomMode = 'manual';
    this.searchMatches = [];
    this.searchIndex = 0;
    this.isFullscreen = false;
    this.init();
  }

  init() {
    const editor = document.getElementById('bpx-editor');
    const sampleSelect = document.getElementById('sample-select');
    const themeBtn = document.getElementById('theme-toggle-btn');
    const exportBtn = document.getElementById('export-svg-btn');

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

    // Viewer Toolbar Events
    const zoomInBtn = document.getElementById('zoom-in-btn');
    const zoomOutBtn = document.getElementById('zoom-out-btn');
    const zoomSelect = document.getElementById('zoom-select');
    const fitWidthBtn = document.getElementById('fit-width-btn');
    const fitPageBtn = document.getElementById('fit-page-btn');
    const printBtn = document.getElementById('print-btn');
    const searchBtn = document.getElementById('search-btn');
    const searchCloseBtn = document.getElementById('search-close-btn');
    const searchInput = document.getElementById('search-input');
    const searchNextBtn = document.getElementById('search-next-btn');
    const searchPrevBtn = document.getElementById('search-prev-btn');
    const fullscreenBtn = document.getElementById('fullscreen-btn');

    if (zoomInBtn) {
      zoomInBtn.addEventListener('click', () => {
        this.zoomMode = 'manual';
        this.zoomLevel = Math.min(500, this.zoomLevel + 25);
        this.applyZoom();
      });
    }

    if (zoomOutBtn) {
      zoomOutBtn.addEventListener('click', () => {
        this.zoomMode = 'manual';
        this.zoomLevel = Math.max(25, this.zoomLevel - 25);
        this.applyZoom();
      });
    }

    if (zoomSelect) {
      zoomSelect.addEventListener('change', (e) => {
        const val = e.target.value;
        if (val === 'fit-width') {
          this.zoomMode = 'fit-width';
        } else if (val === 'fit-page') {
          this.zoomMode = 'fit-page';
        } else {
          this.zoomMode = 'manual';
          this.zoomLevel = parseInt(val, 10) || 100;
        }
        this.applyZoom();
      });
    }

    if (fitWidthBtn) {
      fitWidthBtn.addEventListener('click', () => {
        this.zoomMode = 'fit-width';
        this.applyZoom();
      });
    }

    if (fitPageBtn) {
      fitPageBtn.addEventListener('click', () => {
        this.zoomMode = 'fit-page';
        this.applyZoom();
      });
    }

    if (printBtn) {
      printBtn.addEventListener('click', () => window.print());
    }

    if (searchBtn) {
      searchBtn.addEventListener('click', () => this.toggleSearch());
    }

    if (searchCloseBtn) {
      searchCloseBtn.addEventListener('click', () => this.toggleSearch(false));
    }

    if (searchInput) {
      searchInput.addEventListener('input', (e) => this.executeSearch(e.target.value));
      searchInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          if (e.shiftKey) this.searchPrev();
          else this.searchNext();
        } else if (e.key === 'Escape') {
          this.toggleSearch(false);
        }
      });
    }

    if (searchNextBtn) {
      searchNextBtn.addEventListener('click', () => this.searchNext());
    }

    if (searchPrevBtn) {
      searchPrevBtn.addEventListener('click', () => this.searchPrev());
    }

    if (fullscreenBtn) {
      fullscreenBtn.addEventListener('click', () => this.toggleFullscreen());
    }

    // Shortcut for Ctrl+F search and Escape
    window.addEventListener('keydown', (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'f') {
        const previewPane = document.getElementById('preview-pane');
        if (previewPane) {
          e.preventDefault();
          this.toggleSearch(true);
        }
      } else if (e.key === 'Escape' && this.isFullscreen) {
        this.toggleFullscreen(false);
      }
    });

    window.addEventListener('resize', () => {
      if (this.zoomMode !== 'manual') {
        this.applyZoom();
      }
    });

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
      this.applyZoom();
    } catch (err) {
      if (errorBanner) {
        errorBanner.textContent = `Error: ${err.message}`;
        errorBanner.style.display = 'block';
      }
    }
  }

  applyZoom() {
    const wrapper = document.getElementById('preview-canvas-wrapper');
    const scrollArea = document.getElementById('preview-scroll-area');
    const zoomSelect = document.getElementById('zoom-select');
    const fitWidthBtn = document.getElementById('fit-width-btn');
    const fitPageBtn = document.getElementById('fit-page-btn');
    const svgEl = wrapper?.querySelector('svg');
    if (!wrapper || !svgEl) return;

    if (this.zoomMode === 'fit-width' && scrollArea) {
      const availableWidth = Math.max(100, scrollArea.clientWidth - 48);
      const naturalWidth = parseFloat(svgEl.getAttribute('width')) || svgEl.viewBox?.baseVal?.width || 595;
      this.zoomLevel = Math.max(25, Math.min(500, Math.round((availableWidth / naturalWidth) * 100)));
    } else if (this.zoomMode === 'fit-page' && scrollArea) {
      const availableHeight = Math.max(100, scrollArea.clientHeight - 48);
      const naturalHeight = parseFloat(svgEl.getAttribute('height')) || svgEl.viewBox?.baseVal?.height || 842;
      this.zoomLevel = Math.max(25, Math.min(500, Math.round((availableHeight / naturalHeight) * 100)));
    }

    wrapper.style.transform = `scale(${this.zoomLevel / 100})`;
    wrapper.style.transformOrigin = 'top center';

    if (zoomSelect) {
      if (this.zoomMode === 'manual') {
        zoomSelect.value = String(this.zoomLevel);
      } else {
        zoomSelect.value = this.zoomMode;
      }
    }

    if (fitWidthBtn) {
      fitWidthBtn.classList.toggle('active', this.zoomMode === 'fit-width');
    }
    if (fitPageBtn) {
      fitPageBtn.classList.toggle('active', this.zoomMode === 'fit-page');
    }
  }

  toggleSearch(explicitOpen) {
    const panel = document.getElementById('search-panel');
    const input = document.getElementById('search-input');
    const searchBtn = document.getElementById('search-btn');
    if (!panel) return;

    const shouldOpen = explicitOpen !== undefined ? explicitOpen : panel.style.display === 'none';
    panel.style.display = shouldOpen ? 'flex' : 'none';
    if (searchBtn) searchBtn.classList.toggle('active', shouldOpen);

    if (shouldOpen && input) {
      input.focus();
      input.select();
      if (input.value) this.executeSearch(input.value);
    } else {
      this.clearSearch();
    }
  }

  executeSearch(query) {
    this.clearSearch();
    const wrapper = document.getElementById('preview-canvas-wrapper');
    const counter = document.getElementById('search-counter');
    if (!wrapper || !query || !query.trim()) {
      if (counter) counter.textContent = '';
      return;
    }

    const q = query.trim().toLowerCase();
    const textNodes = Array.from(wrapper.querySelectorAll('text'));
    this.searchMatches = textNodes.filter(t => t.textContent && t.textContent.toLowerCase().includes(q));
    this.searchIndex = 0;

    this.searchMatches.forEach(el => {
      el.setAttribute('data-original-fill', el.getAttribute('fill') || '#000000');
      el.setAttribute('fill', '#eab308');
      el.style.fontWeight = 'bold';
    });

    this.updateSearchUI();
  }

  updateSearchUI() {
    const counter = document.getElementById('search-counter');
    if (!counter) return;

    if (this.searchMatches.length === 0) {
      counter.textContent = '0 found';
      return;
    }

    counter.textContent = `${this.searchIndex + 1} of ${this.searchMatches.length}`;
    this.searchMatches.forEach((el, idx) => {
      if (idx === this.searchIndex) {
        el.setAttribute('fill', '#ef4444');
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      } else {
        el.setAttribute('fill', '#eab308');
      }
    });
  }

  searchNext() {
    if (this.searchMatches.length === 0) return;
    this.searchIndex = (this.searchIndex + 1) % this.searchMatches.length;
    this.updateSearchUI();
  }

  searchPrev() {
    if (this.searchMatches.length === 0) return;
    this.searchIndex = (this.searchIndex - 1 + this.searchMatches.length) % this.searchMatches.length;
    this.updateSearchUI();
  }

  clearSearch() {
    const wrapper = document.getElementById('preview-canvas-wrapper');
    if (wrapper) {
      wrapper.querySelectorAll('text[data-original-fill]').forEach(el => {
        el.setAttribute('fill', el.getAttribute('data-original-fill'));
        el.removeAttribute('data-original-fill');
        el.style.fontWeight = '';
      });
    }
    this.searchMatches = [];
    this.searchIndex = 0;
    const counter = document.getElementById('search-counter');
    if (counter) counter.textContent = '';
  }

  toggleFullscreen(explicitState) {
    const previewPane = document.getElementById('preview-pane');
    const btn = document.getElementById('fullscreen-btn');
    if (!previewPane) return;

    this.isFullscreen = explicitState !== undefined ? explicitState : !this.isFullscreen;
    previewPane.classList.toggle('fullscreen', this.isFullscreen);
    if (btn) {
      btn.textContent = this.isFullscreen ? '✕ Exit Fullscreen' : '⛶ Fullscreen';
    }
    setTimeout(() => this.applyZoom(), 50);
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
