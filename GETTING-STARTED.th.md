# 📖 คู่มือการใช้งาน Bangplanix (Getting Started Guide)

ยินดีต้อนรับสู่คู่มือการเริ่มต้นใช้งาน **Bangplanix Enterprise Reporting Engine (.NET 10 / C# 14 / Native AOT)** ตั้งแต่เริ่มต้นติดตั้ง ออกแบบเทมเพลต ไปจนถึงการนำขึ้นใช้งานจริงบนระบบ Production

> 🌐 **ภาษาอื่นๆ / Available Languages:**
> * 🇬🇧 [English (GETTING-STARTED.md)](./GETTING-STARTED.md)
> * 🇹🇭 [ไทย (GETTING-STARTED.th.md)](./GETTING-STARTED.th.md)
> * 🇨🇳 [简体中文 (GETTING-STARTED.zh.md)](./GETTING-STARTED.zh.md)
> * 🇯🇵 [日本語 (GETTING-STARTED.ja.md)](./GETTING-STARTED.ja.md)
> * 🇪🇸 [Español (GETTING-STARTED.es.md)](./GETTING-STARTED.es.md)

---

## 📑 สารบัญ (Table of Contents)

1. [🚀 บทที่ 1: Quick Start ติดตั้งและรันใน 3 นาที](#-บทที่-1-quick-start-ติดตั้งและรันใน-3-นาที)
2. [🎨 บทที่ 2: โครงสร้างไฟล์ `.bpx` และการออกแบบรายงาน](#-บทที่-2-โครงสร้างไฟล์-bpx-และการออกแบบรายงาน)
3. [🤖 บทที่ 3: การใช้ AI Suite สั่งสร้างรายงานด้วยภาษาพูด](#-บทที่-3-การใช้-ai-suite-สั่งสร้างรายงานด้วยภาษาพูด)
4. [💾 บทที่ 4: การป้อนข้อมูล (JSON Data Push & SQL Databases)](#-บทที่-4-การป้อนข้อมูล-json-data-push--sql-databases)
5. [🖥️ บทที่ 5: การนำไปฝังในหน้าเว็บ Frontend (React, Vue, Web Components)](#-บทที่-5-การนำไปฝังในหน้าเว็บ-frontend-react-vue-web-components)
6. [🔌 บทที่ 6: การเรียกใช้งานผ่าน Backend SDKs (5 ภาษา)](#-บทที่-6-การเรียกใช้งานผ่าน-backend-sdks-5-ภาษา)
7. [🔄 บทที่ 7: การแปลงรายงานเดิม (Crystal, SSRS, Jasper, FastReport)](#-บทที่-7-การแปลงรายงานเดิม-crystal-ssrs-jasper-fastreport)
8. [🚢 บทที่ 8: การนำขึ้นใช้งานบน Production & License Activation](#-บทที่-8-การนำขึ้นใช้งานบน-production--license-activation)

---

## 🚀 บทที่ 1: Quick Start ติดตั้งและรันใน 3 นาที

Bangplanix ให้บริการทั้งในรูปแบบ Standalone Container, CLI Tool และ Web Portal

### 1.1 รันผ่าน Docker Compose (แนะนำ)
สร้างไฟล์ `docker-compose.yml` หรือใช้ไฟล์ในโปรเจกต์:

```bash
# รัน Container ในโหมด Background
docker compose up -d
```

เมื่อรันสำเร็จ สามารถเข้าใช้งานผ่าน Browser ได้ทันที:
* **REST API & Status Endpoint:** [`http://localhost:9545`](http://localhost:9545) *(Interactive Web Management Portal GUI อยู่ใน [Roadmap v1.1.0](./ROADMAP.md))*
* **High-Speed gRPC Endpoint:** `localhost:9546`

### 1.2 รันผ่าน Docker CLI โดยตรง
```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

### 1.3 ตรวจสอบความพร้อมของระบบด้วย `thabot doctor`
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```
ระบบจะตรวจเช็ค .NET Runtime, Server GC, SIMD Hardware Acceleration, AES-256 Crypto, และ Fonts พร้อมรายงานผลทันที

---

## 🎨 บทที่ 2: โครงสร้างไฟล์ `.bpx` และการออกแบบรายงาน

เทมเพลตรายงานของ Bangplanix จัดเก็บในรูปแบบ **`.bpx` (Bangplanix JSON Schema)** ซึ่งเป็น Plain JSON ที่ Zero-Allocation Parser อ่านได้อย่างรวดเร็ว

### โครงสร้างหลักของไฟล์ `.bpx`:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "ใบเสร็จรับเงิน / Tax Invoice",
    "author": "Siam Enterprise Co., Ltd."
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
          "text": "บริษัท สยามเทคโนโลยี ซิสเต็มส์ จำกัด",
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
          "expression": "=\"รวมทั้งสิ้น \" + BahtText(Sum(Fields.Price))",
          "x": 0, "y": 0, "width": 190, "height": 8,
          "style": { "fontSize": 9, "color": "#475569" }
        }
      ]
    }
  }
}
```

### การใช้งาน Visual Designer (`<bangplanix-designer>`)
หากต้องการออกแบบรายงานแบบ Drag & Drop ผ่านหน้าจอเว็บ:
1. ติดตั้ง `@bangplanix/designer`:
   ```bash
   npm install @bangplanix/designer
   ```
2. ฝังลงในหน้าเว็บ:
   ```html
   <bangplanix-designer></bangplanix-designer>
   ```

---

## 🤖 บทที่ 3: การใช้ AI Suite สั่งสร้างรายงานด้วยภาษาพูด

> 💡 *หมายเหตุ: Core AI Engine (`ReportAiGenerator`, `HybridLlmGateway`, `AiSqlSafetyValidator`) สามารถเรียกใช้งานผ่าน C# และ JavaScript SDK ได้ในปัจจุบัน ส่วนปุ่มสั่งการ Visual AI Designer บนหน้าจอ Ribbon และหน้าต่างแชทอยู่ใน [Roadmap v1.1.0](./ROADMAP.md)*

Bangplanix มาพร้อมกับ **Bangplanix AI Suite** ช่วยให้สร้างเทมเพลต `.bpx` ได้จาก Natural Language ทั้งภาษาไทยและภาษาอังกฤษ

### ตัวอย่างการสั่งงานด้วยภาษาไทย:
> *"สร้างใบเสร็จรับเงินสำหรับบริษัทไอที มีรหัสสินค้า ชื่อสินค้า จำนวน ราคา และยอดรวม ด้านล่างใส่ PromptPay QR Code และยอดเงินบาทถ้วน"*

AI จะสร้างโครงสร้าง `.bpx` ที่ประกอบด้วย:
1. **Header:** โลโก้และข้อมูลผู้ขาย
2. **Detail Band:** ตารางสินค้าพร้อม Expression เชื่อมโยงข้อมูล
3. **Footer:** สรุปยอดเงินพร้อมฟังก์ชัน `=BahtText(Sum(Fields.Amount))` และ QR Code EMVCo PromptPay

---

## 💾 บทที่ 4: การป้อนข้อมูล (JSON Data Push & SQL Databases)

### 4.1 ส่งข้อมูล JSON ตรงเข้า Engine (Microservices Pattern)
ไม่จำเป็นต้องต่อ Database ตรงกับ Engine สามารถส่ง JSON Payload เข้ามาทาง API ได้ทันที:

```json
[
  { "ItemCode": "SKU-001", "ItemName": "Cloud Server Subscription", "Price": 15000.00 },
  { "ItemCode": "SKU-002", "ItemName": "Bangplanix Pro License", "Price": 85000.00 }
]
```

### 4.2 เชื่อมต่อกับฐานข้อมูล SQL (SQL Server, PostgreSQL, MySQL)
ระบุ Dataset ในไฟล์ `.bpx`:
```json
{
  "datasets": [
    {
      "name": "InvoiceData",
      "type": "Sql",
      "connectionRef": "EncryptedConnectionStringHere",
      "queryOrUrl": "SELECT ItemCode, ItemName, Price FROM Invoices WHERE CustomerId = @CustomerId"
    }
  ]
}
```

---

## 🖥️ บทที่ 5: การนำไปฝังในหน้าเว็บ Frontend (React, Vue, Web Components)

### 5.1 ติดตั้ง Web Viewer Component
```bash
npm install @bangplanix/viewer
```

### 5.2 ใช้งานใน HTML / Vanilla JS
```html
<!DOCTYPE html>
<html>
<head>
  <script type="module" src="node_modules/@bangplanix/viewer/dist/viewer.js"></script>
</head>
<body>
  <!-- แปะหน้าจอ Preview รายงาน -->
  <bangplanix-viewer 
    src="http://localhost:9545/api/v1/reports/render"
    theme="light"
    zoom="fit-width">
  </bangplanix-viewer>
</body>
</html>
```

### 5.3 ใช้งานใน React (Next.js / Vite)
```tsx
import React, { useEffect, useRef } from 'react';
import '@bangplanix/viewer';

export function ReportViewerPage({ reportData }) {
  return (
    <div style={{ width: '100%', height: '100vh' }}>
      <bangplanix-viewer 
        src="/api/reports/invoice.pdf"
        ref={(el) => {
          if (el) el.parameters = { CustomerId: 1001 };
        }}
      />
    </div>
  );
}
```

---

## 🔌 บทที่ 6: การเรียกใช้งานผ่าน Backend SDKs และภาษาต่างๆ

Bangplanix มี Official Client SDKs 5 ภาษาหลักแบบ Strongly-typed (C# .NET, Node.js/TypeScript, Python, Go, และ Java) และรองรับการเชื่อมต่อผ่าน HTTP/REST หรือ gRPC API สำหรับภาษาอื่นๆ (ส่วน Standalone Client SDK Packages สำหรับ PHP, Dart/Flutter, Rust, Ruby อยู่ใน [Roadmap v1.2.0](./ROADMAP.md)):

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
    DataJson = JsonSerializer.Serialize(myDataset)
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
  data: [ { ItemName: 'Laptop', Price: 35000 } ]
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
    data=[{"ItemName": "Monitor", "Price": 8500}],
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
    pdf, err := client.RenderReport(context.Background(), &bangplanix.RenderRequest{
        TemplatePath: "templates/invoice.bpx",
        DataJson:     `[{"ItemName": "Keyboard", "Price": 2500}]`,
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
<summary><b>🐘 6. PHP (เชื่อมต่อผ่าน REST API — Standalone Composer Package อยู่ใน Roadmap v1.2.0)</b></summary>

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
<summary><b>🎯 7. Dart / Flutter (เชื่อมต่อผ่าน REST API — Standalone pub.dev Package อยู่ใน Roadmap v1.2.0)</b></summary>

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
        {'ItemName': 'Mobile POS Machine', 'Price': 8900}
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
<summary><b>🦀 8. Rust (เชื่อมต่อผ่าน reqwest Async REST API — Standalone Crate อยู่ใน Roadmap v1.2.0)</b></summary>

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
<summary><b>💎 9. Ruby (เชื่อมต่อผ่าน Net::HTTP REST API — Standalone Gem อยู่ใน Roadmap v1.2.0)</b></summary>

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
    "dataJson": "[{\"ItemName\":\"Cloud Server\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## 🔄 บทที่ 7: การแปลงรายงานเดิม (Crystal, SSRS, Jasper, FastReport)

หากคุณมีรายงานเดิมจากระบบอื่นๆ สามารถใช้คำสั่ง **`thabot migrate`** แปลงไฟล์เป็น `.bpx` อัตโนมัติทันที:

```bash
# แปลงไฟล์เดี่ยว (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# แปลงทั้งโฟลเดอร์ (Batch Migration Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy_crystal/ -o ./migrated_bpx/
```

### ฟอร์แมตเดิมที่รองรับการแปลง:
| Engine เดิม | นามสกุลไฟล์ |
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
| **Office & Web Templates** | `.docx`, `.xlsx`, `.html`, `.liquid` |

---

## 🚢 บทที่ 8: การนำขึ้นใช้งานบน Production & License Activation

### 8.1 การเปิดใช้งาน License Key (Air-Gapped Offline)
เมื่อได้รับ Commercial License Token จากการสั่งซื้อ ให้นำไปใส่ใน Environment Variable:

```bash
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMS...<payload>.<signature>"
```
เมื่อ Server ตรวจพบ License Key ที่ถูกต้อง:
* ลายน้ำ "Generated by Bangplanix Community" จะถูกปลดล็อกอัตโนมัติ
* ขยายโควตา CPU Cores และเปิดใช้งานฟีเจอร์ระดับ Enterprise (AI Suite, Report Bursting, ลายมือชื่อ PAdES *(Production CA/TSA & Cloud Drivers อยู่ใน [Roadmap v1.1.0](./ROADMAP.md))*))

### 8.2 การ Deploy บน Kubernetes ด้วย Helm Chart
```bash
# ติดตั้ง Bangplanix Helm Chart
helm upgrade --install bangplanix ./deploy/helm/bangplanix \
  --namespace reporting --create-namespace \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set licenseKey="eyJ..."
```

---

## 💬 การขอรับความช่วยเหลือและร่วมพัฒนา

* 🐞 **แจ้งปัญหา / ข้อผิดพลาด:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💡 **พูดคุยแลกเปลี่ยนความคิดเห็น:** [GitHub Discussions](https://github.com/thabot/bangplanix/discussions)
* 💼 **ติดต่อสอบถาม License เชิงพาณิชย์ & OEM:** `thabot47@gmail.com`
