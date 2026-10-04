//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using AngouriMath.Numerics;
using PeterO.Numbers;
using Xunit;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// The interval evaluation holds the value it stands for -- the hundred-digit decimal
    /// evaluation's, inside its bounds -- and is narrow where nothing cancels, declines what it
    /// does not read, and compares in three outcomes.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1019">#1019</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class IntervalEvaluationTest
    {
        [Theory]
        [InlineData("sqrt(2)")]
        [InlineData("pi")]
        [InlineData("e^2")]
        [InlineData("sin(1)")]
        [InlineData("cos(1/3)")]
        [InlineData("tan(3/10)")]
        [InlineData("sec(1) + csc(2) + cot(3)")]
        [InlineData("ln(7)")]
        [InlineData("log(2, 10)")]
        [InlineData("arctan(2)")]
        [InlineData("arcsin(1/3)")]
        [InlineData("arccos(-2/3)")]
        [InlineData("arccot(2)")]
        [InlineData("arccot(-3/2)")]
        [InlineData("arccot(0)")]
        [InlineData("arcsec(3)")]
        [InlineData("arccsc(-2)")]
        [InlineData("(2/3)^(1/3)")]
        [InlineData("(1 + 2i)^3")]
        [InlineData("1/(3 + 4i)")]
        [InlineData("e^(i * pi/4)")]
        [InlineData("sqrt(-3)")]
        [InlineData("sin(1 + i)")]
        [InlineData("cos(2 - 3i)")]
        [InlineData("ln(-2 + i)")]
        [InlineData("ln(-2)")]
        [InlineData("abs(-3 + 4i)")]
        [InlineData("(sin(1)^2 + cos(1)^2) * 7/3")]
        [InlineData("sqrt(2) provided 1 < 2")]
        [InlineData("piecewise(1 provided 2 < 1, sin(1) provided 3 > 2)")]
        [InlineData("sqrt(2) provided -4 - 4 * (137/100)^2 in RR")]
        [InlineData("sqrt(2) provided sqrt(-2) in CC")]
        [InlineData("iverson(2 > 1) * sqrt(2) + iverson(sin(1) < 0)")]
        public void TheIntervalHoldsTheValue(string expression)
        {
            var expr = expression.ToEntity();
            var interval = IntervalEvaluation.Of(expr);
            Assert.True(interval.HasValue, $"{expression} was not evaluated");
            var value = expr.EvalNumerical();
            Holds(interval!.Value.Re, value.RealPart.EDecimal, expression, "real");
            Holds(interval.Value.Im, value.ImaginaryPart.EDecimal, expression, "imaginary");
        }

        private static void Holds(Interval interval, EDecimal value, string expression, string part)
        {
            Assert.True(EDecimal.FromDouble(interval.Low).CompareTo(value) <= 0 && EDecimal.FromDouble(interval.High).CompareTo(value) >= 0,
                $"the {part} part of {expression}, {value}, is outside [{interval.Low:R}, {interval.High:R}]");
            var scale = System.Math.Max(1.0, interval.Magnitude);
            Assert.True(interval.High - interval.Low <= 1e-12 * scale,
                $"the {part} part of {expression} is [{interval.Low:R}, {interval.High:R}], wider than it has to be");
        }

        /// <summary>
        /// The special functions, from their double-precision routines. Each is allowed <c>1e-10</c>
        /// of its value, and <c>1e-10</c> more near a zero, against a measured error of at most
        /// <c>3e-13</c>. An argument that is itself an interval, <c>sqrt(2)</c> or <c>pi</c>, widens the
        /// value by the most the function moves across it. A real value stays real where the
        /// function is real, and <c>Ci(-2)</c> is <c>Ci(2) + i pi</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1607">#1607</a>
        /// </summary>
        [Theory]
        [InlineData("erf(1/2)")]
        [InlineData("erf(sqrt(2))")]
        [InlineData("erfc(3)")]
        [InlineData("erfi(1 + i)")]
        [InlineData("erf(2 - 3i)")]
        [InlineData("Ei(2)")]
        [InlineData("Ei(-3/2)")]
        [InlineData("Ei(1 + 2i)")]
        [InlineData("li(5)")]
        [InlineData("li(1/3)")]
        [InlineData("Si(pi)")]
        [InlineData("Si(7)")]
        [InlineData("Ci(3)")]
        [InlineData("Ci(-2)")]
        [InlineData("Shi(-1)")]
        [InlineData("Chi(4)")]
        [InlineData("erf(1/2) + Si(2) * Ei(1)")]
        public void TheIntervalHoldsASpecialFunctionsValue(string expression)
        {
            var expr = expression.ToEntity();
            var interval = IntervalEvaluation.Of(expr);
            Assert.True(interval.HasValue, $"{expression} was not evaluated");
            var value = expr.EvalNumerical();
            HoldsWithin(interval!.Value.Re, value.RealPart.EDecimal, 1e-9, expression, "real");
            HoldsWithin(interval.Value.Im, value.ImaginaryPart.EDecimal, 1e-9, expression, "imaginary");
            if (value.ImaginaryPart.EDecimal.IsZero)
                Assert.True(interval.Value.IsReal, $"{expression} is real, and its interval is not");
        }

        private static void HoldsWithin(Interval interval, EDecimal value, double width, string expression, string part)
        {
            Assert.True(EDecimal.FromDouble(interval.Low).CompareTo(value) <= 0 && EDecimal.FromDouble(interval.High).CompareTo(value) >= 0,
                $"the {part} part of {expression}, {value}, is outside [{interval.Low:R}, {interval.High:R}]");
            Assert.True(interval.High - interval.Low <= width * System.Math.Max(1.0, interval.Magnitude),
                $"the {part} part of {expression} is [{interval.Low:R}, {interval.High:R}], wider than it has to be");
        }

        /// <summary>
        /// Declined, in doubles and in decimals: a variable, a pole, the logarithm of zero, a node
        /// the evaluation does not read, and a value whose condition fails.
        /// </summary>
        [Theory]
        [InlineData("x + 1")]
        [InlineData("1/0")]
        [InlineData("ln(0)")]
        [InlineData("tan(pi/2)")]
        [InlineData("arcsec(1/2)")]
        [InlineData("3!")]
        [InlineData("1 provided 3 < 2")]
        [InlineData("1 provided sqrt(-2) in RR")]
        [InlineData("Ci(0)")]
        [InlineData("li(1)")]
        [InlineData("Ci(-2 + sin(pi) * i)")]
        [InlineData("Ei(sin(pi))")]
        [InlineData("iverson(sin(pi) > 0)")]
        public void UndecidedWhereItShouldBe(string expression)
        {
            Assert.Null(IntervalEvaluation.Of(expression.ToEntity()));
            Assert.Null(PreciseEvaluation.Of(expression.ToEntity(), 40));
        }

        /// <summary>
        /// Where cancellation leaves double intervals too wide to tell, forty digits tell: the
        /// hundred million cancels and leaves the sine, which the doubles hold only to within a
        /// few hundred-millionths.
        /// </summary>
        [Fact]
        public void MoreDigitsSettleWhatDoublesCannot()
        {
            var cancelled = "(10^8 + sin(1)) - 10^8".ToEntity();
            var sine = "sin(1)".ToEntity();
            Assert.Null(IntervalEvaluation.Agree(IntervalEvaluation.Of(cancelled)!.Value, IntervalEvaluation.Of(sine)!.Value, 1e-9));
            Assert.True(PreciseEvaluation.Agree(PreciseEvaluation.Of(cancelled, 40)!.Value, PreciseEvaluation.Of(sine, 40)!.Value, 1e-9));
            var nearby = "sin(1) + 1/1000000".ToEntity();
            Assert.False(PreciseEvaluation.Agree(PreciseEvaluation.Of(nearby, 40)!.Value, PreciseEvaluation.Of(sine, 40)!.Value, 1e-9));
        }

        [Theory]
        [InlineData("sqrt(2)")]
        [InlineData("e^2 - pi")]
        [InlineData("sin(1 + i) * cos(2 - 3i)")]
        [InlineData("ln(-2 + i) + arctan(2) - arcsin(1/3)")]
        [InlineData("(2/3)^(1/3) + sec(1) + csc(2)")]
        [InlineData("arccot(-2) + arcsec(3) + arccsc(-2) + arccot(0)")]
        [InlineData("sin(10^6) + cos(10^6)")]
        [InlineData("sqrt(2) provided 1 < 2")]
        [InlineData("sqrt(2) provided -4 - 4 * (137/100)^2 in RR and sqrt(-2) in CC")]
        [InlineData("erf(1/2) + erfc(3) + erfi(-2/3)")]
        [InlineData("erf(1 + i)")]
        [InlineData("Ei(3/2) + Ei(-2)")]
        [InlineData("Ei(1 - 2i)")]
        [InlineData("li(1/2) + li(3)")]
        [InlineData("li(-2)")]
        [InlineData("Si(2) + Si(-3 + i)")]
        [InlineData("Ci(3/10)")]
        [InlineData("Ci(-3)")]
        [InlineData("Ci(2 + 5i)")]
        [InlineData("Ci(2 + sin(pi) * i)")]
        [InlineData("Shi(3/2) + Chi(-1) + Chi(1 + 2i)")]
        [InlineData("e^(-2) * Ei(sqrt(3)) + Si(pi/4)^2")]
        [InlineData("iverson(pi > 3) * sqrt(2) + iverson(e < 2)")]
        public void ThePreciseIntervalHoldsTheValue(string expression)
        {
            var expr = expression.ToEntity();
            var interval = PreciseEvaluation.Of(expr, 40);
            Assert.True(interval.HasValue, $"{expression} was not evaluated");
            var value = expr.EvalNumerical();
            Assert.True(interval!.Value.Re.Low.CompareTo(value.RealPart.EDecimal) <= 0 && interval.Value.Re.High.CompareTo(value.RealPart.EDecimal) >= 0,
                $"the real part of {expression}, {value.RealPart}, is outside [{interval.Value.Re.Low}, {interval.Value.Re.High}]");
            Assert.True(interval.Value.Im.Low.CompareTo(value.ImaginaryPart.EDecimal) <= 0 && interval.Value.Im.High.CompareTo(value.ImaginaryPart.EDecimal) >= 0,
                $"the imaginary part of {expression}, {value.ImaginaryPart}, is outside [{interval.Value.Im.Low}, {interval.Value.Im.High}]");
            Assert.True(interval.Value.Re.High.Subtract(interval.Value.Re.Low).Abs().CompareTo(EDecimal.FromString("1E-30")) <= 0,
                $"the real part of {expression} is wider than forty digits should leave it");
        }

        /// <summary>
        /// A whole power at the end of the range of a long is not negated into itself: the
        /// reciprocal of <c>2^(2^63)</c> was <c>2^(-2^63)</c> again, and the recursion never ended.
        /// </summary>
        [Fact]
        public void TheSmallestLongPowerFinishes()
        {
            var power = MathS.Pow(2, Entity.Number.Integer.Create(long.MinValue));
            Assert.Equal(0, IntervalEvaluation.Of(power)!.Value.Re.Mignitude);
            Assert.Equal(0, PreciseEvaluation.Of(power, 40)!.Value.Re.Mignitude.Sign);
        }

        /// <summary>
        /// An exact zero factor makes the product zero, whatever the other factor -- as the
        /// decimal evaluation's simplification of <c>0 x</c> has it. A derivative can hold a term
        /// whose coefficient is exactly zero at the pinned values beside a factor with no value
        /// there, and reading the product as undefined lost Rubi's
        /// <c>1/(a + b csch(c + d x)^2)</c>.
        /// </summary>
        [Theory]
        [InlineData("0 * (1/0)")]
        [InlineData("(1/0) * 0")]
        public void AnExactZeroFactorIsZero(string expression)
        {
            Assert.True(IntervalEvaluation.Of(expression.ToEntity()) is { Re.IsZero: true, Im.IsZero: true });
            Assert.True(PreciseEvaluation.Of(expression.ToEntity(), 40) is { Re.IsZero: true, Im.IsZero: true });
        }

        /// <summary>
        /// The special functions are read, so an answer whose derivative keeps one -- by parts,
        /// <c>x erf(x)</c> differentiates to <c>erf(x)</c> and more -- is checked, where reading
        /// none of them left no point to compare at and the answer was turned away. On the real
        /// line left of 0 the values are the ones on the cut: <c>Chi(-0.61)</c> is
        /// <c>Chi(0.61) + i pi</c>, and <c>li(-0.61)</c> is complex.
        /// </summary>
        [Theory]
        [InlineData("x * erf(x) + e^(-x^2)/sqrt(pi)", "erf(x)")]
        [InlineData("x * erfi(x) - e^(x^2)/sqrt(pi)", "erfi(x)")]
        [InlineData("x * Si(x) + cos(x)", "Si(x)")]
        [InlineData("x * Ci(x) - sin(x)", "Ci(x)")]
        [InlineData("x * Ei(x) - e^x", "Ei(x)")]
        [InlineData("x * Chi(x) - sinh(x)", "Chi(x)")]
        [InlineData("x * li(x) - Ei(2 * ln(x))", "li(x)")]
        public void ADerivativeKeepingASpecialFunctionIsChecked(string antiderivative, string integrand)
        {
            var x = MathS.Var("x");
            var derivative = antiderivative.ToEntity().Differentiate(x);
            Assert.True(Functions.PartialFractions.HoldsAtSampledPoints(derivative, integrand.ToEntity(), x));
            Assert.False(Functions.PartialFractions.HoldsAtSampledPoints(derivative, (integrand + " + 1/1000").ToEntity(), x));
        }

        [Fact]
        public void AgreementHasThreeOutcomes()
        {
            var one = IntervalEvaluation.Of("1".ToEntity())!.Value;
            var identity = IntervalEvaluation.Of("sin(1)^2 + cos(1)^2".ToEntity())!.Value;
            var nearby = IntervalEvaluation.Of("1 + 1/1000000".ToEntity())!.Value;
            Assert.True(IntervalEvaluation.Agree(identity, one, 1e-9));
            Assert.False(IntervalEvaluation.Agree(nearby, one, 1e-9));
            var wide = ComplexInterval.Real(new Interval(0.9, 1.1));
            Assert.Null(IntervalEvaluation.Agree(wide, one, 1e-9));
        }
    }
}
