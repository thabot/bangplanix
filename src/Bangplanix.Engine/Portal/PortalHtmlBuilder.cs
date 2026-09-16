namespace Bangplanix.Engine.Portal;

public static class PortalHtmlBuilder
{
    public static string BuildHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Bangplanix — Cloud Management Portal</title>
  <style>
    :root {
      --bg: #0d1117;
      --card-bg: #161b22;
      --card-hover: #21262d;
      --border: #30363d;
      --text: #c9d1d9;
      --text-muted: #8b949e;
      --accent: #58a6ff;
      --accent-hover: #1f6feb;
      --success: #3fb950;
      --warning: #d29922;
      --danger: #f85149;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: var(--text); padding: 20px; line-height: 1.5; }
    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border); padding-bottom: 16px; margin-bottom: 20px; flex-wrap: wrap; gap: 12px; }
    .brand { font-size: 20px; font-weight: 700; color: #fff; display: flex; align-items: center; gap: 10px; }
    .badge { font-size: 12px; background: rgba(88, 166, 255, 0.2); color: var(--accent); padding: 3px 8px; border-radius: 12px; border: 1px solid rgba(88, 166, 255, 0.3); }
    .nav-tabs { display: flex; gap: 8px; border-bottom: 1px solid var(--border); margin-bottom: 20px; overflow-x: auto; }
    .tab-btn { background: transparent; border: none; color: var(--text-muted); padding: 10px 16px; font-size: 14px; font-weight: 600; cursor: pointer; border-bottom: 2px solid transparent; transition: all 0.2s; white-space: nowrap; }
    .tab-btn:hover { color: #fff; }
    .tab-btn.active { color: var(--accent); border-bottom-color: var(--accent); }
    .tab-content { display: none; }
    .tab-content.active { display: block; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px; margin-bottom: 20px; }
    .card { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 18px; }
    .card h3 { font-size: 12px; text-transform: uppercase; color: var(--text-muted); letter-spacing: 0.5px; margin-bottom: 6px; }
    .card .value { font-size: 26px; font-weight: 700; color: #fff; }
    .card .subtext { font-size: 12px; color: var(--text-muted); margin-top: 4px; }
    .status-ok { color: var(--success); }
    .panel { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 18px; margin-bottom: 20px; }
    .panel-title { font-size: 15px; font-weight: 600; color: #fff; margin-bottom: 14px; display: flex; justify-content: space-between; align-items: center; }
    table { width: 100%; border-collapse: collapse; text-align: left; font-size: 13px; }
    th, td { padding: 10px 12px; border-bottom: 1px solid var(--border); }
    th { color: var(--text-muted); font-weight: 500; }
    .btn { background: #238636; color: #fff; border: none; padding: 7px 14px; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: 13px; transition: background 0.2s; display: inline-flex; align-items: center; gap: 6px; }
    .btn:hover { background: #2ea043; }
    .btn-secondary { background: #21262d; border: 1px solid var(--border); color: var(--text); }
    .btn-secondary:hover { background: #30363d; }
    .btn-danger { background: #da3633; color: #fff; }
    .btn-danger:hover { background: #b62324; }
    .btn-outline { background: transparent; border: 1px solid var(--accent); color: var(--accent); }
    .btn-outline:hover { background: rgba(88, 166, 255, 0.15); }
    input, select, textarea { background: #0d1117; border: 1px solid var(--border); color: #fff; padding: 8px 12px; border-radius: 6px; font-size: 13px; width: 100%; outline: none; }
    input:focus, select:focus, textarea:focus { border-color: var(--accent); }
    .log-box { background: #090d13; border: 1px solid var(--border); border-radius: 6px; padding: 14px; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 12px; height: 350px; overflow-y: auto; white-space: pre-wrap; color: #8b949e; }
    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: none; align-items: center; justify-content: center; z-index: 1000; }
    .modal-backdrop.active { display: flex; }
    .modal { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; width: 100%; max-width: 420px; padding: 24px; box-shadow: 0 8px 24px rgba(0,0,0,0.5); }
    .modal-title { font-size: 18px; font-weight: 600; color: #fff; margin-bottom: 12px; }
    .preview-frame { width: 100%; height: 480px; border: 1px solid var(--border); border-radius: 6px; background: #fff; }
    .dropzone { border: 2px dashed var(--border); border-radius: 8px; padding: 30px; text-align: center; cursor: pointer; transition: border-color 0.2s; }
    .dropzone:hover { border-color: var(--accent); }
  </style>
</head>
<body>
  <!-- Header -->
  <div class="header">
    <div class="brand">
      <span>🚀 Bangplanix Cloud Management Portal</span>
      <span class="badge">.NET 10 (Native AOT / Alpine)</span>
      <span id="authBadge" class="badge" style="background: rgba(63, 185, 80, 0.15); color: var(--success); border-color: var(--success);">Guest Mode</span>
    </div>
    <div style="display: flex; gap: 8px; align-items: center;">
      <button class="btn btn-secondary" onclick="fetchMetrics()">🔄 Refresh</button>
      <button id="authBtn" class="btn btn-outline" onclick="toggleAuthModal()">🔐 Login</button>
    </div>
  </div>

  <!-- Navigation Tabs -->
  <div class="nav-tabs">
    <button class="tab-btn active" onclick="switchTab('dashboard')">📊 Telemetry Dashboard</button>
    <button class="tab-btn" onclick="switchTab('files')">📁 Container Files</button>
    <button class="tab-btn" onclick="switchTab('converter')">🔄 Report Converter</button>
    <button class="tab-btn" onclick="switchTab('sandbox')">👁️ Report Sandbox</button>
    <button class="tab-btn" onclick="switchTab('logs')">📜 Live Logs</button>
    <button class="tab-btn" onclick="switchTab('system')">⚙️ System & License</button>
  </div>

  <!-- Tab 1: Dashboard -->
  <div id="tab-dashboard" class="tab-content active">
    <div class="grid">
      <div class="card">
        <h3>Active Queue Depth</h3>
        <div class="value" id="queueDepth">0</div>
        <div class="subtext">Pending render jobs in broker</div>
      </div>
      <div class="card">
        <h3>Total Processed</h3>
        <div class="value status-ok" id="totalProcessed">0</div>
        <div class="subtext">Successfully generated documents</div>
      </div>
      <div class="card">
        <h3>Dead-Letter Queue (DLQ)</h3>
        <div class="value" id="dlqCount" style="color: var(--danger)">0</div>
        <div class="subtext">Failed slices requiring replay</div>
      </div>
      <div class="card">
        <h3>RAM Consumption</h3>
        <div class="value" id="memoryUsage">0 MB</div>
        <div class="subtext">Alpine managed heap ceiling: &lt; 128MB</div>
      </div>
    </div>

    <div class="panel">
      <div class="panel-title">Cluster Node & Licensing Status</div>
      <table>
        <thead>
          <tr>
            <th>Node Identifier</th>
            <th>Engine Runtime</th>
            <th>Worker Threads</th>
            <th>Licensing Tier</th>
            <th>Health Status</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td id="nodeId">worker-local-01</td>
            <td>.NET 10 (Alpine Linux / Multi-thread)</td>
            <td id="concurrency">4 Cores</td>
            <td><span id="tierBadge" class="badge" style="color: var(--success); background: rgba(63, 185, 80, 0.2)">Community Edition</span></td>
            <td><span class="status-ok">● Online</span></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- Tab 2: Container Files -->
  <div id="tab-files" class="tab-content">
    <div class="panel">
      <div class="panel-title">
        <span>Container Volumes File Manager</span>
        <div style="display: flex; gap: 8px; align-items: center;">
          <select id="folderSelect" style="width: 140px;" onchange="loadFiles()">
            <option value="templates">/templates</option>
            <option value="data">/data</option>
            <option value="fonts">/fonts</option>
            <option value="logs">/logs</option>
          </select>
          <input type="file" id="fileUploadInput" style="display: none;" onchange="uploadSelectedFile()">
          <button class="btn" onclick="document.getElementById('fileUploadInput').click()">⬆️ Upload File</button>
        </div>
      </div>
      <table>
        <thead>
          <tr>
            <th>File Name</th>
            <th>Size</th>
            <th>Last Modified</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody id="filesTableBody">
          <tr><td colspan="4" style="text-align: center; color: var(--text-muted);">Loading files...</td></tr>
        </tbody>
      </table>
    </div>
  </div>

  <!-- Tab 3: Report Converter -->
  <div id="tab-converter" class="tab-content">
    <div class="panel">
      <div class="panel-title">Web Report Converter (Legacy -> Bangplanix .bpx)</div>
      <p style="color: var(--text-muted); font-size: 13px; margin-bottom: 16px;">
        Convert legacy reports (Crystal .rpt.xml, SSRS .rdl, Jaspersoft .jrxml, FastReport .frx, DevExpress .repx, Stimulsoft .mrt) into Bangplanix .bpx schema.
      </p>
      <div class="dropzone" id="dropzone" onclick="document.getElementById('convertFileInput').click()">
        <input type="file" id="convertFileInput" style="display: none;" onchange="convertUploadedFile(event)">
        <div style="font-size: 24px; margin-bottom: 8px;">📄</div>
        <div style="font-weight: 600; color: #fff;">Click to select file or drag & drop here</div>
        <div style="font-size: 12px; color: var(--text-muted); margin-top: 4px;">Supports .rdl, .jrxml, .frx, .rpt.xml, .repx, .mrt</div>
      </div>
      <div id="convertResultBox" style="display: none; margin-top: 16px;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
          <strong style="color: var(--success)">✅ Conversion Succeeded</strong>
          <div style="display: flex; gap: 8px;">
            <button class="btn btn-secondary" onclick="downloadConvertedBpx()">💾 Download .bpx</button>
            <button class="btn" onclick="saveBpxToTemplates()">📁 Save to Container Templates</button>
          </div>
        </div>
        <textarea id="convertedBpxContent" style="height: 240px; font-family: monospace; font-size: 12px;" readonly></textarea>
      </div>
    </div>
  </div>

  <!-- Tab 4: Report Sandbox -->
  <div id="tab-sandbox" class="tab-content">
    <div class="panel">
      <div class="panel-title">Interactive Report Sandbox (Live Render PDF & Excel)</div>
      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 16px;">
        <div>
          <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Select Template or Paste .bpx JSON</label>
          <div style="display: flex; gap: 8px; margin-bottom: 8px;">
            <select id="sandboxTemplateSelect" onchange="loadSandboxTemplate()">
              <option value="">-- Choose from Container Templates --</option>
            </select>
          </div>
          <textarea id="sandboxBpxText" style="height: 180px; font-family: monospace; font-size: 12px;" placeholder="Paste .bpx template JSON here..."></textarea>
        </div>
        <div>
          <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Data JSON Array</label>
          <textarea id="sandboxDataText" style="height: 220px; font-family: monospace; font-size: 12px;">[
  { "ItemCode": "ITEM-001", "ItemName": "Cloud Server Subscription", "Price": 15000.00 },
  { "ItemCode": "ITEM-002", "ItemName": "Bangplanix Enterprise License", "Price": 85000.00 }
]</textarea>
        </div>
      </div>
      <div style="display: flex; gap: 8px; margin-bottom: 16px;">
        <button class="btn" onclick="renderSandboxPdf()">📄 Render to PDF</button>
        <button class="btn btn-secondary" onclick="renderSandboxXlsx()">📊 Render to Excel (XLSX)</button>
      </div>
      <div id="previewContainer" style="display: none;">
        <iframe id="pdfPreviewFrame" class="preview-frame"></iframe>
      </div>
    </div>
  </div>

  <!-- Tab 5: Live Logs -->
  <div id="tab-logs" class="tab-content">
    <div class="panel">
      <div class="panel-title">
        <span>Container Live Logs</span>
        <div style="display: flex; gap: 8px;">
          <input type="text" id="logFilter" placeholder="Filter logs (e.g. ERROR, WARN)..." style="width: 220px;" oninput="filterLogs()">
          <button class="btn btn-secondary" onclick="fetchLogs()">🔄 Refresh Logs</button>
        </div>
      </div>
      <div id="logsBox" class="log-box">Loading container logs...</div>
    </div>
  </div>

  <!-- Tab 6: System & License -->
  <div id="tab-system" class="tab-content">
    <div class="panel">
      <div class="panel-title">🛡️ Commercial License Activation</div>
      <div style="display: flex; gap: 12px; margin-bottom: 12px;">
        <input type="text" id="licenseTokenInput" placeholder="Paste signed commercial license token here...">
        <button class="btn" style="white-space: nowrap;" onclick="activateLicense()">Apply License</button>
      </div>
      <div id="licenseResult" style="font-size: 13px;"></div>
    </div>

    <div class="panel">
      <div class="panel-title">🗄️ Database Connection Health Tester</div>
      <div style="display: grid; grid-template-columns: 140px 1fr auto; gap: 10px; align-items: center; margin-bottom: 12px;">
        <select id="dbTypeSelect">
          <option value="postgres">PostgreSQL</option>
          <option value="sqlserver">SQL Server</option>
          <option value="mysql">MySQL</option>
          <option value="sqlite">SQLite</option>
        </select>
        <input type="text" id="dbConnString" placeholder="Host=localhost;Database=test;Username=postgres;Password=...">
        <button class="btn btn-secondary" onclick="testDbConnection()">Test Connection</button>
      </div>
      <div id="dbTestResult" style="font-size: 13px;"></div>
    </div>

    <div class="panel">
      <div class="panel-title">⚙️ Installed Fonts & Environment</div>
      <div id="envInfo" style="font-size: 13px; color: var(--text-muted); margin-bottom: 10px;">Loading environment details...</div>
      <div style="font-weight: 600; color: #fff; font-size: 13px; margin-bottom: 6px;">Fonts available in Container:</div>
      <ul id="fontsList" style="list-style-type: square; padding-left: 20px; font-size: 13px; color: var(--text-muted);">
        <li>Loading fonts...</li>
      </ul>
    </div>
  </div>

  <!-- Login Modal -->
  <div class="modal-backdrop" id="authModal">
    <div class="modal">
      <div class="modal-title">🔐 Sign in to Management Portal</div>
      <div style="font-size: 12px; color: var(--text-muted); margin-bottom: 16px; background: rgba(88,166,255,0.1); padding: 8px; border-radius: 6px; border: 1px solid rgba(88,166,255,0.2);">
        💡 <b>Default Credentials:</b> User: <code>admin</code> / Password: <code>bangplanix2026!</code>
      </div>
      <div style="margin-bottom: 12px;">
        <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Username</label>
        <input type="text" id="loginUsername" value="admin">
      </div>
      <div style="margin-bottom: 18px;">
        <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Password</label>
        <input type="password" id="loginPassword" value="bangplanix2026!">
      </div>
      <div style="display: flex; justify-content: flex-end; gap: 8px;">
        <button class="btn btn-secondary" onclick="toggleAuthModal()">Cancel</button>
        <button class="btn" onclick="submitLogin()">Login</button>
      </div>
      <div id="loginError" style="color: var(--danger); font-size: 12px; margin-top: 10px; display: none;"></div>
    </div>
  </div>

  <!-- Scripts -->
  <script>
    let authToken = sessionStorage.getItem('bangplanix_token') || '';
    let currentUser = sessionStorage.getItem('bangplanix_user') || '';
    let rawLogs = '';

    function switchTab(tabId) {
      document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
      event.target.classList.add('active');
      document.getElementById('tab-' + tabId).classList.add('active');
      if (tabId === 'files') loadFiles();
      if (tabId === 'sandbox') loadSandboxTemplateOptions();
      if (tabId === 'logs') fetchLogs();
      if (tabId === 'system') fetchEnvironment();
    }

    function toggleAuthModal() {
      if (authToken) {
        logout();
        return;
      }
      const m = document.getElementById('authModal');
      m.classList.toggle('active');
    }

    async function submitLogin() {
      const u = document.getElementById('loginUsername').value;
      const p = document.getElementById('loginPassword').value;
      const err = document.getElementById('loginError');
      err.style.display = 'none';

      try {
        const res = await fetch('/api/v1/auth/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ username: u, password: p })
        });
        const data = await res.json();
        if (!res.ok) {
          err.innerText = data.error || 'Login failed';
          err.style.display = 'block';
          return;
        }
        authToken = data.token;
        currentUser = data.username;
        sessionStorage.setItem('bangplanix_token', authToken);
        sessionStorage.setItem('bangplanix_user', currentUser);
        updateAuthUi();
        document.getElementById('authModal').classList.remove('active');
      } catch (e) {
        err.innerText = e.message;
        err.style.display = 'block';
      }
    }

    function logout() {
      authToken = '';
      currentUser = '';
      sessionStorage.removeItem('bangplanix_token');
      sessionStorage.removeItem('bangplanix_user');
      updateAuthUi();
    }

    function updateAuthUi() {
      const b = document.getElementById('authBadge');
      const btn = document.getElementById('authBtn');
      if (authToken) {
        b.innerText = 'Admin: ' + currentUser;
        b.style.background = 'rgba(63, 185, 80, 0.2)';
        b.style.color = 'var(--success)';
        btn.innerText = '🚪 Logout';
        btn.classList.remove('btn-outline');
        btn.classList.add('btn-secondary');
      } else {
        b.innerText = 'Guest Mode';
        b.style.background = 'rgba(210, 153, 34, 0.2)';
        b.style.color = 'var(--warning)';
        btn.innerText = '🔐 Login';
        btn.classList.add('btn-outline');
        btn.classList.remove('btn-secondary');
      }
    }

    async function fetchMetrics() {
      try {
        const res = await fetch('/api/metrics');
        if (!res.ok) return;
        const data = await res.json();
        document.getElementById('queueDepth').innerText = data.queueDepth;
        document.getElementById('totalProcessed').innerText = data.totalProcessed;
        document.getElementById('dlqCount').innerText = data.deadLetterCount;
        document.getElementById('memoryUsage').innerText = (data.memoryAllocatedMb || 0).toFixed(2) + ' MB';
        document.getElementById('nodeId').innerText = data.nodeId || 'worker-01';
        document.getElementById('concurrency').innerText = (data.workerConcurrency || 4) + ' Cores';
      } catch (e) {
        console.error('Failed to load metrics', e);
      }
    }

    async function loadFiles() {
      const folder = document.getElementById('folderSelect').value;
      const tbody = document.getElementById('filesTableBody');
      tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Loading...</td></tr>';
      try {
        const res = await fetch('/api/v1/files?folder=' + folder, {
          headers: authToken ? { 'Authorization': 'Bearer ' + authToken } : {}
        });
        const files = await res.json();
        if (!files || files.length === 0) {
          tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: var(--text-muted);">No files in /' + folder + '</td></tr>';
          return;
        }
        tbody.innerHTML = files.map(f => `
          <tr>
            <td>📄 ${f.name}</td>
            <td>${formatBytes(f.sizeBytes)}</td>
            <td>${new Date(f.lastModifiedUtc).toLocaleString()}</td>
            <td>
              <a href="/api/v1/files/download?folder=${folder}&name=${encodeURIComponent(f.name)}" class="btn btn-secondary" style="padding: 3px 8px; font-size: 11px;">⬇️ Download</a>
              <button onclick="deleteFile('${folder}', '${f.name}')" class="btn btn-danger" style="padding: 3px 8px; font-size: 11px;">🗑️ Delete</button>
            </td>
          </tr>
        `).join('');
      } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" style="color: var(--danger); text-align: center;">' + e.message + '</td></tr>';
      }
    }

    async function uploadSelectedFile() {
      const input = document.getElementById('fileUploadInput');
      if (!input.files || !input.files[0]) return;
      const file = input.files[0];
      const folder = document.getElementById('folderSelect').value;
      const fd = new FormData();
      fd.append('file', file);

      const res = await fetch('/api/v1/files/upload?folder=' + folder, {
        method: 'POST',
        headers: authToken ? { 'Authorization': 'Bearer ' + authToken } : {},
        body: fd
      });
      if (res.ok) {
        alert('File uploaded successfully!');
        loadFiles();
      } else {
        const err = await res.json();
        alert('Upload failed: ' + (err.error || res.statusText));
      }
      input.value = '';
    }

    async function deleteFile(folder, name) {
      if (!confirm('Are you sure you want to delete ' + name + '?')) return;
      const res = await fetch('/api/v1/files?folder=' + folder + '&name=' + encodeURIComponent(name), {
        method: 'DELETE',
        headers: authToken ? { 'Authorization': 'Bearer ' + authToken } : {}
      });
      if (res.ok) {
        loadFiles();
      } else {
        const err = await res.json();
        alert('Delete failed: ' + (err.error || res.statusText));
      }
    }

    async function convertUploadedFile(e) {
      const file = e.target.files[0];
      if (!file) return;
      const fd = new FormData();
      fd.append('file', file);

      try {
        const res = await fetch('/api/v1/convert', { method: 'POST', body: fd });
        const data = await res.json();
        if (!res.ok) {
          alert('Conversion failed: ' + (data.error || 'Unknown error'));
          return;
        }
        document.getElementById('convertedBpxContent').value = JSON.stringify(data, null, 2);
        document.getElementById('convertResultBox').style.display = 'block';
      } catch (err) {
        alert('Conversion failed: ' + err.message);
      }
    }

    function downloadConvertedBpx() {
      const txt = document.getElementById('convertedBpxContent').value;
      const blob = new Blob([txt], { type: 'application/json' });
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = 'converted_report.bpx';
      a.click();
    }

    async function saveBpxToTemplates() {
      const txt = document.getElementById('convertedBpxContent').value;
      const name = prompt('Enter template file name (e.g. MyMigratedReport.bpx):', 'MigratedReport.bpx');
      if (!name) return;
      const blob = new Blob([txt], { type: 'application/json' });
      const fd = new FormData();
      fd.append('file', blob, name);

      const res = await fetch('/api/v1/files/upload?folder=templates', {
        method: 'POST',
        headers: authToken ? { 'Authorization': 'Bearer ' + authToken } : {},
        body: fd
      });
      if (res.ok) alert('Saved to templates successfully!');
      else alert('Save failed. (Make sure you are logged in)');
    }

    async function loadSandboxTemplateOptions() {
      const sel = document.getElementById('sandboxTemplateSelect');
      try {
        const res = await fetch('/api/v1/files?folder=templates');
        const files = await res.json();
        sel.innerHTML = '<option value="">-- Choose from Container Templates --</option>' + 
          files.filter(f => f.name.endsWith('.bpx')).map(f => `<option value="${f.name}">${f.name}</option>`).join('');
      } catch (e) {}
    }

    async function loadSandboxTemplate() {
      const sel = document.getElementById('sandboxTemplateSelect');
      if (!sel.value) return;
      const res = await fetch('/api/v1/files/download?folder=templates&name=' + encodeURIComponent(sel.value));
      if (res.ok) {
        const txt = await res.text();
        document.getElementById('sandboxBpxText').value = txt;
      }
    }

    async function renderSandboxPdf() {
      const bpx = document.getElementById('sandboxBpxText').value;
      const data = document.getElementById('sandboxDataText').value;
      if (!bpx.trim()) { alert('Please provide .bpx template'); return; }

      const res = await fetch('/api/v1/render/pdf', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: bpx
      });
      if (!res.ok) {
        alert('Render failed');
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      document.getElementById('pdfPreviewFrame').src = url;
      document.getElementById('previewContainer').style.display = 'block';
    }

    async function renderSandboxXlsx() {
      const bpx = document.getElementById('sandboxBpxText').value;
      if (!bpx.trim()) { alert('Please provide .bpx template'); return; }

      const res = await fetch('/api/v1/render/xlsx', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: bpx
      });
      if (!res.ok) {
        alert('Excel Export failed');
        return;
      }
      const blob = await res.blob();
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = 'Report.xlsx';
      a.click();
    }

    async function fetchLogs() {
      try {
        const res = await fetch('/api/v1/logs');
        rawLogs = await res.text();
        filterLogs();
      } catch (e) {
        document.getElementById('logsBox').innerText = 'Failed to fetch logs: ' + e.message;
      }
    }

    function filterLogs() {
      const q = document.getElementById('logFilter').value.toLowerCase();
      if (!q) {
        document.getElementById('logsBox').innerText = rawLogs;
        return;
      }
      const lines = rawLogs.split('\n');
      document.getElementById('logsBox').innerText = lines.filter(l => l.toLowerCase().includes(q)).join('\n');
    }

    async function activateLicense() {
      const tok = document.getElementById('licenseTokenInput').value;
      const resEl = document.getElementById('licenseResult');
      if (!tok) return;

      const res = await fetch('/api/v1/license/activate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token: tok })
      });
      const data = await res.json();
      if (res.ok) {
        resEl.innerHTML = `<span style="color: var(--success)">✅ License activated successfully! Tier: <b>${data.tier}</b></span>`;
        document.getElementById('tierBadge').innerText = data.tier;
      } else {
        resEl.innerHTML = `<span style="color: var(--danger)">❌ Activation failed: ${data.error}</span>`;
      }
    }

    async function testDbConnection() {
      const type = document.getElementById('dbTypeSelect').value;
      const conn = document.getElementById('dbConnString').value;
      const resEl = document.getElementById('dbTestResult');

      const res = await fetch('/api/v1/tools/db-test', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type, connectionString: conn })
      });
      const data = await res.json();
      if (data.success) {
        resEl.innerHTML = `<span style="color: var(--success)">✅ Connection successful! Latency: ${data.latencyMs}ms</span>`;
      } else {
        resEl.innerHTML = `<span style="color: var(--danger)">❌ Connection failed: ${data.error}</span>`;
      }
    }

    async function fetchEnvironment() {
      try {
        const res = await fetch('/api/v1/tools/environment');
        const data = await res.json();
        document.getElementById('envInfo').innerText = `OS: ${data.osDescription} | Arch: ${data.processArchitecture} | Cores: ${data.processorCount} | Runtime: ${data.frameworkDescription}`;
        const fontsEl = document.getElementById('fontsList');
        if (data.installedFonts && data.installedFonts.length > 0) {
          fontsEl.innerHTML = data.installedFonts.map(f => `<li>🔤 ${f}</li>`).join('');
        } else {
          fontsEl.innerHTML = '<li>Standard system vector fonts</li>';
        }
      } catch (e) {}
    }

    function formatBytes(bytes) {
      if (!bytes || bytes === 0) return '0 B';
      const k = 1024;
      const sizes = ['B', 'KB', 'MB', 'GB'];
      const i = Math.floor(Math.log(bytes) / Math.log(k));
      return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    }

    updateAuthUi();
    fetchMetrics();
    setInterval(fetchMetrics, 4000);
  </script>
</body>
</html>
""";
    }
}
