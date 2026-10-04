//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;

namespace AngouriMath.Mcp;

/// <summary>
/// User-invocable workflows, surfaced by MCP hosts as slash commands.
///
/// These exist because of the routing problem this whole server is shaped around: a model
/// will not reach for a maths tool, since it is confident it can do algebra unaided. A
/// prompt is the *user* reaching for it instead, which sidesteps the question entirely. They
/// also cost nothing in tool-list context, which matters once a server has twenty-one tools.
///
/// Each one encodes a workflow already proven in test/scenarios.sh rather than inventing a
/// new one.
/// </summary>
internal static class Prompts
{
    private sealed record Argument(string Name, string Description, bool Required);

    private sealed record Definition(
        string Name,
        string Description,
        Argument[] Arguments,
        Func<Func<string, string>, string> Build);

    /// <summary>Advice repeated into every prompt, because it is what goes wrong.</summary>
    private const string Discipline = """

        While doing this:
        - Read the `parsed` field of every response before trusting the answer. A trailing
          digit is an exponent here, so a variable named `t0` silently becomes t^0, and an
          unknown function name silently becomes a multiplication.
        - Treat a `declined` or `unchanged` status as "no answer", never as the answer.
        - If a call reports `verified: false` or `status: conflict`, stop and say so rather
          than presenting the result.
        - Say which tool produced each result. Do not present tool output as your own working.
        """;

    private static readonly Definition[] All =
    [
        new("verify-derivation",
            "Check a chain of algebra step by step and identify exactly which step broke.",
            [
                new("working", "Your steps, one per line, each a complete expression.", true),
            ],
            get => $"""
                Here is a derivation. Each line should be mathematically equal to the one
                before it:

                {get("working")}

                Split it into successive expressions and pass them to `am_check_steps` in
                order. Then tell me plainly whether the working is sound.

                If a step is invalid, report which one, quote the `difference` the tool
                returns, and explain in one sentence what went wrong there — a dropped term,
                a sign, a bad cancellation. Do not re-derive the whole thing; the point is to
                locate the error, not replace my working.
                {Discipline}
                """),

        new("check-formula",
            "Compare a formula as documented against a formula as implemented in code.",
            [
                new("documented", "The formula as written in the comment, spec or datasheet.", true),
                new("implemented", "The expression as it actually appears in the code.", true),
            ],
            get => $"""
                A formula appears two ways. As documented:

                    {get("documented")}

                As implemented in the code:

                    {get("implemented")}

                Use `am_verify_equal` to decide whether they are the same function.

                If they differ, quote the `difference` — that names the discrepancy far more
                precisely than a description does — and say which of the two is likely wrong.
                If they agree, say so without hedging.

                Then call `am_domain_check` on the implemented form and mention anything that
                would bite in practice: a division that can vanish, a logarithm or square root
                that leaves the reals, a value the hardware could plausibly produce that the
                expression cannot handle.
                {Discipline}
                """),

        new("derive-jacobian",
            "Differentiate a model with respect to each state variable, with every partial verified.",
            [
                new("model", "The measurement or state function, e.g. 'sqrt(x^2 + y^2)'.", true),
                new("variables", "Comma-separated variables to differentiate by, e.g. 'x,y'.", true),
            ],
            get => $"""
                Build the Jacobian row for this model:

                    {get("model")}

                with respect to: {get("variables")}

                For each variable, call `am_differentiate`. Then verify each partial rather
                than trusting it: integrate it back with `am_integrate` and check it returns
                the original, or use `am_verify_equal` against a form you derive
                independently. Say which check you used.

                Present the result as a list of partials, then as a single row vector. Keep
                the exact symbolic forms — do not convert to decimals. Hand-derived Jacobians
                are where silent errors live for months, so flag anything you could not
                verify rather than quietly including it.
                {Discipline}
                """),

        new("analyse-approximation",
            "Work out whether a fast approximation is sound and where it stops being good enough.",
            [
                new("exact", "The exact expression, e.g. '1/sqrt(x)'.", true),
                new("approximation",
                    "The fast form, or one iteration of it, e.g. 'y*(3 - x*y^2)/2'.", true),
                new("variable", "The variable, e.g. 'x'.", true),
                new("range", "Operating range as 'from,to', e.g. '1,4'. Optional.", false),
            ],
            get => $"""
                A fast approximation is being used in place of an exact expression.

                    exact:         {get("exact")}
                    approximation: {get("approximation")}
                    variable:      {get("variable")}
                    range:         {Or(get("range"), "not stated — ask, or say what you assumed")}

                Work out whether it is sound, in this order:

                1. **Is the iteration what it claims to be?** If the approximation is an
                   iterative refinement, it is probably Newton's method for some f. Work out
                   which f, then use `am_verify_equal` to confirm the update rule really is
                   `y - f(y)/f'(y)` for it. If it is not Newton, say what it is instead.

                2. **Derive the error term, do not guess it.** Substitute a perturbed value —
                   for a reciprocal-root style iteration that means `y = (1+e)/sqrt(x)` — with
                   `am_substitute`, then use `am_verify_equal` to pin the new error against
                   your candidate expression. The leading power of `e` is the order of
                   convergence, and that is the number that decides how many iterations are
                   needed. Quadratic means one iteration roughly squares the error.

                3. **Measure it.** Use `am_compare_numeric` across the range to get the worst
                   absolute and relative error and, more usefully, WHERE they occur. Sampling
                   says nothing between samples, so if the error curve could have a spike,
                   raise the sample count and say you did.

                4. **Account for the constants.** If a magic constant or a bit pattern is
                   involved, decompose it with `am_represent ieee754` and explain what it does
                   rather than repeating that it is magic. If the result is destined for fixed
                   point, run `am_represent fixed_point` and include the quantisation error
                   alongside the approximation error — they add up, and the smaller one is
                   often not the one people worry about.

                Finish with a judgement: how many iterations for a stated tolerance, and the
                input range over which that holds. If the approximation is unsound, or the
                error is unbounded somewhere in range, lead with that.
                {Discipline}
                """),

        new("solve-with-constraints",
            "Solve an equation and keep only the physically meaningful branch.",
            [
                new("equation", "The equation, e.g. 'v = k*d^2 + m*d'.", true),
                new("variable", "The variable to solve for.", true),
                new("constraints",
                    "Physical constraints, comma-separated, e.g. 'd > 0'. Optional.", false),
            ],
            get => $"""
                Solve this for {get("variable")}:

                    {get("equation")}

                Physical constraints: {Or(get("constraints"), "none stated")}

                Pass the equation and the constraints together to `am_solve` as a single
                `constraints` list — it handles inequalities, so the solver itself discards
                the branches that cannot occur. That is better than solving first and
                filtering afterwards.

                If no constraints were given, solve unconstrained, then tell me which roots
                are physically implausible and what constraint would exclude them. A negative
                length, a complex time, a root outside the measurable range: mathematically
                correct and useless. Say so explicitly rather than presenting every root as
                an answer.
                {Discipline}
                """),
    ];

    private static string Or(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    public static JsonArray List()
    {
        var listed = new JsonArray();
        foreach (var prompt in All)
        {
            var arguments = new JsonArray();
            foreach (var argument in prompt.Arguments)
                arguments.Add(new JsonObject
                {
                    ["name"] = argument.Name,
                    ["description"] = argument.Description,
                    ["required"] = argument.Required,
                });

            listed.Add(new JsonObject
            {
                ["name"] = prompt.Name,
                ["description"] = prompt.Description,
                ["arguments"] = arguments,
            });
        }
        return listed;
    }

    /// <summary>Null when the name is unknown; throws when a required argument is missing.</summary>
    public static JsonObject? Get(string name, JsonObject? arguments)
    {
        var prompt = All.FirstOrDefault(p => p.Name == name);
        if (prompt is null) return null;

        string Read(string key) =>
            arguments?[key]?.GetValue<string>() ?? string.Empty;

        var missing = prompt.Arguments
            .Where(a => a.Required && string.IsNullOrWhiteSpace(Read(a.Name)))
            .Select(a => a.Name)
            .ToArray();

        if (missing.Length > 0)
            throw new ArgumentException(
                $"prompt '{name}' requires: {string.Join(", ", missing)}");

        return new JsonObject
        {
            ["description"] = prompt.Description,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = prompt.Build(Read),
                    },
                },
            },
        };
    }
}
