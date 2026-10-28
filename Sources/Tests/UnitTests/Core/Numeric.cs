//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using PeterO.Numbers;
using static AngouriMath.Entity.Number;
using Xunit;
using static AngouriMath.Entity.Set;
using AngouriMath.Extensions;
using System;

namespace AngouriMath.Tests.Core
{
    [Trait("Area", "Core")]
    public sealed class Numeric
    {
        [Fact]
        public void TestRational1() =>
            Assert.Equal(Rational.Create(19, 12),
                Rational.Create(3, 4) + Rational.Create(5, 6));

        [Fact]
        public void TestRational2() =>
            Assert.Equal(Rational.Create(7, 4), Rational.Create(3, 4) + 1);

        [Fact]
        public void TestRational3() =>
            Assert.Equal(Rational.Create(15, 4),
                Rational.Create(7, 4) + Rational.Create(8, 4));

        [Fact]
        public void TestInteger()
        {
            var actual = Rational.Create(3);
            Assert.IsType<Integer>(actual);
            Assert.Equal(Integer.Create(3), actual);
        }

        [Fact]
        public void TestComplex1()
        {
            var x = Complex.Create(2, 0.0m);
            Assert.IsType<Integer>(x);
            var res = x / 0;
            Assert.IsType<Real>(res);
            Assert.Equal(Real.NaN, res);
        }
        [Fact]
        public void TestComplex2()
        {
            Complex a = 3;
            Complex b = MathS.i;
            Complex c = a + b;
            Assert.Equal(Complex.Create(3, 1), c);
        }
        /// <summary>
        /// A root of a negative real. Evaluating one used to search for a real root first, which
        /// for an even order cannot exist — every even root of a negative real is off the real
        /// line — and the search is not a cheap miss, so
        /// <a href="https://github.com/asc-community/AngouriMath/issues/975">#975</a> skips it
        /// there. These pin what the skipping must not cost: the exact answers on the even side,
        /// and the real root the search is still there to find on the odd side.
        /// </summary>
        [Theory]
        // Even order: the real-root search is skipped, and the exact value has to survive the
        // general complex formula it falls through to.
        [InlineData("sqrt(-4)", "2i")]
        [InlineData("(-4) ^ (3/2)", "-8i")]
        [InlineData("(-1) ^ (1/2)", "i")]
        // Odd order: a real root exists, so the search still runs and still answers exactly.
        [InlineData("(-8) ^ (1/3)", "-2")]
        [InlineData("(-27) ^ (2/3)", "9")]
        // And the positive side, which the guard does not touch.
        [InlineData("4 ^ (1/2)", "2")]
        [InlineData("16 ^ (1/4)", "2")]
        [InlineData("1 ^ (1/4)", "1")]
        public void ARootOfANegativeRealKeepsItsExactValue(string expression, string expected) =>
            Assert.Equal(expected.ToEntity().Evaled, expression.ToEntity().Evaled);

        /// <summary>
        /// And where it is not exact it is still the imaginary multiple of the positive root, to
        /// the last digit of the precision context — which is the identity the skipped search was
        /// never contributing to. #975
        /// </summary>
        [Theory]
        [InlineData("sqrt(-2)", "i * sqrt(2)")]
        [InlineData("sqrt(-pi)", "i * sqrt(pi)")]
        public void AnEvenRootOfANegativeRealIsTheImaginaryOne(string negative, string imaginary) =>
            Assert.Equal(imaginary.ToEntity().Evaled, negative.ToEntity().Evaled);

        [Fact]
        public void TestRationalDowncasting()
        {
            var frac21_10 = Rational.Create(21, 10);
            Assert.IsType<Rational>(frac21_10);
            Assert.Equal(frac21_10, frac21_10.EvalNumerical());
            Assert.Equal(frac21_10, frac21_10.Simplify().EvalNumerical());

            var squared = Entity.Number.Pow(frac21_10, 2);
            Assert.IsType<Rational>(squared);
            Assert.Equal(Rational.Create(441, 100), squared);
            Assert.Equal(squared, squared.EvalNumerical());
            Assert.Equal(squared, squared.Simplify().EvalNumerical());

            var cubed = Entity.Number.Pow(squared, Rational.Create(3, 2));
            Assert.IsType<Rational>(cubed);
            Assert.Equal(Rational.Create(9261, 1000), cubed);
            Assert.Equal(cubed, cubed.EvalNumerical());
            Assert.Equal(cubed, cubed.Simplify().EvalNumerical());

            var ten = cubed + Rational.Create(739, 1000);
            Assert.IsType<Integer>(ten);
            Assert.Equal(Integer.Create(10), ten);
            Assert.Equal(ten, ten.EvalNumerical());
            Assert.Equal(ten, ten.Simplify().EvalNumerical());
        }
        [Fact]
        public void TestComplexDowncasting()
        {
            Entity x = "x2 + 1/9";
            var roots = x.SolveEquation("x");
            if (roots is not FiniteSet finite)
                throw new Exception("Mee?");
            foreach (var root in finite)
            {
                var number = Assert.IsType<Complex>(root);
                Assert.True(Assert.IsType<Integer>(number.RealPart).EInteger.IsZero);
                var im = Assert.IsType<Rational>(number.ImaginaryPart).ERational;
                Assert.Equal(1, im.UnsignedNumerator);
                Assert.Equal(3, im.Denominator);
            }
        }

        [Fact]
        public void TestWithUndefined1()
        {
            var a = Real.NegativeInfinity;
            var b = Real.PositiveInfinity;
            Assert.Equal(Real.NaN, a + b);
        }

        [Fact]
        public void TestWithUndefined2()
        {
            var a = Real.NegativeInfinity;
            var b = Real.NegativeInfinity;
            Assert.Equal(Real.NegativeInfinity, a + b);
        }

        [Fact]
        public void TestWithUndefined3()
        {
            var a = Real.NegativeInfinity;
            var b = Real.NegativeInfinity;
            Assert.Equal(Real.PositiveInfinity, a * b);
        }

        [Fact]
        public void TestWithUndefined4()
        {
            var a = Real.NegativeInfinity;
            var b = Real.NaN;
            Assert.Equal(Real.NaN, a * b);
        }

        [Fact]
        public void TestWithUndefined5()
        {
            var a = Real.Create(4);
            var b = Real.Create(0.0m);
            Assert.Equal(Real.NaN, a / b);
        }
        [Fact]
        public void TestWithUndefined6()
        {
            var a = Real.Create(4);
            var b = Rational.Create(0, 6);
            Assert.IsType<Integer>(b);
            Assert.Equal(Real.NaN, a / b);
        }
        [Fact]
        public void TestWithUndefined7()
        {
            var a = Integer.Create(4);
            var b = Real.Create(0);
            Assert.Equal(Real.NaN, a / b);
        }

        [Fact]
        public void TestWithUndefined8()
        {
            var x = Rational.Create(-1, 2);
            Assert.IsType<Rational>(x);
            Assert.Equal(Real.NaN, x / 0);
        }

        [Fact]
        public void TestWithUndefined9()
        {
            var x = Rational.Create(-1, 2);
            var result = x / Real.PositiveInfinity;
            Assert.IsType<Integer>(result);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TestWithUndefined10()
        {
            var x = Real.Create(PeterO.Numbers.EDecimal.FromDouble(0.5));
            Assert.IsType<Rational>(x);
            var result = x / Real.PositiveInfinity;
            Assert.IsType<Integer>(result);
            Assert.Equal(0, result);
        }

        [Fact]
        public void TestWithUndefined11() =>
            Assert.Equal(Real.PositiveInfinity, Real.PositiveInfinity / 5);
        [Fact]
        public void TestWithUndefined12() =>
            Assert.Equal(Real.NegativeInfinity, Real.PositiveInfinity / -5);
        [Fact]
        public void TestWithUndefined13() =>
            Assert.Equal(Real.NaN, Real.PositiveInfinity / Real.NegativeInfinity);

        [Fact]
        public void TestWithUndefined14() =>
            Assert.Equal(Real.NegativeInfinity, Real.NegativeInfinity / 5);

        [Fact]
        public void TestWithUndefined15() =>
            Assert.Equal(Real.NaN, Real.NegativeInfinity / 0);

        [Fact]
        public void TestWithUndefined16() =>
            Assert.Equal(Real.NaN, Real.PositiveInfinity / 0);

        [Fact]
        public void TestWithUndefined17() =>
            Assert.Equal(Real.NaN, Real.NaN / 0);

        [Fact]
        public void TestWithUndefined18() =>
            Assert.Equal(Real.NaN, Real.Create(MathS.DecimalConst.pi) / 0);

        [Fact]
        public void TestWithUndefined19() =>
            Assert.Equal(Real.NaN, Complex.PosPosInfinity / 0);

        [Fact]
        public void TestWithUndefined20() =>
            Assert.Equal(Real.NaN, Complex.PosNegInfinity / 0);

        [Fact]
        public void TestWithUndefined21() =>
            Assert.Equal(Real.NaN, Complex.NegPosInfinity / 0);

        [Fact]
        public void TestWithUndefined22() =>
            Assert.Equal(Real.NaN, Complex.NegNegInfinity / 0);

        [Fact]
        public void TestWithUndefined23() =>
            Assert.Equal(Real.NaN, Complex.ImaginaryOne / 0);

        [Fact]
        public void TestWithUndefined24() =>
            Assert.Equal(Real.NaN, Complex.Create(2.13m, 4.21m) / 0);
        [Fact]
        public void TestImaginaryInfinity1() =>
            Assert.Equal(Complex.Create(0, Real.PositiveInfinity),
                Real.PositiveInfinity * Complex.ImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity2() =>
            Assert.Equal(Complex.Create(0, Real.NegativeInfinity),
                Real.NegativeInfinity * Complex.ImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity3() =>
            Assert.Equal(Real.NaN,
                Real.NaN * Complex.ImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity4() =>
            Assert.Equal(Complex.Create(0, Real.PositiveInfinity),
                Complex.ImaginaryOne * Real.PositiveInfinity);
        [Fact]
        public void TestImaginaryInfinity5() =>
            Assert.Equal(Complex.Create(0, Real.NegativeInfinity),
                Complex.ImaginaryOne * Real.NegativeInfinity);
        [Fact]
        public void TestImaginaryInfinity6() =>
            Assert.Equal(Real.NaN,
                Complex.ImaginaryOne * Real.NaN);
        [Fact]
        public void TestImaginaryInfinity7() =>
            Assert.Equal(Complex.Create(0, Real.NegativeInfinity),
                Real.PositiveInfinity * Complex.MinusImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity8() =>
            Assert.Equal(Complex.Create(0, Real.PositiveInfinity),
                Real.NegativeInfinity * Complex.MinusImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity9() =>
            Assert.Equal(Real.NaN,
                Real.NaN * Complex.MinusImaginaryOne);
        [Fact]
        public void TestImaginaryInfinity10() =>
            Assert.Equal(Complex.Create(0, Real.NegativeInfinity),
                Complex.MinusImaginaryOne * Real.PositiveInfinity);
        [Fact]
        public void TestImaginaryInfinity11() =>
            Assert.Equal(Complex.Create(0, Real.PositiveInfinity),
                Complex.MinusImaginaryOne * Real.NegativeInfinity);
        [Fact]
        public void TestImaginaryInfinity12() =>
            Assert.Equal(Real.NaN,
                Complex.MinusImaginaryOne * Real.NaN);
        [Fact]
        public void TestImaginaryInfinity13() =>
            Assert.Equal(Real.NegativeInfinity,
                Complex.Create(0, Real.PositiveInfinity) * Complex.Create(0, Real.PositiveInfinity));
        [Fact]
        public void TestImaginaryInfinity14() =>
            Assert.Equal(Real.PositiveInfinity,
                Complex.Create(0, Real.PositiveInfinity) * Complex.Create(0, Real.NegativeInfinity));
        [Fact]
        public void TestImaginaryInfinity15() =>
            Assert.Equal(Real.PositiveInfinity,
                Complex.Create(0, Real.NegativeInfinity) * Complex.Create(0, Real.PositiveInfinity));
        [Fact]
        public void TestImaginaryInfinity16() =>
            Assert.Equal(Real.NegativeInfinity,
                Complex.Create(0, Real.NegativeInfinity) * Complex.Create(0, Real.NegativeInfinity));

        [Theory]
        [InlineData(1, 0, 1, 0)]
        [InlineData(0, 1, 0, 1)]
        [InlineData(0, 2, 0, 1)]
        [InlineData(2, 0, 1, 0)]
        public void TestSignum(float argReal, float argImaginary,
            float expectedReal, float expectedImaginary)
            => Assert.Equal(
            Complex.Create(expectedReal, expectedImaginary),
            Complex.Signum(Complex.Create(argReal, argImaginary))
                );

        [Theory]
        [InlineData("1 / (a - 1)", "-1 / (1 - a)")]
        [InlineData("(a - 1) / (1 - b) - (1 - a) / (b - 1)", "0")]
        [InlineData("sin(x)2 + cos(x)2", "1")]
        public void TestAreNumericallyEqual(string one, string another)
        {
            var left = one.ToEntity();
            var right = another.ToEntity();
            Assert.True(MathS.UnsafeAndInternal.AreEqualNumerically(left, right));
        }

        [Theory]
        [InlineData("1 / (a - 1)", "-1 / (1 - a) + 1")]
        [InlineData("(a - 1) x", "a")]
        public void TestAreNumericallyNotEqual(string one, string another)
        {
            var left = one.ToEntity();
            var right = another.ToEntity();
            Assert.False(MathS.UnsafeAndInternal.AreEqualNumerically(left, right));
        }
        
        
        

        /// <summary>
        /// The principal root of a negative real is the root of the modulus times i, exactly:
        /// through the polar form it carried a real part of 6e-102 from cos(pi/2), and its
        /// square then lay a hair below the negative axis, where ln reads -i pi for the +i pi
        /// the number has. And a whole power written as a decimal -- which is how 2 arrives
        /// with the downcasting off -- is the exact multiplication, not the polar form.
        /// https://github.com/asc-community/AngouriMath/issues/1378
        /// </summary>
        [Theory]
        [InlineData("(-0.473)^(1/2)", "0", "0.6877499545619759617491613953754659852862020677583601831091659395819946673286686341485665164548954945")]
        [InlineData("(-4)^(1/2)", "0", "2")]
        [InlineData("(-4)^(3/2)", "0", "-8")]
        [InlineData("(-4)^(-3/2)", "0", "0.125")]
        [InlineData("(-2.5)^2", "6.25", "0")]
        [InlineData("(0.6877i)^2", "-0.47293129", "0")]
        [InlineData("((-0.473)^(1/2))^2", "-0.473", "0")]
        public void ARootOfANegativeRealIsExactlyImaginary(string expression, string realPart, string imaginaryPart)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            var value = (Complex)expression.ToEntity().EvalNumerical();
            Assert.True(value.RealPart.EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString(realPart)) == 0, $"real part {value.RealPart} for {realPart}");
            Assert.True(value.ImaginaryPart.EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString(imaginaryPart)) == 0, $"imaginary part {value.ImaginaryPart} for {imaginaryPart}");
        }

        /// <summary>
        /// The logarithm of that square is the logarithm of the negative real it is, on the
        /// principal branch: <c>ln(-0.473)</c> has imaginary part <c>+pi</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1378
        /// </summary>
        [Fact]
        public void TheLogarithmOfTheSquareOfARootOfANegativeRealTakesThePrincipalBranch()
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            var value = (Complex)"ln(((-0.473)^(1/2))^2)".ToEntity().EvalNumerical();
            var pi = (Real)MathS.pi.EvalNumerical();
            Assert.True(value.ImaginaryPart.EDecimal.Subtract(pi.EDecimal).Abs().CompareTo(PeterO.Numbers.EDecimal.Create(1, -90)) < 0, $"imaginary part {value.ImaginaryPart}");
        }

        /// <summary>
        /// A complex division formed the reciprocal of the divisor's squared modulus first,
        /// which for a divisor of 3 * 10^125 is 10^-251, under the context's exponent floor,
        /// and the quotient came out zero where it was 6 * 10^-7. Smith's method, far from
        /// one, never forms the square. https://github.com/asc-community/AngouriMath/issues/1372
        /// </summary>
        [Theory]
        [InlineData("1.98e119", "0", "3.39e125", "0", "5.8407079646017699115044247787610619469026548672566371681415929203539823008849557522123893805309734513e-7", "0")]
        [InlineData("1", "0", "1e80", "1e80", "5e-81", "-5e-81")]
        [InlineData("2", "3", "4", "-5", "-0.17073170731707317073170731707317073170731707317073170731707317073170731707317073170731707317073170732", "0.53658536585365853658536585365853658536585365853658536585365853658536585365853658536585365853658536585")]
        [InlineData("1", "2", "0", "1e90", "2e-90", "-1e-90")]
        public void AComplexDivisionWithALargeDivisor(string ar, string ai, string br, string bi, string qr, string qi)
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            static EDecimal D(string s) => EDecimal.FromString(s);
            var quotient = Complex.Create(D(ar), D(ai)) / Complex.Create(D(br), D(bi));
            var tolerance = EDecimal.FromString("1e-95");
            Assert.True(quotient.RealPart.EDecimal.Subtract(D(qr)).Abs().CompareTo(D(qr).Abs().Multiply(tolerance).Add(EDecimal.Create(1, -180))) <= 0, $"real part {quotient.RealPart} for {qr}");
            Assert.True(quotient.ImaginaryPart.EDecimal.Subtract(D(qi)).Abs().CompareTo(D(qi).Abs().Multiply(tolerance).Add(EDecimal.Create(1, -180))) <= 0, $"imaginary part {quotient.ImaginaryPart} for {qi}");
        }

        /// <summary>
        /// The modulus is made under the downcasting setting in force. It turned the downcasting on
        /// for itself, so with it off <c>|75 + 316.22776601683796i|</c>, which is <c>325 + 2.6e-17</c>,
        /// came back as 325. With it on, the setting rounds a value to half the working digits, which
        /// 2.6e-17 is far above, so it is the same value; a modulus that is 325 to the working digits is 325.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1615">#1615</a>
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1944">#1944</a>
        /// </summary>
        [Fact]
        public void AModulusIsMadeUnderTheDowncastingInForce()
        {
            // sqrt(75^2 + 316.22776601683796^2), from mpmath at 150 digits.
            var expected = EDecimal.FromString("325.0000000000000260767357490722475692366964200883807015522337353559864579715873660423128636761679756827129766030459244723400170041");
            using (MathS.Settings.DowncastingEnabled.Set(false))
            {
                var modulus = Complex.Create(EDecimal.FromInt32(75), EDecimal.FromString("316.22776601683796")).Abs();
                Assert.True(modulus.EDecimal.Subtract(expected).Abs().CompareTo(EDecimal.Create(1, -90)) < 0, $"{modulus}");
                var evaluated = "abs(75 + 316.22776601683796i)".ToEntity().EvalNumerical();
                Assert.True(evaluated.RealPart.EDecimal.Subtract(expected).Abs().CompareTo(EDecimal.Create(1, -90)) < 0, $"{evaluated}");
            }
            using (MathS.Settings.DowncastingEnabled.Set(true))
            {
                var modulus = Complex.Create(EDecimal.FromInt32(75), EDecimal.FromString("316.22776601683796")).Abs();
                Assert.True(modulus.EDecimal.Subtract(expected).Abs().CompareTo(EDecimal.Create(1, -90)) < 0, $"{modulus}");
                var rootOf100000 = EDecimal.FromInt32(100000).Sqrt(MathS.Settings.DecimalPrecisionContext);
                Assert.Equal(Integer.Create(325), Complex.Create(EDecimal.FromInt32(75), rootOf100000).Abs());
            }
        }

        /// <summary>
        /// A conjugate is made under the setting in force, as every other number is. It was kept on
        /// the instance, so a division by <c>i</c> with the downcasting off left <c>0 - 1i</c>, in
        /// decimals, as the conjugate of <see cref="Complex.ImaginaryOne"/>, and <c>conj(i)</c> was
        /// that for every caller after it: two tests failed or passed by the order the suite ran in.
        /// </summary>
        [Fact]
        public void AConjugateIsMadeUnderTheSettingInForce()
        {
            using (MathS.Settings.DowncastingEnabled.Set(false))
                Assert.IsType<Complex>((1 / Complex.ImaginaryOne).Evaled);
            Assert.Equal(Complex.MinusImaginaryOne, Complex.ImaginaryOne.Conjugate);
            Assert.Equal(Complex.MinusImaginaryOne, "conj(i)".ToEntity().InnerSimplified);
        }
    }
}
