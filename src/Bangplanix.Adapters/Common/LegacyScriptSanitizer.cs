using System.Text.RegularExpressions;

namespace Bangplanix.Adapters.Common;

public static class LegacyScriptSanitizer
{
    private static readonly Regex UnsafeKeywordRegex = new(
        @"\b(System\.Reflection|Activator\.CreateInstance|Process\.Start|DllImport|Marshal\.|Unsafe\.|Type\.GetType|Assembly\.Load)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CodeBlockRegex = new(@"<Code[^>]*>([\s\S]*?)<\/Code>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ScriptBlockRegex = new(@"<Script[^>]*>([\s\S]*?)<\/Script>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Sanitize(string xmlOrScript)
    {
        if (string.IsNullOrWhiteSpace(xmlOrScript)) return string.Empty;

        // Check for unsafe RCE payload
        if (UnsafeKeywordRegex.IsMatch(xmlOrScript))
        {
            // Strip out unsafe code blocks completely
            xmlOrScript = CodeBlockRegex.Replace(xmlOrScript, "<!-- Unsafe Legacy Code Block Removed for AOT Sandbox Security -->");
            xmlOrScript = ScriptBlockRegex.Replace(xmlOrScript, "<!-- Unsafe Legacy Script Block Removed for AOT Sandbox Security -->");
        }

        return xmlOrScript;
    }

    public static bool IsSafe(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        return !UnsafeKeywordRegex.IsMatch(expression);
    }
}
