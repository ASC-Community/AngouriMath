//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// A root found by Newton's method was computed in <c>double</c> and widened to an <c>EDecimal</c>, which keeps
    /// every digit of the binary fraction: <c>cos(x) = x</c> was <c>0.7390851332151606722931092008366249501705169677734375</c>,
    /// right to sixteen digits of fifty. Every digit printed must be right.
    /// https://github.com/asc-community/AngouriMath/issues/2613
    /// </summary>
    /// <remarks>The reference values are mpmath's, at sixty digits.</remarks>
    [Trait("Area", "Algebra")]
    public sealed class ANewtonRootPrintsOnlyDigitsItKnowsTest
    {
        /// <summary>
        /// Whether <paramref name="part"/> is within one unit of its own last digit of <paramref name="truth"/>, and has
        /// at least <paramref name="digits"/> digits after the point.
        /// </summary>
        private static void EveryDigitIsRight(EDecimal part, string truth, int digits)
        {
            var reference = EDecimal.FromString(truth);
            var ulp = EDecimal.Create(EInteger.One, part.Exponent);
            var error = part.Subtract(reference).Abs();
            // The reference has sixty digits; a part with more is checked to the reference's own.
            var tolerance = ulp.CompareTo(EDecimal.Create(1, -58)) < 0 ? EDecimal.Create(1, -58) : ulp;
            Assert.True(error.CompareTo(tolerance) <= 0, $"{part} is not {truth} to its last digit");
            Assert.True(-part.Exponent.ToInt32Checked() >= digits, $"{part} has fewer than {digits} digits");
        }

        private static Complex[] Roots(string equation)
            => Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x")).Elements.Cast<Complex>().ToArray();

        [Fact]
        public void TheRealRootOfCosXEqualsX()
        {
            var root = Assert.Single(Roots("cos(x) = x"), root => root.ImaginaryPart.IsZero);
            EveryDigitIsRight(root.RealPart.EDecimal, "0.739085133215160641655312087673873404013411758900757464965681", 50);
        }

        [Fact]
        public void AComplexRootOfCosXEqualsX()
        {
            var root = Assert.Single(Roots("cos(x) = x"), root =>
                System.Math.Abs(root.RealPart.EDecimal.ToDouble() + 2.4869) < 1e-3 && root.ImaginaryPart.EDecimal.ToDouble() > 0);
            EveryDigitIsRight(root.RealPart.EDecimal, "-2.48688569890856023069695703435065797942282855288883147897824", 50);
            EveryDigitIsRight(root.ImaginaryPart.EDecimal, "1.80936134129570331901627610019401132560007622440538558824467", 50);
        }

        [Theory]
        [InlineData("x - e^(1 - ln(x)) = 0", "1.64872127070012814684865078781416357165377610071014801157508")]
        [InlineData("x - e^(2 - ln(x)) = 0", "2.71828182845904523536028747135266249775724709369995957496697")]
        public void APlusOrMinusSqrtOfAPowerOfE(string equation, string positive)
        {
            var roots = Roots(equation);
            Assert.Equal(2, roots.Length);
            foreach (var root in roots)
            {
                Assert.True(root.ImaginaryPart.IsZero);
                EveryDigitIsRight(root.RealPart.EDecimal.Abs(), positive, 50);
            }
        }

        [Fact]
        public void ASystemAnsweredByNewton()
        {
            var solutions = MathS.Equations("ln(x) + ln(y) - 1", "x - y").Solve("x", "y");
            Assert.NotNull(solutions);
            var x = Assert.IsAssignableFrom<Real>(solutions![0, 0].Evaled);
            // Exact or refined: its value is sqrt(e) to fifty digits either way.
            EveryDigitIsRight(x.EDecimal.RoundToPrecision(EContext.ForPrecision(55)),
                "1.64872127070012814684865078781416357165377610071014801157508", 50);
        }

        // The public search, asked by name, refines as well.
        [Fact]
        public void SolveNtRefines()
        {
            var root = Assert.Single("cos(x) - x".ToEntity().SolveNt("x"), root => root.ImaginaryPart.IsZero);
            EveryDigitIsRight(root.RealPart.EDecimal, "0.739085133215160641655312087673873404013411758900757464965681", 50);
        }
    }
}
