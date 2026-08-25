namespace RiderLayout.Core.Model;

public sealed class LayoutPattern
{
    public List<TypePattern> TypePatterns { get; } = [];
    public List<LayoutNode> FileNodes { get; } = [];
}

public abstract class LayoutNode
{
    public string? DisplayName { get; init; }
    public int Priority { get; init; }
}

public sealed class EntryNode : LayoutNode
{
    public Matching.MatchExpression? Match { get; init; }
    public List<SortRule> SortBy { get; } = [];
}

public sealed class RegionNode : LayoutNode
{
    public string Name { get; init; } = "";
    public Matching.MatchExpression? GroupBy { get; init; }
    public List<LayoutNode> Children { get; } = [];
}

public sealed class TypePattern
{
    public string? DisplayName { get; init; }
    public int Priority { get; init; }
    public Matching.MatchExpression? Match { get; init; }
    public List<LayoutNode> Children { get; } = [];
}

public sealed class SortRule
{
    public required string Key { get; init; }
    public bool Descending { get; init; }
    public IReadOnlyList<string> Order { get; init; } = [];
}
