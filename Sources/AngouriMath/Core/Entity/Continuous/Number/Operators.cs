//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core.Exceptions;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        public abstract partial record Number
        {
            internal static EInteger CtxAdd(EInteger a, EInteger b) => a.Add(b);
            internal static EInteger CtxSubtract(EInteger a, EInteger b) => a.Subtract(b);
            internal static EInteger CtxMultiply(EInteger a, EInteger b) => a.Multiply(b);
            internal static ERational? CtxDivide(EInteger a, EInteger b) =>
                b.IsZero ? null : ERational.Create(a, b);
            internal static EInteger CtxMod(EInteger a, EInteger b) => a.Remainder(b);
            internal static EInteger CtxPow(EInteger a, EInteger b) => a.Pow(b);
            internal static ERational CtxAdd(ERational a, ERational b) => a.Add(b);
            internal static ERational CtxSubtract(ERational a, ERational b) => a.Subtract(b);
            internal static ERational CtxMultiply(ERational a, ERational b) => a.Multiply(b);
            internal static ERational? CtxDivide(ERational a, ERational b) =>
                b.IsZero ? null : a.Divide(b);
            internal static ERational CtxMod(ERational a, ERational b) => a.Remainder(b);
            internal static EDecimal CtxAdd(EDecimal a, EDecimal b)
                => a.Add(b, MathS.Settings.DecimalPrecisionContext);
            internal static EDecimal CtxSubtract(EDecimal a, EDecimal b)
                => a.Subtract(b, MathS.Settings.DecimalPrecisionContext);
            internal static EDecimal CtxMultiply(EDecimal a, EDecimal b)
                => a.Multiply(b, MathS.Settings.DecimalPrecisionContext);
            // To the working precision's number of decimal places, which is the precision's digits for a quotient
            // near one and cheaper than dividing to digits; a quotient that has visibly lost digits that way is
            // divided again to them: to places, 3.5e-68 kept thirty-three digits at a hundred, and a quotient below
            // 1e-100 was zero. https://github.com/asc-community/AngouriMath/issues/1944
            internal static EDecimal CtxDivide(EDecimal a, EDecimal b)
            {
                if (b.IsZero)
                    return EDecimal.NaN;
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var quotient = a.DivideToExponent(b, -context.Precision);
                // 3.3 bits a digit, and five digits to spare; an unlimited context keeps the division to places.
                if (!context.Precision.IsZero && quotient.IsFinite && !a.IsZero
                    && quotient.UnsignedMantissa.GetUnsignedBitLengthAsInt64() < (context.Precision.ToInt64Checked() - 5) * 3)
                    return a.Divide(b, context);
                return quotient;
            }
            internal static EDecimal CtxMod(EDecimal a, EDecimal b)
                => a.Remainder(b, MathS.Settings.DecimalPrecisionContext);
            internal static EDecimal CtxPow(EDecimal a, EDecimal b)
                => a.Pow(b, MathS.Settings.DecimalPrecisionContext);
            internal static T Min<T>(T a, T b) where T : Real => a < b ? a : b;

            /// <summary>
            /// The scale of what cancels in <c>a + b</c>: a term where their signs differ, and zero where they cannot
            /// cancel, so that a sum of like signs is never a residual.
            /// https://github.com/asc-community/AngouriMath/issues/1944
            /// </summary>
            internal static EDecimal CancelledScale(EDecimal a, EDecimal b)
                // Either term: where they nearly cancel they are alike, and where they do not the sum is near the
                // larger and no residual of either. Comparing them rescaled a mantissa, 500 bytes at a hundred digits.
                => a.Sign * b.Sign < 0 ? a : EDecimal.Zero;

            /// <summary>The scale of what cancels in <c>a - b</c>, as <see cref="CancelledScale"/>.</summary>
            internal static EDecimal CancelledScaleOfADifference(EDecimal a, EDecimal b)
                => a.Sign * b.Sign > 0 ? a : EDecimal.Zero;
            internal static T Max<T>(T a, T b) where T : Real => a > b ? a : b;

            /// <summary>
            /// This function serves not only convenience but also protects from unexpected cases, for example,
            /// if a new type added
            /// </summary>
            protected static T SuperSwitch<T>(
                Number num1, Number num2,
                Func<Integer, Integer, T> ifInt,
                Func<Rational, Rational, T> ifRat,
                Func<Real, Real, T> ifReal,
                Func<Complex, Complex, T> ifCom
            ) => (num1, num2) switch
            {
                (Integer n1, Integer n2) => ifInt(n1, n2),
                (Rational r1, Rational r2) => ifRat(r1, r2),
                (Real r1, Real r2) => ifReal(r1, r2),
                (Complex c1, Complex c2) => ifCom(c1, c2),
                _ => throw new AngouriBugException($"({num1.GetType()}, {num2.GetType()}) is not supported.")
            };

            /// <summary>
            /// This function serves not only convenience but also protects from unexpected cases, for example,
            /// if a new type added
            /// </summary>
            protected static T SuperSwitch<T>(
                T num1, T num2,
                Func<Integer, Integer, Integer> ifInt,
                Func<Rational, Rational, Rational> ifRat,
                Func<Real, Real, Real> ifReal,
                Func<Complex, Complex, Complex> ifCom
            ) where T : Number
                => (T)(Number)((num1, num2) switch
                {
                    (Integer n1, Integer n2) => ifInt(n1, n2),
                    (Rational r1, Rational r2) => ifRat(r1, r2),
                    (Real r1, Real r2) => ifReal(r1, r2),
                    (Complex c1, Complex c2) => ifCom(c1, c2),
                    _ => throw new AngouriBugException($"({num1.GetType()}, {num2.GetType()}) is not supported.")
                });

            internal static T OpSum<T>(T a, T b) where T : Number =>
                SuperSwitch<T>(a, b,
                    (a, b) => Integer.Create(CtxAdd(a.EInteger, b.EInteger)),
                    (a, b) => Rational.Create(CtxAdd(a.ERational, b.ERational)),
                    (a, b) => Real.CreateAtScale(CtxAdd(a.EDecimal, b.EDecimal), CancelledScale(a.EDecimal, b.EDecimal)),
                    (a, b) => Complex.CreateAtScales(CtxAdd(a.RealPart.EDecimal, b.RealPart.EDecimal),
                                             CtxAdd(a.ImaginaryPart.EDecimal, b.ImaginaryPart.EDecimal),
                                             CancelledScale(a.RealPart.EDecimal, b.RealPart.EDecimal),
                                             CancelledScale(a.ImaginaryPart.EDecimal, b.ImaginaryPart.EDecimal))
                    );

            internal static T OpSub<T>(T a, T b) where T : Number =>
                SuperSwitch<T>(a, b,
                    (a, b) => Integer.Create(CtxSubtract(a.EInteger, b.EInteger)),
                    (a, b) => Rational.Create(CtxSubtract(a.ERational, b.ERational)),
                    (a, b) => Real.CreateAtScale(CtxSubtract(a.EDecimal, b.EDecimal), CancelledScaleOfADifference(a.EDecimal, b.EDecimal)),
                    (a, b) => Complex.CreateAtScales(CtxSubtract(a.RealPart.EDecimal, b.RealPart.EDecimal),
                                             CtxSubtract(a.ImaginaryPart.EDecimal, b.ImaginaryPart.EDecimal),
                                             CancelledScaleOfADifference(a.RealPart.EDecimal, b.RealPart.EDecimal),
                                             CancelledScaleOfADifference(a.ImaginaryPart.EDecimal, b.ImaginaryPart.EDecimal))
                    );

            internal static T OpMul<T>(T a, T b) where T : Number =>
                SuperSwitch<T>(a, b,
                    (a, b) => Integer.Create(CtxMultiply(a.EInteger, b.EInteger)),
                    (a, b) => Rational.Create(CtxMultiply(a.ERational, b.ERational)),
                    (a, b) => Real.CreateAtScale(CtxMultiply(a.EDecimal, b.EDecimal), EDecimal.Zero),
                    (a, b) =>
                    {
                    // A whole zero against a whole infinity is 0 * oo, which is NaN for a real
                    // and is NaN here: the reading below, that a zero *part* times an infinite
                    // part is zero, is for one part against another and read 0 * (i * oo) as
                    // 0, whereupon (2 + i * x) * abs(2 + i * x)^(-1) at +oo answered 0.
                    // https://github.com/asc-community/AngouriMath/issues/1186
                    if (IsZero(a) && !b.IsFinite || IsZero(b) && !a.IsFinite)
                        return Real.NaN;
                    // Define both (oo * i) and (i * oo) to be (oo i) which is (0 + oo i) instead of (NaN + oo i)
                    static EDecimal ModifiedMultiply(EDecimal a, EDecimal b) =>
                            a.IsInfinity() && b.IsZero || b.IsInfinity() && a.IsZero ? EDecimal.Zero : CtxMultiply(a, b);
                        var (ac, bd) = (ModifiedMultiply(a.RealPart.EDecimal, b.RealPart.EDecimal), ModifiedMultiply(a.ImaginaryPart.EDecimal, b.ImaginaryPart.EDecimal));
                        var (ad, bc) = (ModifiedMultiply(a.RealPart.EDecimal, b.ImaginaryPart.EDecimal), ModifiedMultiply(a.ImaginaryPart.EDecimal, b.RealPart.EDecimal));
                        return Complex.CreateAtScales(CtxSubtract(ac, bd), CtxAdd(ad, bc), CancelledScaleOfADifference(ac, bd), CancelledScale(ad, bc));
                    }
                    );

            internal static Complex OpDiv<T>(T a, T b) where T : Number =>
                SuperSwitch<Complex>(a, b,
                    (a, b) => CtxDivide(a.ERational, b.ERational) is { } n ? Rational.Create(n) : Real.NaN,
                    (a, b) => CtxDivide(a.ERational, b.ERational) is { } n ? Rational.Create(n) : Real.NaN,
                    (a, b) => Real.CreateAtScale(CtxDivide(a.EDecimal, b.EDecimal), EDecimal.Zero),
                    (a, b) =>
                    {
                        /*
                        * (a + ib) / (c + id) = (a + ib) * (1 / (c + id))
                        * 1 / (c + id) = (c2 + d2) / (c + id) / (c2 + d2) = (c - id) / (c2 + d2)
                        * => ans = (a + ib) * (c - id) / (c2 + d2)
                        */
                        var conj = b.Conjugate;
                        // The modulus under the downcasting setting in force, which Abs() now keeps.
                        // With it off, the modulus as it is: 75 + 316.22776601683796i, whose modulus
                        // is 325 + 2.6e-17, was divided as if by one of 325. A relative 8e-20 is
                        // nothing to one quotient, and it put a factorial 24 orders of magnitude out,
                        // whose series of such quotients cancels its terms almost entirely. With it on,
                        // the rounding is the setting's own, and it keeps the quotients of a divisor
                        // like 1 + i sqrt(3) exact.
                        // https://github.com/asc-community/AngouriMath/issues/1614
                        // https://github.com/asc-community/AngouriMath/issues/1615
                        var bAbs = b.Abs().EDecimal;
                        // The squared modulus of a divisor of 3 * 10^125 is 10^251 and its
                        // reciprocal is under the context's exponent floor, so the quotient
                        // came out zero where it was 6 * 10^-7 -- a right antiderivative's
                        // derivative read as wrong at one point. Far from one, Smith's method,
                        // which never forms the square; near one, the route as it was, whose
                        // digits are pinned to the last place by the tests.
                        // https://github.com/asc-community/AngouriMath/issues/1372
                        if (bAbs.IsFinite && !bAbs.IsZero && bAbs.Exponent.Add(bAbs.Precision()).Abs().CompareTo(EInteger.FromInt32(40)) > 0)
                            return DivideBySmith(a, b);
                        var abs2 = CtxMultiply(bAbs, bAbs);
                        var Re = CtxDivide(conj.RealPart.EDecimal, abs2);
                        var Im = CtxDivide(conj.ImaginaryPart.EDecimal, abs2);
                        var c = Complex.CreateAtScales(Re, Im, EDecimal.Zero, EDecimal.Zero);
                        return a * c;
                    }
                    );

            /// <summary>
            /// <c>(a + ib)/(c + id)</c> by Smith's method: the smaller of <c>c</c> and <c>d</c>
            /// over the larger, and a denominator of the larger's size, so that nothing squared
            /// is formed and a divisor of any size within the context's range divides.
            /// </summary>
            private static Complex DivideBySmith(Complex a, Complex b)
            {
                var (ar, ai) = (a.RealPart.EDecimal, a.ImaginaryPart.EDecimal);
                var (c, d) = (b.RealPart.EDecimal, b.ImaginaryPart.EDecimal);
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                // A part is a residual only of the sum in its numerator, at that sum's scale over the
                // denominator's; the denominator is a sum of like signs, and a quotient cancels nothing.
                EDecimal Over(EDecimal scale, EDecimal denominator) => scale.IsZero ? scale : scale.Abs().Divide(denominator.Abs(), context);
                if (d.IsZero)
                    return Complex.CreateAtScales(CtxDivide(ar, c), CtxDivide(ai, c), EDecimal.Zero, EDecimal.Zero);
                if (c.IsZero)
                    return Complex.CreateAtScales(CtxDivide(ai, d), CtxDivide(ar.Negate(), d), EDecimal.Zero, EDecimal.Zero);
                if (c.Abs().CompareTo(d.Abs()) >= 0)
                {
                    var ratio = CtxDivide(d, c);
                    var denominator = CtxAdd(c, CtxMultiply(d, ratio));
                    var (aiRatio, arRatio) = (CtxMultiply(ai, ratio), CtxMultiply(ar, ratio));
                    return Complex.CreateAtScales(
                        CtxDivide(CtxAdd(ar, aiRatio), denominator),
                        CtxDivide(CtxSubtract(ai, arRatio), denominator),
                        Over(CancelledScale(ar, aiRatio), denominator),
                        Over(CancelledScaleOfADifference(ai, arRatio), denominator));
                }
                else
                {
                    var ratio = CtxDivide(c, d);
                    var denominator = CtxAdd(CtxMultiply(c, ratio), d);
                    var (arRatio, aiRatio) = (CtxMultiply(ar, ratio), CtxMultiply(ai, ratio));
                    return Complex.CreateAtScales(
                        CtxDivide(CtxAdd(arRatio, ai), denominator),
                        CtxDivide(CtxSubtract(aiRatio, ar), denominator),
                        Over(CancelledScale(arRatio, ai), denominator),
                        Over(CancelledScaleOfADifference(aiRatio, ar), denominator));
                }
            }

            /// <summary>
            /// Checks whether a number is zero
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public static bool IsZero(Real num)
            {
                if (!num.IsFinite)
                    return false;
                return IsZero(num.EDecimal);
            }

            /// <summary>
            /// Checks whether a number is zero
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public static bool IsZero(EDecimal num)
            {
                return num.Abs().LessThan(MathS.Settings.PrecisionErrorZeroRange);
            }

            /// <summary>
            /// Checks whether a number is zero
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public static bool IsZero(Complex num) => IsZero(num.RealPart) && IsZero(num.ImaginaryPart);

            internal static bool AreEqual<T>(T a, T b) where T : Number =>
                SuperSwitch<bool>(a, b,
                    (a, b) => a.EInteger.Equals(b.EInteger),
                    (a, b) => a.ERational.Equals(b.ERational),
                    (a, b) => a.IsFinite && b.IsFinite
                                && CtxSubtract(a.EDecimal, b.EDecimal).Abs().LessThan(MathS.Settings.PrecisionErrorCommon)
                                || !a.IsFinite && !b.IsFinite && a.EDecimal.Equals(b.EDecimal),
                    (a, b) => AreEqual<Real>(a.RealPart, b.RealPart) && AreEqual<Real>(a.ImaginaryPart, b.ImaginaryPart)
                    );

            // These compute rather than build: unlike the operators on Entity, which assemble a
            // Sumf or a Mulf to be evaluated later, these are arithmetic on numbers and hand back
            // the number that results.

            /// <summary>Their sum, computed.</summary>
            public static Number operator +(Number a, Number b) => OpSum(a, b);

            /// <summary>Their difference, computed.</summary>
            public static Number operator -(Number a, Number b) => OpSub(a, b);

            /// <summary>Their product, computed.</summary>
            public static Number operator *(Number a, Number b) => OpMul(a, b);

            /// <summary>Their quotient, computed.</summary>
            public static Number operator /(Number a, Number b) => OpDiv(a, b);

            /// <summary>The operand itself; unary plus changes nothing.</summary>
            public static Number operator +(Number a) => a;

            /// <summary>Its negation, computed.</summary>
            public static Number operator -(Number a) => OpMul(-1, a);

            /// <summary>
            /// Gets all n-th roots of a number,
            /// that is, all numbers whose n-th power equals 1
            /// </summary>
            public static IEnumerable<Entity> GetAllRootsOf1(EInteger rootPower)
            {
                for (int i = 0; i < rootPower; i++)
                {
                    var angle = Rational.Create(i * 2, rootPower) * MathS.pi;
                    yield return (MathS.Cos(angle) + MathS.i * MathS.Sin(angle)).InnerSimplified;
                }
            }

            /// <summary>
            /// Finds all complex roots of a number
            /// e. g. sqrt(1) = { -1, 1 }
            /// root(1, 4) = { -i, i, -1, 1 }
            /// </summary>
            public static HashSet<Complex> GetAllRoots(Complex value, EInteger rootPower)
            {
                // Avoid infinite recursion from Abs to GetAllRoots again
                using var _ = MathS.Settings.FloatToRationalIterCount.Set(0);
                var res = new HashSet<Complex>();
                EDecimal phi = (Ln(value / value.Abs()) / MathS.i).RealPart.EDecimal;
                if (phi.IsNaN()) // (value / value.Abs()) is NaN when value is zero
                    phi = EDecimal.Zero;

                EDecimal newMod = Pow(value.Abs(), CtxDivide(EDecimal.One, rootPower)).RealPart.EDecimal;

                var i = Complex.ImaginaryOne;
                for (int n = 0; n < rootPower; n++)
                {
                    EDecimal newPow = CtxAdd(CtxDivide(phi, rootPower),
                        CtxDivide(CtxMultiply(CtxMultiply(2, MathS.DecimalConst.pi), n), rootPower));
                    var root = newMod * Exp(i * newPow);
                    res.Add(root);
                }
                return res;
            }

            /// <summary>
            /// Whether <see cref="FindGoodRoot"/> can return anything at all for this base and
            /// root power, asked before it runs so that its cost is only paid where its answer
            /// can be used.
            /// </summary>
            /// <remarks>
            /// <para>
            /// It answers with a <see cref="Real"/>, and a negative real has no real root of even
            /// order — for real <c>x</c> and even <c>q</c>, <c>x ^ q</c> is never negative — so
            /// every even root of it lies off the real line and the search is a certain miss.
            /// </para>
            /// <para>
            /// It is not a cheap miss. The search computes all <c>q</c> roots to the full decimal
            /// precision and attempts a rational conversion of each part of each, which is 7.5 ms
            /// of the 12.6 ms that evaluating <c>sqrt(-pi)</c> used to cost. What makes that reach
            /// a caller is that <c>Simplify</c> asks for the value of the same expression dozens of
            /// times over — each rewrite hands it a freshly built node, whose cached value is not
            /// the previous node's — so <c>Simplify("sqrt(-pi)")</c> spent 556 ms returning its
            /// argument unchanged, and spends 232 ms now.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/975">#975</a>
            /// </para>
            /// <para>
            /// A non-real base is left to the search rather than excluded here. A complex number
            /// has no real root either, but a real one whose imaginary part is a rounding artefact
            /// arrives as <see cref="Complex"/>, and this is a performance guard rather than the
            /// place to decide what such a number is.
            /// </para>
            /// </remarks>
            private static bool CanHaveARealRoot(Complex @base, EInteger rootPower) =>
                @base is not Real { IsNegative: true } || !rootPower.IsEven;

            internal static Real? FindGoodRoot(Complex @base, Integer power)
            {
                // Searched at the size of one, by an exact power of ten: the candidates are rounded with
                // the zero range at 1e-6, so a root below it was the integer 0, and 0 ^ q was within 1e-6
                // of the base, so sqrt(1/10^20) was 0. Scaled, it is sqrt(1) / 10^10.
                // https://github.com/asc-community/AngouriMath/issues/2065
                var size = EDecimal.Max(@base.RealPart.EDecimal.Abs(), @base.ImaginaryPart.EDecimal.Abs());
                if (!size.IsZero && size.IsFinite)
                {
                    var shift = size.Exponent.Add(size.Precision()).Subtract(1).Divide(power.EInteger);
                    if (!shift.IsZero)
                    {
                        Real factor = Integer.Create(EInteger.FromInt32(10).Pow(shift.Abs()));
                        Complex power10 = Integer.Create(EInteger.FromInt32(10).Pow(shift.Abs().Multiply(power.EInteger)));
                        var scaled = shift.Sign < 0 ? @base * power10 : @base / power10;
                        return FindGoodRootNearOne(scaled, power) is { } root ? (shift.Sign < 0 ? root / factor : root * factor) : null;
                    }
                }
                return FindGoodRootNearOne(@base, power);
            }

            private static Real? FindGoodRootNearOne(Complex @base, Integer power)
            {
                Real? positive = null, real = null;
                foreach (var root in GetAllRoots(@base, power.EInteger))
                {
                    // Rounded to six digits only to look for a rational root, which is that root only if its
                    // power is the base at the working precision: within 1e-6, sqrt(10^12 + 1/2) was 10^6.
                    // A real root is the candidate as computed, never the rounded one.
                    // https://github.com/asc-community/AngouriMath/issues/2065
                    Complex rounded;
                    using (MathS.Settings.FloatToRationalIterCount.Set(15))
                    using (MathS.Settings.PrecisionErrorZeroRange.Set(1e-6m))
                        rounded = Complex.Create(root.RealPart, root.ImaginaryPart);
                    if (rounded is Rational rational && IsZero(Pow(rational, power) - @base))  // To keep user's desired precision
                        return rational;
                    // The root as GetAllRoots made it, at the working precision.
                    switch (root)
                    {
                        case Real r when r > 0:
                            positive ??= r;
                            break;
                        case Real r:
                            real ??= r;
                            break;
                    }
                }
                return positive ?? real;
            }

            private static EDecimal Hypot(EDecimal a, EDecimal b, EContext context)
            {
                // Using
                //   sqrt(a^2 + b^2) = |a| * sqrt(1 + (b/a)^2)
                // we can factor out the larger component to dodge overflow even when a * a would overflow.

                a = a.Abs();
                b = b.Abs();

                var (small, large) = a.LessThan(b) ? (a, b) : (b, a);

                if (small.IsZero)
                    return large;
                else if (large.IsPositiveInfinity() && !small.IsNaN())
                    // The NaN test is necessary so we don't return +inf when small=NaN and large=+inf.
                    // NaN in any other place returns NaN without any special handling.
                    return EDecimal.PositiveInfinity;
                else
                {
                    var ratio = small.Divide(large, context);
                    return ratio.MultiplyAndAdd(ratio, EDecimal.One, context).SqrtByIntegerRoot(context).Multiply(large, context);
                }
            }

            /// <summary>Calculates the exact value of square root of num</summary>
            public static Complex Sqrt(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;

                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,7dc9c2ee4f99814a
                if (num is Real { EDecimal: var real })
                    if (real.IsNegative)
                        return Complex.CreateAtScales(EDecimal.Zero, real.Negate().SqrtByIntegerRoot(context), EDecimal.Zero, EDecimal.Zero);
                    else return Real.CreateAtScale(real.SqrtByIntegerRoot(context), EDecimal.Zero);
                else
                {

                    // One way to compute Sqrt(z) is just to call Pow(z, 0.5), which coverts to polar coordinates
                    // (sqrt + atan), halves the phase, and reconverts to cartesian coordinates (cos + sin).
                    // Not only is this more expensive than necessary, it also fails to preserve certain expected
                    // symmetries, such as that the square root of a pure negative is a pure imaginary, and that the
                    // square root of a pure imaginary has exactly equal real and imaginary parts. This all goes
                    // back to the fact that Math.PI is not stored with infinite precision, so taking half of Math.PI
                    // does not land us on an argument with cosine exactly equal to zero.

                    // To find a fast and symmetry-respecting formula for complex square root,
                    // note x + i y = \sqrt{a + i b} implies x^2 + 2 i x y - y^2 = a + i b,
                    // so x^2 - y^2 = a and 2 x y = b. Cross-substitute and use the quadratic formula to obtain
                    //   x = \sqrt{\frac{\sqrt{a^2 + b^2} + a}{2}}  y = \pm \sqrt{\frac{\sqrt{a^2 + b^2} - a}{2}}
                    // There is just one complication: depending on the sign on a, either x or y suffers from
                    // cancelation when |b| << |a|. We can get aroud this by noting that our formulas imply
                    // x^2 y^2 = b^2 / 4, so |x| |y| = |b| / 2. So after computing the one that doesn't suffer
                    // from cancelation, we can compute the other with just a division. This is basically just
                    // the right way to evaluate the quadratic formula without cancelation.

                    // All this reduces our total cost to two sqrts and a few flops, and it respects the desired
                    // symmetries. Much better than atan + cos + sin!

                    // The signs are a matter of choice of branch cut, which is traditionally taken so x > 0 and sign(y) = sign(b).

                    // [Skipped overflow handling from original .NET Core source - we don't need it]

                    // This is the core of the algorithm. Everything else is special case handling.

                    var (re, im) = (num.RealPart.EDecimal, num.ImaginaryPart.EDecimal);

                    EDecimal x, y;
                    if (!re.IsNegative)
                    {
                        x = Hypot(re, im, context).Add(re, context).Divide(2, context).SqrtByIntegerRoot(context);
                        y = im.Divide(x.Multiply(2, context), context);
                    }
                    else
                    {
                        y = Hypot(re, im, context).Subtract(re, context).Divide(2, context).SqrtByIntegerRoot(context);
                        // Strictly negative: a negative zero is on the axis, where the root is
                        // +i times the root of the modulus, as for any negative real.
                        if (im.Sign < 0) y = -y;
                        x = im.Divide(y.Multiply(2, context), context);
                    }
                    // Each part is a root of a sum of like signs or a quotient by one, and cancels nothing.
                    return Complex.CreateAtScales(x, y, EDecimal.Zero, EDecimal.Zero);
                }
            }

            /// <summary>exp(x) = e^x</summary>
            public static Complex Exp(Complex x)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                var expReal = x.RealPart.EDecimal.Exponential(context);
                var imaginary = x.ImaginaryPart.EDecimal;
                if (imaginary.IsZero)
                    return Real.CreateAtScale(expReal, EDecimal.Zero);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,7dc9c2ee4f99814a
                var cosImaginary = expReal.Multiply(imaginary.Cos(context), context);
                var sinImaginary = expReal.Multiply(imaginary.Sin(context), context);
                // The residual of a cosine or a sine is the modulus's: e^(i pi) leaves 1e-100 i of it.
                return Complex.CreateAtScales(cosImaginary, sinImaginary, expReal, expReal);
            }

            /// <summary>e.g. Pow(2, 5) = 32</summary>
            /// <param name="base">The base of the exponential, base^power</param>
            /// <param name="power">The power of the exponential, base^power</param>
            public static Complex Pow(Complex @base, Complex power)
            {
                // The half is computed once and squared, not computed twice: as it was, a
                // cube was three multiplications and a tenth power was nine, each with the
                // rational search on its result, and the recursion was linear in the exponent
                // where it should be logarithmic.
                static Complex BinaryIntPow(Complex num, EInteger val)
                {
                    if (val.IsZero)
                        return 1;
                    if (val.Equals(1))
                        return num;
                    if (val.Equals(-1))
                        return 1 / num;
                    var divRem = val.DivRem(2); // divRem[0] == val / 2, divRem[1] == val % 2
                    var half = BinaryIntPow(num, divRem[0]);
                    var squared = half * half;
                    return divRem[1].IsZero ? squared : squared * BinaryIntPow(num, divRem[1]);
                }
                // A complex whose imaginary part is exactly zero is the real number it is, and
                // takes the real paths below: with the downcasting off, Complex.Create keeps
                // `-1.54 - 0i` a Complex, and through the polar form the sign of that zero
                // put the number a hair below the negative axis -- `(-1.54)^(-1/2)` came back
                // `+0.80 i` there and `-0.80 i` with the downcasting on. The phase reads a
                // zero as a zero now, and a real base gets the exact half-power arithmetic
                // rather than a cosine of pi/2 at a hundred digits.
                // https://github.com/asc-community/AngouriMath/issues/1378
                if (@base is not Real && @base.ImaginaryPart.EDecimal.IsZero)
                    @base = Real.Create(@base.RealPart.EDecimal);
                if (power is not Real && power.ImaginaryPart.EDecimal.IsZero)
                    power = Real.Create(power.RealPart.EDecimal);
                // e to a real power is the exponential of the power: raising the constant's
                // hundred digits loses to the power what it multiplies their error by --
                // e^700 came back to ninety-seven digits -- and the exponential is a series
                // where the power was a logarithm and a series.
                if (@base is Real { EDecimal: var maybeE } && power is Real { EDecimal: var realExponent }
                    && maybeE.Equals(InternalAMExtensions.ConstantCache.Lookup(MathS.Settings.DecimalPrecisionContext).E))
                    return Real.CreateAtScale(realExponent.Exponential(MathS.Settings.DecimalPrecisionContext), EDecimal.Zero);
                // TODO: make it more detailed (e. g. +oo ^ +oo = +oo)
                // A whole power, whether written as an integer or, for a base off the
                // positive axis, as a decimal with nothing after the point -- which is how `2`
                // arrives with the downcasting off: the polar form below took a pure
                // imaginary squared a hair off the negative axis, where the exact
                // multiplication lands on it. A positive real keeps its exponential of the
                // logarithm, whose hundred digits every power of it shares, so that the terms
                // of a sum with 3.66^120 in each still cancel to what they cancel to.
                // https://github.com/asc-community/AngouriMath/issues/1378
                var pow = power switch
                {
                    Integer whole => whole.EInteger,
                    Real { EDecimal: { IsFinite: true } decimalPower } when @base is not Real { EDecimal.IsNegative: false }
                        && decimalPower.IsInteger() && decimalPower.Abs().CompareTo(EDecimal.FromInt32(1 << 20)) <= 0 => decimalPower.ToEInteger(),
                    _ => null,
                };
                // An infinite real base to a whole power: (+oo)^n is +oo, (-oo)^n is +oo or -oo by
                // the parity of n, and either to a negative power is 0 -- the extended reals'
                // arithmetic, which the product of infinities already follows. It fell through
                // to the polar form and came back NaN, so the image of (-oo; +oo) under a cube
                // had no ends. https://github.com/asc-community/AngouriMath/issues/1409
                if (@base is Real { EDecimal: var infinite } && infinite.IsInfinity() && pow is { } wholeOfInfinity && !wholeOfInfinity.IsZero)
                {
                    if (wholeOfInfinity.Sign < 0)
                        return Integer.Zero;
                    return infinite.IsNegative && !wholeOfInfinity.IsEven ? Real.NegativeInfinity : Real.PositiveInfinity;
                }
                if (@base.IsFinite && pow is { })
                {
                    // A real base that is not exact -- a decimal, which is what a numerical
                    // evaluation multiplies -- is raised in one call on the decimal, with one
                    // rational search on the result instead of one per multiplication; an
                    // exact base keeps the exact arithmetic. Zero and the huge exponents keep
                    // the path that already answers them.
                    if (@base is Real { IsExact: false, EDecimal: var decimalBase } && !decimalBase.IsZero
                        && pow.Abs().CompareTo(EInteger.FromInt32(1 << 20)) <= 0)
                    {
                        var wholeContext = MathS.Settings.DecimalPrecisionContext;
                        var raised = decimalBase.Pow(pow.Abs().ToInt32Checked(), wholeContext);
                        return Real.CreateAtScale(pow.Sign < 0 ? EDecimal.One.Divide(raised, wholeContext) : raised, EDecimal.Zero);
                    }
                    return BinaryIntPow(@base, pow);
                }

                // A half power of a nonnegative decimal is its square root, not its exponential
                // of half its logarithm -- Newton's iteration against two series, seventy times
                // the time at a hundred digits -- and not a search for an exact root either,
                // which an inexact decimal has no use for.
                if (@base is Real { IsExact: false, EDecimal: { IsNegative: false } rootBase } && power is Rational { ERational: var halfPower }
                    && halfPower.Denominator.Equals(EInteger.FromInt32(2)) && halfPower.Numerator.Abs().CompareTo(EInteger.FromInt32(1 << 20)) <= 0)
                {
                    var halfContext = MathS.Settings.DecimalPrecisionContext;
                    var root = rootBase.SqrtByIntegerRoot(halfContext);
                    var n = halfPower.Numerator.Abs().ToInt32Checked();
                    var raised = n == 1 ? root : root.Pow(n, halfContext);
                    return Real.CreateAtScale(halfPower.Numerator.Sign < 0 ? EDecimal.One.Divide(raised, halfContext) : raised, EDecimal.Zero);
                }
                // A negative real to a real power that is a whole number of halves -- a half
                // power above all -- is the modulus to that power times a power of i, exactly:
                // through the polar form below, sqrt(-0.473) came back with a real part of
                // 6e-102 from cos(pi/2), and the square of that root then lay a hair below the
                // negative axis, where ln reads -i pi for the +i pi the number has. The power
                // is a decimal as often as a rational, since a numerical evaluation with the
                // downcasting off writes 1/2 as 0.5.
                // https://github.com/asc-community/AngouriMath/issues/1378
                if (@base is Real { EDecimal: { IsNegative: true, IsFinite: true } negativeBase } && power is Real { EDecimal: { IsFinite: true } halves }
                    && halves.Multiply(2) is { } twice && twice.IsInteger() && twice.Abs().CompareTo(EDecimal.FromInt32(1 << 21)) <= 0)
                {
                    var quarterTurns = twice.ToEInteger().Remainder(4);
                    if (quarterTurns.Sign < 0)
                        quarterTurns = quarterTurns.Add(4);
                    // The modulus to the power: an exact base whose numerator and denominator
                    // are squares has an exact root, and any other takes the decimal square
                    // root and the whole power of it -- not the search over all the roots of
                    // unity that an exact base's power otherwise runs, which cost `sqrt(-3)`
                    // four times a solve of `x^2 + x + 1` a sixth of its allocation.
                    var halfContext = MathS.Settings.DecimalPrecisionContext;
                    var numeratorOfPower = twice.ToEInteger();
                    Complex modulus;
                    if (@base is Rational { ERational: var exactBase } && exactBase.Numerator.Abs().Sqrt() is { } rootAbove && exactBase.Denominator.Sqrt() is { } rootBelow
                        && rootAbove.Multiply(rootAbove).Equals(exactBase.Numerator.Abs()) && rootBelow.Multiply(rootBelow).Equals(exactBase.Denominator))
                        modulus = Pow(Rational.Create(rootAbove, rootBelow), Integer.Create(numeratorOfPower));
                    else if (numeratorOfPower.IsEven)
                        modulus = Pow(Real.CreateAtScale(negativeBase.Negate(), EDecimal.Zero), Integer.Create(numeratorOfPower.Divide(2)));
                    else
                    {
                        var root = negativeBase.Negate().SqrtByIntegerRoot(halfContext);
                        var n = numeratorOfPower.Abs().ToInt32Checked();
                        var raised = n == 1 ? root : root.Pow(n, halfContext);
                        modulus = Real.CreateAtScale(numeratorOfPower.Sign < 0 ? EDecimal.One.Divide(raised, halfContext) : raised, EDecimal.Zero);
                    }
                    return quarterTurns.ToInt32Checked() switch
                    {
                        0 => modulus,
                        // A turn of the modulus, which is not a residual of anything.
                        1 => Complex.CreateAtScales(EDecimal.Zero, modulus.RealPart.EDecimal, EDecimal.Zero, EDecimal.Zero),
                        2 => Real.CreateAtScale(modulus.RealPart.EDecimal.Negate(), EDecimal.Zero),
                        _ => Complex.CreateAtScales(EDecimal.Zero, modulus.RealPart.EDecimal.Negate(), EDecimal.Zero, EDecimal.Zero),
                    };
                }
                if (@base.IsFinite && power is Rational r && r.ERational.Denominator.Abs() < 10 // there should be a minimal threshold to avoid long searches
                    && CanHaveARealRoot(@base, r.ERational.Denominator.Abs())
                    && FindGoodRoot(@base, r.ERational.Denominator) is { } goodRoot)
                    return Pow(goodRoot, r.ERational.Numerator);

                var context = MathS.Settings.DecimalPrecisionContext;
                if (@base is Real { EDecimal: { IsNegative: false } realBase } && power is Real { EDecimal: var realPower })
                    return PowerOfReals(realBase, realPower, context);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,7dc9c2ee4f99814a
                // NOTE: System.Numerics.Complex.Pow(0, System.Numerics.Complex(-2, 1)) gives 0 + 0i despite being mathematically undefined
                // NOTE: System.Numerics.Complex.Pow(0, 0) gives 1 + 0i despite being mathematically undefined
                var baseReal = @base.RealPart.EDecimal;
                var baseImaginary = @base.ImaginaryPart.EDecimal;
                var powerReal = power.RealPart.EDecimal;
                var powerImaginary = power.ImaginaryPart.EDecimal;

                if (powerReal.IsZero && powerImaginary.IsZero)
                    return Integer.One;
                if (baseReal.IsZero && baseImaginary.IsZero)
                    return Integer.Zero;

                var rho = @base.Abs().EDecimal;
                var theta = baseImaginary.Arctan2(baseReal, context);
                var newRho = powerReal.MultiplyAndAdd(theta, powerImaginary.Multiply(rho.NaturalLogarithm(context), context), context);
                var t = PowerOfReals(rho, powerReal, context).Multiply(powerImaginary.Multiply(-theta, context).Exponential(context), context);

                // The residual of a cosine or a sine is the modulus's.
                var modulusScale = t.Abs();
                return Complex.CreateAtScales(t.Multiply(newRho.Cos(context), context), t.Multiply(newRho.Sin(context), context), modulusScale, modulusScale);
            }

            /// <summary>e.g. Log(2, 32) = 5</summary>
            /// <param name="base">Log's base, log(base, x) is a number y such that base^y = x</param>
            /// <param name="x">The number of which we want to get its base power</param>
            public static Complex Log(Complex @base, Complex x)
            {
                // 0 is not a base: no power of it is anything but 0, and log_0(x) has no value, which is what
                // Logf's condition says. ln(x)/ln(0) is ln(x)/(-oo), and was 0. An exact zero only: a base that
                // is small and not 0 has its logarithm. https://github.com/asc-community/AngouriMath/issues/2255
                if (@base.RealPart.EDecimal.IsZero && @base.ImaginaryPart.EDecimal.IsZero)
                    return Real.NaN;
                if (LostToExponentRange(x) || LostToExponentRange(@base))
                    return Ln(x) / Ln(@base);
                // A base of 1 is excluded from the real shortcut so that it falls through to the
                // ratio below. log_1(x) is ln(x)/ln(1) = ln(x)/0, and every division by zero in
                // this library is NaN -- but LogN answers 0 for log_1(1) and +oo for log_1(2),
                // so the shortcut and the definition disagreed.
                // https://github.com/asc-community/AngouriMath/issues/890
                if (x is Real real && real.EDecimal.CompareTo(EDecimal.Zero) > 0
                    && @base is Real realBase && realBase.EDecimal.CompareTo(EDecimal.Zero) > 0
                    && realBase.EDecimal.CompareTo(EDecimal.One) != 0)
                    return LogOfReals(real.EDecimal, realBase.EDecimal, MathS.Settings.DecimalPrecisionContext);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1
                return Ln(x) / Ln(@base);
            }

            /// <summary>
            /// <c>base^power</c> for a nonnegative real base and a real power, as
            /// <c>exp(power ln base)</c> with the logarithm and the product carrying eight
            /// guard digits. Zero, an infinity and NaN are PeterO's answers.
            /// </summary>
            private static EDecimal PowerOfReals(EDecimal @base, EDecimal power, EContext context)
            {
                if (@base.IsZero || !@base.IsFinite || !power.IsFinite)
                    return @base.Pow(power, context);
                var working = InternalAMExtensions.WithGuardDigits(context, 8);
                return power.Multiply(@base.NaturalLogarithm(working), working).Exponential(working).RoundToPrecision(context);
            }

            /// <summary>
            /// <c>log_base(x)</c> for positive reals as <c>ln x / ln base</c>, with the base
            /// <c>e</c> -- the value the constant evaluates to in this context -- recognised, so
            /// that <c>ln(x)</c>, which arrives here as a logarithm to the base <c>e</c>, is one
            /// logarithm and not two and a division.
            /// </summary>
            private static EDecimal LogOfReals(EDecimal x, EDecimal @base, EContext context)
            {
                var log = x.NaturalLogarithm(context);
                if (@base.Equals(InternalAMExtensions.ConstantCache.Lookup(context).E))
                    return log;
                return log.Divide(@base.NaturalLogarithm(context), context);
            }

            /// <summary>
            /// Whether a number is exactly known as a ratio, yet its
            /// <see cref="Real.EDecimal"/> -- which is rounded into
            /// <see cref="MathS.Settings.DecimalPrecisionContext"/> the moment the number is
            /// built -- has lost it to the context's exponent range. <c>2 ^ (-1000)</c>
            /// flushes to zero and <c>2 ^ 10000</c> saturates to +oo, so a logarithm taken
            /// off the decimal answers -oo or +oo where the true answers, -693.14... and
            /// 6931.47..., are ordinary numbers.
            /// </summary>
            private static bool LostToExponentRange(Complex x)
                => x is Rational { ERational.Sign: not 0 } ratio
                    && (!ratio.EDecimal.IsFinite || ratio.EDecimal.IsZero);

            /// <summary>ln(p / q) = ln(p) - ln(q), with each half inside the exponent range.</summary>
            private static Complex LnOfRatio(ERational ratio, EContext context)
                => LnOfEInteger(ratio.Numerator, context)
                    .Subtract(LnOfEInteger(ratio.Denominator, context), context);

            /// <summary>
            /// ln(n) for a whole number, kept inside the context's exponent range by
            /// writing n = head * 10^shift and answering ln(head) + shift * ln(10).
            /// Without the split, <c>ln(2 ^ 10000)</c> -- a 3011-digit integer, past the
            /// context's ceiling of 10^1000 -- answers +oo instead of 6931.47...
            /// </summary>
            private static EDecimal LnOfEInteger(EInteger n, EContext context)
            {
                // Guard digits, so that dropping the tail stays well under the rounding error.
                var keep = (context.Precision.IsZero ? EInteger.FromInt32(100) : context.Precision).Add(10);
                var digits = n.GetDigitCountAsEInteger();
                if (digits.CompareTo(keep) <= 0)
                    return EDecimal.FromEInteger(n).NaturalLogarithm(context);
                var shift = digits.Subtract(keep);
                // Create(n, -shift) is n with its decimal point moved, not a division:
                // the digits are untouched, only the exponent comes back into range.
                return EDecimal.Create(n, shift.Negate()).NaturalLogarithm(context)
                    .Add(EDecimal.FromInt32(10).NaturalLogarithm(context)
                        .Multiply(EDecimal.FromEInteger(shift), context), context);
            }

            /// <summary>ln(x) = log(e, x)</summary>
            public static Complex Ln(Complex x)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (LostToExponentRange(x) && x is Rational { ERational: { Sign: > 0 } ratio })
                    return LnOfRatio(ratio, context);
                // A function's residual is its argument's: sin(pi) leaves about 1e-100 pi, where sin(1e-60)
                // cancels nothing and is 1e-60. https://github.com/asc-community/AngouriMath/issues/2074
                if (x is Real { EDecimal: { IsNegative: false } real })
                    return Real.CreateAtScale(real.NaturalLogarithm(context), real);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1
                return Complex.Create(x.Abs().EDecimal.NaturalLogarithm(context), x.ImaginaryPart.EDecimal.Arctan2(x.RealPart.EDecimal, context));
            }

            /// <summary>Calculates the exact value of sine of num</summary>
            public static Complex Sin(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real })
                    return Real.CreateAtScale(real.Sin(context), real);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1
                var (re, im) = (num.RealPart.EDecimal, num.ImaginaryPart.EDecimal);
                // We need both sinh and cosh of imaginary part.
                // To avoid multiple calls to Exp with the same value,
                // we compute them both here from a single call to Exp.
                var p = im.Exponential(context);
                var q = EDecimal.One.Divide(p, context);
                var sinh = p.Subtract(q, context).Divide(2, context);
                var cosh = p.Add(q, context).Divide(2, context);
                return Complex.Create(
                    re.Sin(context).Multiply(cosh, context),
                    re.Cos(context).Multiply(sinh, context));
            }

            /// <summary>Calculates the exact value of secant of num</summary>
            public static Complex Secant(Complex num)
                => 1 / Cos(num);

            /// <summary>Calculates the exact value of cosecant of num</summary>
            public static Complex Cosecant(Complex num)
                => 1 / Sin(num);

            /// <summary>
            /// sec(x) = value
            /// 1 / cos(x) = value
            /// 1 / value = cos(x)
            /// x = arccos(1 / value)
            /// </summary>
            public static Complex Arcsecant(Complex num)
                => Arccos(1 / num);

            /// <summary>
            /// csc(x) = value
            /// 1 / sin(x) = value
            /// 1 / value = sin(x)
            /// x = arcsin(1 / value)
            /// </summary>
            public static Complex Arccosecant(Complex num)
                => Arcsin(1 / num);

            /// <summary>
            /// Defines the Signum function on complex numbers
            /// Which is z / |z|
            /// </summary>
            /// <param name="num">Number to find Signum of</param>
            /// <returns>
            /// A complex signum value for a non-zero argument,
            /// 0 otherwise
            /// </returns>
            public static Complex Signum(Complex num)
                => num.IsZero ? Integer.Zero : num / num.Abs();

            /// <summary>
            /// Complex absolute value
            /// </summary>
            public static Real Abs(Complex num)
                => num.Abs();

            /// <summary>Calculates the exact value of cosine of num</summary>
            public static Complex Cos(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real })
                    return Real.CreateAtScale(real.Cos(context), real);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1
                var (re, im) = (num.RealPart.EDecimal, num.ImaginaryPart.EDecimal);
                var p = im.Exponential(context);
                var q = EDecimal.One.Divide(p, context);
                var sinh = p.Subtract(q, context).Divide(2, context);
                var cosh = p.Add(q, context).Divide(2, context);
                return Complex.Create(
                    re.Cos(context).Multiply(cosh, context),
                    -re.Sin(context).Multiply(sinh, context));
            }

            /// <summary>Calculates the exact value of tangent of num</summary>
            public static Complex Tan(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real })
                    return Real.CreateAtScale(real.Tan(context), real);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1

                // tan z = sin z / cos z, but to avoid unnecessary repeated trig computations, use
                //   tan z = (sin(2x) + i sinh(2y)) / (cos(2x) + cosh(2y))
                // (see Abramowitz & Stegun 4.3.57 or derive by hand), and compute trig functions here.

                // This approach does not work for |y| > ~355, because sinh(2y) and cosh(2y) overflow,
                // even though their ratio does not. In that case, divide through by cosh to get:
                //   tan z = (sin(2x) / cosh(2y) + i \tanh(2y)) / (1 + cos(2x) / cosh(2y))
                // which correctly computes the (tiny) real part and the (normal-sized) imaginary part.

                var x2 = num.RealPart.EDecimal.Multiply(2, context);
                var y2 = num.ImaginaryPart.EDecimal.Multiply(2, context);
                var p = y2.Exponential(context);
                var q = EDecimal.One.Divide(p, context);
                var cosh = p.Add(q, context).Divide(2, context);
                if (num.ImaginaryPart.EDecimal.Abs().LessThanOrEquals(4))
                {
                    var sinh = p.Subtract(q, context).Divide(2, context);
                    var D = x2.Cos(context).Add(cosh, context);
                    return Complex.Create(x2.Sin(context).Divide(D, context), sinh.Divide(D, context));
                }
                else
                {
                    var D = x2.Cos(context).Divide(cosh, context).Increment();
                    return Complex.Create(
                        x2.Sin(context).Divide(cosh, context).Divide(D, context),
                        y2.Tanh(context).Divide(D, context));
                }
            }

            /// <summary>Calculates the exact value of cotangent of num</summary>
            public static Complex Cotan(Complex num)
            {
                var cotan = Integer.One / Tan(num);
                if (cotan.RealPart.EDecimal.Abs().LessThan(MathS.Settings.PrecisionErrorZeroRange)
                    && cotan.ImaginaryPart.EDecimal.Abs().LessThan(MathS.Settings.PrecisionErrorZeroRange))
                    return Integer.Zero;
                else return cotan;
            }

            // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1

            // This method for the inverse complex sine (and cosine) is described in Hull, Fairgrieve,
            // and Tang, "Implementing the Complex Arcsine and Arccosine Functions Using Exception Handling",
            // ACM Transactions on Mathematical Software (1997)
            // (https://www.researchgate.net/profile/Ping_Tang3/publication/220493330_Implementing_the_Complex_Arcsine_and_Arccosine_Functions_Using_Exception_Handling/links/55b244b208ae9289a085245d.pdf)

            // First, the basics: start with sin(w) = (e^{iw} - e^{-iw}) / (2i) = z. Here z is the input
            // and w is the output. To solve for w, define t = e^{i w} and multiply through by t to
            // get the quadratic equation t^2 - 2 i z t - 1 = 0. The solution is t = i z + sqrt(1 - z^2), so
            //   w = arcsin(z) = - i log( i z + sqrt(1 - z^2) )
            // Decompose z = x + i y, multiply out i z + sqrt(1 - z^2), use log(s) = |s| + i arg(s), and do a
            // bunch of algebra to get the components of w = arcsin(z) = u + i v
            //   u = arcsin(beta)  v = sign(y) log(alpha + sqrt(alpha^2 - 1))
            // where
            //   alpha = (rho + sigma) / 2      beta = (rho - sigma) / 2
            //   rho = sqrt((x + 1)^2 + y^2)    sigma = sqrt((x - 1)^2 + y^2)
            // These formulas appear in DLMF section 4.23. (http://dlmf.nist.gov/4.23), along with the analogous
            //   arccos(w) = arccos(beta) - i sign(y) log(alpha + sqrt(alpha^2 - 1))
            // So alpha and beta together give us arcsin(w) and arccos(w).

            static (EDecimal beta, EDecimal v) ArcSinCosInner(Complex num, EContext context)
            {
                var (x, y) = (num.RealPart.EDecimal, num.ImaginaryPart.EDecimal);
                var xp1 = x.Increment();
                var xm1 = x.Decrement();
                var rho = xp1.MultiplyAndAdd(xp1, y.Multiply(y, context), context).SqrtByIntegerRoot(context);
                var sigma = xm1.MultiplyAndAdd(xm1, y.Multiply(y, context), context).SqrtByIntegerRoot(context);
                var alpha = rho.Add(sigma, context).Divide(2, context);
                // beta is (|w + 1| - |w - 1|) / 2, in [-1, 1] by the triangle inequality and exactly 1 for a real w past
                // 1; the two roots are rounded, and a beta of 1 + ε made arcsin(beta) NaN, as arcsin(6 + pi) was.
                // https://github.com/asc-community/AngouriMath/issues/2244
                var beta = rho.Subtract(sigma, context).Divide(2, context);
                if (beta.GreaterThan(EDecimal.One))
                    beta = EDecimal.One;
                else if (beta.LessThan(-EDecimal.One))
                    beta = -EDecimal.One;
                return (beta,
                    alpha.MultiplyAndSubtract(alpha, EDecimal.One, context).SqrtByIntegerRoot(context).Add(alpha, context).NaturalLogarithm(context).Multiply((y.IsNegative || y.IsZero) ? -1 : 1, context));
            }

            /// <summary>Calculates the exact value of arcsine of num</summary>
            public static Complex Arcsin(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real } && !(real.GreaterThan(EDecimal.One) || real.LessThan(-EDecimal.One)))
                    return Real.CreateAtScale(real.Arcsin(context), real);
                var (beta, v) = ArcSinCosInner(num, context);
                return Complex.Create(beta.Arcsin(context), v);
            }

            /// <summary>Calculates the exact value of arccosine of num</summary>
            public static Complex Arccos(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real } && !(real.GreaterThan(EDecimal.One) || real.LessThan(-EDecimal.One)))
                    return Real.CreateAtScale(real.Acos(context), real);
                var (beta, v) = ArcSinCosInner(num, context);
                return Complex.Create(beta.Acos(context), -v);
            }

            /// <summary>Calculates the exact value of arctangent of num</summary>
            public static Complex Arctan(Complex num)
            {
                var context = MathS.Settings.DecimalPrecisionContext;
                if (num is Real { EDecimal: var real })
                    return Real.CreateAtScale(real.Arctan(context), real);
                // From https://source.dot.net/#System.Runtime.Numerics/System/Numerics/Complex.cs,cf15f2e5cc49cef1
                var one = Integer.One;
                var two = Integer.Create(2);
                var i = Complex.ImaginaryOne;
                return i / two * (Ln(one - i * num) - Ln(one + i * num));
            }

            /// <summary>Calculates the exact value of arccotangent of num</summary>
            public static Complex Arccotan(Complex num) => num == 0 ? MathS.DecimalConst.pi / 2 : Arctan(1 / num);

            // From https://github.com/eobermuhlner/big-math/blob/ba75e9a80f040224cfeef3c2ac06390179712443/ch.obermuhlner.math.big/src/main/java/ch/obermuhlner/math/big/BigComplexMath.java
            /// 
            /// <summary>
            /// Calculates the factorial of the specified <see cref="Complex"/>.
            /// 
            /// <para>This implementation uses
            /// <a href="https://en.wikipedia.org/wiki/Spouge%27s_approximation">Spouge's approximation</a>
            /// to calculate the factorial for non-integer values.</para>
            /// 
            /// <para>This involves calculating a series of constants that depend on the desired precision.
            /// Since this constant calculation is quite expensive (especially for higher precisions),
            /// the constants for a specific precision will be cached
            /// and subsequent calls to this method with the same precision will be much faster.</para>
            /// 
            /// <para>It is therefore recommended to do one call to this method with the standard precision of your application during the startup phase
            /// and to avoid calling it with many different precisions.</para>
            /// 
            /// <para>See: <a href="https://en.wikipedia.org/wiki/Factorial#Extension_of_factorial_to_non-integer_values_of_argument">Wikipedia: Factorial - Extension of factorial to non-integer values of argument</a></para>
            /// </summary>
            /// <param name="x">The <see cref="Complex"/></param>
            /// <returns>The factorial <see cref="Complex"/></returns>
            /// <seealso cref="InternalAMExtensions.Factorial(EDecimal, EContext)"/>
            /// <seealso cref="Gamma(Complex)"/>
            /// 
            public static Complex Factorial(Complex x)
            {
                var mathContext = MathS.Settings.DecimalPrecisionContext.Value;
                switch (x)
                {
                    case Integer { EInteger: var value } when value.Sign >= 0:
                        return value.Factorial();
                    case Real { EDecimal: var value }:
                        return value.Factorial(mathContext);
                }

                if (!mathContext.Precision.CanFitInInt32())
                    throw new CannotEvalException($"The precision of the {nameof(mathContext)} is outside the int32 range");
                // With no imaginary part it is a real number, and its poles are the real line's.
                if (x.ImaginaryPart.EDecimal.IsZero)
                    return x.RealPart.EDecimal.Factorial(mathContext);

                var precision = mathContext.Precision.ToInt32Checked();
                var working = InternalAMExtensions.FactorialWorkingContext(precision);
                using var _ = MathS.Settings.DowncastingEnabled.Set(false);
                // Twice the digits, and no bound on the exponent, which the caller's context put on
                // every intermediate: up the imaginary axis the value falls as e^(-pi |y|/2), and
                // near that context's floor (280i)! lost its imaginary part and (316i)! came out 1e-190,
                // where it is 8e-215. Left of zero the reflection formula, as on the real line.
                // https://github.com/asc-community/AngouriMath/issues/1614
                using var __ = MathS.Settings.DecimalPrecisionContext.Set(working);
                var result = x.RealPart.EDecimal.IsNegative
                    ? Real.Create(InternalAMExtensions.ConstantCache.Lookup(working).Pi) * x
                        / (Sin(Real.Create(InternalAMExtensions.ConstantCache.Lookup(working).Pi) * x) * SpougeFactorial(-x, precision))
                    : SpougeFactorial(x, precision);

                return Complex.Create(result.RealPart.EDecimal.RoundToPrecision(mathContext),
                                        result.ImaginaryPart.EDecimal.RoundToPrecision(mathContext));
            }

            /// <summary>
            /// <c>x!</c> by Spouge's approximation, for <c>Re x &gt;= 0</c>, in the working context
            /// in force, before any rounding back.
            /// https://en.wikipedia.org/wiki/Spouge%27s_approximation
            /// </summary>
            private static Complex SpougeFactorial(Complex x, int precision)
            {
                var a = precision * 13 / 10;
                var constants = InternalAMExtensions.GetSpougeFactorialConstants(a);
                var factor = Complex.Create(constants[0], 0);
                for (int k = 1; k < a; k++)
                    factor += constants[k] / (x + k);
                return Pow(x + a, x + 0.5m) * Exp(-x - a) * factor;
            }

            /// <summary>
            /// Calculates the gamma function of the specified <see cref="Complex"/>.
            /// 
            /// <para>This implementation uses {@link #factorial(ComplexNumber, MathContext)} internally,
            /// therefore the performance implications described there apply also for this method.</para>
            /// 
            /// <para>See: <a href="https://en.wikipedia.org/wiki/Gamma_function">Wikipedia: Gamma function</a></para>
            /// </summary>
            /// <param name="x">The <see cref="Complex"/></param>
            /// <returns>The gamma <see cref="Complex"/></returns>
            /// <seealso cref="InternalAMExtensions.Gamma(EDecimal, EContext)"/>
            /// <seealso cref="Factorial(Complex)"/>
            public static Complex Gamma(Complex x) => Factorial(x - Integer.One);

            /// <inheritdoc/>
            public override string ToString() => Stringize();
        }
    }
}