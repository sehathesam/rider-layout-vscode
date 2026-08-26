using System.Text;
using System.Text.Json;
using RiderLayout.Core.Model;
using RiderLayout.CSharp.Source;
using RiderLayout.Rider.Settings;
using RiderLayout.Rider.Xml;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
string? line;

while ((line = Console.ReadLine()) is not null)
{
    if (string.IsNullOrWhiteSpace(line)) continue;

    Request? request = null;
    try
    {
        request = JsonSerializer.Deserialize<Request>(line, options)
            ?? throw new InvalidOperationException("Invalid request.");

        switch (request.Command)
        {
            case "stop":
                Write(Response.Ok(request.Id));
                return;
            case "rearrange":
            {
                var layoutXml = request.LayoutXml;
                if (string.IsNullOrWhiteSpace(layoutXml) && !string.IsNullOrWhiteSpace(request.ProjectRoot))
                    layoutXml = new RiderSettingsReader().FindLayoutXml(request.ProjectRoot!);

                if (string.IsNullOrWhiteSpace(layoutXml))
                    throw new InvalidOperationException("No Rider File Layout XML was found.");

                var pattern = new RiderLayoutXmlParser().Parse(layoutXml);
                var typePattern = pattern.TypePatterns
                    .OrderByDescending(x => x.Priority)
                    .FirstOrDefault();

                if (typePattern is null)
                    throw new InvalidOperationException("The Rider layout contains no TypePattern. File-level rearrangement is not implemented in MVP.");

                var output = new CSharpRewriter().Rearrange(
                    request.Source ?? "",
                    typePattern,
                    new RegionOptions { Enabled = new HashSet<string>(request.Regions ?? [], StringComparer.OrdinalIgnoreCase) },
                    request.ProjectRoot);
                Write(Response.Ok(request.Id).WithSource(output));
                break;
            }
            case "parse":
            {
                var layout = new RiderLayoutXmlParser().Parse(request.LayoutXml ?? "");
                Write(Response.Ok(request.Id).WithDiagnostics(
                    [$"TypePatterns: {layout.TypePatterns.Count}", $"FileNodes: {layout.FileNodes.Count}"]));
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown command '{request.Command}'.");
        }
    }
    catch (Exception ex)
    {
        Write(Response.Fail(request?.Id, ex.Message));
    }
}

void Write(Response response)
{
    Console.WriteLine(JsonSerializer.Serialize(response, options));
    Console.Out.Flush();
}

public sealed record Request
{
    public int Id { get; init; }
    public string Command { get; init; } = "";
    public string? Source { get; init; }
    public string? LayoutXml { get; init; }
    public string? ProjectRoot { get; init; }
    public string[]? Regions { get; init; }
}

public sealed record Response
{
    public bool Success { get; init; }
    public int Id { get; init; }
    public string? Source { get; init; }
    public string? Error { get; init; }
    public string[] Diagnostics { get; init; } = [];

    public static Response Ok(int id) => new() { Success = true, Id = id };
    public static Response Fail(int? id, string error) => new() { Success = false, Id = id ?? 0, Error = error };

    public Response WithSource(string source) => this with { Source = source };
    public Response WithDiagnostics(string[] diagnostics) => this with { Diagnostics = diagnostics };
}