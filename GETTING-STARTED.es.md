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

1. [🚀 Capítulo 1: Arquitectura y 3 Modos de Ejecución (Execution Modes)](#-capítulo-1-arquitectura-y-3-modos-de-ejecución-execution-modes)
2. [🎨 Capítulo 2: Estructura `.bpx` y Paquete de Plantillas Estándar (Starter Templates Pack)](#-capítulo-2-estructura-bpx-y-paquete-de-plantillas-estándar-starter-templates-pack)
3. [🤖 Capítulo 3: Bangplanix AI Suite para Generación por Lenguaje Natural](#-capítulo-3-bangplanix-ai-suite-para-generación-por-lenguaje-natural)
4. [💾 Capítulo 4: Inyección de Datos (JSON Push y Bases de Datos SQL)](#-capítulo-4-inyección-de-datos-json-push-y-bases-de-datos-sql)
5. [🖥️ Capítulo 5: Integración Frontend (React, Vue, Web Components)](#-capítulo-5-integración-frontend-react-vue-web-components)
6. [🔌 Capítulo 6: SDKs Backend y 3 Formatos de Retorno (FilePath, Stream, Base64)](#-capítulo-6-sdks-backend-y-3-formatos-de-retorno-filepath-stream-base64)
7. [🔄 Capítulo 7: Migración desde Sistemas Heredados (Crystal, SSRS, Jasper, FastReport)](#-capítulo-7-migración-desde-sistemas-heredados-crystal-ssrs-jasper-fastreport)
8. [🚢 Capítulo 8: Despliegue en Producción y Activación de Licencias](#-capítulo-8-despliegue-en-producción-y-activación-de-licencias)
9. [🛠️ Capítulo 9: Tipografía Global y Preguntas Frecuentes (Typography & FAQ)](#-capítulo-9-tipografía-global-y-preguntas-frecuentes-typography--faq)

---

## 🚀 Capítulo 1: Arquitectura y 3 Modos de Ejecución (Execution Modes)

Bangplanix ofrece **3 modos de ejecución y despliegue altamente flexibles**, adaptándose a cualquier arquitectura tecnológica:

### 📊 Matriz de Decisión de Modos de Ejecución (Decision Matrix)

| Criterio | Modo 1: Biblioteca C# Embebida | Modo 2: CLI Nativo AOT Independiente | Modo 3: Microservicio Docker |
| :--- | :--- | :--- | :--- |
| **Audiencia Principal** | Desarrolladores .NET / C# (estilo QuestPDF) | CI/CD, Scripts de Automatización, DevOps | Equipos Políglotas (Node, Python, Go, Java...) |
| **Dependencias Externas** | Solo paquetes NuGet (Zero dependencias) | Binario único standalone (Zero Docker) | Docker / Kubernetes |
| **Latencia de Generación**| **Sub-milisegundo (< 1ms)** | Ultrarrápido (~10ms) | Latencia de Red I/O (~5-15ms) |
| **Soporte Políglota** | C# / F# / VB.NET | Línea de Comandos CLI / Shell | SDKs Oficiales y REST / gRPC |
| **Casos de Uso Ideales** | Apps de escritorio (WPF/MAUI), ASP.NET Core | Pipelines por lotes, GitHub Actions | Microservicios y Cloud-Native Empresarial |

---

### 📦 Modo 1: Biblioteca C# Embebida en Proceso (In-Process C# Library)
Ideal para desarrolladores .NET; genera reportes dentro del mismo proceso sin servicios externos:

```bash
dotnet add package Bangplanix.Core
dotnet add package Bangplanix.Rendering.SkiaSharp
dotnet add package Bangplanix.Connectors.Json
```

```csharp
using Bangplanix.Core;
using Bangplanix.Rendering.SkiaSharp;

// 1. Cargar plantilla y datos
var template = ReportTemplate.LoadFromJsonFile("templates/commercial-invoice-ubl.bpx");
var dataJson = File.ReadAllText("data/invoice.json");

// 2. Generación PDF sub-milisegundo en memoria
var engine = new ReportEngine();
var renderResult = await engine.RenderAsync(template, dataJson, ExportFormat.Pdf);

// 3. Guardar archivo directo o convertir a Stream
await File.WriteAllBytesAsync("output/invoice.pdf", renderResult.ToByteArray());
```

---

### 💻 Modo 2: CLI Nativo Autónomo (Standalone Local CLI & Native AOT)
Ideal para pipelines de CI/CD y scripts sin requerir Docker ni runtime de .NET:

```bash
# Generar reporte PDF o Excel
bangplanix render -t templates/commercial-invoice-ubl.bpx -d data/invoice.json -o output/invoice.pdf

# Validar sintaxis del esquema de plantilla
bangplanix validate -t templates/commercial-invoice-ubl.bpx

# Enviar bytes directos a impresoras térmicas POS o de etiquetas
bangplanix print -t templates/receipt-80mm.bpx -d data/receipt.json --printer-ip 192.168.1.200 --printer-type escpos
```

---

### 🐳 Modo 3: Microservicio Docker (Docker Microservice)
Ideal para entornos corporativos con múltiples lenguajes de programación:

```bash
docker run -d \
  --name bangplanix-server \
  -p 9545:9545 -p 9546:9546 \
  -v $(pwd)/volumes/templates:/app/volumes/templates \
  -v $(pwd)/volumes/fonts:/app/volumes/fonts \
  ghcr.io/thabot/bangplanix:latest
```

---

### ⚡ Quickstart Minimal API en 30 Segundos (ASP.NET Core)
Agregue este endpoint a su `Program.cs` para servir reportes PDF instantáneamente:

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

## 🎨 Capítulo 2: Estructura `.bpx` y Paquete de Plantillas Estándar (Starter Templates Pack)

Bangplanix utiliza el formato ligero **`.bpx` (JSON Schema)** para lograr ejecuciones en microsegundos con Zero-GC y aceleración por hardware SIMD.

### 📦 Paquete de Plantillas Estándar Globales (Starter Templates Pack)

El repositorio incluye 5 plantillas comerciales de estándar internacional:

1. **📄 Global Commercial Invoice (UBL 2.1 Standard)** (`templates/commercial-invoice-ubl.bpx`): Factura comercial internacional B2B con desglose tributario, multimoneda y términos de pago.
2. **📊 Executive Financial KPI Summary** (`templates/financial-summary.bpx`): Resumen ejecutivo financiero con indicadores KPI y tablas de pérdidas/ganancias.
3. **💼 Corporate Employee Payslip** (`templates/corporate-payslip.bpx`): Recibo de nómina corporativo con deducciones, retenciones y aportes patronales.
4. **🏷️ Global Shipping Logistics Label (4x6 inch)** (`templates/logistics-label-4x6.bpx`): Etiqueta logística estándar con códigos de barras GS1-128 y Code 128.
5. **🧾 POS Retail Thermal Receipt (80mm ESC/POS)** (`templates/receipt-80mm.bpx`): Ticket de venta para comercios y restaurantes con soporte para papel de 58mm/80mm y QR de validación fiscal.

### Ejemplo de Plantilla `.bpx`:
```json
{
  "version": "1.0",
  "metadata": {
    "title": "Global Commercial Invoice (UBL 2.1)",
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

## 🔌 Capítulo 6: SDKs Backend y 3 Formatos de Retorno (FilePath, Stream, Base64)

Bangplanix ofrece 5 SDKs oficiales fuertemente tipados (C# .NET, Node.js/TypeScript, Python, Go, Java), compatibles nativamente con **3 formatos de salida (FilePath, Stream, Base64)**:

<details open>
<summary><b>🔷 1. SDK para C# / .NET (.NET 8 / 9 / 10)</b></summary>

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

// 1. Guardar archivo directo en disco (FilePath)
await client.RenderToFileAsync(request, "output/invoice.pdf");

// 2. Obtener como flujo en memoria (Stream)
var result = await client.RenderReportAsync(request);
using Stream stream = result.ToStream();

// 3. Obtener como cadena Base64 (Base64)
string base64String = result.ToBase64();
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
const req = {
  templatePath: 'templates/commercial-invoice-ubl.bpx',
  data: [{ ItemName: 'High-Performance Cloud Server', Price: 35000 }]
};

// 1. Guardar archivo directo en disco (FilePath)
await client.renderToFile(req, 'output/invoice.pdf');

// 2. Obtener como Readable Stream (Stream)
const stream = await client.renderToStream(req);

// 3. Obtener como cadena Base64 (Base64)
const base64Str = await client.renderToBase64(req);
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
payload = {
    "template_path": "templates/commercial-invoice-ubl.bpx",
    "data": [{"ItemName": "Enterprise Cloud Node", "Price": 8500}]
}

# 1. Guardar archivo directo en disco (FilePath)
client.render_to_file(**payload, output_path="output/invoice.pdf")

# 2. Obtener como flujo binario en memoria (Stream)
stream = client.render_to_stream(**payload)

# 3. Obtener como cadena Base64 (Base64)
base64_str = client.render_to_base64(**payload)
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
    req := &bangplanix.RenderRequest{
        TemplatePath: "templates/commercial-invoice-ubl.bpx",
        DataJson:     `[{"ItemName": "Mechanical Keyboard", "Price": 2500}]`,
    }

    // 1. Guardar archivo directo en disco (FilePath)
    _ = client.RenderToFile(context.Background(), req, "output/invoice.pdf")

    // 2. Obtener como flujo Reader en memoria (Stream)
    reader, _ := client.RenderToReader(context.Background(), req)

    // 3. Obtener como cadena Base64 (Base64)
    base64Str, _ := client.RenderToBase64(context.Background(), req)
}
```
</details>

<details>
<summary><b>☕ 5. SDK para Java</b></summary>

```java
BangplanixClient client = new BangplanixClient("http://localhost:9545");
RenderRequest req = new RenderRequest("templates/commercial-invoice-ubl.bpx", jsonData);

// 1. Guardar archivo directo en disco (FilePath)
client.renderToFile(req, "output/invoice.pdf");

// 2. Obtener como flujo InputStream en memoria (Stream)
RenderResponse res = client.renderReport(req);
InputStream stream = res.toInputStream();

// 3. Obtener como cadena Base64 (Base64)
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
<summary><b>🦀 8. Rust (reqwest Asíncrono)</b></summary>

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
<summary><b>⚡ 10. cURL / Terminal / Script</b></summary>

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

## 🔄 Capítulo 7: Migración desde Sistemas Heredados (Crystal, SSRS, Jasper, FastReport)

Utilice la herramienta CLI integrada para migrar plantillas heredadas al formato `.bpx`:

```bash
# Migración de archivo individual (SSRS RDL -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./legacy/Invoice.rdl -o ./templates/Invoice.bpx

# Migración por lotes (Crystal Reports XML -> BPX)
dotnet run --project tools/Bangplanix.Cli -- migrate -i ./crystal_reports/ -o ./bpx_templates/
```

### Motores de Reportes Compatibles:
| Sistema Heredado | Extensión |
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

## 🚢 Capítulo 8: Despliegue en Producción y Activación de Licencias

### 8.1 Activación por Variables de Entorno (Air-Gapped Offline)
```bash
# Activación de licencia comercial (validación 100% offline)
export LICENSE_KEY="eyJsaWNlbnNlSWQiOiJMSUMtMTAwMSIs...<payload>.<signature>"

# Iniciar servidor
dotnet run --project src/Bangplanix.Server
```
* Elimina automáticamente la marca de agua "Generated by Bangplanix Community" en los PDFs.
* Desbloquea la ejecución sin límite de núcleos de CPU y activa características empresariales (AI Suite, Report Bursting, Firmas Digitales PAdES *(CA/TSA de producción y controladores en la nube en [Roadmap v1.1.0](./ROADMAP.md))*).

### 8.2 Despliegue con Kubernetes Helm Chart
```bash
helm upgrade --install bangplanix ./deploy/helm/bangplanix \
  --namespace reporting --create-namespace \
  --set replicaCount=3 \
  --set autoscaling.enabled=true \
  --set licenseKey="eyJ..."
```

---

## 🛠️ Capítulo 9: Tipografía Global y Preguntas Frecuentes (Typography & FAQ)

### 9.1 Tipografía Multilingüe y Modelado de Texto Complejo (HarfBuzz Text Shaping)
* **Integración con HarfBuzzSharp:** Bangplanix incluye **HarfBuzzSharp** para soportar el renderizado de idiomas asiáticos (CJK), tonos y vocales flotantes del tailandés, ligaduras árabes de derecha a izquierda y devanagari sin recortes de glifos.
* **Carga de Fuentes Personalizadas:** Coloque fuentes `.ttf` o `.otf` en `/app/volumes/fonts` o pase la ruta en el CLI mediante `--font-dir`.

### 9.2 Preguntas Frecuentes (FAQ)

#### P: ¿Cómo solucionar cuadros vacíos o caracteres faltantes (Tofu boxes) en Docker/Linux?
**R:** Monte el directorio de fuentes TrueType en el contenedor con el flag de volumen:
```bash
docker run -v $(pwd)/volumes/fonts:/app/volumes/fonts ...
```
Bangplanix indexa el directorio al iniciar y genera una caché en memoria para búsquedas instantáneas.

#### P: ¿Por qué la exportación a Excel (.xlsx) es más rápida que a PDF?
**R:** Excel utiliza el motor **MiniExcel** con zero-allocation que genera directamente el paquete ZIP OpenXML con mínimo consumo de RAM, mientras que el PDF ejecuta operaciones vectoriales de alta precisión y modelado tipográfico.

#### P: ¿Puedo imprimir directamente en impresoras de tickets POS sin generar un PDF?
**R:** ¡Sí! Utilice el comando CLI `print` con `--printer-type escpos` o `--printer-type zpl` para enviar la secuencia de bytes directos al puerto 9100 o vía CUPS/IPP.

---

## 💬 Comunidad y Soporte

* 🐞 **Reporte de Problemas:** [GitHub Issues](https://github.com/thabot/bangplanix/issues)
* 💡 **Debates y Preguntas:** [GitHub Discussions](https://github.com/thabot/bangplanix/discussions)
* 💼 **Licencias Comerciales y Soporte OEM:** `thabot47@gmail.com`

