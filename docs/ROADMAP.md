# Roadmap

## Phase 1 — MVP foundation

- [x] Extension project
- [x] .NET CLI
- [x] JSON-lines IPC
- [x] Rider XML parser
- [x] Roslyn member model
- [x] Basic rearrangement
- [x] Golden fixtures

## Phase 2 — Rider matching compatibility

- [ ] Constraint-strength calculation
- [ ] Multiple TypePattern selection
- [ ] FilePattern
- [ ] Full member kind set
- [ ] More modifiers
- [ ] `HasAttribute` normalization
- [ ] semantic base-type/interface checks
- [ ] generic/type-parameter constraints

## Phase 3 — Regions and groups

- [ ] Region AST
- [ ] GroupBy
- [ ] `${Name}` expansion
- [ ] Region removal before layout
- [ ] Region insertion after layout

## Phase 4 — Unity

- [ ] `unity:SerializableClass`
- [ ] Unity lifecycle method matcher
- [ ] Unity attribute aliases
- [ ] Unity-specific type matching

## Phase 5 — VS Code integration

- [ ] Format on save
- [ ] Workspace-wide rearrangement
- [ ] Preview/diff UI
- [ ] Explain why a member matched a rule
- [ ] Diagnostics for unsupported Rider matchers

## Phase 6 — Compatibility suite

Use Rider as an oracle for a large corpus of input files and compare output with this engine. Every discovered difference becomes a regression test.
