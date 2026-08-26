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

    [Fact]
    public void DetectsFieldsAssignedInConstructor()
    {
        const string source = """
        public class Demo
        {
            private IDependency _dep;
            private int _count;
            private static int _seed;

            public Demo(int dep, int fallback)
            {
                _dep = new Dependency(dep);
                var local = 0;
                _count = local;
                fallback = 1;
                _seed = 2;
            }
        }
        """;

        var parsed = new CSharpDocumentParser().ParseFirstClass(source);
        var byName = parsed.Members.ToDictionary(x => x.Name, x => x);

        Assert.True(byName["_dep"].IsAssignedInConstructor);
        Assert.True(byName["_count"].IsAssignedInConstructor);
        Assert.False(byName["_seed"].IsAssignedInConstructor, "static fields must be excluded");
    }

    [Fact]
    public void CtorAssignedFieldsRouteToDependencyRegionInAnyLayout()
    {
        const string source = """
        public class Demo
        {
            private IDependency _dep;
            private int _other;
            public Demo(IDependency dep) { _dep = dep; }
        }
        """;

        const string layoutXml = """
        <Patterns xmlns="urn:schemas-jetbrains-com:member-reordering-patterns">
            <TypePattern DisplayName="X" Priority="1">
                <Region Name="DEPENDENCIES">
                    <Entry DisplayName="Injected">
                        <Entry.Match>
                            <And>
                                <Kind Is="Field" />
                                <HasAttribute Name="InjectAttribute" />
                            </And>
                        </Entry.Match>
                    </Entry>
                </Region>
                <Region Name="FIELDS">
                    <Entry DisplayName="Fields">
                        <Entry.Match>
                            <Kind Is="Field" />
                        </Entry.Match>
                    </Entry>
                </Region>
            </TypePattern>
        </Patterns>
        """;

        var pattern = new RiderLayout.Rider.Xml.RiderLayoutXmlParser().Parse(layoutXml).TypePatterns[0];
        var groups = new RiderLayout.Core.Engine.LayoutEngine().ArrangeGroups(
            new CSharpDocumentParser().ParseFirstClass(source).Members, pattern);

        var dependencyGroup = groups.First(g => g.RegionName?.Equals("DEPENDENCIES", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains("_dep", dependencyGroup.Members.Select(x => x.Name));
        Assert.DoesNotContain("_other", dependencyGroup.Members.Select(x => x.Name));
        var fieldGroup = groups.First(g => g.RegionName?.Equals("FIELDS", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains("_other", fieldGroup.Members.Select(x => x.Name));
    }

    [Fact]
    public void DetectsImplicitInterfaceImplementationsFromBcl()
    {
        const string source = """
        using System;

        public class Demo : IDisposable
        {
            public void Dispose() { }
            public void Helper() { }
        }
        """;

        var parsed = new CSharpDocumentParser().ParseFirstClass(source);
        var byName = parsed.Members.ToDictionary(x => x.Name, x => x);

        Assert.True(byName["Dispose"].IsImplicitInterfaceImplementation);
        Assert.False(byName["Helper"].IsImplicitInterfaceImplementation, "non-interface public method must not match");
    }

    [Fact]
    public void DetectsImplicitInterfaceImplementationsFromSameFileInterface()
    {
        const string source = """
        public interface IGreeter { void Greet(); }

        public class Demo : IGreeter
        {
            public void Greet() { }
            public void Log() { }
        }
        """;

        var parsed = new CSharpDocumentParser().ParseFirstClass(source);
        var byName = parsed.Members.ToDictionary(x => x.Name, x => x);

        Assert.True(byName["Greet"].IsImplicitInterfaceImplementation);
        Assert.False(byName["Log"].IsImplicitInterfaceImplementation);
    }

    [Fact]
    public void DetectsImplicitImplementationOfInterfaceInAnotherFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "riderlayout_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        File.WriteAllText(Path.Combine(root, "IReply.cs"), """
        namespace Homa.Logic
        {
            public interface IReply
            {
                void Init(Request request);
                Task<GenericMessage> CreateGenericMessage();
                void SetupResponse(ref Response response);
                GenericMessage GenericMessage { get; }
            }

            public class Request { }
            public class GenericMessage { }
            public class Response { }
        }
        """);

        const string source = """
        namespace App
        {
            public class ReplyHandler : Homa.Logic.IReply
            {
                private Homa.Logic.Request request;

                public virtual void Init(Homa.Logic.Request request)
                {
                    this.request = request;
                }

                public void Other() { }
            }
        }
        """;

        try
        {
            var parsed = new CSharpDocumentParser().ParseFirstClass(source, root);
            var byName = parsed.Members.ToDictionary(x => x.Name, x => x);

            Assert.True(byName["Init"].IsImplicitInterfaceImplementation);
            Assert.False(byName["Other"].IsImplicitInterfaceImplementation);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}