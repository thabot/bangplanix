/**
 * Polyglot SDK Code Generator for Bangplanix Designer
 * Generates ready-to-run code snippets in 5 programming languages:
 * C# (.NET), Node.js, Python, Go, and Java
 */
import { BpxReportSchema, SdkLanguage } from '../core/types.js';

export class SdkCodeGenerator {
  generate(lang: SdkLanguage, report: BpxReportSchema): string {
    const jsonStr = JSON.stringify(report, null, 2);

    switch (lang) {
      case 'csharp':
        return `// C# (.NET 10 / Native AOT)
using Bangplanix.Engine;
using Bangplanix.Core;

var template = BangplanixTemplate.FromJson("""
${jsonStr}
""");

// Fast sub-millisecond document generation
byte[] pdfBytes = await BangplanixEngine.RenderAsync(template, new {
    InvoiceNo = "INV-2026-8801",
    Total = 1999.00m
});

await File.WriteAllBytesAsync("document.pdf", pdfBytes);`;

      case 'nodejs':
        return `// Node.js (TypeScript / JavaScript)
import { BangplanixClient } from '@bangplanix/client';
import * as fs from 'node:fs/promises';

const client = new BangplanixClient({
  endpoint: process.env.BANGPLANIX_SERVER_URL || 'http://localhost:5000'
});

const template = ${jsonStr};

const pdfBuffer = await client.render({
  template,
  data: { InvoiceNo: "INV-2026-8801", Total: 1999.00 }
});

await fs.writeFile('document.pdf', pdfBuffer);`;

      case 'python':
        return `# Python 3.10+
from bangplanix import Client
import json

client = Client(endpoint="http://localhost:5000")

template = json.loads("""
${jsonStr}
""")

pdf_bytes = client.render(template, data={
    "InvoiceNo": "INV-2026-8801",
    "Total": 1999.00
})

with open("document.pdf", "wb") as f:
    f.write(pdf_bytes)`;

      case 'go':
        return `// Go 1.22+
package main

import (
    "context"
    "os"
    "github.com/thabot/bangplanix/sdk/go"
)

func main() {
    client := bangplanix.NewClient("http://localhost:5000")
    templateJson := \`${jsonStr}\`

    pdfBytes, err := client.Render(context.Background(), templateJson, map[string]any{
        "InvoiceNo": "INV-2026-8801",
        "Total": 1999.00,
    })
    if err != nil {
        panic(err)
    }

    _ = os.WriteFile("document.pdf", pdfBytes, 0644)
}`;

      case 'java':
        return `// Java 21+
import io.bangplanix.client.BangplanixClient;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;

public class App {
    public static void main(String[] args) throws Exception {
        var client = BangplanixClient.builder()
            .endpoint("http://localhost:5000")
            .build();

        String templateJson = """
${jsonStr}
""";

        byte[] pdfBytes = client.render(templateJson, Map.of(
            "InvoiceNo", "INV-2026-8801",
            "Total", 1999.00
        ));

        Files.write(Path.of("document.pdf"), pdfBytes);
    }
}`;

      default:
        return jsonStr;
    }
  }
}
