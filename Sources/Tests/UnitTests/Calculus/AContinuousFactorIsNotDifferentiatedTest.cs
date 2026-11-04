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
    /// <c>sin(x)^tanh(x)</c> at 0 from the right was left unevaluated, where it is 1, and <c>tan(2x)^sin(tanh(x))</c>
    /// declined after thirteen seconds. Read through its exponent, the power is a quotient l'Hopital's rule takes, and the
    /// rule differentiated every factor of both parts, including those that tend to a number other than 0, such as the
    /// <c>1 - tanh(x)^2</c> the derivative of tanh brings. With tanh written through exponentials, each such factor
    /// doubled the derivative, which outgrew the rule's bound. A factor that is continuous at a finite point with a non-zero
    /// value there is now taken out as that value: lim c(x) f(x) / g(x) is c(a) lim f(x) / g(x). Found by limitcheck at
    /// <c>DEPTH=3</c>, <c>SEED=40</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AContinuousFactorIsNotDifferentiatedTest
    {
        [Theory]
        [InlineData("sin(x)^tanh(x)")]
        [InlineData("tan(x)^sin(tanh(x))")]
        [InlineData("tan(2*x)^sin(tanh(x))")]
        public void IsOneFromTheRight(string input)
            => Assert.Equal(MathS.FromString("1"), MathS.FromString(input).Limit("x", 0, ApproachFrom.Right));

        // The value taken out is kept exact, and an infinity it multiplies is still that infinity.
        [Theory]
        [InlineData("tanh(1 + x) * (x - 1) / (x^2 - 1)", "1", "tanh(2) / 2")]
        [InlineData("(2 + cos(x)) * x / sin(x)", "0", "3")]
        [InlineData("(2 + cos(x)) / sin(x)^2", "0", "+oo")]
        public void KeepsTheValueTakenOut(string input, string destination, string expected)
        {
            var limit = MathS.FromString(input).Limit("x", destination);
            var wanted = MathS.FromString(expected);
            if (wanted.Evaled is Entity.Number { IsFinite: false })
                Assert.Equal(wanted, limit);
            else
                Assert.Equal(Entity.Number.Integer.Zero, (limit - wanted).Simplify());
        }
    }
}
