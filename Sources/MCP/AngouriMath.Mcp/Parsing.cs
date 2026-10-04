//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AngouriMath;
using HonkSharp.Functional;

namespace AngouriMath.Mcp;

/// <summary>
/// Parsing plus the two warnings that matter.
///
/// AngouriMath's parser is permissive in two ways that are silent and therefore dangerous
/// when the caller is a language model rather than a human reading its own formula:
///
///   * a trailing number is an EXPONENT, not a factor: `x2` is x², `2(g+e)3` is 2(g+e)³.
///     A model that names a variable `x2`, `v1` or `a0` — completely ordinary naming —
///     gets it silently squared. (MathS.cs documents this on ExplicitParsingOnly.)
///   * an unknown identifier becomes implicit multiplication: `im(z)` is the product
///     `im * z`, not the imaginary part. The library refuses some such names by name —
///     `trunc`, `lcm`, `erf`, `conjugate` and the arc- spellings of the inverse hyperbolics —
///     so those fail loudly; the rest degrade with nothing said, because refusing every
///     unknown name is refusing `a(b + c)`.
///
/// Both produce a valid parse of a DIFFERENT expression, which is the worst failure class
/// available: no exception, plausible answer, wrong. Rather than force strict mode on
/// everyone (which rejects the very common `2x`), the server parses permissively and always
/// reports what it understood, with a warning when either pattern is present.
/// </summary>
internal static class Parsing
{
    /// <summary>Either a parsed entity with its warnings, or an error string — never both.</summary>
    public sealed record Outcome(Entity? Entity, List<string> Warnings, string? Error);

    /// <summary>
    /// Whether the grammar reads <c>name(...)</c> as a call, rather than as the variable
    /// <c>name</c> times a bracketed group. Asked of the parser itself, by parsing
    /// <c>name(x)</c>, so a name that becomes a function or stops being one is answered
    /// correctly without anything here changing: a hand-kept list of names warns on correct
    /// input when it lags behind the grammar, and that teaches a caller to ignore the channel.
    /// </summary>
    /// <remarks>
    /// A name the grammar refuses outright, or calls with the wrong number of arguments, also
    /// counts as known: either way the input fails to parse, and the warnings below only look
    /// at an input that parsed. Parsed permissively whatever the caller asked for, since the
    /// answer is cached for every later request.
    /// </remarks>
    internal static bool IsFunction(string name) => Calls.GetOrAdd(name, static candidate =>
    {
        using var _ = MathS.Settings.ExplicitParsingOnly.Set(false);
        try
        {
            var x = MathS.Var("x");
            return MathS.Parse(candidate + "(x)").Switch(
                call => MathS.Parse(candidate).Switch(alone => call != alone * x, _ => true),
                _ => true);
        }
        catch (Exception)
        {
            return true;
        }
    });

    private static readonly ConcurrentDictionary<string, bool> Calls = new(StringComparer.Ordinal);

    /// <summary>Names people reach for that this grammar spells differently, and that parse
    /// as something else rather than failing. A name the library refuses outright is not
    /// here: its own exception says more than this table could.</summary>
    internal static readonly Dictionary<string, string> Misspellings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["setsubtraction"] = @"the infix '\' — write 'A \ B'",
        ["elementin"] = "the infix 'in' — write 'x in A'",
        ["impl"] = "'->' or 'implies' — write 'a -> b'",
        ["re"] = "not in this grammar; there is no real-part function",
        ["im"] = "not in this grammar; there is no imaginary-part function",
    };

    // The alphabet the grammar's VARIABLE rule accepts (Core/Antlr/AngouriMath.g): ASCII
    // letters, Greek and Coptic, Greek Extended, Cyrillic. Both warnings below are lexical
    // guesses at what the grammar will do, so they have to guess in the grammar's alphabet:
    // `[A-Za-z]` would exempt every Greek and Cyrillic name from both checks, and `α2` would
    // be squared with nothing said.
    // Kept as escapes rather than literal Greek so the ranges can be read straight off the
    // grammar. The string is verbatim, so C# passes the escapes through untouched and the
    // regex engine is what interprets them.
    private const string VariableChars = @"a-zA-Z\u0370-\u03FF\u1F00-\u1FFF\u0400-\u04FF";

    // A digit directly after a LETTER is the trap: `x2` parses as x^2. Two things are NOT
    // the trap. A digit after an underscore — `t_0` is a single variable named t_0, and is
    // the conventional safe way to write a subscript. And the digits of an exponent: `1.5e3`
    // is ONE number token, because EXPONENT is a fragment of NUMBER, so its `e` belongs to
    // the literal rather than to a variable. Hence the second branch: an `e` IS a warnable
    // identifier when no numeric literal precedes it, which is why bare `e3` (that is, e^3)
    // still warns. False positives are how you teach a caller to ignore warnings.
    private static readonly Regex TrailingDigit =
        new($@"[{VariableChars}-[eE]]\d|(?<![\d.])[eE]\d", RegexOptions.Compiled);

    // A name followed by '(' — the grammar has no leading-underscore variable, so the name
    // must start with a letter, and only continue with what VARIABLE allows.
    private static readonly Regex CallLike =
        new($@"([{VariableChars}][{VariableChars}0-9_]*)\s*\(", RegexOptions.Compiled);

    public static Outcome Parse(string source, bool strict = false)
    {
        // Scoped, and reverted on leaving. The scope follows the call rather than the thread,
        // so it holds under concurrency as well.
        using var _ = MathS.Settings.ExplicitParsingOnly.Set(strict);

        // MathS.Parse is the non-throwing parser: it returns a reason rather than raising,
        // which is what lets the caller report a clean message instead of a stack trace.
        return MathS.Parse(source).Switch(
            entity => new Outcome(entity, Warnings(source), null),
            failure => new Outcome(null, [], failure.Reason.Switch<string>(
                unknown => $"could not parse: {unknown.Reason}",
                missingOperator => $"missing operator: {missingOperator.Details}",
                internalError => $"internal parser error: {internalError.Details}")));
    }

    private static List<string> Warnings(string source)
    {
        var warnings = new List<string>();
        var calls = CallLike.Matches(source);

        // A known function's own name is not an implicit power, even when it ends in a
        // digit. `log2(8)` is the base-2 logarithm, and the `g2` inside it is
        // not the `x2` trap — warning there fires on correct input, which is how a caller
        // learns to ignore the channel. Spans rather than a special case for `log2` and
        // `log10`, so the next function name carrying a digit needs no second fix.
        var functionNameSpans = calls
            .Where(m => IsFunction(m.Groups[1].Value))
            .Select(m => (Start: m.Groups[1].Index, End: m.Groups[1].Index + m.Groups[1].Length))
            .ToList();

        var implicitPower = TrailingDigit.Matches(source).Any(m =>
            !functionNameSpans.Any(s => m.Index >= s.Start && m.Index + m.Length <= s.End));

        if (implicitPower)
            warnings.Add(
                "implicit-power: a number directly after an identifier is an EXPONENT, " +
                "not a factor — 'x2' parses as x^2. Check the 'parsed' field; write 'x*2' " +
                "if you meant multiplication, and avoid variable names ending in a digit.");

        foreach (Match m in calls)
        {
            var name = m.Groups[1].Value;
            if (IsFunction(name)) continue;

            var hint = Misspellings.TryGetValue(name, out var spelling)
                ? $" Use {spelling}."
                : string.Empty;

            warnings.Add(
                $"unknown-function: '{name}' is not a function AngouriMath knows, so it was " +
                $"read as a VARIABLE multiplied by the bracketed group, not as a call." +
                hint + " Check the 'parsed' field.");
        }

        return warnings;
    }
}
