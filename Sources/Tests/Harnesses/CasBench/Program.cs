//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Extensions;

namespace CasBench
{
    public enum Verdict { Solved, CorrectlyRefused, Unsolved, Wrong, Error, Timeout }

    public sealed record Result(Problem Problem, Verdict Verdict, string Answer, string Note, long Ms);

    public static class Program
    {
        private static readonly TimeSpan Budget = TimeSpan.FromSeconds(20);

        public static int Main(string[] args)
        {
            var only = args.FirstOrDefault(a => a.StartsWith("--only="))?.Substring(7);
            var problems = Corpus.All
                .Where(p => only is null || p.Category.StartsWith(only))
                .ToList();

            var results = new List<Result>();
            foreach (var p in problems)
            {
                var r = RunWithBudget(p);
                results.Add(r);
                Console.Error.WriteLine($"{r.Verdict,-8} {p.Category,-24} {p.Input}");
            }

            Report(results);
            // A wrong answer or an exception fails the run; an unsolved problem is coverage,
            // and a timeout is the runner's speed as much as the library's.
            return results.Any(r => r.Verdict is Verdict.Wrong or Verdict.Error) ? 1 : 0;
        }

        /// <summary>
        /// Each problem runs on its own thread so that a hang costs us the budget and
        /// not the whole run. The thread is abandoned, not killed -- .NET cannot abort
        /// threads, so a hung case leaks one thread for the rest of the process.
        /// </summary>
        private static Result RunWithBudget(Problem p)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Result result = null;
            var done = new ManualResetEventSlim(false);
            var t = new Thread(() =>
            {
                try { result = Run(p, sw); }
                catch (Exception e) { result = new Result(p, Verdict.Error, "", Short(e), sw.ElapsedMilliseconds); }
                finally { done.Set(); }
            }, 64 * 1024 * 1024);
            t.IsBackground = true;
            t.Start();
            if (!done.Wait(Budget))
                return new Result(p, Verdict.Timeout, "", $"no answer in {Budget.TotalSeconds:0}s", sw.ElapsedMilliseconds);
            return result;
        }

        private static string Short(Exception e)
        {
            var ex = e is AggregateException ae ? ae.InnerException ?? e : e;
            var m = ex.Message.Replace("\n", " ").Replace("\r", "");
            return $"{ex.GetType().Name}: {(m.Length > 90 ? m.Substring(0, 90) + "..." : m)}";
        }

        private static Result Run(Problem p, System.Diagnostics.Stopwatch sw) => p.Op switch
        {
            Op.Integrate => RunIntegral(p, sw),
            Op.Limit => RunLimit(p, sw),
            Op.Solve => RunSolve(p, sw),
            Op.Simplify => RunSimplify(p, sw),
            _ => new Result(p, Verdict.Error, "", "unsupported op", sw.ElapsedMilliseconds),
        };

        // ---------- integrals: verified by differentiating the answer back ----------
        private static Result RunIntegral(Problem p, System.Diagnostics.Stopwatch sw)
        {
            var x = MathS.Var(p.Variable);
            var integrand = p.Input.ToEntity();
            var answer = integrand.Integrate(x);
            var text = answer.Stringize();
            if (text.Contains("integral("))
                return p.NoElementaryForm
                    // Declining is the only correct answer available, so counting it against
                    // coverage would set a target nothing can reach. Ei and erfi are not
                    // elementary, and no amount of work on the integrator will make them so.
                    ? new Result(p, Verdict.CorrectlyRefused, text,
                        "no elementary antiderivative exists; left unevaluated", sw.ElapsedMilliseconds)
                    : new Result(p, Verdict.Unsolved, text, "returned unevaluated integral", sw.ElapsedMilliseconds);

            if (p.NoElementaryForm)
            {
                // No elementary answer exists, so an answer is a special function's -- Ei and erfi
                // are library functions as of 2.6, and the integral of e^x / x is Ei(x) + C -- and
                // it is checked the way every other answer is, by differentiating it back. It was
                // counted wrong unread, which reported two correct answers as wrong ones.
                var backSpecial = answer.Differentiate(x);
                var (fine, why) = NumericallyEqual(backSpecial, integrand, new[] { x });
                return new Result(p, fine ? Verdict.Solved : Verdict.Wrong, text,
                    fine ? "answered in special functions; d/dx matches the integrand"
                         : $"answered, though no elementary antiderivative exists, and d/dx does not match: {why}",
                    sw.ElapsedMilliseconds);
            }

            // d/dx of the answer must equal the integrand. Compared numerically so that
            // no particular canonical form is assumed.
            var back = answer.Differentiate(x);
            var (ok, note) = NumericallyEqual(back, integrand, new[] { x });
            return new Result(p, ok ? Verdict.Solved : Verdict.Wrong, text,
                ok ? "d/dx matches integrand" : $"d/dx does not match: {note}", sw.ElapsedMilliseconds);
        }

        // ---------- limits ----------
        private static Result RunLimit(Problem p, System.Diagnostics.Stopwatch sw)
        {
            var x = MathS.Var(p.Variable);
            var answer = p.Input.ToEntity().Limit(x, p.Approach.ToEntity());
            // Check the raw answer: an unevaluated limit(...) node simplifies to NaN,
            // which would otherwise be misreported as a wrong answer rather than a
            // missing solver.
            if (answer.Stringize().Contains("limit("))
                return new Result(p, Verdict.Unsolved, answer.Stringize(),
                    "returned unevaluated limit", sw.ElapsedMilliseconds);
            var simplified = answer.Simplify();
            var text = simplified.Stringize();
            if (text.Contains("limit("))
                return new Result(p, Verdict.Unsolved, text, "returned unevaluated limit", sw.ElapsedMilliseconds);
            if (p.Expected is null)
                return new Result(p, Verdict.Solved, text, "no expected value recorded", sw.ElapsedMilliseconds);

            var expected = p.Expected.ToEntity();
            var (ok, note) = ConstantsEqual(simplified, expected);
            return new Result(p, ok ? Verdict.Solved : Verdict.Wrong, text,
                ok ? $"= {p.Expected}" : $"expected {p.Expected} ({note})", sw.ElapsedMilliseconds);
        }

        // ---------- equations: verified by substituting the roots back ----------
        private static Result RunSolve(Problem p, System.Diagnostics.Stopwatch sw)
        {
            var x = MathS.Var(p.Variable);
            var statement = p.Input.ToEntity();
            var solutions = statement.Solve(x);
            var text = solutions.Stringize();

            if (solutions is Entity.Set.FiniteSet fs)
            {
                if (fs.Count == 0)
                    return new Result(p, Verdict.Unsolved, text, "empty solution set", sw.ElapsedMilliseconds);
                var bad = new List<string>();
                foreach (var root in fs)
                {
                    if (!RootSatisfies(statement, x, root, out var why))
                        bad.Add($"{root.Stringize()} ({why})");
                }
                return bad.Count == 0
                    ? new Result(p, Verdict.Solved, text, $"{fs.Count} root(s) verified", sw.ElapsedMilliseconds)
                    : new Result(p, Verdict.Wrong, text, "unverified: " + string.Join("; ", bad.Take(2)), sw.ElapsedMilliseconds);
            }
            // Non-finite sets (ConditionalSet etc.) are how AM expresses parametric
            // families; count as solved but flag that it was not checked.
            if (text.Contains("solve("))
                return new Result(p, Verdict.Unsolved, text, "returned unevaluated solve", sw.ElapsedMilliseconds);
            if (RestatesTheQuestion(solutions, statement))
                return new Result(p, Verdict.Unsolved, text, "restated the equation as a set builder", sw.ElapsedMilliseconds);
            return new Result(p, Verdict.Solved, text, "non-finite solution set (not verified)", sw.ElapsedMilliseconds);
        }


        /// <summary>
        /// Since #1036 a search that settled nothing answers with the equation itself as a
        /// set builder rather than with the empty set. That is the honest answer, but it is
        /// not a solution: it restates the question. Scoring it as a non-finite family would
        /// move every give-up from `unsolved` into `solved` and inflate the corpus figure
        /// without the library solving anything more.
        /// </summary>
        private static bool RestatesTheQuestion(Entity solutions, Entity statement)
        {
            if (solutions is not Entity.Set.ConditionalSet cs)
                return false;
            try
            {
                return cs.Predicate.InnerSimplified == statement.InnerSimplified;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A root may still carry free parameters -- AngouriMath expresses periodic
        /// solution families as e.g. `2 * pi * n_1`. Those are legitimate answers, so
        /// bind each parameter to a few concrete values and require the root to check
        /// out for all of them, rather than reporting the family as unverifiable.
        /// </summary>
        private static readonly Entity[] ParameterBindings = { 0, 1, -1, 2 };

        private static bool RootSatisfies(Entity statement, Entity.Variable x, Entity root, out string why)
        {
            var parameters = root.Vars.Where(v => v != x).ToArray();
            if (parameters.Length > 0)
            {
                why = "";
                foreach (var binding in ParameterBindings)
                {
                    var bound = root;
                    foreach (var param in parameters)
                        bound = bound.Substitute(param, binding);
                    if (!RootSatisfiesConcrete(statement, x, bound, out var reason))
                    {
                        why = $"with {string.Join(",", parameters.Select(v => v.Name))}={binding.Stringize()}: {reason}";
                        return false;
                    }
                }
                return true;
            }
            return RootSatisfiesConcrete(statement, x, root, out why);
        }

        private static bool RootSatisfiesConcrete(Entity statement, Entity.Variable x, Entity root, out string why)
        {
            why = "";
            try
            {
                // Numeric residual first. Roots found numerically (Lambert-W branches,
                // Newton results) carry ~1e-15 error, which is above the library's own
                // 1e-16 zero threshold, so Simplify() reports them as False even though
                // they are correct. The residual is the honest test; symbolic
                // simplification is only the fallback.
                if (statement is Entity.Equalsf eq)
                {
                    var residual = (eq.Left - eq.Right).Substitute(x, root);
                    try
                    {
                        var mag = Magnitude(residual.EvalNumerical());
                        if (!double.IsNaN(mag))
                        {
                            if (mag < 1e-8) return true;
                            why = $"residual {mag:0.###e+0}";
                            return false;
                        }
                    }
                    catch { /* fall through to the symbolic check */ }
                }

                // An equality substitutes to a boolean once both sides are constant.
                var simplified = statement.Substitute(x, root).Simplify();
                if (simplified == MathS.Boolean.True) return true;
                if (simplified == MathS.Boolean.False) { why = "substitutes to false"; return false; }
                why = $"did not reduce ({simplified.Stringize()})";
                return false;
            }
            catch (Exception e) { why = Short(e); return false; }
        }

        // ---------- simplification ----------
        private static Result RunSimplify(Problem p, System.Diagnostics.Stopwatch sw)
        {
            var simplified = p.Input.ToEntity().Simplify();
            var text = simplified.Stringize();
            var expected = p.Expected.ToEntity();

            var vars = simplified.Vars.Concat(expected.Vars).Distinct().ToArray();
            var (equivalent, note) = vars.Length == 0
                ? ConstantsEqual(simplified, expected)
                : NumericallyEqual(simplified, expected, vars);

            if (!equivalent)
                return new Result(p, Verdict.Wrong, text, $"not equivalent to {p.Expected}: {note}", sw.ElapsedMilliseconds);

            // Equivalent but did it actually reach the target form? Structural equality is
            // too strict to answer that. Simplify is not canonical over commutation --
            // sqrt(12) + sqrt(27) reduces to sqrt(3) * 5 while 5 * sqrt(3) stays as written,
            // and (x^2 - 1) / (x - 1) reduces to x + 1 while x + 1 itself becomes 1 + x --
            // and a correct answer may carry a domain condition the target form omits, as
            // (x^2 - 1) / (x - 1) is genuinely undefined at 1. Since equivalence has already
            // been established numerically, an answer that is no larger than the target has
            // reached it whichever way round it is written.
            var bare = simplified;
            while (bare is Entity.Providedf(var inner, _)) bare = inner;
            var reached = bare == expected.Simplify() || text == p.Expected
                          || bare.Nodes.Count() <= expected.Simplify().Nodes.Count();
            return reached
                ? new Result(p, Verdict.Solved, text, $"reached {p.Expected}", sw.ElapsedMilliseconds)
                : new Result(p, Verdict.Unsolved, text, $"equivalent but not reduced to {p.Expected}", sw.ElapsedMilliseconds);
        }

        // ---------- numeric machinery ----------
        // Positive reals. Complex sample points cannot be used here: correct
        // antiderivatives routinely contain abs() (e.g. int 1/x = ln|x|), which is not
        // holomorphic, so off the real line d/dx of a correct answer genuinely differs
        // from the integrand. Positive reals also keep ln and sqrt in-domain.
        private static readonly double[] SamplePoints = { 0.37, 1.31, 2.17, 0.59, 3.43 };

        // Free variables other than the one being integrated/solved over -- including
        // the constant of integration AngouriMath appends -- are bound to fixed
        // positive reals, consistently on both sides of the comparison.
        private static readonly double[] ParameterValues = { 1.7, 2.3, 2.9, 1.3, 3.1 };

        private static double Magnitude(Entity.Number.Complex c)
        {
            try
            {
                var re = (double)c.RealPart.EDecimal.ToDouble();
                var im = (double)c.ImaginaryPart.EDecimal.ToDouble();
                return Math.Sqrt(re * re + im * im);
            }
            catch { return double.NaN; }
        }

        /// <summary>
        /// Substitutes complex sample points for every free variable and compares.
        /// Complex points are used deliberately: they keep sqrt/ln of negative
        /// arguments in-domain, so a domain accident is not misreported as a wrong answer.
        /// </summary>
        private static Entity Num(double d) =>
            (Entity)(Entity.Number.Real)PeterO.Numbers.EDecimal.FromDouble(d);

        private static (bool, string) NumericallyEqual(Entity a, Entity b, Entity.Variable[] vars)
        {
            // Bind every free variable appearing on either side, not just the listed
            // ones, so a leftover constant of integration cannot poison the evaluation.
            var others = a.Vars.Concat(b.Vars).Distinct()
                .Where(v => !vars.Contains(v))
                .OrderBy(v => v.Name)
                .ToArray();

            int agree = 0, usable = 0;
            string last = "no usable sample point";
            for (int i = 0; i < SamplePoints.Length; i++)
            {
                Entity ea = a, eb = b;
                for (int v = 0; v < vars.Length; v++)
                {
                    var value = Num(SamplePoints[(i + v) % SamplePoints.Length]);
                    ea = ea.Substitute(vars[v], value);
                    eb = eb.Substitute(vars[v], value);
                }
                for (int v = 0; v < others.Length; v++)
                {
                    var value = Num(ParameterValues[v % ParameterValues.Length]);
                    ea = ea.Substitute(others[v], value);
                    eb = eb.Substitute(others[v], value);
                }
                try
                {
                    var va = ea.EvalNumerical();
                    var vb = eb.EvalNumerical();
                    var diff = Magnitude((Entity.Number.Complex)(va - vb));
                    var scale = Math.Max(1.0, Math.Max(Magnitude(va), Magnitude(vb)));
                    if (double.IsNaN(diff)) { last = "NaN at sample point"; continue; }
                    usable++;
                    if (diff / scale < 1e-6) agree++;
                    else last = $"differs by {diff:0.###e+0} at sample {i}";
                }
                catch (Exception e) { last = Short(e); }
            }
            if (usable == 0) return (false, last);
            // An antiderivative differs from the integrand's true antiderivative by a
            // constant only; d/dx removes that, so full agreement is the right bar.
            return (agree == usable, agree == usable ? "" : last);
        }

        private static (bool, string) ConstantsEqual(Entity a, Entity b)
        {
            try
            {
                if (a == b) return (true, "");
                var at = a.Stringize();
                var bt = b.Stringize();
                if (at == bt) return (true, "");
                // Infinities cannot be subtracted meaningfully.
                if (at.Contains("oo") || bt.Contains("oo")) return (at == bt, $"got {at}");
                var diff = (a - b).EvalNumerical();
                var mag = Magnitude(diff);
                if (double.IsNaN(mag)) return (false, "NaN difference");
                return (mag < 1e-8, mag < 1e-8 ? "" : $"differs by {mag:0.###e+0}");
            }
            catch (Exception e) { return (false, Short(e)); }
        }

        // ---------- reporting ----------
        private static void Report(List<Result> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# AngouriMath solver coverage");
            sb.AppendLine();
            sb.AppendLine($"Measured against `{Harness.Measured.Commit()}`.");
            sb.AppendLine();
            sb.AppendLine($"Corpus of {results.Count} problems, {Budget.TotalSeconds:0}s budget each.");
            sb.AppendLine("Integrals are verified by differentiating the answer; equations by substituting");
            sb.AppendLine("the roots back; limits and simplifications against a recorded expected value.");
            sb.AppendLine();

            sb.AppendLine("## By category");
            sb.AppendLine();
            sb.AppendLine("| Category | Solved | Refused | Unsolved | Wrong | Error | Timeout | Rate |");
            sb.AppendLine("|---|--:|--:|--:|--:|--:|--:|--:|");
            foreach (var g in results.GroupBy(r => r.Problem.Category).OrderBy(g => g.Key))
            {
                int s = g.Count(r => r.Verdict == Verdict.Solved);
                int refused = g.Count(r => r.Verdict == Verdict.CorrectlyRefused);
                // The rate is out of what an answer exists for. A problem with no elementary
                // answer cannot be solved by anyone, so leaving it in the denominator would
                // measure the corpus rather than the library.
                int answerable = g.Count() - refused;
                sb.AppendLine($"| `{g.Key}` | {s} | {refused} | {g.Count(r => r.Verdict == Verdict.Unsolved)} " +
                              $"| {g.Count(r => r.Verdict == Verdict.Wrong)} | {g.Count(r => r.Verdict == Verdict.Error)} " +
                              $"| {g.Count(r => r.Verdict == Verdict.Timeout)} " +
                              $"| {(answerable == 0 ? "n/a" : $"{100.0 * s / answerable:0}%")} |");
            }
            sb.AppendLine();
            int total = results.Count, solved = results.Count(r => r.Verdict == Verdict.Solved);
            int correctlyRefused = results.Count(r => r.Verdict == Verdict.CorrectlyRefused);
            int answerableTotal = total - correctlyRefused;
            sb.AppendLine($"**Overall: {solved}/{answerableTotal} ({100.0 * solved / answerableTotal:0.0}%) " +
                          $"of the problems that have an elementary answer**");
            sb.AppendLine();
            sb.AppendLine($"- Correctly refused, no elementary form exists: {correctlyRefused} " +
                          $"(counted out of the denominator, not against it)");
            sb.AppendLine($"- Wrong answers: {results.Count(r => r.Verdict == Verdict.Wrong)}");
            sb.AppendLine($"- Errors: {results.Count(r => r.Verdict == Verdict.Error)}");
            sb.AppendLine($"- Timeouts: {results.Count(r => r.Verdict == Verdict.Timeout)}");
            sb.AppendLine();

            sb.AppendLine("## Every problem");
            sb.AppendLine();
            sb.AppendLine("| Category | Problem | Verdict | Answer | Note | ms |");
            sb.AppendLine("|---|---|---|---|---|--:|");
            foreach (var r in results)
            {
                var input = r.Problem.Op == Op.Limit
                    ? $"lim {r.Problem.Variable}->{r.Problem.Approach} ({r.Problem.Input})"
                    : r.Problem.Op == Op.Integrate
                        ? $"∫ ({r.Problem.Input}) d{r.Problem.Variable}"
                        : r.Problem.Input;
                sb.AppendLine($"| `{r.Problem.Category}` | `{Esc(input)}` | {r.Verdict} | `{Esc(Trunc(r.Answer, 60))}` " +
                              $"| {Esc(Trunc(r.Note, 70))} | {r.Ms} |");
            }

            // Where Harness.Reports says, whichever directory the run was started from.
            var path = Harness.Reports.PathFor("coverage.md");
            System.IO.File.WriteAllText(path, sb.ToString());
            Console.Error.WriteLine($"\nwrote {path}");
            Console.WriteLine($"OVERALL {solved}/{answerableTotal} solved of those with an answer to give; " +
                              $"{results.Count(r => r.Verdict == Verdict.Unsolved)} unsolved, " +
                              $"{correctlyRefused} correctly refused, " +
                              $"{results.Count(r => r.Verdict == Verdict.Wrong)} wrong, " +
                              $"{results.Count(r => r.Verdict == Verdict.Error)} error, " +
                              $"{results.Count(r => r.Verdict == Verdict.Timeout)} timeout");
        }

        private static string Trunc(string s, int n) =>
            string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n) + "...";
        private static string Esc(string s) => (s ?? "").Replace("|", "\\|");
    }
}
