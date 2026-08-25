using System.Text;
using RiderLayout.Core.Engine;
using RiderLayout.Core.Model;
using RiderLayout.CSharp.Parsing;

namespace RiderLayout.CSharp.Source;

public sealed class CSharpRewriter
{
    private readonly CSharpDocumentParser _parser = new();
    private readonly LayoutEngine _engine = new();

    public string Rearrange(string source, TypePattern pattern)
    {
        var parsed = _parser.ParseFirstClass(source);
        var ordered = _engine.Arrange(parsed.Members, pattern);
        if (ordered.Select(x => x.OriginalIndex).SequenceEqual(parsed.Members.Select(x => x.OriginalIndex)))
            return source;

        var declaration = parsed.Declaration;
        var openBrace = declaration.OpenBraceToken;
        var closeBrace = declaration.CloseBraceToken;

        var firstMember = declaration.Members[0];
        var lineStart = source.LastIndexOf('\n', firstMember.SpanStart - 1) + 1;
        var indent = source[lineStart..firstMember.SpanStart];

        var members = ordered.Select(x => declaration.Members[x.OriginalIndex]).ToList();

        var builder = new StringBuilder(source.Length);
        builder.Append(source, 0, declaration.SpanStart);
        builder.Append(source, declaration.SpanStart, openBrace.Span.End - declaration.SpanStart);
        builder.Append('\n');
        builder.Append(indent);
        builder.Append(source, members[0].SpanStart, members[0].Span.Length);

        foreach (var member in members.Skip(1))
        {
            builder.Append("\n\n");
            builder.Append(indent);
            builder.Append(source, member.SpanStart, member.Span.Length);
        }

        builder.Append('\n');
        builder.Append(source, closeBrace.SpanStart, source.Length - closeBrace.SpanStart);
        return builder.ToString();
    }
}