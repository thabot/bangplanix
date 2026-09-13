using System.Text;
using Bangplanix.Printing.Raster;

namespace Bangplanix.Printing.Zpl;

public enum ZplDpi
{
    Dpi203 = 8,  // 8 dots per mm (203.2 DPI)
    Dpi300 = 12, // 12 dots per mm (300 DPI)
    Dpi600 = 24  // 24 dots per mm (600 DPI)
}

public enum ZplRotation
{
    Normal = 0,
    Rotated90 = 1,
    Rotated180 = 2,
    Rotated270 = 3
}

public sealed class ZplGenerator
{
    private readonly StringBuilder _sb = new();
    private readonly ZplDpi _dpi;

    public ZplGenerator(ZplDpi dpi = ZplDpi.Dpi203)
    {
        _dpi = dpi;
        StartLabel();
    }

    public int DotsPerMm => (int)_dpi;

    public int PointsToDots(double pt)
    {
        // 1 pt = 1/72 inch = 0.352778 mm
        var mm = pt * 0.352778;
        return (int)Math.Round(mm * DotsPerMm);
    }

    public int MmToDots(double mm)
    {
        return (int)Math.Round(mm * DotsPerMm);
    }

    public ZplGenerator StartLabel()
    {
        _sb.AppendLine("^XA");
        _sb.AppendLine("^CI28"); // UTF-8 Encoding mode
        return this;
    }

    public ZplGenerator EndLabel()
    {
        _sb.AppendLine("^XZ");
        return this;
    }

    public ZplGenerator SetPrintSpeed(int speedIps = 4)
    {
        _sb.AppendLine($"^PR{Math.Clamp(speedIps, 2, 14)}");
        return this;
    }

    public ZplGenerator SetDarkness(int darkness = 15)
    {
        _sb.AppendLine($"^MD{Math.Clamp(darkness, 0, 30)}");
        return this;
    }

    public ZplGenerator SetQuantity(int quantity = 1)
    {
        _sb.AppendLine($"^PQ{Math.Max(1, quantity)}");
        return this;
    }

    public ZplGenerator Text(int x, int y, string text, int fontHeightDots = 30, int fontWidthDots = 0, ZplRotation rotation = ZplRotation.Normal, char fontName = '0')
    {
        if (string.IsNullOrEmpty(text)) return this;
        var rotChar = GetRotationChar(rotation);
        if (fontWidthDots <= 0) fontWidthDots = fontHeightDots;

        _sb.AppendLine($"^FO{x},{y}^A{fontName}{rotChar},{fontHeightDots},{fontWidthDots}^FD{text}^FS");
        return this;
    }

    public ZplGenerator Box(int x, int y, int width, int height, int borderThickness = 2, int rounding = 0)
    {
        _sb.AppendLine($"^FO{x},{y}^GB{width},{height},{borderThickness},B,{rounding}^FS");
        return this;
    }

    public ZplGenerator Line(int x, int y, int length, int thickness = 2, bool horizontal = true)
    {
        if (horizontal)
        {
            _sb.AppendLine($"^FO{x},{y}^GB{length},{thickness},{thickness}^FS");
        }
        else
        {
            _sb.AppendLine($"^FO{x},{y}^GB{thickness},{length},{thickness}^FS");
        }
        return this;
    }

    public ZplGenerator Barcode128(int x, int y, string content, int height = 80, int moduleWidth = 2, ZplRotation rotation = ZplRotation.Normal, bool showText = true)
    {
        if (string.IsNullOrEmpty(content)) return this;
        var rotChar = GetRotationChar(rotation);
        var printTextChar = showText ? 'Y' : 'N';

        _sb.AppendLine($"^FO{x},{y}^BY{Math.Clamp(moduleWidth, 1, 10)}^BC{rotChar},{height},{printTextChar},N,N^FD{content}^FS");
        return this;
    }

    public ZplGenerator QrCode(int x, int y, string content, int magnification = 6, ZplRotation rotation = ZplRotation.Normal, char errorCorrection = 'M')
    {
        if (string.IsNullOrEmpty(content)) return this;
        var rotChar = GetRotationChar(rotation);

        _sb.AppendLine($"^FO{x},{y}^BQ{rotChar},2,{Math.Clamp(magnification, 1, 10)},{errorCorrection}^FDQA,{content}^FS");
        return this;
    }

    public ZplGenerator RasterImage(int x, int y, bool[,] monoBits)
    {
        var gfCommand = MonoBitmapRasterizer.ToZplGraphicField(monoBits, x, y);
        _sb.Append(gfCommand);
        return this;
    }

    public string Build()
    {
        return _sb.ToString();
    }

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(Build());
    }

    private static char GetRotationChar(ZplRotation rot) => rot switch
    {
        ZplRotation.Rotated90 => 'R',
        ZplRotation.Rotated180 => 'I',
        ZplRotation.Rotated270 => 'B',
        _ => 'N'
    };
}
