//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// A complex number with rational parts was enclosed from its parts' decimals, which are rounded
    /// to the working hundred digits, so an interval worked to more digits than that left its value
    /// out. A root is declined where an interval shows its residual is not 0, and the root of
    /// <c>cos(2x) - sin(x) - 1 = 0</c> with <c>23/108 + 1/24 i</c> in it was shown not to be one
    /// once the equation was multiplied by <c>10^7</c>, which takes the interval past a hundred
    /// digits. Found by solving generated equations multiplied by <c>10^12</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AComplexConstantIsEnclosedExactlyTest
    {
        [Theory]
        [InlineData(150)]
        [InlineData(300)]
        public void TheEnclosureHoldsTheExactParts(int digits)
        {
            var number = Number.Complex.Create(Number.Rational.Create(23, 108), Number.Rational.Create(1, 24));
            var found = Numerics.PreciseEvaluation.Of(number, digits);
            Assert.True(found.HasValue);
            var enclosure = found!.Value;
            var re = ERational.Create(23, 108);
            var im = ERational.Create(1, 24);
            Assert.True(ERational.FromEDecimal(enclosure.Re.Low).CompareTo(re) <= 0 && ERational.FromEDecimal(enclosure.Re.High).CompareTo(re) >= 0);
            Assert.True(ERational.FromEDecimal(enclosure.Im.Low).CompareTo(im) <= 0 && ERational.FromEDecimal(enclosure.Im.High).CompareTo(im) >= 0);
        }

        [Theory]
        [InlineData("10^7")]
        [InlineData("10^12")]
        [InlineData("10^30")]
        public void AnEquationTimesANumberIsSolvedAsItWas(string factor)
        {
            var asWritten = "cos(2x) - sin(x) - 1 = 0".ToEntity().Solve("x");
            var scaled = $"(cos(2x) - sin(x) - 1) * {factor} = 0".ToEntity().Solve("x");
            Assert.IsNotType<ConditionalSet>(scaled);
            Assert.Equal(asWritten, scaled);
        }
    }
}
