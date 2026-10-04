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
using System.Text;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;

namespace CrashCheck;

/// <summary>
/// Runs every case in a child process of its own, so that a crash is a result rather than the
/// end of the run.
/// </summary>
/// <remarks>
/// A stack overflow cannot be caught in .NET: it takes the process down, and a test runner that
/// dies mid-suite reports nothing about the tests it had not reached yet. `propcheck` on stock
/// master died that way in `IntegrateByPartsPolynomial`, so the suite was green and the library
/// was not. Anything that wants to find that class has to be able to lose a process and carry
/// on, which means one process per case.
///
/// Verdicts, and which of them count against the library:
///
/// - **ok** — returned.
/// - **declined** — threw an `AngouriMathBaseException`. That is the library saying it will not
///   answer, which is a legitimate answer.
/// - **unexpected** — threw anything else. A `NullReferenceException` or an
///   `IndexOutOfRangeException` out of a CAS is a defect however it is spelled.
/// - **timeout** — did not finish. Worse than declining and better than lying, and it has to be
///   visible, so it is counted.
/// - **crash** — the child died. Exit code other than the ones above, or killed by a signal:
///   stack overflow, an uncatchable failure, a runtime abort.
/// </remarks>
static class Program
{
    const int OkExit = 0, DeclinedExit = 3, UnexpectedExit = 4, TimeoutExit = 5;

    static readonly TimeSpan PerCase = TimeSpan.FromSeconds(20);
    /// The child's own cap, a little under the parent's, so that a hang is reported by the child
    /// as a timeout where it can be and by the parent only when the child is past saving.
    static readonly TimeSpan ChildCap = TimeSpan.FromSeconds(15);

    static int Main(string[] args)
    {
        var one = Arg(args, "--case");
        if (one is not null) return RunChild(one);
        if (args.Contains("--selftest")) return SelfTest();
        return RunParent(args);
    }

    /// <summary>
    /// Proves the detector can see a crash, by causing one on purpose.
    /// </summary>
    /// <remarks>
    /// "Nothing crashed" is worth nothing from a harness that cannot report a crash, and every
    /// part of that path is easy to get wrong in a way that looks like success: a swallowed
    /// exception, an exit code read as zero, a child that was never started. So the run proves
    /// itself first, on a case whose only purpose is to take its process down.
    /// </remarks>
    static int SelfTest()
    {
        var self = Path.Combine(AppContext.BaseDirectory, "crashcheck.dll");
        var (verdict, detail) = RunCase(self, new Case("deliberate", "selftest-overflow"));
        Console.WriteLine($"selftest: a deliberate stack overflow is reported as '{verdict}'"
                          + (detail.Length > 0 ? $" ({detail})" : ""));
        if (verdict == "crash") return 0;
        Console.Error.WriteLine("The detector did not see a crash it was told to cause, so a "
                                + "clean run would prove nothing. Fix this before trusting a count.");
        return 1;
    }

    static int Overflow(int depth) => Overflow(depth + 1) + Overflow(depth + 2);

    static Exception Unwrap(Exception e) =>
        e is AggregateException aggregate && aggregate.InnerExceptions.Count == 1
            ? Unwrap(aggregate.InnerException)
            : e;

    static string FirstLine(string message)
    {
        var i = message.IndexOfAny(new[] { '\r', '\n' });
        return i < 0 ? message : message.Substring(0, i);
    }

    // ---------------------------------------------------------------- the child

    /// <summary>One case, in a process that may not survive it.</summary>
    static int RunChild(string spec)
    {
        var split = spec.Split("::", 2);
        if (split.Length != 2) return UnexpectedExit;
        var (operation, expression) = (split[0], split[1]);

        // Not catchable and not meant to be: this is the case the self-test uses to show that a
        // dead child is noticed. It must run outside the try, since the point is that no handler
        // can intervene.
        if (operation == "selftest-overflow") return Overflow(0);

        try
        {
            var task = Task.Run(() => Apply(operation, expression));
            if (!task.Wait(ChildCap)) return TimeoutExit;
            return OkExit;
        }
        catch (Exception e)
        {
            // Task.Wait reports a faulted task by throwing an AggregateException, so the
            // library's own exception arrives one layer down and nothing matched it directly.
            // Unwrapping is not cosmetic: without it every legitimate refusal read as a defect.
            var thrown = Unwrap(e);

            // AngouriBugException is on the wrong side of this line from its base type. It is
            // the library saying "an internal error occurred, report it" -- so it is a finding,
            // not a refusal, however it is spelled. NotSufficientlySupportedException is the
            // one that means a known gap, and #872 moved the gaps onto it for this reason.
            if (thrown is AngouriMathBaseException and not AngouriBugException) return DeclinedExit;

            Console.Error.WriteLine(thrown.GetType().Name + ": " + FirstLine(thrown.Message));
            return UnexpectedExit;
        }
    }

    static void Apply(string operation, string source)
    {
        var expr = source.ToEntity();
        Entity ignored;
        switch (operation)
        {
            case "simplify": ignored = expr.Simplify(); break;
            case "expand": ignored = expr.Expand(); break;
            case "factorize": ignored = expr.Factorize(); break;
            case "eval": ignored = expr.Evaled; break;
            case "innersimplify": ignored = expr.InnerSimplified; break;
            case "differentiate": ignored = expr.Differentiate("x"); break;
            case "integrate": ignored = expr.Integrate("x"); break;
            case "limit": ignored = expr.Limit("x", 0); break;
            case "solve": ignored = expr.Solve("x"); break;
            case "alternate": _ = expr.Alternate(4).Take(8).ToList(); break;
            case "latexize": _ = expr.Latexize(); break;
            case "domain": ignored = expr.DomainCondition; break;
            case "stringize-roundtrip":
                var printed = expr.Stringize();
                if (printed.ToEntity() != expr)
                    throw new InvalidOperationException(
                        $"printed as `{printed}`, which parses back as something else");
                break;
            case "compile":
                try { _ = expr.Compile<double, double>("x"); }
                catch (UncompilableNodeException) { }
                break;
            default: throw new ArgumentException($"unknown operation {operation}");
        }
    }

    // ---------------------------------------------------------------- the parent

    static int RunParent(string[] args)
    {
        var report = Harness.Reports.PathFor("crashcheck.md");
        var self = Path.Combine(AppContext.BaseDirectory, "crashcheck.dll");

        var nodes = Cases.NodeInstances(out var uninstantiable);
        var expressions = nodes.Concat(Cases.WrittenShapes).Distinct().ToList();
        var only = Arg(args, "--operation");
        var operations = only is null
            ? Cases.Operations
            : Cases.Operations.Where(o => o == only).ToArray();

        var cases = (from e in expressions from o in operations select new Case(e, o)).ToList();

        Console.WriteLine($"{nodes.Count} node instances by reflection"
                          + (uninstantiable.Count > 0
                              ? $" ({uninstantiable.Count} node types could not be built: "
                                + string.Join(", ", uninstantiable) + ")"
                              : ""));
        Console.WriteLine($"{expressions.Count} expressions x {operations.Length} operations "
                          + $"= {cases.Count} cases, one child process each");

        var results = new Dictionary<Case, (string Verdict, string Detail)>();
        var done = 0;
        foreach (var c in cases)
        {
            results[c] = RunCase(self, c);
            if (++done % 200 == 0)
                Console.WriteLine($"  {done}/{cases.Count}");
        }

        Report(report, results, uninstantiable);

        int n(string verdict) => results.Values.Count(r => r.Verdict == verdict);
        // A crash or an unexpected exception fails the run. A case that did not finish is
        // reported and does not: a shared runner is slower than the machine its budget was
        // set on, so on CI that is a fact about the runner as often as about the library.
        var bad = n("crash") + n("unexpected");
        Console.WriteLine($"crashcheck: {cases.Count} cases -- {n("crash")} crashed, "
                          + $"{n("timeout")} did not finish, {n("unexpected")} threw something "
                          + $"unexpected; {n("ok")} returned, {n("declined")} declined");
        Console.WriteLine($"Wrote {report}");
        return bad == 0 ? 0 : 1;
    }

    static (string, string) RunCase(string self, Case c)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(self);
        psi.ArgumentList.Add("--case=" + c);

        using var process = Process.Start(psi);
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)PerCase.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return ("timeout", $"still running after {PerCase.TotalSeconds:0} s");
        }
        var detail = stderr.Result.Trim().Split('\n').FirstOrDefault() ?? "";
        return process.ExitCode switch
        {
            OkExit => ("ok", ""),
            DeclinedExit => ("declined", ""),
            UnexpectedExit => ("unexpected", detail),
            TimeoutExit => ("timeout", $"the case itself did not finish in {ChildCap.TotalSeconds:0} s"),
            var code => ("crash", $"the child exited with {code}"
                                  + (detail.Length > 0 ? ": " + detail : "")),
        };
    }

    static void Report(string path, Dictionary<Case, (string Verdict, string Detail)> results,
        List<string> uninstantiable)
    {
        int n(string verdict) => results.Values.Count(r => r.Verdict == verdict);
        var sb = new StringBuilder();
        sb.AppendLine("# Does anything take the process down");
        sb.AppendLine();
        sb.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
        sb.AppendLine();
        sb.AppendLine("Generated by `Sources/Tests/Harnesses/CrashCheck`. Every case runs in a child process of its "
                      + "own, because a stack overflow cannot be caught: it kills the process, and "
                      + "a test run that dies reports nothing about what it had not reached. So a "
                      + "dead child is a result here rather than the end of the run.");
        sb.AppendLine();
        sb.AppendLine("| | Cases |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Total | {results.Count} |");
        sb.AppendLine($"| Returned | {n("ok")} |");
        sb.AppendLine($"| Declined, with an `AngouriMathBaseException` other than `AngouriBugException` | {n("declined")} |");
        sb.AppendLine($"| **Crashed** | **{n("crash")}** |");
        sb.AppendLine($"| **Did not finish** | **{n("timeout")}** |");
        sb.AppendLine($"| **Threw something unexpected** | **{n("unexpected")}** |");
        sb.AppendLine();
        if (uninstantiable.Count > 0)
        {
            sb.AppendLine($"{uninstantiable.Count} node types could not be built by reflection and "
                          + "are therefore **not** covered by the generated half — "
                          + string.Join(", ", uninstantiable.Select(u => $"`{u}`"))
                          + ". Each wants either a written shape in `Cases.WrittenShapes` or a "
                          + "constructor this can reach.");
            sb.AppendLine();
        }

        foreach (var verdict in new[] { "crash", "timeout", "unexpected" })
        {
            var group = results.Where(r => r.Value.Verdict == verdict)
                .OrderBy(r => r.Key.ToString(), StringComparer.Ordinal).ToList();
            if (group.Count == 0) continue;
            sb.AppendLine($"## {verdict}");
            sb.AppendLine();
            sb.AppendLine("| Operation | Expression | What happened |");
            sb.AppendLine("|---|---|---|");
            foreach (var (c, r) in group)
                sb.AppendLine($"| `{c.Operation}` | `{c.Expression}` | {r.Detail} |");
            sb.AppendLine();
        }

        if (n("crash") + n("timeout") + n("unexpected") == 0)
            sb.AppendLine("Nothing crashed, nothing hung, and nothing threw an exception that is "
                          + "not the library declining to answer.");

        File.WriteAllText(path, sb.ToString());
    }

    static string Arg(string[] args, string name) =>
        args.FirstOrDefault(a => a.StartsWith(name + "="))?.Substring(name.Length + 1);
}
