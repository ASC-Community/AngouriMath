//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// A preliminary tool may change its arguments and answers in a minor release. It is offered only
/// by a server started as <c>amcli mcp --preliminary</c>, and says that it is preliminary wherever
/// a model reads it: in its description, and in each answer.
/// </summary>
public sealed class PreliminaryTest
{
    private const string Gravitation = """{"expression":"G*M*m/r^2"}""";

    private static Dictionary<string, string> Listed(bool preliminary) =>
        Mcp.Request("tools/list", preliminary: preliminary)["tools"]!.AsArray()
            .ToDictionary(tool => tool!["name"]!.GetValue<string>(), tool => tool!["description"]!.GetValue<string>());

    [Fact]
    public void APreliminaryToolIsListedOnlyWhenAskedFor()
    {
        var stable = Listed(preliminary: false);
        var all = Listed(preliminary: true);

        Assert.DoesNotContain("am_classify", stable.Keys);
        Assert.Equal(stable.Keys.Append("am_classify").Order(), all.Keys.Order());
        Assert.StartsWith("Preliminary:", all["am_classify"]);
        Assert.All(stable, tool => Assert.Equal(tool.Value, all[tool.Key]));
        Assert.DoesNotContain(stable.Values, description => description.StartsWith("Preliminary", StringComparison.Ordinal));
    }

    [Fact]
    public void APreliminaryToolCalledWithoutTheFlagSaysHowToAskForIt()
    {
        var answer = Mcp.Call("am_classify", Gravitation);
        Assert.Equal("failed", answer.Field("status"));
        Assert.Contains("amcli mcp --preliminary", answer.Field("error"));
        Assert.Null(answer["likely_fields"]);
    }

    [Fact]
    public void APreliminaryToolsAnswerSaysItIsPreliminary()
    {
        var answer = Mcp.Call("am_classify", Gravitation, preliminary: true);
        Assert.Equal("solved", answer.Field("status"));
        Assert.True(answer["preliminary"]!.GetValue<bool>());
        Assert.Equal("Newtonian gravitation", answer["likely_fields"]![0]!["field"]!.GetValue<string>());

        Assert.Null(Mcp.Call("am_parse", """{"expression":"x + 1"}""", preliminary: true)["preliminary"]);
    }

    [Fact]
    public void TheHelpNamesThePreliminaryTools()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Server.Run(["--help"], new StringReader(""), output, error));
        Assert.Contains("amcli mcp --preliminary  serve the preliminary tools as well: am_classify", output.ToString());
    }
}
