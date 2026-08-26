using RiderLayout.CSharp.Source;
using RiderLayout.Core.Model;
using RiderLayout.Rider.Xml;
using Xunit;

namespace RiderLayout.CSharp.Tests;

public class IdeenLayoutGoldenTests
{
    private static string Read(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "fixtures")))
            dir = dir.Parent;
        if (dir is null) throw new DirectoryNotFoundException("Repository root not found from test output.");
        return File.ReadAllText(Path.Combine(dir.FullName, relativePath));
    }

    [Fact]
    public void RearrangesPlayerWithStyleCopUnityLayout()
    {
        var layout = Read("fixtures/rider/ideen-layout.xml");
        var input = Read("fixtures/csharp/input/Player.cs");
        var expected = Read("fixtures/csharp/expected/Player.cs").Replace("\r\n", "\n").TrimEnd();

        var pattern = new RiderLayoutXmlParser().Parse(layout).TypePatterns[0];
        var actual = new CSharpRewriter().Rearrange(input, pattern).Replace("\r\n", "\n").TrimEnd();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmitsRequestedRegionsAroundMembers()
    {
        var layout = Read("fixtures/rider/ideen-layout.xml");
        var input = Read("fixtures/csharp/input/Player.cs");
        var expected = Read("fixtures/csharp/expected/Player.regions.cs").Replace("\r\n", "\n").TrimEnd();

        var pattern = new RiderLayoutXmlParser().Parse(layout).TypePatterns[0];
        var regions = new RegionOptions
        {
            Enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DEPENDENCIES", "CONSTANTS", "FIELDS", "SERIALIZED FIELDS",
                "CTORS", "PUBLIC EVENTS", "PRIVATE EVENTS",
                "PUBLIC PROPERTIES", "PRIVATE PROPERTIES",
                "INTERFACE IMPLEMENTATIONS", "PUBLIC METHODS", "UNITY METHODS"
            }
        };

        var actual = new CSharpRewriter().Rearrange(input, pattern, regions).Replace("\r\n", "\n").TrimEnd();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmitsRegionsEvenWhenOrderIsAlreadyCorrect()
    {
        // Start from the region-tagged golden output, then strip the #region /
        // #endregion directives. The remaining body is still in layout order but
        // has no tags. Because the order is unchanged the legacy path would have
        // returned the source verbatim; the region emitter must re-insert them.
        var full = Read("fixtures/csharp/expected/Player.regions.cs").Replace("\r\n", "\n");
        var input = StripRegionDirectives(full);
        var expected = full.TrimEnd();

        var layout = Read("fixtures/rider/ideen-layout.xml");
        var pattern = new RiderLayoutXmlParser().Parse(layout).TypePatterns[0];
        var regions = new RegionOptions
        {
            Enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DEPENDENCIES", "CONSTANTS", "FIELDS", "SERIALIZED FIELDS",
                "CTORS", "PUBLIC EVENTS", "PRIVATE EVENTS",
                "PUBLIC PROPERTIES", "PRIVATE PROPERTIES",
                "INTERFACE IMPLEMENTATIONS", "PUBLIC METHODS", "UNITY METHODS"
            }
        };

        var actual = new CSharpRewriter().Rearrange(input, pattern, regions).Replace("\r\n", "\n").TrimEnd();

        Assert.Equal(expected, actual);
    }

    private static string StripRegionDirectives(string source)
    {
        var lines = source.Split('\n').Where(l => !l.Contains("#region") && !l.Contains("#endregion"));
        return string.Join('\n', lines);
    }

    [Fact]
    public void ReapplyingToAlreadyRegionedFileIsIdempotent()
    {
        // Re-running the layout on a file that already contains #region blocks
        // must not duplicate the tags: the #region/#endregion directives must be
        // stripped from member trivia and only the emitter's tags survive.
        var full = Read("fixtures/csharp/expected/Player.regions.cs").Replace("\r\n", "\n").TrimEnd();
        var input = full;

        var layout = Read("fixtures/rider/ideen-layout.xml");
        var pattern = new RiderLayoutXmlParser().Parse(layout).TypePatterns[0];
        var regions = new RegionOptions
        {
            Enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DEPENDENCIES", "CONSTANTS", "FIELDS", "SERIALIZED FIELDS",
                "CTORS", "PUBLIC EVENTS", "PRIVATE EVENTS",
                "PUBLIC PROPERTIES", "PRIVATE PROPERTIES",
                "INTERFACE IMPLEMENTATIONS", "PUBLIC METHODS", "UNITY METHODS"
            }
        };

        var first = new CSharpRewriter().Rearrange(input, pattern, regions).Replace("\r\n", "\n").TrimEnd();
        var second = new CSharpRewriter().Rearrange(first, pattern, regions).Replace("\r\n", "\n").TrimEnd();

        Assert.Equal(first, second);
        Assert.Equal(full, first);
    }
}