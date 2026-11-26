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

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>e^u - e^v</c> was evaluated by rounding both powers and subtracting, which loses every digit of
    /// <c>u - v</c> below half the working ones: <c>sinh(10^(-60))</c>, which is <c>(e^(10^(-60)) - e^(-10^(-60)))/2</c>,
    /// was 0, and so were <c>tanh(10^(-60))</c> and <c>e^(10^(-60)) - 1</c>. It is evaluated as
    /// <c>e^v (e^(u - v) - 1)</c>, the last with the digits it would cancel.
    /// https://github.com/asc-community/AngouriMath/issues/2074
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ADifferenceOfExponentialsKeepsItsDigitsTest
    {
        [Theory]
        [InlineData("sinh(10^(-60))", "1E-60", "0")]
        [InlineData("tanh(10^(-60))", "1E-60", "0")]
        [InlineData("e^(10^(-60)) - 1", "1.0000000000000000000000000000000000000000000000000000000000005E-60", "0")]
        [InlineData("(e^(2 * 10^(-60)) - 1) / (2 * e^(10^(-60)))", "1E-60", "0")]
        [InlineData("sinh(10^(-60) * (1 + i))", "1E-60", "1E-60")]
        [InlineData("sinh(10^(-60) + 10^(-130) * i)", "1E-60", "1E-130")]
        [InlineData("sinh(10^(-60) * i) / i", "1E-60", "0")]
        // Controls: an exact cancellation, one that is only rounding (e^(2 pi i) - 1), and none.
        [InlineData("e^2 - e^2", "0", "0")]
        [InlineData("sinh(pi * i)", "0", "0")]
        [InlineData("sinh(-200 + pi * i)", "361298688406287462908873852109465284867843721426365964201634894561610954680736945830780.9632945312853", "0")]
        [InlineData("sinh(1/2)", "0.5210953054937473616224256264114915591059289826114805279460935764528022508902335923170644542741885935", "0")]
        public void TheValueKeepsItsDigits(string expression, string real, string imaginary)
        {
            var value = Assert.IsAssignableFrom<Number.Complex>(expression.ToEntity().Evaled);
            // Within 1e-40: a quotient or a tanh still carries what downcasting its denominator to an integer
            // leaves, a relative 1e-60. https://github.com/asc-community/AngouriMath/issues/1019
            AssertNear(real, value.RealPart.EDecimal, "1E-40");
            AssertNear(imaginary, value.ImaginaryPart.EDecimal, "1E-40");
        }

        [Theory]
        // sinh(10^(-20)) = 10^(-20) + 10^(-60)/6 + ..., which kept eighty digits; and a difference that cancels
        // nothing keeps its last digit.
        [InlineData("sinh(10^(-20))", "1.000000000000000000000000000000000000000016666666666666666666666666666666666666666750000000000000000E-20")]
        [InlineData("(e - 1/e) / 2", "1.175201193643801456882381850595600815155717981334095870229565413013307567304323895607117452089623392")]
        [InlineData("e^3 - e^2", "12.69648082425701751369810219400671008380759226800230282029180640717627208201291596782018467517389591")]
        public void EveryDigitIsKept(string expression, string expected)
            => AssertNear(expected, Assert.IsAssignableFrom<Number.Real>(expression.ToEntity().Evaled).EDecimal, "1E-98");

        [Theory]
        // Past the exponent range, where 1 added exactly to e^(10^7) was an integer of four million digits: each of
        // these took seconds or never returned, and did not stop when cancelled.
        // https://github.com/asc-community/AngouriMath/issues/2563
        [InlineData("tanh(10^7)", "1")]
        [InlineData("tanh(-10^8)", "-1")]
        [InlineData("coth(10^8)", "1")]
        [InlineData("coth(-10^7)", "-1")]
        [InlineData("1 - e^(-10^7)", "1")]
        [InlineData("sinh(10^7)", "Infinity")]
        [InlineData("sinh(-10^8)", "-Infinity")]
        [InlineData("e^(10^7) - 1", "Infinity")]
        [InlineData("e^(10^7) - e^(-10^7)", "Infinity")]
        public void ALargeArgumentReturns(string expression, string expected)
        {
            var evaluation = System.Threading.Tasks.Task.Run(() => expression.ToEntity().Evaled);
            Assert.True(evaluation.Wait(System.TimeSpan.FromSeconds(60)), $"{expression} did not return");
            var value = Assert.IsAssignableFrom<Number.Real>(evaluation.Result).EDecimal;
            if (expected.EndsWith("Infinity"))
                Assert.Equal(EDecimal.FromString(expected), value);
            else
                AssertNear(expected, value, "1E-98");
        }

        private static void AssertNear(string expectedText, EDecimal actual, string tolerance)
        {
            var expected = EDecimal.FromString(expectedText);
            if (expected.IsZero)
                Assert.True(actual.IsZero, $"expected 0, got {actual}");
            else
                Assert.True(actual.Subtract(expected).Abs().CompareTo(expected.Abs().Multiply(EDecimal.FromString(tolerance))) < 0,
                    $"expected {expected}, got {actual}");
        }
    }
}
