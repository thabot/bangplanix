using System.Text;

namespace Bangplanix.Printing.Raster;

public static class MonoBitmapRasterizer
{
    public static bool[,] ConvertToMonochrome(int width, int height, byte[] rgbaPixels, bool useDithering = true)
    {
        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (rgbaPixels.Length < width * height * 4)
        {
            throw new ArgumentException("Pixel buffer length is smaller than width * height * 4", nameof(rgbaPixels));
        }

        var gray = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                byte r = rgbaPixels[idx];
                byte g = rgbaPixels[idx + 1];
                byte b = rgbaPixels[idx + 2];
                byte a = rgbaPixels[idx + 3];

                if (a < 128)
                {
                    gray[x, y] = 255.0; // White transparent
                }
                else
                {
                    gray[x, y] = 0.299 * r + 0.587 * g + 0.114 * b;
                }
            }
        }

        var result = new bool[width, height];

        if (!useDithering)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = gray[x, y] < 128.0; // true = black dot
                }
            }
            return result;
        }

        // Floyd-Steinberg Error Diffusion Dithering
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double oldVal = gray[x, y];
                double newVal = oldVal < 128.0 ? 0.0 : 255.0;
                result[x, y] = newVal == 0.0; // true = black
                double error = oldVal - newVal;

                if (x + 1 < width)
                    gray[x + 1, y] += error * 7.0 / 16.0;
                if (x - 1 >= 0 && y + 1 < height)
                    gray[x - 1, y + 1] += error * 3.0 / 16.0;
                if (y + 1 < height)
                    gray[x, y + 1] += error * 5.0 / 16.0;
                if (x + 1 < width && y + 1 < height)
                    gray[x + 1, y + 1] += error * 1.0 / 16.0;
            }
        }

        return result;
    }

    public static byte[] ToEscPosRasterBytes(bool[,] monoBits)
    {
        int width = monoBits.GetLength(0);
        int height = monoBits.GetLength(1);

        int bytesPerLine = (width + 7) / 8;
        using var ms = new MemoryStream();

        // GS v 0 m xL xH yL yH
        // m = 0 (Normal)
        ms.WriteByte(0x1D);
        ms.WriteByte(0x76);
        ms.WriteByte(0x30);
        ms.WriteByte(0x00);

        ms.WriteByte((byte)(bytesPerLine & 0xFF));
        ms.WriteByte((byte)((bytesPerLine >> 8) & 0xFF));

        ms.WriteByte((byte)(height & 0xFF));
        ms.WriteByte((byte)((height >> 8) & 0xFF));

        for (int y = 0; y < height; y++)
        {
            for (int b = 0; b < bytesPerLine; b++)
            {
                byte currentByte = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int x = b * 8 + bit;
                    if (x < width && monoBits[x, y])
                    {
                        currentByte |= (byte)(1 << (7 - bit));
                    }
                }
                ms.WriteByte(currentByte);
            }
        }

        return ms.ToArray();
    }

    public static string ToZplGraphicField(bool[,] monoBits, int xPos = 0, int yPos = 0)
    {
        int width = monoBits.GetLength(0);
        int height = monoBits.GetLength(1);

        int bytesPerLine = (width + 7) / 8;
        int totalBytes = bytesPerLine * height;

        var sb = new StringBuilder();
        sb.Append($"^FO{xPos},{yPos}^GFA,{totalBytes},{totalBytes},{bytesPerLine},");

        for (int y = 0; y < height; y++)
        {
            for (int b = 0; b < bytesPerLine; b++)
            {
                byte currentByte = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int x = b * 8 + bit;
                    if (x < width && monoBits[x, y])
                    {
                        currentByte |= (byte)(1 << (7 - bit));
                    }
                }
                sb.Append(currentByte.ToString("X2"));
            }
        }

        sb.Append("^FS\n");
        return sb.ToString();
    }
}
