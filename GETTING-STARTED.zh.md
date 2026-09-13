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

1. [🚀 第 1 章: 3 分钟快速启动与运行](#-第-1-章-3-分钟快速启动与运行)
2. [🎨 第 2 章: `.bpx` 模板架构与设计器](#-第-2-章-bpx-模板架构与设计器)
3. [🤖 第 3 章: Bangplanix AI Suite 自然语言生成报表](#-第-3-章-bangplanix-ai-suite-自然语言生成报表)
4. [💾 第 4 章: 数据注入 (JSON Push & SQL 数据库)](#-第-4-章-数据注入-json-push--sql-数据库)
5. [🖥️ 第 5 章: 前端 Web 组件嵌入 (React, Vue, Web Components)](#-第-5-章-前端-web-组件嵌入-react-vue-web-components)
6. [🔌 第 6 章: 多语言后端 SDK 与通用 REST/gRPC 接入](#-第-6-章-多语言后端-sdk-与通用-restgrpc-接入)
7. [🔄 第 7 章: 传统报表系统一键迁移 (Crystal, SSRS, Jasper, FastReport)](#-第-7-章-传统报表系统一键迁移-crystal-ssrs-jasper-fastreport)
8. [🚢 第 8 章: 生产环境部署与许可证激活](#-第-8-章-生产环境部署与许可证激活)

---

## 🚀 第 1 章: 3 分钟快速启动与运行

Bangplanix 提供开箱即用的 Docker 容器、CLI 诊断工具以及 Web 管理门户。

### 1.1 使用 Docker Compose 运行 (推荐)
```bash
docker compose up -d
```

启动完成后可立即访问以下端点：
* **Web 管理控制台 & REST API:** [`http://localhost:9545`](http://localhost:9545)
* **高性能 gRPC 端点:** `localhost:9546`

### 1.2 使用 Docker CLI 直接启动
```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

### 1.3 运行系统诊断 (System Doctor)
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```

---

## 🎨 第 2 章: `.bpx` 模板架构与设计器

Bangplanix 采用现代化轻量级 **`.bpx` (JSON Schema)** 作为模板格式，解析性能达到微秒级别，无需垃圾回收压力 (Zero-GC)。

### 基础 `.bpx` 模板示例:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "商业发票 / Commercial Invoice",
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
          "text": "企业销售发票 / Commercial Invoice",
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
          "expression": "=\"总计: ¥\" + FormatNumber(Sum(Fields.Price), 2)",
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

## 🔌 第 6 章: 多语言后端 SDK 与通用 REST/gRPC 接入

选择您使用的编程语言或技术栈：

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
  data: [{ ItemName: '服务器节点', Price: 2500 }]
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
    data=[{"ItemName": "存储节点", "Price": 4500}],
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
        DataJson:     `[{"ItemName": "内存条", "Price": 650}]`,
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
<summary><b>🐘 6. PHP (Laravel / 原生 cURL)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/invoice.bpx',
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
      'templatePath': 'templates/invoice.bpx',
      'format': 'Pdf',
      'dataJson': jsonEncode([
        {'ItemName': '智能收银机 POS', 'Price': 8900}
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
            "templatePath": "templates/invoice.bpx",
            "format": "Pdf",
            "dataJson": "[{\"ItemName\":\"高性能计算集群\",\"Price\":9900}]"
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
  templatePath: "templates/invoice.bpx",
  format: "Pdf",
  dataJson: [{ ItemName: "SaaS 月度订阅费", Price: 1200 }].to_json
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
    "templatePath": "templates/invoice.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"云服务器\",\"Price\":15000}]"
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

---

## 🚢 第 8 章: 生产环境部署与许可证激活

### 8.1 环境变量配置
```bash
# 激活商业许可证 (离线纯本地异步验证)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# 启动服务端
dotnet run --project src/Bangplanix.Server
```

更多定价与商业支持信息，请参阅 [PRICING.md](./PRICING.md)。
