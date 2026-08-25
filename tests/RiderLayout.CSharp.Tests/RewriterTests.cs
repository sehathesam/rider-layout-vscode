using RiderLayout.CSharp.Source;
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
}
