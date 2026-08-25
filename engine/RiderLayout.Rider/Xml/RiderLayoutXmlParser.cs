using System.Xml.Linq;
using RiderLayout.Core.Matching;
using RiderLayout.Core.Model;

namespace RiderLayout.Rider.Xml;

public sealed class RiderLayoutXmlParser
{
    private const string PatternsNamespace = "urn:schemas-jetbrains-com:member-reordering-patterns";

    public LayoutPattern Parse(string xml)
    {
        xml = NormalizeWhitespace(xml);
        var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = doc.Root ?? throw new InvalidOperationException("Layout XML has no root element.");
        var result = new LayoutPattern();

        // Rider layouts can use several namespaces (e.g. the unity vocabulary).
        // Declare the common ones so prefixed-but-undeclared elements do not fail.
        if (root.GetNamespaceOfPrefix("unity") is null && root.Name.Namespace != XNamespace.None)
            root.Add(new XAttribute(XNamespace.Xmlns + "unity", "urn:schemas-jetbrains-com:member-reordering-patterns-unity"));

        foreach (var element in root.Elements())
        {
            switch (Local(element))
            {
                case "TypePattern":
                    result.TypePatterns.Add(ParseTypePattern(element));
                    break;
                case "Entry":
                    result.FileNodes.Add(ParseEntry(element));
                    break;
                case "Region":
                    result.FileNodes.Add(ParseRegion(element));
                    break;
            }
        }

        return result;
    }

    private static TypePattern ParseTypePattern(XElement element)
    {
        var node = new TypePattern
        {
            DisplayName = (string?)element.Attribute("DisplayName"),
            Priority = ParseInt(element, "Priority") ?? 0,
            Match = ParseMatch(element.Element(element.Name.Namespace + "TypePattern.Match"))
        };

        foreach (var child in element.Elements())
        {
            var local = Local(child);
            if (local == "Entry") node.Children.Add(ParseEntry(child));
            else if (local == "Region") node.Children.Add(ParseRegion(child));
        }

        return node;
    }

    private static EntryNode ParseEntry(XElement element)
    {
        var node = new EntryNode
        {
            DisplayName = (string?)element.Attribute("DisplayName"),
            Priority = ParseInt(element, "Priority") ?? 0,
            Match = ParseMatch(element.Elements().FirstOrDefault(x => Local(x) == "Entry.Match"))
        };

        var sort = element.Elements().FirstOrDefault(x => Local(x) == "Entry.SortBy");
        if (sort is not null)
        {
            foreach (var child in sort.Elements())
            {
                var key = Local(child);
                if (key is not null)
                {
                    var (order, descending) = ReadSortOrder(child);
                    node.SortBy.Add(new SortRule { Key = key, Order = order, Descending = descending });
                }
            }
        }

        return node;
    }

    private static (List<string> Order, bool Descending) ReadSortOrder(XElement element)
    {
        var order = new List<string>();
        var value = (string?)element.Attribute("Order");
        var descending = ParseBoolAttribute(element, "Descending", false);

        if (!string.IsNullOrWhiteSpace(value))
        {
            var parts = value.Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[^1].Equals("Descending", StringComparison.OrdinalIgnoreCase))
            {
                descending = true;
                parts = parts[..^1];
            }
            order.AddRange(parts);
        }

        return (order, descending);
    }

    private static RegionNode ParseRegion(XElement element)
    {
        var node = new RegionNode
        {
            Name = (string?)element.Attribute("Name") ?? "",
            Priority = ParseInt(element, "Priority") ?? 0,
            GroupBy = ParseMatch(element.Elements().FirstOrDefault(x => Local(x) == "Region.GroupBy"))
        };

        foreach (var child in element.Elements())
            if (Local(child) == "Entry") node.Children.Add(ParseEntry(child));

        return node;
    }

    private static MatchExpression? ParseMatch(XElement? container)
    {
        if (container is null) return null;
        var child = container.Elements().FirstOrDefault();
        return child is null ? null : ParseExpression(child);
    }

    private static MatchExpression ParseExpression(XElement element)
    {
        switch (Local(element))
        {
            case "And":
            {
                var node = new AndExpression();
                foreach (var child in element.Elements()) node.Children.Add(ParseExpression(child));
                return node;
            }
            case "Or":
            {
                var node = new OrExpression();
                foreach (var child in element.Elements()) node.Children.Add(ParseExpression(child));
                return node;
            }
            case "Not":
            {
                var child = element.Elements().FirstOrDefault() ?? throw new InvalidOperationException("Not requires a child matcher.");
                return new NotExpression(ParseExpression(child));
            }
            case "Kind":
                return new KindExpression(ParseKind((string?)element.Attribute("Is")));
            case "Access":
                return new AccessExpression(ParseAccess((string?)element.Attribute("Is")));
            case "Name":
                return new NameExpression((string?)element.Attribute("Is") ?? ".*");
            case "Static":
                return new ModifierExpression(ModifierKind.Static, ParseBoolAttribute(element, "Is", true));
            case "Readonly":
                return new ModifierExpression(ModifierKind.Readonly, ParseBoolAttribute(element, "Is", true));
            case "Abstract":
                return new ModifierExpression(ModifierKind.Abstract, ParseBoolAttribute(element, "Is", true));
            case "Virtual":
                return new ModifierExpression(ModifierKind.Virtual, ParseBoolAttribute(element, "Is", true));
            case "Override":
                return new ModifierExpression(ModifierKind.Override, ParseBoolAttribute(element, "Is", true));
            case "Constant":
            case "Const":
                return new ModifierExpression(ModifierKind.Const, ParseBoolAttribute(element, "Is", true));
            case "HasAttribute":
                return new AttributeExpression((string?)element.Attribute("Name") ?? (string?)element.Attribute("Is") ?? "");
            case "SerializedField":
                return new SerializedFieldExpression();
            case "EventFunction":
                return new UnityEventFunctionExpression();
            case "ImplementsInterface":
                return new ExplicitInterfaceExpression();
            case "HandlesEvent":
                // Requires event subscription data; not modeled in the MVP.
                return new UnsupportedExpression(Local(element));
            default:
                // Unknown matchers are deliberately fail-closed rather than crashing the whole layout.
                return new UnsupportedExpression(Local(element));
        }
    }

    private static MemberKind ParseKind(string? value) => value?.ToLowerInvariant() switch
    {
        "field" => MemberKind.Field,
        "constant" => MemberKind.Constant,
        "property" => MemberKind.Property,
        "constructor" => MemberKind.Constructor,
        "destructor" => MemberKind.Destructor,
        "method" => MemberKind.Method,
        "event" => MemberKind.Event,
        "delegate" => MemberKind.Delegate,
        "indexer" => MemberKind.Indexer,
        "operator" => MemberKind.Operator,
        "class" => MemberKind.Class,
        "struct" => MemberKind.Struct,
        "interface" => MemberKind.Interface,
        "enum" => MemberKind.Enum,
        "record" => MemberKind.Record,
        _ => MemberKind.Unknown
    };

    private static Accessibility ParseAccess(string? value) => value?.ToLowerInvariant() switch
    {
        "public" => Accessibility.Public,
        "protected" => Accessibility.Protected,
        "internal" => Accessibility.Internal,
        "private" => Accessibility.Private,
        "protectedinternal" or "protected internal" => Accessibility.ProtectedInternal,
        "privateprotected" or "private protected" => Accessibility.PrivateProtected,
        _ => Accessibility.None
    };

    private static bool ParseBoolAttribute(XElement element, string name, bool defaultValue)
        => bool.TryParse((string?)element.Attribute(name), out var value) ? value : defaultValue;

    private static int? ParseInt(XElement element, string name)
        => int.TryParse((string?)element.Attribute(name), out var value) ? value : null;

    private static string Local(XElement element) => element.Name.LocalName;

    private static string NormalizeWhitespace(string xml)
    {
        // NBSP is frequently pasted into hand-written or exported layouts but is
        // not valid XML whitespace. Normalize it to a plain space so the document
        // parses while preserving the rest of the source exactly.
        return xml.Replace('\u00A0', ' ');
    }

    private sealed class UnsupportedExpression(string matcherName) : MatchExpression
    {
        public string MatcherName { get; } = matcherName;
        public override bool Evaluate(MatchContext context) => false;
    }
}
