//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The antiderivative of <c>1/u</c> is <c>ln(u)</c>, and over a range where <c>u &lt; 0</c> a definite integral took
    /// it at a negative number. A finite integral was then written through <c>i pi</c> and <c>ln(-3)</c>. A divergent
    /// one kept an imaginary part beside its infinity, so <c>1/x</c> over <c>[-1, 0]</c> was <c>(-i) pi - +oo</c>,
    /// where the integral of a real function is <c>-oo</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2611
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ARealIntegralThroughALogarithmIsRealTest
    {
        [Theory]
        [InlineData("1/x", "-1", "0", "-oo")]
        [InlineData("1/x", "-oo", "-1", "-oo")]
        [InlineData("-1/x", "-1", "0", "+oo")]
        [InlineData("1/(x - 3)", "0", "3", "-oo")]
        [InlineData("1/(x^2 - 1)", "0", "1", "-oo")]
        [InlineData("x/(x^2 - 1)", "0", "1", "-oo")]
        [InlineData("1/(x * (x - 1))", "0", "1/2", "-oo")]
        [InlineData("tan(x)", "pi/2", "3 pi/4", "-oo")]
        public void ADivergentIntegralIsARealInfinity(string integrand, string from, string to, string expected)
            => Assert.Equal(expected.ToEntity().Evaled,
                integrand.ToEntity().Integrate("x", from.ToEntity(), to.ToEntity()).Simplify().Evaled);

        /// <summary>The value, and no <c>i</c> in how it is written.</summary>
        [Theory]
        [InlineData("1/x", "-2", "-1", "-ln(2)")]
        [InlineData("1/(x - 3)", "0", "2", "-ln(3)")]
        [InlineData("1/(x^2 - 1)", "-1/2", "1/2", "-ln(3)")]
        [InlineData("1/(x * (x - 1))", "1/4", "1/2", "-ln(3)")]
        [InlineData("1/(2x + 1)", "-2", "-1", "-ln(3)/2")]
        [InlineData("x/(x^2 - 1)", "-1/2", "1/2", "0")]
        [InlineData("tan(x)", "3 pi/4", "pi", "-ln(2)/2")]
        public void AFiniteIntegralIsWrittenWithoutI(string integrand, string from, string to, string expected)
        {
            var integral = integrand.ToEntity().Integrate("x", from.ToEntity(), to.ToEntity()).Simplify();
            Assert.DoesNotContain(integral.Nodes, node => node is Integralf || node == MathS.i);
            var difference = (integral - expected.ToEntity()).EvalNumerical();
            Assert.True(difference.Abs() < 1e-12, $"{integrand} from {from} to {to} was {integral}, not {expected}");
        }

        /// <summary>A principal value is no integral, and a logarithm of a complex argument is left as it was.</summary>
        [Fact]
        public void APrincipalValueStaysUndefined()
            => Assert.True("1/x".ToEntity().Integrate("x", -1, 1).Simplify().Evaled.IsNaN);

        [Fact]
        public void AComplexIntegrandKeepsItsImaginaryPart()
        {
            var integral = "1/(x + i)".ToEntity().Integrate("x", 0, 1).Simplify();
            var expected = "ln(1 + i) - ln(i)".ToEntity();
            Assert.True((integral - expected).EvalNumerical().Abs() < 1e-12, $"1/(x + i) over [0, 1] was {integral}");
        }
    }
}
