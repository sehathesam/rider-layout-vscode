using System.Text;
using System.Text.RegularExpressions;

namespace RiderLayout.Rider.Settings;

public sealed class RiderSettingsReader
{
    private static readonly Regex KeyRegex = new(
        @"CSharpFileLayoutPatterns|AdditionalFileLayout|FileLayout",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string? FindLayoutXml(string projectRoot)
    {
        foreach (var file in EnumerateCandidateFiles(projectRoot))
        {
            try
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                var xml = ExtractEmbeddedLayout(text);
                if (!string.IsNullOrWhiteSpace(xml)) return xml;
            }
            catch
            {
                // Ignore unreadable/non-text candidates; the CLI will continue searching.
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateFiles(string projectRoot)
    {
        if (!Directory.Exists(projectRoot)) yield break;

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.xml", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                continue;

            var name = Path.GetFileName(file);
            if (KeyRegex.IsMatch(name) || file.Contains(".idea", StringComparison.OrdinalIgnoreCase) || file.Contains(".DotSettings", StringComparison.OrdinalIgnoreCase))
                yield return file;
        }

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.DotSettings", SearchOption.AllDirectories))
            yield return file;
    }

    private static string? ExtractEmbeddedLayout(string text)
    {
        const string start = "&lt;Patterns";
        var encodedStart = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (encodedStart >= 0)
        {
            var decoded = System.Net.WebUtility.HtmlDecode(text[encodedStart..]);
            return ExtractPatterns(decoded);
        }

        return ExtractPatterns(text);
    }

    private static string? ExtractPatterns(string text)
    {
        var start = text.IndexOf("<Patterns", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        var end = text.IndexOf("</Patterns>", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0) return null;
        return text[start..(end + "</Patterns>".Length)];
    }
}
