# Roadmap

## Phase 1 — MVP foundation

- [x] Extension project
- [x] .NET CLI (long-lived JSON-lines IPC, id-correlated)
- [x] Rider XML parser (incl. `unity` namespace, NBSP normalization)
- [x] Roslyn member model
- [x] Basic rearrangement (span-based rewrite)
- [x] Region support + inherited priorities + catch-all entries
- [x] `SortBy` with explicit `Order` and boolean keys
- [x] Full member kind set (incl. `Constant`, `Destructor`, `Indexer`, `Operator`)
- [x] Access defaults (class members private, interface members public)
- [x] Semantic matchers: `HasAttribute` short-name, `SerializedField`, `EventFunction` (Unity lifecycle), explicit `ImplementsInterface`
- [x] Golden fixtures (incl. a StyleCop/Unity layout)
- [x] VS Code: select-layout command, auto-apply on focus, toggle, preview

## Phase 2 — Rider matching compatibility

- [ ] Constraint-strength calculation
- [ ] Multiple TypePattern selection per file
- [ ] FilePattern
- [x] `HasAttribute` normalization
- [ ] semantic base-type/interface checks (beyond explicit specifier)
- [ ] generic/type-parameter constraints
- [ ] `HandlesEvent` and event-subscription semantics

## Phase 3 — Regions and groups

- [x] Region AST + priority inheritance
- [ ] `GroupBy`
- [ ] `${Name}` expansion
- [ ] Region removal before layout
- [ ] Region insertion after layout

## Phase 4 — Unity

- [ ] `unity:SerializableClass`
- [x] Unity lifecycle method matcher (`EventFunction`)
- [x] Unity attribute aliases (`SerializedField`)
- [ ] Unity-specific type matching

## Phase 5 — VS Code integration

- [ ] Format on save
- [ ] Workspace-wide rearrangement
- [ ] Preview/diff UI
- [ ] Explain why a member matched a rule
- [ ] Diagnostics for unsupported Rider matchers

## Phase 6 — Compatibility suite

Use Rider as an oracle for a large corpus of input files and compare output with this engine. Every discovered difference becomes a regression test.