//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DocSamples;

static class Program
{
    const string WikiUrl = "https://github.com/asc-community/AngouriMath.wiki.git";

    static int Main(string[] args)
    {
        // The harness's own project directory, from the assembly rather than the working
        // directory, and the repository four levels above it.
        var root = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
        var repository = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));

        var wiki = Arg(args, "--wiki") ?? Path.Combine(root, "wiki");
        var report = Arg(args, "--report") ?? Harness.Reports.PathFor("docsamples.md");
        var sources = Path.Combine(repository, "Sources");
        var csproj = Path.Combine(sources, "AngouriMath", "AngouriMath.csproj");
        var fsproj = Path.Combine(sources, "Wrappers", "AngouriMath.FSharp", "AngouriMath.FSharp.fsproj");

        if (!Directory.Exists(wiki))
        {
            Console.WriteLine($"Cloning the wiki into {wiki}");
            if (Run("git", $"clone --depth 1 {WikiUrl} \"{wiki}\"", repository, out var cloneLog) != 0)
            {
                Console.Error.WriteLine(cloneLog);
                Console.Error.WriteLine("Could not clone the wiki. Pass --wiki=<path> to use a local copy.");
                return 2;
            }
        }
        else if (Arg(args, "--wiki") is null && Directory.Exists(Path.Combine(wiki, ".git")))
        {
            // The clone is kept between runs and brought up to date on each, so that a page
            // fixed and pushed is read as it now stands. Only the clone this harness owns is
            // touched; a path passed with --wiki belongs to the caller.
            Console.WriteLine($"Updating the wiki clone at {wiki}");
            if (Run("git", "fetch --depth 1 origin", wiki, out var fetchLog) != 0
                || Run("git", "reset --hard FETCH_HEAD", wiki, out fetchLog) != 0)
            {
                Console.Error.WriteLine(fetchLog);
                Console.Error.WriteLine(
                    "Could not update the wiki clone. Delete it, or pass --wiki=<path>.");
                return 2;
            }
        }
        if (!File.Exists(csproj))
        {
            Console.Error.WriteLine($"No library to check against at {csproj}");
            return 2;
        }

        var snippets = Extractor.FromDirectory(wiki);
        var pages = Directory.GetFiles(wiki, "*.md").Length;
        var unannotated = 0;

        // The website is a second repository, and its quickstart is the page a new reader
        // actually follows, so it is checked here too when a working copy is to hand.
        var site = Arg(args, "--site");
        if (site is not null)
        {
            var content = Path.Combine(site, "src", "content");
            var siteRoot = Directory.Exists(content) ? content : site;
            var fromSite = Extractor.FromHtmlDirectory(siteRoot, out unannotated);
            Console.WriteLine($"{fromSite.Count} annotated samples in {siteRoot}, "
                              + $"{unannotated} code blocks not annotated");
            snippets.AddRange(fromSite);
            pages += Directory.GetFiles(siteRoot, "*.html", SearchOption.AllDirectories).Length;
        }

        var csharp = snippets.Where(s => !s.IsFSharp).ToList();
        var fsharp = snippets.Where(s => s.IsFSharp).ToList();
        Console.WriteLine($"{snippets.Count} samples ({csharp.Count} C#, {fsharp.Count} F#) in "
                          + $"{pages} pages");

        var failed = new HashSet<string>();
        var diagnostics = new Dictionary<string, string>();
        var results = new Dictionary<string, RunResult>();

        if (!Measure("C#", csharp, Path.Combine(root, "generated"), "generated.csproj",
                (dir, exclude) => Generator.Write(dir, csproj, csharp, exclude),
                ByFileName, failed, diagnostics, results))
            return 2;

        if (fsharp.Any(s => s.Mode != Mode.Skip) && File.Exists(fsproj)
            && !Measure("F#", fsharp, Path.Combine(root, "generated-fsharp"), "generated-fsharp.fsproj",
                (dir, exclude) => FSharpGenerator.Write(dir, fsproj, fsharp, exclude),
                ByLineRange, failed, diagnostics, results))
            return 2;

        var findings = new List<Finding>();
        foreach (var s in snippets)
        {
            if (s.Mode == Mode.Skip)
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Skipped });
            else if (failed.Contains(s.Id))
                findings.Add(new Finding
                {
                    Snippet = s, Verdict = Verdict.CompileError,
                    Detail = diagnostics.TryGetValue(s.Id, out var d) ? d : "(no diagnostic captured)",
                });
            else if (!results.TryGetValue(s.Id, out var r))
                findings.Add(new Finding
                {
                    Snippet = s, Verdict = Verdict.Skipped,
                    Detail = "no toolchain for this language here",
                });
            else if (r.Status == "compiled")
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Compiled });
            else if (r.Status == "timeout")
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Timeout, Detail = "60 s cap" });
            else if (r.Status.StartsWith("threw"))
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Threw, Detail = r.Status });
            else if (s.Expected is null)
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Unchecked, Actual = r.Output });
            else if (Normalise(r.Output) == Normalise(s.Expected))
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.Ok, Actual = r.Output });
            else
                findings.Add(new Finding { Snippet = s, Verdict = Verdict.OutputMismatch, Actual = r.Output });
        }

        Report.Write(report, WikiUrl, repository, findings, unannotated);

        int n(Verdict v) => findings.Count(f => f.Verdict == v);
        Console.WriteLine($"docsamples: {findings.Count} samples -- "
                          + $"{n(Verdict.CompileError)} compile errors, "
                          + $"{n(Verdict.OutputMismatch)} output mismatches, "
                          + $"{n(Verdict.Threw)} threw, {n(Verdict.Timeout)} did not finish; "
                          + $"{n(Verdict.Ok)} outputs verified, {n(Verdict.Unchecked)} outputs not stated, "
                          + $"{n(Verdict.Compiled)} compile-only, {n(Verdict.Skipped)} skipped");
        Console.WriteLine($"Wrote {report}");

        // A sample that did not finish is reported and fails nothing: a shared runner is
        // slower than the machine the cap was set on.
        return n(Verdict.CompileError) + n(Verdict.OutputMismatch) + n(Verdict.Threw) == 0 ? 0 : 1;
    }

    record RunResult(string Id, string Status, string Output);

    /// <summary>Builds one language's samples, dropping what does not compile until the rest
    /// build, then runs them. A single broken sample must not stop the others from being
    /// measured, so both numbers come out of one invocation.</summary>
    static bool Measure(string language, List<Snippet> snippets, string dir, string projectFile,
        Func<string, ISet<string>, Dictionary<string, int>> generate,
        Func<Match, Dictionary<string, int>, List<Snippet>, Snippet> attribute,
        HashSet<string> failed, Dictionary<string, string> diagnostics,
        Dictionary<string, RunResult> results)
    {
        for (var pass = 1; ; pass++)
        {
            var offsets = generate(dir, failed);
            var ok = Run("dotnet", $"build -c Release \"{Path.Combine(dir, projectFile)}\"", dir, out var log) == 0;
            var round = new HashSet<string>();
            foreach (Match m in Diagnostic.Matches(log))
            {
                var s = attribute(m, offsets, snippets);
                if (s is null) continue;
                var generatedLine = int.Parse(m.Groups["line"].Value);
                var wikiLine = s.Line + (generatedLine - offsets[s.Id]) + 1;
                round.Add(s.Id);
                var text = $"{s.At(wikiLine)}: error {m.Groups["rest"].Value.Trim()}";
                var had = diagnostics.GetValueOrDefault(s.Id);
                if (had is null) diagnostics[s.Id] = text;
                else if (!had.Split('\n').Contains(text)) diagnostics[s.Id] = had + "\n" + text;
            }
            if (ok) break;
            var before = failed.Count;
            failed.UnionWith(round);
            if (failed.Count == before || pass > 20)
            {
                Console.Error.WriteLine($"The generated {language} project does not build, and "
                                        + "dropping the samples the compiler names does not help, "
                                        + "so this is the harness rather than the wiki:");
                Console.Error.WriteLine(log);
                return false;
            }
            Console.WriteLine($"{language} pass {pass}: {failed.Count(f => snippets.Any(s => s.Id == f))} "
                              + "samples do not compile");
        }

        var resultsPath = Path.Combine(dir, "results.json");
        if (File.Exists(resultsPath)) File.Delete(resultsPath);
        if (Run("dotnet", $"run -c Release --no-build --project \"{Path.Combine(dir, projectFile)}\" "
                          + $"-- \"{resultsPath}\"", dir, out var runLog) != 0 || !File.Exists(resultsPath))
        {
            Console.Error.WriteLine(runLog);
            Console.Error.WriteLine($"The {language} samples did not run to completion.");
            return false;
        }

        foreach (var r in JsonSerializer.Deserialize<List<RunResult>>(File.ReadAllText(resultsPath)))
            results[r.Id] = r;
        return true;
    }

    /// One generated file per sample, so the file name is the sample.
    static Snippet ByFileName(Match m, Dictionary<string, int> offsets, List<Snippet> snippets)
    {
        var id = Path.GetFileNameWithoutExtension(m.Groups["file"].Value.Trim());
        return offsets.ContainsKey(id) ? snippets.FirstOrDefault(s => s.Id == id) : null;
    }

    /// One generated file for all samples, so the line decides which one it is.
    static Snippet ByLineRange(Match m, Dictionary<string, int> offsets, List<Snippet> snippets)
    {
        var line = int.Parse(m.Groups["line"].Value);
        return snippets.FirstOrDefault(s => offsets.TryGetValue(s.Id, out var start)
                                            && line >= start && line < start + s.Body.Count + 1);
    }

    static readonly Regex Diagnostic = new(
        @"^(?<file>[^(\r\n]+)\((?<line>\d+),(?<col>\d+)\):\s*error\s+(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// Trailing whitespace and blank lines are formatting, not output.
    static string Normalise(string s) =>
        string.Join("\n", (s ?? "").Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()))
            .Trim('\n');

    static string Arg(string[] args, string name) =>
        args.FirstOrDefault(a => a.StartsWith(name + "="))?.Substring(name.Length + 1);

    static int Run(string file, string arguments, string cwd, out string log)
    {
        var psi = new ProcessStartInfo(file, arguments)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi);
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        log = stdout + stderr;
        return p.ExitCode;
    }
}
