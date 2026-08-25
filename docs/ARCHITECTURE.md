# Architecture

## Separation of concerns

`RiderLayout.Core` contains no Rider XML and no VS Code dependencies. It consumes normalized layout objects and normalized C# member objects.

`RiderLayout.Rider` knows how Rider stores and serializes layout patterns.

`RiderLayout.CSharp` knows Roslyn. It converts syntax nodes into `CSharpMember` objects and rewrites the selected type without editing source using regex/string offsets.

`RiderLayout.Cli` is deliberately boring: JSON request in, JSON response out. This keeps the VS Code extension independent from Roslyn runtime details.

## Compatibility principle

Do not encode Rider XML tags directly inside the rearrangement algorithm. Every Rider construct should be parsed into an engine-level model first. That allows unsupported matchers to be diagnosed without corrupting source.
