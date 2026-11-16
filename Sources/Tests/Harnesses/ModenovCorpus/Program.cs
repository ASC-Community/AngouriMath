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
using System.Threading;
using AngouriMath;
using AngouriMath.Extensions;

namespace ModenovCorpus
{
    /// <summary>
    /// What a row earns. <see cref="Refused"/> is the right answer to an erratum: the library did
    /// not confirm the form the book printed. <see cref="BadRow"/> is the corpus's own error, a row
    /// whose claim does not hold by value, and it fails the run whatever the baseline says.
    /// </summary>
    public enum Verdict { Solved, Refused, Unsolved, Wrong, Error, Timeout, BadRow }

    public enum Op { Zero, Equal, Solve, Erratum }

    /// <summary>One computation from Modenov's <i>Problems in Geometry</i>, as modenov.tsv records it.</summary>
    public sealed record Row(string Id, string Source, Op Op, string Var, string Input, string Expected, char Points, string Note, int Line)
    {
        public string Chapter => Id.Substring(0, Id.IndexOf('.'));
    }

    public sealed record Result(Row Row, Verdict Verdict, string Answer, string Note, long Ms);

    /// <summary>
    /// Modenov's computations as a corpus. Every row is checked twice by value: the row's own claim
    /// first, so that a mistyped identity cannot pass as the library's failure, and then the
    /// library's answer against the input it came from.
    /// https://github.com/asc-community/AngouriMath/issues/2153
    /// </summary>
    public static class Program
    {
        private static readonly TimeSpan Budget = TimeSpan.FromSeconds(20);

        public static int Main(string[] args)
        {
            var only = args.FirstOrDefault(a => a.StartsWith("--only=", StringComparison.Ordinal))?.Substring(7);
            var rows = Load(Path.Combine(ProjectDirectory(), "modenov.tsv"));
            var run = rows.Where(r => only is null || r.Id.StartsWith(only, StringComparison.Ordinal)).ToList();

            var clock = System.Diagnostics.Stopwatch.StartNew();
            var results = new List<Result>();
            foreach (var row in run)
            {
                var r = RunWithBudget(row);
                results.Add(r);
                Console.Error.WriteLine($"{r.Verdict,-8} {r.Ms,6}ms {row.Id,-16} {Trunc(row.Input, 70)}");
            }
            Report(results, clock.Elapsed);

            var failed = false;
            foreach (var r in results.Where(r => r.Verdict is Verdict.Wrong or Verdict.BadRow))
            {
                Console.WriteLine($"{r.Verdict.ToString().ToUpperInvariant()} {r.Row.Id} (modenov.tsv:{r.Row.Line}): {r.Row.Input} -> {r.Answer}: {r.Note}");
                failed = true;
            }
            if (only is not null)
            {
                Console.WriteLine("--only runs part of the corpus, so the baseline is not compared.");
                return failed ? 1 : 0;
            }

            // A wrong answer is never recorded as known: it fails the run by itself, as CasBench's does.
            var timedOut = new HashSet<string>(results.Where(r => r.Verdict == Verdict.Timeout).Select(r => r.Row.Id));
            var findings = results
                .Where(r => r.Verdict is not (Verdict.Timeout or Verdict.Wrong or Verdict.BadRow))
                .Select(r => $"{r.Row.Id}\t{r.Verdict}");
            var baseline = Harness.Baseline.Check(
                "modenov-baseline.tsv",
                "modenovcorpus: a row of modenov.tsv and the verdict it earns. A timed-out row is unmeasured and not listed.",
                findings,
                line => timedOut.Contains(line.Split('\t')[0]));
            return failed || baseline != 0 ? 1 : 0;
        }

        // ---------- the data ----------

        private static List<Row> Load(string path)
        {
            var rows = new List<Row>();
            var ids = new HashSet<string>();
            var lines = File.ReadAllLines(path);
            for (int n = 0; n < lines.Length; n++)
            {
                var line = lines[n];
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                var f = line.Split('\t');
                if (f.Length != 8)
                    throw new InvalidDataException($"modenov.tsv:{n + 1}: {f.Length} fields, not 8");
                var op = f[2] switch
                {
                    "zero" => Op.Zero,
                    "equal" => Op.Equal,
                    "solve" => Op.Solve,
                    "erratum" => Op.Erratum,
                    _ => throw new InvalidDataException($"modenov.tsv:{n + 1}: unknown op '{f[2]}'"),
                };
                if (f[6].Length != 1 || "CRPU-".IndexOf(f[6][0]) < 0)
                    throw new InvalidDataException($"modenov.tsv:{n + 1}: unknown points '{f[6]}'");
                if (!ids.Add(f[0]) || f[0].IndexOf('.') < 1)
                    throw new InvalidDataException($"modenov.tsv:{n + 1}: id '{f[0]}' is repeated or has no chapter");
                rows.Add(new Row(f[0], f[1], op, f[3], f[4], f[5], f[6][0], f[7], n + 1));
            }
            return rows;
        }

        private static string ProjectDirectory()
            => Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;

        // ---------- running ----------

        /// <summary>
        /// Each row runs on its own thread so that a hang costs the budget and not the run. The
        /// thread is abandoned, not killed: .NET cannot abort a thread.
        /// </summary>
        private static Result RunWithBudget(Row row)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Result result = null;
            var done = new ManualResetEventSlim(false);
            var t = new Thread(() =>
            {
                try { result = Run(row, sw); }
                catch (Exception e) { result = new Result(row, Verdict.Error, "", Short(e), sw.ElapsedMilliseconds); }
                finally { done.Set(); }
            }, 64 * 1024 * 1024);
            t.IsBackground = true;
            t.Start();
            if (!done.Wait(Budget))
                return new Result(row, Verdict.Timeout, "", $"no answer in {Budget.TotalSeconds:0}s", sw.ElapsedMilliseconds);
            return result;
        }

        private static Result Run(Row row, System.Diagnostics.Stopwatch sw)
        {
            Entity input, expected = null;
            try
            {
                input = row.Input.ToEntity();
                if (row.Expected != "-")
                    expected = row.Expected.ToEntity();
            }
            catch (Exception e)
            {
                return new Result(row, Verdict.BadRow, "", "does not parse: " + Short(e), sw.ElapsedMilliseconds);
            }
            return row.Op switch
            {
                Op.Zero => RunZero(row, input, sw),
                Op.Equal => RunEqual(row, input, expected, sw),
                Op.Erratum => RunErratum(row, input, expected, sw),
                Op.Solve => RunSolve(row, input, expected, sw),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>An identity: the input is 0 wherever it has a value, and Simplify is to say so.</summary>
        private static Result RunZero(Row row, Entity input, System.Diagnostics.Stopwatch sw)
        {
            var samples = Samples(row.Points, Free(input));
            var claim = Vanishes(input, samples);
            if (claim is not null)
                return new Result(row, Verdict.BadRow, "", "the row's identity does not hold: " + claim, sw.ElapsedMilliseconds);

            var simplified = input.Simplify();
            var text = simplified.Stringize();
            if (Bare(simplified) == 0)
                return new Result(row, Verdict.Solved, text, "reduced to 0", sw.ElapsedMilliseconds);
            var moved = Disagrees(simplified, input, samples);
            return moved is not null
                ? new Result(row, Verdict.Wrong, text, "Simplify changed the value: " + moved, sw.ElapsedMilliseconds)
                : new Result(row, Verdict.Unsolved, text, "0 by value, not reduced to 0", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// A reduction the book gives: Simplify of the input is to equal the book's form by value
        /// and be no larger than it, which is how CasBench decides that a target form was reached.
        /// </summary>
        private static Result RunEqual(Row row, Entity input, Entity expected, System.Diagnostics.Stopwatch sw)
        {
            var samples = Samples(row.Points, Free(input).Concat(Free(expected)));
            var claim = Vanishes(input - expected, samples);
            if (claim is not null)
                return new Result(row, Verdict.BadRow, "", "the row's equality does not hold: " + claim, sw.ElapsedMilliseconds);

            var simplified = input.Simplify();
            var text = simplified.Stringize();
            var moved = Disagrees(simplified, input, samples);
            if (moved is not null)
                return new Result(row, Verdict.Wrong, text, "Simplify changed the value: " + moved, sw.ElapsedMilliseconds);
            var bare = Bare(simplified);
            var target = expected.Simplify();
            var reached = bare == Bare(target) || bare.Nodes.Count() <= Bare(target).Nodes.Count();
            return reached
                ? new Result(row, Verdict.Solved, text, $"reached {row.Expected}", sw.ElapsedMilliseconds)
                : new Result(row, Verdict.Unsolved, text, $"equal by value, not reduced to {row.Expected}", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// An error in the book: the input is not the form the book printed. The library must not
        /// confirm the printed form, neither by reducing the difference to 0 nor by deciding the
        /// equality true; giving the right value or leaving it undecided are both correct.
        /// </summary>
        private static Result RunErratum(Row row, Entity input, Entity printed, System.Diagnostics.Stopwatch sw)
        {
            var difference = input - printed;
            var samples = Samples(row.Points, Free(difference));
            if (!DiffersSomewhere(difference, samples, out var why))
                return new Result(row, Verdict.BadRow, "", "the erratum is not confirmed by value: " + why, sw.ElapsedMilliseconds);

            var simplified = difference.Simplify();
            var text = simplified.Stringize();
            if (Bare(simplified) == 0)
                return new Result(row, Verdict.Wrong, text, "confirms the book's printed form: the difference reduces to 0", sw.ElapsedMilliseconds);
            var statement = ((Entity)MathS.Equality(input, printed)).Simplify();
            if (Bare(statement) == MathS.Boolean.True)
                return new Result(row, Verdict.Wrong, statement.Stringize(), "confirms the book's printed form: the equality simplifies to true", sw.ElapsedMilliseconds);
            var moved = Disagrees(simplified, difference, samples);
            return moved is not null
                ? new Result(row, Verdict.Wrong, text, "Simplify changed the value of the difference: " + moved, sw.ElapsedMilliseconds)
                : new Result(row, Verdict.Refused, text, "the printed form is not confirmed", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// An equation: every root returned satisfies it, at every sample of the parameters, and
        /// every root the book gives is among them. A finite set missing a root claims there is
        /// none, so it is wrong, not unsolved.
        /// </summary>
        private static Result RunSolve(Row row, Entity statement, Entity expectedSet, System.Diagnostics.Stopwatch sw)
        {
            var x = MathS.Var(row.Var);
            if (statement is not Entity.Equalsf eq || expectedSet is not Entity.Set.FiniteSet expectedRoots)
                return new Result(row, Verdict.BadRow, "", "a solve row is an equation and a finite set of roots", sw.ElapsedMilliseconds);
            var residual = eq.Left - eq.Right;
            var parameters = Free(statement).Where(v => v != x).ToArray();
            var samples = Samples(row.Points, parameters);
            // A family of roots, 2 pi n_1 + pi / 3, carries an integer of its own: the book's
            // families are bound to a few integers, the library's to a wider range around them.
            var expectedConcrete = Concrete(expectedRoots, parameters, x, 1);
            foreach (var root in expectedConcrete)
            {
                var claim = Vanishes(residual.Substitute(x, root), samples);
                if (claim is not null)
                    return new Result(row, Verdict.BadRow, "", $"the book's root {root.Stringize()} does not satisfy the equation: {claim}", sw.ElapsedMilliseconds);
            }

            var solutions = statement.Solve(x);
            var text = solutions.Stringize();
            if (solutions is not Entity.Set.FiniteSet found)
                return new Result(row, Verdict.Unsolved, text, "not a finite set; not verified", sw.ElapsedMilliseconds);

            var foundConcrete = Concrete(found, parameters, x, 4);
            foreach (var root in foundConcrete)
            {
                var fails = Vanishes(residual.Substitute(x, root), samples);
                if (fails is not null)
                    return new Result(row, Verdict.Wrong, text, $"{root.Stringize()} is not a root: {fails}", sw.ElapsedMilliseconds);
            }
            foreach (var root in expectedConcrete)
            {
                var missing = Missing(root, foundConcrete, samples);
                if (missing is not null)
                    return new Result(row, Verdict.Wrong, text, $"the root {root.Stringize()} is missing: {missing}", sw.ElapsedMilliseconds);
            }
            var size = found.Sum(r => r.Nodes.Count());
            var target = expectedRoots.Sum(r => r.Simplify().Nodes.Count());
            return size <= target
                ? new Result(row, Verdict.Solved, text, $"{found.Count} root(s) verified, complete", sw.ElapsedMilliseconds)
                : new Result(row, Verdict.Unsolved, text, $"roots verified and complete, not reduced to {row.Expected}", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// The roots of a set with each family's own integer bound to every value from -reach to
        /// reach; a root without one is itself.
        /// </summary>
        private static List<Entity> Concrete(IEnumerable<Entity> roots, Entity.Variable[] parameters, Entity.Variable x, int reach)
        {
            var concrete = new List<Entity>();
            foreach (var root in roots)
            {
                var own = Free(root).Where(v => v != x && !parameters.Contains(v)).ToArray();
                if (own.Length == 0)
                {
                    concrete.Add(root);
                    continue;
                }
                for (int n = -reach; n <= reach; n++)
                    concrete.Add(own.Aggregate(root, (r, v) => r.Substitute(v, n)));
            }
            return concrete;
        }

        // ---------- sampling ----------
        //
        // Exact rationals, so that a sample point adds no rounding of its own: abs of a point of the
        // unit pool is exactly 1 and its conjugate is exactly its reciprocal. Each pool mixes signs
        // and quadrants, and keeps away from 0, 1 and -1, where the book's denominators vanish.

        private static readonly string[] Complex =
        {
            "37/100 + 61/100*i", "-121/100 + 43/100*i", "19/10 - 7/10*i", "-53/100 - 137/100*i", "23/10 + 11/10*i",
            "-17/10 - 29/100*i", "7/9 + 13/7*i", "-5/11 + 9/4*i", "41/20 - 33/20*i", "-3/7 - 2/9*i",
            "13/8 + 1/3*i", "-29/12 + 17/10*i", "11/17 - 23/9*i", "-9/5 - 13/11*i", "31/30 + 47/20*i",
            "-1/3 + 5/6*i", "17/9 - 1/8*i",
        };

        private static readonly string[] RealPool =
        {
            "37/100", "-121/100", "19/10", "-53/100", "231/100", "-17/10", "7/9", "-5/11", "41/20", "-3/7",
            "13/8", "-29/12", "11/17", "-9/5", "31/30", "-1/3", "17/9",
        };

        private static readonly string[] Positive =
        {
            "37/100", "121/100", "19/10", "53/100", "231/100", "17/10", "7/9", "5/11", "41/20", "3/7",
            "13/8", "29/12", "11/17", "9/5", "31/30", "1/3", "17/9",
        };

        // Pythagorean triples: (a/c)^2 + (b/c)^2 = 1 exactly.
        private static readonly string[] Unit =
        {
            "3/5 + 4/5*i", "-5/13 + 12/13*i", "-8/17 - 15/17*i", "7/25 - 24/25*i", "20/29 + 21/29*i",
            "-12/37 - 35/37*i", "40/41 - 9/41*i", "-28/53 + 45/53*i", "11/61 + 60/61*i", "-56/65 - 33/65*i",
            "63/65 + 16/65*i", "-48/73 + 55/73*i", "13/85 - 84/85*i", "-77/85 - 36/85*i", "39/89 + 80/89*i",
            "-72/97 + 65/97*i", "99/101 - 20/101*i",
        };

        private static readonly Dictionary<string, Entity> Parsed = new();

        private static Entity Parse(string text)
        {
            lock (Parsed)
            {
                if (!Parsed.TryGetValue(text, out var v))
                    Parsed[text] = v = text.ToEntity().EvalNumerical();
                return v;
            }
        }

        private const int SampleCount = 6;

        /// <summary>
        /// Six assignments of the row's free variables, drawn from the pool its points field names.
        /// Within one assignment every variable gets a different value, so no difference of two
        /// variables vanishes by accident.
        /// </summary>
        private static List<Dictionary<Entity.Variable, Entity>> Samples(char points, IEnumerable<Entity.Variable> variables)
        {
            var vars = variables.Distinct().OrderBy(v => v.Name, StringComparer.Ordinal).ToArray();
            var pool = points switch { 'R' => RealPool, 'P' => Positive, 'U' => Unit, _ => Complex };
            var samples = new List<Dictionary<Entity.Variable, Entity>>();
            for (int i = 0; i < (vars.Length == 0 ? 1 : SampleCount); i++)
            {
                var s = new Dictionary<Entity.Variable, Entity>();
                for (int v = 0; v < vars.Length; v++)
                    s[vars[v]] = Parse(pool[(5 * i + v) % pool.Length]);
                samples.Add(s);
            }
            return samples;
        }

        /// <summary>
        /// The variables a value has to be given for: not a constant such as pi, and not a name a
        /// binder declares, as the index of a sum or the variable of a limit.
        /// </summary>
        private static IEnumerable<Entity.Variable> Free(Entity e) => e.FreeVariables.Where(v => !v.IsConstant);

        private static Entity Bare(Entity e)
        {
            while (e is Entity.Providedf(var inner, _))
                e = inner;
            return e;
        }

        /// <summary>
        /// A value at a sample: one number, or the entries of a vector or matrix, which the
        /// first chapters of the book compute with.
        /// </summary>
        private sealed record Value(Entity.Number.Complex[] Parts)
        {
            public double Size => Parts.Max(Magnitude);

            /// <summary>How far apart two values are; infinite when their shapes differ.</summary>
            public double From(Value other)
                => Parts.Length != other.Parts.Length
                    ? double.PositiveInfinity
                    : Parts.Zip(other.Parts, (a, b) => Magnitude((Entity.Number.Complex)(a - b).EvalNumerical())).Max();
        }

        /// <summary>The value at a sample, or null where there is none.</summary>
        private static Value At(Entity e, Dictionary<Entity.Variable, Entity> sample)
        {
            try
            {
                // A derivative is taken before its variable is given a value, which would
                // otherwise differentiate with respect to a number.
                var bare = Bare(e);
                if (bare.Nodes.Any(n => n is Entity.Derivativef))
                    bare = bare.Evaled;
                var substituted = bare.Substitute(sample);
                Entity.Number.Complex[] parts;
                if (substituted.Nodes.Any(n => n is Entity.Matrix))
                {
                    // A dot product written aT * b is a matrix expression with a number for a value.
                    var evaled = substituted.Evaled;
                    parts = evaled is Entity.Matrix m
                        ? Enumerable.Range(0, m.RowCount).SelectMany(r => Enumerable.Range(0, m.ColumnCount).Select(c => m[r, c].EvalNumerical())).ToArray()
                        : new[] { evaled.EvalNumerical() };
                }
                else
                    parts = new[] { substituted.EvalNumerical() };
                var v = new Value(parts);
                return double.IsNaN(v.Size) || double.IsInfinity(v.Size) ? null : v;
            }
            catch { return null; }
        }

        private static double Magnitude(Entity.Number.Complex c)
        {
            try
            {
                var re = c.RealPart.EDecimal.ToDouble();
                var im = c.ImaginaryPart.EDecimal.ToDouble();
                return Math.Sqrt(re * re + im * im);
            }
            catch { return double.NaN; }
        }

        /// <summary>
        /// How large the terms of a sum are at a sample, so that a cancellation is judged relative
        /// to what cancelled, not to 1.
        /// </summary>
        private static double Scale(Entity e, Dictionary<Entity.Variable, Entity> sample)
        {
            var scale = 1.0;
            var stack = new Stack<Entity>();
            stack.Push(Bare(e));
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node is Entity.Sumf or Entity.Minusf)
                {
                    foreach (var child in node.DirectChildren)
                        stack.Push(child);
                    continue;
                }
                var v = At(node, sample);
                if (v is not null)
                    scale = Math.Max(scale, v.Size);
            }
            return scale;
        }

        private const double Tolerance = 1e-8;
        private const int Required = 3;

        /// <summary>Null when the expression is 0 at every sample where it has a value, and at enough of them.</summary>
        private static string Vanishes(Entity e, List<Dictionary<Entity.Variable, Entity>> samples)
        {
            int usable = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                var v = At(e, samples[i]);
                if (v is null) continue;
                usable++;
                var m = v.Size;
                if (m > Tolerance * Scale(e, samples[i]))
                    return $"{m:0.###e+0} at sample {i}";
            }
            return usable >= Math.Min(Required, samples.Count) ? null : $"a value at only {usable} of {samples.Count} samples";
        }

        private static bool DiffersSomewhere(Entity e, List<Dictionary<Entity.Variable, Entity>> samples, out string why)
        {
            why = "0 at every sample with a value";
            for (int i = 0; i < samples.Count; i++)
            {
                var v = At(e, samples[i]);
                if (v is not null && v.Size > 1e-6 * Scale(e, samples[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Null when the answer has the input's value wherever the input has one. An answer with no
        /// value where the input has one disagrees too: it says there is nothing there.
        /// </summary>
        private static string Disagrees(Entity answer, Entity input, List<Dictionary<Entity.Variable, Entity>> samples)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                var want = At(input, samples[i]);
                if (want is null) continue;
                var got = At(answer, samples[i]);
                if (got is null)
                    return $"no value at sample {i}, where the input is {want.Size:0.###e+0} in size";
                var diff = got.From(want);
                if (diff > 1e-6 * Math.Max(Scale(input, samples[i]), want.Size))
                    return $"differs by {diff:0.###e+0} at sample {i}";
            }
            return null;
        }

        /// <summary>Null when, at every sample, the expected root's value is the value of one of the roots found.</summary>
        private static string Missing(Entity root, List<Entity> found, List<Dictionary<Entity.Variable, Entity>> samples)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                var want = At(root, samples[i]);
                if (want is null) continue;
                var hit = found.Any(r =>
                {
                    var got = At(r, samples[i]);
                    return got is not null
                        && got.From(want) <= 1e-6 * Math.Max(1, want.Size);
                });
                if (!hit)
                    return $"no root found has its value at sample {i}";
            }
            return null;
        }

        // ---------- reporting ----------

        private static void Report(List<Result> results, TimeSpan elapsed)
        {
            var verdicts = Enum.GetValues<Verdict>();
            var sb = new StringBuilder();
            sb.AppendLine("# Modenov's computations as a corpus");
            sb.AppendLine();
            sb.AppendLine($"Measured against `{Harness.Measured.Commit()}`, in {elapsed.TotalSeconds:0} s.");
            sb.AppendLine();
            sb.AppendLine($"{results.Count} rows from Modenov, *Problems in Geometry* (Mir, 1981), {Budget.TotalSeconds:0} s budget each.");
            sb.AppendLine("An identity is verified at exact sample points from its row's domain before Simplify is asked;");
            sb.AppendLine("a root is substituted back and the book's roots must all be found; an erratum must not be confirmed.");
            sb.AppendLine("https://github.com/asc-community/AngouriMath/issues/2153");
            sb.AppendLine();
            Table(sb, "Chapter", results.GroupBy(r => r.Row.Chapter), verdicts);
            Table(sb, "Operation", results.GroupBy(r => r.Row.Op.ToString().ToLowerInvariant()), verdicts);

            sb.AppendLine("## Every row");
            sb.AppendLine();
            sb.AppendLine("| Row | Source | Op | Verdict | Answer | Note | ms |");
            sb.AppendLine("|---|---|---|---|---|---|--:|");
            foreach (var r in results)
                sb.AppendLine($"| `{r.Row.Id}` | {Esc(r.Row.Source)} | {r.Row.Op.ToString().ToLowerInvariant()} | {r.Verdict} " +
                              $"| `{Esc(Trunc(r.Answer, 60))}` | {Esc(Trunc(r.Note, 80))} | {r.Ms} |");

            var path = Harness.Reports.PathFor("modenov.md");
            File.WriteAllText(path, sb.ToString());
            Console.Error.WriteLine($"\nwrote {path}");
            Console.WriteLine($"OVERALL {results.Count} rows in {elapsed.TotalSeconds:0} s: " +
                              string.Join(", ", verdicts.Select(v => $"{results.Count(r => r.Verdict == v)} {v.ToString().ToLowerInvariant()}")));
        }

        private static void Table(StringBuilder sb, string what, IEnumerable<IGrouping<string, Result>> groups, Verdict[] verdicts)
        {
            sb.AppendLine($"## By {what.ToLowerInvariant()}");
            sb.AppendLine();
            sb.AppendLine($"| {what} | Rows | {string.Join(" | ", verdicts)} |");
            sb.AppendLine($"|---|--:|{string.Concat(verdicts.Select(_ => "--:|"))}");
            foreach (var g in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
                sb.AppendLine($"| {g.Key} | {g.Count()} | {string.Join(" | ", verdicts.Select(v => g.Count(r => r.Verdict == v)))} |");
            sb.AppendLine();
        }

        private static string Short(Exception e)
        {
            var ex = e is AggregateException ae ? ae.InnerException ?? e : e;
            var m = ex.Message.Replace("\n", " ").Replace("\r", "");
            return $"{ex.GetType().Name}: {(m.Length > 90 ? m.Substring(0, 90) + "..." : m)}";
        }

        private static string Trunc(string s, int n) =>
            string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n) + "...";

        private static string Esc(string s) => (s ?? "").Replace("|", "\\|");
    }
}
