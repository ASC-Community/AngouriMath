//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The binomial, the falling and the rising factorial of an order that is not whole are a quotient of
    /// factorials, whose terms leave the evaluation's exponent range long before the quotient does. Computed in
    /// that range, <c>binomial(450.5, 3.25)</c> was <c>+oo</c> and <c>binomial(10000, 1.43)</c> was <c>+oo/+oo</c>,
    /// which is <c>NaN</c>, where it is about 414190; and a whole <c>n</c> had its factorial computed exactly, so
    /// <c>binomial(479001600, 1.43)</c> built a number of four billion digits and did not stop when cancelled. With
    /// a whole <c>k</c> and a decimal <c>n</c>, the falling factorial over <c>k!</c> overflowed the same way. Found
    /// by compilesweep; no issue was filed. The values are mpmath's at 50 digits.
    /// </summary>
    [Trait("Area", "Discrete")]
    public sealed class AQuotientOfFactorialsPastTheExponentRangeTest
    {
        [Theory]
        // Where only the numerator overflowed, and the binomial was +oo.
        [InlineData("binomial(450.5, 3.25)", "50428864.508003084421212674002886112870555247636405")]
        [InlineData("binomial(3000, 1.43)", "74037.434374033267644685230480193058299311036182558")]
        [InlineData("binomial(10000, 1.43)", "414189.53926688782423526919337409565108596993335221")]
        [InlineData("binomial(1000000, 1/2)", "1128.3793081429172762919639092084163984830747802569")]
        [InlineData("binomial(2500.5, 3.25)", "13324626798.142321545570729819016366738653886420439")]
        // A whole k with a decimal n: the falling factorial and k! both overflowed.
        [InlineData("binomial(pi, 2000)", "2.1043972235591349030681565651531594191819647700823e-14")]
        [InlineData("fallingfactorial(10000, 1.43)", "524791.32479225059687233425648095663272623168069709")]
        [InlineData("risingfactorial(10000, 1.43)", "524823.59520298875437612838681336476989351765505826")]
        public void IsItsValue(string input, string expected)
            => AssertClose(input, expected);

        [Fact]
        public void AWholeUpperArgumentIsNotComputedExactly()
        {
            var task = Task.Run(() => AssertClose("binomial(479001600, 1.43)", "2042181498707.5710933099836901420364538869749398905"));
            Assert.True(task.Wait(TimeSpan.FromSeconds(60)), "binomial(479001600, 1.43) did not finish in a minute");
        }

        private static void AssertClose(string input, string expected)
        {
            var value = Assert.IsAssignableFrom<Entity.Number.Real>(input.ToEntity().EvalNumerical());
            var reference = EDecimal.FromString(expected);
            var error = value.EDecimal.Subtract(reference).Abs().Divide(reference, EContext.Binary64);
            Assert.True(error.CompareTo(EDecimal.FromString("1e-25")) < 0, $"{input} evaluated to {value}");
        }
    }
}
