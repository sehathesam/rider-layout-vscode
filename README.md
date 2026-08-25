# Rider Layout for C#
**Bring JetBrains Rider's file layout to your C# code — right in VS Code.**

> The missing piece for C# developers who love Rider's *member arrangement* but work in VS Code. Point it at your Rider layout, and your files organize themselves — no new formatter to learn, no config abuse.

---

## 🎯 What you get

Rider's **File Layout** is one of its most-loved features: real projects enforce a consistent order for fields, constructors, properties, methods, Unity lifecycle hooks, and more — automatically. This extension reproduces that behavior for VS Code, so your whole team can agree on one arrangement and stop arguing about where things go.

### File layout, adopted to your rules
The exact layout you already use in Rider (`.xml` / `.DotSettings`, including Unity-style namespaces) can be loaded *directly*. No migration, no rewriting — the same pattern your team already relies on.

### Zero-config daily flow
Select your layout file **once**, and from then on every `*.cs` file you focus is reordered automatically. No buttons to hunt for, no remembering shortcuts.

### Built for real Rider layouts
- `TypePattern`, `Entry`, regions with **nested priorities**
- Sort by explicit `Order` (`public → internal → protected → private`), kind, static/readonly/virtual/override
- Semantic matchers for Unity teams: `[SerializeField]`, lifecycle methods, interface implementations
- Catch-all rules so nothing is silently dropped

### Identity & diff-friendly
A span-based rewriter reorders only what belongs in the class body — usings, other types, and surrounding code are untouched, so your diffs stay clean and your `using` block isn't destroyed.

## ⚡ Get started in 3 steps

1. **Install** the extension (or just open the detail page and press **Install**).
2. **Choose your layout** — `Ctrl+Shift+P` → **Rider Layout: Select Layout File** → pick your `.xml` / `.DotSettings`.
3. **Open any `.cs` file** — done. It's reordered the moment it gains focus.

Auto-apply is on by default. Flip a switch anytime with **Rider Layout: Toggle Auto-Apply**.

## 📚 Try the bundled StyleCop/Unity layout
The repo includes a working StyleCop-class layout (`fixtures/rider/ideen-layout.xml`) that organizes classes in the order your team loves:

`Injected Fields → Constants → Static fields → Fields → Serialized fields → Ctors → Events & Delegates → Properties → Interface implementations → Methods (public → protected → private, with Unity/RPC/none)`

## 🛣️ Commands

| Command | What it does |
|---|---|
| **Select Layout File** | Pick a Rider layout `.xml` / `.DotSettings` file |
| **Toggle Auto-Apply** | On/off automatic reorder when a `*.cs` gains focus |
| **Toggle Format on Save** | On/off reorder before every save (default: off) |
| **Rearrange Document** | Apply the layout to the current file now |
| **Preview Active Layout** | Show the active layout XML |

## 🔧 Settings

- `riderLayout.enabled` — master switch (default `true`)
- `riderLayout.autoApplyOnFocus` — auto-reorder on focus (default `true`); needs `.idea`
- `riderLayout.autoDetect` — discover layout in `.idea` / `.Settings` in workspace
- `riderLayout.formatOnSave` — apply on save (default `false`)
- `riderLayout.settingsPath` — explicit path to your layout file

## Development

Built with:
- **TypeScript** (VS Code extension + long-lived JSON-lines IPC)
- **.NET 8 + Roslyn** for C# parsing
- **xUnit** golden/unit tests validated against real Rider layout XML

See [docs/README.md](docs/README.md) (in the repository) for developer docs, and `docs/ROADMAP.md` for what's next.

## Release notes
**v0.2.0** — Format on save + global layout:
- New **Toggle Format on Save** command (default off) — rearranges and saves the file
- Layout file is stored globally — set it **once**, used in every project
- Better feedback & no-workspace support

**v0.1.0** — MVP scratchpad:
- Load a Rider layout file & auto-apply on focus
- Regions, priorities, `Order` sorting, full member-kind set
- Semantic Unity matchers & golden tests

---

*Built with ❤️ for teams that love Rider's order but live in VS Code.*