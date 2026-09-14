# 📖 Bangplanix — Guía de Inicio Rápido y Desarrollo (Getting Started Guide)

Bienvenido a la guía oficial de **Bangplanix Enterprise Reporting Engine (.NET 10 / C# 14 / Native AOT)**. Esta guía cubre desde la instalación inicial, diseño de plantillas, generación con IA, integración de SDKs hasta el despliegue en entornos de producción.

> 🌐 **Idiomas Disponibles / Available Languages:**
> * 🇬🇧 [English (GETTING-STARTED.md)](./GETTING-STARTED.md)
> * 🇹🇭 [ไทย (GETTING-STARTED.th.md)](./GETTING-STARTED.th.md)
> * 🇨🇳 [简体中文 (GETTING-STARTED.zh.md)](./GETTING-STARTED.zh.md)
> * 🇯🇵 [日本語 (GETTING-STARTED.ja.md)](./GETTING-STARTED.ja.md)
> * 🇪🇸 [Español (GETTING-STARTED.es.md)](./GETTING-STARTED.es.md)

---

## 📑 Tabla de Contenidos

1. [🚀 Capítulo 1: Inicio Rápido en 3 Minutos](#-capítulo-1-inicio-rápido-en-3-minutos)
2. [🎨 Capítulo 2: Estructura de Plantillas `.bpx` y Diseñador Visual](#-capítulo-2-estructura-de-plantillas-bpx-y-diseñador-visual)
3. [🤖 Capítulo 3: Bangplanix AI Suite para Generación por Lenguaje Natural](#-capítulo-3-bangplanix-ai-suite-para-generación-por-lenguaje-natural)
4. [💾 Capítulo 4: Inyección de Datos (JSON Push y Bases de Datos SQL)](#-capítulo-4-inyección-de-datos-json-push-y-bases-de-datos-sql)
5. [🖥️ Capítulo 5: Integración Frontend (React, Vue, Web Components)](#-capítulo-5-integración-frontend-react-vue-web-components)
6. [🔌 Capítulo 6: SDKs Backend y Conexión Genérica REST/gRPC](#-capítulo-6-sdks-backend-y-conexión-genérica-restgrpc)
7. [🔄 Capítulo 7: Migración desde Sistemas Heredados (Crystal, SSRS, Jasper, FastReport)](#-capítulo-7-migración-desde-sistemas-heredados-crystal-ssrs-jasper-fastreport)
8. [🚢 Capítulo 8: Despliegue en Producción y Activación de Licencias](#-capítulo-8-despliegue-en-producción-y-activación-de-licencias)

---

## 🚀 Capítulo 1: Inicio Rápido en 3 Minutos

### 1.1 Ejecución con Docker Compose (Recomendado)
```bash
docker compose up -d
```

Acceso directo a los servicios:
* **Punto de enlace de estado y API REST:** [`http://localhost:9545`](http://localhost:9545) *(Portal Web GUI en [Roadmap v1.1.0](./ROADMAP.md))*
* **Punto de enlace gRPC de Alta Velocidad:** `localhost:9546`

### 1.2 Ejecución directa con Docker CLI
```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

### 1.3 Diagnóstico del Sistema con System Doctor
```bash
dotnet run --project tools/Bangplanix.Cli -- doctor
```

---

## 🎨 Capítulo 2: Estructura de Plantillas `.bpx` y Diseñador Visual

Bangplanix utiliza el formato ligero **`.bpx` (JSON Schema)** para lograr ejecuciones en microsegundos con Zero-GC y aceleración por hardware SIMD.

### Ejemplo de Plantilla `.bpx`:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "Factura Comercial / Invoice",
    "author": "Sistemas Empresariales Globales"
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
          "text": "FACTURA COMERCIAL / INVOICE",
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
          "expression": "=\"Total a Pagar: $\" + FormatNumber(Sum(Fields.Price), 2)",
          "x": 0, "y": 0, "width": 190, "height": 8,
          "style": { "fontSize": 10, "color": "#475569" }
        }
      ]
    }
  }
}
```

---

## 🤖 Capítulo 3: Bangplanix AI Suite para Generación por Lenguaje Natural

Permite generar y editar plantillas `.bpx` a partir de instrucciones en lenguaje natural (español o inglés):

> 💡 **Ejemplo de Prompt:**
> *"Genera una factura de venta con encabezado corporativo, tabla detallada de productos con cantidad, precio unitario, cálculo de IVA del 16%, código QR de verificación fiscal y firma digital."*

---

## 💾 Capítulo 4: Inyección de Datos (JSON Push y Bases de Datos SQL)

### 4.1 Envío directo de datos JSON vía API REST
```json
[
  { "ItemCode": "SKU-001", "ItemName": "Servidor Cloud Enterprise", "Price": 15000.00 },
  { "ItemCode": "SKU-002", "ItemName": "Licencia Bangplanix Pro", "Price": 4900.00 }
]
```

---

## 🖥️ Capítulo 5: Integración Frontend (React, Vue, Web Components)

### 5.1 Visor de Informes `<bangplanix-viewer>`
```html
<script type="module" src="http://localhost:9545/viewer/bangplanix-viewer.js"></script>

<bangplanix-viewer 
  src="http://localhost:9545/api/v1/reports/render"
  theme="light"
  zoom="fit-width">
</bangplanix-viewer>
```

---

## 🔌 Capítulo 6: SDKs Backend y Conexión Genérica REST/gRPC

Seleccione su lenguaje o entorno de desarrollo:

<details open>
<summary><b>🔷 1. SDK para C# / .NET (.NET 8 / 9 / 10)</b></summary>

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
<summary><b>🟢 2. SDK para Node.js / TypeScript</b></summary>

```bash
npm install @bangplanix/client
```
```typescript
import { BangplanixClient } from '@bangplanix/client';
import * as fs from 'fs';

const client = new BangplanixClient({ baseUrl: 'http://localhost:9545' });
const pdfBuffer = await client.renderReport({
  templatePath: 'templates/invoice.bpx',
  data: [{ ItemName: 'Nodo de Cómputo', Price: 2500 }]
});
fs.writeFileSync('invoice.pdf', pdfBuffer);
```
</details>

<details>
<summary><b>🐍 3. SDK para Python</b></summary>

```bash
pip install bangplanix
```
```python
from bangplanix import BangplanixClient

client = BangplanixClient(base_url="http://localhost:9545")
pdf_bytes = client.render_to_file(
    template_path="templates/invoice.bpx",
    data=[{"ItemName": "Nodo de Almacenamiento", "Price": 4500}],
    output_path="invoice.pdf"
)
```
</details>

<details>
<summary><b>🔵 4. SDK para Go</b></summary>

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
        DataJson:     `[{"ItemName": "Módulo RAM", "Price": 650}]`,
    })
    os.WriteFile("invoice.pdf", pdf, 0644)
}
```
</details>

<details>
<summary><b>☕ 5. SDK para Java</b></summary>

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
    'dataJson' => json_encode([['ItemName' => 'Honorarios Técnicos', 'Price' => 5000]])
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
<summary><b>🎯 7. Dart / Flutter (Terminal POS Móvil)</b></summary>

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
        {'ItemName': 'Terminal POS Móvil', 'Price': 8900}
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
<summary><b>🦀 8. Rust (reqwest Asíncrono)</b></summary>

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
            "dataJson": "[{\"ItemName\":\"Cómputo de Alta Velocidad\",\"Price\":9900}]"
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
  dataJson: [{ ItemName: "Suscripción Mensual SaaS", Price: 1200 }].to_json
}

response = Net::HTTP.post(uri, payload.to_json, { 'Content-Type' => 'application/json' })
File.open("invoice.pdf", "wb") { |f| f.write(response.body) }
```
</details>

<details>
<summary><b>⚡ 10. cURL / Terminal / Script</b></summary>

```bash
curl -X POST http://localhost:9545/api/v1/reports/render \
  -H "Content-Type: application/json" \
  -d '{
    "templatePath": "templates/invoice.bpx",
    "format": "Pdf",
    "dataJson": "[{\"ItemName\":\"Servidor Cloud\",\"Price\":15000}]"
  }' \
  --output invoice.pdf
```
</details>

---

## 🔄 Capítulo 7: Migración desde Sistemas Heredados (Crystal, SSRS, Jasper, FastReport)

```bash
# Migración de archivo individual (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# Migración por lotes (Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

---

## 🚢 Capítulo 8: Despliegue en Producción y Activación de Licencias

### 8.1 Activación por Variables de Entorno
```bash
# Activación de licencia comercial (validación 100% offline)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# Iniciar servidor
dotnet run --project src/Bangplanix.Server
```

Para más detalles de precios y soporte empresarial, consulte [PRICING.md](./PRICING.md).
