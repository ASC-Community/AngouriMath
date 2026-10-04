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
/// Guesses which field a formula comes from, by reading its symbols.
///
/// This is inference from naming convention, not mathematics. `c` means the speed of light
/// in relativity and a concentration in chemistry; `T` is temperature or a period. The tool
/// says what it noticed and how strongly, and the caller decides. It is deliberately
/// unwilling to claim certainty — a wrong confident label is worse than no label.
/// </summary>
internal static class Classify
{
    private sealed record Field(
        string Name,
        string[] Strong,     // symbols that are near-diagnostic of the field
        string[] Weak,       // symbols consistent with it but common elsewhere
        string[] Structure); // "derivative", "integral", "trig", "complex", ...

    private static readonly Field[] Fields =
    [
        new("special relativity", ["c"], ["m", "E", "v", "p", "t"], []),
        new("Newtonian gravitation", ["G"], ["M", "m", "r", "F"], []),
        new("classical mechanics", ["F", "a"], ["m", "v", "t", "x", "p", "g", "k"], []),
        new("quantum mechanics", ["hbar", "h", "psi"], ["E", "nu", "lambda", "p", "m"],
            ["complex"]),
        new("electromagnetism / circuits", ["V", "I", "Z", "L", "C", "R"],
            ["Q", "omega", "f", "t"], ["complex"]),
        new("thermodynamics", ["S", "T"], ["P", "V", "n", "R", "k", "Q", "U"], []),
        new("fluid dynamics", ["rho", "Re", "mu"], ["v", "p", "d", "A"], []),
        new("statistics / probability", ["sigma", "mu"], ["n", "p", "x", "N"], []),
        new("chemistry / kinetics", ["K", "Ea"], ["n", "c", "V", "T", "R"], ["log"]),
        new("finance", ["PV", "FV", "APR"], ["P", "r", "n", "t", "i"], []),
        new("signal processing", ["omega", "j"], ["t", "f", "T", "A"], ["complex", "trig"]),
        new("geometry / mensuration", ["pi"], ["r", "h", "a", "b", "A", "V"], ["trig"]),
        new("number theory", [], ["n", "k", "p", "m"], ["factorial", "integers-only"]),
        new("analysis / calculus", [], ["x", "y", "t"], ["derivative", "integral", "limit"]),
        new("optics", ["n1", "n2"], ["theta", "n", "d", "lambda"], ["trig"]),
    ];

    /// <summary>
    /// Shapes distinctive enough to name outright. Each also names the field it implies:
    /// a recognised form is much stronger evidence than loose symbol overlap, and without
    /// this `n*R*T/V` ranked as circuits — R and V are strong circuit markers — while the
    /// signature was correctly calling it the ideal gas law.
    /// </summary>
    private static readonly (string Fragment, string Says, string Field)[] Signatures =
    [
        ("m * c ^ 2", "mass-energy equivalence (E = mc^2)", "special relativity"),
        ("c ^ 2 * m", "mass-energy equivalence (E = mc^2)", "special relativity"),
        ("v ^ 2 / c ^ 2", "Lorentz factor — special relativity", "special relativity"),
        ("G * M", "Newtonian gravitation", "Newtonian gravitation"),
        ("h * nu", "photon energy (E = h*nu)", "quantum mechanics"),
        ("sqrt(L * C)", "LC resonant frequency", "electromagnetism / circuits"),
        ("2 * pi * sqrt", "a resonant period or LC/pendulum frequency", ""),
        ("1 / 2 * m * v ^ 2", "kinetic energy", "classical mechanics"),
        ("n * R * T", "ideal gas law", "thermodynamics"),
        ("R * T * n", "ideal gas law", "thermodynamics"),
        ("sin(x) / x", "the sinc function — diffraction and sampling", "signal processing"),
    ];

    public sealed record Guess(string Field, int Score, string Because);

    public static (List<Guess> Guesses, List<string> Signatures, List<string> Features)
        Analyse(Entity e)
    {
        var symbols = e.Vars.Select(v => v.Stringize()).ToHashSet(StringComparer.Ordinal);
        var features = Features(e);
        var text = e.Stringize();

        var matched = Signatures
            .Where(s => text.Contains(s.Fragment, StringComparison.Ordinal))
            .ToList();

        var signatures = matched.Select(s => s.Says).Distinct().ToList();
        var implied = matched
            .Where(s => s.Field.Length > 0)
            .Select(s => s.Field)
            .ToHashSet(StringComparer.Ordinal);

        var guesses = new List<Guess>();
        foreach (var field in Fields)
        {
            var strong = field.Strong.Where(symbols.Contains).ToArray();
            var weak = field.Weak.Where(symbols.Contains).ToArray();
            var structural = field.Structure.Where(features.Contains).ToArray();

            var recognised = implied.Contains(field.Name);
            var score = strong.Length * 3 + weak.Length + structural.Length * 2
                        + (recognised ? 10 : 0);
            if (score == 0) continue;

            // A field claimed on common symbols alone is barely evidence.
            if (!recognised && strong.Length == 0 && structural.Length == 0 && weak.Length < 2)
                continue;

            var reasons = new List<string>();
            if (recognised) reasons.Add("matches a known formula shape");
            if (strong.Length > 0) reasons.Add($"distinctive symbols {string.Join(", ", strong)}");
            if (weak.Length > 0) reasons.Add($"also uses {string.Join(", ", weak)}");
            if (structural.Length > 0) reasons.Add($"structure: {string.Join(", ", structural)}");

            guesses.Add(new Guess(field.Name, score, string.Join("; ", reasons)));
        }

        return (guesses.OrderByDescending(g => g.Score).Take(4).ToList(), signatures, features);
    }

    private static List<string> Features(Entity e)
    {
        var features = new List<string>();
        var numeric = true;

        foreach (var node in e.Nodes)
        {
            switch (node)
            {
                case Derivativef: features.Add("derivative"); break;
                case Integralf: features.Add("integral"); break;
                case Limitf: features.Add("limit"); break;
                case Sinf or Cosf or Tanf or Cotanf or Secantf or Cosecantf:
                    features.Add("trig"); break;
                case Logf: features.Add("log"); break;
                case Factorialf: features.Add("factorial"); break;
                case Number.Complex complex when !complex.ImaginaryPart.EDecimal.IsZero:
                    features.Add("complex"); break;
            }

            if (node is Variable) numeric = false;
        }

        if (numeric && e.Vars.Count == 0) features.Add("integers-only");
        return features.Distinct().ToList();
    }
}
