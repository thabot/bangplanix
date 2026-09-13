using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.EscPos;

public sealed class EscPosReceiptAdapter : ILegacyReportAdapter
{
    public string FormatName => "Epson ESC/POS Receipt";
    public IReadOnlyList<string> SupportedExtensions => [".escpos", ".pos", ".bin.prn", ".receipt.bin"];

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ConvertBytes(ms.ToArray());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return ConvertBytes(Encoding.UTF8.GetBytes(content));
    }

    private static ReportDefinition ConvertBytes(byte[] bytes)
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "POS Receipt",
                Author = "ESC/POS Binary Converter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 226.77,
                Height = 600,
                Margins = new MarginDefinition { Left = 10, Right = 10, Top = 10, Bottom = 10 }
            }
        };

        var detailBand = new BandDefinition { Height = 40 };
        double currentY = 10;

        var textSb = new StringBuilder();
        bool isBold = false;
        bool isDoubleHeight = false;

        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];

            if (b == 0x1B)
            {
                if (i + 1 < bytes.Length)
                {
                    byte cmd = bytes[++i];
                    if (cmd == 0x45)
                    {
                        if (i + 1 < bytes.Length) isBold = bytes[++i] != 0;
                    }
                    else if (cmd == 0x21)
                    {
                        if (i + 1 < bytes.Length)
                        {
                            byte mode = bytes[++i];
                            isBold = (mode & 0x08) != 0;
                            isDoubleHeight = (mode & 0x10) != 0;
                        }
                    }
                    else if (cmd == 0x61)
                    {
                        if (i + 1 < bytes.Length) i++;
                    }
                }
                continue;
            }
            else if (b == 0x1D)
            {
                if (i + 1 < bytes.Length)
                {
                    byte cmd = bytes[++i];
                    if (cmd == 0x56)
                    {
                        if (i + 1 < bytes.Length) i++;
                    }
                }
                continue;
            }
            else if (b == 0x0A)
            {
                var lineStr = textSb.ToString().TrimEnd('\r');
                textSb.Clear();

                if (!string.IsNullOrWhiteSpace(lineStr))
                {
                    var elem = new ElementDefinition
                    {
                        Type = ElementType.Text,
                        X = 10,
                        Y = currentY,
                        Width = 200,
                        Height = isDoubleHeight ? 28 : 18,
                        Text = lineStr,
                        Style = new StyleDefinition
                        {
                            FontSize = isDoubleHeight ? 14 : 9,
                            FontWeight = isBold ? "Bold" : "Normal"
                        }
                    };
                    detailBand.Elements.Add(elem);
                    currentY += elem.Height + 4;
                }
                continue;
            }

            textSb.Append((char)b);
        }

        if (textSb.Length > 0)
        {
            var lineStr = textSb.ToString().TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(lineStr))
            {
                detailBand.Elements.Add(new ElementDefinition
                {
                    Type = ElementType.Text,
                    X = 10,
                    Y = currentY,
                    Width = 200,
                    Height = 18,
                    Text = lineStr
                });
                currentY += 22;
            }
        }

        detailBand.Height = Math.Max(40, currentY + 10);
        report.Bands.Detail = detailBand;

        return report;
    }
}
