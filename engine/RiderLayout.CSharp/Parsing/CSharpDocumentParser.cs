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
    public ParsedClass ParseFirstClass(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var declaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()
            ?? throw new InvalidOperationException("No class declaration found.");

        var members = declaration.Members.Select((member, index) => ToMember(member, index)).ToList();
        return new ParsedClass { Declaration = declaration, Members = members };
    }

    public ClassDeclarationSyntax ReplaceMembers(ParsedClass parsed, IReadOnlyList<CSharpMember> ordered)
    {
        var nodes = ordered.Select(x => FindMember(parsed.Declaration, x.OriginalIndex)).ToList();
        return parsed.Declaration.WithMembers(SyntaxFactory.List(nodes));
    }

    private static MemberDeclarationSyntax FindMember(ClassDeclarationSyntax declaration, int originalIndex)
        => declaration.Members[originalIndex];

    private static CSharpMember ToMember(MemberDeclarationSyntax node, int index)
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
            IsAbstract = node.Modifiers.Any(x => x.Kind() == SyntaxKind.AbstractKeyword),
            IsVirtual = node.Modifiers.Any(x => x.Kind() == SyntaxKind.VirtualKeyword),
            IsOverride = node.Modifiers.Any(x => x.Kind() == SyntaxKind.OverrideKeyword),
            IsConst = node.Modifiers.Any(x => x.Kind() == SyntaxKind.ConstKeyword),
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
