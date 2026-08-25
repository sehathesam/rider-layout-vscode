using RiderLayout.Core.Matching;
using RiderLayout.Core.Model;
using RiderLayout.Core.Sorting;

namespace RiderLayout.Core.Engine;

public sealed class LayoutEngine
{
    private readonly record struct Slot(EntryNode Entry, int EffectivePriority, int DeclarationIndex);

    private const int CatchAllPriority = int.MinValue;

    public IReadOnlyList<CSharpMember> Arrange(
        IReadOnlyList<CSharpMember> members,
        TypePattern pattern)
    {
        var slots = BuildSlots(pattern.Children);
        var buckets = new Dictionary<Slot, List<CSharpMember>>();
        var unmatched = new List<CSharpMember>();

        foreach (var member in members)
        {
            var winner = PickSlot(member, slots);
            if (winner is null)
            {
                unmatched.Add(member);
                continue;
            }

            if (!buckets.TryGetValue(winner.Value, out var bucket))
                buckets[winner.Value] = bucket = [];
            bucket.Add(member);
        }

        var result = new List<CSharpMember>();
        foreach (var slot in slots)
        {
            if (!buckets.TryGetValue(slot, out var bucket)) continue;
            result.AddRange(MemberSorter.Sort(bucket, slot.Entry.SortBy));
        }

        result.AddRange(unmatched.OrderBy(x => x.OriginalIndex));
        return result;
    }

    /// <summary>
    /// Flattens the type pattern into a single ordered list of entry slots.
    /// A region contributes its priority to every entry it contains, so entries
    /// inside a high-priority region (e.g. RPC METHODS) win over entries in
    /// default-priority regions for the same member.
    /// </summary>
    private static List<Slot> BuildSlots(IReadOnlyList<LayoutNode> children)
    {
        var slots = new List<Slot>();
        var index = 0;

        void Walk(IEnumerable<LayoutNode> nodes, int inheritedPriority)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case EntryNode entry:
                        // An entry without a match clause is a catch-all: it must
                        // never outcompete an explicit matcher regardless of where
                        // it appears in the document.
                        var priority = entry.Match is null
                            ? CatchAllPriority
                            : inheritedPriority + entry.Priority;
                        slots.Add(new Slot(entry, priority, index++));
                        break;
                    case RegionNode region:
                        Walk(region.Children, inheritedPriority + region.Priority);
                        break;
                }
            }
        }

        Walk(children, 0);
        return slots;
    }

    private static Slot? PickSlot(CSharpMember member, IReadOnlyList<Slot> slots)
    {
        Slot? best = null;
        foreach (var slot in slots)
        {
            if (slot.Entry.Match is not null && !slot.Entry.Match.Evaluate(new MatchContext(member)))
                continue;

            if (best is null ||
                slot.EffectivePriority > best.Value.EffectivePriority ||
                (slot.EffectivePriority == best.Value.EffectivePriority &&
                 slot.DeclarationIndex < best.Value.DeclarationIndex))
            {
                best = slot;
            }
        }

        return best;
    }
}