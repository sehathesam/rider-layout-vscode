using RiderLayout.CSharp.Parsing;
using RiderLayout.Core.Model;
using Xunit;

namespace RiderLayout.CSharp.Tests;

public class CSharpDocumentParserTests
{
    [Fact]
    public void ClassifiesNewKindsCorrectly()
    {
        const string source = """
        public class Demo
        {
            public const int Max = 1;
            private int _field;
            public int this[int i] => i;
            ~Demo() { }
            public static Demo operator +(Demo a, Demo b) => a;
            public event System.Action<int> Other;
            public event System.Action Changed { add { } remove { } }
        }
        """;

        var parsed = new CSharpDocumentParser().ParseFirstClass(source);
        var byName = parsed.Members.ToDictionary(x => x.Name, x => x.Kind);

        Assert.Equal(MemberKind.Constant, byName["Max"]);
        Assert.Equal(MemberKind.Field, byName["_field"]);
        Assert.Equal(MemberKind.Indexer, byName["this"]);
        Assert.Equal(MemberKind.Destructor, parsed.Members.First(x => x.Kind == MemberKind.Destructor).Kind);
        Assert.Equal(MemberKind.Operator, parsed.Members.First(x => x.Kind == MemberKind.Operator).Kind);
        Assert.Equal(MemberKind.Event, byName["Other"]);
        Assert.Equal(MemberKind.Event, byName["Changed"]);
    }

    [Fact]
    public void UnmodifiedClassMembersDefaultToPrivate()
    {
        const string source = """
        public class Demo
        {
            int _hidden;
            void Work() { }
        }
        """;

        var parsed = new CSharpDocumentParser().ParseFirstClass(source);
        Assert.All(parsed.Members, m => Assert.Equal(Accessibility.Private, m.Access));
    }
}