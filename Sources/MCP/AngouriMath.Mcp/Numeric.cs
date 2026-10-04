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
/// Why not just use MathS.UnsafeAndInternal.AreEqualNumerically? Because despite the name it
/// compares with EXACT equality — the implementation is `if (evaled1 != evaled2 ...) return
/// false` over arbitrary-precision decimals, with no tolerance at all. Any expression
/// involving a transcendental therefore fails: ln(x) computed via two different but
/// mathematically identical routes disagrees in the last digit and the check reports the
/// expressions as different.
///
/// That is not a hypothetical. `∫ x*ln(x)` returns the correct antiderivative, which
/// differentiates back to the correct integrand, and AreEqualNumerically still says false.
/// So this compares with a relative tolerance instead.
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
    /// True / false / null, where null means "could not be evaluated at any sample point"
    /// — which is not evidence either way and must not be reported as inequality.
    /// </summary>
    public static bool? Equal(Entity left, Entity right, Entity[]? points = null)
    {
        points ??= PositivePoints;

        // `provided` guards are correct mathematics but do not evaluate to a number, so
        // they are dropped for the comparison only.
        var a = Strip(left);
        var b = Strip(right);

        var variables = a.Vars.Concat(b.Vars)
            .Select(v => v.Name)
            .Distinct()
            .ToArray();

        var usable = 0;

        // Rotating the offset gives each variable a different value on each pass, so
        // f(x, y) = x*y is not mistaken for f(x, y) = x^2.
        for (var offset = 0; offset < points.Length; offset++)
        {
            var subA = a;
            var subB = b;
            for (var i = 0; i < variables.Length; i++)
            {
                var point = points[(i + offset) % points.Length];
                var variable = MathS.Var(variables[i]);
                subA = subA.Substitute(variable, point);
                subB = subB.Substitute(variable, point);
            }

            var valueA = AsDouble(subA);
            var valueB = AsDouble(subB);

            // A point where either side is undefined tells us nothing; skip it rather than
            // counting it as disagreement.
            if (valueA is null || valueB is null) continue;

            var (realA, imagA) = valueA.Value;
            var (realB, imagB) = valueB.Value;

            var separation = Math.Sqrt(
                (realA - realB) * (realA - realB) + (imagA - imagB) * (imagA - imagB));
            var scale = Math.Max(
                1.0,
                Math.Max(Math.Sqrt(realA * realA + imagA * imagA),
                         Math.Sqrt(realB * realB + imagB * imagB)));

            if (separation / scale > RelativeTolerance) return false;
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

            var valueA = AsDouble(a.Substitute(variable, point));
            var valueB = AsDouble(b.Substitute(variable, point));

            // A point where either side is undefined says nothing about agreement, so it is
            // counted and skipped rather than scored as a mismatch.
            if (valueA is null || valueB is null) { skipped++; continue; }

            // ln and even roots return principal-branch complex values on the negatives
            // rather than failing. Comparing their magnitudes is defensible, but a caller
            // asking about a physical quantity needs to know the reference left the reals.
            if (Math.Abs(valueA.Value.Imaginary) > 1e-12 ||
                Math.Abs(valueB.Value.Imaginary) > 1e-12) complex++;

            var difference = Math.Sqrt(
                Math.Pow(valueA.Value.Real - valueB.Value.Real, 2) +
                Math.Pow(valueA.Value.Imaginary - valueB.Value.Imaginary, 2));

            var magnitude = Math.Sqrt(
                valueA.Value.Real * valueA.Value.Real +
                valueA.Value.Imaginary * valueA.Value.Imaginary);

            if (difference > maxAbsolute)
            {
                maxAbsolute = difference;
                maxAbsoluteAt = at.ToString("R");
            }

            // Relative error is meaningless where the reference is ~0, so it is scored only
            // where there is something to be relative to.
            if (magnitude > 1e-12)
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

    // Step counts for the two runs. AngouriMath's rule is first order — measured, by
    // doubling: the error of `∫[0,1] e^(x^2)` halves at every doubling of the step count
    // rather than falling by four — so the gap between a run and its double IS the error of
    // the coarser one, to a constant. Two runs at these counts cost about a third of a
    // second together, which is inside the request budget; going further buys one digit per
    // quadrupling and is not worth the wall clock.
    private const int CoarseSteps = 2_000;
    private const int FineSteps = 4_000;

    /// <summary>
    /// Numeric definite integration via `Entity.DefiniteIntegral`, run TWICE so that the
    /// answer arrives with an error estimate rather than as a hundred digits of false
    /// precision. https://github.com/asc-community/AngouriMath/issues/1675
    ///
    /// That precision is the whole reason this is not a one-line call. The library returns
    /// an arbitrary-precision decimal whatever the step count, so `∫[0,1] e^(x^2)` prints as
    /// 1.4666222366253148581140206684127892598 when the true value is 1.46265174590718 — a
    /// figure that is wrong in its third decimal and reads as converged to thirty. Reporting
    /// only the digits two runs agree on is exactly the job this server exists to do.
    ///
    /// Returns null when the integrand cannot be sampled: the rule evaluates at the
    /// endpoints, so a singularity at either — `∫[0,1] sin(x)/x`, `∫[0,1] ln(x)`, both of
    /// which converge — comes back NaN and must be declined rather than reported.
    /// </summary>
    public static Quadrature? TryDefiniteIntegral(Entity integrand, Variable variable,
        double from, double to)
    {
        var coarse = Sample(integrand, variable, from, to, CoarseSteps);
        var fine = Sample(integrand, variable, from, to, FineSteps);
        if (coarse is not { } c || fine is not { } f) return null;

        var error = Math.Abs(f - c);

        // Digits justified by the error, relative to the magnitude of the answer: an error
        // of 2e-4 on a value near 1.46 pins three decimals, and the fourth is noise. One
        // digit is dropped on purpose — the estimate is itself an estimate.
        var scale = Math.Max(Math.Abs(f), double.Epsilon);
        var digits = error <= 0
            ? 15
            : (int)Math.Floor(-Math.Log10(error / scale)) - 1;
        digits = Math.Clamp(digits, 0, 15);

        return new Quadrature(f, error, digits);
    }

    private static double? Sample(Entity integrand, Variable variable,
        double from, double to, int steps)
    {
        try
        {
            var value = integrand.DefiniteIntegral(variable, from, to, steps);
            var real = value.RealPart.EDecimal.ToDouble();
            var imaginary = value.ImaginaryPart.EDecimal.ToDouble();
            if (double.IsNaN(real) || double.IsInfinity(real)) return null;
            // A real integrand integrated over a real interval that comes back complex means
            // the rule wandered off the domain; that is not an answer to report.
            if (double.IsNaN(imaginary) || Math.Abs(imaginary) > 1e-12) return null;
            return real;
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
