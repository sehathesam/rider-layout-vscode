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

/// <summary>
/// A consecutive run of members that share the same source region. When
/// <see cref="RegionName"/> is null the members are emitted without a
/// #region tag (matches the legacy flat behavior); when non-null, the rewriter
/// wraps them in a single #region ... #endregion block.
/// </summary>
public sealed class ArrangeGroup
{
    public string? RegionName { get; init; }
    public required IReadOnlyList<CSharpMember> Members { get; init; }
}

/// <summary>
/// Controls whether (and which) source #region blocks are emitted. When no
/// region names are enabled the rewriter falls back to the legacy flat
/// member ordering with no tags.
/// </summary>
public sealed class RegionOptions
{
    public IReadOnlySet<string> Enabled { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasEnabled => Enabled.Count > 0;

    public bool IsEnabled(string? regionName)
        => regionName is not null && Enabled.Contains(regionName);
}
