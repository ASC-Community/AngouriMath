//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A definite integral's value at a bound is the antiderivative's value there, and where that
    /// is undefined, its limit from inside the range, which is what an improper integral is.
    /// <c>-(x + 1) e^(-x)</c> at <c>+oo</c> is <c>-oo * 0</c>, so the integral of <c>x e^(-x)</c>
    /// over <c>[0, +oo)</c> was <c>NaN</c>; <c>x ln(x) - x</c> at 0 is <c>0 * -oo</c>, so the
    /// integral of <c>ln(x)</c> over <c>[0, 1]</c> was too.
    /// https://github.com/asc-community/AngouriMath/issues/1507
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ImproperIntegralTest
    {
        private static Entity Definite(string integrand, string from, string to)
            => MathS.Integral(integrand.ToEntity(), "x", from.ToEntity(), to.ToEntity()).Simplify();

        [Theory]
        [InlineData("x * e^(-x)", "0", "+oo", "1")]
        [InlineData("x^2 * e^(-x)", "0", "+oo", "2")]
        [InlineData("x^3 * e^(-x^2)", "0", "+oo", "1/2")]
        [InlineData("ln(x) / x^2", "1", "+oo", "1")]
        [InlineData("e^(-x) * sin(x)", "0", "+oo", "1/2")]
        [InlineData("x * e^x", "-oo", "0", "-1")]
        public void AnInfiniteBoundIsALimit(string integrand, string from, string to, string value)
            => Assert.Equal(value.ToEntity(), Definite(integrand, from, to));

        /// <summary>
        /// A finite bound is approached from the side the other bound is on, so the range may
        /// run either way.
        /// </summary>
        [Theory]
        [InlineData("ln(x)", "0", "1", "-1")]
        [InlineData("x * ln(x)", "0", "1", "-1/4")]
        [InlineData("ln(x)", "1", "0", "1")]
        [InlineData("x * e^(-x)", "+oo", "0", "-1")]
        // A bound written as a constant is placed by its value: the half-angle antiderivative's
        // tan(x/2) is undefined at pi.
        [InlineData("1/(2 + cos(x))", "0", "pi", "pi / sqrt(3)")]
        public void ABoundWhereTheAntiderivativeIsUndefinedIsALimit(string integrand, string from, string to, string value)
            => Assert.Equal(value.ToEntity().Simplify(), Definite(integrand, from, to));

        /// <summary>A divergent integral is <c>+oo</c> where the limit is.</summary>
        [Fact]
        public void ADivergentIntegralIsItsLimit() => Assert.Equal("+oo".ToEntity(), Definite("1/x^2", "0", "1"));

        /// <summary>
        /// And <c>NaN</c> where there is no limit: <c>-cos(x)</c> does not settle as <c>x</c> grows.
        /// </summary>
        [Fact]
        public void AnAntiderivativeWithoutALimitLeavesNaN() => Assert.Equal(MathS.NaN, Definite("sin(x)", "0", "+oo"));

        /// <summary>
        /// And as written where the limit is not decided: undefined would say the integral does not exist, and
        /// <c>ln(x) erf(x)</c> over <c>[0, 1]</c>, whose antiderivative is an infinity less an infinity at 0 that no rule
        /// settles, was <c>NaN</c> where it is -0.2613. The limit there is asked within a budget, and the simplification,
        /// which asks it again for each spelling of the integrand it tries, ran past two minutes without one.
        /// https://github.com/asc-community/AngouriMath/issues/2081
        /// </summary>
        [Theory]
        [InlineData("ln(x) * erf(x)")]
        [InlineData("ln(x) * Si(x)")]
        public void ABoundWhoseLimitIsNotDecidedLeavesTheIntegralAsWritten(string integrand)
        {
            Entity? result = null;
            var task = System.Threading.Tasks.Task.Run(() => result = Definite(integrand, "0", "1"));
            // The budget bounds the work, in limits; this wall is a backstop against a hang that the budget no longer
            // bounds, not a measure of time. The erf integral takes about nine seconds alone, and a wall of sixty
            // fired in the full suite on a loaded machine.
            Assert.True(task.Wait(System.TimeSpan.FromMinutes(5)), $"the integral of {integrand} from 0 to 1 was still being simplified");
            Assert.IsType<Entity.Integralf>(result);
            Assert.DoesNotContain(result!.Nodes, node => node == MathS.NaN);
        }

        /// <summary>A bound the substitution answers is answered as it was.</summary>
        [Theory]
        [InlineData("e^(-x)", "0", "+oo", "1")]
        [InlineData("1/(x^2 + 1)", "-oo", "+oo", "pi")]
        [InlineData("x * e^(-x^2)", "0", "+oo", "1/2")]
        [InlineData("1/sqrt(x)", "0", "1", "2")]
        [InlineData("1/x", "1", "+oo", "+oo")]
        public void ABoundWithAValueKeepsIt(string integrand, string from, string to, string value)
            => Assert.Equal(value.ToEntity(), Definite(integrand, from, to));

        private static Entity DefiniteOverT(string integrand, string from, string to)
            => MathS.Integral(integrand.ToEntity(), "t", from.ToEntity(), to.ToEntity()).Simplify();

        private static bool HoldsAnInfinity(Entity expr)
            => expr.Nodes.Any(node => node is Entity.Number.Complex number
                && (!number.RealPart.EDecimal.IsFinite || !number.ImaginaryPart.EDecimal.IsFinite));

        private static void AgreeAtRealY(Entity got, Entity want, string what)
        {
            foreach (var at in new[] { -2.3, -0.7, 0, 0.4, 1.9 })
            {
                var g = got.Substitute("y", at).EvalNumerical();
                var w = want.Substitute("y", at).EvalNumerical();
                Assert.True(System.Math.Abs((double)(g - w).RealPart) + System.Math.Abs((double)(g - w).ImaginaryPart)
                            < 1e-12 * System.Math.Max(1, System.Math.Abs((double)w.RealPart)),
                    $"{what} is {g} at y = {at}, where it should be {w}");
            }
        }

        /// <summary>
        /// An infinite bound substituted into an antiderivative with a parameter left the infinity inside it,
        /// <c>erf(+oo - y)</c>, which the arithmetic does not settle; the limit there does, reading <c>y</c> as the
        /// finite value an integral with a parameter is a function of. Checked against the closed forms, which were
        /// checked against quadrature to forty digits at the same five points.
        /// https://github.com/asc-community/AngouriMath/issues/2451
        /// </summary>
        [Theory]
        [InlineData("e^(-(t-y)^2)", "-oo", "+oo", "sqrt(pi)")]
        [InlineData("e^(-(t-y)^2)", "0", "+oo", "sqrt(pi) * (1 + erf(y)) / 2")]
        [InlineData("e^(-(t-y)^2)", "-oo", "0", "sqrt(pi) * (1 - erf(y)) / 2")]
        [InlineData("e^(-(t-y)^2)", "1", "+oo", "sqrt(pi) * (1 - erf(1 - y)) / 2")]
        [InlineData("1/(1+(t-y)^2)", "-oo", "+oo", "pi")]
        [InlineData("1/(1+(t-y)^2)", "0", "+oo", "pi / 2 + arctan(y)")]
        [InlineData("t * e^(-(t-y)^2)", "-oo", "+oo", "sqrt(pi) * y")]
        [InlineData("t^2 * e^(-(t-y)^2)", "-oo", "+oo", "sqrt(pi) * (1/2 + y^2)")]
        [InlineData("e^(-(t-y)^2/2)", "-oo", "+oo", "sqrt(2 pi)")]
        [InlineData("e^(-t^2 - 2 y t)", "-oo", "+oo", "sqrt(pi) * e^(y^2)")]
        public void Issue2451_AnInfiniteBoundWithAParameterIsALimit(string integrand, string from, string to, string value)
        {
            var result = DefiniteOverT(integrand, from, to);
            Assert.DoesNotContain(result.Nodes, node => node is Entity.Integralf);
            Assert.False(HoldsAnInfinity(result), $"{result} still holds an infinity");
            AgreeAtRealY(result, value.ToEntity(), $"the integral of {integrand} from {from} to {to}");
        }

        /// <summary>
        /// And differentiated in the parameter, under the integral sign: the integral of <c>f(t - y)</c> from 0 to
        /// <c>+oo</c> has derivative <c>f(-y)</c> in <c>y</c>, since <c>f</c> vanishes at the infinite end.
        /// https://github.com/asc-community/AngouriMath/issues/2451
        /// </summary>
        [Theory]
        [InlineData("e^(-(t-y)^2)", "e^(-y^2)")]
        [InlineData("1/(1+(t-y)^2)", "1/(1+y^2)")]
        public void Issue2451_TheIntegralDifferentiatesBackInItsParameter(string integrand, string derivative)
            => AgreeAtRealY(DefiniteOverT(integrand, "0", "+oo").Differentiate("y"), derivative.ToEntity(),
                $"d/dy of the integral of {integrand} from 0 to +oo");

        /// <summary>
        /// Where the limit depends on more than the parameter being finite it is not decided, and the value is left as
        /// the substitution gave it: <c>e^(-y t)</c> vanishes at <c>+oo</c> only for a positive <c>y</c>. Nor is it
        /// taken where a break of the antiderivative moves with the parameter and cannot be listed: <c>1/(t - y)^2</c>
        /// over <c>[0, +oo)</c> diverges for a positive <c>y</c>, and <c>1/(t^2 + y^2)</c>'s logarithm for
        /// <c>y^2 &lt; 0</c> would answer the principal value, 0, where the integral does not exist.
        /// https://github.com/asc-community/AngouriMath/issues/2451
        /// </summary>
        [Theory]
        [InlineData("e^(-y*t)", "0", "+oo")]
        [InlineData("1/(t-y)^2", "0", "+oo")]
        [InlineData("1/(t^2+y^2)", "-oo", "+oo")]
        public void Issue2451_ALimitThatDependsOnTheParameterIsNotTaken(string integrand, string from, string to)
            => Assert.True(HoldsAnInfinity(DefiniteOverT(integrand, from, to)));
    }
}
