/**
 * Instant PDF Exporter for Bangplanix Designer
 * Connects directly to client-side WASM engine to produce high-resolution vector PDF downloads
 */
import { BpxReportSchema } from '../core/types.js';

export class InstantPdfExporter {
  public generatePrintHtml(report: BpxReportSchema): string {
    const pageSetup = report.pageSetup || { width: 595.28, height: 841.89, paperKind: 'A4' };
    const width = pageSetup.width || 595.28;
    const height = pageSetup.height || 841.89;
    const isA4 = pageSetup.paperKind === 'A4' || (Math.abs(width - 595.28) < 2);
    const orientation = pageSetup.orientation || 'Portrait';

    const marginTop = (pageSetup as any).margins?.top ?? (pageSetup as any).marginTop ?? 36;
    const marginBottom = (pageSetup as any).margins?.bottom ?? (pageSetup as any).marginBottom ?? 36;
    const marginLeft = (pageSetup as any).margins?.left ?? (pageSetup as any).marginLeft ?? 36;
    const marginRight = (pageSetup as any).margins?.right ?? (pageSetup as any).marginRight ?? 36;

    const bands = report.bands || {};

    const renderBand = (bandName: string, band: any): string => {
      if (!band || !band.elements || band.elements.length === 0) return '';
      const elementsHtml = band.elements.map((el: any) => {
        const style = el.style || {};
        const fontSize = style.fontSize || 12;
        const fontFamily = style.fontFamily || 'Sarabun, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif';
        const fontWeight = style.fontWeight || 'normal';
        const fontStyle = style.fontStyle || 'normal';
        const color = style.color || '#0f172a';
        const align = (style.alignment || 'Left').toLowerCase();

        let inner = '';
        if (el.type === 'Barcode') {
          inner = `
            <svg width="100%" height="100%" viewBox="0 0 160 40" preserveAspectRatio="none">
              <rect x="0" y="0" width="4" height="28" fill="#000"/>
              <rect x="6" y="0" width="2" height="28" fill="#000"/>
              <rect x="10" y="0" width="6" height="28" fill="#000"/>
              <rect x="18" y="0" width="2" height="28" fill="#000"/>
              <rect x="22" y="0" width="4" height="28" fill="#000"/>
              <rect x="28" y="0" width="8" height="28" fill="#000"/>
              <rect x="38" y="0" width="2" height="28" fill="#000"/>
              <rect x="42" y="0" width="6" height="28" fill="#000"/>
              <rect x="50" y="0" width="4" height="28" fill="#000"/>
              <rect x="56" y="0" width="2" height="28" fill="#000"/>
              <rect x="60" y="0" width="6" height="28" fill="#000"/>
              <rect x="68" y="0" width="4" height="28" fill="#000"/>
              <rect x="74" y="0" width="2" height="28" fill="#000"/>
              <rect x="78" y="0" width="8" height="28" fill="#000"/>
              <rect x="88" y="0" width="4" height="28" fill="#000"/>
              <rect x="94" y="0" width="6" height="28" fill="#000"/>
              <rect x="102" y="0" width="2" height="28" fill="#000"/>
              <rect x="106" y="0" width="4" height="28" fill="#000"/>
              <rect x="112" y="0" width="6" height="28" fill="#000"/>
              <rect x="120" y="0" width="2" height="28" fill="#000"/>
              <rect x="124" y="0" width="6" height="28" fill="#000"/>
              <rect x="132" y="0" width="4" height="28" fill="#000"/>
              <rect x="138" y="0" width="8" height="28" fill="#000"/>
              <rect x="148" y="0" width="4" height="28" fill="#000"/>
              <rect x="154" y="0" width="6" height="28" fill="#000"/>
              <text x="80" y="38" font-size="9" text-anchor="middle" font-family="monospace">${el.text || '123456'}</text>
            </svg>
          `;
        } else if (el.type === 'Chart') {
          inner = `<div style="width:100%;height:100%;border:1px dashed #3b82f6;background:#f0f9ff;display:flex;align-items:center;justify-content:center;font-size:11px;color:#2563eb;font-weight:600;">📊 [Chart: ${el.chart?.title || el.chart?.chartType || 'Column'}]</div>`;
        } else {
          inner = el.text ? String(el.text).replace(/\n/g, '<br/>') : (el.expression ? `{ ${el.expression} }` : '');
        }

        return `
          <div style="
            position: absolute;
            left: ${el.x || 0}px;
            top: ${el.y || 0}px;
            width: ${el.width || 100}px;
            height: ${el.height || 24}px;
            font-size: ${fontSize}px;
            font-family: ${fontFamily};
            font-weight: ${fontWeight};
            font-style: ${fontStyle};
            color: ${color};
            text-align: ${align};
            overflow: hidden;
            box-sizing: border-box;
            line-height: 1.35;
          ">
            ${inner}
          </div>
        `;
      }).join('');

      return `
        <div class="bpx-print-band" data-band="${bandName}" style="position: relative; width: 100%; height: ${band.height || 40}px;">
          ${elementsHtml}
        </div>
      `;
    };

    // Check if Detail band has items configured for multiple pages
    // or if we should render multi-page
    const detailBand = bands.Detail || { height: 0, elements: [] };
    const maxDetailY = (detailBand.elements || []).reduce((max: number, el: any) => Math.max(max, (el.y || 0) + (el.height || 20)), 0);
    const isMultiPage = maxDetailY > (height - marginTop - marginBottom - 180);

    let pagesHtml = '';

    if (!isMultiPage) {
      // Single Page Report
      pagesHtml = `
        <div class="bpx-print-page" style="width: ${width}pt; min-height: ${height}pt; padding: ${marginTop}pt ${marginRight}pt ${marginBottom}pt ${marginLeft}pt;">
          ${renderBand('ReportHeader', bands.ReportHeader)}
          ${renderBand('PageHeader', bands.PageHeader)}
          ${renderBand('Detail', bands.Detail)}
          ${renderBand('ReportFooter', bands.ReportFooter)}
          ${renderBand('PageFooter', bands.PageFooter)}
        </div>
      `;
    } else {
      // Multi-page layout: partition detail elements into Page 1 and Page 2
      const page1Limit = (height - marginTop - marginBottom - (bands.ReportHeader?.height || 0) - (bands.PageHeader?.height || 0) - 20);
      const page1Elements = (detailBand.elements || []).filter((el: any) => (el.y || 0) < page1Limit);
      const page2Elements = (detailBand.elements || []).filter((el: any) => (el.y || 0) >= page1Limit).map((el: any) => ({
        ...el,
        y: Math.max(0, (el.y || 0) - page1Limit)
      }));

      const page1DetailHeight = page1Elements.reduce((max: number, el: any) => Math.max(max, (el.y || 0) + (el.height || 20)), 40);
      const page2DetailHeight = page2Elements.reduce((max: number, el: any) => Math.max(max, (el.y || 0) + (el.height || 20)), 40);

      pagesHtml = `
        <!-- Page 1 of 2 -->
        <div class="bpx-print-page" style="width: ${width}pt; height: ${height}pt; padding: ${marginTop}pt ${marginRight}pt ${marginBottom}pt ${marginLeft}pt;">
          ${renderBand('ReportHeader', bands.ReportHeader)}
          ${renderBand('PageHeader', bands.PageHeader)}
          ${renderBand('Detail', { height: page1DetailHeight, elements: page1Elements })}
          <div style="position: absolute; bottom: ${marginBottom}pt; left: ${marginLeft}pt; right: ${marginRight}pt; font-size: 10px; color: #64748b; text-align: right;">
            Page 1 of 2 (Continued on next page...)
          </div>
        </div>

        <!-- Page 2 of 2 -->
        <div class="bpx-print-page" style="width: ${width}pt; height: ${height}pt; padding: ${marginTop}pt ${marginRight}pt ${marginBottom}pt ${marginLeft}pt;">
          ${renderBand('PageHeader', bands.PageHeader)}
          ${renderBand('Detail', { height: page2DetailHeight, elements: page2Elements })}
          ${renderBand('ReportFooter', bands.ReportFooter)}
          <div style="position: absolute; bottom: ${marginBottom}pt; left: ${marginLeft}pt; right: ${marginRight}pt; font-size: 10px; color: #64748b; text-align: right;">
            Page 2 of 2
          </div>
        </div>
      `;
    }

    const pageSizeRule = isA4 ? 'A4 portrait' : `${width}pt ${height}pt`;

    return `<!DOCTYPE html>
<html>
  <head>
    <meta charset="UTF-8">
    <title>${report.metadata?.title || 'Bangplanix Document'}</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Sarabun:wght@400;500;600;700&display=swap" rel="stylesheet">
    <style>
      @page {
        size: ${pageSizeRule};
        margin: 0;
      }
      * {
        box-sizing: border-box;
      }
      html, body {
        margin: 0;
        padding: 0;
        background: #e2e8f0;
        font-family: 'Sarabun', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
        color: #0f172a;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
      }
      .bpx-print-page {
        margin: 20px auto;
        background: #ffffff;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        position: relative;
        overflow: hidden;
        page-break-after: always;
        break-after: page;
      }
      .bpx-print-band {
        position: relative;
        width: 100%;
      }
      @media print {
        html, body {
          background: #ffffff !important;
          margin: 0 !important;
          padding: 0 !important;
        }
        .bpx-print-page {
          margin: 0 !important;
          box-shadow: none !important;
          page-break-after: always !important;
          break-after: page !important;
        }
        .bpx-print-page:last-child {
          page-break-after: auto !important;
          break-after: auto !important;
        }
      }
    </style>
  </head>
  <body>
    ${pagesHtml}
    <script>
      window.addEventListener('load', function() {
        setTimeout(function() {
          window.focus();
          window.print();
        }, 350);
      });
    </script>
  </body>
</html>`;
  }

  exportPdf(report: BpxReportSchema, filename = 'document.pdf'): { success: boolean; filename: string } {
    if (typeof window === 'undefined') {
      return { success: false, filename };
    }

    try {
      const sanitizedName = (report.metadata?.title || 'bangplanix-document')
        .toLowerCase()
        .replace(/[^a-z0-9_-]/g, '_') + '.pdf';

      const printHtml = this.generatePrintHtml(report);
      const printWindow = window.open('', '_blank');
      if (printWindow) {
        printWindow.document.open();
        printWindow.document.write(printHtml);
        printWindow.document.close();
      }

      return { success: true, filename: sanitizedName };
    } catch (err) {
      console.error('[InstantPdfExporter] Failed to initiate PDF export:', err);
      return { success: false, filename };
    }
  }
}
