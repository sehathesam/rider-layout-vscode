using Roslyn = Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RiderLayout.Core.Model;

namespace RiderLayout.CSharp.Parsing;

public sealed class ParsedClass
{
    public required ClassDeclarationSyntax Declaration { get; init; }
    public required List<CSharpMember> Members { get; init; }
}

public sealed class CSharpDocumentParser
{
    /// <summary>
    /// Parses the first class declaration found in the source. Semantic analysis
    /// (implicit interface detection) is only run when the caller's layout
    /// actually uses the ImplementsInterface matcher; otherwise it is skipped so
    /// a long-lived CLI never pays for it. The check is deliberately limited to
    /// the current file plus the shared framework references: scanning the whole
    /// project tree pinned hundreds of MB in a persistent process.
    /// </summary>
    public ParsedClass ParseFirstClass(string source, bool analyzeInterfaces = true)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var declaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? throw new InvalidOperationException("No class declaration found.");

        var assignedFields = DetectConstructorAssignedFields(declaration);
        var implicitImpls = analyzeInterfaces
            ? DetectImplicitInterfaceImplementations(declaration)
            : new HashSet<string>(StringComparer.Ordinal);
        var members = declaration.Members.Select((member, index) => ToMember(member, index, assignedFields, implicitImpls)).ToList();
        return new ParsedClass { Declaration = declaration, Members = members };
    }

    public ClassDeclarationSyntax ReplaceMembers(ParsedClass parsed, IReadOnlyList<CSharpMember> ordered)
    {
        var nodes = ordered.Select(x => FindMember(parsed.Declaration, x.OriginalIndex)).ToList();
        return parsed.Declaration.WithMembers(SyntaxFactory.List(nodes));
    }

    /// <summary>
    /// Collects the names of instance fields that are assigned somewhere inside
    /// an instance constructor of the same class. This is a purely syntactic
    /// analysis: an assignment is treated as targeting a field when the target
    /// is either a bare identifier that is neither a constructor parameter nor
    /// a local variable declared in the body, or a "this." member access naming
    /// an actual instance field.
    /// </summary>
    private static HashSet<string> DetectConstructorAssignedFields(ClassDeclarationSyntax declaration)
    {
        var instanceFieldNames = declaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Where(f => !f.Modifiers.Any(x => x.Kind() == SyntaxKind.StaticKeyword))
            .SelectMany(f => f.Declaration.Variables)
            .Select(v => v.Identifier.Text)
            .ToHashSet(StringComparer.Ordinal);

        var assigned = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ctor in declaration.Members.OfType<ConstructorDeclarationSyntax>())
        {
            if (ctor.Modifiers.Any(x => x.Kind() == SyntaxKind.StaticKeyword)) continue;

            IEnumerable<AssignmentExpressionSyntax> assignments;
            if (ctor.Body is not null)
            {
                assignments = ctor.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>();
            }
            else if (ctor.ExpressionBody is not null)
            {
                assignments = ctor.ExpressionBody.Expression.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>();
            }
            else
            {
                continue;
            }

            var parameters = ctor.ParameterList.Parameters.Select(p => p.Identifier.Text).ToHashSet(StringComparer.Ordinal);
            var locals = ctor.Body is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : ctor.Body.DescendantNodes()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .SelectMany(l => l.Declaration.Variables)
                    .Select(v => v.Identifier.Text)
                    .ToHashSet(StringComparer.Ordinal);

            foreach (var assignment in assignments)
            {
                if (assignment.OperatorToken.Kind() != SyntaxKind.EqualsToken) continue;
                var candidate = ResolveFieldTarget(assignment.Left, parameters, locals, instanceFieldNames);
                if (candidate is not null) assigned.Add(candidate);
            }
        }

        return assigned;
    }

private static string? ResolveFieldTarget(
        ExpressionSyntax left,
        HashSet<string> parameters,
        HashSet<string> locals,
        HashSet<string> instanceFieldNames)
    {
        switch (left)
        {
            case IdentifierNameSyntax identifier:
                var name = identifier.Identifier.Text;
                if (parameters.Contains(name) || locals.Contains(name)) return null;
                return instanceFieldNames.Contains(name) ? name : null;
            case MemberAccessExpressionSyntax member when member.Expression is ThisExpressionSyntax:
                var field = member.Name.Identifier.Text;
                return instanceFieldNames.Contains(field) ? field : null;
            default:
                return null;
        }
    }

    private static MemberDeclarationSyntax FindMember(ClassDeclarationSyntax declaration, int originalIndex)
        => declaration.Members[originalIndex];

    /// <summary>
    /// Cached references from the current shared framework. Only the platform
    /// modules that actually define commonly implemented interfaces are loaded
    /// (the full runtime directory holds hundreds of DLLs and pinned hundreds of
    /// MB in the long-lived CLI). The semantic model can still resolve BCL
    /// interfaces such as System.IDisposable.
    /// </summary>
    private static readonly HashSet<string> PlatformModuleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Standard .NET distribution: the core runtime module that carries Object,
        // IDisposable, IComparable, Iterator and the rest of the BCL interfaces.
        "System.Private.CoreLib.dll",
        // Modular/CSP-style runtimes expose the same interfaces via flat modules.
        "System.dll", "IO.dll", "Base.dll", "Collections.dll", "Concurrency.dll",
        "ModuleSystem.dll", "Reflection.dll", "Serialization.dll", "Networking.dll",
        "Time.dll", "Misc.dll", "Utilities.dll", "Process.dll", "Telemetry.dll",
        // Standard-distribution facades that may still declare implementable types.
        "System.Collections.dll", "System.Collections.Concurrent.dll",
        "System.Collections.Immutable.dll", "System.IO.dll", "System.Linq.dll",
        "System.Net.dll", "System.Runtime.dll", "System.Security.dll",
        "System.Threading.dll", "System.ObjectModel.dll"
    };

    private static readonly Lazy<IReadOnlyList<Roslyn::MetadataReference>> SharedReferences = new(() =>
    {
        var refs = new List<Roslyn::MetadataReference>();
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (string.IsNullOrEmpty(runtimeDir)) return refs;

        foreach (var dll in Directory.EnumerateFiles(runtimeDir, "*.dll"))
        {
            if (!PlatformModuleNames.Contains(Path.GetFileName(dll))) continue;
            try { refs.Add(Roslyn::MetadataReference.CreateFromFile(dll)); }
            catch { /* skip unreadable assemblies */ }
        }
        return refs;
    });

    /// <summary>
    /// Returns the names of members that implement an interface the class
    /// declares (implicitly). The model is built from the current file plus the
    /// allowlisted framework references, so same-file interfaces and BCL
    /// interfaces (e.g. IDisposable) resolve. Interfaces declared in other
    /// project files no longer resolve: scanning the whole tree pinned hundreds
    /// of MB in the persistent CLI. Fail-closed: any compilation trouble means
    /// we simply flag nothing.
    /// </summary>
    private static HashSet<string> DetectImplicitInterfaceImplementations(ClassDeclarationSyntax declaration)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var tree = declaration.SyntaxTree;
            var compilation = CSharpCompilation.Create(
                "RiderLayout",
                [tree],
                SharedReferences.Value,
                new CSharpCompilationOptions(Roslyn::OutputKind.DynamicallyLinkedLibrary));
            var model = compilation.GetSemanticModel(tree);
            var typeSymbol = model.GetDeclaredSymbol(declaration);
            if (typeSymbol is null) return result;

            var interfaces = typeSymbol.AllInterfaces
                .SelectMany(i => i.GetMembers())
                .ToArray();

            foreach (var member in declaration.Members)
            {
                var symbol = model.GetDeclaredSymbol(member);
                if (symbol is null) continue;
                var implements = interfaces.Any(im =>
                    Roslyn::SymbolEqualityComparer.Default.Equals(
                        typeSymbol.FindImplementationForInterfaceMember(im), symbol));
                if (implements) result.Add(GetName(member));
            }
        }
        catch
        {
            // ignore: no implicit interface detection when the model cannot be built
        }
        return result;
    }

    private static CSharpMember ToMember(MemberDeclarationSyntax node, int index, HashSet<string> assignedFields, HashSet<string> implicitImpls)
    {
        var attrs = node.AttributeLists
            .SelectMany(x => x.Attributes)
            .Select(x => x.Name.ToString())
            .ToArray();

        return new CSharpMember
        {
            Kind = GetKind(node),
            Name = GetName(node),
            Access = GetAccess(node),
            IsStatic = node.Modifiers.Any(x => x.Kind() == SyntaxKind.StaticKeyword),
            IsReadonly = node.Modifiers.Any(x => x.Kind() == SyntaxKind.ReadOnlyKeyword),
            HasInitializer = node is FieldDeclarationSyntax fieldDecl
                && fieldDecl.Declaration.Variables.Any(v => v.Initializer is not null),
            IsAbstract = node.Modifiers.Any(x => x.Kind() == SyntaxKind.AbstractKeyword),
            IsVirtual = node.Modifiers.Any(x => x.Kind() == SyntaxKind.VirtualKeyword),
            IsOverride = node.Modifiers.Any(x => x.Kind() == SyntaxKind.OverrideKeyword),
            IsConst = node.Modifiers.Any(x => x.Kind() == SyntaxKind.ConstKeyword),
            IsAssignedInConstructor = node is FieldDeclarationSyntax field
                && !node.Modifiers.Any(x => x.Kind() == SyntaxKind.StaticKeyword)
                && field.Declaration.Variables.Any(v => assignedFields.Contains(v.Identifier.Text)),
            IsImplicitInterfaceImplementation = !HasExplicitInterfaceSpecifier(node)
                && implicitImpls.Contains(GetName(node)),
            Attributes = attrs,
            IsExplicitInterfaceImplementation = HasExplicitInterfaceSpecifier(node),
            OriginalIndex = index,
            Start = node.FullSpan.Start,
            Length = node.FullSpan.Length,
            SourceText = node.ToFullString()
        };
    }

    private static bool HasExplicitInterfaceSpecifier(MemberDeclarationSyntax node) => node switch
    {
        MethodDeclarationSyntax m => m.ExplicitInterfaceSpecifier is not null,
        PropertyDeclarationSyntax p => p.ExplicitInterfaceSpecifier is not null,
        EventDeclarationSyntax e => e.ExplicitInterfaceSpecifier is not null,
        IndexerDeclarationSyntax i => i.ExplicitInterfaceSpecifier is not null,
        _ => false
    };

    private static MemberKind GetKind(MemberDeclarationSyntax node) => node switch
    {
        FieldDeclarationSyntax => node.Modifiers.Any(x => x.Kind() == SyntaxKind.ConstKeyword)
            ? MemberKind.Constant : MemberKind.Field,
        PropertyDeclarationSyntax => MemberKind.Property,
        ConstructorDeclarationSyntax => MemberKind.Constructor,
        DestructorDeclarationSyntax => MemberKind.Destructor,
        MethodDeclarationSyntax => MemberKind.Method,
        EventFieldDeclarationSyntax => MemberKind.Event,
        EventDeclarationSyntax => MemberKind.Event,
        DelegateDeclarationSyntax => MemberKind.Delegate,
        IndexerDeclarationSyntax => MemberKind.Indexer,
        OperatorDeclarationSyntax => MemberKind.Operator,
        ClassDeclarationSyntax => MemberKind.Class,
        StructDeclarationSyntax => MemberKind.Struct,
        InterfaceDeclarationSyntax => MemberKind.Interface,
        EnumDeclarationSyntax => MemberKind.Enum,
        _ => MemberKind.Unknown
    };

    private static string GetName(MemberDeclarationSyntax node) => node switch
    {
        BaseTypeDeclarationSyntax x => x.Identifier.Text,
        PropertyDeclarationSyntax x => x.Identifier.Text,
        MethodDeclarationSyntax x => x.Identifier.Text,
        BaseFieldDeclarationSyntax x => x.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "",
        ConstructorDeclarationSyntax x => x.Identifier.Text,
        DestructorDeclarationSyntax x => x.Identifier.Text,
        EventDeclarationSyntax x => x.Identifier.Text,
        IndexerDeclarationSyntax x => x.ThisKeyword.Text,
        OperatorDeclarationSyntax x => x.OperatorToken.Text,
        DelegateDeclarationSyntax x => x.Identifier.Text,
        _ => ""
    };

    private static Accessibility GetAccess(MemberDeclarationSyntax node)
    {
        foreach (var modifier in node.Modifiers)
        {
            if (modifier.Kind() == SyntaxKind.PublicKeyword) return Accessibility.Public;
            if (modifier.Kind() == SyntaxKind.ProtectedKeyword)
            {
                if (node.Modifiers.Any(x => x.Kind() == SyntaxKind.InternalKeyword)) return Accessibility.ProtectedInternal;
                return Accessibility.Protected;
            }
            if (modifier.Kind() == SyntaxKind.InternalKeyword)
            {
                if (node.Modifiers.Any(x => x.Kind() == SyntaxKind.ProtectedKeyword)) return Accessibility.ProtectedInternal;
                return Accessibility.Internal;
            }
            if (modifier.Kind() == SyntaxKind.PrivateKeyword)
            {
                if (node.Modifiers.Any(x => x.Kind() == SyntaxKind.ProtectedKeyword)) return Accessibility.PrivateProtected;
                return Accessibility.Private;
            }
        }

        // No explicit modifier: class members default to private, interface
        // members default to public. Destructors/indexers cannot be modified.
        if (node is DestructorDeclarationSyntax or IndexerDeclarationSyntax)
            return Accessibility.None;
        if (node.Ancestors().Any(x => x is InterfaceDeclarationSyntax)) return Accessibility.Public;
        return Accessibility.Private;
    }
}