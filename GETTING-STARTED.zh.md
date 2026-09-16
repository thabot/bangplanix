# 📖 Bangplanix 快速入门与开发指南 (Getting Started Guide)

欢迎使用 **Bangplanix 高性能企业级报表引擎 (.NET 10 / C# 14 / Native AOT)**。本指南将指导您完成从快速启动、模板设计、AI 辅助、后端 SDK 集成到企业级生产环境部署的全流程。

> 🌐 **多语言版本 / Available Languages:**
> * 🇬🇧 [English (GETTING-STARTED.md)](./GETTING-STARTED.md)
> * 🇹🇭 [ไทย (GETTING-STARTED.th.md)](./GETTING-STARTED.th.md)
> * 🇨🇳 [简体中文 (GETTING-STARTED.zh.md)](./GETTING-STARTED.zh.md)
> * 🇯🇵 [日本語 (GETTING-STARTED.ja.md)](./GETTING-STARTED.ja.md)
> * 🇪🇸 [Español (GETTING-STARTED.es.md)](./GETTING-STARTED.es.md)

---

## 📑 目录 (Table of Contents)

1. [🚀 第 1 章: 架构选型与 3 大运行模式 (Execution Modes)](#-第-1-章-架构选型与-3-大运行模式-execution-modes)
2. [🎨 第 2 章: `.bpx` 模板架构与全球标准模板库 (Starter Templates Pack)](#-第-2-章-bpx-模板架构与全球标准模板库-starter-templates-pack)
3. [🤖 第 3 章: Bangplanix AI Suite 自然语言生成报表](#-第-3-章-bangplanix-ai-suite-自然语言生成报表)
4. [💾 第 4 章: 数据注入 (JSON Push & SQL 数据库)](#-第-4-章-数据注入-json-push--sql-数据库)
5. [🖥️ 第 5 章: 前端 Web 组件嵌入 (React, Vue, Web Components)](#-第-5-章-前端-web-组件嵌入-react-vue-web-components)
6. [🔌 第 6 章: 多语言后端 SDK 与 3 种返回格式 (FilePath, Stream, Base64)](#-第-6-章-多语言后端-sdk-与-3-种返回格式-filepath-stream-base64)
7. [🔄 第 7 章: 传统报表系统一键迁移 (Crystal, SSRS, Jasper, FastReport)](#-第-7-章-传统报表系统一键迁移-crystal-ssrs-jasper-fastreport)
8. [🚢 第 8 章: 生产环境部署与许可证激活](#-第-8-章-生产环境部署与许可证激活)
9. [🛠️ 第 9 章: 全球字体排版与常见问题解答 (Typography & FAQ)](#-第-9-章-全球字体排版与常见问题解答-typography--faq)

---

## 🚀 第 1 章: 架构选型与 3 大运行模式 (Execution Modes)

Bangplanix 提供 **3 种高度灵活的运行与部署模式**，完美适配从单体应用到分布式微服务的不同技术栈：

### 📊 运行模式选型矩阵 (Decision Matrix)

| 维度 | 模式 1: C# 进程内嵌入库 | 模式 2: 独立 CLI / Native AOT | 模式 3: Docker 微服务 |
| :--- | :--- | :--- | :--- |
| **主要目标人群** | .NET / C# 开发者 (类似 QuestPDF) | CI/CD, 自动化脚本, 终端运维 | 多语言团队 (Node, Python, Go, Java...) |
| **外部依赖** | 仅 NuGet 包 (零外部依赖) | 仅单文件可执行文件 (零 Docker) | Docker / Kubernetes |
| **生成延迟** | **亚毫秒级 (< 1ms)** | 极快 (~10ms) | 网络 I/O 延迟 (~5-15ms) |
| **多语言支持** | C# / F# / VB.NET | CLI 命令行 / Shell 脚本 | 官方多语言 SDK & REST / gRPC |
| **适用场景** | 桌面应用 (WPF/MAUI), 极速 ASP.NET Core | 批处理管道, GitHub Actions | 企业级微服务、异构云原生架构 |

---

### 📦 模式 1: 进程内嵌入式 C# 库 (In-Process C# Library)
适合 .NET 开发者，无需启动任何外部服务即可在进程内以最高性能生成报表：

```bash
dotnet add package Bangplanix.Core
dotnet add package Bangplanix.Rendering.SkiaSharp
dotnet add package Bangplanix.Connectors.Json
```

```csharp
using Bangplanix.Core;
using Bangplanix.Rendering.SkiaSharp;

// 1. 读取模板并绑定数据
var template = ReportTemplate.LoadFromJsonFile("templates/commercial-invoice-ubl.bpx");
var dataJson = File.ReadAllText("data/invoice.json");

// 2. 纯进程内亚毫秒级生成 PDF
var engine = new ReportEngine();
var renderResult = await engine.RenderAsync(template, dataJson, ExportFormat.Pdf);

// 3. 直接保存到文件或转为内存流
await File.WriteAllBytesAsync("output/invoice.pdf", renderResult.ToByteArray());
```

---

### 💻 模式 2: 独立原生 CLI 命令行 (Standalone Local CLI & Native AOT)
适合自动化脚本或 CI/CD 流水线，无需安装 Docker 或配置 .NET 运行时：

```bash
# 生成 PDF / Excel 报表
bangplanix render -t templates/commercial-invoice-ubl.bpx -d data/invoice.json -o output/invoice.pdf

# 验证模板语法合规性
bangplanix validate -t templates/commercial-invoice-ubl.bpx

# 直接发送原始字节到 POS 热敏或网络标签打印机
bangplanix print -t templates/receipt-80mm.bpx -d data/receipt.json --printer-ip 192.168.1.200 --printer-type escpos
```

---

### 🐳 模式 3: 独立 Docker 微服务 (Docker Microservice)
适合多语言异构团队，集中式集中渲染管理：

```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

---

### ⚡ 30 秒 Minimal API 极速实战 (ASP.NET Core)
只需在 `Program.cs` 中添加以下代码，即可快速暴露 PDF 渲染接口：

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/invoice", async (InvoiceDto dto) =>
{
    var template = ReportTemplate.LoadFromJsonFile("templates/commercial-invoice-ubl.bpx");
    var engine = new ReportEngine();
    var result = await engine.RenderAsync(template, JsonSerializer.Serialize(dto), ExportFormat.Pdf);
    return Results.File(result.ToByteArray(), "application/pdf", "invoice.pdf");
});

app.Run();
```

---

## 🎨 第 2 章: `.bpx` 模板架构与全球标准模板库 (Starter Templates Pack)

Bangplanix 采用现代化轻量级 **`.bpx` (JSON Schema)** 作为模板格式，解析性能达到微秒级别，无需垃圾回收压力 (Zero-GC)。

### 📦 开箱即用全球标准模板库 (Starter Templates Pack)

系统内置 5 套符合全球商业标准的经典报表模板：

1. **📄 Global Commercial Invoice (UBL 2.1 Standard)** (`templates/commercial-invoice-ubl.bpx`): 国际 B2B 贸易发票，含税务明细、多货币计算、条款与付款银行信息。
2. **📊 Executive Financial KPI Summary** (`templates/financial-summary.bpx`): 董事会财务损益、KPI 概览与汇总表格。
3. **💼 Corporate Employee Payslip** (`templates/corporate-payslip.bpx`): 企业月度工资单，含扣款项、税收与社保明细。
4. **🏷️ Global Shipping Logistics Label (4x6 inch)** (`templates/logistics-label-4x6.bpx`): 国际物流标准运单，含 GS1-128、Code 128 条形码与收发地址区。
5. **🧾 POS Retail Thermal Receipt (80mm ESC/POS)** (`templates/receipt-80mm.bpx`): 超市与餐饮收银小票，支持 58mm / 80mm 热敏纸与核销二维码。

### 基础 `.bpx` 模板示例:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "Global Commercial Invoice (UBL 2.1)",
    "author": "Enterprise Cloud Systems"
  },
  "pageSetup": {
    "paperKind": "A4",
    "orientation": "Portrait",
    "unit": "Mm",
    "margins": { "top": 10, "bottom": 10, "left": 10, "right": 10 }
  },
  "bands": {
    "pageHeader": {
      "height": 25,
      "elements": [
        {
          "type": "Text",
          "text": "COMMERCIAL INVOICE",
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
          "expression": "=\"Total: $\" + FormatNumber(Sum(Fields.Price), 2)",
          "x": 0, "y": 0, "width": 190, "height": 8,
          "style": { "fontSize": 10, "color": "#475569" }
        }
      ]
    }
  }
}
```

---

## 🤖 第 3 章: Bangplanix AI Suite 自然语言生成报表

内置 **Bangplanix AI Suite** 允许使用中文或英文直接生成或修改 `.bpx` 报表模板：

> 💡 **提示词示例:**
> *"生成一份现代风格的销售月报，包含公司名称、产品明细表格、按部门汇总的金额、以及底部的确认二维码与审计签名。"*

---

## 💾 第 4 章: 数据注入 (JSON Push & SQL 数据库)

### 4.1 直接通过 REST API 推送 JSON 数据 (推荐微服务架构)
```json
[
  { "ItemCode": "SKU-1001", "ItemName": "企业云服务器节点", "Price": 12000.00 },
  { "ItemCode": "SKU-1002", "ItemName": "Bangplanix Enterprise 授权", "Price": 35000.00 }
]
```

---

## 🖥️ 第 5 章: 前端 Web 组件嵌入 (React, Vue, Web Components)

### 5.1 嵌入报表预览器 `<bangplanix-viewer>`
```html
<script type="module" src="http://localhost:9545/viewer/bangplanix-viewer.js"></script>

<bangplanix-viewer 
  src="http://localhost:9545/api/v1/reports/render"
  theme="light"
  zoom="fit-width">
</bangplanix-viewer>
```

---

## 🔌 第 6 章: 多语言后端 SDK 与 3 种返回格式 (FilePath, Stream, Base64)

Bangplanix 提供 5 种官方强类型 Client SDKs (C# .NET, Node.js/TypeScript, Python, Go, 和 Java)，均原生支持 **3 种灵活的报表接收方式 (FilePath, Stream, Base64)**：

<details open>
<summary><b>🔷 1. C# / .NET SDK (.NET 8 / 9 / 10)</b></summary>

```bash
dotnet add package Bangplanix.Client
```
```csharp
using Bangplanix.Client;

var client = new BangplanixClient("http://localhost:9545");
var request = new RenderReportRequest
{
    TemplatePath = "templates/commercial-invoice-ubl.bpx",
    DataJson = JsonSerializer.Serialize(orders)
};

// 1. 直接保存到文件 (FilePath)
await client.RenderToFileAsync(request, "output/invoice.pdf");

// 2. 接收为内存流 (Stream)
var result = await client.RenderReportAsync(request);
using Stream stream = result.ToStream();

// 3. 接收为 Base64 字符串 (Base64)
string base64String = result.ToBase64();
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
const req = {
  templatePath: 'templates/commercial-invoice-ubl.bpx',
  data: [{ ItemName: 'High-Performance Cloud Server', Price: 35000 }]
};

// 1. 直接保存到文件 (FilePath)
await client.renderToFile(req, 'output/invoice.pdf');

// 2. 接收为 Readable Stream (Stream)
const stream = await client.renderToStream(req);

// 3. 接收为 Base64 字符串 (Base64)
const base64Str = await client.renderToBase64(req);
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
payload = {
    "template_path": "templates/commercial-invoice-ubl.bpx",
    "data": [{"ItemName": "Enterprise Cloud Node", "Price": 8500}]
}

# 1. 直接保存到文件 (FilePath)
client.render_to_file(**payload, output_path="output/invoice.pdf")

# 2. 接收为内存二进制流 (Stream)
stream = client.render_to_stream(**payload)

# 3. 接收为 Base64 字符串 (Base64)
base64_str = client.render_to_base64(**payload)
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
    req := &bangplanix.RenderRequest{
        TemplatePath: "templates/commercial-invoice-ubl.bpx",
        DataJson:     `[{"ItemName": "Mechanical Keyboard", "Price": 2500}]`,
    }

    // 1. 直接保存到文件 (FilePath)
    _ = client.RenderToFile(context.Background(), req, "output/invoice.pdf")

    // 2. 接收为内存 Reader 流 (Stream)
    reader, _ := client.RenderToReader(context.Background(), req)

    // 3. 接收为 Base64 字符串 (Base64)
    base64Str, _ := client.RenderToBase64(context.Background(), req)
}
```
</details>

<details>
<summary><b>☕ 5. Java SDK</b></summary>

```java
BangplanixClient client = new BangplanixClient("http://localhost:9545");
RenderRequest req = new RenderRequest("templates/commercial-invoice-ubl.bpx", jsonData);

// 1. 直接保存到文件 (FilePath)
client.renderToFile(req, "output/invoice.pdf");

// 2. 接收为内存流 (Stream)
RenderResponse res = client.renderReport(req);
InputStream stream = res.toInputStream();

// 3. 接收为 Base64 字符串 (Base64)
String base64 = res.toBase64();
```
</details>

<details>
<summary><b>🐘 6. PHP (Laravel / 原生 cURL)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/commercial-invoice-ubl.bpx',
    'format' => 'Pdf',
    'dataJson' => json_encode([['ItemName' => '技术支持费', 'Price' => 5000]])
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
<summary><b>🎯 7. Dart / Flutter (移动 POS / 平板出单)</b></summary>

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
      'templatePath': 'templates/commercial-invoice-ubl.bpx',
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
<summary><b>🦀 8. Rust (reqwest 异步)</b></summary>

```rust
use reqwest::Client;
use serde_json::json;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let client = Client::new();
    let res = client.post("http://localhost:9545/api/v1/reports/render")
        .json(&json!({
            "templatePath": "templates/commercial-invoice-ubl.bpx",
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
<summary><b>💎 9. Ruby (Net::HTTP)</b></summary>

```ruby
require 'net/http'
require 'uri'
require 'json'

uri = URI.parse("http://localhost:9545/api/v1/reports/render")
payload = {
  templatePath: "templates/commercial-invoice-ubl.bpx",
  format: "Pdf",
  dataJson: [{ ItemName: "SaaS Monthly Subscription", Price: 1200 }].to_json
}

response = Net::HTTP.post(uri, payload.to_json, { 'Content-Type' => 'application/json' })
File.open("invoice.pdf", "wb") { |f| f.write(response.body) }
```
</details>

<details>
<summary><b>⚡ 10. cURL / Shell 命令行示例</b></summary>

```bash
curl -X POST http://localhost:9545/api/v1/reports/render \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "templates/commercial-invoice-ubl.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"Cloud Compute Node\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## 🔄 第 7 章: 传统报表系统一键迁移 (Crystal, SSRS, Jasper, FastReport)

通过 CLI 工具快速将老旧系统报表转换为现代化的 `.bpx` 格式：

```bash
# 单文件迁移 (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# 批量目录迁移 (Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

### 支持的报表引擎格式:
| 传统系统 | 文件扩展名 |
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

## 🚢 第 8 章: 生产环境部署与许可证激活

### 8.1 环境变量配置 (Air-Gapped Offline 离线验证)
```bash
# 激活商业许可证 (离线纯本地异步验证)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# 启动服务端
dotnet run --project src/Bangplanix.Server
```
* 自动消除 PDF 报表中的 "Generated by Bangplanix Community" 水印。
* 解锁多核并发限制及企业级高级功能 (AI Suite, Report Bursting, PAdES 数字签名 *(生产环境 CA/TSA 与云端驱动在 [Roadmap v1.1.0](./ROADMAP.md) 中)*)。

### 8.2 Kubernetes Helm Chart 部署
```bash
helm upgrade --install bangplanix ./deploy/helm/bangplanix \
  --namespace reporting --create-namespace \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set licenseKey="eyJ..."
```

---

## 🛠️ 第 9 章: 全球字体排版与常见问题解答 (Typography & FAQ)

### 9.1 多语言排版与复杂文本塑形 (HarfBuzz Text Shaping)
* **HarfBuzzSharp 集成:** Bangplanix 深度集成 **HarfBuzzSharp**，原生支持中文、日文、韩文 (CJK) 及泰语上下标声调符号、阿拉伯语连笔、梵文等多语言排版，文字绝不出现裁切或错位。
* **自定义字体加载:** 将 `.ttf` 或 `.otf` 字体放置于 `/app/volumes/fonts` 目录，或通过 CLI 参数 `--font-dir` 指定。

### 9.2 常见问题解答 (FAQ)

#### Q: 在 Docker / Linux 下生成报表时出现方块乱码 (Tofu boxes) 怎么解决？
**A:** 将系统 TrueType 字体目录挂载到容器中：
```bash
docker run -v $(pwd)/volumes/fonts:/app/volumes/fonts ...
```
Bangplanix 启动时会自动扫描该目录并建立字体缓存，实现毫秒级快速匹配。

#### Q: 为什么 Excel (.xlsx) 导出速度比 PDF 更快？
**A:** Excel 导出基于 **MiniExcel** 的零内存分配流式引擎，直接生成 OpenXML ZIP 压缩包；而 PDF 需要进行高精度的矢量排版计算与字体塑形。

#### Q: 能否直接向 POS 小票打印机发送打印指令而无需生成 PDF？
**A:** 完全可以！使用 CLI `print` 命令并指定 `--printer-type escpos` 或 `--printer-type zpl`，即可通过 9100 端口或 CUPS/IPP 直接向网络热敏打印机发送原始字节流。

---

## 💬 社区与技术支持

* 🐞 **问题反馈 / Bug 提交:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💡 **社区交流:** [GitHub Discussions](https://github.com/thabot/bangplanix/discussions)
* 💼 **商业授权与 OEM 合作:** `thabot47@gmail.com`

