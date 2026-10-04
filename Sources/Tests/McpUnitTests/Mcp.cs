//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;
using Xunit;

namespace AngouriMath.Mcp.Tests;

/// <summary>Drives the server through <see cref="Server.Run"/>, a line of JSON-RPC at a time.</summary>
internal static class Mcp
{
    /// <summary>
    /// The replies to <paramref name="lines"/>, one per line the server wrote, and everything it
    /// wrote to the error stream.
    /// </summary>
    public static (List<JsonObject> Replies, string Errors) Session(params string[] lines)
    {
        using var input = new StringReader(string.Join("\n", lines));
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, Server.Run([], input, output, error));
        var replies = output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => JsonNode.Parse(line) as JsonObject ?? throw new InvalidOperationException($"not an object: {line}"))
            .ToList();
        return (replies, error.ToString());
    }

    /// <summary>The result of one request with id 1, after <c>initialize</c>.</summary>
    public static JsonObject Request(string method, string parametersJson = "{}")
    {
        var (replies, errors) = Session(
            """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{}}""",
            $$"""{"jsonrpc":"2.0","id":1,"method":"{{method}}","params":{{parametersJson}}}""");
        Assert.Equal("", errors);
        var reply = Assert.Single(replies, reply => (int?)reply["id"] == 1);
        return reply["result"] as JsonObject ?? throw new InvalidOperationException($"{method} answered {reply.ToJsonString()}");
    }

    /// <summary>
    /// What one tool answered: the JSON in its text content, which is what a model reads.
    /// </summary>
    public static JsonObject Call(string tool, string argumentsJson)
    {
        var result = Request("tools/call", $$"""{"name":"{{tool}}","arguments":{{argumentsJson}}}""");
        var text = result["content"]?[0]?["text"]?.GetValue<string>()
            ?? throw new InvalidOperationException($"{tool} answered {result.ToJsonString()}");
        return JsonNode.Parse(text) as JsonObject ?? throw new InvalidOperationException($"{tool} answered {text}");
    }

    /// <summary>A string field of a tool's answer.</summary>
    public static string Field(this JsonObject payload, string name) =>
        payload[name]?.ToString() ?? throw new InvalidOperationException($"no '{name}' in {payload.ToJsonString()}");

    /// <summary>The warnings a tool raised, or none.</summary>
    public static IEnumerable<string> Warnings(this JsonObject payload) =>
        payload["warnings"] is JsonArray warnings ? warnings.Select(w => w!.GetValue<string>()) : [];
}
