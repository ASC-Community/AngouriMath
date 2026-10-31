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
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// Every worked example in the XML documentation, compiled, run, and its output compared with
    /// the output it says it prints.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The guarantee <see cref="ExtensionsExampleTest"/> gives one namespace, given to all of
    /// them.</b> That class says it exists so that "a change of answer then fails a test instead of
    /// leaving a wrong claim standing in the XML docs", and it does that for
    /// <c>AngouriMath.Extensions</c> by transcribing each documented output into an assertion by
    /// hand. Transcription is the part that does not scale and can itself drift: this reads the
    /// documentation as the build emits it, so the thing checked is the thing a user reads.
    /// </para>
    /// <para>
    /// <b>The convention it relies on</b> is the one the examples already follow: a
    /// <c>&lt;code&gt;</c> block, the word <c>Prints</c>, and a second <c>&lt;code&gt;</c> block
    /// holding the expected output. An example in any other shape is counted and skipped rather
    /// than guessed at.
    /// </para>
    /// <para>
    /// <b>What it found on the commit that added it:</b> of 254 examples in that shape, 187 printed
    /// what they claimed, 56 printed something else and 11 did not compile or threw. Most of the 56
    /// are the documentation trailing a deliberate change — <c>sqrt(x ^ 2)</c> stopped collapsing to
    /// <c>x</c> with <a href="https://github.com/asc-community/AngouriMath/issues/752">#752</a>,
    /// <c>tan(x) * cotan(x)</c> gained the conditions that make it true — and every one of them is a
    /// promise the library was not keeping. They are listed below rather than fixed here, so that
    /// the mechanism and the corrections are separately reviewable.
    /// </para>
    /// <para>
    /// Snippets are compiled with <c>AngouriMath.MathS</c> and <c>AngouriMath.Entity</c> imported
    /// statically, because that is how they are written and how the README and wiki open them.
    /// Without that, <c>pi</c> and <c>Providedf</c> read as undefined and the harness reports the
    /// documentation broken when it is the harness that is.
    /// </para>
    /// </remarks>
    [Trait("Area", "Convenience")]
    public sealed class DocumentationExamplesRunTest
    {
        /// <summary>
        /// The examples that do not print what they say, or do not run at all, as the member names
        /// the XML uses. <b>Every entry here is a defect in the documentation, not an exemption
        /// that has been argued for</b> — the list exists so that the count cannot grow quietly
        /// while they are corrected in batches.
        /// </summary>
        private static readonly HashSet<string> Failing = new()
        {
            "M:AngouriMath.Entity.Expand(System.Int32)",
            "M:AngouriMath.Entity.Factorize(System.Int32)",
            "M:AngouriMath.Entity.Latexize",
            "M:AngouriMath.Entity.Simplify(System.Int32)",
            "M:AngouriMath.Entity.Solve(AngouriMath.Entity.Variable)",
            "M:AngouriMath.Entity.Substitute(System.ValueTuple{AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity},System.ValueTuple{AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity})",
            "M:AngouriMath.Entity.ToSymPy(System.Boolean)",
            "M:AngouriMath.Extensions.AngouriMathExtensions.Factorize(System.String)",
            "M:AngouriMath.MathS.Apply(AngouriMath.Entity,AngouriMath.Entity[])",
            "M:AngouriMath.MathS.Arccos(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Arccosec(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Arccotan(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Arcsec(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Arcsin(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Arctan(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Cbrt(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Compute.SymbolicFormOfCosine(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Compute.SymbolicFormOfSine(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Cotan(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Derivative(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Derivative(AngouriMath.Entity,AngouriMath.Entity,System.Int32)",
            "M:AngouriMath.MathS.Equality(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Equations(System.Collections.Generic.IEnumerable{AngouriMath.Entity})",
            "M:AngouriMath.MathS.Factorial(AngouriMath.Entity)",
            "M:AngouriMath.MathS.FromBaseN(System.String,System.Int32)",
            "M:AngouriMath.MathS.GreaterOrEqualThan(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.GreaterThan(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Hyperbolic.Cosech(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Hyperbolic.Cotanh(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Hyperbolic.Sech(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Hyperbolic.Tanh(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Integral(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Integral(AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Lambda(AngouriMath.Entity.Variable,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Latex(AngouriMath.Core.ILatexizeable)",
            "M:AngouriMath.MathS.LessOrEqualThan(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.LessThan(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Limit(AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Entity,AngouriMath.Core.ApproachFrom)",
            "M:AngouriMath.MathS.Multithreading.SetLocalCancellationToken(System.Threading.CancellationToken)",
            "M:AngouriMath.MathS.Piecewise(System.Collections.Generic.IEnumerable{AngouriMath.Entity.Providedf},AngouriMath.Entity)",
            "M:AngouriMath.MathS.Piecewise(System.ValueTuple{AngouriMath.Entity,AngouriMath.Entity}[])",
            "M:AngouriMath.MathS.Pow(AngouriMath.Entity,AngouriMath.Entity)",
            "M:AngouriMath.MathS.Signum(AngouriMath.Entity)",
            "M:AngouriMath.MathS.SolveOde(AngouriMath.Entity,AngouriMath.Entity.Variable,AngouriMath.Entity.Variable)",
            "M:AngouriMath.MathS.Sqr(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Sqrt(AngouriMath.Entity)",
            "M:AngouriMath.MathS.Tan(AngouriMath.Entity)",
            "M:AngouriMath.MathS.ToBaseN(AngouriMath.Entity.Number.Real,System.Int32)",
            "M:AngouriMath.MathS.ToSympyCode(AngouriMath.Entity)",
            "M:AngouriMath.MathS.TryPolynomial(AngouriMath.Entity,AngouriMath.Entity.Variable,AngouriMath.Entity@)",
            "M:AngouriMath.MathS.Utils.TryGetPolyLinear(AngouriMath.Entity,AngouriMath.Entity.Variable,AngouriMath.Entity@,AngouriMath.Entity@)",
            "M:AngouriMath.MathS.Utils.TryGetPolyQuadratic(AngouriMath.Entity,AngouriMath.Entity.Variable,AngouriMath.Entity@,AngouriMath.Entity@,AngouriMath.Entity@)",
            "M:AngouriMath.MathS.Utils.TryGetPolynomial(AngouriMath.Entity,AngouriMath.Entity.Variable,System.Collections.Generic.Dictionary{PeterO.Numbers.EInteger,AngouriMath.Entity}@)",
            "P:AngouriMath.Entity.FreeVariables",
            "P:AngouriMath.Entity.Vars",
            "P:AngouriMath.Entity.VarsAndConsts",
            "P:AngouriMath.MathS.DecimalConst.e",
            "P:AngouriMath.MathS.DecimalConst.pi",
            "P:AngouriMath.MathS.Settings.AllowNewton",
            "P:AngouriMath.MathS.Settings.FloatToRationalIterCount",
            "P:AngouriMath.MathS.Settings.MaxAbsNumeratorOrDenominatorValue",
            "T:AngouriMath.Core.ILatexizeable",
            "T:AngouriMath.Core.MatrixBuilder",
        };

        private static string DocumentationPath()
        {
            var beside = Path.GetDirectoryName(typeof(MathS).Assembly.Location)!;
            return Path.Combine(beside, "AngouriMath.xml");
        }

        private static ScriptOptions Options() => ScriptOptions.Default
            .WithReferences(
                typeof(MathS).Assembly,
                typeof(global::AngouriMath.Extensions.AngouriMathExtensions).Assembly,
                typeof(PeterO.Numbers.EDecimal).Assembly,
                typeof(GenericTensor.Core.GenTensor<,>).Assembly,
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(Enumerable).Assembly)
            .WithImports(
                "System", "System.Linq", "System.Collections.Generic", "System.Numerics",
                "AngouriMath", "AngouriMath.Extensions", "AngouriMath.Core",
                "AngouriMath.Core.Compilation.IntoLinq",
                "PeterO.Numbers",
                "AngouriMath.MathS", "AngouriMath.Entity");

        /// <summary>
        /// One example: the program it shows, and what it says that program prints.
        /// </summary>
        private readonly record struct Example(string Member, string Program, string Prints);

        private static List<Example> Examples()
        {
            var found = new List<Example>();
            foreach (var member in XDocument.Load(DocumentationPath()).Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;
                if (name is null) continue;
                foreach (var example in member.Elements("example"))
                {
                    var codes = example.Elements("code").ToList();
                    var prose = string.Concat(example.Nodes().OfType<XText>().Select(t => t.Value));
                    if (codes.Count != 2 || !prose.Contains("Prints")) continue;
                    found.Add(new Example(name, codes[0].Value, Lines(codes[1].Value)));
                }
            }
            return found;
        }

        /// <summary>Trimmed and blank-stripped, so that indentation in the XML is not the subject.</summary>
        private static string Lines(string text) => string.Join("\n", text
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));

        /// <summary>
        /// Runs the snippet and answers what it printed, or the error that stopped it.
        /// </summary>
        /// <remarks>
        /// <c>using var x = ...;</c> is not legal at a script's top level and several examples open
        /// a settings scope that way, so the keyword is dropped and the scope lasts to the end of
        /// the snippet — which is what those examples mean by it.
        /// </remarks>
        private static (string? Printed, string? Error) Run(string program, ScriptOptions options)
        {
            var script = Regex.Replace(program, @"(?m)^(\s*)using\s+var\s", "$1var ");
            var captured = new StringWriter();
            var restore = Console.Out;
            Console.SetOut(captured);
            try
            {
                CSharpScript.EvaluateAsync(script, options).GetAwaiter().GetResult();
                return (Lines(captured.ToString()), null);
            }
            catch (CompilationErrorException error)
            {
                return (null, "did not compile: " + error.Message.Split('\n')[0]);
            }
            catch (Exception error)
            {
                return (null, error.GetType().Name + ": " + error.Message.Split('\n')[0]);
            }
            finally { Console.SetOut(restore); }
        }

        [Fact]
        public void EveryDocumentedExamplePrintsWhatItSaysItPrints()
        {
            var options = Options();
            var wrong = new List<string>();
            var offending = new HashSet<string>();

            var run = 0;
            foreach (var example in Examples())
            {
                var (printed, error) = Run(example.Program, options);
                // Each example is a compilation of its own, garbage once it has run, and the collector
                // left it standing: alone this test peaked at 2.4 GB, and beside the rest of the suite the
                // test host reached 8 GB. Collected every fifth example it peaks at 570 MB, for 3 s more.
                if (++run % 5 == 0)
                    GC.Collect();
                if (error is null && printed == example.Prints) continue;
                offending.Add(example.Member);
                wrong.Add(error is not null
                    ? $"{example.Member}\n    {error}"
                    : $"{example.Member}\n    says: {example.Prints.Replace("\n", " | ")}"
                      + $"\n    does: {printed!.Replace("\n", " | ")}");
            }

            var unexpected = offending.Except(Failing).ToList();
            Assert.True(unexpected.Count == 0,
                $"{unexpected.Count} documented examples newly disagree with what they print:\n"
                + string.Join("\n", wrong.Where(w => unexpected.Any(w.StartsWith)).Take(20)));

            // The other direction, so a corrected example is deleted from the list above rather
            // than left there excusing nothing.
            var fixedSince = Failing.Except(offending).ToList();
            Assert.True(fixedSince.Count == 0,
                $"{fixedSince.Count} examples in the list now print what they say and should be "
                + "deleted from it:\n" + string.Join("\n", fixedSince));
        }

        /// <summary>
        /// The corpus is the whole strength of the check above, and it does not fail when the
        /// corpus shrinks. Asserted so that it cannot.
        /// </summary>
        [Fact]
        public void TheDocumentedExamplesAreStillThere()
        {
            var examples = Examples();
            Assert.True(examples.Count >= 254,
                $"only {examples.Count} examples are in the two-block 'Prints' shape, and there "
                + "were 254. An example that stops following the convention stops being checked.");
        }
    }
}
