//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A base tending to 0, to an exponent tending to a negative number, tends to +oo where the base is
    /// positive on the way in. The descent knew it only for a base that is never negative anywhere, so
    /// <c>(1/x)^(2 ((x - 1)/x^2 - 2))</c> at +oo was read as <c>0^(-4)</c>, which has no value, and left
    /// unevaluated. A one-sided limit at 0 is taken at +oo through x = 1/x, so
    /// <c>(sqrt(x) x)^(x - 2 - x^2)</c> at 0 from the right came to the same place once <c>Simplify</c>
    /// wrote the exponent over its least common denominator. The sign is now read at an infinity, and for
    /// a product from its factors. Found by limitcheck at <c>DEPTH=3</c>, <c>SEED=77</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AVanishingBaseToANegativePowerTest
    {
        [Theory]
        [InlineData("(1/x)^(2*((x - 1)/x^2 - 2))", "+oo", ApproachFrom.BothSides)]
        [InlineData("(sqrt(x) * x)^(x - 2 - x * x)", "0", ApproachFrom.Right)]
        [InlineData("x^(2*(x - x^2 - 2))", "0", ApproachFrom.Right)]
        public void IsPositiveInfinity(string input, string destination, ApproachFrom side)
            => Assert.Equal(Entity.Number.Real.PositiveInfinity, MathS.FromString(input).Limit("x", destination, side));

        // A positive base to a real power is positive, at an infinity too, where no slope is read.
        [Fact]
        public void APositiveBaseToARealPowerIsPositiveAtAnInfinity()
            => Assert.Equal(Entity.Number.Integer.One, MathS.FromString("sgn(e^x)").Limit("x", "-oo", ApproachFrom.Right));

        [Fact]
        public void ThatSignIsTakenThroughAFunction()
            => Assert.Equal(MathS.FromString("sinh(1)").EvalNumerical(),
                MathS.FromString("sinh(sgn(e^x))").Limit("x", "-oo").EvalNumerical());

        // Read at 0 after putting -1/x for x, sinh(e^x)'s slope is that of a flat function, and asking
        // it took 24 seconds before the limit was declined.
        [Fact]
        public void ASignNotReadAtAnInfinityIsDeclinedPromptly()
        {
            var limit = System.Threading.Tasks.Task.Run(() => MathS.FromString("sgn(sinh(e^x))").Limit("x", "-oo"));
            Assert.True(limit.Wait(System.TimeSpan.FromSeconds(10)));
        }
    }
}
