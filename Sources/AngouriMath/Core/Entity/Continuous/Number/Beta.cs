//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            // Past this a product of whole factors, or a factorial, is not built exactly: the value is left to
            // evaluation, which takes the gamma function to the working precision.
            private const int LargestExactBetaArgument = 10000;

            /// <summary>
            /// <c>B(a, b)</c> exactly, at rational arguments where it is rational or a rational times <c>pi</c>:
            /// where either is a whole number, and where both are half-whole; <c>NaN</c> at a pole and <c>0</c> where only
            /// <c>a + b</c> is one; <see langword="null"/> elsewhere.
            /// </summary>
            /// <remarks>
            /// Where an argument is <c>0, -1, -2, ...</c> and <c>a + b</c> is as well, the other is a whole number
            /// <c>n &gt;= 1</c>, and the value is that of the rational function <c>(n - 1)!/(b (b + 1) ... (b + n - 1))</c>,
            /// whose factors do not vanish there: the limit along a whole <c>n</c>, as mpmath's <c>beta</c> takes it.
            /// Where both are, more gamma functions in the numerator have poles than in the denominator, and there is
            /// none. https://github.com/asc-community/AngouriMath/issues/2182
            /// </remarks>
            internal static Entity? ExactBeta(Rational a, Rational b)
            {
                var poleA = a is Integer { EInteger.Sign: <= 0 };
                var poleB = b is Integer { EInteger.Sign: <= 0 };
                var sum = a.ERational.Add(b.ERational);
                var poleSum = sum.IsInteger() && sum.Sign <= 0;
                if (poleA && poleB)
                    return Real.NaN;
                if (poleA || poleB)
                    return poleSum ? OfAWholeArgument(((Integer)(poleA ? b : a)).EInteger, poleA ? a : b) : Real.NaN;
                if (poleSum)
                    return Integer.Zero;
                if (a is Integer { EInteger: var n })
                    return OfAWholeArgument(n, b);
                if (b is Integer { EInteger: var m })
                    return OfAWholeArgument(m, a);
                if (!a.ERational.Denominator.Equals(EInteger.FromInt32(2)) || !b.ERational.Denominator.Equals(EInteger.FromInt32(2))
                    || a.ERational.Abs().CompareTo(ERational.FromInt32(LargestExactBetaArgument)) > 0
                    || b.ERational.Abs().CompareTo(ERational.FromInt32(LargestExactBetaArgument)) > 0)
                    return null;
                // Gamma(a) Gamma(b) is a rational times pi, and Gamma(a + b) = (a + b - 1)! with a + b >= 1 whole.
                var whole = sum.ToEInteger();
                return Rational.Create(GammaOverRootPi(a.ERational.Numerator).Multiply(GammaOverRootPi(b.ERational.Numerator))
                    .Divide(ERational.FromEInteger((whole - 1).Factorial()))) * MathS.pi;
            }

            /// <summary>
            /// <c>B(n, b) = (n - 1)!/(b (b + 1) ... (b + n - 1))</c>, for a whole <c>n &gt;= 1</c> and a <c>b</c> where
            /// no factor vanishes, or <see langword="null"/> for an <c>n</c> too large to take.
            /// </summary>
            private static Entity? OfAWholeArgument(EInteger n, Rational b)
            {
                if (n.CompareTo(EInteger.FromInt32(LargestExactBetaArgument)) > 0)
                    return null;
                var count = n.ToInt32Checked();
                var product = ERational.One;
                for (var k = 0; k < count; k++)
                    product = product.Multiply(b.ERational.Add(ERational.FromInt32(k)));
                return Rational.Create(ERational.FromEInteger((n - 1).Factorial()).Divide(product));
            }

            /// <summary>
            /// <c>Gamma(h/2)/sqrt(pi)</c> for an odd <c>h</c>, as <see cref="Factorialf"/> takes <c>(h/2 - 1)!</c>:
            /// <c>(2k)!/(4^k k!)</c> at <c>h/2 = k + 1/2</c>, and <c>(-4)^k k!/(2k)!</c> at <c>h/2 = 1/2 - k</c>.
            /// </summary>
            private static ERational GammaOverRootPi(EInteger h)
            {
                if (h.Sign > 0)
                {
                    var k = (h - 1) / 2;
                    return ERational.Create((2 * k).Factorial(), EInteger.FromInt32(4).Pow(k) * k.Factorial());
                }
                var j = (1 - h) / 2;
                return ERational.Create(EInteger.FromInt32(-4).Pow(j) * j.Factorial(), (2 * j).Factorial());
            }

            /// <summary>
            /// <c>B(a, b)</c> at the working precision, real or complex, as <see cref="ExactBeta"/> takes its poles;
            /// <see langword="null"/> at an infinite argument.
            /// </summary>
            /// <remarks>
            /// <c>Gamma(a) Gamma(b)/Gamma(a + b)</c>, each gamma at the working precision, so that the value is within a
            /// few units in its last digit: there is no subtraction to lose digits to. A real argument goes to the real
            /// gamma function, which takes a large whole one by Spouge's approximation rather than as an exact factorial
            /// to be rounded. https://github.com/asc-community/AngouriMath/issues/2182
            /// </remarks>
            internal static Complex? Beta(Complex a, Complex b)
            {
                var parts = new[] { a.RealPart.EDecimal, a.ImaginaryPart.EDecimal, b.RealPart.EDecimal, b.ImaginaryPart.EDecimal };
                foreach (var part in parts)
                    if (part.IsNaN())
                        return Real.NaN;
                foreach (var part in parts)
                    if (!part.IsFinite)
                        return null;
                var sum = a + b;
                var (wholeA, wholeB, wholeSum) = (WholeOrNull(a), WholeOrNull(b), WholeOrNull(sum));
                var (poleA, poleB) = (wholeA is { EInteger.Sign: <= 0 }, wholeB is { EInteger.Sign: <= 0 });
                if (poleA && poleB)
                    return Real.NaN;
                // With a pole in one argument and in a + b, the other argument is whole too.
                if (poleA || poleB)
                    return wholeSum is { EInteger.Sign: <= 0 } ? ExactBeta(wholeA!, wholeB!)?.Evaled as Complex : Real.NaN;
                if (wholeSum is { EInteger.Sign: <= 0 })
                    return Integer.Zero;
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var working = BetaWorkingContext(context);
                Complex value;
                using (MathS.Settings.DowncastingEnabled.Set(false))
                using (MathS.Settings.DecimalPrecisionContext.Set(working))
                    value = GammaAt(a, working) * GammaAt(b, working) / GammaAt(sum, working);
                // The value of a product and a quotient, which is never a residual of cancellation, however small:
                // B(100.5, 100.25) is 1.3e-61, and was 0. A part that is small beside the other is one, its scale the
                // larger part's. https://github.com/asc-community/AngouriMath/issues/1944
                var (re, im) = (value.RealPart.EDecimal.RoundToPrecision(context), value.ImaginaryPart.EDecimal.RoundToPrecision(context));
                var scale = EDecimal.Max(re.Abs(), im.Abs());
                return Complex.CreateAtScales(re, im, im.IsZero ? EDecimal.Zero : scale, re.IsZero ? EDecimal.Zero : scale);

                static Integer? WholeOrNull(Complex z)
                    => z.ImaginaryPart.EDecimal.IsZero && z.RealPart.EDecimal.IsInteger() ? Integer.Create(z.RealPart.EDecimal.ToEInteger()) : null;
            }

            /// <summary>
            /// <paramref name="context"/> with sixteen more digits and no bound on the exponent, made once per precision
            /// since the library caches its constants per context: the gamma functions of the ratio are far past the
            /// exponents the caller's context allows where their ratio is not, <c>B(10^5, 10^5)</c> being about
            /// <c>10^(-60208)</c> with the gamma functions near <c>10^456568</c> and <c>10^973345</c>, and the ratio of two infinities was <c>NaN</c>.
            /// </summary>
            private static EContext BetaWorkingContext(EContext context)
                => betaWorkingContexts.GetOrAdd(context.Precision.ToInt32Checked() + 16,
                    static digits => EContext.ForPrecision(digits).WithRounding(ERounding.HalfEven).WithUnlimitedExponents());
            [ConcurrentField] private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, EContext> betaWorkingContexts = new();

            private static Complex GammaAt(Complex z, EContext context)
                => z.ImaginaryPart.EDecimal.IsZero
                    ? Real.Create(z.RealPart.EDecimal.Subtract(EDecimal.One, context).Factorial(context))
                    : Gamma(z);
        }
    }
}
