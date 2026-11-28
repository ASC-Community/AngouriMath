//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra
{
    /// <summary>
    /// The points strictly inside a numeric range where an antiderivative can stop being
    /// continuous, so that a definite integral is taken through it piece by piece.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>F(b) - F(a)</c> is the integral only where <c>F</c> is continuous on <c>[a, b]</c>. A pole
    /// of the integrand between the bounds is a point where <c>F</c> is not even defined: the
    /// integral of <c>1/x^2</c> over <c>[-1, 1]</c> came back as <c>-1/x</c> at 1 less <c>-1/x</c>
    /// at -1, which is <c>-2</c>, a negative answer for a positive integrand whose integral
    /// diverges. An antiderivative can also jump where the integrand is continuous, and the
    /// answer through it is then wrong by the jump: <c>arccot(x)</c> jumps at 0 in this library's
    /// convention, and a half-angle substitution writes <c>tan(x/2)</c>.
    /// </para>
    /// <para>
    /// The places read are where a node of the antiderivative stops being continuous: the zeros
    /// of a denominator, of a base raised to a negative power, of a logarithm's argument, of the
    /// argument of <c>sgn</c>, <c>arccot</c>, <c>arcsec</c> and <c>arccsc</c>, and the zeros of the
    /// cosine or the sine under a tangent, secant, cotangent or cosecant of a linear argument.
    /// Where one of them cannot be listed -- a root with a symbol in it, a periodic argument
    /// that is not linear, more breaks than <see cref="MostBreaks"/> -- the caller is told so,
    /// and integrates as it did before.
    /// </para>
    /// <para>
    /// A break is carried as its exact point, where the one-sided limits are taken, and its
    /// decimal value, which only orders the breaks and places them in the range.
    /// https://github.com/asc-community/AngouriMath/issues/1508
    /// </para>
    /// </remarks>
    internal static class AntiderivativeBreaks
    {
        /// <summary>
        /// More breaks than this in one range are not listed: a tangent over a thousand
        /// periods has no integral to find, and listing its poles would be all the work.
        /// </summary>
        private const int MostBreaks = 64;

        /// <summary>
        /// The points strictly between <paramref name="lower"/> and <paramref name="upper"/>,
        /// ascending, where <paramref name="antiderivative"/> may break, or
        /// <see langword="null"/> where one of them cannot be listed.
        /// </summary>
        internal static List<(Entity At, Real Value)>? Inside(Entity antiderivative, Variable x, Real lower, Real upper)
        {
            var breaks = new List<(Entity At, Real Value)>();
            // A subexpression repeated in the antiderivative is read once.
            var read = new HashSet<Entity>();
            foreach (var node in antiderivative.Nodes)
            {
                if (!read.Add(node))
                    continue;
                var listed = node switch
                {
                    Divf(_, var denominator) when denominator.ContainsNode(x) => ZerosOf(denominator, x, lower, upper, breaks),
                    Powf(var @base, Real { IsNegative: true }) when @base.ContainsNode(x) => ZerosOf(@base, x, lower, upper, breaks),
                    Logf(_, var antilogarithm) when antilogarithm.ContainsNode(x) => ZerosOf(antilogarithm, x, lower, upper, breaks),
                    Signumf(var argument) when argument.ContainsNode(x) => ZerosOf(argument, x, lower, upper, breaks),
                    Arccotanf(var argument) when argument.ContainsNode(x) => ZerosOf(argument, x, lower, upper, breaks),
                    Arcsecantf(var argument) when argument.ContainsNode(x) => ZerosOf(argument, x, lower, upper, breaks),
                    Arccosecantf(var argument) when argument.ContainsNode(x) => ZerosOf(argument, x, lower, upper, breaks),
                    Tanf(var argument) when argument.ContainsNode(x) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: true, breaks),
                    Secantf(var argument) when argument.ContainsNode(x) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: true, breaks),
                    Cotanf(var argument) when argument.ContainsNode(x) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: false, breaks),
                    Cosecantf(var argument) when argument.ContainsNode(x) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: false, breaks),
                    _ => true
                };
                if (!listed || breaks.Count > MostBreaks)
                    return null;
            }
            breaks.Sort((a, b) => a.Value.CompareTo(b.Value));
            // The same point from two nodes -- a pole of -ln(cos(x)) is also a zero of the
            // cosine's -- is one break.
            var distinct = new List<(Entity At, Real Value)>();
            foreach (var point in breaks)
                if (distinct.Count == 0 || !Coincide(distinct[distinct.Count - 1].Value, point.Value))
                    distinct.Add(point);
            return distinct;
        }

        /// <summary>
        /// Adds the real zeros of <paramref name="expression"/> strictly inside the range, and
        /// says whether they could all be listed. A cosine or a sine is zero on a periodic set,
        /// which the solver writes with an integer parameter, so it is listed across the range
        /// instead: <c>-ln(cos(x))</c>, the antiderivative of <c>tan(x)</c>, has no tangent in it
        /// to read. The modulus and a positive power are zero where what they hold is, a product
        /// where one of its factors is, and a quotient where its numerator is.
        /// </summary>
        internal static bool ZerosOf(Entity expression, Variable x, Real lower, Real upper, List<(Entity At, Real Value)> breaks)
            => expression switch
            {
                Cosf(var argument) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: true, breaks),
                Sinf(var argument) => PeriodicZeros(argument, x, lower, upper, halfPeriodShift: false, breaks),
                Absf(var inner) => ZerosOf(inner, x, lower, upper, breaks),
                Powf(var inner, Real { IsPositive: true }) => ZerosOf(inner, x, lower, upper, breaks),
                Mulf(var left, var right) =>
                    (!left.ContainsNode(x) || ZerosOf(left, x, lower, upper, breaks))
                    && (!right.ContainsNode(x) || ZerosOf(right, x, lower, upper, breaks)),
                // A quotient is zero where its numerator is; its denominator is read as one.
                Divf(var numerator, _) => !numerator.ContainsNode(x) || ZerosOf(numerator, x, lower, upper, breaks),
                _ => SolvedZeros(expression, x, lower, upper, breaks)
            };

        /// <summary>The real zeros the solver lists for <paramref name="expression"/>, strictly inside the range.</summary>
        private static bool SolvedZeros(Entity expression, Variable x, Real lower, Real upper, List<(Entity At, Real Value)> breaks)
        {
            // With a family's parameter free, so that its real members can be read off it below.
            if (EquationSolver.Solve(expression, x) is not FiniteSet families)
                return false;
            var roots = new List<Entity>();
            foreach (var family in families)
                if (!RealMembers(family, expression, x, lower, upper, roots))
                    return false;
            foreach (var root in roots)
            {
                if (root.ContainsNode(x) || root.Vars.Any())
                    return false;
                switch (root.Evaled)
                {
                    case Real { EDecimal.IsFinite: true } value:
                        if (value > lower && value < upper)
                            breaks.Add((root, value));
                        break;
                    // A real root written through i, ln(r) / i + 2 pi n, evaluates with an imaginary
                    // part that is rounding alone.
                    case Complex { RealPart.EDecimal.IsFinite: true } nearlyReal when IsRounding(nearlyReal.ImaginaryPart):
                        if (nearlyReal.RealPart > lower && nearlyReal.RealPart < upper)
                            breaks.Add((root, nearlyReal.RealPart));
                        break;
                    case Complex { ImaginaryPart.IsZero: false }:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The members of a root family <c>p + q n</c>, one for each whole <c>n</c>, that can lie
        /// strictly inside the range, added to <paramref name="roots"/>; a root with no parameter
        /// is added as it is. False where the family is not linear in one parameter with numeric
        /// <c>p</c> and <c>q</c>, or has more members in the range than <see cref="MostBreaks"/>.
        /// </summary>
        /// <remarks>
        /// Over the complex numbers <c>e ^ x = 2</c> is <c>ln(2) + 2 pi i n</c>, real at <c>n = 0</c>
        /// alone, and a trigonometric equation solved through <c>e ^ (i x)</c> is a family with a
        /// real step. Read as a root with a symbol in it, either declined the breaks, and the
        /// integral was taken across a pole: 1/(e^x - 2) over [0, 1] came back finite.
        /// https://github.com/asc-community/AngouriMath/issues/2319
        /// </remarks>
        private static bool RealMembers(Entity family, Entity expression, Variable x, Real lower, Real upper, List<Entity> roots)
        {
            var parameters = family.Vars.Where(v => v != x && !expression.Vars.Contains(v)).Distinct().ToList();
            if (parameters.Count == 0)
            {
                roots.Add(family);
                return true;
            }
            if (parameters.Count > 1 || !lower.EDecimal.IsFinite || !upper.EDecimal.IsFinite
                || !TreeAnalyzer.TryGetPolyLinear(family, parameters[0], out var step, out var start)
                || step.Evaled is not Complex { IsFinite: true, IsZero: false } q
                || start.Evaled is not Complex { IsFinite: true } p)
                return false;
            Entity Member(EInteger n) => family.Substitute(parameters[0], Integer.Create(n)).InnerSimplified;
            if (!q.ImaginaryPart.IsZero)
            {
                // Real at most where the imaginary parts cancel, n = -Im(p) / Im(q).
                var at = (-p.ImaginaryPart / q.ImaginaryPart).EDecimal;
                var whole = at.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.HalfEven));
                if (at.Subtract(whole).Abs().CompareTo(EDecimal.Create(1, -30)) < 0)
                    roots.Add(Member(whole.ToEInteger()));
                return true;
            }
            if (!IsRounding(p.ImaginaryPart))
                return true;
            // A real step: n runs over the whole numbers between the ends mapped back.
            var (atLower, atUpper) = ((lower - p.RealPart) / q.RealPart, (upper - p.RealPart) / q.RealPart);
            var (from, to) = atLower < atUpper ? (atLower, atUpper) : (atUpper, atLower);
            var first = from.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Floor)).ToEInteger();
            var last = to.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Ceiling)).ToEInteger();
            if ((last - first).CompareTo(EInteger.FromInt32(MostBreaks)) > 0)
                return false;
            for (var n = first; n.CompareTo(last) <= 0; n += EInteger.One)
                roots.Add(Member(n));
            return true;
        }

        private static bool IsRounding(Real part) => part.EDecimal.Abs().CompareTo(EDecimal.Create(1, -30)) < 0;

        /// <summary>
        /// Adds the zeros of the cosine (<paramref name="halfPeriodShift"/>) or the sine of a
        /// linear argument strictly inside the range: where <c>a x + b</c> is <c>pi/2 + k pi</c>,
        /// or <c>k pi</c>, for a whole <c>k</c>.
        /// </summary>
        private static bool PeriodicZeros(Entity argument, Variable x, Real lower, Real upper, bool halfPeriodShift, List<(Entity At, Real Value)> breaks)
        {
            if (!TreeAnalyzer.TryGetPolyLinear(argument, x, out var a, out var b)
                || a.Evaled is not Real { EDecimal.IsFinite: true, IsZero: false } slope
                || b.Evaled is not Real { EDecimal.IsFinite: true } intercept
                || MathS.pi.Evaled is not Real pi)
                return false;
            if (!lower.EDecimal.IsFinite || !upper.EDecimal.IsFinite)
                return false;
            // x = (c + k pi - b)/a, so k runs over the whole numbers between the ends mapped back.
            var shift = halfPeriodShift ? pi / 2 : Integer.Zero;
            var atLower = (slope * lower + intercept - shift) / pi;
            var atUpper = (slope * upper + intercept - shift) / pi;
            var (from, to) = atLower < atUpper ? (atLower, atUpper) : (atUpper, atLower);
            var first = from.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Floor)).ToEInteger();
            var last = to.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Ceiling)).ToEInteger();
            if ((last - first).CompareTo(EInteger.FromInt32(MostBreaks)) > 0)
                return false;
            for (var k = first; k.CompareTo(last) <= 0; k += EInteger.One)
            {
                var at = ((halfPeriodShift ? MathS.pi / 2 : Integer.Zero) + Integer.Create(k) * MathS.pi - b) / a;
                if (at.InnerSimplified.Evaled is Real { EDecimal.IsFinite: true } value && value > lower && value < upper)
                    breaks.Add((at.InnerSimplified, value));
            }
            return true;
        }

        /// <summary>
        /// The points of <c>[<paramref name="lower"/>, <paramref name="upper"/>]</c> where a node of
        /// <paramref name="antiderivative"/> whose argument leaves the real line crosses its branch cut, each with the
        /// node, the point, its decimal value and the unit its jump there is a whole multiple of; an empty list where no
        /// argument leaves the real line, or none crosses a cut; and <see langword="null"/> where a crossing cannot be
        /// ruled out or its jump cannot be settled. Bounds that are not numbers are passed as <see langword="null"/>, and
        /// any crossing at all is then one that cannot be placed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Inside"/> reads the real line with the parameters real, where <c>arctan</c> and the logarithm of a
        /// real argument have no cut to cross. A complex constant takes the argument off the line, and the path then
        /// can cross a cut where the integrand is continuous: <c>arctan(t - 3i)</c>, the antiderivative of
        /// <c>1/(1 + (t - 3i)^2)</c>, meets the imaginary axis at <c>-3i</c>, on <c>arctan</c>'s cut, and jumps by
        /// <c>pi</c> there, so its values at -2 and 2 differ by <c>pi</c> more than the integral between them does.
        /// https://github.com/asc-community/AngouriMath/issues/2460
        /// </para>
        /// <para>
        /// The cuts placed are <c>arctan</c>'s, the imaginary axis beyond <c>±i</c>, and the logarithm's, the negative
        /// real axis: for an argument that is a polynomial with numeric coefficients, where its real or its imaginary
        /// part, a real polynomial, is zero; and for the logarithm of the cosine or the sine of <c>a t + b</c>, which
        /// <c>tan</c> and <c>cot</c> integrate to, where its imaginary part is zero (see
        /// <see cref="TrigonometricCrossings"/>). Their jumps are whole multiples of <c>pi</c> and of
        /// <c>2 pi i / ln(base)</c>, and which multiple is read from the values a little either side. A power with an
        /// argument off the line is taken where its base does not cross the negative real axis, since its jump there
        /// is not a constant; a tangent, secant, cotangent or cosecant where its argument does not meet the real line,
        /// where its poles are. Anything else with an argument off the line is not read at all.
        /// </para>
        /// </remarks>
        internal static List<(Entity Node, Entity At, Real Value, Entity Unit)>? CutCrossings(Entity antiderivative, Variable x, Real? lower, Real? upper)
        {
            var crossings = new List<(Entity Node, Entity At, Real Value, Entity Unit)>();
            var read = new HashSet<Entity>();
            foreach (var node in antiderivative.Nodes)
            {
                if (!read.Add(node) || !node.DirectChildren.Any(child => child.ContainsNode(x) && OffTheRealLine(child)))
                    continue;
                var settled = node switch
                {
                    Sumf or Minusf or Mulf or Divf or Absf or Signumf or Sinf or Cosf
                        or Erff or Erfcf or Erfif or Sif or Shif or Conjugatef or Ref or Imf => true,
                    // b^g = e^(g ln b) for a constant b, and an integer power of anything, are continuous wherever their
                    // argument is.
                    Powf(var @base, _) when !@base.ContainsNode(x) || !OffTheRealLine(@base) => true,
                    Powf(_, Integer) => true,
                    Powf(var @base, _) => PolynomialCrossings(@base, x, imaginaryAxis: false) is { } points
                        && points.All(point => lower is not null && upper is not null && (point.Value < lower || point.Value > upper)),
                    Tanf(var argument) => MeetsTheRealLine(argument, x) is false,
                    Secantf(var argument) => MeetsTheRealLine(argument, x) is false,
                    Cotanf(var argument) => MeetsTheRealLine(argument, x) is false,
                    Cosecantf(var argument) => MeetsTheRealLine(argument, x) is false,
                    Arctanf(var argument) => Add(node, PolynomialCrossings(argument, x, imaginaryAxis: true), MathS.pi),
                    Logf(var @base, Cosf(var argument)) when !@base.ContainsNode(x) =>
                        Add(node, TrigonometricCrossings(argument, x, lower, upper, cosine: true), 2 * MathS.pi * MathS.i / MathS.Ln(@base)),
                    Logf(var @base, Sinf(var argument)) when !@base.ContainsNode(x) =>
                        Add(node, TrigonometricCrossings(argument, x, lower, upper, cosine: false), 2 * MathS.pi * MathS.i / MathS.Ln(@base)),
                    Logf(var @base, var argument) when !@base.ContainsNode(x) =>
                        Add(node, PolynomialCrossings(argument, x, imaginaryAxis: false), 2 * MathS.pi * MathS.i / MathS.Ln(@base)),
                    _ => false
                };
                if (!settled)
                    return null;
            }
            return crossings;

            bool Add(Entity node, List<(Entity At, Real Value)>? points, Entity unit)
            {
                if (points is null)
                    return false;
                foreach (var (at, value) in points)
                {
                    if (lower is null || upper is null)
                        return false;
                    if (value >= lower && value <= upper)
                        crossings.Add((node, at, value, unit));
                }
                return true;
            }
        }

        /// <summary>Whether <paramref name="expression"/> holds a constant that is not real.</summary>
        private static bool OffTheRealLine(Entity expression)
        {
            foreach (var node in expression.Nodes)
                if (node is Complex { ImaginaryPart.IsZero: false })
                    return true;
            return false;
        }

        /// <summary>
        /// Whether the linear <paramref name="argument"/> takes a real value at a real <paramref name="x"/>, or
        /// <see langword="null"/> where it is not linear with numeric coefficients.
        /// </summary>
        private static bool? MeetsTheRealLine(Entity argument, Variable x)
            => TreeAnalyzer.TryGetPolyLinear(argument, x, out var a, out var b)
               && a.Evaled is Complex { RealPart.EDecimal.IsFinite: true, ImaginaryPart.EDecimal.IsFinite: true } slope
               && b.Evaled is Complex { RealPart.EDecimal.IsFinite: true, ImaginaryPart.EDecimal.IsFinite: true } intercept
                ? !slope.ImaginaryPart.IsZero || intercept.ImaginaryPart.IsZero
                : null;

        /// <summary>
        /// The real <paramref name="x"/> where the polynomial <paramref name="argument"/> crosses the imaginary axis
        /// beyond <c>±i</c> (<paramref name="imaginaryAxis"/>) or the negative real axis, or <see langword="null"/> where
        /// it is not a polynomial with numeric coefficients, its crossings cannot be listed, it runs along the axis, or it
        /// meets the end of the cut, which is a singularity of the integrand.
        /// </summary>
        private static List<(Entity At, Real Value)>? PolynomialCrossings(Entity argument, Variable x, bool imaginaryAxis)
        {
            if (!TreeAnalyzer.TryGetPolynomial(argument, x, out var coefficients))
                return null;
            // The part that is zero on the axis, and the one that decides whether a point of it is on the cut.
            Entity across = Integer.Zero, along = Integer.Zero;
            var acrossIsZero = true;
            foreach (var pair in coefficients)
            {
                var (power, coefficient) = (pair.Key, pair.Value);
                if (power.Sign < 0 || coefficient.Evaled is not Complex { RealPart.EDecimal.IsFinite: true, ImaginaryPart.EDecimal.IsFinite: true } value)
                    return null;
                var (acrossPart, alongPart) = imaginaryAxis ? (value.RealPart, value.ImaginaryPart) : (value.ImaginaryPart, value.RealPart);
                if (!acrossPart.IsZero && !power.IsZero)
                    acrossIsZero = false;
                // x^0 is written as 1, since 0^0 is undefined here and would take the zero at 0 out of the solution.
                Entity monomial = power.IsZero ? Integer.One : MathS.Pow(x, Integer.Create(power));
                if (!acrossPart.IsZero)
                    across += acrossPart * monomial;
                if (!alongPart.IsZero)
                    along += alongPart * monomial;
            }
            // Constant across the axis: off it everywhere, or along it everywhere.
            if (acrossIsZero)
                return across.Evaled is Real { IsZero: true } ? null : new();
            if (across.InnerSimplified.SolveEquation(x) is not FiniteSet roots)
                return null;
            var points = new List<(Entity At, Real Value)>();
            foreach (var root in roots)
            {
                if (root.ContainsNode(x) || root.Vars.Any())
                    return null;
                switch (root.Evaled)
                {
                    case Real { EDecimal.IsFinite: true } at:
                        if (along.Substitute(x, root).Evaled is not Real { EDecimal.IsFinite: true } there)
                            return null;
                        var onTheCut = imaginaryAxis ? there > 1 || there < -1 : there < 0;
                        var atItsEnd = imaginaryAxis ? there >= 1 && there <= 1 || there <= -1 && there >= -1 : there.IsZero;
                        if (atItsEnd)
                            return null;
                        if (onTheCut)
                            points.Add((root, at));
                        break;
                    case Complex { ImaginaryPart.IsZero: false }:
                        break;
                    default:
                        return null;
                }
            }
            return points;
        }

        /// <summary>
        /// The real <paramref name="x"/> in the range where the cosine (<paramref name="cosine"/>) or the sine of
        /// <c>a x + b</c>, with <c>a</c> and <c>b</c> complex numbers, crosses the negative real axis, or
        /// <see langword="null"/> where the argument is not of that shape, the range is not finite where it would have
        /// to be, or the path meets a zero.
        /// </summary>
        /// <remarks>
        /// With <c>p = Re(a x + b)</c> and <c>q = Im(a x + b)</c>, both linear in <c>x</c>,
        /// <c>cos(p + i q) = cos p cosh q - i sin p sinh q</c> is real where <c>sin p = 0</c> or <c>q = 0</c>: at the
        /// multiples of <c>pi</c>, negative at the odd ones, and at the one point where <c>q = 0</c>, negative where
        /// <c>cos p</c> is there. <c>sin(p + i q) = sin p cosh q + i cos p sinh q</c> is real where <c>cos p = 0</c> or
        /// <c>q = 0</c>: negative at <c>-pi/2 + 2 k pi</c>, and where <c>sin p</c> is at the point <c>q = 0</c>. A point
        /// where the value touches the axis without crossing it jumps by no multiple, which the caller reads.
        /// </remarks>
        private static List<(Entity At, Real Value)>? TrigonometricCrossings(Entity argument, Variable x, Real? lower, Real? upper, bool cosine)
        {
            if (lower is null || upper is null
                || !TreeAnalyzer.TryGetPolyLinear(argument, x, out var a, out var b)
                || a.Evaled is not Complex { RealPart.EDecimal.IsFinite: true, ImaginaryPart.EDecimal.IsFinite: true } slope
                || b.Evaled is not Complex { RealPart.EDecimal.IsFinite: true, ImaginaryPart.EDecimal.IsFinite: true } intercept
                || slope.RealPart.IsZero && slope.ImaginaryPart.IsZero
                || MathS.pi.Evaled is not Real pi)
                return null;
            var points = new List<(Entity At, Real Value)>();
            // Where q = 0, if q moves at all: the value there is the real cos p or sin p.
            if (!slope.ImaginaryPart.IsZero)
            {
                var at = (-(Entity)intercept.ImaginaryPart / slope.ImaginaryPart).InnerSimplified;
                if (at.Evaled is not Real { EDecimal.IsFinite: true } value)
                    return null;
                if (value >= lower && value <= upper)
                {
                    var p = (slope.RealPart * (Entity)at + intercept.RealPart).InnerSimplified;
                    if ((cosine ? MathS.Cos(p) : MathS.Sin(p)).EvalNumerical() is not { ImaginaryPart.IsZero: true, RealPart: var there })
                        return null;
                    // A zero on the path is a pole of the integrand's tan or cot.
                    if (there.EDecimal.Abs().CompareTo(EDecimal.Create(1, -30)) < 0)
                        return null;
                    if (there < 0)
                        points.Add((at, value));
                }
            }
            // Where sin p = 0, or cos p = 0, if p moves at all; if it does not, q does and the point above is all.
            if (!slope.RealPart.IsZero)
            {
                if (!lower.EDecimal.IsFinite || !upper.EDecimal.IsFinite)
                    return null;
                // p = c + 2 k pi, so k runs over the whole numbers between the ends mapped back.
                Entity first = cosine ? MathS.pi : -MathS.pi / 2;
                var shift = cosine ? pi : -pi / 2;
                var atLower = (slope.RealPart * lower + intercept.RealPart - shift) / (2 * pi);
                var atUpper = (slope.RealPart * upper + intercept.RealPart - shift) / (2 * pi);
                var (from, to) = atLower < atUpper ? (atLower, atUpper) : (atUpper, atLower);
                var k0 = from.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Floor)).ToEInteger();
                var k1 = to.EDecimal.RoundToIntegerNoRoundedFlag(EContext.CliDecimal.WithRounding(ERounding.Ceiling)).ToEInteger();
                if ((k1 - k0).CompareTo(EInteger.FromInt32(MostBreaks)) > 0)
                    return null;
                for (var k = k0; k.CompareTo(k1) <= 0; k += EInteger.One)
                {
                    var at = ((first + 2 * Integer.Create(k) * MathS.pi - intercept.RealPart) / slope.RealPart).InnerSimplified;
                    if (at.Evaled is not Real { EDecimal.IsFinite: true } value)
                        return null;
                    // The same point as the one where q = 0 is one crossing.
                    if (value >= lower && value <= upper && !points.Any(point => Coincide(point.Value, value)))
                        points.Add((at, value));
                }
            }
            else if ((cosine ? MathS.Sin(intercept.RealPart) : MathS.Cos(intercept.RealPart)).EvalNumerical() is not { RealPart: var constant }
                     || constant.EDecimal.Abs().CompareTo(EDecimal.Create(1, -30)) < 0)
                // p is constant at a point where the value is real all along the path.
                return null;
            return points;
        }

        /// <summary>
        /// Two decimal values of the same point, reached by different exact spellings.
        /// </summary>
        private static bool Coincide(Real a, Real b)
            => a.EDecimal.Subtract(b.EDecimal).Abs().CompareTo(EDecimal.Create(1, -30)) < 0;
    }
}
