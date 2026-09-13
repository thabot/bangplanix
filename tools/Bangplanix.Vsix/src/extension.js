const vscode = require('vscode');

/**
 * Diagnostic collection for BPX files
 */
let diagnosticCollection;
let currentPreviewPanel = null;
let debounceTimer = null;

/**
 * Activates the Bangplanix extension
 * @param {vscode.ExtensionContext} context 
 */
function activate(context) {
  diagnosticCollection = vscode.languages.createDiagnosticCollection('bangplanix');
  context.subscriptions.push(diagnosticCollection);

  // Register Validate command
  const validateCmd = vscode.commands.registerCommand('bangplanix.validate', () => {
    const editor = vscode.window.activeTextEditor;
    if (editor && isBpxDocument(editor.document)) {
      validateBpxDocument(editor.document);
    }
  });
  context.subscriptions.push(validateCmd);

  // Register Preview command
  const previewCmd = vscode.commands.registerCommand('bangplanix.preview', () => {
    const editor = vscode.window.activeTextEditor;
    if (editor && isBpxDocument(editor.document)) {
      openLivePreview(context, editor.document);
    } else {
      vscode.window.showWarningMessage('Bangplanix: Please open a .bpx report template file first.');
    }
  });
  context.subscriptions.push(previewCmd);

  // Auto validate on open and save
  context.subscriptions.push(
    vscode.workspace.onDidOpenTextDocument(doc => {
      if (isBpxDocument(doc)) validateBpxDocument(doc);
    })
  );

  context.subscriptions.push(
    vscode.workspace.onDidSaveTextDocument(doc => {
      if (isBpxDocument(doc)) validateBpxDocument(doc);
    })
  );

  // Live preview update on text change
  context.subscriptions.push(
    vscode.workspace.onDidChangeTextDocument(event => {
      if (isBpxDocument(event.document)) {
        const config = vscode.workspace.getConfiguration('bangplanix');
        const debounceMs = config.get('livePreview.debounceMs', 300);

        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
          validateBpxDocument(event.document);
          if (currentPreviewPanel && currentPreviewPanel.visible) {
            updatePreviewContent(currentPreviewPanel, event.document);
          }
        }, debounceMs);
      }
    })
  );
}

/**
 * Checks if document is a Bangplanix .bpx file
 */
function isBpxDocument(document) {
  return document.fileName.endsWith('.bpx') || document.languageId === 'bangplanix';
}

/**
 * Validates BPX JSON schema and band structure
 */
function validateBpxDocument(document) {
  const diagnostics = [];
  const text = document.getText();

  try {
    const json = JSON.parse(text);

    if (!json.version) {
      diagnostics.push(createDiagnostic(document, 0, 0, 'Missing "version" field in report template.', vscode.DiagnosticSeverity.Error));
    }

    if (!json.pageSetup) {
      diagnostics.push(createDiagnostic(document, 0, 0, 'Missing "pageSetup" definition.', vscode.DiagnosticSeverity.Error));
    }

    if (!json.bands || typeof json.bands !== 'object') {
      diagnostics.push(createDiagnostic(document, 0, 0, 'Report must contain a "bands" object (e.g. title, pageHeader, detail, pageFooter).', vscode.DiagnosticSeverity.Warning));
    }

    // Validate Expressions
    if (json.bands) {
      Object.entries(json.bands).forEach(([bandName, band]) => {
        if (band && Array.isArray(band.elements)) {
          band.elements.forEach(elem => {
            if (elem.expression && typeof elem.expression === 'string') {
              if (!elem.expression.startsWith('=')) {
                diagnostics.push(createDiagnostic(document, 0, 0, `Band "${bandName}" element expression "${elem.expression}" should start with "=" prefix.`, vscode.DiagnosticSeverity.Information));
              }
            }
          });
        }
      });
    }

    diagnosticCollection.set(document.uri, diagnostics);
    if (diagnostics.length === 0) {
      vscode.window.setStatusBarMessage('Bangplanix: Template is valid ✔', 3000);
    }
  } catch (err) {
    const diag = new vscode.Diagnostic(
      new vscode.Range(0, 0, 0, 1),
      `Invalid JSON syntax: ${err.message}`,
      vscode.DiagnosticSeverity.Error
    );
    diagnosticCollection.set(document.uri, [diag]);
  }
}

function createDiagnostic(document, line, col, message, severity) {
  const range = new vscode.Range(line, col, line, col + 10);
  return new vscode.Diagnostic(range, message, severity);
}

/**
 * Opens a side-by-side Webview panel for live preview
 */
function openLivePreview(context, document) {
  if (currentPreviewPanel) {
    currentPreviewPanel.reveal(vscode.ViewColumn.Beside);
  } else {
    currentPreviewPanel = vscode.window.createWebviewPanel(
      'bangplanixPreview',
      `Preview: ${document.fileName.split(/[\\/]/).pop()}`,
      vscode.ViewColumn.Beside,
      {
        enableScripts: true,
        retainContextWhenHidden: true
      }
    );

    currentPreviewPanel.onDidDispose(() => {
      currentPreviewPanel = null;
    }, null, context.subscriptions);
  }

  updatePreviewContent(currentPreviewPanel, document);
}

/**
 * Updates the HTML/SVG content in the preview webview
 */
function updatePreviewContent(panel, document) {
  const text = document.getText();
  let bpx;
  try {
    bpx = JSON.parse(text);
  } catch (e) {
    panel.webview.html = `
      <!DOCTYPE html>
      <html>
      <body style="font-family: sans-serif; padding: 20px; color: #e53e3e; background: #1a202c;">
        <h3>⚠️ Syntax Error in .bpx Template</h3>
        <pre>${e.message}</pre>
      </body>
      </html>
    `;
    return;
  }

  const svgContent = generateSvgPreview(bpx);
  panel.webview.html = `
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="UTF-8">
      <style>
        body {
          margin: 0;
          padding: 24px;
          display: flex;
          flex-direction: column;
          align-items: center;
          background-color: var(--vscode-editor-background, #1e1e1e);
          color: var(--vscode-editor-foreground, #d4d4d4);
          font-family: var(--vscode-font-family, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif);
        }
        .toolbar {
          display: flex;
          align-items: center;
          gap: 12px;
          margin-bottom: 16px;
          padding: 6px 12px;
          background: var(--vscode-sideBar-background, #252526);
          border-radius: 6px;
          border: 1px solid var(--vscode-panel-border, #333);
        }
        .badge {
          font-size: 11px;
          font-weight: bold;
          padding: 2px 8px;
          border-radius: 4px;
          background: #3182ce;
          color: #fff;
        }
        .page-container {
          background: white;
          color: black;
          box-shadow: 0 4px 16px rgba(0,0,0,0.3);
          border-radius: 4px;
          overflow: hidden;
        }
      </style>
    </head>
    <body>
      <div class="toolbar">
        <span class="badge">LIVE PREVIEW</span>
        <span>${bpx.metadata?.title || 'Bangplanix Report'}</span>
        <span>|</span>
        <span>Size: ${bpx.pageSetup?.paperKind || 'A4'} (${bpx.pageSetup?.orientation || 'Portrait'})</span>
      </div>
      <div class="page-container">
        ${svgContent}
      </div>
    </body>
    </html>
  `;
}

/**
 * Generates an SVG representation of the BPX report
 */
function generateSvgPreview(bpx) {
  const width = bpx.pageSetup?.orientation === 'Landscape' ? 842 : 595;
  const height = bpx.pageSetup?.orientation === 'Landscape' ? 595 : 842;
  const margins = bpx.pageSetup?.margins || { top: 20, bottom: 20, left: 20, right: 20 };

  let currentY = margins.top;
  let svgElements = '';

  if (bpx.bands) {
    const bandOrder = ['title', 'pageHeader', 'detail', 'pageFooter'];
    for (const name of bandOrder) {
      const band = bpx.bands[name];
      if (!band) continue;

      const bandHeight = band.height || 40;
      // Band boundary line (subtle)
      svgElements += `<line x1="${margins.left}" y1="${currentY}" x2="${width - margins.right}" y2="${currentY}" stroke="#e2e8f0" stroke-dasharray="4" />`;
      
      if (Array.isArray(band.elements)) {
        band.elements.forEach(elem => {
          const bx = margins.left + (elem.bounds?.x || 0);
          const by = currentY + (elem.bounds?.y || 0);
          const bw = elem.bounds?.width || 100;
          const bh = elem.bounds?.height || 20;

          if (elem.type === 'text') {
            const textVal = elem.content || elem.expression || '(empty)';
            const fontSize = elem.fontSize || 12;
            const fontWeight = elem.fontWeight === 'Bold' ? 'bold' : 'normal';
            const fill = elem.textColor || '#1a202c';
            svgElements += `<text x="${bx}" y="${by + fontSize}" font-size="${fontSize}" font-weight="${fontWeight}" fill="${fill}" font-family="sans-serif">${escapeXml(textVal)}</text>`;
          } else if (elem.type === 'line') {
            svgElements += `<line x1="${bx}" y1="${by}" x2="${bx + bw}" y2="${by + bh}" stroke="${elem.strokeColor || '#cbd5e0'}" stroke-width="${elem.strokeWidth || 1}" />`;
          } else if (elem.type === 'rectangle') {
            svgElements += `<rect x="${bx}" y="${by}" width="${bw}" height="${bh}" fill="${elem.fillColor || '#f7fafc'}" stroke="${elem.borderColor || '#cbd5e0'}" rx="${elem.cornerRadius || 0}" />`;
          } else if (elem.type === 'barcode') {
            svgElements += `<rect x="${bx}" y="${by}" width="${bw}" height="${bh}" fill="#edf2f7" stroke="#a0aec0" /><text x="${bx + bw/2}" y="${by + bh/2 + 4}" font-size="10" text-anchor="middle" fill="#4a5568">[BARCODE: ${escapeXml(elem.content || '')}]</text>`;
          }
        });
      }

      currentY += bandHeight;
    }
  }

  return `
    <svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
      <rect width="${width}" height="${height}" fill="#ffffff" />
      ${svgElements}
    </svg>
  `;
}

function escapeXml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function deactivate() {
  if (diagnosticCollection) diagnosticCollection.clear();
}

module.exports = {
  activate,
  deactivate,
  validateBpxDocument,
  generateSvgPreview
};
