using RiderLayout.Core.Engine;
using RiderLayout.Core.Matching;
using RiderLayout.Core.Model;
using Xunit;

namespace RiderLayout.Core.Tests;

public class LayoutEngineTests
{
    [Fact]
    public void OrdersMembersByEntryPositionAndKeepsUnmatchedLast()
    {
        var pattern = new TypePattern();
        pattern.Children.Add(new EntryNode { Match = new KindExpression(MemberKind.Field) });
        pattern.Children.Add(new EntryNode { Match = new KindExpression(MemberKind.Property) });
        pattern.Children.Add(new EntryNode { Match = new KindExpression(MemberKind.Method) });

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Method, Name = "Update", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_value", OriginalIndex = 1 },
            new() { Kind = MemberKind.Property, Name = "Value", OriginalIndex = 2 },
            new() { Kind = MemberKind.Event, Name = "Changed", OriginalIndex = 3 }
        };

        var result = new LayoutEngine().Arrange(members, pattern);
        Assert.Equal(["_value", "Value", "Update", "Changed"], result.Select(x => x.Name));
    }
}
