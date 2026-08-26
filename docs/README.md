# Developer Guide

This page is for **contributors** building the extension from source. End users read the [Marketplace README](../README.md).

## Repo layout

```text
src/                         VS Code extension (TypeScript)
engine/RiderLayout.Core/     Rider-independent rule engine
engine/RiderLayout.Rider/    Rider XML/settings compatibility
engine/RiderLayout.CSharp/   Roslyn parsing and source rewriting
engine/RiderLayout.Cli/      Long-lived JSON-lines process (id-correlated)
tests/                       Unit + golden tests
fixtures/                    Golden test inputs/outputs
```

## Prerequisites

- Node.js 20+
- .NET 8 SDK+
- `@vscode/vsce` (for packaging) — used via `npx`

## Build

```bash
npm install
npm run compile

dotnet restore engine/RiderLayout.sln
dotnet build engine/RiderLayout.sln -c Release
```

## Test

```bash
dotnet test engine/RiderLayout.sln -c Release
```

The suites cover the engine (region priority, sorting, semantic matchers), the XML parser, member classification, and golden rewrites (including a StyleCop/Unity layout).

## Package & install locally

```bash
# Compile the TS extension
npm run compile

# Publish the CLI into the extension runtime (required; the extension spawns it)
dotnet publish engine/RiderLayout.Cli/RiderLayout.Cli.csproj -c Release -o runtime

# Build the VSIX and install it
npx @vscode/vsce package
code --install-extension rider-layout-0.1.0.vsix
```

Restart VS Code (or `Reload Window`) after installing. The extension activates
on C# files, on `riderLayout.pickLayoutFile`, and `riderLayout.toggleEnabled`.

## Configuration reference

| Setting | Default | Meaning |
|---|---|---|
| `riderLayout.enabled` | `true` | Master switch. |
| `riderLayout.autoApplyOnFocus` | `true` | Reorder a `*.cs` when it gains focus. |
| `riderLayout.autoDetect` | `true` | Auto-discover `.DotSettings`/`.idea` layouts. |
| `riderLayout.formatOnSave` | `false` | Apply layout on save. |
| `riderLayout.cliPath` | `""` | Path to a published `RiderLayout.Cli.dll`. |
| `riderLayout.settingsPath` | `null` | Explicit path to a layout file. |
| `riderLayout.regions` | `[]` | Region names to emit as `#region` blocks (match `<Region Name>` in the layout XML). Empty keeps the flat ordering. |

## Troubleshooting

- **"No Rider File Layout found"** — select a layout file first (command palette → `Rider Layout: Select Layout File`).
- **Nothing happens on focus** — check `riderLayout.enabled` and `riderLayout.autoApplyOnFocus`; the CLI prints diagnostics to the **Rider Layout** output channel.
- **CLI not found** — set `riderLayout.cliPath` or re-publish with `dotnet publish ... -o runtime` before packaging.