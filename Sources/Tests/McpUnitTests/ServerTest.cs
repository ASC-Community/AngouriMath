//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;
using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>
/// The protocol: what a client sends and what comes back. Nothing but JSON-RPC may reach the
/// output, since one stray line corrupts the stream and the host reports the server as failed.
/// </summary>
public sealed class ServerTest
{
    [Fact]
    public void PingIsAnsweredWithNothing() => Assert.Empty(Mcp.Request("ping"));

    [Fact]
    public void InitializeNamesTheProtocolRevisionAndTheLibrarysVersion()
    {
        var (replies, errors) = Mcp.Session("""{"jsonrpc":"2.0","id":0,"method":"initialize","params":{}}""");
        Assert.Equal("", errors);
        var initialized = Assert.Single(replies)["result"]!;
        Assert.Equal("2024-11-05", initialized["protocolVersion"]!.GetValue<string>());
        Assert.Equal("angourimath", initialized["serverInfo"]!["name"]!.GetValue<string>());
        Assert.Equal(Server.Version, initialized["serverInfo"]!["version"]!.GetValue<string>());
        Assert.Matches(@"^\d+\.\d+\.\d+", Server.Version);
    }

    [Fact]
    public void ANotificationIsNeverAnswered()
    {
        var (replies, errors) = Mcp.Session(
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":3}}""",
            """{"jsonrpc":"2.0","method":"no/such/notification"}""");
        Assert.Empty(replies);
        Assert.Equal("", errors);
    }

    [Theory]
    [InlineData("""{"jsonrpc":"2.0","id":7,"method":"no/such/method","params":{}}""", -32601)]
    [InlineData("""{"jsonrpc":"2.0","id":7,"params":{}}""", -32600)]
    [InlineData("""{"jsonrpc":"2.0","id":7,"method":"prompts/get","params":{"name":"no-such-prompt"}}""", -32603)]
    [InlineData("""{"jsonrpc":"2.0","id":7,"method":"resources/read","params":{"uri":"angourimath://nothing"}}""", -32603)]
    public void AnErrorIsAnsweredWithItsCode(string request, int code)
    {
        var (replies, _) = Mcp.Session(request);
        var reply = Assert.Single(replies);
        Assert.Equal(7, (int)reply["id"]!);
        Assert.Equal(code, (int)reply["error"]!["code"]!);
    }

    [Fact]
    public void ALineThatIsNotJsonIsAParseErrorAndTheSessionGoesOn()
    {
        var (replies, _) = Mcp.Session("{not json", """{"jsonrpc":"2.0","id":2,"method":"ping"}""");
        Assert.Equal(2, replies.Count);
        Assert.Equal(-32700, (int)replies[0]["error"]!["code"]!);
        Assert.Equal(2, (int)replies[1]["id"]!);
    }

    [Fact]
    public void EveryToolIsReadOnlyAndClosedWorld()
    {
        // An MCP client can approve these without asking, which is what gets a maths tool
        // used at all.
        var tools = Mcp.Request("tools/list")["tools"]!.AsArray();
        Assert.Equal(23, tools.Count);
        foreach (var tool in tools)
        {
            var name = tool!["name"]!.GetValue<string>();
            Assert.StartsWith("am_", name);
            Assert.False(string.IsNullOrWhiteSpace(tool["description"]?.GetValue<string>()), name);
            Assert.Equal("object", tool["inputSchema"]!["type"]!.GetValue<string>());
            Assert.True(tool["annotations"]!["readOnlyHint"]!.GetValue<bool>(), name);
            Assert.False(tool["annotations"]!["openWorldHint"]!.GetValue<bool>(), name);
        }
    }

    [Fact]
    public void EveryResourceReadsBack()
    {
        var resources = Mcp.Request("resources/list")["resources"]!.AsArray();
        Assert.Equal(3, resources.Count);
        foreach (var resource in resources)
        {
            var uri = resource!["uri"]!.GetValue<string>();
            var contents = Mcp.Request("resources/read", $$"""{"uri":"{{uri}}"}""")["contents"]!.AsArray();
            var text = Assert.Single(contents)!["text"]!.GetValue<string>();
            Assert.StartsWith("# ", text);
        }
    }

    [Fact]
    public void EveryPromptReadsBack()
    {
        var prompts = Mcp.Request("prompts/list")["prompts"]!.AsArray();
        Assert.Equal(5, prompts.Count);
        foreach (var prompt in prompts)
        {
            var name = prompt!["name"]!.GetValue<string>();
            var arguments = new JsonObject();
            foreach (var argument in prompt["arguments"]?.AsArray() ?? [])
                arguments[argument!["name"]!.GetValue<string>()] = "x^2";
            var messages = Mcp.Request("prompts/get", $$"""{"name":"{{name}}","arguments":{{arguments.ToJsonString()}}}""")["messages"]!.AsArray();
            Assert.NotEmpty(messages);
        }
    }

    [Fact]
    public void AToolsAnswerIsReadableAsWritten()
    {
        // The model reads the text block as it is, so `+` and `α` stay themselves rather than
        // arriving as JSON escapes.
        var (replies, _) = Mcp.Session("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"am_parse","arguments":{"expression":"α + 1"}}}""");
        var text = Assert.Single(replies)["result"]!["content"]![0]!["text"]!.GetValue<string>();
        Assert.Contains("\"parsed\":\"α + 1\"", text);
    }

    [Fact]
    public void HelpAndTheSelfTestDoNotServe()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Server.Run(["--help"], new StringReader("""{"jsonrpc":"2.0","id":1,"method":"ping"}"""), output, error));
        Assert.Contains("amcli mcp --selftest", output.ToString());
        Assert.DoesNotContain("jsonrpc", output.ToString());
    }
}
