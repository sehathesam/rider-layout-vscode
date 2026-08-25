using RiderLayout.Core.Matching;
using RiderLayout.Core.Model;
using Xunit;

namespace RiderLayout.Core.Tests;

public class SemanticMatcherTests
{
    private static CSharpMember Member(string name, bool isExplicit = false, string[]? attributes = null) => new()
    {
        Name = name,
        Kind = MemberKind.Method,
        IsExplicitInterfaceImplementation = isExplicit,
        Attributes = attributes ?? []
    };

    [Fact]
    public void AttributeShortNameMatchesFullTypeNames()
    {
        Assert.True(AttributeNames.Matches("Inject", "VContainer.InjectAttribute"));
        Assert.True(AttributeNames.Matches("InjectAttribute", "VContainer.InjectAttribute"));
        Assert.True(AttributeNames.Matches("SerializeField", "UnityEngine.SerializeFieldAttribute"));
        Assert.True(AttributeNames.Matches("UnityEngine.SerializeField", "UnityEngine.SerializeFieldAttribute"));
        Assert.False(AttributeNames.Matches("Inject", "UnityEngine.SerializeFieldAttribute"));
    }

    [Fact]
    public void SerializedFieldMatchesFieldWithSerializeFieldAttribute()
    {
        var expr = new SerializedFieldExpression();
        Assert.True(expr.Evaluate(new MatchContext(Member("speed", attributes: ["SerializeField"]))));
        Assert.False(expr.Evaluate(new MatchContext(Member("speed"))));
    }

    [Fact]
    public void UnityEventFunctionMatchesKnownLifecycleNames()
    {
        var expr = new UnityEventFunctionExpression();
        Assert.True(expr.Evaluate(new MatchContext(Member("Start"))));
        Assert.True(expr.Evaluate(new MatchContext(Member("LateUpdate"))));
        Assert.False(expr.Evaluate(new MatchContext(Member("UpdateScore"))));
    }

    [Fact]
    public void ExplicitInterfaceMatchesOnlyExplicitImplementations()
    {
        var expr = new ExplicitInterfaceExpression();
        Assert.True(expr.Evaluate(new MatchContext(Member("Execute", isExplicit: true))));
        Assert.False(expr.Evaluate(new MatchContext(Member("Execute", isExplicit: false))));
    }
}