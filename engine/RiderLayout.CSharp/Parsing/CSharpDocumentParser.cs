using System.Collections.Concurrent;
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
    public ParsedClass ParseFirstClass(string source, string? projectRoot = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var declaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? throw new InvalidOperationException("No class declaration found.");

        var assignedFields = DetectConstructorAssignedFields(declaration);
        var referenceTrees = projectRoot is null ? Array.Empty<Roslyn::SyntaxTree>() : ProjectTrees(projectRoot);
        var implicitImpls = DetectImplicitInterfaceImplementations(declaration, referenceTrees);
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
    /// Cached references from the current shared framework. Loading the runtime
    /// assemblies once lets the semantic model resolve interfaces such as
    /// System.IDisposable when deciding whether a public member is an implicit
    /// interface implementation.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<Roslyn::MetadataReference>> SharedReferences = new(() =>
    {
        var refs = new List<Roslyn::MetadataReference>();
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (string.IsNullOrEmpty(runtimeDir)) return refs;

        foreach (var dll in Directory.EnumerateFiles(runtimeDir, "*.dll"))
        {
            try { refs.Add(Roslyn::MetadataReference.CreateFromFile(dll)); }
            catch { /* skip unreadable assemblies */ }
        }
        return refs;
    });

    /// <summary>
    /// Cache of parsed source trees per project root, so repeated rearranges of
    /// files in the same project do not re-parse the whole tree each time. The
    /// CLI stays alive for the lifetime of the editor session, so this is a real
    /// win; staleness only matters if sources change mid-session.
    /// </summary>
    private static readonly ConcurrentDictionary<string, IReadOnlyList<Roslyn::SyntaxTree>> ProjectTreeCache = new(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<Roslyn::SyntaxTree> ProjectTrees(string projectRoot)
    {
        var key = Path.GetFullPath(projectRoot);
        return ProjectTreeCache.GetOrAdd(key, static root =>
        {
            var trees = new List<Roslyn::SyntaxTree>();
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    var lower = file.ToLowerInvariant();
                    if (lower.IndexOf("\\obj\\", StringComparison.Ordinal) >= 0
                        || lower.IndexOf("\\bin\\", StringComparison.Ordinal) >= 0
                        || lower.IndexOf("\\node_modules\\", StringComparison.Ordinal) >= 0
                        || lower.IndexOf("\\.git\\", StringComparison.Ordinal) >= 0)
                        continue;
                    try { trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)); }
                    catch { /* skip unreadable files */ }
                }
            }
            catch
            {
                // cannot enumerate the project root; fall back to the single file
            }
            return trees;
        });
    }

    /// <summary>
    /// Returns the names of members that implement an interface the class
    /// declares (implicitly). Uses a semantic model fed by both the project's
    /// own source trees and the shared framework assemblies, so interfaces in
    /// other namespaces/files (e.g. a Homa.Logic.IReply implemented in this
    /// class) and BCL interfaces (e.g. IDisposable) both resolve.
    /// Fail-closed: any compilation trouble means we simply flag nothing.
    /// </summary>
    private static HashSet<string> DetectImplicitInterfaceImplementations(
        ClassDeclarationSyntax declaration,
        IReadOnlyList<Roslyn::SyntaxTree> referenceTrees)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var tree = declaration.SyntaxTree;
            var trees = new Roslyn::SyntaxTree[referenceTrees.Count + 1];
            trees[0] = tree;
            for (var i = 0; i < referenceTrees.Count; i++) trees[i + 1] = referenceTrees[i];

            var compilation = CSharpCompilation.Create(
                "RiderLayout",
                trees,
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