# 🗺️ Bangplanix — Product Roadmap & Future Milestones

This document tracks planned architectural milestones, upcoming capabilities, and future feature backlogs for the Bangplanix enterprise reporting ecosystem.

---

## 📌 Executive Status & Roadmap Tracking Summary (สรุปสถานะฟีเจอร์ใน Roadmap)

ตารางสรุปสถานะฟีเจอร์ที่อยู่ใน Roadmap ที่กำลังพัฒนาหรือเตรียมเปิดใช้งานในรุ่นถัดไป:

| Feature / Capability | Current State in Codebase | Target Milestone | Planned Deliverables & Scope |
| :--- | :--- | :---: | :--- |
| **1. Interactive Visual AI Designer UI & Server AI Proxy** | AI Engine Core (C#/.NET & JS) is functional; Ribbon buttons, chat dialog, and `/api/v1/ai/*` server endpoints are staged | **v1.1.0** | Web UI Assistant in `<bangplanix-designer>`, Chat modal, BYOK settings modal, Server AI REST proxy |
| **2. Production Cloud Storage & Email Bursting Channels** | Bursting Slicing & Cron Engine functional; Delivery channels use simulation stubs (`Task.Delay(5)`) | **v1.1.0** | MailKit production SMTP client, AWS S3 SigV4/SDK driver, Azure Blob storage client, SSH.NET SFTP driver |
| **3. Cryptographic PAdES Digital Signatures & Certified RFC 3161 TSA** | PDF Signature structure tree and ETDA XML generation functional; Uses zero-padded SHA-256 placeholder & text token | **v1.1.0** | RFC 5652 PKCS#7 / CMS detached signing with X.509 cert chains, Cloud HSM support, Real RFC 3161 TSP HTTP client |
| **4. Web Management Portal GUI Mounting & Database Storage** | Fully mounted in `Program.cs` at `GET /`, `GET /portal` with SQLite/Postgres DB, File Manager & Converter | **v1.1.0** *(Completed)* | Route wiring in `Program.cs` mounting GUI dashboard at `GET /` and `GET /portal` with live telemetry, container files, and web converter |
| **5. Kubernetes GitOps Operator Daemon Controller** | `BangplanixReportJob` CRD manifests defined in `deploy/k8s/` | **v1.2.0** | Active Kubernetes Controller Daemon (Go / .NET Worker) reconciling CRDs for automated ArgoCD/Flux GitOps pipelines |
| **6. Polyglot Standalone Published SDK Packages** | 5 Official SDKs available (.NET, TS, Python, Go, Java); PHP, Dart, Rust, Ruby supported via REST HTTP | **v1.2.0** | Official published packages: Composer (`bangplanix/client`), pub.dev (`bangplanix`), crates.io (`bangplanix`), RubyGems (`bangplanix`) |

---

## 🚀 Upcoming Release: v1.1.0 (Phase 5: Interactive AI Designer & Smart Assistant)

### 🤖 1. Bangplanix AI Suite — Web UI & Interactive Designer Integration

While the core AI Engine (`ReportAiGenerator`, `HybridLlmGateway`, `AiSqlSafetyValidator`, `SelfHealingReportAgent`, `AiExpressionAssistant`) is fully implemented in the .NET engine and JavaScript core logic, the visual frontend controls will be introduced in v1.1.0:

#### 1.1 AI Provider & API Key Configuration Dialog (`<bangplanix-designer>`)
- [ ] **Multi-Provider Settings Modal:**
  - Support for **Google Gemini** (Gemini 2.5 Flash / Pro / Gemini 3.x)
  - Support for **Azure OpenAI** (GPT-4o / GPT-4o-mini)
  - Support for **Anthropic Claude** (Claude 3.5 Sonnet / Claude 3.7)
  - Support for **Local / On-Premise Ollama** (e.g., `http://localhost:11434`, Llama 3.3 / DeepSeek-R1 / Qwen 2.5)
- [ ] **Key & Credential Management:**
  - Secure encrypted client-side credential storage (`localStorage` / `sessionStorage`).
  - Option for team/enterprise managed credentials via backend server proxy.
  - Zero telemetry or third-party credential leakage.
- [ ] **Connectivity & Model Health Check:**
  - Integrated *"Test Connection"* / *"Ping Model"* button with latency measurement and quota status check.

#### 1.2 Interactive AI Chat & Prompt-to-Report Generator Modal
- [ ] **Ribbon Toolbar Integration:**
  - Prominent **"✨ AI Assistant"** and **"🤖 Generate with AI"** action buttons on the top ribbon toolbar of `<bangplanix-designer>`.
- [ ] **Conversational Chat Dialog:**
  - Interactive chat interface supporting natural language prompts in **Thai** and **English**.
  - Example Thai prompt: *"สร้างรายงานใบเสร็จรับเงิน มีรหัสสินค้า ชื่อสินค้า จำนวน ราคา ยอดรวม พร้อม QR Code PromptPay และยอดเงินบาทถ้วน"*
  - Example English prompt: *"Generate a modern executive sales dashboard with revenue column chart, customer table, and KPI cards."*
- [ ] **One-Click Apply & Visual Schema Diff:**
  - Preview generated `.bpx` schema with visual side-by-side or overlay diff before applying to canvas.
  - Seamless integration with Undo/Redo history stack (`Ctrl+Z` / `Ctrl+Y`).
- [ ] **AI Auto-Fix & Diagnostics Button:**
  - One-click trigger for `SelfHealingReportAgent.DiagnoseAndRepair()` to automatically fix broken bands, missing page setups, or corrupted JSON directly in the designer.
- [ ] **AI Expression Assistant Popup:**
  - Contextual prompt popup in the Property Inspector and Bottom Editor to translate formula intent into sandboxed C# Roslyn expressions (e.g., *"คำนวณภาษี VAT 7%"* ➔ `=Convert.ToDecimal(Fields["Amount"]) * 0.07m`).

---

### 🌐 2. Server-Side REST AI Proxy / Gateway API (`Bangplanix.Server`)

To allow browser clients to leverage AI features in enterprise environments without exposing provider API keys on the client:

- [ ] **`POST /api/v1/ai/generate`**:
  - Accepts natural language prompt and optional table schemas.
  - Passes through `PromptInjectionFirewall` and server-side `HybridLlmGateway`.
  - Returns safe, validated `.bpx` JSON schema with read-only SQL query.
- [ ] **`POST /api/v1/ai/expression`**:
  - Translates business calculation prompts into sandboxed Roslyn C# expressions.
- [ ] **`POST /api/v1/ai/diagnose-repair`**:
  - Diagnoses and auto-repairs broken `.bpx` template payloads via `SelfHealingReportAgent`.
- [ ] **`POST /api/v1/ai/executive-summary`**:
  - Generates analytical markdown summaries and IQR/Z-score outlier detection from dataset arrays.

---

### 📬 3. Production Storage & Email Bursting Delivery Channels (`Bangplanix.Engine.Bursting`)

Replace current simulation delay stubs (`await Task.Delay(5)`) with robust, production-grade cloud storage and network protocol clients:

- [ ] **Real SMTP Email Dispatcher (`SmtpEmailDeliveryChannel`):**
  - Integrate production `MailKit` / `MimeKit` or authenticated `SmtpClient`.
  - Full support for STARTTLS / SSL (Port 587 / 465 / 25), per-recipient dynamic AES-256 PDF attachments, inline HTML templates, and connection pooling.
- [ ] **Real AWS S3 Cloud Storage Dispatcher (`S3DeliveryChannel`):**
  - Integrate official AWS SDK or zero-dependency AWS SigV4 REST client.
  - Multi-part streaming upload, custom bucket prefixes, object tags, and AWS KMS server-side encryption.
- [ ] **Real Azure Blob Storage Dispatcher (`AzureBlobDeliveryChannel`):**
  - Integrate `Azure.Storage.Blobs` client or Azure REST API.
  - Support for SAS tokens, Azure Managed Identity, and blob tiering (Hot/Cool/Archive).
- [ ] **Real SFTP Network Dispatcher (`SftpDeliveryChannel`):**
  - Integrate `SSH.NET` library for secure file transfer.
  - Password and private key (OpenSSH/RSA/Ed25519) authentication with automatic host key verification and auto-reconnect.

---

### 🛡️ 4. Cryptographic PAdES Digital Signatures & Certified RFC 3161 TSA Timestamps

Upgrade current hash-placeholder and text-token simulations to true cryptographic compliance:

- [ ] **True PAdES / PKCS#7 CMS Detached Signer (`PdfDigitalSigner`):**
  - Upgrade from zero-padded SHA-256 placeholder to true RFC 5652 PKCS#7 / CMS detached cryptographic signatures (`adbe.pkcs7.detached` / `ETSI.CAdES.detached`).
  - Support for X.509 certificate chains, private keys from `.pfx` / `.p12` files, Windows Certificate Store, and Cloud HSMs (Azure Key Vault, AWS CloudHSM).
- [ ] **Real RFC 3161 Time Stamping Protocol (TSP) Client (`ThaiETaxEngine`):**
  - Upgrade from simulated text block (`-----BEGIN TSA TIMESTAMP TOKEN-----`) to true ASN.1 DER encoded RFC 3161 Timestamp Protocol over HTTP.
  - Integration with ETDA-certified National Root CAs / commercial TSAs for Thai e-Tax and European e-Invoicing (Factur-X / Peppol BIS 3.0).

---

### 🖥️ 5. Web Management Portal GUI Mounting, Storage & Advanced Tools (`Bangplanix.Server/Program.cs`)
- [x] **Server Host Route Wiring & Content Negotiation:**
  - Mounted `ManagementPortalServer` directly to `GET /` (when requested by a browser via `Accept: text/html`), `GET /portal`, and `GET /admin`.
  - Preserves 100% JSON status backward compatibility for REST API / cURL clients at `GET /`.
- [x] **Dual Database Storage Layer (SQLite Default & PostgreSQL Option):**
  - Out-of-the-box persistent storage via **SQLite** (`/app/volumes/data/portal.db`) with zero external infrastructure required.
  - Enterprise clustering option via **PostgreSQL** configured via `BANGPLANIX_PORTAL_DB_TYPE=postgres` and `BANGPLANIX_PORTAL_DB_CONNECTION`.
  - Automated schema migration for users, sessions, and audit trail logs.
- [x] **Authentication & Auto-Seeded Default Credentials:**
  - Secure PBKDF2/SHA-256 hashed password verification and tokenized session management (`POST /api/v1/auth/login`, `logout`, `status`).
  - Auto-seeded initial administrator account (`admin` / `bangplanix2026!`) with ENV override support.
  - Full functional parity and UI accessibility for both Guest mode and Logged-in Admin mode.
- [x] **Container Volume File Manager & Security Sandbox:**
  - Full GUI and REST API (`GET/POST/DELETE /api/v1/files`) managing `/templates`, `/data`, `/fonts`, and `/logs`.
  - 100% strict Path Traversal protection preventing directory escape attacks (`../` / `..\`).
- [x] **Web Report Converter Studio:**
  - In-browser Drag & Drop converter supporting 21 legacy report engines (SSRS `.rdl`, Crystal `.rpt.xml`, Jaspersoft `.jrxml`, FastReport `.frx`, DevExpress `.repx`, Stimulsoft `.mrt`).
  - Instant `.bpx` schema preview, single-click download, and direct save into container `/templates`.
- [x] **Interactive Report Playground & Test Sandbox:**
  - Live in-browser rendering to PDF (with embedded viewer iframe) and Excel (XLSX download) from container templates or custom JSON.
- [x] **Live Server & Worker Logs Streamer:**
  - Real-time in-browser log streaming (`GET /api/v1/logs`) with dynamic keyword/level filtering (`[INFO]`, `[WARN]`, `[ERROR]`).
- [x] **Online Commercial License Activation:**
  - Runtime license tier status inspector and online token applicator (`POST /api/v1/license/activate`) without server restart.
- [x] **Database Connection Health & Query Tester:**
  - Ping latency measurement and query connectivity verification for PostgreSQL, SQL Server, MySQL, and SQLite.
- [x] **System Settings & Installed Fonts Inspector:**
  - Runtime environment telemetry and real-time Thai/global TTF/OTF font discovery in `/app/volumes/fonts`.

---

## 🔮 Future Backlog: v1.2.0+

### ☸️ 6. Kubernetes GitOps Operator Daemon Controller
- [ ] **Active Operator Controller Daemon:**
  - Develop an active Kubernetes Controller (Go or C# .NET Worker) to watch and reconcile `BangplanixReportJob` CRDs.
  - Enable declarative GitOps report execution pipelines (ArgoCD / Flux) with automated pod scheduling, retry policies, and output artifact forwarding.

### 📦 7. Polyglot Standalone Client SDK Packages
- [ ] **PHP Client Package:** Official Composer package (`bangplanix/client`) with Guzzle client and Laravel service provider.
- [ ] **Dart / Flutter Package:** Official pub.dev package (`bangplanix`) for cross-platform mobile POS and tablet printing.
- [ ] **Rust Client Crate:** Official crates.io crate (`bangplanix`) with `reqwest` async streaming support.
- [ ] **Ruby Client Gem:** Official RubyGems package (`bangplanix`) with `Net::HTTP` connection pooling and streaming deserialization.

### 📊 8. Autonomous Analytics & Visual Insights
- [ ] **AI Chart Recommendation:** Automatically inspects dataset column types and suggests optimal chart types (Bar, Line, Radar, Waterfall, Sparkline).
- [ ] **Natural Language Drill-Down Queries:** Click any chart element to ask follow-up questions in natural language.

### 🏢 9. Multi-Tenant Enterprise AI Hub
- [ ] Centralized Admin Console for enterprise prompt audit logs, DLP violation alarms, and tenant token consumption dashboards.
- [ ] Fine-tuned local domain models for Thai Government / Tax Invoicing regulations.

