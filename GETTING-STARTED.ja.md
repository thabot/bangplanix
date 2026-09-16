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

1. [🚀 第 1 章: アーキテクチャ選定と 3 つの実行モード (Execution Modes)](#-第-1-章-アーキテクチャ選定と-3-つの実行モード-execution-modes)
2. [🎨 第 2 章: `.bpx` テンプレート構造と標準テンプレート集 (Starter Templates Pack)](#-第-2-章-bpx-テンプレート構造と標準テンプレート集-starter-templates-pack)
3. [🤖 第 3 章: Bangplanix AI Suite による自然言語帳票生成](#-第-3-章-bangplanix-ai-suite-による自然言語帳票生成)
4. [💾 第 4 章: データ連携 (JSON Push & SQL データベース)](#-第-4-章-データ連携-json-push--sql-データベース)
5. [🖥️ 第 5 章: フロントエンド Web コンポーネント埋め込み](#-第-5-章-フロントエンド-web-コンポーネント埋め込み)
6. [🔌 第 6 章: バックエンド SDK および 3 つの出力形式 (FilePath, Stream, Base64)](#-第-6-章-バックエンド-sdk-および-3-つの出力形式-filepath-stream-base64)
7. [🔄 第 7 章: 既存帳票システムからの移行 (Crystal, SSRS, Jasper, FastReport)](#-第-7-章-既存帳票システムからの移行-crystal-ssrs-jasper-fastreport)
8. [🚢 第 8 章: 本番環境デプロイとライセンス認証](#-第-8-章-本番環境デプロイとライセンス認証)
9. [🛠️ 第 9 章: グローバルタイポグラフィとトラブルシューティング FAQ (Typography & FAQ)](#-第-9-章-グローバルタイポグラフィとトラブルシューティング-faq-typography--faq)

---

## 🚀 第 1 章: アーキテクチャ選定と 3 つの実行モード (Execution Modes)

Bangplanix は **3 つの柔軟な実行・デプロイモード** を提供し、モノリスからマイクロサービスまであらゆるアーキテクチャに対応します：

### 📊 実行モード選定マトリクス (Decision Matrix)

| 項目 | モード 1: C# プロセス内ライブラリ | モード 2: 単体 CLI / Native AOT | モード 3: Docker マイクロサービス |
| :--- | :--- | :--- | :--- |
| **主な対象ユーザー** | .NET / C# 開発者 (ネイティブ プロセス内呼び出し) | CI/CD, 自動化スクリプト, 運用バッチ | 多言語チーム (Node, Python, Go, Java...) |
| **外部依存** | NuGet パッケージのみ (外部依存ゼロ) | 単一バイナリのみ (Docker 不要) | Docker / Kubernetes |
| **生成レイテンシ** | **サブミリ秒 (< 1ms)** | 超高速 (~10ms) | ネットワーク I/O (~5-15ms) |
| **多言語対応** | C# / F# / VB.NET | CLI コマンドライン / Shell | 公式多言語 SDK & REST / gRPC |
| **適用シーン** | デスクトップ (WPF/MAUI), 高速 ASP.NET Core | バッチパイプライン, GitHub Actions | エンタープライズ・クラウドネイティブ |

---

### 📦 モード 1: プロセス内 C# 埋め込みライブラリ (In-Process C# Library)
.NET アプリケーション内に直接組み込み、外部プロセス不要で最高速度の帳票生成を実現します：

```bash
dotnet add package Bangplanix.Core
dotnet add package Bangplanix.Rendering.SkiaSharp
dotnet add package Bangplanix.Connectors.Json
```

```csharp
using Bangplanix.Core;
using Bangplanix.Rendering.SkiaSharp;

// 1. テンプレート読み込みとデータバインド
var template = ReportTemplate.LoadFromJsonFile("templates/commercial-invoice-ubl.bpx");
var dataJson = File.ReadAllText("data/invoice.json");

// 2. プロセス内での超高速サブミリ秒 PDF 生成
var engine = new ReportEngine();
var renderResult = await engine.RenderAsync(template, dataJson, ExportFormat.Pdf);

// 3. ファイル出力またはメモリ ストリーム変換
await File.WriteAllBytesAsync("output/invoice.pdf", renderResult.ToByteArray());
```

---

### 💻 モード 2: スタンドアロン ネイティブ CLI (Standalone Local CLI & Native AOT)
Docker や .NET ランタイムをインストールすることなく、単体バイナリで実行可能です：

```bash
# PDF / Excel 帳票生成
bangplanix render -t templates/commercial-invoice-ubl.bpx -d data/invoice.json -o output/invoice.pdf

# テンプレートの構文検証
bangplanix validate -t templates/commercial-invoice-ubl.bpx

# POS レシートやバーコードプリンターへの直接 Raw バイト印刷
bangplanix print -t templates/receipt-80mm.bpx -d data/receipt.json --printer-ip 192.168.1.200 --printer-type escpos
```

---

### 🐳 モード 3: 独立 Docker マイクロサービス (Docker Microservice)
多言語チーム向けの集中レンダリングサーバー構成：

```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

---

### ⚡ 30 秒 Minimal API クイックスタート (ASP.NET Core)
`Program.cs` に数行追加するだけで PDF 帳票 API を公開できます：

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

## 🎨 第 2 章: `.bpx` テンプレート構造と標準テンプレート集 (Starter Templates Pack)

Bangplanix は軽量な **`.bpx` (JSON Schema)** を採用しており、ゼロアロケーション (Zero-GC) でマイクロ秒単位の高速処理を実現します。

### 📦 標準テンプレート パック (Starter Templates Pack)

グローバル標準に準拠した 5 種類のテンプレートが同梱されています：

1. **📄 Global Commercial Invoice (UBL 2.1 Standard)** (`templates/commercial-invoice-ubl.bpx`): 国際 B2B 商業請求書（税金計算、複数通貨、支払条件対応）。
2. **📊 Executive Financial KPI Summary** (`templates/financial-summary.bpx`): 役員会向け財務サマリー、KPI および損益計算書。
3. **💼 Corporate Employee Payslip** (`templates/corporate-payslip.bpx`): 企業給与明細書（控除項目、税金、社会保険料対応）。
4. **🏷️ Global Shipping Logistics Label (4x6 inch)** (`templates/logistics-label-4x6.bpx`): 国際物流向け出荷ラベル（GS1-128 / Code 128 バーコード対応）。
5. **🧾 POS Retail Thermal Receipt (80mm ESC/POS)** (`templates/receipt-80mm.bpx`): 小売・飲食向け 80mm / 58mm サーマルレシート（検証用 QR コード対応）。

### `.bpx` テンプレート基本例:
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

## 🔌 第 6 章: バックエンド SDK および 3 つの出力形式 (FilePath, Stream, Base64)

Bangplanix は 5 つの公式型付け SDK (C# .NET, Node.js/TypeScript, Python, Go, Java) を提供し、すべての言語で **3 つの柔軟な出力形式 (FilePath, Stream, Base64)** をサポートします：

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

// 1. ファイルへ直接保存 (FilePath)
await client.RenderToFileAsync(request, "output/invoice.pdf");

// 2. メモリ ストリームとして取得 (Stream)
var result = await client.RenderReportAsync(request);
using Stream stream = result.ToStream();

// 3. Base64 文字列として取得 (Base64)
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

// 1. ファイルへ直接保存 (FilePath)
await client.renderToFile(req, 'output/invoice.pdf');

// 2. Readable Stream として取得 (Stream)
const stream = await client.renderToStream(req);

// 3. Base64 文字列として取得 (Base64)
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

# 1. ファイルへ直接保存 (FilePath)
client.render_to_file(**payload, output_path="output/invoice.pdf")

# 2. メモリ バイナリ ストリームとして取得 (Stream)
stream = client.render_to_stream(**payload)

# 3. Base64 文字列として取得 (Base64)
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

    // 1. ファイルへ直接保存 (FilePath)
    _ = client.RenderToFile(context.Background(), req, "output/invoice.pdf")

    // 2. メモリ Reader ストリームとして取得 (Stream)
    reader, _ := client.RenderToReader(context.Background(), req)

    // 3. Base64 文字列として取得 (Base64)
    base64Str, _ := client.RenderToBase64(context.Background(), req)
}
```
</details>

<details>
<summary><b>☕ 5. Java SDK</b></summary>

```java
BangplanixClient client = new BangplanixClient("http://localhost:9545");
RenderRequest req = new RenderRequest("templates/commercial-invoice-ubl.bpx", jsonData);

// 1. ファイルへ直接保存 (FilePath)
client.renderToFile(req, "output/invoice.pdf");

// 2. メモリ ストリームとして取得 (Stream)
RenderResponse res = client.renderReport(req);
InputStream stream = res.toInputStream();

// 3. Base64 文字列として取得 (Base64)
String base64 = res.toBase64();
```
</details>

<details>
<summary><b>🐘 6. PHP (Laravel / cURL)</b></summary>

```php
<?php
$payload = [
    'templatePath' => 'templates/commercial-invoice-ubl.bpx',
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
<summary><b>🦀 8. Rust (reqwest 非同期)</b></summary>

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
<summary><b>⚡ 10. cURL / Shell スクリプト例</b></summary>

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

## 🔄 第 7 章: 既存帳票システムからの移行 (Crystal, SSRS, Jasper, FastReport)

CLI 移行ツールを使用して、既存の帳票定義をモダンな `.bpx` 形式に素早く移行できます：

```bash
# 単一ファイル移行 (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# ディレクトリ一括移行 (Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

### サポート対象の帳票エンジン:
| 移行元システム | 拡張子 |
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

## 🚢 第 8 章: 本番環境デプロイとライセンス認証

### 8.1 環境変数によるライセンス適用 (Air-Gapped 完全オフライン)
```bash
# 商用ライセンスの有効化 (完全オフライン署名検証)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# サーバー起動
dotnet run --project src/Bangplanix.Server
```
* PDF の "Generated by Bangplanix Community" 透かしを自動解除。
* CPU コア数制限の解除およびエンタープライズ機能（AI Suite, Report Bursting, PAdES 電子署名 *(本番 CA/TSA およびクラウド ドライバーは [Roadmap v1.1.0](./ROADMAP.md) に掲載)*）を有効化。

### 8.2 Kubernetes Helm Chart デプロイ
```bash
helm upgrade --install bangplanix ./deploy/helm/bangplanix \
  --namespace reporting --create-namespace \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set licenseKey="eyJ..."
```

---

## 🛠️ 第 9 章: グローバルタイポグラフィとトラブルシューティング FAQ (Typography & FAQ)

### 9.1 多言語タイポグラフィと複雑な文字シェーピング (HarfBuzz Text Shaping)
* **HarfBuzzSharp 統合:** Bangplanix は **HarfBuzzSharp** を標準統合しており、日本語・中国語・韓国語（CJK）のほか、タイ語の声調記号やアラビア語の連筆、サンスクリット語などを欠落なく高精度に描画します。
* **カスタムフォントの配置:** `.ttf` または `.otf` フォントファイルを `/app/volumes/fonts` に配置するか、CLI オプション `--font-dir` で指定します。

### 9.2 よくある質問 (FAQ)

#### Q: Docker / Linux 環境で文字が豆腐（□）になる場合の対処法は？
**A:** ホスト OS の TrueType フォントディレクトリをボリュームマウントしてください：
```bash
docker run -v $(pwd)/volumes/fonts:/app/volumes/fonts ...
```
Bangplanix は起動時にフォントを自動スキャンしてキャッシュを構築します。

#### Q: なぜ Excel (.xlsx) 出力は PDF より高速なのですか？
**A:** Excel 出力は **MiniExcel** のゼロアロケーション・ストリーミング処理により、OpenXML ZIP をダイレクトに生成するためです。一方、PDF は高精度のベクターグラフィックスおよび文字シェーピング演算を行います。

#### Q: PDF を作成せずに POS レシートプリンターへ直接印刷できますか？
**A:** はい、可能です！ CLI の `print` コマンドで `--printer-type escpos` または `--printer-type zpl` を指定すれば、ポート 9100 または CUPS/IPP 経由で生のバイトストリームを直接プリンターへ送信できます。

---

## 💬 コミュニティ＆サポート

* 🐞 **問題報告 / バグ報告:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💡 **ディスカッション:** [GitHub Discussions](https://github.com/thabot/bangplanix/discussions)
* 💼 **商用ライセンス・OEM のお問い合わせ:** `thabot47@gmail.com`

