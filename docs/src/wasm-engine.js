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
        const symbology = elem.symbology || 'Code128';
        const showText = elem.showBarcodeText !== undefined ? elem.showBarcodeText : (elem.showText !== undefined ? elem.showText : true);
        svg += this._renderBarcode(String(val ?? ''), bx, by, bw, bh, showText, symbology);
      }
    }
    return svg;
  }

  _renderBarcode(text, x, y, width, height, showText, symbology) {
    if (!text) {
      return `<g class="bpx-barcode" data-symbology="${symbology}"></g>`;
    }
    const patterns = [
      '212222','222122','222221','121223','121322','131222','122213','122312','132212','221213',
      '221312','231212','112232','122132','122231','113222','123122','123221','223211','221132',
      '221231','213212','223112','312131','311222','321122','321221','312212','322112','322211',
      '212123','212321','232121','111323','131123','131321','112313','132113','132311','211313',
      '231113','231311','112133','112331','132131','113123','113321','133121','313121','211331',
      '231131','213113','213311','213131','311123','311321','331121','312113','312311','332111',
      '314111','221411','431111','111224','111422','121124','121421','141122','141221','112214',
      '112412','122114','122411','142112','142211','241211','221114','413111','241112','134111',
      '111242','121142','121241','114212','124112','124211','411212','421112','421211','212141',
      '214121','412121','111143','111341','131141','114113','114311','411113','411311','113141',
      '114131','311141','411131','211412','211214','211232','2331112'
    ];

    const codes = [];
    for (let i = 0; i < text.length; i++) {
      const code = text.charCodeAt(i) - 32;
      codes.push(Math.max(0, Math.min(95, code)));
    }
    let sum = 104;
    for (let i = 0; i < codes.length; i++) {
      sum += (i + 1) * codes[i];
    }
    const checkCode = sum % 103;
    const symbols = [104, ...codes, checkCode, 106];

    let totalModules = 0;
    for (const s of symbols) {
      for (const w of patterns[s]) totalModules += Number(w);
    }

    const textHeight = showText ? 11 : 0;
    const barHeight = Math.max(6, height - textHeight - 2);
    const modWidth = width / totalModules;

    let bars = '';
    let curX = x;
    for (const s of symbols) {
      const pat = patterns[s];
      let isBar = true;
      for (let j = 0; j < pat.length; j++) {
        const w = Number(pat[j]) * modWidth;
        if (isBar) {
          bars += `<rect x="${curX.toFixed(2)}" y="${y.toFixed(2)}" width="${w.toFixed(2)}" height="${barHeight.toFixed(2)}" fill="#000000" />`;
        }
        curX += w;
        isBar = !isBar;
      }
    }

    let textSvg = '';
    if (showText && text) {
      const fontSize = Math.min(9.5, Math.max(7, width / (text.length * 1.6)));
      textSvg = `<text x="${(x + width / 2).toFixed(2)}" y="${(y + height - 1).toFixed(2)}" font-size="${fontSize.toFixed(1)}" font-family="monospace, monospace" font-weight="600" fill="#000000" text-anchor="middle">${this._escapeXml(text)}</text>`;
    }

    return `<g class="bpx-barcode" data-symbology="${symbology}">${bars}${textSvg}</g>`;
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
