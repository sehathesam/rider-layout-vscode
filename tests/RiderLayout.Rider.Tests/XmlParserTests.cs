using RiderLayout.Rider.Xml;
using RiderLayout.Core.Model;
using Xunit;

namespace RiderLayout.Rider.Tests;

public class XmlParserTests
{
    [Fact]
    public void ParsesBasicRiderTypePattern()
    {
        const string xml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
          <TypePattern DisplayName="Default" Priority="100">
            <Entry DisplayName="Fields">
              <Entry.Match><Kind Is="Field" /></Entry.Match>
            </Entry>
            <Entry DisplayName="Methods">
              <Entry.Match><Kind Is="Method" /></Entry.Match>
              <Entry.SortBy><Name /></Entry.SortBy>
            </Entry>
          </TypePattern>
        </Patterns>
        """;

        var result = new RiderLayoutXmlParser().Parse(xml);
        Assert.Single(result.TypePatterns);
        Assert.Equal(2, result.TypePatterns[0].Children.Count);
        Assert.Equal("Name", ((RiderLayout.Core.Model.EntryNode)result.TypePatterns[0].Children[1]).SortBy[0].Key);
        Assert.True(((RiderLayout.Core.Model.EntryNode)result.TypePatterns[0].Children[0]).Match!.Evaluate(new RiderLayout.Core.Matching.MatchContext(
            new CSharpMember { Kind = MemberKind.Field, Name = "x" })));
    }
}
