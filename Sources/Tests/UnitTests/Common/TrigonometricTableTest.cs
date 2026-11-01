//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The table of exact trigonometric values is written as <c>2pi/n</c>, so it names only
    /// the angles of the first turn that divide it evenly, and the identities used to read
    /// it reached some of the rest and not the others. <c>sin(2pi/3)</c> came back
    /// <c>sqrt(3)/2</c> while <c>sin(5pi/3)</c>, the same value negated, was left written as
    /// a sine -- so the roots of x^6 = 1 were answered five exactly and one as
    /// <c>1/2 + i*sin(5/3*pi)</c>, its own conjugate spelled differently.
    /// https://github.com/asc-community/AngouriMath/issues/743
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TrigonometricTableTest
    {
        /// <summary>The denominators the table has entries for, plus a few it has not.</summary>
        private static readonly int[] Denominators =
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 18, 20, 24 };

        private static readonly string[] Functions = { "sin", "cos", "tan" };

        private static IEnumerable<(string Function, string Angle)> EveryAngleOfTheFirstTurn()
        {
            foreach (var n in Denominators)
                for (var k = 0; k <= 2 * n; k++)
                    foreach (var function in Functions)
                        yield return (function, $"{k}/{n} * pi");
        }

        private static bool IsUnevaluatedTrig(Entity expr) =>
            expr.Nodes.Any(node => node is Entity.Sinf or Entity.Cosf or Entity.Tanf
                                       or Entity.Cotanf or Entity.Secantf or Entity.Cosecantf);

        private static double? Value(Entity expr)
        {
            try
            {
                var value = expr.EvalNumerical();
                if (!value.IsFinite)
                    return null;
                return value.RealPart.EDecimal.ToDouble();
            }
            catch (System.Exception) { return null; }
        }

        /// <summary>
        /// The property that matters most, and the one a wrong table entry would break: an
        /// exact value handed back in place of a trigonometric call has to be that call's
        /// value. Swept over every k/n of the first turn rather than over the angles the
        /// issue names, because the risk of widening a lookup table is that it starts
        /// answering angles it does not know.
        /// </summary>
        [Fact]
        public void EveryExactValueAgreesWithTheCallItReplaces()
        {
            var resolved = 0;
            foreach (var (function, angle) in EveryAngleOfTheFirstTurn())
            {
                var call = $"{function}({angle})".ToEntity();
                var simplified = call.InnerSimplified;
                if (IsUnevaluatedTrig(simplified))
                    continue;
                resolved++;
                var expected = Value(call);
                var actual = Value(simplified);
                if (expected is null)
                {
                    // tan(pi/2) and its kind: undefined either way is agreement.
                    Assert.True(actual is null,
                        $"{function}({angle}) has no finite value but was given {simplified.Stringize()}");
                    continue;
                }
                Assert.True(actual is not null,
                    $"{function}({angle}) is {expected} but was given the non-finite " +
                    $"{simplified.Stringize()}");
                Assert.Equal(expected.Value, actual!.Value, 12);
            }
            // A sweep that resolved nothing would pass every assertion above. Measured over
            // the 1158 calls swept: 581 of them resolved before the half turn was applied
            // and 690 after, and the 581 were already sound -- what was missing was missing,
            // not wrong. Raise this if the table gains entries; it is here to catch losing
            // them silently. 642 since the entries written through the complex numbers, the
            // sevenths, ninths and eighteenths and the half angles of them, are passed over and
            // their calls left as written: https://github.com/asc-community/AngouriMath/issues/2143
            Assert.True(resolved >= 642, $"only {resolved} of the swept angles resolved at all");
        }

        /// <summary>
        /// The angles the issue names. Each is an exact value wearing an unevaluated form:
        /// sin(5pi/3) is -sqrt(3)/2 and cos(4pi/5) is -(1 + sqrt(5))/4.
        /// </summary>
        [Theory]
        [InlineData("sin(5/3 * pi)", "-sqrt(3) / 2")]
        [InlineData("cos(4/5 * pi)", "-(1 + sqrt(5)) / 4")]
        [InlineData("sin(8/5 * pi)", "-sqrt(10 + 2 * sqrt(5)) / 4")]
        // Their neighbours in the lower half of the circle, which the table reached no
        // better and which nothing had reported.
        [InlineData("sin(7/6 * pi)", "-1/2")]
        [InlineData("sin(11/6 * pi)", "-1/2")]
        [InlineData("cos(5/4 * pi)", "-sqrt(2) / 2")]
        [InlineData("cos(7/4 * pi)", "sqrt(2) / 2")]
        [InlineData("tan(5/3 * pi)", "-sqrt(3)")]
        [InlineData("cos(6/5 * pi)", "-(1 + sqrt(5)) / 4")]
        public void AnAngleOfTheLowerHalfIsAnsweredExactly(string call, string expected)
        {
            var simplified = call.ToEntity().Simplify();
            Assert.False(IsUnevaluatedTrig(simplified),
                $"{call} came back as {simplified.Stringize()}");
            Assert.Equal(Value(expected.ToEntity())!.Value, Value(simplified)!.Value, 12);
        }

        /// <summary>
        /// What the issue was found through: the roots of x^n = 1 are handed back in a + bi
        /// form, and an unevaluated sine in the last of them made the answer inconsistent
        /// with the conjugate two lines above it.
        /// </summary>
        [Theory]
        [InlineData("x ^ 3 - 1 = 0", 3)]
        [InlineData("x ^ 4 - 1 = 0", 4)]
        [InlineData("x ^ 5 - 1 = 0", 5)]
        [InlineData("x ^ 6 - 1 = 0", 6)]
        [InlineData("x ^ 8 - 1 = 0", 8)]
        [InlineData("x ^ 12 - 1 = 0", 12)]
        public void NoRootOfUnityCarriesAnUnevaluatedTrigonometricCall(string equation, int count)
        {
            var roots = (Entity.Set.FiniteSet)equation.ToEntity().Solve("x");
            Assert.Equal(count, roots.Count);
            foreach (var root in roots)
                Assert.False(IsUnevaluatedTrig(root),
                    $"{equation} answered {root.Stringize()}, which is not an exact value");
        }

        /// <summary>
        /// A root and its conjugate must be written the same way. Reading the table before
        /// building a value out of the doubled angle is what settles this: the doubled-angle
        /// route gives sqrt((1 + (sqrt(5) - 1)/4) / 2) where the table names the same number
        /// (sqrt(5) + 1)/4, so before it x^5 = 1 came back written both ways at once.
        /// </summary>
        [Theory]
        [InlineData("x ^ 5 - 1 = 0")]
        [InlineData("x ^ 6 - 1 = 0")]
        [InlineData("x ^ 12 - 1 = 0")]
        public void ARootOfUnityIsWrittenAsItsConjugateIs(string equation)
        {
            var roots = (Entity.Set.FiniteSet)equation.ToEntity().Solve("x");
            var byValue = roots.ToDictionary(
                root => (System.Math.Round(root.EvalNumerical().RealPart.EDecimal.ToDouble(), 9),
                         System.Math.Round(root.EvalNumerical().ImaginaryPart.EDecimal.ToDouble(), 9)),
                root => root);
            foreach (var ((re, im), root) in byValue)
            {
                if (im == 0 || !byValue.TryGetValue((re, -im), out var conjugate))
                    continue;
                Assert.Equal(root.Stringize().Replace("-", ""),
                             conjugate.Stringize().Replace("-", ""));
            }
        }

        // What must not change: the angles the table always answered.
        [Theory]
        [InlineData("sin(0)", "0")]
        [InlineData("sin(pi)", "0")]
        [InlineData("cos(pi)", "-1")]
        [InlineData("sin(pi / 2)", "1")]
        [InlineData("sin(2/3 * pi)", "sqrt(3) / 2")]
        [InlineData("cos(1/5 * pi)", "(sqrt(5) + 1) / 4")]
        [InlineData("cos(2/5 * pi)", "(sqrt(5) - 1) / 4")]
        [InlineData("sin(4/3 * pi)", "-sqrt(3) / 2")]
        [InlineData("cos(pi / 3)", "1/2")]
        [InlineData("tan(pi / 4)", "1")]
        public void TheAnglesTheTableAlreadyAnsweredAreUnchanged(string call, string expected)
        {
            var simplified = call.ToEntity().Simplify();
            Assert.False(IsUnevaluatedTrig(simplified),
                $"{call} came back as {simplified.Stringize()}");
            Assert.Equal(Value(expected.ToEntity())!.Value, Value(simplified)!.Value, 12);
        }
    }
}
