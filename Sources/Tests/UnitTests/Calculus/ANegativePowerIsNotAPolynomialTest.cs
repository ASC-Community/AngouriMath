//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A product with a negative power of the variable in it, <c>e^(2x) x^(-1)</c>, is not a
    /// polynomial times something, and integration by parts does not take it for one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The polynomial reader takes <c>x^(-1)</c> as a monomial of degree -1, and two rules
    /// differentiated what it read until it vanished: tabular integration by parts, and the
    /// closed loop for a polynomial times an exponential and a sine or a cosine. A negative power
    /// never vanishes, and each derivative was larger than the last, so <c>e^(2x) x^(-1)</c> ran
    /// the process out of memory in seconds, where <c>e^(2x)/x</c> is <c>Ei(2x)</c> in
    /// milliseconds. It is the same integrand, and the power is how the integrator itself writes
    /// a quotient once a constant factor is taken out.
    /// </para>
    /// <para>
    /// What regresses here is a runaway rather than an answer, so each integral runs under the
    /// guard, and every answer is differentiated back at points on both sides of the pole.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ANegativePowerIsNotAPolynomialTest
    {
        private static readonly double[] Points = { -1.3, -0.6, 0.45, 1.7 };

        private static Entity IntegrateWithinTheGuard(string integrand)
        {
            Entity? answer = null;
            // A thread of its own: a CPU-bound task queued behind other tests' work would spend
            // the guard waiting to start.
            var task = Task.Factory.StartNew(() => answer = integrand.ToEntity().Integrate("x"),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.True(task.Wait(LimitTermination.Guard),
                $"{integrand} did not finish within {LimitTermination.Guard}");
            return answer!;
        }

        private static void DifferentiatesBack(string integrand, Entity integral)
        {
            // The parameter is pinned only for the numeric comparison; the antiderivative itself
            // was found symbolically.
            var derivative = integral.Substitute("C", 0).Differentiate("x").Substitute("a", 0.7);
            var original = integrand.ToEntity().Substitute("a", 0.7);
            var compared = 0;
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                if (got.IsNaN || want.IsNaN)
                    continue;
                compared++;
                var difference = Math.Abs((double)(got - want).RealPart)
                               + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, "
                    + $"where the integrand is {want}");
            }
            Assert.True(compared >= 3,
                $"only {compared} of {Points.Length} points were comparable for {integrand}, "
                + "so this asserts almost nothing");
        }

        /// <summary>
        /// Each was out of memory or past a minute, and each is an exponential integral once the
        /// power is not taken for a polynomial: by parts against the power, whose remainder is
        /// the quotient spelling.
        /// </summary>
        [Theory]
        [InlineData("e^(2*x)*x^(-1)")]
        [InlineData("e^(a*x)*x^(-1)")]
        [InlineData("e^x*x^(-2)")]
        [InlineData("x^(-3)*e^x")]
        [InlineData("(x + x^(-1))*e^x")]
        [InlineData("2^x*x^(-1)")]
        public void IsAnswered(string integrand)
        {
            var integral = IntegrateWithinTheGuard(integrand);
            Assert.DoesNotContain("integral(", integral.Stringize());
            DifferentiatesBack(integrand, integral);
        }

        /// <summary>
        /// Beside a sine or a cosine as well, which is the closed loop's shape, and beside an
        /// inverse tangent, whose decline took fifteen seconds. These are not answered yet, and
        /// what is asserted is that they finish; should one be answered, its value is checked.
        /// </summary>
        [Theory]
        [InlineData("e^x*sin(x)*x^(-1)")]
        [InlineData("e^x*cos(x)*x^(-2)")]
        [InlineData("x^(-1)*arctan(x)")]
        public void Finishes(string integrand)
        {
            var integral = IntegrateWithinTheGuard(integrand);
            if (!integral.Stringize().Contains("integral("))
                DifferentiatesBack(integrand, integral);
        }
    }
}
