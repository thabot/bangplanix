# 📖 Bangplanix スタートアップ＆開発者ガイド (Getting Started Guide)

**Bangplanix 高性能エンタープライズ帳票エンジン (.NET 10 / C# 14 / Native AOT)** へようこそ。本ガイドでは、環境構築、テンプレート設計、AI 連携、各種バックエンド SDK 連携から本番環境のデプロイまでの手順を解説します。

> 🌐 **利用可能な言語 / Available Languages:**
> * 🇬🇧 [English (GETTING-STARTED.md)](./GETTING-STARTED.md)
> * 🇹🇭 [ไทย (GETTING-STARTED.th.md)](./GETTING-STARTED.th.md)
> * 🇨🇳 [简体中文 (GETTING-STARTED.zh.md)](./GETTING-STARTED.zh.md)
> * 🇯🇵 [日本語 (GETTING-STARTED.ja.md)](./GETTING-STARTED.ja.md)
> * 🇪🇸 [Español (GETTING-STARTED.es.md)](./GETTING-STARTED.es.md)

---

## 📑 目次 (Table of Contents)

1. [🚀 第 1 章: 3 分クイックスタート＆セットアップ](#-第-1-章-3-分クイックスタートセットアップ)
2. [🎨 第 2 章: `.bpx` テンプレート構造とデザイナー](#-第-2-章-bpx-テンプレート構造とデザイナー)
3. [🤖 第 3 章: Bangplanix AI Suite による自然言語帳票生成](#-第-3-章-bangplanix-ai-suite-による自然言語帳票生成)
4. [💾 第 4 章: データ連携 (JSON Push & SQL データベース)](#-第-4-章-データ連携-json-push--sql-データベース)
5. [🖥️ 第 5 章: フロントエンド Web コンポーネント埋め込み](#-第-5-章-フロントエンド-web-コンポーネント埋め込み)
6. [🔌 第 6 章: バックエンド SDK および汎用 REST/gRPC 接続](#-第-6-章-バックエンド-sdk-および汎用-restgrpc-接続)
7. [🔄 第 7 章: 既存帳票システムからの移行 (Crystal, SSRS, Jasper, FastReport)](#-第-7-章-既存帳票システムからの移行-crystal-ssrs-jasper-fastreport)
8. [🚢 第 8 章: 本番環境デプロイとライセンス認証](#-第-8-章-本番環境デプロイとライセンス認証)

---

## 🚀 第 1 章: 3 分クイックスタート＆セットアップ

### 1.1 Docker Compose による起動 (推奨)
```bash
docker compose up -d
```

起動後、直ちに以下のエンドポイントにアクセス可能です：
* **REST API 状態エンドポイント:** [`http://localhost:9545`](http://localhost:9545) *(Web 管理ポータル GUI は [Roadmap v1.1.0](./ROADMAP.md) に掲載)*
* **超高速 gRPC エンドポイント:** `localhost:9546`

### 1.2 Docker CLI で直接起動
```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

### 1.3 システム状態診断 (System Doctor)
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```

---

## 🎨 第 2 章: `.bpx` テンプレート構造とデザイナー

Bangplanix は軽量な **`.bpx` (JSON Schema)** を採用しており、ゼロアロケーション (Zero-GC) でマイクロ秒単位の高速処理を実現します。

### `.bpx` テンプレート基本例:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "請求書 / Invoice",
    "author": "株式会社エンタープライズ・クラウド"
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
          "text": "御請求書 / INVOICE",
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
          "expression": "=\"合計金額: ¥\" + FormatNumber(Sum(Fields.Price), 0)",
          "x": 0, "y": 0, "width": 190, "height": 8,
          "style": { "fontSize": 10, "color": "#475569" }
        }
      ]
    }
  }
}
```

---

## 🤖 第 3 章: Bangplanix AI Suite による自然言語帳票生成

**Bangplanix AI Suite** を使用することで、日本語または英語のプロンプトから帳票定義を自動生成できます。

> 💡 **プロンプト例:**
> *"自社ロゴ入りの納品書を作成してください。品目明細表、数量、単価、小計、消費税 10% 計算、合計金額、および適格請求書発行事業者番号を含めてください。"*

---

## 💾 第 4 章: データ連携 (JSON Push & SQL データベース)

### 4.1 REST API 経由でのダイレクト JSON 送信 (マイクロサービス構成)
```json
[
  { "ItemCode": "SKU-001", "ItemName": "クラウドサーバー利用料", "Price": 15000 },
  { "ItemCode": "SKU-002", "ItemName": "Bangplanix Enterprise ライセンス", "Price": 49000 }
]
```

---

## 🖥️ 第 5 章: フロントエンド Web コンポーネント埋め込み

### 5.1 帳票ビューアー `<bangplanix-viewer>`
```html
<script type="module" src="http://localhost:9545/viewer/bangplanix-viewer.js"></script>

<bangplanix-viewer 
  src="http://localhost:9545/api/v1/reports/render"
  theme="light"
  zoom="fit-width">
</bangplanix-viewer>
```

---

## 🔌 第 6 章: バックエンド SDK および汎用 REST/gRPC 接続

利用する開発言語またはフレームワークを選択してください：

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
  data: [{ ItemName: 'クラウドノード', Price: 2500 }]
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
    data=[{"ItemName": "ストレージノード", "Price": 4500}],
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
        DataJson:     `[{"ItemName": "RAM モジュール", "Price": 6500}]`,
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
<summary><b>🐘 6. PHP (Laravel / cURL)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/invoice.bpx',
    'format' => 'Pdf',
    'dataJson' => json_encode([['ItemName' => 'サポート費用', 'Price' => 5000]])
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
<summary><b>🎯 7. Dart / Flutter (モバイル POS / レシート印刷)</b></summary>

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
        {'ItemName': 'スマートPOSレジ端末', 'Price': 8900}
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
<summary><b>🦀 8. Rust (reqwest 非同期)</b></summary>

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
            "dataJson": "[{\"ItemName\":\"高速コンピュート\",\"Price\":9900}]"
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
  dataJson: [{ ItemName: "SaaS 月額サブスクリプション", Price: 1200 }].to_json
}

response = Net::HTTP.post(uri, payload.to_json, { 'Content-Type' => 'application/json' })
File.open("invoice.pdf", "wb") { |f| f.write(response.body) }
```
</details>

<details>
<summary><b>⚡ 10. cURL / Shell スクリプト例</b></summary>

```bash
curl -X POST http://localhost:9545/api/v1/reports/render \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "templates/invoice.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"クラウドサーバー\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## 🔄 第 7 章: 既存帳票システムからの移行 (Crystal, SSRS, Jasper, FastReport)

```bash
# 単一ファイル移行 (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# ディレクトリ一括移行 (Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

---

## 🚢 第 8 章: 本番環境デプロイとライセンス認証

### 8.1 環境変数によるライセンス適用
```bash
# 商用ライセンスの有効化 (完全オフライン署名検証)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# サーバー起動
dotnet run --project src/Bangplanix.Server
```

ライセンス体系の詳細は [PRICING.md](./PRICING.md) をご確認ください。
