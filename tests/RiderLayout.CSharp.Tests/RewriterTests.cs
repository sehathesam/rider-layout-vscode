using RiderLayout.CSharp.Source;
using RiderLayout.Core.Model;
using RiderLayout.Rider.Xml;
using Xunit;

namespace RiderLayout.CSharp.Tests;

public class RewriterTests
{
    [Fact]
    public void RearrangesAClassUsingRiderPattern()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default">
            <Entry DisplayName="Fields"><Entry.Match><Kind Is="Field" /></Entry.Match></Entry>
            <Entry DisplayName="Properties"><Entry.Match><Kind Is="Property" /></Entry.Match></Entry>
            <Entry DisplayName="Constructors"><Entry.Match><Kind Is="Constructor" /></Entry.Match></Entry>
            <Entry DisplayName="Methods"><Entry.Match><Kind Is="Method" /></Entry.Match><Entry.SortBy><Name /></Entry.SortBy></Entry>
          </TypePattern>
        </Patterns>
        """;

        const string input = """
        public class Demo
        {
            public void Z() { }
            private int _x;
            public int X => _x;
            public Demo() { }
            public void A() { }
        }
        """;

        var pattern = new RiderLayoutXmlParser().Parse(xml).TypePatterns[0];
        var output = new CSharpRewriter().Rearrange(input, pattern);

        var field = output.IndexOf("private int _x", StringComparison.Ordinal);
        var property = output.IndexOf("public int X", StringComparison.Ordinal);
        var ctor = output.IndexOf("public Demo()", StringComparison.Ordinal);
        var methodA = output.IndexOf("public void A", StringComparison.Ordinal);
        var methodZ = output.IndexOf("public void Z", StringComparison.Ordinal);

        Assert.True(field < property && property < ctor && ctor < methodA && methodA < methodZ);
    }

    [Fact]
    public void PreservesCommentsAboveMembersWhenReordering()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default">
            <Entry DisplayName="Fields"><Entry.Match><Kind Is="Field" /></Entry.Match></Entry>
            <Entry DisplayName="Methods"><Entry.Match><Kind Is="Method" /></Entry.Match></Entry>
          </TypePattern>
        </Patterns>
        """;

        const string input = """
        public class Demo
        {
            /// <summary>Zombie docs.</summary>
            public void Z() { }

            // Plain comment for the field.
            private int _x;
        }
        """;

        var pattern = new RiderLayoutXmlParser().Parse(xml).TypePatterns[0];
        var output = new CSharpRewriter().Rearrange(input, pattern);

        var method = output.IndexOf("/// <summary>Zombie docs.</summary>", StringComparison.Ordinal);
        var comment = output.IndexOf("// Plain comment for the field.", StringComparison.Ordinal);
        var field = output.IndexOf("private int _x", StringComparison.Ordinal);
        var z = output.IndexOf("public void Z", StringComparison.Ordinal);

        Assert.True(field < z);
        Assert.True(comment < field);
        Assert.True(method < z);
    }

    [Fact]
    public void PreservesTrailingInlineCommentOnMemberLine()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default">
            <Entry DisplayName="Fields"><Entry.Match><Kind Is="Field" /></Entry.Match></Entry>
            <Entry DisplayName="Methods"><Entry.Match><Kind Is="Method" /></Entry.Match></Entry>
          </TypePattern>
        </Patterns>
        """;

        const string input = """
        public class Demo
        {
            public void Z() { }
            private int _x; // keep the count
        }
        """;

        var pattern = new RiderLayoutXmlParser().Parse(xml).TypePatterns[0];
        var output = new CSharpRewriter().Rearrange(input, pattern);

        Assert.Contains("private int _x; // keep the count", output);
    }

    [Fact]
    public void PreservesMultilineDocCommentAboveMember()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default">
            <Entry DisplayName="Fields"><Entry.Match><Kind Is="Field" /></Entry.Match></Entry>
            <Entry DisplayName="Methods"><Entry.Match><Kind Is="Method" /></Entry.Match></Entry>
          </TypePattern>
        </Patterns>
        """;

        const string input = """
        public class Demo
        {
            /// <summary>
            /// A longer summary that spans
            /// multiple lines.
            /// </summary>
            public void Z() { }

            private int _x;
        }
        """;

        var pattern = new RiderLayoutXmlParser().Parse(xml).TypePatterns[0];
        var output = new CSharpRewriter().Rearrange(input, pattern);

        var method = output.IndexOf("public void Z", StringComparison.Ordinal);

        Assert.Contains("/// <summary>", output);
        Assert.Contains("/// A longer summary that spans", output);
        Assert.Contains("/// multiple lines.", output);
        Assert.Contains("/// </summary>", output);
        Assert.True(output.IndexOf("/// <summary>", StringComparison.Ordinal) < method);
    }

    [Fact]
    public void PreservesIndentationOfClosingBraceInNestedClass()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default">
            <Entry DisplayName="Fields"><Entry.Match><Kind Is="Field" /></Entry.Match></Entry>
            <Entry DisplayName="Methods"><Entry.Match><Kind Is="Method" /></Entry.Match></Entry>
          </TypePattern>
        </Patterns>
        """;

        const string input = """
        namespace App
        {
            public class Demo
            {
                public void Z() { }
                private int _x;
                public void A() { }
            }
        }
        """;

        var pattern = new RiderLayoutXmlParser().Parse(xml).TypePatterns[0];
        var output = new CSharpRewriter().Rearrange(input, pattern).Replace("\r\n", "\n");

        Assert.True(output.Contains("\n    }\n}", StringComparison.Ordinal));
        Assert.False(output.Contains("\n}\n}", StringComparison.Ordinal));
    }

    [Fact]
    public void FileWithoutClassReturnsUnchanged()
    {
        const string source = """
        namespace App
        {
            public interface IReply
            {
                void Init();
            }
        }
        """;

        const string layoutXml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
            <TypePattern DisplayName="X" Priority="1">
                <Region Name="CTORS">
                    <Entry DisplayName="Ctor">
                        <Entry.Match>
                            <Kind Is="Constructor" />
                        </Entry.Match>
                    </Entry>
                </Region>
            </TypePattern>
        </Patterns>
        """;

        var pattern = new RiderLayoutXmlParser().Parse(layoutXml).TypePatterns[0];
        var regions = new RegionOptions
        {
            Enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CTORS" }
        };

        var output = new CSharpRewriter().Rearrange(source, pattern, regions);
        Assert.Equal(source, output);
    }
}
