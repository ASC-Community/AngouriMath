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
using System.Text.RegularExpressions;

namespace DocSamples;

enum Mode
{
    /// Compile it, run it, and -- if the page states one -- check the output.
    Run,
    /// Compile it only. For samples that throw on purpose, need input, or draw.
    CompileOnly,
    /// Not compiled. For fragments and pseudo-code.
    Skip,
}

sealed class Snippet
{
    public string Page;
    public int Index;
    public int Line;
    public string Language;
    public Mode Mode = Mode.Run;
    public string Reason = "";
    /// Lines of code, in order; a `continues` block appends to the one before it.
    public List<string> Body = new();
    /// What the page says this prints, or null where the page does not say.
    public string Expected;

    public string Id => $"{Ident(Page)}_{Index}";
    public string Where => At(Line);
    public bool IsFSharp => Language is "fs" or "fsharp";
    /// Where in the source page a line of this sample sits. Site pages are HTML, wiki pages
    /// are markdown, and the suffix is what makes the reference clickable in either.
    public string At(int line) => Page.StartsWith("site:") ? $"{Page}:{line}" : $"{Page}.md:{line}";

    public static string Ident(string page)
    {
        var sb = new StringBuilder();
        foreach (var c in page)
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }
}

/// <summary>
/// Pulls the code samples out of the wiki's markdown.
///
/// A fenced block tagged `cs` is a sample. What the page claims it prints is the next
/// bare (untagged) fence, and only when the line before that fence is exactly `Output:`
/// -- so a bare fence used for anything else is never mistaken for an expectation.
/// A sample that documents its output in trailing `// ...` comments on its
/// `Console.WriteLine` lines is read the same way, as long as every such line carries one.
///
/// Four directives, written as HTML comments so they do not render, override the default:
///
///   &lt;!-- amcheck:skip reason --&gt;      not compiled
///   &lt;!-- amcheck:compile reason --&gt;   compiled but not run
///   &lt;!-- amcheck:continues --&gt;        appended to the sample before it
///   &lt;!-- amcheck:nooutput --&gt;         the fence that follows is not an expectation
/// </summary>
static class Extractor
{
    static readonly Regex Directive = new(@"<!--\s*amcheck:(\w+)\s*(.*?)\s*-->", RegexOptions.Compiled);
    static readonly Regex OutputLead = new(@"^(output|prints|should print|will print|returns)\s*:?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex WriteLineWithComment =
        new(@"^\s*Console\.WriteLine\(.*\);\s*//\s*(?<text>.*?)\s*$", RegexOptions.Compiled);
    static readonly Regex WriteLineAny = new(@"Console\.Write(Line)?\s*\(", RegexOptions.Compiled);

    public static List<Snippet> FromDirectory(string dir)
    {
        var all = new List<Snippet>();
        foreach (var file in Directory.GetFiles(dir, "*.md").OrderBy(f => f, StringComparer.Ordinal))
            all.AddRange(FromFile(file));
        return all;
    }

    public static List<Snippet> FromFile(string path)
    {
        var page = Path.GetFileNameWithoutExtension(path);
        var lines = File.ReadAllLines(path);
        var snippets = new List<Snippet>();

        // Directives attach to the block that follows them, so they are collected as we walk.
        Mode? pendingMode = null;
        var pendingReason = "";
        var pendingContinues = false;
        var pendingNoOutput = false;
        var index = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            var m = Directive.Match(line);
            if (m.Success)
            {
                switch (m.Groups[1].Value.ToLowerInvariant())
                {
                    case "skip": pendingMode = Mode.Skip; pendingReason = m.Groups[2].Value; break;
                    case "compile": pendingMode = Mode.CompileOnly; pendingReason = m.Groups[2].Value; break;
                    case "continues": pendingContinues = true; break;
                    case "nooutput": pendingNoOutput = true; break;
                    default: throw new Exception($"{page}.md:{i + 1}: unknown directive '{m.Groups[1].Value}'");
                }
                continue;
            }

            if (!IsFence(line, out var language))
                continue;

            var open = i;
            var body = new List<string>();
            for (i++; i < lines.Length && !IsFence(lines[i], out _); i++)
                body.Add(lines[i]);

            if (language is not ("cs" or "csharp" or "fs" or "fsharp"))
            {
                // A bare fence outside a sample's expectation is prose -- output of a shell
                // command, a rendered form. Only `cs` blocks are code we can check.
                pendingMode = null; pendingReason = ""; pendingContinues = false; pendingNoOutput = false;
                continue;
            }

            var expected = pendingNoOutput ? null : FollowingOutputBlock(lines, i);
            if (expected is null && !pendingNoOutput)
                expected = OutputFromComments(body);

            if (pendingContinues && snippets.Count > 0)
            {
                var previous = snippets[^1];
                previous.Body.Add("");
                previous.Body.AddRange(body);
                // A continuation prints after everything before it, so its stated output
                // extends the expectation rather than replacing it.
                if (expected is not null)
                    previous.Expected = previous.Expected is null
                        ? expected
                        : previous.Expected + "\n" + expected;
                if (pendingMode is not null) previous.Mode = pendingMode.Value;
            }
            else
            {
                snippets.Add(new Snippet
                {
                    Page = page,
                    Index = ++index,
                    Line = open + 1,
                    Language = language,
                    Mode = pendingMode ?? Mode.Run,
                    Reason = pendingReason,
                    Body = body,
                    Expected = expected,
                });
            }

            pendingMode = null; pendingReason = ""; pendingContinues = false; pendingNoOutput = false;
        }

        return snippets;
    }

    static readonly Regex HtmlBlock = new(
        @"<!--\s*amcheck:(?<lang>cs|fs)(?<rest>[^>]*?)-->\s*<pre><code>(?<body>.*?)</code></pre>",
        RegexOptions.Compiled | RegexOptions.Singleline);
    static readonly Regex AnyHtmlBlock = new(@"<pre><code>", RegexOptions.Compiled);

    /// <summary>
    /// The website's pages are HTML, and a `pre code` block there could be C#, F#, shell or
    /// CMake with nothing to say which. So a block is checked only where the page says to,
    /// with a preceding `&lt;!-- amcheck:cs --&gt;` or `&lt;!-- amcheck:fs --&gt;`, and the count of
    /// unannotated blocks is reported rather than passed over.
    /// </summary>
    public static List<Snippet> FromHtmlDirectory(string dir, out int unannotated)
    {
        var all = new List<Snippet>();
        var annotated = 0;
        var total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.html", SearchOption.AllDirectories)
                     .OrderBy(f => f, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            total += AnyHtmlBlock.Matches(text).Count;
            var page = PageName(dir, file);
            var index = 0;
            foreach (Match m in HtmlBlock.Matches(text))
            {
                annotated++;
                all.Add(new Snippet
                {
                    Page = page,
                    Index = ++index,
                    Line = text.Take(m.Index).Count(c => c == '\n') + 1,
                    Language = m.Groups["lang"].Value,
                    Mode = m.Groups["rest"].Value.Contains("compile") ? Mode.CompileOnly : Mode.Run,
                    Reason = m.Groups["rest"].Value.Trim(),
                    Body = Decode(m.Groups["body"].Value).Split('\n').Select(l => l.TrimEnd()).ToList(),
                });
            }
        }
        unannotated = total - annotated;
        return all;
    }

    /// The page as a reader reaches it: `quickstart/index.html` is `quickstart`.
    static string PageName(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
        if (relative.EndsWith("/index.html")) relative = relative[..^"/index.html".Length];
        return "site:" + relative;
    }

    static string Decode(string body) => body
        .Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"")
        .Replace("&#39;", "'").Replace("&nbsp;", " ").Replace("&amp;", "&")
        .Replace("\r\n", "\n").Trim('\n');

    static bool IsFence(string line, out string language)
    {
        language = null;
        var t = line.TrimStart();
        if (!t.StartsWith("```")) return false;
        language = t.Substring(3).Trim().ToLowerInvariant();
        return true;
    }

    /// The bare fence after the sample, when the page introduces it with `Output:`.
    static string FollowingOutputBlock(string[] lines, int afterCloseFence)
    {
        var lead = -1;
        for (var i = afterCloseFence + 1; i < lines.Length; i++)
        {
            var t = lines[i].Trim();
            if (t.Length == 0) continue;
            if (OutputLead.IsMatch(t)) { lead = i; continue; }
            if (!IsFence(lines[i], out var language)) return null;
            if (lead < 0) return null;
            if (language.Length != 0) return null;

            var body = new List<string>();
            for (i++; i < lines.Length && !IsFence(lines[i], out _); i++)
                body.Add(lines[i].TrimEnd());
            return string.Join("\n", body).Trim('\n');
        }
        return null;
    }

    /// `Console.WriteLine(expr); // what it prints`, which the wiki uses for short outputs.
    /// Read only when every printing line carries one, so a partially annotated sample is
    /// reported as unchecked rather than checked against half its output.
    static string OutputFromComments(List<string> body)
    {
        var texts = new List<string>();
        var printers = 0;
        foreach (var line in body)
        {
            if (!WriteLineAny.IsMatch(line)) continue;
            printers++;
            var m = WriteLineWithComment.Match(line);
            if (!m.Success) return null;
            texts.Add(m.Groups["text"].Value);
        }
        return printers > 0 && texts.Count == printers ? string.Join("\n", texts) : null;
    }
}
