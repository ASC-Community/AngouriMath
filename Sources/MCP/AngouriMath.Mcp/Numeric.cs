//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

/// <summary>
/// Tolerance-based numeric equality.
///
/// Why not MathS.UnsafeAndInternal.AreEqualNumerically? It compares to half the evaluation's
/// digits since #1674, but at its own check points, negative and complex among them, where a
/// correct antiderivative with ln(x) or abs(x) in it can disagree with its integrand on a branch,
/// and it answers only true or false. This samples positive reals and answers null where no
/// point can be evaluated, which is not evidence either way.
/// https://github.com/asc-community/AngouriMath/issues/1674
/// </summary>
internal static class Numeric
{
    /// <summary>Deliberately positive reals: correct antiderivatives contain ln(x) and
    /// abs(x), which are undefined or non-holomorphic on the negatives.</summary>
    public static readonly Entity[] PositivePoints =
        ["0.37", "1.31", "2.17", "0.59", "3.43"];

    private const double RelativeTolerance = 1e-6;

    /// <summary>
    /// What is left of an exact zero after evaluation: half the evaluator's digits, the margin
    /// <c>MathS.UnsafeAndInternal.AreEqualNumerically</c> keeps since #1674. It is the only
    /// absolute part of a tolerance. A value of 1e-9 is a value, and it is as far from
    /// 2e-9 as 1 is from 2.
    /// https://github.com/asc-community/AngouriMath/issues/1674
    /// </summary>
    private static double AbsoluteTolerance =>
        Math.Pow(10, -(double)MathS.Settings.DecimalPrecisionContext.Value.Precision.ToInt32Checked() / 2);

    /// <summary>
    /// True / false / null, where null means "could not be evaluated at any sample point"
    /// — which is not evidence either way and must not be reported as inequality.
    /// </summary>
    public static bool? Equal(Entity left, Entity right, Entity[]? points = null) =>
        Vanishes(null, left, right, points);

    /// <summary>
    /// As <see cref="Equal"/>, for a <paramref name="difference"/> of <paramref name="left"/>
    /// and <paramref name="right"/> that has already been simplified. The difference is what is
    /// evaluated, since Simplify has collapsed any derivative(…) or integral(…) node in it
    /// that leaves a side without a value, and the sides give it a scale. Compared against 0
    /// it has none, and an absolute tolerance read every small difference as zero.
    /// </summary>
    public static bool? DifferenceVanishes(Entity difference, Entity left, Entity right,
        Entity[]? points = null) =>
        Vanishes(difference, left, right, points);

    private static bool? Vanishes(Entity? difference, Entity left, Entity right, Entity[]? points)
    {
        points ??= PositivePoints;

        // `provided` guards are correct mathematics but do not evaluate to a number, so
        // they are dropped for the comparison only.
        var a = Strip(left);
        var b = Strip(right);
        var d = difference is null ? null : Strip(difference);
        Entity? simplifiedA = null, simplifiedB = null;

        var variables = a.Vars.Concat(b.Vars).Concat(d?.Vars ?? [])
            .Select(v => v.Name)
            .Distinct()
            .ToArray();

        var usable = 0;

        // Rotating the offset gives each variable a different value on each pass, so
        // f(x, y) = x*y is not mistaken for f(x, y) = x^2.
        for (var offset = 0; offset < points.Length; offset++)
        {
            Entity At(Entity e)
            {
                for (var i = 0; i < variables.Length; i++)
                    e = e.Substitute(MathS.Var(variables[i]), points[(i + offset) % points.Length]);
                return e;
            }

            var valueA = Evaluate(At(a));
            var valueB = Evaluate(At(b));

            Number.Complex gap;
            if (d is null)
            {
                // A point where either side is undefined tells us nothing; skip it rather
                // than counting it as disagreement.
                if (valueA is null || valueB is null) continue;
                gap = valueA - valueB;
            }
            else
            {
                if (Evaluate(At(d)) is not { } evaluated) continue;
                gap = evaluated;
                // A side holding an unevaluated derivative(…) has its value once simplified.
                valueA ??= Evaluate(At(simplifiedA ??= a.Simplify()));
                valueB ??= Evaluate(At(simplifiedB ??= b.Simplify()));
                if (valueA is null && valueB is null) continue;
            }

            // A value past the range of a double rounds to infinity, and an infinite scale
            // would make any gap fit inside it. Such a point is skipped, as it was before.
            var scale = Math.Max(Magnitude(valueA), Magnitude(valueB));
            var separation = Magnitude(gap);
            if (double.IsInfinity(scale) || double.IsInfinity(separation)) continue;
            if (separation > RelativeTolerance * scale + AbsoluteTolerance) return false;
            usable++;
        }

        return usable == 0 ? null : true;
    }

    public sealed record Divergence(
        double MaxAbsolute, string MaxAbsoluteAt,
        double MaxRelative, string MaxRelativeAt,
        double RootMeanSquare, int Sampled, int Skipped, int Complex);

    /// <summary>
    /// How far apart are two expressions across an interval?
    ///
    /// A different question from <see cref="Equal"/>, and the one engineering actually asks:
    /// not "is this approximation exact" — it is not — but "does it hold to 1e-6 across the
    /// operating range, and where is it worst".
    /// </summary>
    public static Divergence? Diverge(
        Entity left, Entity right, Variable variable, double from, double to, int samples)
    {
        var a = Strip(left);
        var b = Strip(right);

        var maxAbsolute = -1.0; var maxAbsoluteAt = "";
        var maxRelative = -1.0; var maxRelativeAt = "";
        var sumOfSquares = 0.0;
        int used = 0, skipped = 0, complex = 0;

        var step = samples > 1 ? (to - from) / (samples - 1) : 0.0;

        for (var i = 0; i < samples; i++)
        {
            var at = samples > 1 ? from + step * i : from;
            Entity point = at.ToString("R");

            var valueA = Evaluate(a.Substitute(variable, point));
            var valueB = Evaluate(b.Substitute(variable, point));

            // A point where either side is undefined says nothing about agreement, so it is
            // counted and skipped rather than scored as a mismatch.
            if (valueA is null || valueB is null) { skipped++; continue; }

            // ln and even roots return principal-branch complex values on the negatives
            // rather than failing. Comparing their magnitudes is defensible, but a caller
            // asking about a physical quantity needs to know the reference left the reals.
            if (Math.Abs(valueA.ImaginaryPart.EDecimal.ToDouble()) > 1e-12 ||
                Math.Abs(valueB.ImaginaryPart.EDecimal.ToDouble()) > 1e-12) complex++;

            // Subtracted at the evaluator's precision and rounded after: two values within
            // 1e-16 of each other are the same double, and their difference was 0.
            var difference = Magnitude(valueA - valueB);
            var magnitude = Magnitude(valueA);
            if (double.IsInfinity(difference) || double.IsInfinity(magnitude) ||
                double.IsInfinity(Magnitude(valueB))) { skipped++; continue; }

            if (difference > maxAbsolute)
            {
                maxAbsolute = difference;
                maxAbsoluteAt = at.ToString("R");
            }

            // Relative error is undefined where the reference is 0, and only there: a
            // reference of 1e-13 is a value, and an approximation of 0 is wrong by all of it.
            if (magnitude > AbsoluteTolerance)
            {
                var relative = difference / magnitude;
                if (relative > maxRelative)
                {
                    maxRelative = relative;
                    maxRelativeAt = at.ToString("R");
                }
            }

            sumOfSquares += difference * difference;
            used++;
        }

        if (used == 0) return null;

        return new Divergence(
            maxAbsolute, maxAbsoluteAt,
            maxRelative < 0 ? 0 : maxRelative, maxRelativeAt,
            Math.Sqrt(sumOfSquares / used), used, skipped, complex);
    }

    public static Entity Strip(Entity e) =>
        e.Replace(node => node is Providedf provided ? provided.Expression : node);

    /// <summary>A quadrature result: the value, and how far it can be trusted.</summary>
    public readonly record struct Quadrature(double Value, double ErrorEstimate, int Digits);

    // Panels for the library's rule: five evaluations each, on these, twice as many and four times
    // as many, which is seven thousand evaluations of a compiled function.
    private const int Panels = 200;

    /// <summary>
    /// Numeric definite integration via `Entity.DefiniteIntegral`, with the digits it reports. The
    /// library's rule is five-point Gauss-Legendre, run on its panels, twice as many and four times
    /// as many, and its value is rounded to the place of the error that leaves, so the digits of the
    /// value it returns are the digits it knows: `∫[0,1] ln(x)` is -1.000000, six of them.
    /// https://github.com/asc-community/AngouriMath/issues/1675
    ///
    /// Returns null where there is nothing to report: a NaN, which is how the library answers an
    /// integral that diverges at a bound, or a complex value for a real integrand over a real
    /// interval, which means the rule left the domain.
    /// </summary>
    public static Quadrature? TryDefiniteIntegral(Entity integrand, Variable variable,
        double from, double to)
    {
        try
        {
            var value = integrand.DefiniteIntegral(variable, from, to, Panels);
            var real = value.RealPart.EDecimal;
            var imaginary = value.ImaginaryPart.EDecimal.ToDouble();
            if (real.IsNaN() || real.IsInfinity()) return null;
            if (double.IsNaN(imaginary) || Math.Abs(imaginary) > 1e-12) return null;
            // A value the library rounded to zero has no significant digit to count, and was
            // rounded at the fifteenth of the integral of the modulus at most.
            var digits = real.IsZero ? 15 : Math.Min(15, real.Mantissa.Abs().ToString().Length);
            var error = 0.5 * Math.Pow(10, real.Exponent.ToInt32Checked());
            return new Quadrature(real.ToDouble(), error, digits);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Real part as a double, or null when it is not numerically evaluable.</summary>
    public static double? AsNumber(Entity e) => AsDouble(e)?.Real;

    /// <summary>
    /// As <see cref="AsNumber"/>, but null rather than the real part when the value is
    /// complex. An integration bound of `i` must not quietly become `0`.
    /// </summary>
    public static double? AsRealNumber(Entity e) =>
        AsDouble(e) is { } value && Math.Abs(value.Imaginary) < 1e-12 ? value.Real : null;

    /// <summary>Is this expression structurally zero once domain guards are removed?</summary>
    public static bool IsZero(Entity e)
    {
        var bare = Strip(e).InnerSimplified;
        return bare.Stringize() is "0" or "0.0" or "-0";
    }

    /// <summary>The evaluator's value, unrounded, or null where there is no finite one.</summary>
    private static Number.Complex? Evaluate(Entity e)
    {
        if (!e.EvaluableNumerical) return null;

        try
        {
            var value = e.EvalNumerical();
            var real = value.RealPart.EDecimal;
            var imaginary = value.ImaginaryPart.EDecimal;
            if (real.IsNaN() || imaginary.IsNaN() || real.IsInfinity() || imaginary.IsInfinity())
                return null;
            return value;
        }
        catch
        {
            return null;
        }
    }

    private static double Magnitude(Number.Complex? value) =>
        value is null ? 0 : Hypotenuse(value.RealPart.EDecimal.ToDouble(), value.ImaginaryPart.EDecimal.ToDouble());

    // Scaled, so that a value past 1e154 does not square to infinity and make every tolerance
    // infinite with it.
    private static double Hypotenuse(double x, double y)
    {
        x = Math.Abs(x); y = Math.Abs(y);
        var larger = Math.Max(x, y);
        if (larger == 0 || double.IsInfinity(larger)) return larger;
        var ratio = Math.Min(x, y) / larger;
        return larger * Math.Sqrt(1 + ratio * ratio);
    }

    private static (double Real, double Imaginary)? AsDouble(Entity e)
    {
        if (!e.EvaluableNumerical) return null;

        try
        {
            var value = e.EvalNumerical();
            var real = value.RealPart.EDecimal.ToDouble();
            var imaginary = value.ImaginaryPart.EDecimal.ToDouble();

            if (double.IsNaN(real) || double.IsNaN(imaginary)) return null;
            if (double.IsInfinity(real) || double.IsInfinity(imaginary)) return null;

            return (real, imaginary);
        }
        catch
        {
            return null;
        }
    }
}
