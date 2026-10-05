//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;

namespace AngouriMath.Functions.Algebra
{
    internal static class IntegralPatterns
    {
        /// <summary>
        /// The logarithm an antiderivative of <c>f'/f</c> is written with: <c>ln(abs(f))</c> when
        /// the codomain is the reals, <c>ln(f)</c> when it is the complex plane.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>ln(abs(f))</c> is an antiderivative of <c>f'/f</c> only on the real line. <c>abs</c>
        /// is not holomorphic, so off the real line the textbook form is not an antiderivative of
        /// anything -- differentiating it does not return the integrand. That is
        /// https://github.com/asc-community/AngouriMath/issues/946, and separating by codomain is
        /// the answer given there.
        /// </para>
        /// <para>
        /// Under <see cref="AngouriMath.Core.Domain.Real"/> this returns exactly what the table
        /// returned before, so a caller who has said they are working on the reals sees no change.
        /// The default codomain is the complex plane, so the default answer does change --
        /// recorded in <c>BREAKING-CHANGES.md</c>.
        /// </para>
        /// <para>
        /// This is for an <c>abs</c> the rule <i>introduces</i>. Where the integrand already
        /// carries one -- the rule for <c>ln(abs(ax + b))</c> below -- the <c>abs</c> in the
        /// result is the caller's own and is left alone.
        /// </para>
        /// </remarks>
        internal static Entity AntiderivativeLog(Entity arg)
            => MathS.Settings.Codomain.Value is AngouriMath.Core.Domain.Real
                ? MathS.Ln(MathS.Abs(arg))
                : MathS.Ln(arg);

        /// <summary>
        /// The argument through which each rule below reads its integrand's dependence on
        /// <paramref name="x"/>, or <see langword="null"/> for a shape none of them matches.
        /// </summary>
        /// <remarks>
        /// The conditions on the two two-argument nodes are the ones their own rules carry:
        /// a base that mentions <c>x</c> is a different integral, not this one.
        /// </remarks>
        private static Entity? RateBearingArgument(Entity expr, Entity.Variable x) => expr switch
        {
            Entity.Sinf(var arg) => arg,
            Entity.Cosf(var arg) => arg,
            Entity.Secantf(var arg) => arg,
            Entity.Cosecantf(var arg) => arg,
            Entity.Tanf(var arg) => arg,
            Entity.Cotanf(var arg) => arg,
            Entity.Absf(var arg) => arg,
            Entity.Signumf(var arg) => arg,
            Entity.Erff(var arg) => arg,
            Entity.Erfcf(var arg) => arg,
            Entity.Erfif(var arg) => arg,
            Entity.Eif(var arg) => arg,
            Entity.Lif(var arg) => arg,
            Entity.Sif(var arg) => arg,
            Entity.Cif(var arg) => arg,
            Entity.Shif(var arg) => arg,
            Entity.Chif(var arg) => arg,
            Entity.Arcsinf(var arg) => arg,
            Entity.Arccosf(var arg) => arg,
            Entity.Arctanf(var arg) => arg,
            Entity.Arccotanf(var arg) => arg,
            Entity.Logf(var @base, var arg) when !@base.ContainsNode(x) => arg,
            Entity.Powf(var @base, var power) when !@base.ContainsNode(x) => power,
            _ => null
        };

        /// <summary>
        /// <c>int F^(a x^2 + b x + c) dx</c> for a non-zero <c>a</c>: the square completed is
        /// <c>a (x + b/(2a))^2 + c - b^2/(4a)</c>, so the integral is <c>F^(c - b^2/(4a))</c> times
        /// the Gaussian of <c>A = a ln F</c> in <c>u = x + b/(2a)</c>.
        /// </summary>
        private static Entity Gaussian(Entity @base, Entity a, Entity b, Entity c, Entity.Variable x)
        {
            // Built without the terms a zero linear part would contribute: b/(2a) is 0 only
            // provided a is not zero, and that condition, stated twice over, was all a symbolic
            // F^(a x^2) got for it. The answer is for the generic case, as F^(a x)/(a ln F) is.
            if (TreeAnalyzer.IsZero(b))
                return MathS.Pow(@base, c) * UnitGaussian(a * MathS.Ln(@base), x);
            return MathS.Pow(@base, c - b * b / (4 * a)) * UnitGaussian(a * MathS.Ln(@base), x + b / (2 * a));
        }

        /// <summary>
        /// <c>int P(x) F^(a x^2 + b x + c) dx</c> for a non-zero <c>a</c> and a polynomial <c>P</c>,
        /// which may be absent: the Gaussian, or the sum of its moments at the square's centre.
        /// </summary>
        internal static Entity? APolynomialTimesTheGaussian(Entity? polynomial, Entity @base, Entity a, Entity b, Entity c, Entity.Variable x)
            => polynomial is null
                ? Gaussian(@base, a, b, c, x)
                : ShiftedMoments(Entity.Number.Integer.One, 0, polynomial, @base, a, b, c, x);

        /// <summary>
        /// <c>int P(x) F^(a x^2 + c) dx</c> for a non-zero <c>a</c>, with <c>P</c> read into
        /// <paramref name="powers"/>, each power of <c>x</c> with its coefficient: the moments
        /// about 0, where a negative even power is a moment too, and a negative odd one, which
        /// ends at the exponential integral, is declined. Asked with no linear term, whose shift
        /// <c>b/(2a)</c> would be <c>0/(2a)</c>, which simplifies to <c>0</c> provided <c>a</c> is
        /// not 0, and is not read as zero where it is substituted.
        /// </summary>
        internal static Entity? MomentsAboutZero(Dictionary<EInteger, Entity> powers, Entity @base, Entity a, Entity c, Entity.Variable x)
        {
            var A = a * MathS.Ln(@base);
            var bell = MathS.Pow(@base, a * MathS.Sqr(x));
            Entity sum = Entity.Number.Integer.Zero;
            foreach (var power in powers)
            {
                if (!power.Key.CanFitInInt32())
                    return null;
                var k = power.Key.ToInt32Checked();
                if (k < 0 && k % 2 != 0 || System.Math.Abs(k) > 32)
                    return null;
                sum += power.Value * Moment(k, A, bell, x);
            }
            return MathS.Pow(@base, c) * sum;
        }

        /// <summary>
        /// <c>int e^(A u^2) du</c> for a non-zero <c>A</c>: <c>sqrt(pi)/(2 sqrt(-A)) erf(sqrt(-A) u)</c>,
        /// or with <c>erfi</c> and the real root where <c>A</c> is decidably positive.
        /// </summary>
        /// <remarks>
        /// The sign is read from <c>Evaled</c>, as the integrator's other sign tests are; they move
        /// together to the interval evaluation of https://github.com/asc-community/AngouriMath/pull/1497.
        /// Finiteness is asked as well, because <see cref="Entity.Number.Real.IsPositive"/> holds
        /// for <c>NaN</c> and <c>+oo</c>.
        /// </remarks>
        private static Entity UnitGaussian(Entity A, Entity u)
            => A.Evaled is Entity.Number.Real { EDecimal.IsFinite: true, IsPositive: true }
                ? MathS.Sqrt(MathS.pi) / (2 * MathS.Sqrt(A)) * MathS.Erfi(MathS.Sqrt(A) * u)
                : MathS.Sqrt(MathS.pi) / (2 * MathS.Sqrt(-A)) * MathS.Erf(MathS.Sqrt(-A) * u);

        /// <summary>
        /// <c>int k x^m F^(a x^2 + c) dx</c> for a whole <c>m</c>, not zero, and a non-zero <c>a</c>: the
        /// Gaussian moments. With <c>A = a ln F</c> and <c>I_m</c> the integral of
        /// <c>x^m e^(A x^2)</c>, parts give <c>I_m = x^(m - 1) e^(A x^2)/(2A) - (m - 1)/(2A) I_(m - 2)</c>,
        /// and read the other way <c>I_m = x^(m + 1) e^(A x^2)/(m + 1) - 2A/(m + 1) I_(m + 2)</c> for a
        /// negative <c>m</c>, each two powers nearer the Gaussian <c>I_0</c>. An odd positive <c>m</c>
        /// ends at <c>I_1 = e^(A x^2)/(2A)</c>, which is elementary and answered elsewhere, and an odd
        /// negative one at <c>I_(-1) = Ei(A x^2)/2</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </summary>
        private static Entity? GaussianMoment(Entity expr, Entity.Variable x)
        {
            // Asked of every integrand that reaches the table, so a type test comes first, and
            // nothing here captures a local: a closure is allocated on entry, before any return.
            if (expr is not (Entity.Mulf or Entity.Divf))
                return null;
            var (numerator, denominator) = expr is Entity.Divf(var n, var d) ? (n, d) : (expr, (Entity)Entity.Number.Integer.One);
            Entity coefficient = Entity.Number.Integer.One;
            int? power = null;
            Entity.Powf? gaussian = null;
            Entity? polynomial = null;
            foreach (var factor in Entity.Mulf.LinearChildren(numerator))
                if (!TakeMomentFactor(factor, inverted: false, x, ref coefficient, ref power, ref gaussian, ref polynomial))
                    return null;
            foreach (var factor in Entity.Mulf.LinearChildren(denominator))
                if (!TakeMomentFactor(factor, inverted: true, x, ref coefficient, ref power, ref gaussian, ref polynomial))
                    return null;
            if (gaussian is null || !TreeAnalyzer.TryGetPolyQuadratic(gaussian.Exponent, x, out var a, out var b, out var c) || TreeAnalyzer.IsZero(a))
                return null;
            // A linear term: every whole power beside it, and a polynomial, at the square's centre.
            // Without one, a polynomial beside the Gaussian is a sum the integrator splits first.
            if (!TreeAnalyzer.IsZero(b))
                return ShiftedMoments(coefficient, power ?? 0, polynomial, gaussian.Base, a, b, c, x);
            // An odd positive power is elementary, and answered elsewhere; an odd negative one ends
            // at the exponential integral.
            if (polynomial is not null || power is not { } m || m == 0 || m > 0 && m % 2 != 0 || System.Math.Abs(m) > 32)
                return null;
            var bell = MathS.Pow(gaussian.Base, a * MathS.Sqr(x));
            return coefficient * MathS.Pow(gaussian.Base, c) * Moment(m, a * MathS.Ln(gaussian.Base), bell, x);
        }

        /// <summary>
        /// <c>int k x^m P(x) F^(a x^2 + b x + c) dx</c> with a linear term <c>b</c>, for a whole
        /// <c>m</c> and a polynomial <c>P</c>, which may be absent. At the square's centre,
        /// <c>u = x + b/(2a)</c>, the exponential is <c>F^(c - b^2/(4a))</c> times <c>F^(a u^2)</c>,
        /// and <c>x^m P(x)</c> a polynomial in <c>u</c>, each power of which is a moment of the
        /// Gaussian. An odd moment ends at <c>I_1 = e^(A u^2)/(2A)</c>, which is elementary.
        /// Rubi's 2.3, <c>f^(a + b x + c x^2) (d + e x)^m</c> and <c>f^(c (a + b x)^2) x^m</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </summary>
        private static Entity? ShiftedMoments(Entity coefficient, int power, Entity? polynomial, Entity @base, Entity a, Entity b, Entity c, Entity.Variable x)
        {
            if (power < 0 || power > 32)
                return null;
            var shift = (b / (2 * a)).InnerSimplified;
            var u = Entity.Variable.CreateUnique(@base + a + b + c + x, "u");
            Entity inX = power == 0 ? Entity.Number.Integer.One : MathS.Pow(x, power);
            if (polynomial is not null)
                inX *= polynomial;
            if (!TreeAnalyzer.TryGetPolynomial(inX.Substitute(x, u - shift), u, out var monomials)
                || monomials.Keys.Any(k => k.Sign < 0 || k.CompareTo(EInteger.FromInt32(32)) > 0))
                return null;
            var A = a * MathS.Ln(@base);
            var bell = MathS.Pow(@base, a * MathS.Sqr(u));
            Entity sum = Entity.Number.Integer.Zero;
            foreach (var monomial in monomials)
                sum += monomial.Value * Moment(monomial.Key.ToInt32Checked(), A, bell, u);
            return coefficient * MathS.Pow(@base, c - b * b / (4 * a)) * sum.Substitute(u, x + shift);
        }

        /// <summary>
        /// Reads one factor of a Gaussian moment into the constant coefficient, the power of
        /// <paramref name="x"/> or the exponential, and says whether it was one of the three.
        /// </summary>
        private static bool TakeMomentFactor(Entity factor, bool inverted, Entity.Variable x,
            ref Entity coefficient, ref int? power, ref Entity.Powf? gaussian, ref Entity? polynomial)
        {
            if (!factor.ContainsNode(x))
            {
                coefficient = inverted ? coefficient / factor : coefficient * factor;
                return true;
            }
            if (power is null && factor == x)
            {
                power = inverted ? -1 : 1;
                return true;
            }
            if (power is null && factor is Entity.Powf(var b, Entity.Number.Integer n) && b == x && n.EInteger.CanFitInInt32())
            {
                power = inverted ? -n.EInteger.ToInt32Checked() : n.EInteger.ToInt32Checked();
                return true;
            }
            // Below the bar too, as the exponential of the negated exponent: `1/(e^(b^2 x^2) x^2)` is
            // `e^(-b^2 x^2)/x^2`, and it is how by parts writes the factor beside `erf(b x)` in
            // `erf(b x)/(e^(b^2 x^2) x^2)`, which was declined for want of its integral.
            // https://github.com/asc-community/AngouriMath/issues/1501
            if (gaussian is null && factor is Entity.Powf(var @base, var exponent) exponential && !@base.ContainsNode(x))
            {
                gaussian = inverted ? new Entity.Powf(@base, -exponent) : exponential;
                return true;
            }
            // A sum, or a whole power of one, taken as it is: read as a polynomial only once a
            // Gaussian with a linear term is known to be beside it.
            if (!inverted && polynomial is null
                && factor is Entity.Sumf or Entity.Minusf or Entity.Powf(Entity.Sumf or Entity.Minusf, Entity.Number.Integer { EInteger.Sign: > 0 }))
            {
                polynomial = factor;
                return true;
            }
            return false;
        }

        /// <summary>
        /// <c>I_k</c>, the integral of <c>x^k e^(A x^2)</c>, where <paramref name="bell"/> is that
        /// exponential as the integrand writes it: an even <c>k</c> down to the Gaussian, an odd
        /// positive one down to the elementary <c>I_1</c>, and an odd negative one up to
        /// <c>I_(-1) = Ei(A x^2)/2</c>, the exponential integral under <c>t = x^2</c>.
        /// </summary>
        private static Entity Moment(int k, Entity A, Entity bell, Entity.Variable x) => k switch
        {
            0 => UnitGaussian(A, x),
            1 => bell / (2 * A),
            -1 => MathS.Ei(A * MathS.Sqr(x)) / 2,
            > 0 => MathS.Pow(x, k - 1) * bell / (2 * A) + ((1 - k) / (2 * A)).InnerSimplified * Moment(k - 2, A, bell, x),
            _ => MathS.Pow(x, k + 1) * bell / (k + 1) + (-2 * A / (k + 1)).InnerSimplified * Moment(k + 2, A, bell, x),
        };

        internal static Entity? TryStandardIntegrals(Entity expr, Entity.Variable x) => expr switch
        {
            // Every rule below divides by the linear rate of its integrand's argument, and
            // an argument that mentions x without depending on it has a rate of zero. That
            // put a literal division by zero into the answer, so `e ^ (x + -x)` integrated
            // to `e ^ (x + -x) / (0 * ln(e))`, which evaluates to NaN. Each of those
            // integrands depends on x only through that argument, so a zero rate means the
            // integrand is a constant and integrates to itself times x.
            //
            // The rate has to be decidably zero: a symbolic one, as in sin(a * x), is not,
            // and answering sin(a * x) * x there would be wrong for every non-zero a.
            //
            // Reachable from ordinary input once two powers of one base are gathered:
            // e^x * e^(-x) becomes e^(x + -x), whose rate is zero while `x + -x` is still
            // written out. https://github.com/asc-community/AngouriMath/issues/785
            _ when RateBearingArgument(expr, x) is { } constant
                   && TreeAnalyzer.TryGetPolyLinear(constant, x, out var rate, out _)
                   && rate.Evaled is Entity.Number.Complex { IsZero: true }
                => expr * x,

            Entity.Sinf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _)  =>
                    -MathS.Cos(arg) / a,

            Entity.Cosf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    MathS.Sin(arg) / a,

            // sin(u)^n * cos(u)^m for whole n and m, which covers sin(x)^2 and cos(x)^2
            // as much as sin(x)^3 or sin(x)^2 * cos(x)^2.
            _ when TryReadSineCosinePowers(expr, x, out var trigArg, out var sinePower, out var cosinePower)
                   && sinePower + cosinePower >= 2
                   && TreeAnalyzer.TryGetPolyLinear(trigArg, x, out var trigRate, out _) =>
                       IntegrateSineCosinePowers(trigArg, sinePower, cosinePower, trigRate, x),

            Entity.Secantf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    MathS.Hyperbolic.Artanh(MathS.Sin(arg)) / a,

            Entity.Cosecantf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    AntiderivativeLog(MathS.Tan(0.5 * arg)) / a,

            Entity.Tanf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    -AntiderivativeLog(MathS.Cos(arg)) / a,

            Entity.Cotanf(var arg) when
               TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    AntiderivativeLog(MathS.Sin(arg)) / a,

            Entity.Logf(var @base, var arg) when
                !@base.ContainsNode(x) && TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out var b) =>
                    ((b / a + x) * MathS.Ln(arg) - x) / MathS.Ln(@base),

            Entity.Powf(var @base, var power) when
                !@base.ContainsNode(x) && TreeAnalyzer.TryGetPolyLinear(power, x, out var a, out _) =>
                    MathS.Pow(@base, power) / (a * MathS.Ln(@base)),

            // The Gaussian: an exponential of a quadratic is an error function of the square it
            // completes. With A u^2 the exponent's square part in u = x + b/(2a), the integral
            // of e^(A u^2) is sqrt(pi)/(2 sqrt(-A)) erf(sqrt(-A) u), which differentiates back for
            // every A that is not zero, whichever root is taken. Where A is decidably positive it
            // is written with erfi and the real root instead, so that e^(x^2) is sqrt(pi)/2 erfi(x)
            // rather than an error function of i x. https://github.com/asc-community/AngouriMath/issues/1501
            Entity.Powf(var @base, var power) when
                !@base.ContainsNode(x) && TreeAnalyzer.TryGetPolyQuadratic(power, x, out var a, out var b, out var c)
                && !TreeAnalyzer.IsZero(a) =>
                    Gaussian(@base, a, b, c, x),

            // And the Gaussian beside an even power of x, to either side of it.
            _ when GaussianMoment(expr, x) is { } moment => moment,

            // By parts against 1, the way the inverse trigonometric functions are:
            // int erf(u) = u erf(u) + e^(-u^2)/sqrt(pi), and the same for the other two.
            Entity.Erff(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Erf(arg) + MathS.Pow(MathS.e, -MathS.Sqr(arg)) / MathS.Sqrt(MathS.pi)) / a,

            Entity.Erfcf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Erfc(arg) - MathS.Pow(MathS.e, -MathS.Sqr(arg)) / MathS.Sqrt(MathS.pi)) / a,

            Entity.Erfif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Erfi(arg) - MathS.Pow(MathS.e, MathS.Sqr(arg)) / MathS.Sqrt(MathS.pi)) / a,

            // And the exponential, logarithmic, trigonometric and hyperbolic integrals, by parts
            // against 1 the same way: each derivative is elementary and cancels the u the
            // integrated 1 leaves beside it. int Ei(u) = u Ei(u) - e^u, int li(u) = u li(u) -
            // Ei(2 ln u), int Si(u) = u Si(u) + cos(u), int Ci(u) = u Ci(u) - sin(u),
            // int Shi(u) = u Shi(u) - cosh(u) and int Chi(u) = u Chi(u) - sinh(u).
            // https://github.com/asc-community/AngouriMath/issues/1501
            Entity.Eif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Ei(arg) - MathS.Pow(MathS.e, arg)) / a,

            Entity.Lif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Li(arg) - MathS.Ei(2 * MathS.Ln(arg))) / a,

            Entity.Sif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Si(arg) + MathS.Cos(arg)) / a,

            Entity.Cif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Ci(arg) - MathS.Sin(arg)) / a,

            Entity.Shif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Shi(arg) - MathS.Hyperbolic.Cosh(arg)) / a,

            Entity.Chif(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Chi(arg) - MathS.Hyperbolic.Sinh(arg)) / a,

            Entity.Absf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) => // ∫ |ax + b| dx = sgn(ax + b) * (ax + b)^2 / (2a)
                    MathS.Signum(arg) * MathS.Pow(arg, 2) / (2 * a),

            Entity.Signumf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) => // ∫ sgn(ax + b) dx = |ax + b| / a
                    MathS.Abs(arg) / a,

            // ∫ ln|ax + b| dx = ((ax + b)/a) * (ln|ax + b| - 1)
            Entity.Logf(var @base, Entity.Absf(var arg)) when
                @base == MathS.e && TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg / a) * (MathS.Ln(MathS.Abs(arg)) - 1),

            // ∫ k/(bx + c)^n dx for a written power of a linear, as the power it is: expanded,
            // `(f/g + x)^2` is a quadratic whose discriminant `4f^2/g^2 - 4(1/g)^2 f^2` is zero
            // in a spelling the arm below does not read as zero, and `1/(f/g + x)^2` came back
            // as a piecewise on it.
            Entity.Divf(var numerator, Entity.Powf(var linear, Entity.Number.Integer exponent)) when
                !numerator.ContainsNode(x)
                && exponent.EInteger.CompareTo(EInteger.FromInt32(2)) >= 0
                && TreeAnalyzer.TryGetPolyLinear(linear, x, out var slope, out _)
                && slope.Evaled is Entity.Number.Complex { IsZero: false }
                    => numerator * MathS.Pow(linear, 1 - exponent) / (slope * (1 - exponent)),

            Entity.Divf(var numerator, var denominator) when
                !numerator.ContainsNode(x)
                && TreeAnalyzer.TryGetPolyQuadratic(denominator, x, out var a, out var b, out var c) // ∫ k/(ax^2 + bx + c) dx
                    => IntegrateRationalQuadratic(numerator, a, b, c, x),

            // The inverse trigonometric functions, each of which is integration by parts
            // against 1 -- a shape the by-parts solver does not look for, since there is no
            // product to split.
            Entity.Arcsinf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arcsin(arg) + MathS.Sqrt(1 - arg * arg)) / a,

            Entity.Arccosf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arccos(arg) - MathS.Sqrt(1 - arg * arg)) / a,

            Entity.Arctanf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arctan(arg) - AntiderivativeLog(1 + arg * arg) / 2) / a,

            Entity.Arccotanf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arccotan(arg) + AntiderivativeLog(1 + arg * arg) / 2) / a,

            // The other two, which were missing. They carry a `signum` where the first four do
            // not, and it is not decoration: `d/dx arcsec(u)` is `1/(|u| sqrt(u^2 - 1))`, so the
            // `u` that by parts multiplies it by leaves `sgn(u)/sqrt(u^2 - 1)` rather than
            // `1/sqrt(u^2 - 1)`. The domain is `|u| >= 1`, two intervals, and the sign is what
            // makes the answer hold on the left one as well as the right.
            // https://github.com/asc-community/AngouriMath/issues/718
            Entity.Arcsecantf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arcsec(arg)
                     - MathS.Signum(arg) * AntiderivativeLog(arg + MathS.Sqrt(arg * arg - 1))) / a,

            Entity.Arccosecantf(var arg) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    (arg * MathS.Arccosec(arg)
                     + MathS.Signum(arg) * AntiderivativeLog(arg + MathS.Sqrt(arg * arg - 1))) / a,

            // ∫ B^(px + q) * sin(mx + n) dx and its cosine twin. Integrating by parts
            // twice returns the integral it started from, so the usual machinery cycles
            // rather than terminating; solving that equation for the integral once gives
            // the closed form below, which is what goes in the table. Not where the rate
            // is the frequency times i: `e^(-i u) cos(u)` is `(1 + e^(-2 i u))/2`, and the
            // form divides by zero on it -- it answered NaN for
            // `e^(-i arctan(a x))/(c + a^2 c x^2)^(3/2)` under `x = tan(u)/a`.
            Entity.Mulf(var exponential, Entity.Sinf(var wave)) when
                IsExponentialRate(exponential, x, out var rate)
                && TreeAnalyzer.TryGetPolyLinear(wave, x, out var frequency, out _) && NotResonant(rate, frequency) =>
                    exponential * (rate * MathS.Sin(wave) - frequency * MathS.Cos(wave))
                        / (rate * rate + frequency * frequency),

            Entity.Mulf(Entity.Sinf(var wave), var exponential) when
                IsExponentialRate(exponential, x, out var rate)
                && TreeAnalyzer.TryGetPolyLinear(wave, x, out var frequency, out _) && NotResonant(rate, frequency) =>
                    exponential * (rate * MathS.Sin(wave) - frequency * MathS.Cos(wave))
                        / (rate * rate + frequency * frequency),

            Entity.Mulf(var exponential, Entity.Cosf(var wave)) when
                IsExponentialRate(exponential, x, out var rate)
                && TreeAnalyzer.TryGetPolyLinear(wave, x, out var frequency, out _) && NotResonant(rate, frequency) =>
                    exponential * (rate * MathS.Cos(wave) + frequency * MathS.Sin(wave))
                        / (rate * rate + frequency * frequency),

            Entity.Mulf(Entity.Cosf(var wave), var exponential) when
                IsExponentialRate(exponential, x, out var rate)
                && TreeAnalyzer.TryGetPolyLinear(wave, x, out var frequency, out _) && NotResonant(rate, frequency) =>
                    exponential * (rate * MathS.Cos(wave) + frequency * MathS.Sin(wave))
                        / (rate * rate + frequency * frequency),

            // ∫ sqrt(ax^2 + bx + c) dx, which is one integration by parts away from the
            // reciprocal form below and is written in terms of it. Not of a square, as below.
            Entity.Powf(var radicand, Entity.Number.Rational(Entity.Number.Integer(1), Entity.Number.Integer(2))) when
                TreeAnalyzer.TryGetPolyQuadratic(radicand, x, out var qa, out var qb, out var qc)
                && qa.Evaled is Entity.Number.Complex { IsZero: false }
                && !IndefiniteIntegralSolver.IsAPerfectSquareDiscriminant(qa, qb, qc)
                    => IntegrateRootOfQuadratic(qa, qb, qc, radicand, x),

            // ∫ k / (x^2 * sqrt(ax^2 + c)) dx, the shape a trigonometric substitution is
            // usually taught for. Differentiating sqrt(ax^2 + c)/x gives exactly
            // -c/(x^2 * sqrt(ax^2 + c)), so every sign of a and c is the one formula and
            // there is no case analysis to get wrong. Without it 1/(x^2 * sqrt(x^2 - 1))
            // had no antiderivative at all.
            _ when TryReadOverSquareTimesRoot(expr, x, out var overFactor, out var overRadicand, out var overConstant)
                => -overFactor * MathS.Sqrt(overRadicand) / (overConstant * x),

            // ∫ k / sqrt(ax^2 + bx + c) dx -- the arcsine and logarithm forms. Without
            // these, 1/sqrt(1 - x^2) had no antiderivative at all.
            // Not of a square: its root is the modulus of a linear, `sqrt(a (x + h)^2)` being
            // `sqrt(a) |x + h|`, which the rule for a root of a perfect square writes. Here the
            // arcsine divides by the root of the discriminant, zero, and the logarithm is of
            // `2a (x + h) + 2a |x + h|`, zero beyond the root: `1/sqrt(x^2 + 2x + 1)` was
            // `ln(0)` for every x below -1, and `1/sqrt(-a^2 - 2abx - b^2 x^2)` NaN everywhere.
            // https://github.com/asc-community/AngouriMath/issues/1670
            Entity.Divf(var numerator,
                        Entity.Powf(var radicand, Entity.Number.Rational(Entity.Number.Integer(1), Entity.Number.Integer(2)))) when
                !numerator.ContainsNode(x)
                && TreeAnalyzer.TryGetPolyQuadratic(radicand, x, out var ra, out var rb, out var rc)
                && !IndefiniteIntegralSolver.IsAPerfectSquareDiscriminant(ra, rb, rc)
                    => IntegrateOverRootOfQuadratic(numerator, ra, rb, rc, radicand, x),

            // ∫ (px + q)/(ax^2 + bx + c) dx. Only the constant numerator was covered, so
            // x/(x^2 + 2x + 5) and (x + 3)/(x^2 + 3x + 2) had no antiderivative at all.
            Entity.Divf(var numerator, var denominator) when
                numerator.ContainsNode(x)
                && TreeAnalyzer.TryGetPolyLinear(numerator, x, out var p, out var q)
                && TreeAnalyzer.TryGetPolyQuadratic(denominator, x, out var a, out var b, out var c)
                && !TreeAnalyzer.IsZero(a)
                    => IntegrateLinearOverQuadratic(p, q, a, b, c, denominator, x),

            // ∫ N(x)/(ax^2 + bx + c)^n dx for a whole n of two or more and a proper fraction.
            // After the single-power arms above, which already read a denominator like
            // (x + 1)^2 -- expanded, it is a quadratic, so the perfect-square arm there answers
            // it and nothing about that changes.
            Entity.Divf(var numerator, Entity.Powf(var repeated, Entity.Number.Integer repetitions)) when
                repetitions.EInteger.CanFitInInt32()
                && repetitions.EInteger.ToInt32Checked() is var n and >= 2
                && TreeAnalyzer.TryGetPolyQuadratic(repeated, x, out var qa, out var qb, out var qc)
                && (!numerator.ContainsNode(x) || !TreeAnalyzer.IsZero(qa))
                && IntegrateRationalOverPowerOfQuadratic(numerator, qa, qb, qc, repeated, n, x,
                        withoutTheFirstPower: TheFirstPowerCancels(numerator, qa, qb, qc, repeated, n, x)) is { } repeatedAnswer
                    => repeatedAnswer,

            // ∫ (px + q)/(bx + c) dx, the same rewrite one degree down: the quotient is the
            // constant p/b plus a remainder over the divisor. x/(x + 1) had no antiderivative.
            Entity.Divf(var numerator, var denominator) when
                numerator.ContainsNode(x)
                && TreeAnalyzer.TryGetPolyLinear(numerator, x, out var p, out var q)
                && TreeAnalyzer.TryGetPolyLinear(denominator, x, out var b, out var c)
                && b.Evaled is Entity.Number.Complex { IsZero: false }
                    => p * x / b + (q - p * c / b) * AntiderivativeLog(denominator) / b,

            // 1/cos(u)^2 and 1/sin(u)^2, which are written that way at least as often as
            // sec(u)^2 and csc(u)^2 and were not recognised in that form.
            Entity.Divf(var numerator, Entity.Powf(Entity.Cosf(var arg), Entity.Number.Integer(2))) when
                !numerator.ContainsNode(x) && TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    numerator * MathS.Tan(arg) / a,

            Entity.Divf(var numerator, Entity.Powf(Entity.Sinf(var arg), Entity.Number.Integer(2))) when
                !numerator.ContainsNode(x) && TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    -numerator * MathS.Cotan(arg) / a,

            Entity.Powf(Entity.Secantf(var arg), Entity.Number.Integer(2)) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    MathS.Tan(arg) / a,

            Entity.Powf(Entity.Cosecantf(var arg), Entity.Number.Integer(2)) when
                TreeAnalyzer.TryGetPolyLinear(arg, x, out var a, out _) =>
                    -MathS.Cotan(arg) / a,

            _ => null
        };


        /// <summary>
        /// Reads an expression as <c>sin(arg)^n * cos(arg)^m</c>, where either power may be
        /// zero, or reports that it is not of that shape. Both factors have to be functions
        /// of the same argument.
        /// </summary>
        private static bool TryReadSineCosinePowers(
            Entity expr, Entity.Variable x, out Entity arg, out int sinePower, out int cosinePower)
        {
            arg = 0;
            sinePower = cosinePower = 0;
            switch (expr)
            {
                case Entity.Sinf(var a):
                    arg = a; sinePower = 1; return a.ContainsNode(x);
                case Entity.Cosf(var a):
                    arg = a; cosinePower = 1; return a.ContainsNode(x);
                case Entity.Powf(Entity.Sinf(var a), Entity.Number.Integer power)
                    when power.EInteger.Sign > 0 && power.EInteger.CanFitInInt32():
                    arg = a; sinePower = power.EInteger.ToInt32Checked(); return a.ContainsNode(x);
                case Entity.Powf(Entity.Cosf(var a), Entity.Number.Integer power)
                    when power.EInteger.Sign > 0 && power.EInteger.CanFitInInt32():
                    arg = a; cosinePower = power.EInteger.ToInt32Checked(); return a.ContainsNode(x);
                case Entity.Mulf(var left, var right)
                    when TryReadSineCosinePowers(left, x, out var leftArg, out var leftSine, out var leftCosine)
                         && TryReadSineCosinePowers(right, x, out var rightArg, out var rightSine, out var rightCosine)
                         && leftArg == rightArg:
                    arg = leftArg;
                    sinePower = leftSine + rightSine;
                    cosinePower = leftCosine + rightCosine;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The antiderivative of <c>sin(u)^n * cos(u)^m</c> where <c>u</c> is linear in x
        /// with slope <paramref name="rate"/>.
        /// </summary>
        /// <remarks>
        /// With an odd power there is a substitution: peel one factor off to be the
        /// differential and write what is left in the other function, which turns the
        /// integral into a polynomial. With both powers even there is no such factor to
        /// peel, so the halved-angle identities go in instead and the result is integrated
        /// again -- the total power halves each time, so this ends.
        /// </remarks>
        private static Entity IntegrateSineCosinePowers(
            Entity arg, int sinePower, int cosinePower, Entity rate, Entity.Variable x)
        {
            if (sinePower == 0 && cosinePower == 0)
                return x;

            // cos^(2k+1) * sin^n: let s = sin(u), ds = cos(u) du
            //   = (1/rate) * sum_j C(k, j) (-1)^j s^(n + 2j + 1) / (n + 2j + 1)
            //
            // Tried before the sine substitution, so that where both powers are odd the
            // answer comes back in sin rather than cos: sin(x)cos(x) integrates to
            // sin(x)^2/2, which is the form everyone writes, rather than the equally
            // correct -cos(x)^2/2 that differs from it by a constant.
            if (cosinePower % 2 == 1)
            {
                var k = (cosinePower - 1) / 2;
                Entity sum = 0;
                for (var j = 0; j <= k; j++)
                {
                    var power = sinePower + 2 * j + 1;
                    sum += Binomial(k, j) * (j % 2 == 0 ? 1 : -1) * MathS.Sin(arg).Pow(power) / power;
                }
                return sum / rate;
            }

            // sin^(2k+1) * cos^m: let c = cos(u), dc = -sin(u) du
            if (sinePower % 2 == 1)
            {
                var k = (sinePower - 1) / 2;
                Entity sum = 0;
                for (var j = 0; j <= k; j++)
                {
                    var power = cosinePower + 2 * j + 1;
                    sum += Binomial(k, j) * (j % 2 == 0 ? 1 : -1) * MathS.Cos(arg).Pow(power) / power;
                }
                return -sum / rate;
            }

            // Both even: sin^2 = (1 - cos(2u))/2 and cos^2 = (1 + cos(2u))/2, expanded into
            // powers of cos(2u), each of which is integrated by the same rules.
            var p = sinePower / 2;
            var q = cosinePower / 2;
            var doubled = 2 * arg;
            Entity result = 0;
            // (1 - t)^p (1 + t)^q with t = cos(2u), over 2^(p+q)
            for (var i = 0; i <= p; i++)
                for (var j = 0; j <= q; j++)
                {
                    var coefficient = Binomial(p, i) * Binomial(q, j) * (i % 2 == 0 ? 1 : -1);
                    var term = IntegrateSineCosinePowers(doubled, 0, i + j, 2 * rate, x);
                    result += coefficient * term;
                }
            return result / Entity.Number.Integer.Create(EInteger.One.ShiftLeft(p + q));
        }

        private static Entity.Number.Integer Binomial(int n, int k)
        {
            var result = EInteger.One;
            for (var i = 0; i < k; i++)
                result = result * EInteger.FromInt32(n - i) / EInteger.FromInt32(i + 1);
            return Entity.Number.Integer.Create(result);
        }

        /// <summary>
        /// Whether <paramref name="expr"/>, with symbols in it, is zero once simplified:
        /// the discriminant of a square of a symbolic linear is written in two spellings of
        /// the same term, <c>b^(-2)</c> and <c>(1/b)^2</c>, which the inner simplification
        /// does not bring together and the simplifier does.
        /// </summary>
        private static bool IsZeroOnceSimplified(Entity expr)
            => expr.Vars.Any() && Functions.PartialFractions.Bare(expr.Simplify()).Evaled is Entity.Number.Complex { IsZero: true };

        /// <summary>
        /// Whether <c>rate^2 + frequency^2</c> is not zero, which the closed form for an
        /// exponential times a sine or cosine divides by: zero for a rate of <c>i</c> times
        /// the frequency, where the product is a sum of two exponentials instead.
        /// </summary>
        /// <remarks>
        /// Symbolically as well as numerically. With a symbolic frequency <c>f</c>, the rate
        /// <c>-i f</c> -- which is what <c>A + i A tan(f x)</c> leaves once written as
        /// <c>A e^(i f x)/cos(f x)</c> -- gives <c>-f^2 + f^2</c>, which
        /// <see cref="Entity.InnerSimplified"/> does not collect, so the evaluation is not a
        /// number and the guard passed: the answer then divided by a zero it could not see and
        /// was <c>NaN</c> at every point.
        /// </remarks>
        private static bool NotResonant(Entity rate, Entity frequency)
        {
            var scale = (rate * rate + frequency * frequency).InnerSimplified;
            return scale.Evaled is not Entity.Number.Complex { IsZero: true } && !IsZeroOnceSimplified(scale);
        }

        /// <summary>
        /// Whether <paramref name="expr"/> is an exponential in <paramref name="x"/>, and
        /// at what rate: <c>B^(px + q)</c> grows as <c>e^(rate * x)</c> with
        /// <c>rate = p * ln(B)</c>. The constant factor <c>B^q</c> needs no separating out,
        /// because the antiderivative is written in terms of the original expression.
        /// </summary>
        private static bool IsExponentialRate(Entity expr, Entity.Variable x, out Entity rate)
        {
            rate = 0;
            if (expr is not Entity.Powf(var @base, var exponent)
                || @base.ContainsNode(x)
                || !TreeAnalyzer.TryGetPolyLinear(exponent, x, out var perX, out _))
                return false;
            rate = perX * MathS.Ln(@base);
            return true;
        }

        /// <summary>
        /// The antiderivative of <c>sqrt(a x^2 + b x + c)</c>:
        /// <c>(2ax + b) sqrt(Q) / (4a) + ((4ac - b^2) / (8a))</c> times the integral of
        /// <c>1/sqrt(Q)</c> -- integration by parts once, leaving the reciprocal form that
        /// <see cref="IntegrateOverRootOfQuadratic"/> already knows.
        /// </summary>
        /// <remarks>
        /// Only where the leading coefficient is a number other than zero. With a = 0 this
        /// is the square root of something linear, which the ordinary power rule already
        /// integrates, and dividing by a would not be allowed anyway.
        /// </remarks>
        /// <summary>
        /// Reads <c>k / (x^2 * sqrt(ax^2 + c))</c>, giving back k, the radicand and its
        /// constant term. The radicand has to be a quadratic in x with no linear term and
        /// with neither of its two coefficients zero: a zero constant makes the formula
        /// below divide by it, and with a zero a there is no root of x left to speak of.
        /// </summary>
        private static bool TryReadOverSquareTimesRoot(
            Entity expr, Entity.Variable x,
            out Entity factor, out Entity radicand, out Entity constantTerm)
        {
            factor = radicand = constantTerm = 0;
            if (expr is not Entity.Divf(var numerator, var denominator) || numerator.ContainsNode(x))
                return false;
            Entity coefficient = numerator;
            var squares = 0;
            Entity? root = null, constant = null;
            foreach (var part in Entity.Mulf.LinearChildren(denominator))
                switch (part)
                {
                    case Entity.Powf(var square, Entity.Number.Integer(2)) when square == x:
                        squares++;
                        break;
                    case Entity.Powf(var under, Entity.Number.Rational(Entity.Number.Integer(1), Entity.Number.Integer(2)))
                        when root is null
                            && TreeAnalyzer.TryGetPolyQuadratic(under, x, out var qa, out var qb, out var qc)
                            && qa.Evaled is Entity.Number.Complex { IsZero: false }
                            && qb.Evaled is Entity.Number.Complex { IsZero: true }
                            && qc.Evaled is Entity.Number.Complex { IsZero: false }:
                        (root, constant) = (under, qc);
                        break;
                    case var other when !other.ContainsNode(x):
                        coefficient /= other;
                        break;
                    default:
                        return false;
                }
            if (squares != 1 || root is null || constant is null)
                return false;
            (factor, radicand, constantTerm) = (coefficient, root, constant);
            return true;
        }

        private static Entity IntegrateRootOfQuadratic(
            Entity a, Entity b, Entity c, Entity radicand, Entity.Variable x)
            => (2 * a * x + b) * MathS.Sqrt(radicand) / (4 * a)
               + (4 * a * c - b * b) / (8 * a) * IntegrateOverRootOfQuadratic(1, a, b, c, radicand, x);

        /// <summary>
        /// The antiderivative of <c>k / sqrt(a x^2 + b x + c)</c>, which takes one of two
        /// forms depending on the sign of the leading coefficient:
        /// <list type="bullet">
        /// <item>a &lt; 0, an arc of a circle: <c>-k/sqrt(-a) * arcsin((2ax + b) / sqrt(b^2 - 4ac))</c></item>
        /// <item>a &gt; 0, a hyperbolic arc: <c>k/sqrt(a) * ln|2ax + b + 2 sqrt(a) sqrt(a x^2 + b x + c)|</c></item>
        /// </list>
        /// Returned as a piecewise on that sign, the way the rational quadratic below is,
        /// since which one applies is not known until a and the coefficients are.
        /// </summary>
        private static Entity IntegrateOverRootOfQuadratic(
            Entity numerator, Entity a, Entity b, Entity c, Entity radicand, Entity.Variable x)
        {
            var twoAxPlusB = 2 * a * x + b;

            // a < 0: the radicand is a downward parabola, positive between its roots
            var arcsinCase =
                -numerator * MathS.Arcsin(twoAxPlusB / MathS.Sqrt(b * b - 4 * a * c)) / MathS.Sqrt(-a);

            // a > 0
            var logarithmCase =
                numerator * AntiderivativeLog(twoAxPlusB + 2 * MathS.Sqrt(a) * MathS.Sqrt(radicand)) / MathS.Sqrt(a);

            // a = 0: sqrt(bx + c), which integrates as an ordinary power, and needs b ≠ 0 for
            // the same reason the rational case below does. Where the radicand has no x term
            // the integrand is the constant k/sqrt(c) and its antiderivative is kx/sqrt(c);
            // the division by zero otherwise left here is what made k/sqrt(a x^2 + c) answer
            // NaN for every symbolic a, since only a numeric a lets this arm be dropped.
            var linearCase = TreeAnalyzer.IsZero(b)
                ? numerator * x / MathS.Sqrt(c)
                : 2 * numerator * MathS.Sqrt(b * x + c) / b;

            var arms = new List<Entity.Providedf> { new Entity.Providedf(linearCase, a.EqualTo(0)) };
            if (ArmOffTheRealLine(logarithmCase, a, b, c) is { } offTheRealLine)
                arms.Add(offTheRealLine);
            arms.Add(new Entity.Providedf(arcsinCase, a < 0));
            arms.Add(new Entity.Providedf(logarithmCase, a > 0));
            return MathS.Piecewise(arms);
        }

        /// <summary>
        /// The arm that a piecewise on a sign owes a quadratic off the real line, answered by
        /// <paramref name="form"/> wherever one of <paramref name="coefficients"/> is not real;
        /// <see langword="null"/> where none of them holds the imaginary unit, and the sign arms
        /// are left to decide.
        /// </summary>
        /// <remarks>
        /// The sign arms choose between forms by which is real where the integrand is, and off
        /// the real line that has nothing to go on. <c>i &lt; 0</c> and <c>i &gt; 0</c> are each
        /// NaN, the complex numbers not being ordered, and a piecewise with no arm that holds is
        /// NaN with them: <c>1/(x^2 + i)</c> and <c>1/sqrt(i x^2 + 1)</c> answered <c>NaN + C</c>,
        /// and <c>1/sqrt(i a x^2 + 1)</c> a piecewise that is NaN for every real <c>a</c> but zero.
        /// Nor is a real sign any help where the rest of the quadratic is not real: with
        /// <c>E = b^2 - 4ac</c> the arcsine differentiates back to <c>1/sqrt(Q)</c> only where
        /// <c>sqrt(E) sqrt(Q/E)</c> is <c>sqrt(Q)</c>, and <c>1/sqrt(-x^2 + (1 + i) x + 1)</c>
        /// came back as minus its integrand below -2. The arctangent and the logarithm use nothing about their root but
        /// <c>sqrt(q)^2 = q</c>, and are antiderivatives off the real line as well.
        /// https://github.com/asc-community/AngouriMath/issues/1598
        /// </remarks>
        private static Entity.Providedf? ArmOffTheRealLine(Entity form, params Entity[] coefficients)
        {
            Entity? offTheRealLine = null;
            foreach (var coefficient in coefficients)
                if (coefficient.InnerSimplified.Nodes.Any(node => node is Entity.Number.Complex number && !number.ImaginaryPart.EDecimal.IsZero))
                    offTheRealLine = offTheRealLine is null
                        ? !coefficient.In(AngouriMath.Core.Domain.Real)
                        : offTheRealLine | !coefficient.In(AngouriMath.Core.Domain.Real);
            return offTheRealLine is null ? null : new Entity.Providedf(form, offTheRealLine);
        }

        /// <summary>
        /// ∫ (px + q)/(ax^2 + bx + c) dx, by writing the numerator as a multiple of the
        /// denominator's derivative plus a constant:
        /// px + q = (p/2a)(2ax + b) + (q - pb/2a). The first part integrates to a logarithm
        /// and the second is the constant-numerator case above.
        /// </summary>
        /// <remarks>
        /// With <c>a</c> a symbol the constant-numerator case is a piecewise with an arm for
        /// <c>a = 0</c>, and on that arm the multiple of the derivative, which divided by
        /// <c>a</c>, is not the integrand's: there it is <c>(px + q)/(bx + c)</c>, which is
        /// <c>px/b + (q - pc/b) ln(bx + c)/b</c>. Timofeev's <c>(b1 + c1 x)/(a + 2b x + c x^2)</c>
        /// had no antiderivative, with the arm declined for the symbol in front.
        /// </remarks>
        private static Entity IntegrateLinearOverQuadratic(
            Entity p, Entity q, Entity a, Entity b, Entity c, Entity denominator, Entity.Variable x)
            => WithTheArmForAZeroLeadingCoefficient(
                IntegrateRationalQuadratic(q - p * b / (2 * a), a, b, c, x),
                a,
                p / (2 * a) * AntiderivativeLog(denominator),
                LinearOverPowerOfLinear(p, q, b, c, 1, x));

        /// <summary>
        /// <paramref name="answer"/> with <paramref name="added"/> added to every arm but the
        /// one for a vanishing <paramref name="a"/>, which is replaced by
        /// <paramref name="onTheLinearArm"/>; <paramref name="added"/> divided by
        /// <paramref name="a"/> and is not the integrand's there.
        /// </summary>
        private static Entity WithTheArmForAZeroLeadingCoefficient(Entity answer, Entity a, Entity added, Entity onTheLinearArm)
        {
            if (answer is not Entity.Piecewise piecewise)
                return added + answer;
            // The arm's predicate is simplified with the piecewise; the coefficient as read is not.
            var leading = a.InnerSimplified;
            var arms = new List<Entity.Providedf>();
            foreach (var arm in piecewise.Cases)
                arms.Add(arm.Predicate is Entity.Equalsf(var left, var right)
                         && (left.InnerSimplified == leading && TreeAnalyzer.IsZero(right) || right.InnerSimplified == leading && TreeAnalyzer.IsZero(left))
                    ? new Entity.Providedf(onTheLinearArm, arm.Predicate)
                    : new Entity.Providedf(added + arm.Expression, arm.Predicate));
            return MathS.Piecewise(arms).InnerSimplified;
        }

        /// <summary>
        /// ∫ (px + q)/(bx + c)^n dx, with <c>u = bx + c</c>: <c>(p/b) u + (q - pc/b)</c> over
        /// <c>u^n</c>, by <c>du/b</c>. For <c>b = 0</c> the integrand is a linear over a constant.
        /// </summary>
        private static Entity LinearOverPowerOfLinear(Entity p, Entity q, Entity b, Entity c, int n, Entity.Variable x)
        {
            if (TreeAnalyzer.IsZero(b))
                return (p * x * x / 2 + q * x) / MathS.Pow(c, n);
            var u = b * x + c;
            var alongU = n == 1 ? p * x / b
                : n == 2 ? p / (b * b) * AntiderivativeLog(u)
                : p / (b * b) * MathS.Pow(u, 2 - n) / (2 - n);
            var constant = q - p * c / b;
            var overU = n == 1 ? constant * AntiderivativeLog(u) / b : constant * MathS.Pow(u, 1 - n) / (b * (1 - n));
            return alongU + overU;
        }

        private static Entity IntegrateRationalQuadratic(Entity numerator, Entity a, Entity b, Entity c, Entity.Variable x)
        {
            // The formula depends on whether it's linear (a = 0) or quadratic (a ≠ 0)
            // Case 0: a = 0 (linear, not quadratic)
            // ∫ k/(bx + c) dx = (k/b) * ln|bx + c|, which needs b ≠ 0. Where the denominator
            // has no x term the integrand is the constant k/c and its antiderivative is kx/c;
            // writing the logarithm there divides by zero, and a Providedf carrying NaN takes
            // the whole piecewise with it. That is only ever reached when a is symbolic, since
            // a numeric a makes this arm decidably unreachable and it is dropped before the
            // NaN can propagate -- so k/(a x^2 + c) answered NaN while k/(2 x^2 + c) did not.
            // And where c is zero as well, that constant is k/0: with a, b and c all zero the
            // denominator is identically zero and there is no integrand to have an
            // antiderivative. So the branch is not merely unreachable, it is asking about an
            // expression that does not exist, and the piecewise leaves it out rather than
            // carrying a NaN that swallows the cases that do exist. `1/(a*x^2)` came back as
            // `NaN + C` -- an assertion that it has no antiderivative -- where it is `-1/(a x)`,
            // read off the perfect-square arm below, which is exactly what a zero discriminant
            // gives here. https://github.com/asc-community/AngouriMath/issues/718
            var denominatorVanishesWithoutA = TreeAnalyzer.IsZero(b) && TreeAnalyzer.IsZero(c);
            var linearCase = TreeAnalyzer.IsZero(b)
                ? numerator * x / c
                : numerator * AntiderivativeLog(b * x + c) / b;
            // A decidably linear denominator is the linear case and nothing else: read as a
            // quadratic with `a = 0`, a constant denominator has a zero discriminant, and the
            // perfect-square arm below answered `-2k/(0 x + 0)` -- NaN for `-2/3`, which is what
            // Euler's third substitution makes of `1/(x sqrt(3x - x^2))`.
            if (TreeAnalyzer.IsZero(a))
                return denominatorVanishesWithoutA ? MathS.NaN : linearCase.InnerSimplified;


            // For true quadratics (a ≠ 0), discriminant Δ = 4ac - b^2 determines the form --
            // expanded, so that a square of a symbolic linear is seen for one: `(a w + b)^2`
            // is `a^2 w^2 + 2ab w + b^2`, whose discriminant `4a^2 b^2 - (2ab)^2` is zero only
            // once it is written out, and unwritten it was a piecewise of three arms, two of
            // them dividing by that zero, that nothing downstream could read.
            var discriminant = (4 * a * c - b * b).Expand().InnerSimplified;
            if (TreeAnalyzer.IsZero(discriminant) || IsZeroOnceSimplified(discriminant))
                // The perfect square, in the generic case as every rule answers: -2k/(2ax + b).
                return (-2 * numerator / (2 * a * x + b)).InnerSimplified;
            
            // Case 1: Δ > 0 (no real roots, use arctan)
            // Result: (2k/√Δ) * arctan((2ax + b)/√Δ)
            var sqrtDiscriminant = MathS.Sqrt(discriminant);
            var twoAxPlusB = 2 * a * x + b;
            var arctanCase = 2 * numerator * MathS.Arctan(twoAxPlusB / sqrtDiscriminant) / sqrtDiscriminant;
            
            // Case 2: Δ = 0 (perfect square, one repeated root)
            // ax^2 + bx + c = a(x + b/(2a))^2
            // Result: -2k/(2ax + b)
            var perfectSquareCase = -2 * numerator / twoAxPlusB;
            
            // Case 3: Δ < 0 (two distinct real roots, use logarithm)
            // Result: (k/√(-Δ)) * ln|(2ax + b - √(-Δ))/(2ax + b + √(-Δ))|
            var sqrtNegDiscriminant = MathS.Sqrt(-discriminant);
            var lnCase = numerator * AntiderivativeLog((twoAxPlusB - sqrtNegDiscriminant) / (twoAxPlusB + sqrtNegDiscriminant)) / sqrtNegDiscriminant;
            
            // Return as piecewise based on a and discriminant, without the arms no real
            // coefficients reach: see ReachesAPositiveDiscriminant and ReachesAZeroDiscriminant.
            var cases = new List<Entity.Providedf>();
            if (!denominatorVanishesWithoutA)
                cases.Add(new Entity.Providedf(linearCase, a.EqualTo(0)));
            if (ArmOffTheRealLine(arctanCase, discriminant) is { } offTheRealLine)
                cases.Add(offTheRealLine);
            if (ReachesAPositiveDiscriminant(b, c))
                cases.Add(new Entity.Providedf(arctanCase, discriminant > 0));
            if (ReachesAZeroDiscriminant(b, c))
                cases.Add(new Entity.Providedf(perfectSquareCase, discriminant.EqualTo(0)));
            cases.Add(new Entity.Providedf(lnCase, discriminant < 0));
            return MathS.Piecewise(cases).InnerSimplified;
        }

        /// <summary>
        /// Whether <c>4 a c - b^2</c> can be positive past the arms before it: not with no
        /// constant term and a real <c>b</c>, where it is <c>-b^2</c>. <c>1/(u (a + b u))</c>,
        /// which every <c>1/(x (a + b x^n))</c> comes to, carried an arctangent on
        /// <c>-a^2 > 0</c>. Real as written only: symbols and real numbers by arithmetic and
        /// whole powers. Under `sqrt(-(a + b x)^2)` the coefficient is `sqrt(-b^2) a/b`, and the
        /// discriminant is `a^2`.
        /// </summary>
        private static bool ReachesAPositiveDiscriminant(Entity b, Entity c)
            => !TreeAnalyzer.IsZero(c) || !b.InnerSimplified.Nodes.All(node => node switch
            {
                Entity.Variable or Entity.Number.Real => true,
                Entity.Sumf or Entity.Minusf or Entity.Mulf or Entity.Divf => true,
                Entity.Powf(_, Entity.Number.Integer) => true,
                _ => false
            });

        /// <summary>
        /// Whether <c>4 a c - b^2</c> can be zero where <c>a</c> is not, which the first arm
        /// takes: not with no linear term and a number other than 0 for the constant one, where it
        /// is <c>4 a c</c>. <c>1/(1 + q u^2)</c> carried a perfect-square arm on <c>4 q = 0</c>,
        /// dividing by <c>q</c>.
        /// </summary>
        private static bool ReachesAZeroDiscriminant(Entity b, Entity c)
            => !TreeAnalyzer.IsZero(b) || c is not Entity.Number || c.Evaled is Entity.Number.Complex { IsZero: true };

        /// <summary>
        /// ∫ k / (a x^2 + b x + c)^n dx for a whole n of two or more, by the reduction that
        /// takes one power off the denominator at a time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Write <c>Q</c> for the quadratic, <c>u</c> for its derivative <c>2ax + b</c> and
        /// <c>D</c> for <c>4ac - b^2</c>. Then <c>u^2</c> is <c>4aQ - D</c>, so differentiating
        /// <c>u / Q^(m-1)</c> gives <c>2a(3 - 2m)/Q^(m-1) + (m-1)D/Q^m</c>, and reading that as
        /// an equation for the integral of <c>1/Q^m</c> leaves
        /// <code>
        /// J_m = (u / Q^(m-1) + 2a(2m - 3) J_(m-1)) / ((m - 1) D)
        /// </code>
        /// which is unrolled down to <c>J_1</c>, the case the table above already answers. It is
        /// an identity in a, b and c rather than a fact about signs, so one line serves a
        /// positive and a negative discriminant alike; <c>D = 0</c> is the only exclusion, and
        /// the division by <c>D</c> is why.
        /// </para>
        /// <para>
        /// The two shapes the reduction cannot speak for get their own arms, and each is
        /// elementary: with <c>a = 0</c> the denominator is a linear power, and with
        /// <c>D = 0</c> the quadratic is <c>u^2/(4a)</c> so the whole integrand is a power of
        /// <c>u</c>. Both are already answered for a numeric quadratic — by the power rule and
        /// by partial fractions — and the arms are here for a symbolic one, where the
        /// discriminant's sign is not known and dropping an arm is not available.
        /// </para>
        /// <para>
        /// What was missing was the irreducible case. <c>1/(x^2 - 1)^2</c> had an antiderivative
        /// and <c>1/(x^2 + 2x + 1)^2</c> had one, because a denominator with real roots comes
        /// apart into linear factors and never reaches here; <c>1/(x^2 + 1)^2</c> had none.
        /// https://github.com/asc-community/AngouriMath/issues/180
        /// </para>
        /// </remarks>
        private static Entity IntegrateOverPowerOfQuadratic(
            Entity numerator, Entity a, Entity b, Entity c, int power, Entity.Variable x, bool withoutTheFirstPower = false)
        {
            var quadratic = a * x * x + b * x + c;
            var derivative = 2 * a * x + b;
            var discriminant = 4 * a * c - b * b;
            // A discriminant that is zero once its spellings meet -- `4a^2 b^(-2)` against
            // `4 (1/b)^2 a^2` for `(x + (1 + i a)/(i b))^2` -- is the perfect square and not a
            // piecewise of three arms, two of them dividing by that zero: the piecewise
            // simplified to NaN, and an integral of `x e^(-2 i arctan(a + b x))` with it.
            var expanded = discriminant.Expand().InnerSimplified;
            if (!TreeAnalyzer.IsZero(a) && (TreeAnalyzer.IsZero(expanded) || IsZeroOnceSimplified(expanded)))
                return (numerator * MathS.Pow(4 * a, power) * MathS.Pow(derivative, 1 - 2 * power)
                    / (2 * a * (1 - 2 * power))).InnerSimplified;

            // a = 0: k/(bx + c)^n, an ordinary power -- and the constant k/c^n when b is zero
            // too, where writing the power would divide by it. Same reason as the rule above.
            var linearCase = TreeAnalyzer.IsZero(b)
                ? numerator * x / MathS.Pow(c, power)
                : numerator * MathS.Pow(b * x + c, 1 - power) / (b * (1 - power));

            // D = 0: the quadratic is u^2/(4a), so the integrand is k(4a)^n / u^(2n).
            var perfectSquareCase =
                numerator * MathS.Pow(4 * a, power) * MathS.Pow(derivative, 1 - 2 * power)
                    / (2 * a * (1 - 2 * power));

            var sqrtDiscriminant = MathS.Sqrt(discriminant);
            var sqrtNegDiscriminant = MathS.Sqrt(-discriminant);

            // The first power's antiderivative is what the reduction bottoms out on, and where
            // the caller has found that it cancels across every piece it is left out here.
            Entity firstPowerArctan = withoutTheFirstPower ? Entity.Number.Integer.Zero
                : 2 * MathS.Arctan(derivative / sqrtDiscriminant) / sqrtDiscriminant;
            Entity firstPowerLog = withoutTheFirstPower ? Entity.Number.Integer.Zero
                : AntiderivativeLog((derivative - sqrtNegDiscriminant) / (derivative + sqrtNegDiscriminant)) / sqrtNegDiscriminant;
            var arms = new List<Entity.Providedf> { new Entity.Providedf(linearCase, a.EqualTo(0)) };
            if (ReachesAZeroDiscriminant(b, c))
                arms.Add(new Entity.Providedf(perfectSquareCase, discriminant.EqualTo(0)));
            var arctanArm = Reduce(firstPowerArctan);
            if (ArmOffTheRealLine(arctanArm, discriminant) is { } offTheRealLine)
                arms.Add(offTheRealLine);
            if (ReachesAPositiveDiscriminant(b, c))
                arms.Add(new Entity.Providedf(arctanArm, discriminant > 0));
            arms.Add(new Entity.Providedf(Reduce(firstPowerLog), discriminant < 0));
            return MathS.Piecewise(arms).InnerSimplified;

            Entity Reduce(Entity firstPower)
            {
                var accumulated = firstPower;
                for (var m = 2; m <= power; m++)
                    accumulated =
                        (derivative / MathS.Pow(quadratic, m - 1) + 2 * a * (2 * m - 3) * accumulated)
                            / ((m - 1) * discriminant);
                return numerator * accumulated;
            }
        }

        /// <summary>
        /// ∫ (px + q)/(a x^2 + b x + c)^n dx, by the same rewrite of the numerator the single
        /// power uses: <c>px + q = (p/2a)(2ax + b) + (q - pb/2a)</c>. The first part is the
        /// denominator's own derivative over a power of it, which integrates as a power; the
        /// second is the constant-numerator case above.
        /// </summary>
        /// <remarks>
        /// A numerator that is already a multiple of that derivative leaves nothing of the
        /// second part, and it is dropped rather than multiplied by zero. Multiplying is not
        /// harmless: the reduction is a quotient by the quadratic, so <c>0 * (2x/(x^2 + 1))</c>
        /// is <c>0 provided not 1 + x^2 = 0</c> and not <c>0</c> — correctly, since the factor
        /// has no value there. Carrying that condition would attach it to the answer for
        /// <c>x/(x^2 + 1)^2</c>, which had none before and is owed none.
        /// </remarks>
        private static Entity IntegrateLinearOverPowerOfQuadratic(
            Entity p, Entity q, Entity a, Entity b, Entity c, Entity quadratic, int power, Entity.Variable x, bool withoutTheFirstPower = false)
        {
            var alongTheDerivative = p / (2 * a) * MathS.Pow(quadratic, 1 - power) / (1 - power);
            var constantPart = q - p * b / (2 * a);
            if (TreeAnalyzer.IsZero(constantPart))
                return alongTheDerivative;
            return WithTheArmForAZeroLeadingCoefficient(
                IntegrateOverPowerOfQuadratic(constantPart, a, b, c, power, x, withoutTheFirstPower),
                a,
                alongTheDerivative,
                LinearOverPowerOfLinear(p, q, b, c, power, x));
        }

        /// <summary>
        /// ∫ N(x)/(a x^2 + b x + c)^n dx for a numerator of any degree, or <see langword="null"/>
        /// where the fraction is improper and this is not the rule for it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Dividing the numerator by the quadratic writes <c>N = qQ + r</c> with <c>r</c> linear
        /// at worst, so <c>N/Q^n</c> is <c>q/Q^(n-1) + r/Q^n</c> — one power off the denominator
        /// and two degrees off the numerator at each step, which ends. The remainder is answered
        /// by the two helpers above and the quotient goes round again.
        /// </para>
        /// <para>
        /// <b>Improper fractions are declined rather than divided out here.</b> The recursion
        /// bottoms out at a single power, and a numerator still of degree two or more there is
        /// exactly the improper case, which
        /// <see cref="IndefiniteIntegralSolver.SolveByPartialFractions"/> already opens with
        /// <see cref="TreeAnalyzer.PolynomialLongDivision"/>. Declining hands it there instead of
        /// answering it twice, in two shapes, from two places.
        /// </para>
        /// <para>
        /// This is not only reach. <c>TryStandardIntegrals</c> runs before every search, so a
        /// shape answered here never reaches the candidate exploration that re-integrates
        /// rewritten forms — which is where this family's cost has always been. Both
        /// <c>x^2/(x^2 + 2)^2</c> and <c>(x^2 + 1)/(x^2 + 2)^2</c> were tens of seconds spent to
        /// return the integral unevaluated.
        /// </para>
        /// </remarks>
        private static Entity? IntegrateRationalOverPowerOfQuadratic(
            Entity numerator, Entity a, Entity b, Entity c, Entity quadratic, int power, Entity.Variable x, bool withoutTheFirstPower = false)
        {
            if (!numerator.ContainsNode(x))
                return power == 1
                    ? withoutTheFirstPower ? Entity.Number.Integer.Zero : IntegrateRationalQuadratic(numerator, a, b, c, x)
                    : IntegrateOverPowerOfQuadratic(numerator, a, b, c, power, x, withoutTheFirstPower);

            if (TreeAnalyzer.TryGetPolyLinear(numerator, x, out var p, out var q))
                return power == 1
                    ? withoutTheFirstPower ? p / (2 * a) * AntiderivativeLog(quadratic) : IntegrateLinearOverQuadratic(p, q, a, b, c, quadratic, x)
                    : IntegrateLinearOverPowerOfQuadratic(p, q, a, b, c, quadratic, power, x, withoutTheFirstPower);

            if (power < 2)
                return null;

            // An improper fraction comes down to the first power with a numerator of the second
            // degree, and is declined there, after a division at every power above it: declined
            // before the first. With symbols in the quadratic each division is a simplification
            // of every coefficient.
            if (TreeAnalyzer.TryGetPolynomial(numerator, x, out var terms) && terms.Count > 0
                && terms.Keys.Max()!.CompareTo(EInteger.FromInt32(2 * power)) >= 0)
                return null;

            var division = TreeAnalyzer.PolynomialLongDivision(numerator, quadratic, genericCase: true, inTermsOf: x);
            if (division is null)
                return null;

            // The quotient is what is wanted; the remainder that comes back with it is already
            // divided by the divisor -- x^2 over x^2 + 2 answers (1, -2/(x^2 + 2)) -- so taking
            // it as a polynomial and dividing it again is a numerator that is not one, and the
            // next division declines. Subtracting gives the polynomial remainder whatever shape
            // the division chose to report.
            // Reduced before either is used: the division reports its quotient built up term by
            // term, so x^2 over x^2 + 2 comes back as `0 + 1` rather than `1`, and the arms below
            // ask whether a numerator is constant.
            var quotient = Functions.PartialFractions.Bare(division.Value.Divided.InnerSimplified);
            var remainder = Functions.PartialFractions.Bare((numerator - quotient * quadratic).InnerSimplified);
            if (remainder.ContainsNode(x) && !TreeAnalyzer.TryGetPolyLinear(remainder, x, out _, out _))
                remainder = Functions.PartialFractions.Bare((numerator - quotient * quadratic).Expand().InnerSimplified);
            // The division writes the quotient's coefficients simplified, and the quadratic is
            // subtracted as it is written, so a term the division took away can come back as the
            // difference of two spellings of one value: `x^4` over `1/(-b + a) + x^2` left
            // `(1/(a - b) - 1/(-b + a)) x^2`, and the reduction declined where over `1/(a - b) + x^2`
            // it answered. The remainder the division reports over the divisor has no such term.
            if (remainder.ContainsNode(x) && !TreeAnalyzer.TryGetPolyLinear(remainder, x, out _, out _)
                && division.Value.Remainder is Entity.Divf(var reportedAbove, var reportedBelow) && reportedBelow == quadratic
                && (!reportedAbove.ContainsNode(x) || TreeAnalyzer.TryGetPolyLinear(reportedAbove, x, out _, out _)))
                remainder = Functions.PartialFractions.Bare(reportedAbove.InnerSimplified);
            // The remainder of a division by a quadratic is linear or a constant; one that
            // still has the variable to a higher power -- the division declined in its own
            // way, on `csch(29/10 + 13/10 x)^3 (17/10 + 23/10 sech(...)^2)^3` through the
            // exponential substitution -- went round this recursion at the same power until
            // the stack ran out. https://github.com/asc-community/AngouriMath/issues/1373
            if (remainder.ContainsNode(x) && !TreeAnalyzer.TryGetPolyLinear(remainder, x, out _, out _))
                return null;

            return IntegrateRationalOverPowerOfQuadratic(remainder, a, b, c, quadratic, power, x, withoutTheFirstPower) is { } head
                   && IntegrateRationalOverPowerOfQuadratic(quotient, a, b, c, quadratic, power - 1, x, withoutTheFirstPower) is { } rest
                ? head + rest
                : null;
        }

        /// <summary>
        /// Whether, for a quadratic whose coefficients are real numbers with a nonzero leading
        /// one and a nonzero discriminant, the first power's antiderivative -- the logarithm or
        /// the arctangent every reduction above bottoms out on -- has coefficient zero once the
        /// pieces of ∫ N(x)/Q^n are added up.
        /// </summary>
        /// <remarks>
        /// The reduction answers each constant over a power of the quadratic on its own, down
        /// to that first power, so a numerator of degree four over a fourth power arrives as
        /// three reductions each carrying the logarithm, with coefficients that sum to zero
        /// where the antiderivative is rational: <c>8(x^4 + x^2)/(x^2 - 1)^4</c> is
        /// <c>-8x^3/(3(x^2 - 1)^3)</c>, and it was written with <c>ln((x - 1)/(x + 1))</c> three
        /// times over. Correct, and read by nothing: integration by parts against it -- it is
        /// <c>cosh(x)/sinh(x)^4</c> under <c>u = e^x</c>, and Timofeev's
        /// <c>arccot(cosh x) cosh x/sinh^4 x</c> is a step of parts against that -- spent four
        /// seconds simplifying the logarithms out of the remainder. The coefficient is a number
        /// and is added up here before anything is written; the writing itself is unchanged.
        /// </remarks>
        private static bool TheFirstPowerCancels(
            Entity numerator, Entity a, Entity b, Entity c, Entity quadratic, int power, Entity.Variable x)
        {
            if (a.Evaled is not Entity.Number.Real { IsZero: false } || b.Evaled is not Entity.Number.Real || c.Evaled is not Entity.Number.Real)
                return false;
            var discriminant = (4 * a * c - b * b).InnerSimplified;
            if (TreeAnalyzer.IsZero(discriminant))
                return false;
            Entity first = Entity.Number.Integer.Zero;
            return Gather(numerator, power) && TreeAnalyzer.IsZero(first.InnerSimplified);

            bool Gather(Entity above, int n)
            {
                if (!above.ContainsNode(x))
                {
                    ConstantOverPower(above, n);
                    return true;
                }
                if (TreeAnalyzer.TryGetPolyLinear(above, x, out var p, out var q))
                {
                    // px + q is (p/2a)(2ax + b) + (q - pb/2a): the first part is the
                    // derivative over the power, which carries no first power.
                    var constantPart = (q - p * b / (2 * a)).InnerSimplified;
                    if (!TreeAnalyzer.IsZero(constantPart))
                        ConstantOverPower(constantPart, n);
                    return true;
                }
                if (n < 2)
                    return false;
                var division = TreeAnalyzer.PolynomialLongDivision(above, quadratic, genericCase: true, inTermsOf: x);
                if (division is null)
                    return false;
                var quotient = division.Value.Divided.InnerSimplified;
                var remainder = (above - quotient * quadratic).InnerSimplified;
                // The remainder of a division by a quadratic is linear or a constant; one
                // that still has the variable to a higher power -- the division declined in
                // its own way, on `csch(29/10 + 13/10 x)^3 (17/10 + 23/10 sech(...)^2)^3`
                // through the exponential substitution -- went round this recursion at the
                // same n until the stack ran out.
                // https://github.com/asc-community/AngouriMath/issues/1373
                if (remainder.ContainsNode(x) && !TreeAnalyzer.TryGetPolyLinear(remainder, x, out _, out _))
                    return false;
                return Gather(remainder, n) && Gather(quotient, n - 1);
            }

            // J_m = (Q'/Q^(m-1) + 2a(2m - 3) J_(m-1))/((m - 1) D), unrolled to J_1: the
            // multiple of J_1 is the product of the scalings.
            void ConstantOverPower(Entity k, int n)
            {
                Entity ofFirst = Entity.Number.Integer.One;
                for (var m = 2; m <= n; m++)
                    ofFirst = ofFirst * (2 * a * (2 * m - 3)) / ((m - 1) * discriminant);
                first += k * ofFirst;
            }
        }
    }
}
