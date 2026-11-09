//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A one-sided limit of a sum, a difference or a product asked each of its parts for its limit from both sides.
    /// x^pi / log(x, 2) - 2 at 0 from the right took its quotient through l'Hopital's rule from the left as well, where
    /// x^pi is not real, and took 10 seconds where the quotient alone takes a fifth of one. Found by limitcheck; no
    /// issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ATermIsAskedFromTheSideOfItsLimitTest
    {
        [Theory]
        [InlineData("x^pi / log(x, 2) - 2", ApproachFrom.Right, "-2")]
        [InlineData("x^pi / log(x, 2) + 2", ApproachFrom.Right, "2")]
        [InlineData("x^e / log(x, 2) - 2", ApproachFrom.Right, "-2")]
        [InlineData("x^pi / log(x, 2) - 2", ApproachFrom.Left, "-2")]
        public void ADifferenceAtZeroFinishes(string input, ApproachFrom side, string expected)
        {
            Entity? limit = null;
            var task = Task.Factory.StartNew(() => limit = input.ToEntity().Limit("x", 0, side), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromMinutes(1)), "the limit did not finish in a minute");
            Assert.Equal(expected.ToEntity(), limit);
        }

        // A part with a limit from the side approached and none from the other no longer keeps the whole unevaluated:
        // x^x at 0 from the left is |x|^x e^(i pi x), which goes to 1.
        [Theory]
        [InlineData("log(x, 1) - x ^ x + 1", "0")]
        [InlineData("-tan(x) ^ abs(x)", "-1")]
        [InlineData("1/2 * x ^ abs(x)", "1/2")]
        public void APartIsReadFromTheLeft(string input, string expected)
            => Assert.Equal(expected.ToEntity(), input.ToEntity().Limit("x", 0, ApproachFrom.Left));

        // Each side is still its own: a step whose sides differ is read from the side asked.
        [Theory]
        [InlineData(ApproachFrom.Right, "1")]
        [InlineData(ApproachFrom.Left, "-1")]
        public void AStepIsReadFromItsSide(ApproachFrom side, string expected)
            => Assert.Equal(expected.ToEntity(), "sqrt(x) + abs(x) / x".ToEntity().Limit("x", 0, side));
    }
}
