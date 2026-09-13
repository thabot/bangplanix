# 🚀 Bangplanix — High-Performance Enterprise Reporting Engine

[![.NET 10](https://img.shields.io/badge/.NET-10.0%20(Native%20AOT)-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)
[![Tests](https://img.shields.io/badge/Tests-428%2F428%20Passed%20(100%25)-success)](https://github.com/thabot/bangplanix/actions)
[![Architecture](https://img.shields.io/badge/Architecture-Zero--GC%20%7C%20SIMD-orange)](GETTING-STARTED.md)
[![Zero Copyleft](https://img.shields.io/badge/Compliance-100%25%20Permissive-brightgreen)](LICENSE-AUDIT.md)

[![Docker Image](https://img.shields.io/badge/Docker%20Hub-thabot%2Fbangplanix-blue?logo=docker&logoColor=white)](https://hub.docker.com/r/thabot/bangplanix)
[![NuGet](https://img.shields.io/badge/NuGet-Bangplanix.Client%20v1.0.0-004880?logo=nuget)](https://www.nuget.org/packages/Bangplanix.Client/)
[![npm](https://img.shields.io/badge/npm-%40bangplanix%2Fclient-CB3837?logo=npm)](https://www.npmjs.com/package/@bangplanix/client)
[![PyPI](https://img.shields.io/badge/PyPI-bangplanix-3775A9?logo=pypi&logoColor=white)](https://pypi.org/project/bangplanix/)
[![Go Reference](https://img.shields.io/badge/Go-pkg.go.dev-00ADD8?logo=go&logoColor=white)](https://pkg.go.dev/github.com/thabot/bangplanix/sdk/go)

> **Bangplanix** is a high-performance, microsecond-level enterprise reporting engine and web middleware built on **.NET 10 (C# 14 / Native AOT)**. Designed as a modern, open-core replacement for legacy reporting suites (Crystal Reports, SSRS, Jaspersoft, FastReport, BIRT, DevExpress).

---

### 📦 Official Packages & Container Registries

| Ecosystem | Registry / Package | Install Command |
| :--- | :--- | :--- |
| 🐳 **Docker Hub** | [`thabot/bangplanix`](https://hub.docker.com/r/thabot/bangplanix) | `docker pull thabot/bangplanix:latest` |
| 🐙 **GitHub Container (GHCR)** | [`ghcr.io/thabot/bangplanix`](https://github.com/thabot/bangplanix/pkgs/container/bangplanix) | `docker pull ghcr.io/thabot/bangplanix:v1.0.0` |
| 🔷 **NuGet (.NET)** | [`Bangplanix.Client`](https://www.nuget.org/packages/Bangplanix.Client/) | `dotnet add package Bangplanix.Client` |
| 🟢 **npm (Node.js/TS)** | [`@bangplanix/client`](https://www.npmjs.com/package/@bangplanix/client) | `npm install @bangplanix/client` |
| 🐍 **PyPI (Python)** | [`bangplanix`](https://pypi.org/project/bangplanix/) | `pip install bangplanix` |
| 🔵 **Go Packages** | [`github.com/thabot/bangplanix/sdk/go`](https://pkg.go.dev/github.com/thabot/bangplanix/sdk/go) | `go get github.com/thabot/bangplanix/sdk/go` |

---

### 📚 Essential Quick Links

* 🌐 **Live Interactive WASM Playground:** [Try in Browser (GitHub Pages)](https://thabot.github.io/bangplanix/)
* ⚡ **Performance Benchmark Report:** [BENCHMARK.md](./BENCHMARK.md)
* 📁 **Popular Starter Templates Pack:** [samples/templates/](./samples/templates/)
* 🇬🇧 **Developer Guide (English):** [GETTING-STARTED.md](./GETTING-STARTED.md)
* 🇹🇭 **คู่มือการใช้งานภาษาไทย (Thai):** [GETTING-STARTED.th.md](./GETTING-STARTED.th.md)
* 🇨🇳 **快速入门指南 (Simplified Chinese):** [GETTING-STARTED.zh.md](./GETTING-STARTED.zh.md)
* 🇯🇵 **スタートアップガイド (Japanese):** [GETTING-STARTED.ja.md](./GETTING-STARTED.ja.md)
* 🇪🇸 **Guía de Inicio Rápido (Spanish):** [GETTING-STARTED.es.md](./GETTING-STARTED.es.md)
* 💎 **Commercial Licensing & Enterprise Tiers:** [PRICING.md](./PRICING.md)
* 📜 **Release Notes v1.0.0:** [RELEASE-NOTES-v1.0.0.md](./RELEASE-NOTES-v1.0.0.md)

---

## ⚡ Key Highlights & Core Capabilities

* 🚀 **Microsecond Execution Speed:** Compiled to Native Machine Code with **.NET 10 Native AOT**, running with **Zero-GC memory pressure** (`ReadOnlySpan<T>`, `ArrayPool<T>`, and SIMD hardware acceleration).
* 🇹🇭 **Complete Thai & Global Typography:** Integrated **HarfBuzzSharp** complex text shaping (สระและวรรณยุกต์ไม่ลอย), Buddhist calendar formatting, and zero-allocation `=BahtText()` currency conversion.
* 🤖 **Bangplanix AI Suite:** Generate and modify `.bpx` report schemas from natural language prompts with built-in SQL AST security sanitization and PII redaction.
* 🎨 **Framework-Agnostic Web Components:** Embed the visual WYSIWYG Designer (`<bangplanix-designer>`) and High-Resolution Viewer (`<bangplanix-viewer>`) across React, Vue, Angular, or plain HTML.
* 🔄 **18+ Legacy Migration Adapters:** Convert existing legacy reports (SSRS RDL, Crystal Reports, Jaspersoft JRXML, FastReport FRX, Stimulsoft MRT, DevExpress REPX, Telerik TRDX, Eclipse BIRT, ActiveReports, Pentaho PRPT, Oracle BI Publisher RTF, IBM Cognos, Handlebars/Mustache HTML, UBL 2.1 e-Invoicing, Zebra ZPL/EPL, Epson ESC/POS) in seconds via CLI.
* 🖨️ **Direct Hardware Printing:** Output directly to network raw sockets (TCP 9100, CUPS, IPP), thermal POS slip printers (ESC/POS), and industrial barcode printers (Zebra ZPL II).
* 🛡️ **Enterprise Zero-Trust Security:** True Vector Redaction, AES-256 DRM encryption, PAdES digital signatures, e-Tax Invoice ETDA (มธอ. 3-2560) compliance, and SIEM cryptographic audit trails.
* 🔌 **Polyglot Client SDKs:** Strongly-typed client libraries for **C# (.NET)**, **Node.js/TypeScript**, **Python**, **Go**, and **Java**.

---

## 🚀 Quick Start in 30 Seconds

### 1. Launch with Docker Compose
```bash
docker compose up -d
```

Access the service immediately:
* **Web Management Portal:** [`http://localhost:9545`](http://localhost:9545)
* **High-Speed gRPC Endpoint:** `localhost:9546`

### 2. Verify System Health with System Doctor
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```

---

## 💻 Polyglot Client SDK Quick Examples

Choose your preferred language / stack:

<details open>
<summary><b>🔷 C# / .NET SDK (<a href="https://www.nuget.org/packages/Bangplanix.Client/">NuGet: Bangplanix.Client</a>)</b></summary>

```bash
dotnet add package Bangplanix.Client
```
```csharp
using Bangplanix.Client;

var client = new BangplanixClient("http://localhost:9545");
byte[] pdf = await client.RenderReportAsync(new RenderReportRequest
{
    TemplatePath = "templates/invoice.bpx",
    DataJson = JsonSerializer.Serialize(myDataset)
});
await File.WriteAllBytesAsync("invoice.pdf", pdf);
```
</details>

<details>
<summary><b>🟢 Node.js / TypeScript SDK (<a href="https://www.npmjs.com/package/@bangplanix/client">npm: @bangplanix/client</a>)</b></summary>

```bash
npm install @bangplanix/client
```
```typescript
import { BangplanixClient } from '@bangplanix/client';
import * as fs from 'fs';

const client = new BangplanixClient({ baseUrl: 'http://localhost:9545' });
const pdfBuffer = await client.renderReport({
  templatePath: 'templates/invoice.bpx',
  data: [ { ItemName: 'Cloud Compute', Price: 15000 } ]
});
fs.writeFileSync('invoice.pdf', pdfBuffer);
```
</details>

<details>
<summary><b>🐍 Python SDK (<a href="https://pypi.org/project/bangplanix/">PyPI: bangplanix</a>)</b></summary>

```bash
pip install bangplanix
```
```python
from bangplanix import BangplanixClient

client = BangplanixClient(base_url="http://localhost:9545")
client.render_to_file("templates/invoice.bpx", data=[{"ItemName": "Cloud Node", "Price": 15000}], output_path="invoice.pdf")
```
</details>

<details>
<summary><b>🔵 Go SDK (<a href="https://pkg.go.dev/github.com/thabot/bangplanix/sdk/go">pkg.go.dev</a>)</b></summary>

```bash
go get github.com/thabot/bangplanix/sdk/go
```
```go
package main

import (
    "context"
    "os"
    bangplanix "github.com/thabot/bangplanix/sdk/go"
)

func main() {
    client := bangplanix.NewClient("http://localhost:9545")
    pdf, _ := client.RenderReport(context.Background(), &bangplanix.RenderRequest{
        TemplatePath: "templates/invoice.bpx",
        DataJson:     `[{"ItemName": "High-Speed Node", "Price": 15000}]`,
    })
    os.WriteFile("invoice.pdf", pdf, 0644)
}
```
</details>

<details>
<summary><b>☕ Java SDK</b></summary>

```java
BangplanixClient client = new BangplanixClient("http://localhost:9545");
byte[] pdf = client.renderReport(new RenderReportRequest()
    .setTemplatePath("templates/invoice.bpx")
    .setDataJson(jsonData));
```
</details>

<details>
<summary><b>🐘 PHP (Laravel / Native cURL)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/invoice.bpx',
    'format' => 'Pdf',
    'dataJson' => json_encode([['ItemName' => 'Cloud Server', 'Price' => 15000]])
];

$ch = curl_init('http://localhost:9545/api/v1/reports/render');
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, ['Content-Type: application/json']);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($payload));

$pdf = curl_exec($ch);
curl_close($ch);
file_put_contents('invoice.pdf', $pdf);
?>
```
</details>

<details>
<summary><b>🎯 Dart / Flutter (Mobile POS & Tablet)</b></summary>

```dart
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;

Future<void> generateInvoicePdf() async {
  final response = await http.post(
    Uri.parse('http://localhost:9545/api/v1/reports/render'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'templatePath': 'templates/invoice.bpx',
      'format': 'Pdf',
      'dataJson': jsonEncode([{'ItemName': 'Mobile POS Terminal', 'Price': 8900}])
    }),
  );

  if (response.statusCode == 200) {
    await File('invoice.pdf').writeAsBytes(response.bodyBytes);
  }
}
```
</details>

<details>
<summary><b>🦀 Rust (reqwest Async)</b></summary>

```rust
use reqwest::Client;
use serde_json::json;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let client = Client::new();
    let res = client.post("http://localhost:9545/api/v1/reports/render")
        .json(&json!({
            "templatePath": "templates/invoice.bpx",
            "format": "Pdf",
            "dataJson": "[{\"ItemName\":\"Compute Node\",\"Price\":9900}]"
        }))
        .send().await?.bytes().await?;

    std::fs::write("invoice.pdf", res)?;
    Ok(())
}
```
</details>

<details>
<summary><b>💎 Ruby (Net::HTTP)</b></summary>

```ruby
require 'net/http'
require 'uri'
require 'json'

uri = URI.parse("http://localhost:9545/api/v1/reports/render")
payload = {
  templatePath: "templates/invoice.bpx",
  format: "Pdf",
  dataJson: [{ ItemName: "SaaS Subscription", Price: 1200 }].to_json
}

response = Net::HTTP.post(uri, payload.to_json, { 'Content-Type' => 'application/json' })
File.open("invoice.pdf", "wb") { |f| f.write(response.body) }
```
</details>

<details>
<summary><b>⚡ cURL / CLI / Shell</b></summary>

```bash
curl -X POST http://localhost:9545/api/v1/reports/render \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "templates/invoice.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"Cloud Compute\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## ⚡ High-Throughput Performance Benchmarks

| Metric / Scenario | 🚀 Bangplanix (.NET 10 Native AOT) | 🐢 SAP Crystal Reports | ☕ TIBCO Jaspersoft | 🌐 Puppeteer / Chrome |
| :--- | :---: | :---: | :---: | :---: |
| **Single Invoice Latency (p50)** | **0.42 ms** | 42.8 ms *(101x slower)* | 38.5 ms *(91x slower)* | 148.0 ms *(352x slower)* |
| **Batch 10,000 Invoices** | **0.18 sec** *(55,500 p/s)* | 4.85 sec | 4.10 sec | 9.20 sec |
| **Peak RAM (10k Batch)** | **24.5 MB** *(Zero-GC)* | 185.0 MB | 460.0 MB | 680.0 MB |
| **Container Size** | **26.8 MB** *(Distroless)* | Windows Core (4.8 GB) | OpenJDK/Tomcat (420 MB) | Node/Chrome (640 MB) |

> 📊 *For complete methodology and reproducible reproduction steps, see [BENCHMARK.md](./BENCHMARK.md).*

---

## 📊 Supported Document Formats & Legacy Adapters

| Category | Supported Technologies |
| :--- | :--- |
| **Output Formats** | PDF & PDF/A, PDF/UA (Section 508), Encrypted PDF (AES-256), Excel (MiniExcel XLSX), Word (DOCX), HTML5 / Lit, SVG, PNG, WebP, CSV / NDJSON |
| **Hardware & Printing** | Raw Socket TCP 9100, CUPS, IPP, Thermal POS (ESC/POS), Industrial Labels (Zebra ZPL II) |
| **Legacy Migration** | SSRS / Power BI (`.rdl`, `.rdlc`), Crystal Reports (`.rpt`, `.xml`, `.rptxml`), Jaspersoft (`.jrxml`), FastReport (`.frx`), Stimulsoft (`.mrt`), Telerik (`.trdx`, `.trdp`), DevExpress (`.repx`), BIRT (`.rptdesign`), ActiveReports (`.rdlx`, `.rpx`), Oracle Reports (`.rex`, `.xdo`), Office & Web (`.docx`, `.xlsx`, `.html`, `.liquid`) |
| **Data Connectors** | SQL Server, PostgreSQL, MySQL, ClickHouse, Snowflake, Google BigQuery, REST API, JSON Push Stream |

---

## 💰 Commercial Licensing & Instant Checkout

Bangplanix is available under **Dual Licensing**:
- **Community Edition (MIT):** Free for evaluation, non-commercial, and open-source projects.
- **Developer Pro ($49/mo):** Royalty-free redistributable runtime, unwatermarked PDF rendering, all 10 SDKs. [👉 Buy Developer Pro](https://bangplanix.lemonsqueezy.com/checkout/buy/99f67d3d-d04f-40de-be6f-a28c06c6624e?discount=0)
- **Enterprise Cloud ($499/mo):** Unlimited Kubernetes worker nodes, Post-Quantum ML-DSA signatures, 24/7 SLA. [👉 Buy Enterprise Cloud](https://bangplanix.lemonsqueezy.com/checkout/buy/e585b5a6-a630-4eeb-ab80-db0553bf35b4)

> 💳 *Detailed features breakdown, comparisons, and custom enterprise invoicing are available in [PRICING.md](./PRICING.md).*

---

## 💬 Community & Support

* 📖 **Documentation:** Read the [Getting Started Guide](./GETTING-STARTED.md)
* 🌐 **WebAssembly Live Playground:** [Interactive Browser Engine](https://thabot.github.io/bangplanix/)
* 🐞 **Issues & Feedback:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💼 **Enterprise Licensing & OEM Partnerships:** `thabot47@gmail.com`

---

## 📜 License & Compliance

Bangplanix is released under the **MIT License** for community evaluation. For commercial licenses and high-core cluster licensing, see [PRICING.md](./PRICING.md) and [LICENSE-COMMERCIAL.md](./LICENSE-COMMERCIAL.md).
