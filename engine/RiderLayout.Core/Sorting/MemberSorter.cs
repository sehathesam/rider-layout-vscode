using RiderLayout.Core.Model;

namespace RiderLayout.Core.Sorting;

public static class MemberSorter
{
    public static IReadOnlyList<CSharpMember> Sort(IEnumerable<CSharpMember> members, IReadOnlyList<SortRule> rules)
    {
        var list = members.ToList();
        if (rules.Count == 0) return list;

        IOrderedEnumerable<CSharpMember>? ordered = null;
        foreach (var rule in rules)
        {
            var comparer = CreateComparer(rule);
            if (ordered is null)
                ordered = rule.Descending ? list.OrderByDescending(x => x, comparer) : list.OrderBy(x => x, comparer);
            else
                ordered = rule.Descending
                    ? ordered.ThenByDescending(x => x, comparer)
                    : ordered.ThenBy(x => x, comparer);
        }

        return ordered is null ? list : (IReadOnlyList<CSharpMember>)ordered.ToList();
    }

    private static IComparer<CSharpMember> CreateComparer(SortRule rule)
    {
        var key = rule.Key.ToLowerInvariant();
        var rank = RankFactory(key);
        var order = NormalizeOrder(rule.Order);

        return Comparer<CSharpMember>.Create((a, b) =>
        {
            var ra = rank(a);
            var rb = rank(b);

            int cmp;
            if (order.Count > 0)
            {
                var ia = order.TryGetValue(ra, out var iav) ? iav : int.MaxValue;
                var ib = order.TryGetValue(rb, out var ibv) ? ibv : int.MaxValue;
                cmp = ia.CompareTo(ib);
            }
            else
            {
                cmp = CompareKey(ra, rb);
            }

            if (cmp != 0) return cmp;
            return a.OriginalIndex.CompareTo(b.OriginalIndex);
        });
    }

    private static Func<CSharpMember, string> RankFactory(string key) => key switch
    {
        "name" => x => x.Name,
        "kind" => x => x.Kind.ToString(),
        "access" or "accessibility" => x => x.Access.ToString(),
        "static" => x => x.IsStatic ? "True" : "False",
        "readonly" => x => x.IsReadonly ? "True" : "False",
        "const" => x => x.IsConst ? "True" : "False",
        "virtual" => x => x.IsVirtual ? "True" : "False",
        "override" => x => x.IsOverride ? "True" : "False",
        _ => x => x.OriginalIndex.ToString()
    };

    private static Dictionary<string, int> NormalizeOrder(IReadOnlyList<string> order)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < order.Count; i++)
            map[order[i]] = i;
        return map;
    }

    private static int CompareKey(string a, string b)
    {
        var left = int.TryParse(a, out var ai) ? ai : (int?)null;
        var right = int.TryParse(b, out var bi) ? bi : (int?)null;
        if (left is not null || right is not null)
            return Nullable.Compare(left, right);
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}