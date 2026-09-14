# 📖 Bangplanix Getting Started Guide & Developer Manual

Welcome to the comprehensive **Bangplanix Enterprise Reporting Engine (.NET 10 / C# 14 / Native AOT)** guide. This manual walks you through setup, template design, frontend embedding, polyglot SDK integration, legacy report migration, and production deployment.

> 🌐 **Available Languages:**
> * 🇬🇧 [English (GETTING-STARTED.md)](./GETTING-STARTED.md)
> * 🇹🇭 [ไทย (GETTING-STARTED.th.md)](./GETTING-STARTED.th.md)
> * 🇨🇳 [简体中文 (GETTING-STARTED.zh.md)](./GETTING-STARTED.zh.md)
> * 🇯🇵 [日本語 (GETTING-STARTED.ja.md)](./GETTING-STARTED.ja.md)
> * 🇪🇸 [Español (GETTING-STARTED.es.md)](./GETTING-STARTED.es.md)

---

## 📑 Table of Contents

1. [🚀 Part 1: Quick Start in 3 Minutes](#-part-1-quick-start-in-3-minutes)
2. [🎨 Part 2: The `.bpx` Schema & Template Design](#-part-2-the-bpx-schema--template-design)
3. [🤖 Part 3: Bangplanix AI Suite Natural Language Generation](#-part-3-bangplanix-ai-suite-natural-language-generation)
4. [💾 Part 4: Data Push & SQL Database Binding](#-part-4-data-push--sql-database-binding)
5. [🖥️ Part 5: Embedding Frontend Web Components (React, Vue, Web Components)](#-part-5-embedding-frontend-web-components-react-vue-web-components)
6. [🔌 Part 6: Backend Polyglot Client SDKs (5 Languages)](#-part-6-backend-polyglot-client-sdks-5-languages)
7. [🔄 Part 7: Legacy Report Migration (Crystal, SSRS, Jasper, FastReport)](#-part-7-legacy-report-migration-crystal-ssrs-jasper-fastreport)
8. [🚢 Part 8: Production Deployment & License Activation](#-part-8-production-deployment--license-activation)

---

## 🚀 Part 1: Quick Start in 3 Minutes

Bangplanix is distributed as a lightweight Native AOT container, CLI utility, and web portal.

### 1.1 Run with Docker Compose (Recommended)
Launch the container using Docker Compose:

```bash
# Start Bangplanix in background
docker compose up -d
```

Access the service immediately:
* **REST API & Status Endpoint:** [`http://localhost:9545`](http://localhost:9545) *(Interactive Web Management Portal GUI in [Roadmap v1.1.0](./ROADMAP.md))*
* **High-Speed gRPC Endpoint:** `localhost:9546`

### 1.2 Run with Docker CLI
```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

### 1.3 System Doctor Health Inspection
Run the diagnostic doctor to verify hardware acceleration (SIMD AVX-512/Neon, AES-GCM, Server GC, and font discovery):
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```

---

## 🎨 Part 2: The `.bpx` Schema & Template Design

Bangplanix report templates are defined in **`.bpx` (Bangplanix JSON Schema v1.0)**, a declarative format parsed at sub-millisecond speeds.

### Basic `.bpx` Template Structure:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "Official Tax Invoice",
    "author": "Acme Global Technologies"
  },
  "pageSetup": {
    "paperKind": "A4",
    "orientation": "Portrait",
    "unit": "Mm",
    "margins": { "top": 10, "bottom": 10, "left": 10, "right": 10 }
  },
  "bands": {
    "pageHeader": {
      "height": 30,
      "elements": [
        {
          "type": "Text",
          "text": "Acme Global Technologies Inc.",
          "x": 0, "y": 0, "width": 190, "height": 10,
          "style": { "fontSize": 14, "fontWeight": "Bold", "color": "#0f172a" }
        }
      ]
    },
    "detail": {
      "height": 8,
      "elements": [
        { "type": "Text", "expression": "=Fields.ItemName", "x": 0, "y": 0, "width": 120, "height": 8 },
        { "type": "Text", "expression": "=Fields.Price", "x": 120, "y": 0, "width": 70, "height": 8, "style": { "align": "Right" } }
      ]
    },
    "pageFooter": {
      "height": 15,
      "elements": [
        {
          "type": "Text",
          "expression": "=\"Total: \" + FormatCurrency(Sum(Fields.Price))",
          "x": 0, "y": 0, "width": 190, "height": 8,
          "style": { "fontSize": 9, "color": "#475569" }
        }
      ]
    }
  }
}
```

### Visual Drag & Drop Designer (`<bangplanix-designer>`)
Install and embed the WYSIWYG Web Component:
```bash
npm install @bangplanix/designer
```
```html
<bangplanix-designer></bangplanix-designer>
```

---

## 🤖 Part 3: Bangplanix AI Suite Natural Language Generation

> 💡 *Note: The core AI engine (`ReportAiGenerator`, `HybridLlmGateway`, `AiSqlSafetyValidator`) is available via .NET and JS SDKs. The interactive Visual AI Designer ribbon buttons and chat dialog are in [Roadmap v1.1.0](./ROADMAP.md).*

Generate complex `.bpx` layouts, calculations, and dataset schemas directly from natural language prompts:

### English Example Prompt:
> *"Generate an executive sales report with a monthly summary chart, tabular line items (SKU, Description, Quantity, UnitPrice, LineTotal), and a footer displaying subtotal, 7% VAT, and grand total."*

The AI engine automatically outputs:
1. Valid `.bpx` schema hierarchy (Page Header, Group Bands, Detail, and Page Footer).
2. Compiled Roslyn formulas (`=Sum(Fields.Quantity * Fields.UnitPrice)`).
3. Barcode and EMVCo QR code visuals.

---

## 💾 Part 4: Data Push & SQL Database Binding

### 4.1 Push JSON Data Directly (Decoupled Microservices)
Send JSON payloads over HTTP/gRPC without giving database credentials to the reporting server:

```json
[
  { "ItemCode": "SKU-001", "ItemName": "High-Performance Cloud Compute", "Price": 1500.00 },
  { "ItemCode": "SKU-002", "ItemName": "Enterprise Bangplanix License", "Price": 499.00 }
]
```

### 4.2 SQL Database Queries (PostgreSQL, MSSQL, MySQL)
Configure parameterized queries with encrypted connection strings in `.bpx`:
```json
{
  "datasets": [
    {
      "name": "InvoiceDataset",
      "type": "Sql",
      "connectionRef": "AesEncryptedConnectionToken",
      "queryOrUrl": "SELECT ItemCode, ItemName, Price FROM OrderItems WHERE OrderId = @OrderId"
    }
  ]
}
```

---

## 🖥️ Part 5: Embedding Frontend Web Components (React, Vue, Web Components)

### 5.1 Install Web Viewer Component
```bash
npm install @bangplanix/viewer
```

### 5.2 Plain HTML / JavaScript
```html
<!DOCTYPE html>
<html>
<head>
  <script type="module" src="node_modules/@bangplanix/viewer/dist/viewer.js"></script>
</head>
<body>
  <bangplanix-viewer 
    src="http://localhost:9545/api/v1/reports/render"
    theme="light"
    zoom="fit-width">
  </bangplanix-viewer>
</body>
</html>
```

### 5.3 React & Next.js Integration
```tsx
import React from 'react';
import '@bangplanix/viewer';

export function InvoiceViewer() {
  return (
    <div style={{ width: '100%', height: '100vh' }}>
      <bangplanix-viewer src="/api/reports/invoice.pdf" />
    </div>
  );
}
```

---

## 🔌 Part 6: Backend Polyglot Client SDKs & Languages

Bangplanix provides 5 strongly-typed official client SDKs (C# .NET, Node.js/TypeScript, Python, Go, and Java). For other environments, you can integrate directly via HTTP/REST or gRPC (standalone client SDK packages for PHP, Dart/Flutter, Rust, and Ruby are currently in [Roadmap v1.2.0](./ROADMAP.md)):

<details open>
<summary><b>🔷 1. C# / .NET SDK (.NET 8 / 9 / 10)</b></summary>

```bash
dotnet add package Bangplanix.Client
```
```csharp
using Bangplanix.Client;

var client = new BangplanixClient("http://localhost:9545");
byte[] pdf = await client.RenderReportAsync(new RenderReportRequest
{
    TemplatePath = "templates/invoice.bpx",
    DataJson = JsonSerializer.Serialize(orders)
});
await File.WriteAllBytesAsync("invoice.pdf", pdf);
```
</details>

<details>
<summary><b>🟢 2. Node.js / TypeScript SDK</b></summary>

```bash
npm install @bangplanix/client
```
```typescript
import { BangplanixClient } from '@bangplanix/client';
import * as fs from 'fs';

const client = new BangplanixClient({ baseUrl: 'http://localhost:9545' });
const pdfBuffer = await client.renderReport({
  templatePath: 'templates/invoice.bpx',
  data: [{ ItemName: 'Server Node', Price: 2500 }]
});
fs.writeFileSync('invoice.pdf', pdfBuffer);
```
</details>

<details>
<summary><b>🐍 3. Python SDK</b></summary>

```bash
pip install bangplanix
```
```python
from bangplanix import BangplanixClient

client = BangplanixClient(base_url="http://localhost:9545")
pdf_bytes = client.render_to_file(
    template_path="templates/invoice.bpx",
    data=[{"ItemName": "Storage Node", "Price": 450}],
    output_path="invoice.pdf"
)
```
</details>

<details>
<summary><b>🔵 4. Go SDK</b></summary>

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
        DataJson:     `[{"ItemName": "RAM Stick", "Price": 120}]`,
    })
    os.WriteFile("invoice.pdf", pdf, 0644)
}
```
</details>

<details>
<summary><b>☕ 5. Java SDK</b></summary>

```java
BangplanixClient client = new BangplanixClient("http://localhost:9545");
byte[] pdf = client.renderReport(new RenderReportRequest()
    .setTemplatePath("templates/invoice.bpx")
    .setDataJson(jsonData));
```
</details>

<details>
<summary><b>🐘 6. PHP (REST API Integration — Standalone Composer Package in Roadmap v1.2.0)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/invoice.bpx',
    'format' => 'Pdf',
    'dataJson' => json_encode([['ItemName' => 'Service Fee', 'Price' => 5000]])
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
<summary><b>🎯 7. Dart / Flutter (REST API Integration — Standalone pub.dev Package in Roadmap v1.2.0)</b></summary>

```dart
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;

Future<void> generateInvoicePdf() async {
  final url = Uri.parse('http://localhost:9545/api/v1/reports/render');
  final response = await http.post(
    url,
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'templatePath': 'templates/invoice.bpx',
      'format': 'Pdf',
      'dataJson': jsonEncode([
        {'ItemName': 'Mobile POS Terminal', 'Price': 8900}
      ])
    }),
  );

  if (response.statusCode == 200) {
    final file = File('invoice.pdf');
    await file.writeAsBytes(response.bodyBytes);
  }
}
```
</details>

<details>
<summary><b>🦀 8. Rust (reqwest Async REST Integration — Standalone Crate in Roadmap v1.2.0)</b></summary>

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
            "dataJson": "[{\"ItemName\":\"High-Speed Compute\",\"Price\":9900}]"
        }))
        .send().await?.bytes().await?;

    std::fs::write("invoice.pdf", res)?;
    Ok(())
}
```
</details>

<details>
<summary><b>💎 9. Ruby (Net::HTTP REST Integration — Standalone Gem in Roadmap v1.2.0)</b></summary>

```ruby
require 'net/http'
require 'uri'
require 'json'

uri = URI.parse("http://localhost:9545/api/v1/reports/render")
payload = {
  templatePath: "templates/invoice.bpx",
  format: "Pdf",
  dataJson: [{ ItemName: "SaaS Monthly Subscription", Price: 1200 }].to_json
}

response = Net::HTTP.post(uri, payload.to_json, { 'Content-Type' => 'application/json' })
File.open("invoice.pdf", "wb") { |f| f.write(response.body) }
```
</details>

<details>
<summary><b>⚡ 10. cURL / Shell Script / Command Line</b></summary>

```bash
curl -X POST http://localhost:9545/api/v1/reports/render \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "templates/invoice.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"Cloud Compute Node\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## 🔄 Part 7: Legacy Report Migration (Crystal, SSRS, Jasper, FastReport)

Convert legacy enterprise reports to `.bpx` with the built-in CLI migration utility:

```bash
# Convert single file (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# Batch migrate entire directory (Crystal XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

### Supported Legacy Formats:
| Legacy System | Extension |
| :--- | :--- |
| **Microsoft SSRS / Power BI** | `.rdl`, `.rdlc` |
| **SAP Crystal Reports** | `.rpt.xml`, `.xml` |
| **SAP Adobe Document Services (ADS) / XFA** | `.xdp`, `.xfa.xml` |
| **BarTender Label Designer** | `.btw`, `.btw.xml`, `.btw.json` |
| **Microsoft Access Database Reports** | `.accdb`, `.mdb`, `.access.txt`, `.access.xml` |
| **TIBCO Jaspersoft** | `.jrxml` |
| **FastReport** | `.frx` |
| **Stimulsoft Reports** | `.mrt` (JSON / XML) |
| **Progress Telerik** | `.trdx`, `.trdp` |
| **DevExpress XtraReports** | `.repx` |
| **Eclipse BIRT** | `.rptdesign`, `.birt.xml` |
| **GrapeCity ActiveReports** | `.rdlx`, `.rpx` |
| **Hitachi Vantara Pentaho** | `.prpt`, `.prpt.xml` |
| **Oracle BI Publisher** | `.xdo`, `.bip.xml`, `.rtf` |
| **IBM Cognos Analytics** | `.spec`, `.cognos.xml` |
| **Handlebars & Mustache** | `.hbs`, `.mustache` |
| **OASIS UBL 2.1 E-Invoicing** | `.ubl`, `.xml` |
| **Zebra ZPL / EPL** | `.zpl`, `.epl` |
| **Epson ESC/POS POS Receipts** | `.escpos`, `.pos` |
| **Office & HTML Templates** | `.docx`, `.xlsx`, `.html`, `.liquid` |

---

## 🚢 Part 8: Production Deployment & License Activation

### 8.1 License Key Activation (Air-Gapped Offline)
Set your cryptographically signed license token via environment variables:

```bash
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMS...<token>"
```
* Automatically unwatermarks generated PDF documents.
* Unlocks unlimited CPU core execution and enterprise features (AI Suite, Report Bursting, PAdES Signatures *(Production CA/TSA & Cloud Drivers in [Roadmap v1.1.0](./ROADMAP.md))*).

### 8.2 Kubernetes Helm Chart Deployment
```bash
helm upgrade --install bangplanix ./deploy/helm/bangplanix \
  --namespace reporting --create-namespace \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set licenseKey="eyJ..."
```

---

## 💬 Community & Support

* 🐞 **Issues & Bugs:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💡 **Discussions:** [GitHub Discussions](https://github.com/thabot/bangplanix/discussions)
* 💼 **Commercial & OEM Inquiries:** `thabot47@gmail.com`
