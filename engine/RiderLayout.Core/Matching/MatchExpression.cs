using RiderLayout.Core.Model;

namespace RiderLayout.Core.Matching;

public sealed record MatchContext(CSharpMember Member);

public abstract class MatchExpression
{
    public abstract bool Evaluate(MatchContext context);
}

public sealed class AndExpression : MatchExpression
{
    public List<MatchExpression> Children { get; } = [];
    public override bool Evaluate(MatchContext context) => Children.All(x => x.Evaluate(context));
}

public sealed class OrExpression : MatchExpression
{
    public List<MatchExpression> Children { get; } = [];
    public override bool Evaluate(MatchContext context) => Children.Any(x => x.Evaluate(context));
}

public sealed class NotExpression(MatchExpression child) : MatchExpression
{
    public MatchExpression Child { get; } = child;
    public override bool Evaluate(MatchContext context) => !Child.Evaluate(context);
}

public sealed class KindExpression(MemberKind kind) : MatchExpression
{
    public MemberKind Kind { get; } = kind;
    public override bool Evaluate(MatchContext context) => context.Member.Kind == Kind;
}

public sealed class AccessExpression(Accessibility access) : MatchExpression
{
    public Accessibility Access { get; } = access;
    public override bool Evaluate(MatchContext context) => context.Member.Access == Access;
}

public sealed class NameExpression(string pattern) : MatchExpression
{
    private readonly System.Text.RegularExpressions.Regex _regex =
        new(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

    public string Pattern { get; } = pattern;
    public override bool Evaluate(MatchContext context) => _regex.IsMatch(context.Member.Name);
}

public enum ModifierKind { Static, Readonly, Abstract, Virtual, Override, Const }

public sealed class ModifierExpression(ModifierKind modifier, bool expected = true) : MatchExpression
{
    public ModifierKind Modifier { get; } = modifier;
    public bool Expected { get; } = expected;

    public override bool Evaluate(MatchContext context)
    {
        var value = Modifier switch
        {
            ModifierKind.Static => context.Member.IsStatic,
            ModifierKind.Readonly => context.Member.IsReadonly,
            ModifierKind.Abstract => context.Member.IsAbstract,
            ModifierKind.Virtual => context.Member.IsVirtual,
            ModifierKind.Override => context.Member.IsOverride,
            ModifierKind.Const => context.Member.IsConst,
            _ => false
        };
        return value == Expected;
    }
}

public sealed class AttributeExpression(string name) : MatchExpression
{
    public string Name { get; } = name;
    public override bool Evaluate(MatchContext context) =>
        context.Member.Attributes.Any(a => AttributeNames.Matches(a, Name));
}

/// <summary>
/// Matches C# attributes by short name without a semantic model. The XML layout
/// usually refers to the full attribute type (e.g. VContainer.InjectAttribute)
/// while the source uses the short name (e.g. [Inject] or [InjectAttribute]).
/// Both are normalized to their last-segment, "Attribute"-suffix-stripped form.
/// </summary>
public static class AttributeNames
{
    public static bool Matches(string attribute, string expected)
    {
        var attrShort = Normalize(attribute);
        var expectedShort = Normalize(expected);
        return attrShort.Length > 0 && attrShort == expectedShort;
    }

    private static string Normalize(string name)
    {
        var segment = name.Contains('.') ? name[(name.LastIndexOf('.') + 1)..] : name;
        if (segment.EndsWith("Attribute", StringComparison.Ordinal))
            segment = segment[..^"Attribute".Length];
        return segment;
    }
}

public sealed class SerializedFieldExpression : MatchExpression
{
    public override bool Evaluate(MatchContext context) =>
        context.Member.Attributes.Any(a => AttributeNames.Matches(a, "SerializeField"));
}

public sealed class UnityEventFunctionExpression : MatchExpression
{
    private static readonly HashSet<string> EventFunctions = new(StringComparer.Ordinal)
    {
        "Awake", "OnEnable", "Start", "Update", "FixedUpdate", "LateUpdate", "OnGUI",
        "OnDisable", "OnDestroy", "Reset", "OnValidate", "OnApplicationFocus",
        "OnApplicationPause", "OnApplicationQuit", "OnTriggerEnter", "OnTriggerStay",
        "OnTriggerExit", "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
        "OnCollisionEnter2D", "OnCollisionStay2D", "OnCollisionExit2D",
        "OnTriggerEnter2D", "OnTriggerStay2D", "OnTriggerExit2D",
        "OnMouseDown", "OnMouseUp", "OnMouseDrag", "OnMouseEnter", "OnMouseExit",
        "OnMouseOver", "OnBecameVisible", "OnBecameInvisible", "OnDrawGizmos"
    };

    public override bool Evaluate(MatchContext context) => EventFunctions.Contains(context.Member.Name);
}

public sealed class ExplicitInterfaceExpression : MatchExpression
{
    public override bool Evaluate(MatchContext context) =>
        context.Member.IsExplicitInterfaceImplementation || context.Member.IsImplicitInterfaceImplementation;
}
