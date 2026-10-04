//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AngouriMath.Mcp;

/// <summary>
/// The MCP server over stdio: newline-delimited JSON-RPC 2.0 read from one stream and written to
/// another, which is what <c>amcli mcp</c> serves on. Nothing but protocol traffic may go to the
/// output -- diagnostics belong on the error stream, or they corrupt the protocol.
/// </summary>
/// <remarks>
/// Requests are handled strictly one at a time. That was once a correctness requirement:
/// <c>MathS.Settings</c> kept its values in thread-static fields, so two concurrent calls with
/// different parse settings interfered. They are an <c>AsyncLocal</c> now and a scope follows the
/// call, so this is no longer load-bearing -- a stdio client sends one request at a time anyway.
/// Making the loop concurrent is a change to measure on its own.
/// </remarks>
public static class Server
{
    /// <summary>The version this server reports to a client, which is the library's: the two ship together.</summary>
    public static string Version =>
        typeof(Entity).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    // A tool's answer is JSON inside a text block, and the model reads that text as written, so it
    // is not escaped for HTML: `+` stays `+` and `α` stays `α` rather than `\u002B` and `\u03B1`.
    // The message around it keeps the default escaping, which a client decodes.
    private static readonly JsonSerializerOptions ToolTextOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Serves until <paramref name="input"/> ends, or runs the self-test or prints the usage, as
    /// <paramref name="arguments"/> ask; returns the exit code.
    /// </summary>
    public static int Run(IReadOnlyList<string> arguments, TextReader input, TextWriter output, TextWriter error)
    {
        // `--selftest` answers "does this install work, and does the library still behave the way
        // the docs claim?" It exits rather than serving, so it never interferes with the protocol.
        if (arguments.Contains("--selftest"))
            return SelfTest.Run(output);

        if (arguments.Contains("--help") || arguments.Contains("-h"))
        {
            output.WriteLine("amcli mcp -- exact symbolic algebra over MCP (stdio JSON-RPC).");
            output.WriteLine();
            output.WriteLine("  amcli mcp              serve on stdin/stdout");
            output.WriteLine("  amcli mcp --selftest   verify the install and check the docs for drift");
            output.WriteLine("  amcli mcp --help       this message");
            return 0;
        }

        var serializerOptions = new JsonSerializerOptions { WriteIndented = false };

        void Send(JsonObject message)
        {
            output.WriteLine(message.ToJsonString(serializerOptions));
            output.Flush();
        }

        string? line;
        while ((line = input.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            JsonObject? request;
            try
            {
                request = JsonNode.Parse(line) as JsonObject;
            }
            catch (JsonException e)
            {
                Send(Error(null, -32700, $"parse error: {e.Message}"));
                continue;
            }

            if (request is null)
                continue;

            var id = request.TryGetPropertyValue("id", out var idNode) ? idNode?.DeepClone() : null;
            var method = request.TryGetPropertyValue("method", out var m) ? m?.GetValue<string>() : null;
            var parameters = request.TryGetPropertyValue("params", out var p) ? p as JsonObject : null;

            if (method is null)
            {
                if (id is not null)
                    Send(Error(id, -32600, "missing 'method'"));
                continue;
            }

            // Notifications carry no id and must never be answered.
            var isNotification = id is null;

            try
            {
                var result = Dispatch(method, parameters, error);
                if (result is null)
                {
                    if (!isNotification)
                        Send(Error(id, -32601, $"unknown method '{method}'"));
                    continue;
                }
                if (!isNotification)
                    Send(Ok(id, result));
            }
            catch (Exception e)
            {
                error.WriteLine($"[amcli mcp] {method} failed: {e}");
                if (!isNotification)
                    Send(Error(id, -32603, $"{e.GetType().Name}: {e.Message}"));
            }
        }

        // The input ended: the host has gone away, which is a normal shutdown.
        return 0;
    }

    private static JsonObject? Dispatch(string method, JsonObject? parameters, TextWriter error)
    {
        switch (method)
        {
            case "initialize":
                return new JsonObject
                {
                    // Pinned rather than echoed: this server implements exactly this revision.
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new JsonObject
                    {
                        ["tools"] = new JsonObject(),
                        ["resources"] = new JsonObject(),
                        ["prompts"] = new JsonObject(),
                    },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "angourimath",
                        ["version"] = Version,
                    },
                    ["instructions"] =
                        "Exact symbolic algebra: simplify, solve, differentiate, integrate, " +
                        "limits, truth tables. Prefer these tools over doing algebra yourself — " +
                        "every answer is machine-checked and integrals are verified by " +
                        "differentiating them back. Always read the 'parsed' field to confirm " +
                        "the expression was understood as intended, and treat a 'declined' or " +
                        "'unchanged' status as 'no answer', never as the answer. Read the " +
                        "angourimath://syntax resource before composing unusual expressions.",
                };

            // Notifications. Acknowledged by returning an empty object; the caller suppresses
            // the response because there is no id.
            case "notifications/initialized":
            case "notifications/cancelled":
                return new JsonObject();

            case "ping":
                return new JsonObject();

            case "tools/list":
                return new JsonObject { ["tools"] = Tools.List() };

            case "tools/call":
            {
                var name = parameters?["name"]?.GetValue<string>();
                if (name is null)
                    return ToolText("{\"status\":\"failed\",\"error\":\"missing tool name\"}", true);

                var arguments = parameters?["arguments"] as JsonObject ?? new JsonObject();

                JsonObject payload;
                try
                {
                    payload = Tools.Call(name, arguments);
                }
                catch (Exception e)
                {
                    // A thrown exception here is a defect in this server, not bad user input;
                    // report it as a tool error rather than killing the connection.
                    error.WriteLine($"[amcli mcp] tool {name} threw: {e}");
                    payload = new JsonObject
                    {
                        ["status"] = "failed",
                        ["error"] = $"{e.GetType().Name}: {e.Message}",
                    };
                }

                var isError = payload["status"]?.GetValue<string>() is "failed";
                return ToolText(payload.ToJsonString(ToolTextOptions), isError);
            }

            case "prompts/list":
                return new JsonObject { ["prompts"] = Prompts.List() };

            case "prompts/get":
            {
                var promptName = parameters?["name"]?.GetValue<string>()
                    ?? throw new InvalidOperationException("prompts/get requires a name");
                return Prompts.Get(promptName, parameters?["arguments"] as JsonObject)
                    ?? throw new InvalidOperationException($"no such prompt: {promptName}");
            }

            case "resources/list":
                return new JsonObject { ["resources"] = Resources.List() };

            case "resources/read":
            {
                var uri = parameters?["uri"]?.GetValue<string>();
                var text = (uri is null ? null : Resources.Read(uri))
                    ?? throw new InvalidOperationException($"no such resource: {uri}");
                return new JsonObject
                {
                    ["contents"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["uri"] = uri,
                            ["mimeType"] = "text/markdown",
                            ["text"] = text,
                        },
                    },
                };
            }

            default:
                return null;
        }
    }

    private static JsonObject ToolText(string text, bool isError) => new()
    {
        ["content"] = new JsonArray
        {
            new JsonObject { ["type"] = "text", ["text"] = text },
        },
        ["isError"] = isError,
    };

    private static JsonObject Ok(JsonNode? id, JsonObject result) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["result"] = result,
    };

    private static JsonObject Error(JsonNode? id, int code, string message) => new()
    {
        ["jsonrpc"] = "2.0",
        ["id"] = id,
        ["error"] = new JsonObject { ["code"] = code, ["message"] = message },
    };
}
