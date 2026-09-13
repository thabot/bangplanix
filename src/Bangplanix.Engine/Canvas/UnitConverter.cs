using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Canvas;

public static class UnitConverter
{
    // Standard PDF Points (72 points per inch)
    public const double PointsPerInch = 72.0;
    public const double MmPerInch = 25.4;
    public const double PointsPerMm = PointsPerInch / MmPerInch; // ~2.83464567

    public static float ToPoints(double value, UnitType unit)
    {
        return (float)(unit switch
        {
            UnitType.Mm => value * PointsPerMm,
            UnitType.Cm => value * 10.0 * PointsPerMm,
            UnitType.In => value * PointsPerInch,
            UnitType.Pt => value,
            UnitType.Px => value * 0.75, // Assuming 96 DPI screen px to 72 DPI pt
            _ => value * PointsPerMm
        });
    }

    public static (float WidthPt, float HeightPt) GetPageDimensionsInPoints(PageSetup pageSetup)
    {
        ArgumentNullException.ThrowIfNull(pageSetup);
        var (wMm, hMm) = pageSetup.GetEffectiveDimensionsMm();
        return (ToPoints(wMm, UnitType.Mm), ToPoints(hMm, UnitType.Mm));
    }
}
