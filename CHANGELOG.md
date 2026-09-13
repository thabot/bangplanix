# 📋 Bangplanix — Master Project Changelog

All notable changes across all four phases of the Bangplanix enterprise reporting platform are documented in this file.

---

## [1.0.0] — 2026-09-13 (Official Production Release)

### 🌟 Phase 1: Core Engine (.NET 10), Base Visuals, Security & CLI (Sprints 1.1–1.8)
- **Zero-Allocation `.bpx` JSON Parser:** Blazing fast SIMD-accelerated template deserializer with zero runtime heap churn.
- **SkiaSharp 2D Canvas & Banding Architecture:** Precision header, detail, and footer flow calculations with vector PDF rendering.
- **HarfBuzz Complex Text Shaping:** Flawless Thai vowel rendering (สระไม่ลอย), 4-way font fallback, and custom TTF/OTF font loading.
- **Base Visual Components:** High-density vector QR codes (EMVCo PromptPay), GS1-128, Code 128, DataMatrix, and embedded SVG/PNG graphics.
- **Roslyn AST Sandboxing:** Multi-layered security blocking arbitrary RCE, anti-SSRF IP filtering, XXE elimination, and AES-256-GCM KMS encryption.
- **Streaming Big Data Export:** MiniExcel zero-copy XLSX export, partitioned CSV, and direct SQL database connectors (MSSQL, PostgreSQL, MySQL).
- **Alpine AOT Docker Image:** Ultra-lean 38MB container image with gRPC service, CLI tool, structured JSON logging, and interactive Web Viewer component (`<bangplanix-viewer>`).

### 🔌 Phase 2: Integration, Polyglot SDKs, Legacy Adapters & WASM Playground (Sprints 2.1–2.8)
- **Polyglot Client SDKs:** Production-ready client packages for Node.js/TypeScript, Python, Java, Go, and .NET with automatic retry backoff and batch rendering.
- **Interactive Parameter Panel:** Dynamic cascading dropdown filters, parameter validation, and mobile drawer touch navigation.
- **Multi-Dataset Data Federation:** Cross-source in-memory joins (SQL + REST + JSON), aggregations, subtotal/grand total rollups, and BahtText currency polyfills.
- **Legacy Migration Adapters:** Direct conversion of SSRS / Power BI (`.rdl`, `.rdlc`), Crystal Reports (`.rpt`, `.xml`), Jaspersoft (`.jrxml`), FastReport (`.frx`), Stimulsoft (`.mrt`), Telerik (`.trdx`, `.trdp`), DevExpress (`.repx`), Eclipse BIRT (`.rptdesign`), ActiveReports (`.rdlx`, `.rpx`), Oracle Reports (`.rex`, `.xdo`), and Office & HTML/Liquid templates to `.bpx`.
- **Direct Hardware Printing:** ESC/POS thermal slip generator, Zebra ZPL barcode label generator, and Raw TCP 9100 socket printing.
- **WASM In-Browser Engine & VS Code Extension:** Real-time client-side preview in WebAssembly and VS Code IDE live preview editor.

### 📊 Phase 3: Advanced Charts, PDF Assembly, SIEM & Streaming (Sprints 3.1–3.7)
- **Comprehensive Vector Chart Engine:** 10 chart families (Bar, Column, Line, Area, Pie, Doughnut, Gauge, Combo, Radar, Waterfall, Funnel, Sparklines) with SkiaSharp canvas and Thai Heritage palettes.
- **Advanced PDF Assembly & Dossier:** Automatic Table of Contents (TOC), leader dots, hierarchical bookmarks/outlines, link annotations, file attachments (`/AF`), and dossier merging.
- **PDF/UA Accessibility & PAC Compliance:** Tagged PDF Structure Trees (`H1`-`H6`, `Table`, `Figure`), WCAG 2.1 AA 4.5:1 color contrast, natural reading order, and Matterhorn protocol compliance.
- **Enterprise Security & Compliance:** True Vector Redaction, dynamic claim-based data masking, forensic zero-width steganographic watermarks, PAdES digital signatures, Factur-X / Peppol BIS 3.0, and Thai e-Tax Invoice TSA timestamps.
- **Enterprise Hardening & 1,000,000 Rows Streaming:** SIEM event formatting (CEF, LEEF, ECS, Datadog), hot-reload C# DLL plugin manager, and memory ceiling governor (< 128MB RAM).

### 🎨 Phase 4: Web Visual Designer, Bangplanix AI Suite & Cloud IaC (Sprints 4.1–4.5)
- **Web Visual Designer Component (`<bangplanix-designer>`):** Multi-panel WYSIWYG Lit/TypeScript designer with drag-and-drop canvas, snap-to-grid, alignment guides, Monaco editor, chart/barcode configurators, and 2-way `.bpx` binding.
- **Bangplanix AI Suite:** Natural language to `.bpx` generator, AST SQL safety validator, AI expression assistant, statistical anomaly detection (IQR/Z-Score), executive summaries, and multi-model gateway (Gemini, OpenAI, Claude, Ollama) with prompt injection firewall and DLP output redaction.
- **Scheduled Report Bursting System:** Zero-alloc Cron scheduler, data-driven report slicing, 5 delivery channels (SMTP, S3, Azure Blob, SFTP, Webhook), Dead-Letter Queue (DLQ) with 1-click replay, per-recipient password encryption, token-bucket rate limiter, and department zip bundler.
- **Cloud Infrastructure as Code (IaC):** Official Kubernetes Helm Chart, distributed worker queue orchestrator (Redis Streams / In-memory), distributed S3/MinIO font/template sync, Docker management portal GUI, Kubernetes CRD & GitOps operator, Redlock distributed locking, Grafana telemetry dashboard, and cross-region cloud replicator.
- **Quantum-Ready Cryptographic Licensing:** Hybrid Ed25519 and NIST Post-Quantum FIPS 204 ML-DSA-65 (Dilithium) air-gapped verification with Cloudflare Worker Lemon Squeezy gateway.
- **Final Release Gate:** 100% test pass rate across 407 automated tests and zero copyleft license compliance.
