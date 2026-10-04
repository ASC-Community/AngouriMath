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
/// Turns the extracted samples into one project that compiles them all, so a rename in the
/// library shows up as a compile error against the page that names it.
///
/// Each sample becomes its own file, which keeps its `using` directives to itself: two
/// samples that import conflicting names both still compile.
/// </summary>
static class Generator
{
    /// What the wiki says is implied in every sample. Anything else a sample needs, it states.
    static readonly string[] ImpliedUsings =
    {
        "using System;",
        "using AngouriMath;",
        "using static AngouriMath.MathS;",
        "using static AngouriMath.Entity;",
    };

    /// <summary>Writes the project, and returns for each sample the generated file line its
    /// body starts on, so a compile error can be pointed back at a line of the wiki.</summary>
    public static Dictionary<string, int> Write(string dir, string angouriMathCsproj,
        IEnumerable<Snippet> snippets, ISet<string> exclude)
    {
        var wanted = snippets.Where(s => s.Mode != Mode.Skip && !exclude.Contains(s.Id)).ToList();

        Directory.CreateDirectory(dir);
        var samplesDir = Path.Combine(dir, "Samples");
        if (Directory.Exists(samplesDir)) Directory.Delete(samplesDir, true);
        Directory.CreateDirectory(samplesDir);

        var offsets = new Dictionary<string, int>();
        foreach (var s in wanted)
        {
            File.WriteAllText(Path.Combine(samplesDir, s.Id + ".cs"), SampleFile(s, out var firstBodyLine));
            offsets[s.Id] = firstBodyLine;
        }

        File.WriteAllText(Path.Combine(dir, "Runner.cs"), RunnerFile(wanted));
        File.WriteAllText(Path.Combine(dir, "generated.csproj"), Csproj(angouriMathCsproj));
        return offsets;
    }

    static string SampleFile(Snippet s, out int firstBodyLine)
    {
        var sb = new StringBuilder();
        var ownUsings = s.Body.Where(l => l.TrimStart().StartsWith("using ")
                                          && l.TrimEnd().EndsWith(";")
                                          && !l.Contains('=')).ToList();

        foreach (var u in ImpliedUsings) sb.AppendLine(u);
        foreach (var u in ownUsings.Where(u => !ImpliedUsings.Contains(u.Trim())))
            sb.AppendLine(u.Trim());
        sb.AppendLine();
        sb.AppendLine("namespace DocSamples.Generated;");
        sb.AppendLine();
        sb.AppendLine($"static class {s.Id}");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Run()");
        sb.AppendLine("    {");
        firstBodyLine = sb.ToString().Count(c => c == '\n') + 1;
        foreach (var line in s.Body)
            sb.AppendLine(ownUsings.Contains(line) ? "" : "        " + line);
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static string RunnerFile(List<Snippet> wanted)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Text.Json;
            using System.Threading;
            using System.Threading.Tasks;

            namespace DocSamples.Generated;

            record Result(string Id, string Status, string Output);

            static class Runner
            {
                // A sample that never returns must not take the run down with it: the cap is
                // far above what any documented sample needs, so hitting it is a finding.
                static readonly TimeSpan Cap = TimeSpan.FromSeconds(60);

                static int Main(string[] args)
                {
                    var results = new List<Result>();
                    var real = Console.Out;
                    foreach (var (id, run, execute) in Samples())
                    {
                        if (!execute) { results.Add(new(id, "compiled", "")); continue; }
                        var captured = new StringWriter();
                        var status = "ok";
                        Console.SetOut(captured);
                        try
                        {
                            var task = Task.Run(run);
                            if (!task.Wait(Cap)) status = "timeout";
                            else if (task.Exception is not null) throw task.Exception.InnerException;
                        }
                        catch (Exception e)
                        {
                            status = "threw " + e.GetType().Name + ": " + FirstLine(e.Message);
                        }
                        finally
                        {
                            Console.SetOut(real);
                        }
                        results.Add(new(id, status, captured.ToString()));
                        Console.Error.WriteLine($"  {id}: {status}");
                    }
                    File.WriteAllText(args.Length > 0 ? args[0] : "results.json",
                        JsonSerializer.Serialize(results));
                    return 0;
                }

                static string FirstLine(string s)
                {
                    var i = s.IndexOfAny(new[] { '\r', '\n' });
                    return i < 0 ? s : s.Substring(0, i);
                }

                static IEnumerable<(string, Action, bool)> Samples()
                {
            """);
        foreach (var s in wanted)
            sb.AppendLine($"        yield return (\"{s.Id}\", {s.Id}.Run, "
                          + (s.Mode == Mode.Run ? "true" : "false") + ");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    static string Csproj(string angouriMathCsproj) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>disable</Nullable>
            <LangVersion>preview</LangVersion>
            <AssemblyName>generated</AssemblyName>
            <RootNamespace>DocSamples.Generated</RootNamespace>
            <InvariantGlobalization>true</InvariantGlobalization>
            <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
            <!--
            A documentation sample declares things it then only prints, and says `using` for
            the reader's benefit even where the file does not need it. None of that is a
            defect in the sample, so it is not compiled as one.
            -->
            <NoWarn>CS0168;CS0219;CS8019;CS0164;CS1717;CS0162</NoWarn>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="{angouriMathCsproj}" />
          </ItemGroup>
        </Project>
        """;
}
