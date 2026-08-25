# Rider Layout for C# — VS Code

A VS Code extension prototype that consumes JetBrains Rider File/Type Layout XAML and rearranges C# members using a Rider-compatible rule model.

## Current MVP

Implemented:

- VS Code extension shell in TypeScript.
- JSON-lines IPC between the extension and a .NET CLI.
- Rider `Patterns` XML parsing.
- `TypePattern`, `Entry`, `Entry.Match`, `Entry.SortBy`.
- `And`, `Or`, `Not`.
- `Kind`, `Access`, `Name`.
- `Static`, `Readonly`, `Abstract`, `Virtual`, `Override`, `Const`.
- Basic `HasAttribute` matching.
- Match priority and declaration-order fallback.
- Basic sorting by `Name`, `Kind`, `Access`, `Static`, `Readonly`, `Const`.
- Roslyn-based C# member extraction and source rewriting.
- VS Code commands and Code Action.
- Initial project/settings discovery for `.DotSettings` and `.idea` XML.

Not yet implemented:

- Full Rider type matcher semantics.
- File-level pattern application.
- Regions/groups and `GroupBy` source transformation.
- All Rider constraints and semantic matchers.
- Multiple classes/types in a file with independent type-pattern selection.
- Exact Rider conflict-strength algorithm.
- `NoReorder` attribute handling.

## Repository layout

```text
src/                         VS Code extension
engine/RiderLayout.Core/     Rider-independent rule engine
engine/RiderLayout.Rider/    Rider XML/settings compatibility
engine/RiderLayout.CSharp/   Roslyn parsing and source rewriting
engine/RiderLayout.Cli/      JSON-lines process boundary
tests/                       Unit tests
fixtures/                    Golden test inputs/outputs
```

## Build prerequisites

- Node.js 20+ recommended.
- .NET 8 SDK+.

The current environment used to create this repository did not have the `dotnet` SDK installed, so the C# solution has been structurally generated but not compiled in this environment.

## Build

```bash
npm install
npm run compile

dotnet restore engine/RiderLayout.sln
dotnet build engine/RiderLayout.sln -c Release
```

Build the CLI and copy it to the extension runtime directory before packaging:

```bash
dotnet publish engine/RiderLayout.Cli/RiderLayout.Cli.csproj -c Release -o runtime
npm run compile
npx @vscode/vsce package
```

Then configure `riderLayout.cliPath` to the published `RiderLayout.Cli.dll`, or copy it to `runtime/RiderLayout.Cli.dll` inside the extension.

## Design goal

The long-term target is not a new formatter syntax. The target is Rider compatibility:

```text
Rider File Layout XAML
        ↓
RiderLayout.Rider
        ↓
RiderLayout.Core
        ↓
Roslyn C# model
        ↓
ordered members / source rewrite
```

Rider's current documentation describes file/type layout as pattern-based matching with priorities, sorting, regions/groups, unmatched members, and type patterns. The MVP intentionally implements only a safe subset first.
