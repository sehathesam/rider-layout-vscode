using RiderLayout.Core.Engine;
using RiderLayout.Core.Matching;
using RiderLayout.Core.Model;
using Xunit;

namespace RiderLayout.Core.Tests;

public class RegionPriorityTests
{
    [Fact]
    public void MembersInRegionsAreEmittedInRegionOrder()
    {
        var pattern = new TypePattern();
        var fields = new RegionNode();
        fields.Children.Add(new EntryNode { DisplayName = "Fields", Match = new KindExpression(MemberKind.Field) });
        var methods = new RegionNode();
        methods.Children.Add(new EntryNode { DisplayName = "Methods", Match = new KindExpression(MemberKind.Method) });
        pattern.Children.Add(fields);
        pattern.Children.Add(methods);

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Method, Name = "Do", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_x", OriginalIndex = 1 },
            new() { Kind = MemberKind.Field, Name = "_y", OriginalIndex = 2 }
        };

        var result = new LayoutEngine().Arrange(members, pattern);
        Assert.Equal(["_x", "_y", "Do"], result.Select(x => x.Name));
    }

    [Fact]
    public void RegionPriorityBeatsEntryPriority()
    {
        // Same member matches two entries; the one inside the higher-priority
        // region must win even though the other entry has a higher own priority.
        var pattern = new TypePattern();
        var low = new RegionNode();
        low.Children.Add(new EntryNode
        {
            DisplayName = "High entry, low region",
            Match = new KindExpression(MemberKind.Field),
            Priority = 500
        });
        var high = new RegionNode { Priority = 200 };
        high.Children.Add(new EntryNode
        {
            DisplayName = "Low entry, high region",
            Match = new KindExpression(MemberKind.Field)
        });
        pattern.Children.Add(low);
        pattern.Children.Add(high);

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Field, Name = "_a", OriginalIndex = 0 }
        };

        var result = new LayoutEngine().Arrange(members, pattern);
        Assert.Equal("_a", result.Single().Name);
    }

    [Fact]
    public void CatchAllEntryAbsorbsUnmatchedMembersOnly()
    {
        var pattern = new TypePattern();
        pattern.Children.Add(new EntryNode { DisplayName = "Fields", Match = new KindExpression(MemberKind.Field) });
        pattern.Children.Add(new EntryNode { DisplayName = "All other members" });

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Method, Name = "Do", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_x", OriginalIndex = 1 },
            new() { Kind = MemberKind.Property, Name = "P", OriginalIndex = 2 }
        };

        var result = new LayoutEngine().Arrange(members, pattern);
        Assert.Equal(["_x", "Do", "P"], result.Select(x => x.Name));
    }

    [Fact]
    public void StaticFieldEntryBeatsGenericFieldEntryOnTie()
    {
        var pattern = new TypePattern();
        var fields = new RegionNode();
        fields.Children.Add(new EntryNode
        {
            DisplayName = "Static fields",
            Match = new AndExpression
            {
                Children =
                {
                    new KindExpression(MemberKind.Field),
                    new ModifierExpression(ModifierKind.Static)
                }
            }
        });
        fields.Children.Add(new EntryNode
        {
            DisplayName = "Fields",
            Match = new KindExpression(MemberKind.Field)
        });
        pattern.Children.Add(fields);

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Field, Name = "_instance", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_static", OriginalIndex = 1, IsStatic = true }
        };

        var result = new LayoutEngine().Arrange(members, pattern);
        Assert.Equal(["_static", "_instance"], result.Select(x => x.Name));
    }

    [Fact]
    public void ArrangeGroupsTagsMembersWithTheirRegionName()
    {
        var pattern = new TypePattern();
        var fields = new RegionNode { Name = "FIELDS" };
        fields.Children.Add(new EntryNode { DisplayName = "Static fields", Match = new KindExpression(MemberKind.Field) });
        fields.Children.Add(new EntryNode { DisplayName = "Fields", Match = new KindExpression(MemberKind.Field) });
        var methods = new RegionNode { Name = "METHODS" };
        methods.Children.Add(new EntryNode { DisplayName = "Methods", Match = new KindExpression(MemberKind.Method) });
        pattern.Children.Add(fields);
        pattern.Children.Add(methods);

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Method, Name = "Do", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_x", OriginalIndex = 1 },
            new() { Kind = MemberKind.Field, Name = "_y", OriginalIndex = 2 }
        };

        var groups = new LayoutEngine().ArrangeGroups(members, pattern);

        Assert.Collection(groups,
            g => { Assert.Equal("FIELDS", g.RegionName); Assert.Equal(["_x", "_y"], g.Members.Select(x => x.Name)); },
            g => { Assert.Equal("METHODS", g.RegionName); Assert.Equal(["Do"], g.Members.Select(x => x.Name)); });
    }

    [Fact]
    public void ArrangeGroupsMergesConsecutiveSlotsInSameRegionIntoOneGroup()
    {
        // Two entries in one region must collapse into a single group so the
        // rewriter can wrap them in one #region block.
        var pattern = new TypePattern();
        var fields = new RegionNode { Name = "FIELDS" };
        fields.Children.Add(new EntryNode { DisplayName = "A", Match = new KindExpression(MemberKind.Field) });
        fields.Children.Add(new EntryNode { DisplayName = "B", Match = new KindExpression(MemberKind.Method) });
        pattern.Children.Add(fields);

        var members = new List<CSharpMember>
        {
            new() { Kind = MemberKind.Method, Name = "Do", OriginalIndex = 0 },
            new() { Kind = MemberKind.Field, Name = "_x", OriginalIndex = 1 }
        };

        var groups = new LayoutEngine().ArrangeGroups(members, pattern);
        Assert.Single(groups);
        Assert.Equal("FIELDS", groups[0].RegionName);
        Assert.Equal(["_x", "Do"], groups[0].Members.Select(x => x.Name));
    }
}