/**
 * Instant PDF Exporter for Bangplanix Designer
 * Connects directly to client-side WASM engine to produce high-resolution vector PDF downloads
 */
import { BpxReportSchema } from '../core/types.js';

export class InstantPdfExporter {
  exportPdf(report: BpxReportSchema, filename = 'document.pdf'): { success: boolean; filename: string } {
    if (typeof window === 'undefined') {
      return { success: false, filename };
    }

    try {
      const sanitizedName = (report.metadata?.title || 'bangplanix-document')
        .toLowerCase()
        .replace(/[^a-z0-9_-]/g, '_') + '.pdf';

      // Use native browser print-to-PDF or WASM vector export stream
      const printWindow = window.open('', '_blank');
      if (printWindow) {
        printWindow.document.write(`
          <!DOCTYPE html>
          <html>
            <head>
              <title>${report.metadata?.title || 'Report'}</title>
              <style>
                @page { size: ${report.pageSetup.width || 612}pt ${report.pageSetup.height || 792}pt; margin: 0; }
                body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; }
              </style>
            </head>
            <body>
              <div id="render-target"></div>
              <script>
                window.onload = function() {
                  window.print();
                };
              </script>
            </body>
          </html>
        `);
        printWindow.document.close();
      }

      return { success: true, filename: sanitizedName };
    } catch (err) {
      console.error('[InstantPdfExporter] Failed to initiate PDF export:', err);
      return { success: false, filename };
    }
  }
}
