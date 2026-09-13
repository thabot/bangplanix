# 🚀 Bangplanix.Client — Official .NET SDK

[![NuGet](https://img.shields.io/nuget/v/Bangplanix.Client.svg)](https://www.nuget.org/packages/Bangplanix.Client/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/thabot/bangplanix/blob/main/LICENSE.md)

Official high-performance **.NET 10 Client SDK** for **Bangplanix Reporting Engine**.

---

## 📦 Installation

```bash
dotnet add package Bangplanix.Client
```

---

## ⚡ Quick Start

```csharp
using System.Text.Json;
using Bangplanix.Client;

// 1. Initialize client
var client = new BangplanixClient("http://localhost:9545");

// 2. Prepare request
var request = new RenderReportRequest
{
    TemplatePath = "templates/invoice.bpx",
    DataJson = JsonSerializer.Serialize(new[]
    {
        new { Item = "Cloud Server", Qty = 1, Price = 15000 },
        new { Item = "Premium Support", Qty = 1, Price = 5000 }
    })
};

// 3. Render report to PDF
byte[] pdfBytes = await client.RenderReportAsync(request);
await File.WriteAllBytesAsync("invoice.pdf", pdfBytes);

Console.WriteLine("Invoice rendered successfully!");
```

---

## 🌐 Features

- 🚀 **High Performance:** Microsecond report generation with native HTTP/gRPC streaming.
- 🇹🇭 **Full Thai Typography:** Complex Thai font shaping and BahtText currency rendering.
- 📄 **Multiple Output Formats:** PDF, XLSX, DOCX, SVG, and HTML5.
- 🛡️ **Enterprise Security:** Built-in HMAC token authentication and PII vector redaction.

---

## 📖 Documentation & Links

- 🌐 **Interactive WASM Playground:** [https://thabot.github.io/bangplanix/](https://thabot.github.io/bangplanix/)
- 🐙 **GitHub Repository:** [https://github.com/thabot/bangplanix](https://github.com/thabot/bangplanix)
- 📜 **License:** [MIT License](https://github.com/thabot/bangplanix/blob/main/LICENSE.md)

