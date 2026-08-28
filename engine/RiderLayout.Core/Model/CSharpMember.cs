namespace RiderLayout.Core.Model;

public sealed class CSharpMember
{
    public required MemberKind Kind { get; init; }
    public required string Name { get; init; }
    public Accessibility Access { get; init; }
    public bool IsStatic { get; init; }
    public bool IsReadonly { get; init; }
    public bool HasInitializer { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsOverride { get; init; }
    public bool IsConst { get; init; }
    public bool IsAssignedInConstructor { get; init; }
    public bool IsImplicitInterfaceImplementation { get; init; }
    public IReadOnlyList<string> Attributes { get; init; } = [];
    public bool IsExplicitInterfaceImplementation { get; init; }
    public int OriginalIndex { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
    public string SourceText { get; init; } = "";
}
