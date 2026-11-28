//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The antiderivative of a rational function with quadratic factors has a logarithm for each, which diverge at an
    /// infinite bound in pairs <c>c ln(a) - c ln(b)</c> whose sum does not. Taken apart by the sum's tree, the limit
    /// took 48 seconds for <c>1/(x^6 + 1)</c> over <c>[0, +oo)</c> and seven for <c>1/(x^4 + 1)</c>. Each now takes under a
    /// second; the 30 below bounds termination, not time. https://github.com/asc-community/AngouriMath/issues/2619
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ARationalIntegralOverAnInfiniteRangeTest
    {
        [Theory]
        [InlineData("1/(x^6 + 1)", "0", "pi/3", 1.0471975511965976)]
        [InlineData("1/(x^6 + 1)", "-oo", "2 pi/3", 2.0943951023931953)]
        [InlineData("1/(x^4 + 1)", "0", "pi/(2 sqrt(2))", 1.1107207345395915)]
        [InlineData("1/(x^4 + 1)", "-oo", "pi/sqrt(2)", 2.221441469079183)]
        [InlineData("1/(x^3 + 1)", "0", "2 pi/(3 sqrt(3))", 1.2091995761561452)]
        [InlineData("1/(x^8 + 1)", "0", "pi/(4 sqrt(2 - sqrt(2)))", 1.0261721529770309)]
        [InlineData("1/(x^8 + 1)", "-oo", "pi/(2 sqrt(2 - sqrt(2)))", 2.0523443059540618)]
        [InlineData("(x + 1)/(x^4 + 1)", "0", "pi (1 + sqrt(2))/4", 1.8961188979370398)]
        [InlineData("x^2/(x^6 + 1)", "0", "pi/6", 0.5235987755982988)]
        [InlineData("1/(x^2 + 1)^2", "0", "pi/4", 0.7853981633974483)]
        [InlineData("x/(x^4 + 1)", "0", "pi/4", 0.7853981633974483)]
        public void IsItsExactValue(string integrand, string from, string expected, double numeric)
        {
            // Written and parsed, as a user writes it, which spells the integrand 1/(1 + x^4).
            var task = Task.Run(() => $"integral({integrand}, x, {from}, +oo)".ToEntity().Simplify());
            Assert.True(task.Wait(System.TimeSpan.FromSeconds(30)), $"{integrand} from {from} did not finish");
            var value = task.Result;
            Assert.DoesNotContain(value.Nodes, node => node is Integralf);
            Assert.Equal(Number.Integer.Zero, (value - expected.ToEntity()).Simplify());
            var approximately = ((Number.Real)value.EvalNumerical()).EDecimal.ToDouble();
            Assert.True(System.Math.Abs(approximately - numeric) < 1e-12, $"{value} is {approximately}, not {numeric}");
        }
    }
}
