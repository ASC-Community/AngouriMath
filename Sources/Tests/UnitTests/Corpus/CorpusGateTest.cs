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
using Xunit;
using Xunit.Abstractions;

namespace AngouriMath.Tests.Corpus
{
    /// <summary>
    /// Runs <see cref="Corpus"/> and fails where the library got <b>worse</b> — or where it got
    /// better and the record was not updated to say so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The answers are checked rather than compared to a stored string. A solution is substituted
    /// back into the equation, an antiderivative is differentiated back, a simplification is
    /// evaluated against the expression it came from. That means the corpus does not have to
    /// record what the library happens to print today, so a change in form is not a failure and
    /// only a change in <i>value</i> is.
    /// </para>
    /// <para>
    /// Checking is numeric at sampled points, and the points are complex and away from the axes
    /// on purpose: a rule that is wrong off the real line is the kind this library keeps finding,
    /// and real sample points would not see it.
    /// </para>
    /// </remarks>
    [Trait("Area", "Corpus")]
    public sealed class CorpusGateTest
    {
        private readonly ITestOutputHelper output;

        public CorpusGateTest(ITestOutputHelper output) => this.output = output;

        /// <summary>Generous: this is a gate against regression, not a performance measurement.</summary>
        private static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

        /// <summary>Away from the real axis, and away from the small integers.</summary>
        private static readonly System.Numerics.Complex[] Samples =
        {
            new(0.3721, 0.5813), new(-0.7211, 0.2917), new(1.4523, -0.6131), new(-1.1907, -0.8419),
        };

        /// <summary>Positive and away from 1, for the antiderivative check. See CheckIntegrate.</summary>
        private static readonly System.Numerics.Complex[] RealSamples =
        {
            new(0.4137, 0), new(1.7219, 0), new(2.9041, 0), new(0.8353, 0),
        };

        /// <summary>Values given to a parameter that a root is stated in terms of.</summary>
        private static readonly int[] ParameterValues = { 0, 1, -1, 2 };

        /// <summary>
        /// The root itself where it names only <c>x</c>, or one root per value of each parameter
        /// it is stated in terms of.
        /// </summary>
        private static IEnumerable<Entity> Instantiate(Entity root)
        {
            var parameters = root.Vars.Where(v => v.Name != "x").ToList();
            if (parameters.Count == 0)
            {
                yield return root;
                yield break;
            }
            foreach (var value in ParameterValues)
            {
                var instance = root;
                foreach (var parameter in parameters)
                    instance = instance.Substitute(parameter, value);
                yield return instance;
            }
        }

        private const double Tolerance = 1e-6;

        [Fact]
        public void TheCorpusHasNotGotWorse()
        {
            var clock = Stopwatch.StartNew();
            var results = Corpus.All.Select(Run).ToList();
            clock.Stop();
            output.WriteLine(Report(results));

            // Written before anything is asserted, so that a run which then fails still leaves
            // the numbers behind. This is the file the workflow uploads.
            var rows = results
                .Select(r => new CorpusRecord.ReportRow(
                    r.Problem.Name, r.Problem.Op, r.Problem.Expect, r.Verdict,
                    Status(r.Problem.Expect, r.Verdict), r.Note, r.Answer))
                .ToList();
            File.WriteAllText(CorpusRecord.ReportPath,
                              CorpusRecord.RenderReport(rows, clock.ElapsedMilliseconds));
            output.WriteLine("Report written to " + CorpusRecord.ReportPath);

            var moved = results.Zip(rows, (result, row) => (result, row.Status))
                               .Where(pair => pair.Status is not "ok")
                               .ToList();
            if (moved.Count == 0) return;

            // The four kinds are spelled out separately, and counted on the first line, because
            // they ask the reader to do different things: a message that does not say which is
            // a message that gets the record updated over a real regression.
            var complaint = new StringBuilder();
            var kinds = new[] { "wrong", "worse", "better", "changed" };
            complaint.AppendLine("The corpus moved: " + string.Join(", ", kinds
                .Select(kind => (kind, n: moved.Count(pair => pair.Status == kind)))
                .Where(pair => pair.n > 0)
                .Select(pair => $"{pair.n} {pair.kind}")) + ".");
            complaint.AppendLine();

            foreach (var (r, _) in moved.Where(pair => pair.Status is "wrong"))
                complaint.AppendLine(
                    $"WRONG    {r.Problem.Name}: {r.Problem.Input} -> {r.Answer} ({r.Note})."
                    + " An answer that is not right is worse than no answer, so this fails"
                    + " whatever the record says. Do not record it: NoEntryExpectsAWrongAnswer"
                    + " rejects Verdict.Wrong as an expectation.");
            foreach (var (r, _) in moved.Where(pair => pair.Status is "worse"))
                complaint.AppendLine(
                    $"WORSE    {r.Problem.Name}: recorded {r.Problem.Expect}, measured {r.Verdict}"
                    + $" ({r.Note}). THE LIBRARY GOT WORSE HERE. Fix it. Only lower the record if"
                    + " the old verdict is shown to have been wrong, and say why in the commit.");
            foreach (var (r, _) in moved.Where(pair => pair.Status is "better"))
                complaint.AppendLine(
                    $"BETTER   {r.Problem.Name}: recorded {r.Problem.Expect}, measured {r.Verdict}"
                    + $" ({r.Note}). NOTHING IS BROKEN -- the library got better and the record"
                    + $" does not say so. Set Expect = Verdict.{r.Verdict} on this problem in"
                    + $" Corpus.cs, re-run with {CorpusRecord.UpdateVariable}=1 to refresh"
                    + $" {CorpusRecord.BaselineFileName}, and commit both.");
            foreach (var (r, _) in moved.Where(pair => pair.Status is "changed"))
                complaint.AppendLine(
                    $"CHANGED  {r.Problem.Name}: recorded {r.Problem.Expect}, measured {r.Verdict}"
                    + $" ({r.Note}). Neither better nor worse -- both are the library producing"
                    + " nothing, and there is no defensible ordering between throwing and"
                    + " hanging. Look at it, then record it the way an improvement is recorded.");

            Assert.True(false, "\n" + complaint + "\n" + Report(results));
        }

        /// <summary>
        /// The record and the measurement compared, in the vocabulary the failure message uses.
        /// </summary>
        private static string Status(Verdict recorded, Verdict measured) =>
            // Checked before equality: Expect can never be Wrong, so a wrong answer is always
            // also a regression, and calling it "wrong" keeps the worst category its own.
            measured is Verdict.Wrong ? "wrong"
            : measured == recorded ? "ok"
            : CorpusRecord.Rank(measured) > CorpusRecord.Rank(recorded) ? "worse"
            : CorpusRecord.Rank(measured) < CorpusRecord.Rank(recorded) ? "better"
            : "changed";

        /// <summary>
        /// The committed record is a projection of <see cref="Corpus.All"/>, and this checks it is
        /// current. It says nothing about the library — that is what
        /// <see cref="TheCorpusHasNotGotWorse"/> is for — and it exists so that the corpus has a
        /// machine-readable history that outlives a job log.
        /// </summary>
        [Fact]
        public void TheBaselineIsTheOneOnRecord()
        {
            var rendered = CorpusRecord.RenderBaseline(Corpus.All);

            if (Environment.GetEnvironmentVariable(CorpusRecord.UpdateVariable) is "1")
            {
                // Written next to the sources, not to the output directory: the output copy is
                // the build's, and the next build would overwrite it.
                File.WriteAllText(CorpusRecord.BaselineSourcePath, rendered);
                output.WriteLine("Rewrote " + CorpusRecord.BaselineSourcePath);
                return;
            }

            Assert.True(File.Exists(CorpusRecord.BaselinePath),
                $"No corpus baseline at {CorpusRecord.BaselinePath}. Re-run the suite with"
                + $" {CorpusRecord.UpdateVariable}=1 to write"
                + $" {CorpusRecord.BaselineSourcePath}, then commit it.");

            var recorded = CorpusRecord.Lines(File.ReadAllText(CorpusRecord.BaselinePath));
            var measured = CorpusRecord.Lines(rendered);
            if (recorded.SequenceEqual(measured, StringComparer.Ordinal))
                return;

            var message = new StringBuilder();
            message.AppendLine();
            message.AppendLine($"{CorpusRecord.BaselineFileName} is not what Corpus.cs says.");
            message.AppendLine("It is generated, so this is never a reason to edit it by hand:"
                               + $" re-run with {CorpusRecord.UpdateVariable}=1 and commit the result.");
            Describe(message, "ON RECORD but no longer true", recorded.Except(measured, StringComparer.Ordinal).ToList());
            Describe(message, "TRUE but not on record", measured.Except(recorded, StringComparer.Ordinal).ToList());
            Assert.True(false, message.ToString());
        }

        private static void Describe(StringBuilder into, string heading, IReadOnlyList<string> lines)
        {
            if (lines.Count == 0) return;
            into.AppendLine().AppendLine($"{heading} ({lines.Count}):");
            foreach (var line in lines.Take(40))
                into.AppendLine("  " + line);
            if (lines.Count > 40)
                into.AppendLine($"  ... and {lines.Count - 40} more");
        }

        /// <summary>
        /// A corpus entry may not record a wrong answer as acceptable. Recording one would turn
        /// the gate into a description of the defect rather than a check against it.
        /// </summary>
        [Fact]
        public void NoEntryExpectsAWrongAnswer()
            => Assert.Empty(Corpus.All.Where(p => p.Expect == Verdict.Wrong));

        private sealed record Result(Problem Problem, Verdict Verdict, string Answer, string Note);

        private static Result Run(Problem problem)
        {
            try
            {
                var task = Task.Run(() => Attempt(problem));
                return task.Wait(Budget)
                    ? task.Result
                    : new Result(problem, Verdict.Timeout, "", $"no answer in {Budget.TotalSeconds:0}s");
            }
            catch (AggregateException e) when (e.InnerException is { } inner)
            {
                // The library declining is not an error; anything else is.
                return inner is AngouriMathBaseException and not AngouriBugException
                    ? new Result(problem, Verdict.Unsolved, "", Short(inner))
                    : new Result(problem, Verdict.Error, "", Short(inner));
            }
            catch (Exception e)
            {
                return new Result(problem, Verdict.Error, "", Short(e));
            }
        }

        private static Result Attempt(Problem problem) => problem.Op switch
        {
            Op.Simplify => CheckSimplify(problem),
            Op.Solve => CheckSolve(problem),
            Op.Integrate => CheckIntegrate(problem),
            Op.Limit => CheckLimit(problem),
            _ => new Result(problem, Verdict.Error, "", "unsupported operation"),
        };

        private static Result CheckSimplify(Problem problem)
        {
            var input = problem.Input.ToEntity();
            var simplified = input.Simplify();
            return AgreeAtSamples(input, simplified, out var note)
                ? new Result(problem, Verdict.Solved, simplified.Stringize(), note)
                : new Result(problem, Verdict.Wrong, simplified.Stringize(), note);
        }

        /// <summary>
        /// The roots an answer lists: the members of a finite set, and of an indexed union each
        /// member at the values of <see cref="ParameterValues"/> its index set has, the family
        /// <c>union({ 2 pi n }, n in ZZ)</c> at <c>n = 0, 1, -1, 2</c>; <see langword="null"/>
        /// for any other set.
        /// </summary>
        private static List<Entity>? Listed(Entity roots)
        {
            switch (roots)
            {
                case Entity.Set.FiniteSet finite:
                    return finite.ToList();
                case Entity.Set.Unionf(var left, var right):
                    return Listed(left) is { } first && Listed(right) is { } second ? first.Concat(second).ToList() : null;
                case Entity.Set.IndexedUnionf family:
                    var members = new List<Entity>();
                    foreach (var value in ParameterValues)
                        if (((Entity)value).In(family.Over).Simplify() == Entity.Boolean.True)
                        {
                            if (Listed(family.Body.Substitute(family.Var, value)) is not { } each)
                                return null;
                            members.AddRange(each);
                        }
                    return members;
                default:
                    return null;
            }
        }

        private static Result CheckSolve(Problem problem)
        {
            var input = problem.Input.ToEntity();
            var roots = input.SolveEquation("x");
            if (Listed(roots) is not { } finite)
                return new Result(problem, Verdict.Unsolved, roots.Stringize(), "not a finite root set or a family");
            if (finite.Count == 0)
                return new Result(problem, Verdict.Unsolved, roots.Stringize(), "no roots found");

            var checkedRoots = 0;
            foreach (var root in finite)
            {
                // A root may carry a parameter -- sin(x) = 0 is solved by 2*pi*n, for every
                // integer n -- and such a root is right rather than unverifiable. Each parameter
                // is given a few integer values and the root is checked at each of them, which
                // also checks that the parameterisation itself is right and not only one branch
                // of it.
                foreach (var instance in Instantiate(root))
                {
                    if (!TryEvaluate(input.Substitute("x", instance), out var residual))
                        return new Result(problem, Verdict.Unsolved, roots.Stringize(),
                            $"root {instance.Stringize()} could not be checked");
                    if (residual.Magnitude > Tolerance)
                        return new Result(problem, Verdict.Wrong, roots.Stringize(),
                            $"root {instance.Stringize()} leaves {residual.Magnitude:0.###e+0}");
                    checkedRoots++;
                }
            }
            return new Result(problem, Verdict.Solved, roots.Stringize(), $"{checkedRoots} root(s) verified");
        }

        /// <summary>
        /// An antiderivative, checked by differentiating it back.
        /// </summary>
        /// <remarks>
        /// Sampled on the <b>positive reals</b> rather than off the axis, unlike everything else
        /// here, because the antiderivatives this library returns are the real ones: the integral
        /// of <c>1/x</c> comes back as <c>ln(abs(x)) + C</c>, and <c>abs</c> is not holomorphic,
        /// so differentiating that does not reproduce <c>1/x</c> away from the real line. Whether
        /// <c>ln(x)</c> would be the better answer is a real question and not one a regression
        /// gate should decide by failing every build until someone settles it.
        /// </remarks>
        private static Result CheckIntegrate(Problem problem)
        {
            var input = problem.Input.ToEntity();
            var integral = input.Integrate("x");
            if (integral.Nodes.Any(node => node is Entity.Integralf))
                return new Result(problem, Verdict.Unsolved, integral.Stringize(), "integral remains");
            var back = integral.Differentiate("x");
            return AgreeAtSamples(input, back, RealSamples, out var note)
                ? new Result(problem, Verdict.Solved, integral.Stringize(), "differentiates back")
                : new Result(problem, Verdict.Wrong, integral.Stringize(), note);
        }

        private static Result CheckLimit(Problem problem)
        {
            var input = problem.Input.ToEntity();
            var limit = input.Limit("x", problem.Approach!.ToEntity());
            if (limit.Nodes.Any(node => node is Entity.Limitf))
                return new Result(problem, Verdict.Unsolved, limit.Stringize(), "limit remains");

            var expected = problem.Expected!.ToEntity();
            if (limit.Simplify() == expected.Simplify())
                return new Result(problem, Verdict.Solved, limit.Stringize(), $"reached {problem.Expected}");
            // Infinities do not compare numerically, so a difference of forms is all there is.
            if (!TryEvaluate(limit, out var got) || !TryEvaluate(expected, out var want))
                return new Result(problem, Verdict.Wrong, limit.Stringize(), $"expected {problem.Expected}");
            return (got - want).Magnitude <= Tolerance
                ? new Result(problem, Verdict.Solved, limit.Stringize(), $"reached {problem.Expected}")
                : new Result(problem, Verdict.Wrong, limit.Stringize(), $"expected {problem.Expected}");
        }

        /// <summary>
        /// Whether two expressions take the same value at the sample points, over the variables
        /// they mention.
        /// </summary>
        /// <remarks>
        /// A point where <b>both</b> are undefined proves nothing and is skipped; a point where
        /// one has a value and the other does not is a disagreement, which is how a rewrite that
        /// quietly widens or narrows a domain gets caught.
        /// </remarks>
        private static bool AgreeAtSamples(Entity left, Entity right, out string note)
            => AgreeAtSamples(left, right, Samples, out note);

        private static bool AgreeAtSamples(
            Entity left, Entity right, System.Numerics.Complex[] points, out string note)
        {
            var variables = left.Vars.Concat(right.Vars).Distinct().ToList();
            var compared = 0;
            for (var i = 0; i < points.Length; i++)
            {
                Entity l = left, r = right;
                foreach (var (variable, index) in variables.Select((v, n) => (v, n)))
                {
                    var point = points[(i + index) % points.Length];
                    l = l.Substitute(variable, point);
                    r = r.Substitute(variable, point);
                }
                var okL = TryEvaluate(l, out var valueL);
                var okR = TryEvaluate(r, out var valueR);
                if (!okL && !okR) continue;
                if (okL != okR)
                {
                    note = $"one side is undefined at sample {i}";
                    return false;
                }
                compared++;
                var scale = Math.Max(1, Math.Max(valueL.Magnitude, valueR.Magnitude));
                if ((valueL - valueR).Magnitude / scale > Tolerance)
                {
                    note = $"differ at sample {i} by {(valueL - valueR).Magnitude:0.###e+0}";
                    return false;
                }
            }
            note = compared == 0 ? "undefined at every sample" : $"agree at {compared} point(s)";
            return true;
        }

        private static bool TryEvaluate(Entity expr, out System.Numerics.Complex value)
        {
            value = default;
            try
            {
                if (!expr.EvaluableNumerical) return false;
                var evaluated = expr.EvalNumerical();
                value = (System.Numerics.Complex)evaluated;
                return !double.IsNaN(value.Real) && !double.IsNaN(value.Imaginary);
            }
            catch (AngouriMathBaseException)
            {
                return false;
            }
        }

        private static string Short(Exception e)
        {
            var text = e.Message.Replace('\n', ' ');
            return e.GetType().Name + ": " + (text.Length > 90 ? text[..90] : text);
        }

        private static string Report(IReadOnlyList<Result> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var group in results.GroupBy(r => r.Problem.Op))
            {
                sb.AppendLine($"{group.Key}:");
                foreach (var r in group)
                    sb.AppendLine($"  {r.Verdict,-8} {r.Problem.Name,-26} {r.Note}");
            }
            sb.AppendLine();
            foreach (var verdict in Enum.GetValues(typeof(Verdict)).Cast<Verdict>())
                sb.Append($"{verdict}={results.Count(r => r.Verdict == verdict)}  ");
            sb.AppendLine($"of {results.Count}");
            return sb.ToString();
        }
    }
}
