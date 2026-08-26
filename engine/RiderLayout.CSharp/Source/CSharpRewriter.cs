using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiderLayout.Core.Engine;
using RiderLayout.Core.Model;
using RiderLayout.CSharp.Parsing;

namespace RiderLayout.CSharp.Source;

public sealed class CSharpRewriter
{
    private readonly CSharpDocumentParser _parser = new();
    private readonly LayoutEngine _engine = new();

    public string Rearrange(string source, TypePattern pattern, RegionOptions? regions = null)
    {
        regions ??= new RegionOptions();
        var parsed = _parser.ParseFirstClass(source);
        var groups = _engine.ArrangeGroups(parsed.Members, pattern);
        var ordered = groups.SelectMany(g => g.Members).ToList();
        if (!regions.HasEnabled &&
            ordered.Select(x => x.OriginalIndex).SequenceEqual(parsed.Members.Select(x => x.OriginalIndex)))
            return source;

        var declaration = parsed.Declaration;
        var openBrace = declaration.OpenBraceToken;
        var closeBrace = declaration.CloseBraceToken;

        var firstMember = declaration.Members[0];
        var lineStart = source.LastIndexOf('\n', firstMember.SpanStart - 1) + 1;
        var indent = source[lineStart..firstMember.SpanStart];

        var builder = new StringBuilder(source.Length);
        builder.Append(source, 0, declaration.SpanStart);
        builder.Append(source, declaration.SpanStart, openBrace.Span.End - declaration.SpanStart);

        if (!regions.HasEnabled)
        {
            // Legacy flat emission: every member on its own, separated by a
            // blank line. Group boundaries (region cosmetics) are ignored.
            for (var i = 0; i < ordered.Count; i++)
            {
                builder.Append('\n');
                if (i > 0) builder.Append('\n');
                builder.Append(RenderMember(source, declaration.Members[ordered[i].OriginalIndex], indent));
            }
        }
        else
        {
            EmitWithRegions(builder, source, declaration, groups, indent, regions);
        }

        builder.Append('\n');
        builder.Append(source, closeBrace.SpanStart, source.Length - closeBrace.SpanStart);
        return builder.ToString();
    }

    private static void EmitWithRegions(
        StringBuilder builder,
        string source,
        ClassDeclarationSyntax declaration,
        IReadOnlyList<ArrangeGroup> groups,
        string indent,
        RegionOptions regions)
    {
        // Build a flat sequence of logical lines where adjacent members and
        // region markers are separated by a blank line. A member's own leading
        // attribute/doc-comment lines may span several physical lines but are
        // treated as one block so the blank-line rhythm stays uniform.
        var blocks = new List<string>();
        foreach (var group in groups)
        {
            if (group.Members.Count == 0) continue;

            if (regions.IsEnabled(group.RegionName))
            {
                blocks.Add(indent + "#region " + group.RegionName);
                foreach (var member in group.Members)
                    blocks.Add(RenderMember(source, declaration.Members[member.OriginalIndex], indent));
                blocks.Add(indent + "#endregion");
            }
            else
            {
                foreach (var member in group.Members)
                    blocks.Add(RenderMember(source, declaration.Members[member.OriginalIndex], indent));
            }
        }

        for (var i = 0; i < blocks.Count; i++)
        {
            builder.Append('\n');
            if (i > 0) builder.Append('\n');
            builder.Append(blocks[i]);
        }
    }

    private static string RenderMember(string source, MemberDeclarationSyntax member, string indent)
    {
        var leading = source.AsSpan(member.FullSpan.Start, member.SpanStart - member.FullSpan.Start).ToString();
        var lines = leading.Split('\n').ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0])) lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);

        var text = source.Substring(member.SpanStart, member.FullSpan.End - member.SpanStart).TrimEnd();
        var sb = new StringBuilder();
        if (lines.Count > 0)
            sb.Append(string.Join('\n', lines)).Append('\n').Append(indent);
        sb.Append(indent).Append(text);
        return sb.ToString();
    }
}