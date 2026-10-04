//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A decline made one level down, by a rule that answers only the question asked, must not
    /// be served from the cache to the question when it is asked.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Five rules are scoped to the top-level integrand
    /// (<see href="https://github.com/asc-community/AngouriMath/issues/1265"/>) and decline the
    /// same integrand inside another rule's search. The integrator's cache held that
    /// <see langword="null"/> without the scope it was made in, so <c>cos(x)^(-3)</c> — tried
    /// inside the search for <c>1/cos(x)^3</c> and declined there by the scoped secant
    /// reduction — was then declined from the cache in two milliseconds when asked for
    /// directly. It went unseen because integration by parts used to answer it at depth two
    /// regardless; when that path was narrowed the cache's mistake surfaced.
    /// </para>
    /// <para>
    /// The two integrals are asked in that order in one test, on one thread, so the cache is
    /// exercised exactly as it was.
    /// </para>
    /// </remarks>
    [Trait("Area", "Calculus")]
    public sealed class ScopedDeclineIsNotCachedTest
    {
        [Fact]
        public void AnIntegrandDeclinedInsideAnotherSearchIsStillAnsweredWhenAsked()
        {
            // The first search tries cos(x)^(-3) one level down, where the reduction declines it.
            var viaQuotient = "1/cos(x)^3".ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", viaQuotient.Stringize());

            var asked = "cos(x)^(-3)".ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", asked.Stringize());
        }

        /// <summary>
        /// The same two levels further down, inside one integral's own search. A rule may answer
        /// the question asked and one or two levels below it and decline the same integrand
        /// deeper. The search for <c>1/(x^(3/2) (a + c x^4))</c> declines the root's substitution
        /// <c>2/(w^2 (a + c w^8))</c> at depth three and asks it again at depth two, and the
        /// decline was held under a key that told the top from the rest and nothing more, so it
        /// was served there and the integral was declined.
        /// </summary>
        /// <remarks>
        /// Checked by differentiating back with <c>a = 2.3</c>, <c>b = 0.7</c>, <c>c = 1.3</c>,
        /// for positive <c>x</c>, where the half powers are real.
        /// </remarks>
        [Theory]
        [InlineData("1/(x^(3/2)*(a + c*x^4))")]
        [InlineData("1/(x^(5/2)*(a + c*x^4))")]
        [InlineData("1/((a + b/x^3)*x^5)")]
        [InlineData("1/(x^(7/2)*(a + b*x^2 + c*x^4))")]
        public void AnIntegrandDeclinedThreeBelowIsStillAnsweredTwoBelow(string integrand)
        {
            var integral = integrand.ToEntity().Integrate("x");
            Assert.DoesNotContain("integral(", integral.Stringize());
            Entity Pinned(Entity e) => e.Substitute("a", 2.3).Substitute("b", 0.7).Substitute("c", 1.3);
            var derivative = Pinned(integral.Substitute("C", 0)).Differentiate("x");
            var original = Pinned(integrand.ToEntity());
            foreach (var at in new[] { 0.4, 0.9, 1.7 })
            {
                var got = derivative.Substitute("x", at).EvalNumerical();
                var want = original.Substitute("x", at).EvalNumerical();
                Assert.True(Math.Abs((double)(got - want).RealPart) + Math.Abs((double)(got - want).ImaginaryPart)
                            < 1e-9 * Math.Max(1, Math.Abs((double)want.RealPart)),
                    $"d/dx of the antiderivative of {integrand} is {got} at x = {at}, where the integrand is {want}");
            }
        }
    }
}
