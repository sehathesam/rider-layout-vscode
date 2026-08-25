using RiderLayout.CSharp.Source;
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
}