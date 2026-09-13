using System.Text;
using Bangplanix.Printing.Raster;

namespace Bangplanix.Printing.EscPos;

public enum EscPosAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}

public enum EscPosCutType
{
    Full = 0,
    Partial = 1
}

public sealed class EscPosGenerator : IDisposable
{
    private readonly MemoryStream _buffer = new();
    private readonly Encoding _encoding;
    private bool _disposed;

    public EscPosGenerator(Encoding? encoding = null)
    {
        _encoding = encoding ?? Encoding.GetEncoding("windows-874", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
        Initialize();
    }

    public static EscPosGenerator CreateThai()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(874);
        var gen = new EscPosGenerator(enc);
        gen.SetCodePage(21); // Typically CP874 on Epson/Star
        return gen;
    }

    public static EscPosGenerator CreateUtf8()
    {
        return new EscPosGenerator(Encoding.UTF8);
    }

    public EscPosGenerator Initialize()
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x40); // ESC @
        return this;
    }

    public EscPosGenerator SetCodePage(byte codePage)
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x74); // ESC t n
        _buffer.WriteByte(codePage);
        return this;
    }

    public EscPosGenerator Align(EscPosAlignment alignment)
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x61); // ESC a n
        _buffer.WriteByte((byte)alignment);
        return this;
    }

    public EscPosGenerator Bold(bool enable = true)
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x45); // ESC E n
        _buffer.WriteByte((byte)(enable ? 1 : 0));
        return this;
    }

    public EscPosGenerator Underline(bool enable = true)
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x2D); // ESC - n
        _buffer.WriteByte((byte)(enable ? 1 : 0));
        return this;
    }

    public EscPosGenerator Invert(bool enable = true)
    {
        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x42); // GS B n
        _buffer.WriteByte((byte)(enable ? 1 : 0));
        return this;
    }

    public EscPosGenerator TextSize(int widthScale = 1, int heightScale = 1)
    {
        widthScale = Math.Clamp(widthScale, 1, 8);
        heightScale = Math.Clamp(heightScale, 1, 8);
        byte n = (byte)(((widthScale - 1) << 4) | (heightScale - 1));

        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x21); // GS ! n
        _buffer.WriteByte(n);
        return this;
    }

    public EscPosGenerator Text(string text)
    {
        if (string.IsNullOrEmpty(text)) return this;
        var bytes = _encoding.GetBytes(text);
        _buffer.Write(bytes, 0, bytes.Length);
        return this;
    }

    public EscPosGenerator TextLine(string text = "")
    {
        Text(text);
        _buffer.WriteByte(0x0A); // LF
        return this;
    }

    public EscPosGenerator FeedLines(int lines = 1)
    {
        for (int i = 0; i < lines; i++)
        {
            _buffer.WriteByte(0x0A);
        }
        return this;
    }

    public EscPosGenerator SeparatorLine(char ch = '-', int length = 42)
    {
        return TextLine(new string(ch, length));
    }

    public EscPosGenerator TwoColumns(string left, string right, int totalWidth = 42)
    {
        var spaces = Math.Max(1, totalWidth - (left?.Length ?? 0) - (right?.Length ?? 0));
        return TextLine($"{left}{new string(' ', spaces)}{right}");
    }

    public EscPosGenerator Cut(EscPosCutType cutType = EscPosCutType.Partial, int feedLines = 3)
    {
        FeedLines(feedLines);
        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x56); // GS V m n
        _buffer.WriteByte((byte)(cutType == EscPosCutType.Full ? 65 : 66));
        _buffer.WriteByte(0x00);
        return this;
    }

    public EscPosGenerator OpenCashDrawer(byte pin = 0)
    {
        _buffer.WriteByte(0x1B);
        _buffer.WriteByte(0x70); // ESC p m t1 t2
        _buffer.WriteByte(pin);  // 0: Pin 2, 1: Pin 5
        _buffer.WriteByte(0x19); // ON time
        _buffer.WriteByte(0xFA); // OFF time
        return this;
    }

    public EscPosGenerator Barcode128(string content, int height = 60, int width = 2)
    {
        if (string.IsNullOrEmpty(content)) return this;

        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x68);
        _buffer.WriteByte((byte)Math.Clamp(height, 1, 255));

        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x77);
        _buffer.WriteByte((byte)Math.Clamp(width, 2, 6));

        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x48);
        _buffer.WriteByte(0x02);

        var bytes = Encoding.ASCII.GetBytes(content);
        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x6B);
        _buffer.WriteByte(0x49); // Code128
        _buffer.WriteByte((byte)bytes.Length);
        _buffer.Write(bytes, 0, bytes.Length);

        _buffer.WriteByte(0x0A);
        return this;
    }

    public EscPosGenerator QrCode(string content, int moduleSize = 6, byte errorCorrection = 49)
    {
        if (string.IsNullOrEmpty(content)) return this;
        var bytes = Encoding.UTF8.GetBytes(content);
        int len = bytes.Length + 3;

        _buffer.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });
        _buffer.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)Math.Clamp(moduleSize, 1, 16) });
        _buffer.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, errorCorrection });

        _buffer.WriteByte(0x1D);
        _buffer.WriteByte(0x28);
        _buffer.WriteByte(0x6B);
        _buffer.WriteByte((byte)(len & 0xFF));
        _buffer.WriteByte((byte)((len >> 8) & 0xFF));
        _buffer.WriteByte(0x31);
        _buffer.WriteByte(0x50);
        _buffer.WriteByte(0x30);
        _buffer.Write(bytes, 0, bytes.Length);

        _buffer.Write(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });
        _buffer.WriteByte(0x0A);

        return this;
    }

    public EscPosGenerator RasterImage(bool[,] monoBits)
    {
        var rasterBytes = MonoBitmapRasterizer.ToEscPosRasterBytes(monoBits);
        _buffer.Write(rasterBytes, 0, rasterBytes.Length);
        _buffer.WriteByte(0x0A);
        return this;
    }

    public byte[] ToByteArray()
    {
        return _buffer.ToArray();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _buffer.Dispose();
            _disposed = true;
        }
    }
}
