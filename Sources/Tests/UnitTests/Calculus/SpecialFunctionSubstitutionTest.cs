//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A special function as the substitution: beside a power of it, its derivative is the
    /// differential, so <c>e^(c - b^2 x^2) erf(b x)^n</c> is a power of <c>u = erf(b x)</c>, and
    /// <c>Si(b x) sin(b x)/x</c> is <c>u du</c> under <c>u = Si(b x)</c>. The rows are Rubi's, from
    /// its files 8.1, 8.3, 8.4 and 8.5.
    /// https://github.com/asc-community/AngouriMath/issues/1501
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class SpecialFunctionSubstitutionTest
    {
        /// <summary>Off 0, where the reciprocals are undefined.</summary>
        private static readonly double[] Points = { -1.7, -0.6, 0.35, 0.9, 1.45 };

        private static readonly (string, string)[] Parameters = { ("b", "13/10"), ("c", "7/10") };

        /// <summary>
        /// Integrates, pins the parameters, and compares the derivative of the answer with the
        /// integrand at <see cref="Points"/>. The parameters are pinned after integrating, so the
        /// rule is asked the symbolic question.
        /// </summary>
        private static void DifferentiatesBack(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => Parameters.Aggregate(e, (current, pin) => current.Substitute(pin.Item1, pin.Item2.ToEntity()));
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in Points)
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                var difference = Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart);
                var scale = Math.Max(1.0, Math.Abs((double)want.RealPart) + Math.Abs((double)want.ImaginaryPart));
                Assert.True(difference / scale < 1e-9,
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }

        [Theory]
        [InlineData("e^(c - b^2*x^2)*erf(b*x)")]
        [InlineData("e^(c - b^2*x^2)*erf(b*x)^2")]
        [InlineData("e^(c - b^2*x^2)/erf(b*x)")]
        [InlineData("e^(c - b^2*x^2)/erf(b*x)^2")]
        [InlineData("e^(c - b^2*x^2)*erfc(b*x)^3")]
        [InlineData("e^(c + b^2*x^2)*erfi(b*x)")]
        [InlineData("e^(b*x)*Ei(b*x)/x")]
        [InlineData("Si(b*x)*sin(b*x)/x")]
        [InlineData("cos(b*x)*Ci(b*x)/x")]
        public void ThePowerBesideTheDerivativeIsASubstitution(string integrand)
            => DifferentiatesBack(integrand);
    }
}
