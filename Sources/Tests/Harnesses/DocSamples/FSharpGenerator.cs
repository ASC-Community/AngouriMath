//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DocSamples;

/// <summary>
/// The F# side of the same check. `AngouriMath.FSharp` is a published package, and the wiki
/// page for it is the only documentation of the wrapper's names, so a wrong name there --
/// `dy/dx` for `d/dx` -- has nothing else to catch it.
///
/// One file, one module per sample, so each sample keeps its own `open` directives. F#
/// wants `open` before anything else in a module, so they are hoisted out of the body.
/// </summary>
static class FSharpGenerator
{
    public static Dictionary<string, int> Write(string dir, string wrapperFsproj,
        IEnumerable<Snippet> snippets, ISet<string> exclude)
    {
        var wanted = snippets.Where(s => s.Mode != Mode.Skip && !exclude.Contains(s.Id)).ToList();
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        var offsets = new Dictionary<string, int>();
        sb.AppendLine("module DocSamples.Generated.FSharpSamples");
        sb.AppendLine();

        foreach (var s in wanted)
        {
            var opens = s.Body.Where(l => l.TrimStart().StartsWith("open ")).ToList();
            sb.AppendLine($"module {s.Id} =");
            foreach (var o in opens) sb.AppendLine("    " + o.Trim());
            sb.AppendLine("    let run () =");
            offsets[s.Id] = sb.ToString().Count(c => c == '\n') + 1;
            var wrote = false;
            foreach (var line in s.Body)
            {
                if (opens.Contains(line)) { sb.AppendLine(); continue; }
                sb.AppendLine(line.Trim().Length == 0 ? "" : "        " + line);
                if (line.Trim().Length > 0) wrote = true;
            }
            if (!wrote) sb.AppendLine("        ()");
            sb.AppendLine();
        }

        sb.AppendLine("module Runner =");
        sb.AppendLine("    open System");
        sb.AppendLine("    open System.IO");
        sb.AppendLine("    open System.Text.Json");
        sb.AppendLine();
        sb.AppendLine("    type Result = { Id: string; Status: string; Output: string }");
        sb.AppendLine();
        sb.AppendLine("    let samples : (string * (unit -> unit) * bool) list =");
        if (wanted.Count == 0) sb.AppendLine("        []");
        else
            sb.AppendLine("        [ " + string.Join("\n          ",
                wanted.Select(s => $"(\"{s.Id}\", {s.Id}.run, {(s.Mode == Mode.Run ? "true" : "false")})"))
                + " ]");
        sb.AppendLine("""

                [<EntryPoint>]
                let main argv =
                    let real = Console.Out
                    let results =
                        samples
                        |> List.map (fun (id, run, execute) ->
                            if not execute then { Id = id; Status = "compiled"; Output = "" }
                            else
                                let captured = new StringWriter()
                                Console.SetOut(captured)
                                let status =
                                    try
                                        run ()
                                        "ok"
                                    with e -> "threw " + e.GetType().Name + ": " + e.Message.Split('\n').[0]
                                Console.SetOut(real)
                                { Id = id; Status = status; Output = captured.ToString() })
                    let path = if argv.Length > 0 then argv.[0] else "results.json"
                    File.WriteAllText(path, JsonSerializer.Serialize(results))
                    0
            """);

        File.WriteAllText(Path.Combine(dir, "Samples.fs"), sb.ToString());
        File.WriteAllText(Path.Combine(dir, "generated-fsharp.fsproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>generatedfsharp</AssemblyName>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                <WarningLevel>0</WarningLevel>
                <NoWarn>FS0025;FS0049;FS0064;FS0193;FS1182</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="Samples.fs" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="{wrapperFsproj}" />
              </ItemGroup>
            </Project>
            """);
        return offsets;
    }
}
