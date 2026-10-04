//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

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
        /// The same one level further down. The rule for a power of the variable beside a block
        /// answers the question asked and the level below it, and declines
        /// <c>2/(w^2 (a + b w^2) (w^4 + 1))</c> at depth three, inside the search for the first
        /// integral; the second asks it at depth two, and the decline was held under a key that
        /// told the top from the rest and nothing more, so the second was declined after the
        /// first and answered on its own.
        /// </summary>
        [Fact]
        public void AnIntegrandDeclinedTwoBelowIsStillAnsweredOneBelow()
        {
            var first = "1/(u^(3/2)*(a + b*u))/(1 + u^2)".ToEntity().Integrate("u");
            Assert.DoesNotContain("integral(", first.Stringize());

            var second = "1/((1 + u^2)*u^(3/2)*(a + b*u))".ToEntity().Integrate("u");
            Assert.DoesNotContain("integral(", second.Stringize());
        }
    }
}
