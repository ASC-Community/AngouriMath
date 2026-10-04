//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

/// <summary>
/// `amcli mcp --selftest`: does this install work, and does the library still behave the way
/// this server's documents say it does?
///
/// Two categories, and the distinction matters. Identities must hold; if one fails, the install
/// or the library is broken and the exit code says so. Documented defects are only reported: one
/// that stops reproducing is good news rather than a failure, but it means the documents describe
/// a defect the library no longer has. The tests in McpUnitTests fail on either, so a change to
/// the library that fixes one fails the build here until the documents say so too.
/// </summary>
internal static class SelfTest
{
    private static bool IsZero(string expression)
    {
        var outcome = Parsing.Parse(expression);
        return outcome.Entity is not null && Numeric.IsZero(outcome.Entity.Simplify());
    }

    private static string Value(string expression)
    {
        var outcome = Parsing.Parse(expression);
        return outcome.Entity is null ? "<parse error>" : outcome.Entity.Evaled.Stringize();
    }

    private static readonly (string Name, Func<bool> Holds)[] Identities =
    [
        ("Euler's identity is exactly zero", () => Value("e^(i*pi) + 1") == "0"),
        ("Machin: 4*atan(1/5) - atan(1/239) = pi/4",
            () => IsZero("4*arctan(1/5) - arctan(1/239) - pi/4")),
        ("golden ratio = 2*cos(pi/5)", () => IsZero("(1+sqrt(5))/2 - 2*cos(pi/5)")),
        ("taxicab 1729 two ways", () => IsZero("(1^3 + 12^3) - (9^3 + 10^3)")),
        ("42 as a sum of three cubes",
            () => Value("(-80538738812075974)^3 + 80435758145817515^3 + 12602123297335631^3") == "42"),
        ("Simpsons' Fermat near-miss is NOT equal",
            () => Value("3987^12 + 4365^12 - 4472^12") != "0"),
        ("six by nine is 42 in base 13", () => MathS.ToBaseN(54, 13) == "42"),
        ("42 is 101010 in binary", () => MathS.ToBaseN(42, 2) == "101010"),
        ("42 is the 5th Catalan number", () => Value("10! / (6! * 5!)") == "42"),
        ("Pythagoras: sin^2 + cos^2 = 1", () => IsZero("sin(x)^2 + cos(x)^2 - 1")),
        ("22/7 - pi is the integral of x^4*(1-x)^4/(1+x^2) over [0, 1]", () =>
        {
            // Kept as an identity so the proof the curiosities resource walks through cannot
            // silently stop working.
            var outcome = Parsing.Parse("x^4*(1-x)^4/(1+x^2)");
            if (outcome.Entity is null) return false;
            var x = MathS.Var("x");
            var antiderivative = outcome.Entity.Integrate(x);
            var value = antiderivative.Substitute(x, 1) - antiderivative.Substitute(x, 0);
            return Numeric.IsZero((value - (MathS.FromString("22/7") - MathS.pi)).Simplify());
        }),
        ("d/dx integral of x*ln(x) returns the integrand", () =>
        {
            var outcome = Parsing.Parse("x*ln(x)");
            if (outcome.Entity is null) return false;
            var x = MathS.Var("x");
            var back = outcome.Entity.Integrate(x).Simplify().Differentiate(x);
            return Numeric.Equal(back, outcome.Entity) == true;
        }),
    ];

    private static readonly (string Name, Func<bool> StillBroken, string Documented)[] Defects =
    [
        ("Simplify(sqrt(x^2))", () => Value("sqrt(x^2)") != "abs(x)",
            "left as written rather than reduced to abs(x)"),
        ("MathS.Equations does not solve a system written as equalities", () =>
        {
            // x + y = 3 and x - y = 1 meet at (2, 1), which the same system written in '= 0'
            // form is solved to. A check for an exception alone would read a system that comes
            // back with no solution as fixed. https://github.com/asc-community/AngouriMath/issues/1673
            try
            {
                var solutions = MathS.Equations("x + y = 3".ToEntity(), "x - y = 1".ToEntity()).Solve("x", "y");
                return solutions is null || solutions.RowCount != 1
                    || solutions[0, 0].Evaled != 2 || solutions[0, 1].Evaled != 1;
            }
            catch (Exception)
            {
                return true;
            }
        }, "x + y = 3, x - y = 1 has no solution when written as equalities; each equation must be given in '= 0' form"),
        ("DefiniteIntegral is a first-order rule", () =>
        {
            // Halving the step count roughly doubles the error iff the rule is first order.
            // https://github.com/asc-community/AngouriMath/issues/1675
            // Numeric.TryDefiniteIntegral's step counts, its error estimate and the caveat
            // am_integrate prints all assume that; a rule of higher order would make the
            // reported error bound far too pessimistic and the wording wrong.
            var e = "e^(x^2)".ToEntity();
            var v = MathS.Var("x");
            var coarse = e.DefiniteIntegral(v, 0, 1, 500).RealPart.EDecimal.ToDouble();
            var fine = e.DefiniteIntegral(v, 0, 1, 1000).RealPart.EDecimal.ToDouble();
            var finer = e.DefiniteIntegral(v, 0, 1, 2000).RealPart.EDecimal.ToDouble();
            var ratio = Math.Abs(fine - coarse) / Math.Abs(finer - fine);
            return ratio is > 1.7 and < 2.3;
        }, "error halves per doubling, so ~4 digits at 4000 steps; Simpson would give more"),
    ];

    /// <summary>Runs every check, writes what it found, and returns the exit code: 1 when an identity fails.</summary>
    public static int Run(TextWriter output) => Check(output).Failed > 0 ? 1 : 0;

    /// <summary>Runs every check and writes what it found.</summary>
    /// <returns>How many identities failed, and how many documented defects no longer reproduce.</returns>
    internal static (int Failed, int Drifted) Check(TextWriter output)
    {
        output.WriteLine("amcli mcp self-test");
        output.WriteLine();
        output.WriteLine("Identities (these must hold):");

        var failed = 0;
        foreach (var (name, holds) in Identities)
        {
            // Through Guard rather than directly: a check that overflowed the stack would take
            // the self-test down with it, and a diagnostic that dies on what it is diagnosing
            // reports nothing.
            var outcome = Guard.Run(holds, timeoutMs: 30_000);

            if (!outcome.Ok)
            {
                output.WriteLine($"  FAIL {name} — {outcome.Status}: {outcome.Error}");
                failed++;
                continue;
            }

            output.WriteLine($"  {(outcome.Value ? "ok  " : "FAIL")} {name}");
            if (!outcome.Value) failed++;
        }

        output.WriteLine();
        output.WriteLine("Documented defects (a fix here means the documents need updating):");

        var drifted = 0;
        foreach (var (name, stillBroken, documented) in Defects)
        {
            var outcome = Guard.Run(stillBroken, timeoutMs: 30_000);
            if (!outcome.Ok)
            {
                output.WriteLine($"  ?      {name} — could not check ({outcome.Status})");
                continue;
            }

            var broken = outcome.Value;
            if (broken)
            {
                output.WriteLine($"  still  {name}: {documented}");
            }
            else
            {
                output.WriteLine($"  FIXED  {name} — documented as: {documented}");
                output.WriteLine($"         Update the resources and Sources/MCP/README.md, and remove this check.");
                drifted++;
            }
        }

        output.WriteLine();
        if (failed > 0)
            output.WriteLine($"{failed} identity check(s) FAILED — this install is not trustworthy.");
        else
            output.WriteLine($"All {Identities.Length} identities hold.");

        if (drifted > 0)
            output.WriteLine($"{drifted} documented defect(s) no longer reproduce. The documents have drifted.");

        return (failed, drifted);
    }
}
