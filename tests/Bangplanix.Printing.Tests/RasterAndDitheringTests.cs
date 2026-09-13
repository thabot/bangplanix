using Bangplanix.Printing.Raster;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Printing.Tests;

public class RasterAndDitheringTests
{
    [Fact]
    public void MonoBitmapRasterizer_ConvertToMonochrome_ShouldProcessRgbaBuffer()
    {
        int width = 8;
        int height = 8;
        byte[] rgba = new byte[width * height * 4];

        // Create 4x8 black and 4x8 white
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width + x) * 4;
                byte val = (byte)(x < 4 ? 0 : 255); // Black left, white right
                rgba[idx] = val;
                rgba[idx + 1] = val;
                rgba[idx + 2] = val;
                rgba[idx + 3] = 255;
            }
        }

        var bits = MonoBitmapRasterizer.ConvertToMonochrome(width, height, rgba, useDithering: false);
        bits.GetLength(0).Should().Be(width);
        bits.GetLength(1).Should().Be(height);

        bits[0, 0].Should().BeTrue();  // Black
        bits[7, 0].Should().BeFalse(); // White
    }

    [Fact]
    public void MonoBitmapRasterizer_ToEscPosRasterBytes_ShouldProduceValidGsV0()
    {
        var bits = new bool[8, 8];
        bits[0, 0] = true;
        bits[1, 1] = true;

        var bytes = MonoBitmapRasterizer.ToEscPosRasterBytes(bits);
        bytes.Should().NotBeEmpty();

        // GS v 0 (0x1D 0x76 0x30 0x00)
        bytes[0].Should().Be(0x1D);
        bytes[1].Should().Be(0x76);
        bytes[2].Should().Be(0x30);
        bytes[3].Should().Be(0x00);
    }

    [Fact]
    public void MonoBitmapRasterizer_ToZplGraphicField_ShouldProduceValidGfCommand()
    {
        var bits = new bool[8, 2];
        bits[0, 0] = true;
        bits[7, 1] = true;

        var gf = MonoBitmapRasterizer.ToZplGraphicField(bits, 10, 20);
        gf.Should().StartWith("^FO10,20^GFA,2,2,1,");
        gf.TrimEnd().Should().EndWith("^FS");
    }
}
