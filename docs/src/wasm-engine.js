/**
 * Bangplanix In-Browser WASM / Client-side Engine
 * Provides instant vector SVG & semantic HTML rendering directly in the browser
 */
export class BangplanixWasmEngine {
  constructor(options = {}) {
    this.options = options;
  }

  /**
   * Parses and validates a .bpx template string or object
   */
  parseTemplate(input) {
    if (typeof input === 'string') {
      try {
        return JSON.parse(input);
      } catch (e) {
        throw new Error(`Invalid .bpx JSON: ${e.message}`);
      }
    }
    if (typeof input === 'object' && input !== null) {
      return input;
    }
    throw new Error('Template input must be a JSON string or an object.');
  }

  /**
   * Renders a .bpx template with data and parameters into SVG
   */
  renderSvg(template, data = {}, parameters = {}) {
    const bpx = this.parseTemplate(template);
    let paperWidth = 595;
    let paperHeight = 842;
    const setup = bpx.pageSetup || {};

    if (setup.width && setup.height) {
      paperWidth = setup.width;
      paperHeight = setup.height;
    } else if (setup.paperKind === 'A5') {
      paperWidth = setup.orientation === 'Landscape' ? 595 : 420;
      paperHeight = setup.orientation === 'Landscape' ? 420 : 595;
    } else if (setup.paperKind === 'A4') {
      paperWidth = setup.orientation === 'Landscape' ? 842 : 595;
      paperHeight = setup.orientation === 'Landscape' ? 595 : 842;
    } else if (setup.orientation === 'Landscape') {
      paperWidth = 842;
      paperHeight = 595;
    }
    const margins = setup.margins || { top: 28, bottom: 28, left: 28, right: 28 };

    const mergedParams = { ...(bpx.parameters?.reduce((acc, p) => ({ ...acc, [p.name]: p.defaultValue }), {})), ...parameters };
    const context = {
      Parameters: mergedParams,
      Fields: {},
      Globals: {
        PageNumber: 1,
        TotalPages: 1,
        ExecutionTime: new Date().toISOString(),
        ReportTitle: bpx.metadata?.title || 'Bangplanix Report'
      }
    };

    let currentY = margins.top;
    let elementsSvg = '';

    // Render Title Band
    if (bpx.bands?.title) {
      const band = bpx.bands.title;
      elementsSvg += this._renderBandElements(band, context, margins.left, currentY);
      currentY += band.height || 40;
    }

    // Render PageHeader Band
    if (bpx.bands?.pageHeader) {
      const band = bpx.bands.pageHeader;
      elementsSvg += this._renderBandElements(band, context, margins.left, currentY);
      currentY += band.height || 30;
    }

    // Render Detail Band (Iterate over dataset rows)
    if (bpx.bands?.detail) {
      const band = bpx.bands.detail;
      const datasetName = band.dataset || (bpx.datasets && bpx.datasets[0]?.name) || 'items';
      const rows = Array.isArray(data[datasetName]) ? data[datasetName] : (Array.isArray(data) ? data : [data]);

      for (let i = 0; i < (rows.length || 1); i++) {
        const rowData = rows[i] || {};
        const rowContext = {
          ...context,
          Fields: rowData,
          Globals: { ...context.Globals, RowNumber: i + 1 }
        };
        elementsSvg += this._renderBandElements(band, rowContext, margins.left, currentY);
        currentY += band.height || 25;
      }
    }

    // Render PageFooter Band
    if (bpx.bands?.pageFooter) {
      const band = bpx.bands.pageFooter;
      const footerY = paperHeight - margins.bottom - (band.height || 30);
      elementsSvg += this._renderBandElements(band, context, margins.left, footerY);
    }

    return `
      <svg class="bangplanix-report-svg" width="${paperWidth}" height="${paperHeight}" viewBox="0 0 ${paperWidth} ${paperHeight}" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <style>
            @import url('https://fonts.googleapis.com/css2?family=Sarabun:wght@400;600;700&family=Inter:wght@400;600;700&display=swap');
            text { font-family: 'Sarabun', 'Inter', -apple-system, sans-serif; }
          </style>
        </defs>
        <rect width="${paperWidth}" height="${paperHeight}" fill="#ffffff" />
        ${elementsSvg}
      </svg>
    `.trim();
  }

  /**
   * Evaluates expression strings like "=Parameters!Title.Value" or "=Fields!Amount.Value * 1.07"
   */
  evaluateExpression(expr, context) {
    if (!expr || typeof expr !== 'string') return '';
    if (!expr.startsWith('=')) return expr;

    const code = expr.substring(1).trim();

    try {
      // Replace SSRS style variables
      const transpiled = code
        .replace(/Parameters!(\w+)\.Value/g, (_, name) => JSON.stringify(context.Parameters?.[name] ?? ''))
        .replace(/Fields!(\w+)\.Value/g, (_, name) => JSON.stringify(context.Fields?.[name] ?? ''))
        .replace(/Globals!(\w+)/g, (_, name) => JSON.stringify(context.Globals?.[name] ?? ''));

      // Safe JS evaluation with limited scope
      const fn = new Function('context', `return (${transpiled});`);
      const val = fn(context);
      return val !== undefined && val !== null ? val : '';
    } catch (e) {
      return expr; // fallback to raw string on error
    }
  }

  _renderBandElements(band, context, offsetX, offsetY) {
    if (!band || !Array.isArray(band.elements)) return '';

    let svg = '';
    for (const elem of band.elements) {
      const bx = offsetX + (elem.bounds?.x || 0);
      const by = offsetY + (elem.bounds?.y || 0);
      const bw = elem.bounds?.width || 100;
      const bh = elem.bounds?.height || 20;

      if (elem.type === 'text') {
        let text = elem.expression ? this.evaluateExpression(elem.expression, context) : (elem.content || '');
        if (elem.format && typeof text === 'number') {
          text = text.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }
        const fontSize = elem.fontSize || 12;
        const fontWeight = elem.fontWeight === 'Bold' ? 'bold' : 'normal';
        const fill = elem.textColor || '#1A202C';
        const anchor = elem.textAlign === 'Right' ? 'end' : (elem.textAlign === 'Center' ? 'middle' : 'start');
        const textX = elem.textAlign === 'Right' ? bx + bw : (elem.textAlign === 'Center' ? bx + bw / 2 : bx);

        svg += `<text x="${textX}" y="${by + fontSize}" font-size="${fontSize}" font-weight="${fontWeight}" fill="${fill}" text-anchor="${anchor}">${this._escapeXml(String(text))}</text>`;
      } else if (elem.type === 'line') {
        svg += `<line x1="${bx}" y1="${by}" x2="${bx + bw}" y2="${by + bh}" stroke="${elem.strokeColor || '#E2E8F0'}" stroke-width="${elem.strokeWidth || 1}" />`;
      } else if (elem.type === 'rectangle') {
        svg += `<rect x="${bx}" y="${by}" width="${bw}" height="${bh}" fill="${elem.fillColor || '#F7FAFC'}" stroke="${elem.borderColor || '#CBD5E0'}" rx="${elem.cornerRadius || 0}" />`;
      } else if (elem.type === 'barcode') {
        const val = elem.expression ? this.evaluateExpression(elem.expression, context) : (elem.content || '');
        const label = `||| [${elem.symbology || 'Code128'}: ${this._escapeXml(String(val))}] |||`;
        const fontSize = Math.min(10, Math.max(7, (bw - 8) / (label.length * 0.65)));
        svg += `
          <g transform="translate(${bx}, ${by})">
            <rect width="${bw}" height="${bh}" fill="#F8FAFC" stroke="#CBD5E1" rx="4" />
            <text x="${bw/2}" y="${bh/2 + fontSize/2.5}" font-size="${fontSize.toFixed(1)}" font-family="monospace, monospace" font-weight="600" fill="#334155" text-anchor="middle">${label}</text>
          </g>
        `;
      }
    }
    return svg;
  }

  _escapeXml(str) {
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  }
}
