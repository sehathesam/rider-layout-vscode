using RiderLayout.Core.Model;
using RiderLayout.Core.Sorting;
using Xunit;

namespace RiderLayout.Core.Tests;

public class MemberSorterTests
{
    private static CSharpMember Member(string name, Accessibility access, MemberKind kind = MemberKind.Field,
        bool isStatic = false, bool isReadonly = false, int originalIndex = 0) => new()
    {
        Name = name,
        Access = access,
        Kind = kind,
        IsStatic = isStatic,
        IsReadonly = isReadonly,
        OriginalIndex = originalIndex
    };

    [Fact]
    public void SortByAccessUsesExplicitOrder()
    {
        var members = new List<CSharpMember>
        {
            Member("_private", Accessibility.Private),
            Member("_public", Accessibility.Public),
            Member("_internal", Accessibility.Internal)
        };

        var rules = new List<SortRule>
        {
            new() { Key = "Access", Order = ["Public", "Internal", "ProtectedInternal", "Protected", "Private"] }
        };

        var result = MemberSorter.Sort(members, rules);
        Assert.Equal(["_public", "_internal", "_private"], result.Select(x => x.Name));
    }

    [Fact]
    public void SortByKindUsesExplicitOrder()
    {
        var members = new List<CSharpMember>
        {
            Member("b", Accessibility.Public, MemberKind.Property),
            Member("a", Accessibility.Public, MemberKind.Method),
            Member("c", Accessibility.Public, MemberKind.Field)
        };

        var rules = new List<SortRule>
        {
            new() { Key = "Kind", Order = ["Method", "Property", "Indexer", "Event"] }
        };

        var result = MemberSorter.Sort(members, rules);
        Assert.Equal(["a", "b", "c"], result.Select(x => x.Name));
    }

    [Fact]
    public void KeysNotInOrderGoLastInOriginalOrder()
    {
        var members = new List<CSharpMember>
        {
            Member("_x", Accessibility.Private, originalIndex: 1),
            Member("_public", Accessibility.Public, originalIndex: 0),
            Member("_y", Accessibility.Private, originalIndex: 2)
        };

        var rules = new List<SortRule>
        {
            new() { Key = "Access", Order = ["Public"] }
        };

        var result = MemberSorter.Sort(members, rules);
        Assert.Equal(["_public", "_x", "_y"], result.Select(x => x.Name));
    }

    [Fact]
    public void SortByStaticPutsNonStaticFirstWithoutOrder()
    {
        var members = new List<CSharpMember>
        {
            Member("_static", Accessibility.Public, isStatic: true),
            Member("_instance", Accessibility.Public, isStatic: false)
        };

        var rules = new List<SortRule> { new() { Key = "Static" } };

        var result = MemberSorter.Sort(members, rules);
        Assert.Equal(["_instance", "_static"], result.Select(x => x.Name));
    }

    [Fact]
    public void TrailingDescendingTokenReversesOrder()
    {
        var members = new List<CSharpMember>
        {
            Member("_private", Accessibility.Private),
            Member("_public", Accessibility.Public),
            Member("_internal", Accessibility.Internal)
        };

        var rules = new List<SortRule>
        {
            new() { Key = "Access", Order = ["Public", "Internal", "Private"], Descending = true }
        };

        var result = MemberSorter.Sort(members, rules);
        Assert.Equal(["_private", "_internal", "_public"], result.Select(x => x.Name));
    }
}