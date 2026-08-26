# Architecture

## Separation of concerns

`RiderLayout.Core` contains no Rider XML and no VS Code dependencies. It consumes normalized layout objects and normalized C# member objects.

`RiderLayout.Rider` knows how Rider stores and serializes layout patterns. It owns the XML parser, the `unity` namespace, and NBSP/encoding normalizations needed for real-world Rider files.

`RiderLayout.CSharp` knows Roslyn. It converts syntax nodes into `CSharpMember` objects and rewrites the selected type by splicing source text spans, preserving the file's surrounding formatting.

`RiderLayout.Cli` is deliberately boring: JSON request in, JSON response out. It is a long-lived process (one line in, one JSON line out, correlated by an `id`), which keeps Roslyn warm across requests and isolates the extension from Roslyn runtime details.

## Data flow

```text
VS Code (TypeScript)
        │  JSON-lines (id-correlated)
        ▼
RiderLayout.Cli
        │
        ▼
RiderLayout.Rider  ──► LayoutPattern (engine-neutral model)
RiderLayout.Core   ──► ordered CSharpMember list
RiderLayout.CSharp ──► rewritten source
```

## The extent of the layout model

The engine flattens a `TypePattern` into an ordered list of "slots":

- Each `Region` contributes its `Priority` to the entries it contains, so a member matching an entry inside a high-priority region (e.g. `RPC METHODS`) beats a matching entry in a default-priority region.
- An `Entry` without a `<Match>` is a catch-all: it never outcompetes an explicit matcher, and simply absorbs any member that no other entry claimed.
- Ties are broken by declaration order in the XML, giving predictable, Rider-like behavior for overlapping matchers.

`CSharpMember` carries syntactic flags (kind, access, modifiers, attributes, explicit-interface marker) that the `MatchExpression`s evaluate against.

## Compatibility principle

Do not encode Rider XML tags directly inside the rearrangement algorithm. Every Rider construct is parsed into an engine-level model first. Unsupported matchers are diagnosed and fail closed rather than corrupting source.

## Source rewriting

`CSharpRewriter` reorders only the spans inside the class body between the opening and closing braces. It extracts the member's indent once, then re-emits each member's body separated by a single blank line. All text outside the class (usings, other types, trailing content) is left untouched, so the change is minimal and diff-friendly.

### Region emission

`LayoutEngine.ArrangeGroups` preserves the source region each member lands in, coalescing consecutive entries that share a region (so sibling slots like static + instance fields collapse into one group). `CSharpRewriter` emits `#region`/`#endregion` wrappers for the region names requested in `RegionOptions.Enabled` (driven by the `riderLayout.regions` VS Code setting). When no regions are enabled the rewriter falls back to the legacy flat, blank-line-separated ordering, keeping the golden fixtures byte-identical.