//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The multinomial coefficient <c>(k_1 + ... + k_m)! / (k_1! ... k_m!)</c>, through the gamma
    /// function off the whole numbers, as the factorial is. It was refused by name.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class MultinomialTest
    {
        [Theory]
        [InlineData("multinomial(2, 3)", "10")]
        [InlineData("multinomial(1, 1, 1)", "6")]
        [InlineData("multinomial(2, 3, 4)", "1260")]
        [InlineData("multinomial(4)", "1")]
        [InlineData("multinomial(0, 0)", "1")]
        [InlineData("multinomial(1/2, 1/2)", "4 / pi")]
        public void ItIsTheFactorialOfTheSumOverTheFactorials(string written, string expected)
            => Assert.Equal(Number.Integer.Zero, (written.ToEntity() - expected.ToEntity()).Simplify());

        /// <summary>The coefficient of a^2 b^3 c in (a + b + c)^6 is multinomial(2, 3, 1).</summary>
        [Fact]
        public void ItIsTheCoefficientInTheMultinomialTheorem()
            => Assert.Equal(Number.Integer.Create(60), "multinomial(2, 3, 1)".ToEntity().Simplify());

        /// <summary>It is symmetric in its arguments, where the product of binomials it factors into is not.</summary>
        [Theory]
        [InlineData("multinomial(3, 1, 2)", "multinomial(1, 2, 3)")]
        [InlineData("multinomial(1/2, 1/3)", "multinomial(1/3, 1/2)")]
        [InlineData("multinomial(-3, 1)", "multinomial(1, -3)")]
        public void ItIsSymmetric(string one, string other)
            => Assert.Equal(one.ToEntity().Evaled, other.ToEntity().Evaled);

        /// <summary>No value where an argument or the sum is a negative integer, a pole of the gamma function.</summary>
        [Theory]
        [InlineData("multinomial(-1, 1)")]
        [InlineData("multinomial(1, -3)")]
        [InlineData("multinomial(1/2, -3/2)")]
        public void ItHasNoValueAtAPole(string written)
            => Assert.Equal(MathS.NaN, written.ToEntity().Simplify());

        /// <summary>Γ(11/6)/(Γ(3/2) Γ(4/3)), against mpmath to 30 digits.</summary>
        [Fact]
        public void OffTheWholeNumbersItIsAGammaRatio()
        {
            var value = Assert.IsAssignableFrom<Number.Real>("multinomial(1/2, 1/3)".ToEntity().EvalNumerical());
            var difference = value.EDecimal.Subtract(EDecimal.FromString("1.18862354635443309167938183202")).Abs();
            Assert.True(difference.CompareTo(EDecimal.FromString("1e-25")) < 0, value.Stringize());
        }
    }
}
