//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

/// <summary>
/// Answers two questions the raw library does not: "what should I watch out for in this
/// expression?" and "is this answer real, or merely correct?".
///
/// AngouriMath has no step engine — there is no derivation output anywhere in the library —
/// so the honest substitute is to check a chain of working the caller supplies, and say
/// which step broke. See <see cref="CheckSteps"/>.
/// </summary>
internal static class Analysis
{
    // Spread across the real line, including negatives and a point near zero, because the
    // whole point here is to FIND the places an expression stops being real or defined.
    private static readonly Entity[] Probes =
        ["-2.7", "-1.3", "-0.4", "0.6", "1.9", "3.2"];

    public sealed record Caution(string Where, string Risk);

    /// <summary>
    /// Structural hazards, found by walking the tree. These are the classic ways an
    /// expression means less than it appears to.
    /// </summary>
    public static List<Caution> Hazards(Entity e)
    {
        var found = new List<Caution>();

        foreach (var node in e.Nodes)
        {
            switch (node)
            {
                case Divf:
                    var divisor = node.DirectChildren.Count > 1
                        ? node.DirectChildren[1].Stringize()
                        : "?";
                    found.Add(new Caution(node.Stringize(),
                        $"division: undefined where {divisor} = 0. Any cancellation of this " +
                        "term silently widens the domain unless a `provided` guard survives."));
                    break;

                case Logf:
                    found.Add(new Caution(node.Stringize(),
                        "logarithm: real only for a positive argument. Combining " +
                        "ln(a)+ln(b) into ln(a*b) widens the domain and is a documented " +
                        "source of extraneous roots."));
                    break;

                case Powf pow when pow.Exponent is not Number.Integer:
                    found.Add(new Caution(node.Stringize(),
                        "fractional or symbolic power: the principal branch is taken, so " +
                        "(-8)^(1/3) is a complex number, not -2. sqrt(x^2) is abs(x), not x."));
                    break;

                case Arcsinf or Arccosf:
                    found.Add(new Caution(node.Stringize(),
                        "inverse trig: real only for an argument in [-1, 1], and the " +
                        "principal branch is returned — the full solution family is periodic."));
                    break;

                case Factorialf:
                    found.Add(new Caution(node.Stringize(),
                        "factorial: defined via the gamma function, so it returns values for " +
                        "non-integers and poles at the negative integers."));
                    break;

                case Absf:
                    found.Add(new Caution(node.Stringize(),
                        "absolute value: not differentiable at 0, and it hides a sign change " +
                        "that matters when solving."));
                    break;
            }
        }

        return found;
    }

    /// <summary>Domain guards AngouriMath itself worked out, pulled out of the tree.</summary>
    public static List<string> Conditions(Entity simplified) =>
        simplified.Nodes
            .OfType<Providedf>()
            .Select(p => p.Predicate.Stringize())
            .Distinct()
            .ToList();

    public sealed record RealityReport(
        List<string> ComplexAt, List<string> UndefinedAt, int Sampled);

    /// <summary>
    /// Where does this expression stop being a real number? A result can be perfectly
    /// correct and still be useless as a length, a resistance or a time.
    /// </summary>
    public static RealityReport Reality(Entity e)
    {
        var complexAt = new List<string>();
        var undefinedAt = new List<string>();
        var variables = e.Vars.ToArray();
        var sampled = 0;

        // With no free variables there is a single value to inspect.
        if (variables.Length == 0)
        {
            Inspect(e, "(constant)");
            return new RealityReport(complexAt, undefinedAt, 1);
        }

        foreach (var probe in Probes)
        {
            var substituted = e;
            foreach (var variable in variables)
                substituted = substituted.Substitute(variable, probe);

            sampled++;
            Inspect(substituted, string.Join(" = ", variables.Select(v => v.Stringize())) +
                                 " = " + probe.Stringize());
        }

        return new RealityReport(complexAt, undefinedAt, sampled);

        void Inspect(Entity at, string label)
        {
            try
            {
                if (!at.EvaluableNumerical) { undefinedAt.Add(label); return; }

                var value = at.EvalNumerical();
                var real = value.RealPart.EDecimal.ToDouble();
                var imaginary = value.ImaginaryPart.EDecimal.ToDouble();

                if (double.IsNaN(real) || double.IsNaN(imaginary)) undefinedAt.Add(label);
                else if (Math.Abs(imaginary) > 1e-12) complexAt.Add(label);
            }
            catch
            {
                undefinedAt.Add(label);
            }
        }
    }

    /// <summary>Force an expression to be real-valued: anything complex becomes NaN.</summary>
    public static Entity AsReal(Entity e) =>
        e.Replace(node => node.WithCodomain(Domain.Real));

    public sealed record StepCheck(
        int Index, string From, string To, bool? Equal, string Method, string? Difference);

    /// <summary>
    /// Check a chain of working, one step at a time, and report the first step that changes
    /// the value.
    ///
    /// This is the useful form of "show me the steps". The library cannot produce a
    /// derivation — but a model is perfectly good at PROPOSING one and unreliable at
    /// EXECUTING it, so having it write the steps and having this check each transition
    /// puts each side on the task it is actually good at. A wrong step is located, not just
    /// detected.
    /// </summary>
    public static List<StepCheck> CheckSteps(List<Entity> steps)
    {
        var checks = new List<StepCheck>();

        for (var i = 1; i < steps.Count; i++)
        {
            var from = steps[i - 1];
            var to = steps[i];

            Entity difference;
            try { difference = (from - to).Simplify(); }
            catch { difference = from - to; }

            if (Numeric.IsZero(difference))
            {
                checks.Add(new StepCheck(i, from.Stringize(), to.Stringize(),
                    true, "exact", null));
                continue;
            }

            var numerically = Numeric.Equal(difference, 0);
            checks.Add(new StepCheck(
                i, from.Stringize(), to.Stringize(),
                numerically,
                numerically is null ? "inconclusive" : "numeric",
                numerically == true ? null : difference.Stringize()));
        }

        return checks;
    }
}
