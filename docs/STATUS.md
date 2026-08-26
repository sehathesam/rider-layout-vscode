# Current implementation status

The repository is fully buildable and tested. The .NET solution compiles with `dotnet build` and all unit/golden tests pass.

## Verified

- `dotnet build engine/RiderLayout.sln -c Release` succeeds.
- The test suites all pass (`dotnet test engine/RiderLayout.sln`):
  - `RiderLayout.Core.Tests` — engine, region priority, sorting, semantic matchers.
  - `RiderLayout.Rider.Tests` — XML parser.
  - `RiderLayout.CSharp.Tests` — parser classification, access defaults, rewriter, and golden fixtures.
- `npm run compile` produces `dist/`.
- `dotnet publish ... -o runtime` produces the CLI that the packaged extension runs.
- A StyleCop/Unity layout (`fixtures/rider/ideen-layout.xml`) is validated end-to-end against a golden C# fixture.

## Known limitations

- A single `TypePattern` (the highest-priority one) is selected per file; independent per-type selection is not implemented.
- `GroupBy` is not implemented; `#region` emission is opt-in via the `riderLayout.regions` setting (see `docs/ARCHITECTURE.md`).
- Region removal (`RemoveRegions`) is not implemented; regions are only inserted for the enabled set and empty regions are skipped.
- Exact Rider conflict-strength is approximated by region/entry priorities.
- `HandlesEvent` is not modeled; `ImplementsInterface` is matched syntactically (explicit interface specifier).

## Environment note

Unlike the original scaffold, this repo has been built and tested on Windows with the .NET 8 SDK and Node.js. Use `scripts\build.cmd` or the build steps in [README](../README.md#building-and-packaging).