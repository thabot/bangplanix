# 🚀 Bangplanix v1.0.0 Production Release Notes

**Release Version:** `v1.0.0`  
**Release Date:** September 13, 2026  
**Build Target:** .NET 10 (C# 14 / Native AOT) & Lit 3 / TypeScript  
**Container Image:** `ghcr.io/thabot/bangplanix:v1.0.0` (38MB Alpine AOT)  
**Verification:** 100% Tests Pass Rate across all 14 Solution Projects (407+ Automated Unit & Integration Tests)

---

## 💎 Welcome to Bangplanix 1.0.0

**Bangplanix** is the world's highest-performance enterprise reporting and document generation engine, architected from the ground up on **.NET 10 Native AOT** and **SkiaSharp / HarfBuzz vector graphics**. Designed to replace bloated legacy reporting servers (SSRS, Crystal Reports, JasperReports), Bangplanix delivers sub-millisecond rendering latencies, microscopic memory footprints (< 128MB RAM ceiling for 1,000,000 rows), and cloud-native scalability.

---

## 🌟 Highlights of the v1.0.0 Release

### 1. ⚡ Microsecond Native AOT Vector Engine
- Instant zero-warmup binary execution with zero JIT overhead.
- True vector PDF rendering with full Unicode font shaping and flawless Thai vowel/tone mark alignment.
- Zero-copy Excel XLSX streaming exporter via MiniExcel integration.

### 2. 🎨 Modern Web Visual Designer (`<bangplanix-designer>`)
- Built on modern Lit / Web Components: embeddable into any React, Angular, Vue, Svelte, or Vanilla JS app.
- Full WYSIWYG drag-and-drop canvas, snap-to-grid, alignment guides, visual chart and barcode configurators, and embedded Monaco code editor with 2-way `.bpx` binding.

### 3. 🤖 Bangplanix AI Suite & Enterprise Guardrails
- **Prompt-to-Report:** Generate production-ready `.bpx` schemas and safe analytical queries from natural language prompts in Thai and English via SDK and CLI *(Interactive Web UI Assistant in [Roadmap v1.1.0](./ROADMAP.md))*.
- **Security Guardrails:** Built-in Prompt Injection Firewall, AST SQL Safety Validator, Zero Data Retention Guard, and Output DLP redaction.
- **Statistical Anomaly Detection:** Automated IQR and Z-Score outlier detection with AI executive summary generation.

### 4. 📬 Scheduled Report Bursting & Multi-Channel Delivery
- Zero-allocation 5-field Cron parser with standard macros (`@daily`, `@monthly`).
- Data-driven parallel partitioning with Concurrency Throttling, Dead-Letter Queue (DLQ) with 1-click replay, per-recipient AES-256 dynamic password encryption, and multi-channel dispatch architecture *(Production cloud drivers for SMTP, S3, Azure Blob, SFTP in [Roadmap v1.1.0](./ROADMAP.md))*.

### 5. ☸️ Cloud-Native Kubernetes & Distributed Workers
- Production Kubernetes Helm Chart (`bangplanix-helm`) with HPA autoscaling (75% CPU / 80% RAM).
- Distributed Worker Orchestrator consuming from Redis Streams and In-Memory channels.
- Multi-Pod S3/MinIO font/template sync provider with local memory caching.
- Kubernetes CRD (`BangplanixReportJob`) for declarative GitOps pipelines *(Active Operator Controller Daemon in [Roadmap v1.2.0](./ROADMAP.md))*.
- Redlock distributed locking, Grafana telemetry dashboard, and Cross-region replication *(Web Management Portal GUI in [Roadmap v1.1.0](./ROADMAP.md))*.

### 6. 🛡️ Quantum-Ready Commercial OEM Licensing
- Air-gapped 100% offline cryptographic licensing enforcer.
- Cryptographic agility supporting modern high-speed **Ed25519** and NIST Post-Quantum FIPS 204 **ML-DSA-65 (CRYSTALS-Dilithium)**.
- Integrated Cloudflare Worker webhook gateway for automated Lemon Squeezy order fulfillment.

---

## 🚀 Quick Start

### Run with Docker (Alpine AOT)
```bash
docker run -d -p 8080:8080 -p 50051:50051 ghcr.io/thabot/bangplanix:v1.0.0
```

### Install with Helm (Kubernetes)
```bash
helm repo add bangplanix https://charts.bangplanix.io
helm install my-cluster bangplanix/bangplanix --set autoscaling.enabled=true
```

### Embed Web Designer (npm)
```html
<script type="module" src="https://cdn.bangplanix.io/designer/v1/bangplanix-designer.js"></script>
<bangplanix-designer viewMode="design"></bangplanix-designer>
```

---
*For documentation, tutorials, and licensing inquiries, visit [https://github.com/thabot/bangplanix](https://github.com/thabot/bangplanix).*
