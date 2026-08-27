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

`Dependencies → Constants → Serialized fields → Static fields → Fields → Ctors → Events → Enums & Interfaces → Properties → Indexers → Interface implementations → Tests → RPC/Unity methods → Methods (public → internal → protected → private) → Nested types`

## 🛣️ Commands

| Command | What it does |
|---|---|
| **Select Layout File** | Pick a Rider layout `.xml` / `.DotSettings` file |
| **Toggle Auto-Apply** | On/off automatic reorder when a `*.cs` gains focus |
| **Toggle Format on Save** | On/off reorder before every save (default: off) |
| **Rearrange Document** | Apply the layout to the current file now |
| **Rearrange All C# Files** | Apply the layout to every `*.cs` in the workspace and save (one bulk change, ready to commit) |
| **Preview Active Layout** | Show the active layout XML |

## 🔧 Settings

- `riderLayout.enabled` — master switch (default `true`)
- `riderLayout.autoApplyOnFocus` — auto-reorder on focus (default `true`); needs `.idea`
- `riderLayout.autoDetect` — discover layout in `.idea` / `.Settings` in workspace
- `riderLayout.formatOnSave` — apply on save (default `false`)
- `riderLayout.settingsPath` — explicit path to your layout file (leave empty for the bundled default)

## Development

Built with:
- **TypeScript** (VS Code extension + long-lived JSON-lines IPC)
- **.NET 8 + Roslyn** for C# parsing
- **xUnit** golden/unit tests validated against real Rider layout XML

See [docs/README.md](docs/README.md) (in the repository) for developer docs, and `docs/ROADMAP.md` for what's next.

## Release notes
**v0.5.9** — Reworked bundled layout with explicit region priorities:
- Every region in `ideen-layout.xml` now carries an explicit `Priority`, giving a single deterministic order across the whole type — no more relying on declaration order
- **DEPENDENCIES** now matches constructor-injected `private readonly` fields (non-static, non-serialized, no initializer) and sits at the top with the highest priority
- New granular access regions: **INTERNAL EVENTS**, **INTERNAL PROPERTIES**, **INTERNAL INDEXERS**, and an **INTERNAL METHODS** block — internal members are now split out instead of sharing the public region
- Properties and indexers (public/internal/protected/private) plus methods/operators now carry `Not ImplementsInterface`, so interface implementations only ever match `INTERFACE IMPLEMENTATIONS`
- Injected fields are excluded from the generic **Static fields** / **Fields** regions; **Serialized fields** and **RPC METHODS** keep their own slots

**v0.5.8** — Cleaner interface-method routing in the bundled layout:
- The bundled `ideen-layout.xml` now keeps **interface implementations** out of `PUBLIC METHODS` (they get `Not ImplementsInterface` in the matcher), so they live only in `INTERFACE IMPLEMENTATIONS` instead of appearing in both regions
- `INTERFACE IMPLEMENTATIONS` entry renamed from "Explicit interface implementations" to the more accurate "Interface implementations"

**v0.5.7** — Ignore folders + always format:
- New **Add Ignored Folder** command (and `riderLayout.ignoreFolders` setting, `Migrations` by default) lets you skip folders like EF `Migrations` in **Rearrange All C# Files**
- Files the layout can't process are still formatted (Shift+Alt+F equivalent) and saved instead of being skipped

**v0.5.6** — Rearrange whole project in one go:
- New **Rearrange All C# Files** command applies the active layout to every `*.cs` in the workspace, runs the formatter (Shift+Alt+F equivalent) on each changed file, and saves it — ready to review and commit as a single change

**v0.5.4** — No more false "Select Layout File" prompts:
- Files with no class (lone interface, enum, using-only) are a silent no-op instead of raising "No class declaration found"
- Removed the obsolete "Select Layout File" action from the auto-apply warning — a default layout is always bundled, so no prompt is needed
- A pattern with its own `<Match>` that doesn't apply to the class now leaves the file untouched (fail-closed)

**v0.5.3** — Interfaces in other files/namespaces:
- `INTERFACE IMPLEMENTATIONS` now resolves interfaces defined anywhere in the project (other namespaces/files), not just the current file, BCL-only, or explicit implementations
- The rewriter compiles the project's `.cs` sources as references, so a class implementing e.g. `Homa.Logic.IReply` with an implicit `public virtual void Init(...)` is placed in the region
- Model is cached per project root for speed

**v0.5.2** — Interface implementations detected implicitly:
- `INTERFACE IMPLEMENTATIONS` now also catches **implicit** interface members (e.g. `public void Dispose()` when the class implements `IDisposable`), not just `void IDisposable.Dispose()`
- Detection is semantic via a Roslyn model, so BCL interfaces (`IDisposable`, `IEnumerable`, …) and interfaces defined in the same file are both resolved; plain public methods that don't implement anything are left in their own region
- Works in any layout that uses `<ImplementsInterface/>`

**v0.5.1** — Constructor-assigned fields go to Dependencies:
- Fields assigned anywhere in an instance constructor now automatically route into a region named **DEPENDENCIES**, in any layout that has one — no XML change needed
- Detects both `this._field = …` and plain `_field = …`, ignoring constructor parameters and locals
- Instance (non-static) fields only; a fresh install or existing layout picks this up automatically

**v0.5.0** — Bundled default layout:
- Ships with the **ideen-layout.xml** (StyleCop Unity Classes) built in — if you haven't set a custom layout it's used automatically, so it works out of the box after install
- Custom layout still available via **Select Layout File** (or set `riderLayout.settingsPath`)
- New **Rider Layout: Reset to Default Layout** command (and a link in the Settings UI) clears your custom path and switches back to the bundled default

**v0.4.1** — Clickable file browser in Settings:
- `riderLayout.settingsPath` now shows a **"Select Layout File…"** button in the Settings UI that opens the file browser and stores the chosen layout globally — no need to switch to the Command Palette

**v0.4.0** — Region settings, toggle & `#region` emission:
- New **Toggle Region Blocks** command (`riderLayout.toggleRegions`) switches the whole region feature on/off from the Command Palette
- New `riderLayout.emitRegions` master switch — flipped by the command without touching your region selection
- New `riderLayout.regions` setting — which regions become `#region` blocks (matches `<Region Name>` in the layout XML). All enabled by default; deselect to omit specific ones
- `riderLayout.formatAfterRearrange` (default on) — runs the editor formatter (Shift+Alt+F) after the layout is applied
- `ArrangeGroups` in the engine preserves region boundaries across consecutive entries, so sibling slots (e.g. static + instance fields) share one `#region`
- Golden test covering region emission against `ideen-layout.xml`

**v0.3.0** — Format on save + global layout:
- New **Toggle Format on Save** command (default off) — rearranges before every save
- Layout file is stored globally — set it **once**, used in every project
- Better feedback & no-workspace support

**v0.2.0** — Clearer auto-apply errors:
- Auto-apply shows a clear warning with a "Select Layout File" action instead of silently doing nothing
- Works with single files (no workspace folder required)

**v0.1.0** — MVP scratchpad:
- Load a Rider layout file & auto-apply on focus
- Regions, priorities, `Order` sorting, full member-kind set
- Semantic Unity matchers & golden tests

---

*Built with ❤️ for teams that love Rider's order but live in VS Code.*