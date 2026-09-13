using System;

namespace Bangplanix.Engine.Security;

#pragma warning disable CA2217 // Do not mark enums with FlagsAttribute (ISO 32000-2 standard permission bits)
[Flags]
public enum PdfPermissions
{
    None = 0,
    PrintLowQuality = 1 << 2,
    PrintHighQuality = (1 << 2) | (1 << 11),
    ModifyContents = 1 << 3,
    CopyContent = 1 << 4,
    ModifyAnnotations = 1 << 5,
    FillForms = 1 << 8,
    ExtractForAccessibility = 1 << 9,
    AssembleDocument = 1 << 10,
    All = PrintHighQuality | ModifyContents | CopyContent | ModifyAnnotations | FillForms | ExtractForAccessibility | AssembleDocument
}
#pragma warning restore CA2217

public class PdfSecurityPolicy
{
    public string? UserPassword { get; set; }
    public string? OwnerPassword { get; set; }
    public PdfPermissions Permissions { get; set; } = PdfPermissions.PrintHighQuality | PdfPermissions.CopyContent;
    public bool EnableAes256Encryption { get; set; } = true;
    public string? ForensicWatermarkText { get; set; }
    public float WatermarkOpacity { get; set; } = 0.12f;
    public float WatermarkRotationDegrees { get; set; } = -35f;
}
