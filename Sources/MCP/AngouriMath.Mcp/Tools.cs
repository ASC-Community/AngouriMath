//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

internal static class Tools
{
    // ---------------------------------------------------------------- schemas

    private static JsonObject Str(string desc) =>
        new() { ["type"] = "string", ["description"] = desc };

    private static JsonObject Schema(JsonObject props, params string[] required)
    {
        var req = new JsonArray();
        foreach (var r in required) req.Add(r);
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = props,
            ["required"] = req,
        };
    }

    private static JsonObject Tool(string name, string description, JsonObject schema) => new()
    {
        ["name"] = name,
        ["description"] = description,
        ["inputSchema"] = schema,
        // Pure computation: no side effects, no network, no filesystem. Declaring this lets
        // clients auto-approve instead of prompting on every call — and a math tool that
        // costs a permission click per invocation simply will not get used.
        ["annotations"] = new JsonObject
        {
            ["readOnlyHint"] = true,
            ["destructiveHint"] = false,
            ["idempotentHint"] = true,
            ["openWorldHint"] = false,
        },
    };

    private const string CallEvenIfConfident =
        " Call this even when you believe you can do the algebra unaided — unaided symbolic " +
        "manipulation is unreliable for exactly this class of problem, and the result here is " +
        "machine-checked.";

    public static JsonArray List() =>
    [
        Tool("am_parse",
            "Parse an expression and show exactly how AngouriMath understood it. Use this " +
            "first whenever an expression contains identifiers with digits (x2, v1) or " +
            "function-looking names, because both are silently misread. Returns the canonical " +
            "form, LaTeX, free variables, and warnings.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression in AngouriMath syntax, e.g. 'x^2 + 3*x - 1'."),
                ["strict"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = "Reject implicit multiplication instead of guessing. Default false.",
                },
            }, "expression")),

        Tool("am_simplify",
            "Simplify an expression exactly." + CallEvenIfConfident +
            " Set alternatives=true to get several candidate forms ranked by complexity, " +
            "which is useful because the default form is not always the tidiest.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to simplify."),
                ["alternatives"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = "Also return alternative simplified forms. Default false.",
                },
            }, "expression")),

        Tool("am_solve",
            "Solve an equation, inequality, or a set of simultaneous constraints for one " +
            "variable. Constraints are combined, so you can narrow a solution incrementally " +
            "(e.g. ['x^2 = 4', 'x > 0'] gives 2)." + CallEvenIfConfident,
            Schema(new JsonObject
            {
                ["constraints"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "One or more statements, e.g. ['x^2 - 1 = 0'] or ['x^2=4','x>0'].",
                },
                ["variable"] = Str("Variable to solve for, e.g. 'x'."),
            }, "constraints", "variable")),

        Tool("am_differentiate",
            "Differentiate an expression with respect to a variable." + CallEvenIfConfident,
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to differentiate."),
                ["variable"] = Str("Variable to differentiate by."),
                ["order"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Order of the derivative. Default 1.",
                },
            }, "expression", "variable")),

        Tool("am_integrate",
            "Integrate an expression symbolically. The answer is ALWAYS verified by " +
            "differentiating it back and comparing numerically; check the 'verified' field. " +
            "If the library has no rule for the integrand the status is 'declined', which is " +
            "an honest answer, not a failure — and when 'from' and 'to' are given, a numeric " +
            "value for that one interval still comes back in 'numeric_definite_value', " +
            "rounded to the digits it is actually good for." + CallEvenIfConfident,
            Schema(new JsonObject
            {
                ["expression"] = Str("Integrand."),
                ["variable"] = Str("Variable of integration."),
                ["from"] = Str("Lower limit. Give both 'from' and 'to' for a definite integral."),
                ["to"] = Str("Upper limit."),
            }, "expression", "variable")),

        Tool("am_limit",
            "Compute a limit, optionally one-sided." + CallEvenIfConfident,
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression."),
                ["variable"] = Str("Variable approaching the destination."),
                ["to"] = Str("Destination, e.g. '0', '+oo', '-oo'."),
                ["side"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray { "both", "left", "right" },
                    ["description"] = "Approach direction. Default 'both'.",
                },
            }, "expression", "variable", "to")),

        Tool("am_evaluate",
            "Evaluate an expression numerically, optionally substituting values first. " +
            "Returns exact form and a decimal approximation.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to evaluate."),
                ["substitutions"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Optional map of variable -> value, e.g. {\"x\": \"2\"}.",
                },
                ["digits"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Significant digits for the numeric result, 1-1000. " +
                        "Omit for a normal double. Raise it when the question turns on the " +
                        "tail — e^(pi*sqrt(163)) only looks like an integer until digit 18.",
                },
            }, "expression")),

        Tool("am_verify_equal",
            "Check whether two expressions are mathematically equal. Use this to check your " +
            "OWN algebra: give it your hand-derived form and the original, and it will tell " +
            "you if you changed the meaning. Tries exact simplification first, then falls " +
            "back to numeric sampling.",
            Schema(new JsonObject
            {
                ["left"] = Str("First expression."),
                ["right"] = Str("Second expression."),
            }, "left", "right")),

        Tool("am_truth_table",
            "Build a truth table for a boolean expression, and list the assignments that " +
            "satisfy it. Useful for reasoning about branch conditions and predicates. " +
            "Operators: 'and', 'or', 'not', 'xor', 'implies'.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Boolean expression, e.g. 'a and (b or not c)'."),
                ["variables"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Variable order. Inferred from the expression if omitted.",
                },
            }, "expression")),

        Tool("am_solve_system",
            "Solve a system of simultaneous equations for several variables. Returns one row " +
            "per solution. Note: nonlinear systems may return no solution even when one " +
            "exists (a known library limitation).",
            Schema(new JsonObject
            {
                ["equations"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Equations, e.g. ['x + y = 3', 'x - y = 1'].",
                },
                ["variables"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Variables to solve for, e.g. ['x','y'].",
                },
            }, "equations", "variables")),

        Tool("am_check_steps",
            "Check a chain of working step by step and report WHICH step broke. Use this to " +
            "present a derivation: write out your steps, have them checked, then explain " +
            "them. AngouriMath cannot generate a derivation — but you can, and this makes " +
            "yours trustworthy. Each consecutive pair must be mathematically equal.",
            Schema(new JsonObject
            {
                ["steps"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Successive forms of the SAME expression, in order, " +
                                      "e.g. ['(x+1)^2 - 1', 'x^2 + 2*x + 1 - 1', 'x^2 + 2*x'].",
                },
            }, "steps")),

        Tool("am_domain_check",
            "Find what to watch out for in an expression: the domain conditions AngouriMath " +
            "derived, the structural hazards (division, logs, fractional powers, inverse " +
            "trig), and the sample points where it stops being a real number. Use this " +
            "before trusting a result as a physical quantity — a value can be perfectly " +
            "correct and still be meaningless as a length, a time or a resistance.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to inspect."),
            }, "expression")),

        Tool("am_represent",
            "How a number is ENCODED, rather than what it equals. Bases 2-36, including " +
            "fractions and repeating expansions. Q-format fixed point, reporting the " +
            "quantisation error and whether the value saturated — the question you actually " +
            "have when putting a coefficient into a processor. The bits of an IEEE 754 float " +
            "or double together with the exact decimal it holds, which is how you settle why " +
            "0.1 is not 0.1. And polar form, kept symbolic, so 1+i gives sqrt(2) and pi/4 " +
            "rather than 1.414 and 0.785.",
            Schema(new JsonObject
            {
                ["operation"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray
                    {
                        "base", "fixed_point", "from_fixed_point", "ieee754",
                        "polar", "rectangular",
                    },
                    ["description"] = "Which representation to produce.",
                },
                ["value"] = Str("The number as an expression. For 'from_fixed_point', the " +
                                "raw stored integer. Unused by 'rectangular'."),
                ["from_base"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "base: base of the input, 2-36. Default 10.",
                },
                ["to_base"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "base: base to convert to, 2-36.",
                },
                ["total_bits"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "fixed_point: machine word width, e.g. 16 or 32. Default 16.",
                },
                ["fraction_bits"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "fixed_point / from_fixed_point: bits after the binary " +
                                      "point. Q15 in a 16-bit word means 15.",
                },
                ["signed"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = "fixed_point: signed word. Default true.",
                },
                ["precision"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray { "double", "single" },
                    ["description"] = "ieee754: which format. Default double.",
                },
                ["magnitude"] = Str("rectangular: magnitude."),
                ["phase"] = Str("rectangular: phase in radians."),
            }, "operation")),

        Tool("am_matrix",
            "Exact symbolic linear algebra. Entries are expressions, so they may contain " +
            "variables — the determinant of a matrix of symbols comes back as a formula, not " +
            "a number. `tensor_product` builds multi-qubit operators from single-qubit ones, " +
            "which is what makes quantum circuit algebra work: CNOT * (H (x) I) * |00> gives " +
            "the Bell state as exactly 1/sqrt(2), not 0.7071.",
            Schema(new JsonObject
            {
                ["operation"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray
                    {
                        "determinant", "inverse", "transpose", "rank", "rref", "trace",
                        "multiply", "add", "subtract", "tensor_product", "power",
                    },
                    ["description"] = "What to compute.",
                },
                ["matrix"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" },
                    },
                    ["description"] = "Rows of expression strings, e.g. [[\"1\",\"x\"],[\"0\",\"1\"]]. " +
                                      "A column vector is [[\"1\"],[\"0\"]].",
                },
                ["matrix_b"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" },
                    },
                    ["description"] = "Second operand, for multiply / add / subtract / tensor_product.",
                },
                ["exponent"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "For 'power'. Non-negative.",
                },
            }, "operation", "matrix")),

        Tool("am_eigenvalues",
            "Eigenvalues of a square matrix, exactly and symbolically — entries may contain " +
            "variables, so a Hamiltonian [[0,J],[J,0]] returns its energy levels in terms of J. Computed " +
            "from the characteristic polynomial det(A - lambda*I). Expect a decline above 4x4 " +
            "with symbolic entries: quintics have no closed form (Abel-Ruffini), which is a " +
            "fact about mathematics rather than a limitation of this server.",
            Schema(new JsonObject
            {
                ["matrix"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" },
                    },
                    ["description"] = "Square matrix as rows of expression strings.",
                },
            }, "matrix")),

        Tool("am_compare_numeric",
            "Measure how far apart two expressions are across an interval: worst absolute " +
            "error, worst relative error, RMS, and WHERE the worst point is. Use this for " +
            "'is this approximation good enough over the operating range' — a different " +
            "question from am_verify_equal, which asks whether two forms are the same " +
            "function. An approximation is never equal; the question is whether it is close " +
            "enough, and where it degrades.",
            Schema(new JsonObject
            {
                ["reference"] = Str("The exact expression, used as the baseline."),
                ["approximation"] = Str("The expression being checked against it."),
                ["variable"] = Str("Variable to sweep."),
                ["from"] = new JsonObject
                {
                    ["type"] = "number",
                    ["description"] = "Start of the interval.",
                },
                ["to"] = new JsonObject
                {
                    ["type"] = "number",
                    ["description"] = "End of the interval.",
                },
                ["samples"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Evenly spaced sample count, 2-10000. Default 200.",
                },
            }, "reference", "approximation", "variable", "from", "to")),

        Tool("am_substitute",
            "Replace variables with expressions and return the result WITHOUT evaluating or " +
            "simplifying it. Use this when you want to see the shape of a substitution — " +
            "putting a model into a formula, specialising a general result — rather than a " +
            "number. am_evaluate always simplifies; this does not.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to substitute into."),
                ["substitutions"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Map of variable -> replacement expression, " +
                                      "e.g. {\"x\": \"a + b\"}.",
                },
                ["simplify"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = "Simplify afterwards as well. Default false.",
                },
            }, "expression", "substitutions")),

        Tool("am_expand",
            "Expand brackets and powers into a sum of terms. The opposite of am_factor.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to expand, e.g. '(x+1)^3'."),
            }, "expression")),

        Tool("am_factor",
            "Factorise an expression into a product. The opposite of am_expand. Multivariate " +
            "factoring is limited, so a result identical to the input means no progress.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to factorise, e.g. 'x^2 - 1'."),
            }, "expression")),

        Tool("am_series",
            "Taylor or Maclaurin series expansion to a given degree. Use it to linearise a " +
            "model around an operating point, to justify a small-angle approximation, or to " +
            "decide how many terms a fixed-point implementation actually needs.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Expression to expand, e.g. 'sin(x)'."),
                ["variable"] = Str("Variable to expand in."),
                ["degree"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Highest power to include. Keep it modest; cost grows quickly.",
                },
                ["around"] = Str("Point to expand about. Omit for 0 (a Maclaurin series)."),
            }, "expression", "variable", "degree")),

        Tool("am_number_theory",
            "Integer facts: prime factorisation, Euler's totient, greatest common divisor, " +
            "divisor count, primality. Exact for large integers.",
            Schema(new JsonObject
            {
                ["operation"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray
                    {
                        "factorize", "totient", "gcd", "count_divisors", "is_prime",
                    },
                    ["description"] = "Which fact to compute.",
                },
                ["value"] = Str("The integer, as an expression, e.g. '5040'."),
                ["value_b"] = Str("Second integer, for 'gcd'."),
            }, "operation", "value")),

        Tool("am_classify",
            "Guess which field of science or mathematics a formula comes from, by reading " +
            "its symbols and structure. Inference from naming convention only — 'c' is the " +
            "speed of light in relativity and a concentration in chemistry — so treat the " +
            "result as a hint, and say it is a guess when you repeat it.",
            Schema(new JsonObject
            {
                ["expression"] = Str("Formula to classify, e.g. 'G*M*m/r^2'."),
            }, "expression")),

        Tool("am_to_sympy",
            "Emit a runnable SymPy program for an expression. Use this to hand the problem to " +
            "Python, or to cross-check a result against a second computer algebra system.",
            Schema(new JsonObject { ["expression"] = Str("Expression to translate.") },
                "expression")),
    ];

    // ---------------------------------------------------------------- dispatch

    public static JsonObject Call(string name, JsonObject args) => name switch
    {
        "am_parse" => Parse(args),
        "am_simplify" => Simplify(args),
        "am_solve" => Solve(args),
        "am_differentiate" => Differentiate(args),
        "am_integrate" => Integrate(args),
        "am_limit" => Limit(args),
        "am_evaluate" => Evaluate(args),
        "am_verify_equal" => VerifyEqual(args),
        "am_truth_table" => TruthTable(args),
        "am_solve_system" => SolveSystem(args),
        "am_check_steps" => CheckSteps(args),
        "am_domain_check" => DomainCheck(args),
        "am_represent" => Represent(args),
        "am_matrix" => MatrixOp(args),
        "am_eigenvalues" => Eigenvalues(args),
        "am_compare_numeric" => CompareNumeric(args),
        "am_substitute" => Substitute(args),
        "am_expand" => Rewrite(args, expand: true),
        "am_factor" => Rewrite(args, expand: false),
        "am_series" => Series(args),
        "am_number_theory" => NumberTheory(args),
        "am_classify" => ClassifyFormula(args),
        "am_to_sympy" => ToSympy(args),
        _ => Fail($"unknown tool '{name}'"),
    };

    // ---------------------------------------------------------------- helpers

    private static string? S(JsonObject o, string key) =>
        o.TryGetPropertyValue(key, out var v) ? v?.GetValue<string>() : null;

    private static bool B(JsonObject o, string key, bool fallback = false) =>
        o.TryGetPropertyValue(key, out var v) && v is not null ? v.GetValue<bool>() : fallback;

    private static List<string> A(JsonObject o, string key)
    {
        var list = new List<string>();
        if (o.TryGetPropertyValue(key, out var v) && v is JsonArray arr)
            foreach (var item in arr)
                if (item is not null) list.Add(item.GetValue<string>());
        return list;
    }

    private static JsonObject Fail(string error) =>
        new() { ["status"] = "failed", ["error"] = error };

    private static JsonArray Warn(List<string> warnings)
    {
        var arr = new JsonArray();
        foreach (var w in warnings) arr.Add(w);
        return arr;
    }

    /// <summary>Parse one input, or produce the error object.</summary>
    private static bool TryParse(string? src, string label, bool strict,
        out Entity entity, out List<string> warnings, out JsonObject? error)
    {
        entity = 0; warnings = []; error = null;
        if (string.IsNullOrWhiteSpace(src))
        {
            error = Fail($"'{label}' is required");
            return false;
        }

        var outcome = Parsing.Parse(src, strict);
        if (outcome.Entity is null)
        {
            error = Fail(outcome.Error ?? "parse failed");
            return false;
        }

        entity = outcome.Entity;
        warnings = outcome.Warnings;
        return true;
    }

    /// <summary>
    /// Build the standard response. The order matters: decline detection runs on the RAW
    /// stringized result before anything simplifies it, because an unevaluated limit(...)
    /// simplifies to NaN and would otherwise be reported as a wrong answer.
    /// </summary>
    private static JsonObject Respond(Entity input, Entity result, List<string> warnings)
    {
        var raw = result.Stringize();
        var status =
            Guard.IsDeclined(raw) ? "declined"
            : raw == input.Stringize() ? "unchanged"
            : Guard.LooksLikeNaN(raw) ? "suspect"
            : "solved";

        var response = new JsonObject
        {
            ["status"] = status,
            ["parsed"] = input.Stringize(),
            ["result"] = raw,
            ["latex"] = result.Latexize(),
        };

        if (status == "declined")
            response["note"] = "AngouriMath returned the expression unevaluated: it has no " +
                "rule for this input. This is a decline, not a wrong answer — do not present " +
                "the unevaluated form as a result.";
        if (status == "unchanged")
            response["note"] = "The result is identical to the input. The library could not " +
                "reduce it further; treat this as 'no progress', not as a simplest form.";
        if (status == "suspect")
            response["note"] = "The result contains NaN, which is almost never a legitimate " +
                "answer here. Treat it as a library failure.";

        if (warnings.Count > 0) response["warnings"] = Warn(warnings);
        return response;
    }

    private static JsonObject FromOutcome<T>(Guard.Outcome<T> outcome, Func<T, JsonObject> ok) =>
        outcome.Status switch
        {
            "ok" => ok(outcome.Value!),
            "timeout" => new JsonObject
            {
                ["status"] = "timeout",
                ["error"] = outcome.Error,
                ["note"] = "The call exceeded its budget and was abandoned. Try a simpler " +
                           "form, or accept that this input is out of reach.",
            },
            // A NotSufficientlySupportedException, which the library raises where it knows
            // it has no rule. Reported with the same word as an unevaluated integral because
            // it means the same thing to the caller. See Guard.StatusFor.
            "declined" => new JsonObject
            {
                ["status"] = "declined",
                ["error"] = outcome.Error,
                ["note"] = "AngouriMath declined this input: it says explicitly that it has " +
                           "no rule covering this case. That is a limit of the library, not " +
                           "a wrong answer and not a bug worth reporting — do not retry the " +
                           "same call, and do not present the message as a result.",
            },
            _ => Fail(outcome.Error ?? "unknown failure"),
        };

    private static Variable Var(string name) => MathS.Var(name);

    // Numeric comparison, `provided`-stripping and the sample points all live in Numeric.cs.

    // ---------------------------------------------------------------- tools

    private static JsonObject Parse(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", B(args, "strict"),
                out var e, out var warnings, out var error))
            return error!;

        var vars = new JsonArray();
        foreach (var v in e.Vars) vars.Add(v.Stringize());

        var response = new JsonObject
        {
            ["status"] = "solved",
            ["parsed"] = e.Stringize(),
            ["result"] = e.Stringize(),
            ["latex"] = e.Latexize(),
            ["free_variables"] = vars,
            ["node_count"] = e.Nodes.Count(),
        };
        if (warnings.Count > 0) response["warnings"] = Warn(warnings);
        return response;
    }

    private static JsonObject Simplify(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var wantAlternatives = B(args, "alternatives");

        return FromOutcome(Guard.Run(() =>
        {
            var simplified = e.Simplify();
            var alternates = new List<string>();
            if (wantAlternatives)
                foreach (var alt in e.Alternate(4).Take(5))
                {
                    var s = alt.Stringize();
                    if (s != simplified.Stringize() && !alternates.Contains(s))
                        alternates.Add(s);
                }
            return (simplified, alternates);
        }), r =>
        {
            var response = Respond(e, r.simplified, warnings);
            if (r.alternates.Count > 0)
            {
                var arr = new JsonArray();
                foreach (var a in r.alternates) arr.Add(a);
                response["alternatives"] = arr;
                response["alternatives_note"] =
                    "Ranked by the library's complexity metric. The first result is not " +
                    "always the tidiest for a human — pick whichever form suits.";
            }
            return response;
        });
    }

    private static JsonObject Solve(JsonObject args)
    {
        var constraints = A(args, "constraints");
        var variable = S(args, "variable");
        if (constraints.Count == 0) return Fail("'constraints' must contain at least one statement");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");

        // Combining with 'and' turns several constraints into a single boolean statement,
        // which Solve handles natively (it accepts statements and inequalities, not just
        // equations). Borrowed from the SharpCells Excel example.
        var combined = string.Join(" and ", constraints);

        if (!TryParse(combined, "constraints", false, out var e, out var warnings, out var error))
            return error!;

        return FromOutcome(Guard.Run(() => e.Solve(Var(variable!))), set =>
        {
            var response = Respond(e, set, warnings);
            if (set is Set.FiniteSet finite)
            {
                var roots = new JsonArray();
                foreach (var root in finite)
                {
                    // Solve leaves roots in raw form: solving f = 1/(2*pi*R*C) for R yields
                    // "--1/2 * 1/pi * 1/C/f". Correct, but a double negation is exactly the
                    // output-tidiness weakness the library is known for, so tidy each root.
                    // Guarded because Simplify can be slow and must not lose the answer.
                    Entity tidy;
                    try { tidy = root.Simplify(); }
                    catch { tidy = root; }
                    roots.Add(tidy.Stringize());
                }
                response["solutions"] = roots;
                response["solution_count"] = finite.Count;
            }
            else
            {
                response["note"] = "The solution set is not finite (an interval, a " +
                    "conditional set, or a periodic family). Read 'result' directly.";
            }
            return response;
        });
    }

    private static JsonObject Differentiate(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;
        var variable = S(args, "variable");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");

        var order = args.TryGetPropertyValue("order", out var o) && o is not null
            ? o.GetValue<int>() : 1;
        if (order < 1) return Fail("'order' must be at least 1");

        return FromOutcome(
            Guard.Run(() => e.Differentiate(Var(variable!), order).Simplify()),
            d => Respond(e, d, warnings));
    }

    private static JsonObject Integrate(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;
        var variable = S(args, "variable");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");

        var fromText = S(args, "from");
        var toText = S(args, "to");
        if (string.IsNullOrWhiteSpace(fromText) != string.IsNullOrWhiteSpace(toText))
            return Fail("give both 'from' and 'to', or neither");

        Entity? lower = null, upper = null;
        if (!string.IsNullOrWhiteSpace(fromText))
        {
            if (!TryParse(fromText, "from", false, out var l, out _, out var lowerError))
                return lowerError!;
            if (!TryParse(toText, "to", false, out var u, out _, out var upperError))
                return upperError!;
            lower = l;
            upper = u;
        }

        return FromOutcome(Guard.Run(() =>
        {
            var v = Var(variable!);

            // The numeric route, used only where the symbolic one produced nothing usable.
            // It is never a substitute for an antiderivative: it answers one interval, to a
            // handful of digits, and it cannot be verified by differentiating back.
            Numeric.Quadrature? Quadrature()
            {
                if (lower is null || upper is null) return null;
                if (Numeric.AsRealNumber(lower) is not { } a) return null;
                if (Numeric.AsRealNumber(upper) is not { } b) return null;
                if (Numeric.Strip(e).Vars.Any(other => other.Name != v.Name)) return null;
                return Numeric.TryDefiniteIntegral(e, v, a, b);
            }

            var antiderivative = e.Integrate(v);

            // Decline check on the RAW result, before Simplify — see Guard.IsDeclined.
            if (Guard.IsDeclined(antiderivative.Stringize()))
                return (antiderivative, (bool?)null, (Entity?)null, Quadrature());

            var tidy = antiderivative.Simplify();

            // Verify by differentiating back, which is the whole reason to trust the answer.
            bool? verified;
            try
            {
                verified = Numeric.Equal(tidy.Differentiate(v), e);
            }
            catch
            {
                verified = null;
            }

            // The definite value is evaluated from the SAME antiderivative that was just
            // verified, rather than asked for separately — so the verification covers it.
            Entity? definite = null;
            if (lower is not null && upper is not null)
            {
                var bare = Numeric.Strip(tidy);
                definite = (bare.Substitute(v, upper) - bare.Substitute(v, lower)).Simplify();
            }

            // Only where the exact route came back with nothing to show. A NaN definite
            // value is the common case: the antiderivative exists but is undefined at a
            // bound, and a number the caller can use beats `NaN` with a caveat attached.
            var quadrature = definite is null || Guard.LooksLikeNaN(definite.Stringize())
                ? Quadrature()
                : null;

            return (tidy, verified, definite, quadrature);
        }), r =>
        {
            var response = Respond(e, r.Item1, warnings);
            response["verified"] = r.Item2;
            response["verification_method"] =
                "differentiated the answer and compared it numerically to the integrand at " +
                "five positive real points";

            if (r.Item2 == false)
            {
                response["status"] = "suspect";
                response["note"] = "The answer does NOT differentiate back to the integrand. " +
                    "Do not use it.";
            }
            else if (r.Item2 is null && (string?)response["status"] == "declined")
            {
                response["note"] = "No antiderivative was found: the library has no rule " +
                    "for this integrand. Some, such as x^x, have no closed form in any " +
                    "function the library knows, so this can be the correct outcome.";
            }

            if (r.Item3 is { } definite)
            {
                response["definite_value"] = definite.Stringize();
                response["definite_latex"] = definite.Latexize();
                response["definite_caveat"] =
                    "Computed as F(to) - F(from) from the verified antiderivative. That is " +
                    "only valid when the integrand is continuous across the whole interval — " +
                    "it will happily return a finite, wrong number for something like " +
                    "1/x^2 across zero. Check for a singularity between the limits yourself, " +
                    "or ask am_domain_check.";
            }

            if (r.Item4 is { } q)
            {
                // Rounded to the digits two step counts agree on. Printing the library's
                // full arbitrary-precision output here would be a hundred digits of a
                // number correct to four, which is the failure this server exists to
                // prevent — the caller cannot see the step count and would read it as exact.
                response["numeric_definite_value"] = Math.Round(q.Value, q.Digits)
                    .ToString("G" + Math.Max(q.Digits + 1, 2),
                        System.Globalization.CultureInfo.InvariantCulture);
                response["numeric_definite_error_bound"] = q.ErrorEstimate
                    .ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
                response["numeric_definite_caveat"] =
                    "APPROXIMATE, and produced only because the exact route gave nothing " +
                    "usable. It is quadrature over 4000 steps, cross-checked against 2000, " +
                    "and reported to the digits the two agree on — every digit shown is " +
                    "believed, and there are no more to be had from this call. Unlike the " +
                    "symbolic answer it is NOT verified by differentiating back. It also " +
                    "assumes the integrand is finite across the closed interval: the rule " +
                    "samples both endpoints, so a convergent integral with a singular " +
                    "endpoint returns nothing here rather than a value.";

                if ((string?)response["status"] == "declined")
                    response["note"] = (string?)response["note"] +
                        " A numeric value is given in 'numeric_definite_value' instead; it " +
                        "answers this one interval and is not an antiderivative.";
            }

            return response;
        });
    }

    private static JsonObject Limit(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;
        var variable = S(args, "variable");
        var to = S(args, "to");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");
        if (string.IsNullOrWhiteSpace(to)) return Fail("'to' is required");

        if (!TryParse(to, "to", false, out var destination, out _, out var destError))
            return destError!;

        var side = (S(args, "side") ?? "both").ToLowerInvariant() switch
        {
            "left" => ApproachFrom.Left,
            "right" => ApproachFrom.Right,
            _ => ApproachFrom.BothSides,
        };

        return FromOutcome(Guard.Run(() =>
        {
            var raw = e.Limit(Var(variable!), destination, side);
            // Check for a decline BEFORE simplifying: an unevaluated limit(...) simplifies
            // to NaN, which would look like a wrong answer instead of a missing solver.
            return Guard.IsDeclined(raw.Stringize()) ? raw : raw.Simplify();
        }), r =>
        {
            var response = Respond(e, r, warnings);
            if ((string?)response["status"] == "suspect")
            {
                // NaN from a genuinely evaluated limit means "does not exist", which is a
                // real answer rather than a library failure.
                response["status"] = "no_limit";
                response["note"] = "The limit evaluated to NaN, which here means the limit " +
                    "does not exist (e.g. 1/x as x -> 0 from both sides).";
            }
            return response;
        });
    }

    private static JsonObject Evaluate(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var substitutions = new List<(string Name, Entity Value)>();
        if (args.TryGetPropertyValue("substitutions", out var subsNode) && subsNode is JsonObject subs)
        {
            foreach (var (key, value) in subs)
            {
                if (value is null) continue;
                if (!TryParse(value.GetValue<string>(), $"substitutions.{key}", false,
                        out var replacement, out _, out var subError))
                    return subError!;
                substitutions.Add((key, replacement));
            }
        }

        var digits = args.TryGetPropertyValue("digits", out var d) && d is not null
            ? d.GetValue<int>() : 0;
        if (digits is < 0 or > 1000) return Fail("'digits' must be between 1 and 1000");

        var input = e;
        return FromOutcome(Guard.Run(() =>
        {
            // Widen the library's working precision for this call only. Without it the
            // answer is computed at the default context and no amount of formatting can
            // recover digits that were never computed.
            var context = digits > 0
                ? new PeterO.Numbers.EContext(digits, PeterO.Numbers.ERounding.HalfEven,
                    -int.MaxValue, int.MaxValue, false)
                : MathS.Settings.DecimalPrecisionContext.Value;
            using var precision = MathS.Settings.DecimalPrecisionContext.Set(context);

            // Resolve calculus nodes BEFORE substituting. Substituting into
            // derivative(f(x), x) cannot work — x is bound there — so `d/dx sqrt(1+x) at
            // x=0` silently came back with x still free. Differentiate first, then plug in.
            var resolved = input.InnerSimplified;
            foreach (var (name, value) in substitutions)
                resolved = resolved.Substitute(Var(name), value);

            // Simplify, not Evaled: Evaled numericises aggressively, turning
            // sin(pi/3) + cos(pi/6) into a hundred decimal digits and throwing away the
            // exact sqrt(3) that is the whole reason to use a CAS.
            var exact = resolved.Simplify();

            string? approx = null;
            if (exact.EvaluableNumerical)
            {
                var complex = exact.EvalNumerical();
                approx = !complex.ImaginaryPart.EDecimal.IsZero
                    ? complex.Stringize()
                    : digits > 0
                        // Full computed precision, not a double — the whole point of asking.
                        ? complex.RealPart.EDecimal.ToString()
                        : complex.RealPart.EDecimal.ToDouble().ToString("R");
            }
            return (exact, approx);
        }), r =>
        {
            var response = Respond(input, r.exact, warnings);
            if (r.approx is not null) response["approximate"] = r.approx;
            else response["note"] = "The expression is not fully numeric — it still contains " +
                "free variables. Supply 'substitutions' to get a number.";
            return response;
        });
    }

    private static JsonObject VerifyEqual(JsonObject args)
    {
        if (!TryParse(S(args, "left"), "left", false, out var left, out var lw, out var le))
            return le!;
        if (!TryParse(S(args, "right"), "right", false, out var right, out var rw, out var re))
            return re!;

        return FromOutcome(Guard.Run(() =>
        {
            // Exact first: if the difference reduces to zero, that settles it. The zero test
            // has to look through any `provided` guard — the difference of two equal
            // expressions is routinely "0 provided x > 0", which is still zero.
            var difference = (left - right).Simplify();
            var exactlyZero = Numeric.IsZero(difference);

            bool? numerically = null;
            bool? wholeLine = null;

            // Trust, then verify the verifier. The exact path believes Simplify, and Simplify
            // can be unsound: it reduces sqrt(x^2) to x, so `sqrt(x^2) = x` comes back
            // "exact: difference simplified to 0" — while the library's own evaluator gives
            // sqrt(x^2) = 2 at x = -2. Sampling the ORIGINAL sides across the real line
            // catches a bad simplification, because it never goes through one.
            bool? conflict = null;
            if (exactlyZero)
            {
                try
                {
                    var direct = Numeric.Equal(left, right,
                        ["-3.1", "-1.7", "-0.4", "0.8", "2.6"]);
                    if (direct == false) conflict = true;
                }
                catch { /* no counter-evidence available */ }
            }

            if (!exactlyZero)
            {
                // Compare the DIFFERENCE against zero rather than the two sides against
                // each other. Simplify has already collapsed any unevaluated derivative(…)
                // or integral(…) node in the difference, whereas the raw sides may still
                // contain one — and an unevaluated node is not numerically evaluable, so
                // comparing the sides directly returns "inconclusive" for inputs that are
                // in fact decidable.
                try { numerically = Numeric.Equal(difference, 0); }
                catch { numerically = null; }

                // The positive-real points above cannot see a disagreement that only occurs
                // on the negatives (abs(x) vs x, say). A second pass across the whole real
                // line catches that.
                if (numerically == true)
                {
                    try
                    {
                        wholeLine = Numeric.Equal(difference, 0,
                            ["-3.1", "-1.7", "-0.4", "0.8", "2.6"]);
                    }
                    catch { wholeLine = null; }
                }
            }

            return (difference, exactlyZero, numerically, wholeLine, conflict);
        }), r =>
        {
            var equal = r.exactlyZero || r.numerically == true;
            // Inconclusive is reported as null, never as false: "we could not check" is not
            // the same claim as "they differ", and a caller acting on a bare false would be
            // acting on something the server never established.
            var conclusive = r.exactlyZero || r.numerically is not null;

            var response = new JsonObject
            {
                ["status"] = conclusive ? "solved" : "inconclusive",
                ["equal"] = conclusive ? equal : null,
                ["method"] = r.exactlyZero
                    ? "exact: left - right simplified to 0"
                    : r.numerically is null
                        ? "inconclusive: neither side could be evaluated at any sample point"
                        : "numeric: sampled at five positive real points, relative tolerance 1e-6",
                ["difference"] = r.difference.Stringize(),
                ["left_parsed"] = left.Stringize(),
                ["right_parsed"] = right.Stringize(),
                ["note"] = equal
                    ? "The two expressions agree."
                    : r.numerically is null
                        ? "Could not establish equality either way. Not proof of inequality."
                        : "The expressions differ. Check 'difference' to see by how much.",
                ["warnings"] = Warn([.. lw, .. rw]),
            };

            if (r.conflict == true)
            {
                response["status"] = "conflict";
                response["equal"] = false;
                response["conflict"] = true;
                response["note"] =
                    "Symbolic simplification says these are equal, but evaluating the two " +
                    "sides directly disagrees at sampled points on the real line. The " +
                    "direct evaluation is the more trustworthy of the two, since it does " +
                    "not pass through a rewrite. Treat them as NOT equal, and treat the " +
                    "simplification as suspect — sqrt(x^2) reducing to x rather than abs(x) " +
                    "is a known instance.";
            }
            else if (equal && r.wholeLine == false)
            {
                response["equal_only_on_positive_reals"] = true;
                response["note"] =
                    "The expressions agree for positive real inputs but disagree elsewhere " +
                    "on the real line — the usual cause is a logarithm, square root or " +
                    "absolute value. Treat them as equal only if your domain is restricted " +
                    "to the positives.";
            }

            return response;
        });
    }

    private static JsonObject TruthTable(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var names = A(args, "variables");
        var vars = names.Count > 0
            ? names.Select(Var).ToArray()
            : e.Vars.ToArray();

        if (vars.Length == 0) return Fail("no variables found in the expression");
        if (vars.Length > 12) return Fail($"{vars.Length} variables would need {1L << vars.Length} rows; cap is 12");

        return FromOutcome(Guard.Run(() =>
        {
            var table = MathS.Boolean.BuildTruthTable(e, vars);
            var satisfying = MathS.SolveBooleanTable(e, vars);
            return (table, satisfying);
        }), r =>
        {
            var order = new JsonArray();
            foreach (var v in vars) order.Add(v.Stringize());

            var response = new JsonObject
            {
                ["status"] = r.table is null ? "failed" : "solved",
                ["parsed"] = e.Stringize(),
                ["variable_order"] = order,
                ["truth_table"] = r.table?.ToString(multilineFormat: true),
                ["satisfiable"] = r.satisfying is not null,
                ["satisfying_assignments"] = r.satisfying?.ToString(multilineFormat: true),
            };
            if (r.satisfying is null)
                response["note"] = "No assignment satisfies this expression — it is a contradiction.";
            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject SolveSystem(JsonObject args)
    {
        var equations = A(args, "equations");
        var variables = A(args, "variables");
        if (equations.Count == 0) return Fail("'equations' must not be empty");
        if (variables.Count == 0) return Fail("'variables' must not be empty");

        var parsed = new List<Entity>();
        var warnings = new List<string>();
        foreach (var eq in equations)
        {
            if (!TryParse(eq, "equations", false, out var e, out var w, out var error))
                return error!;

            // EquationSystem wants each equation in "= 0" form, and handed an equality it
            // finds no solution, so normalise here: writing 'x + y = 3' is the natural thing
            // for a caller to do, and it should just work.
            // https://github.com/asc-community/AngouriMath/issues/1673
            parsed.Add(e is Equalsf equality ? equality.Left - equality.Right : e);
            warnings.AddRange(w);
        }

        return FromOutcome(Guard.Run(() =>
            MathS.Equations([.. parsed]).Solve([.. variables.Select(Var)])), matrix =>
        {
            var order = new JsonArray();
            foreach (var v in variables) order.Add(v);

            var response = new JsonObject
            {
                ["status"] = matrix is null ? "declined" : "solved",
                ["variable_order"] = order,
                ["solutions"] = matrix?.ToString(multilineFormat: true),
                ["solution_count"] = matrix?.RowCount,
            };

            if (matrix is null)
                response["note"] = "No solution was returned. A nonlinear system can come " +
                    "back with nothing even when it has a solution, so absence here is not " +
                    "proof that the system is unsolvable.";

            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject CheckSteps(JsonObject args)
    {
        var raw = A(args, "steps");
        if (raw.Count < 2) return Fail("'steps' needs at least two entries to compare");

        var steps = new List<Entity>();
        var warnings = new List<string>();
        foreach (var step in raw)
        {
            if (!TryParse(step, "steps", false, out var e, out var w, out var error))
                return error!;
            steps.Add(e);
            warnings.AddRange(w);
        }

        return FromOutcome(Guard.Run(() => Analysis.CheckSteps(steps)), checks =>
        {
            var rows = new JsonArray();
            int? firstBad = null;

            foreach (var check in checks)
            {
                rows.Add(new JsonObject
                {
                    ["step"] = check.Index,
                    ["from"] = check.From,
                    ["to"] = check.To,
                    ["equal"] = check.Equal,
                    ["method"] = check.Method,
                    ["difference"] = check.Difference,
                });
                if (firstBad is null && check.Equal == false) firstBad = check.Index;
            }

            var response = new JsonObject
            {
                ["status"] = "solved",
                ["all_steps_valid"] = firstBad is null,
                ["first_invalid_step"] = firstBad,
                ["steps"] = rows,
                ["note"] = firstBad is null
                    ? "Every step preserves the value. The working is sound — note this " +
                      "checks equality, not whether the steps are the clearest route."
                    : $"Step {firstBad} changes the value. Everything before it is sound, " +
                      "so the error is introduced exactly there.",
            };
            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject DomainCheck(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        return FromOutcome(Guard.Run(() =>
        {
            Entity simplified;
            try { simplified = e.Simplify(); }
            catch { simplified = e; }

            return (simplified,
                    Analysis.Conditions(simplified),
                    Analysis.Hazards(e),
                    Analysis.Reality(e));
        }), r =>
        {
            var conditions = new JsonArray();
            foreach (var c in r.Item2) conditions.Add(c);

            var hazards = new JsonArray();
            foreach (var h in r.Item3)
                hazards.Add(new JsonObject { ["in"] = h.Where, ["risk"] = h.Risk });

            var complexAt = new JsonArray();
            foreach (var p in r.Item4.ComplexAt) complexAt.Add(p);
            var undefinedAt = new JsonArray();
            foreach (var p in r.Item4.UndefinedAt) undefinedAt.Add(p);

            var response = new JsonObject
            {
                ["status"] = "solved",
                ["parsed"] = e.Stringize(),
                ["simplified"] = r.simplified.Stringize(),
                ["conditions"] = conditions,
                ["hazards"] = hazards,
                ["not_real_at"] = complexAt,
                ["undefined_at"] = undefinedAt,
                ["points_sampled"] = r.Item4.Sampled,
            };

            if (r.Item2.Count > 0)
                response["conditions_note"] = "AngouriMath derived these guards itself. They " +
                    "are part of the answer — dropping them makes the result wrong on the " +
                    "excluded points.";

            if (r.Item4.ComplexAt.Count > 0 || r.Item4.UndefinedAt.Count > 0)
                response["reality_note"] = "The expression leaves the reals at some sampled " +
                    "points. If it stands for a physical quantity, constrain the input " +
                    "range before using it — pass the constraint to am_solve alongside the " +
                    "equation.";

            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject Represent(JsonObject args)
    {
        var operation = S(args, "operation");
        if (string.IsNullOrWhiteSpace(operation)) return Fail("'operation' is required");

        return operation switch
        {
            "base" => RepresentBase(args),
            "fixed_point" => RepresentFixed(args),
            "from_fixed_point" => RepresentFromFixed(args),
            "ieee754" => RepresentIeee(args),
            "polar" => RepresentPolar(args),
            "rectangular" => RepresentRectangular(args),
            _ => Fail($"unknown operation '{operation}'"),
        };
    }

    private static JsonObject RepresentBase(JsonObject args)
    {
        var value = S(args, "value");
        if (string.IsNullOrWhiteSpace(value)) return Fail("'value' is required");

        var fromBase = args.TryGetPropertyValue("from_base", out var f) && f is not null
            ? f.GetValue<int>() : 10;
        if (!args.TryGetPropertyValue("to_base", out var t) || t is null)
            return Fail("'to_base' is required for operation 'base'");
        var toBase = t.GetValue<int>();

        if (fromBase is < 2 or > 36) return Fail("'from_base' must be between 2 and 36");
        if (toBase is < 2 or > 36) return Fail("'to_base' must be between 2 and 36");

        return FromOutcome(Guard.Run(() =>
        {
            var asDecimal = fromBase == 10
                ? value!.ToEntity()
                : MathS.FromBaseN(value!, fromBase);

            var real = (Number.Real)asDecimal.Evaled;
            var converted = toBase == 10 ? real.Stringize() : MathS.ToBaseN(real, toBase);
            return (real, converted);
        }), r => new JsonObject
        {
            ["status"] = "solved",
            ["operation"] = "base",
            ["input"] = value,
            ["from_base"] = fromBase,
            ["to_base"] = toBase,
            ["decimal"] = r.real.Stringize(),
            ["result"] = r.converted,
        });
    }

    /// <summary>Evaluate an expression to an exact decimal, or explain why not.</summary>
    private static bool TryDecimal(JsonObject args, string key,
        out PeterO.Numbers.EDecimal value, out JsonObject? error)
    {
        value = PeterO.Numbers.EDecimal.Zero;
        if (!TryParse(S(args, key), key, false, out var e, out _, out error)) return false;

        if (!e.EvaluableNumerical)
        {
            error = Fail($"'{key}' must evaluate to a number; got '{e.Stringize()}'");
            return false;
        }

        var complex = e.EvalNumerical();
        if (!complex.ImaginaryPart.EDecimal.IsZero)
        {
            error = Fail($"'{key}' must be real; got '{complex.Stringize()}'");
            return false;
        }

        value = complex.RealPart.EDecimal;
        return true;
    }

    private static JsonObject RepresentFixed(JsonObject args)
    {
        if (!TryDecimal(args, "value", out var value, out var error)) return error!;

        var totalBits = args.TryGetPropertyValue("total_bits", out var tb) && tb is not null
            ? tb.GetValue<int>() : 16;
        if (!args.TryGetPropertyValue("fraction_bits", out var fb) || fb is null)
            return Fail("'fraction_bits' is required for 'fixed_point'");
        var fractionBits = fb.GetValue<int>();
        var signed = B(args, "signed", true);

        if (totalBits is < 2 or > 128) return Fail("'total_bits' must be between 2 and 128");
        if (fractionBits < 0 || fractionBits > totalBits)
            return Fail("'fraction_bits' must be between 0 and 'total_bits'");

        return FromOutcome(
            Guard.Run(() => Representations.ToFixed(value, totalBits, fractionBits, signed)),
            fixedPoint =>
            {
                var response = new JsonObject
                {
                    ["status"] = fixedPoint.Saturated ? "suspect" : "solved",
                    ["operation"] = "fixed_point",
                    ["format"] = $"Q{totalBits - fractionBits - (signed ? 1 : 0)}.{fractionBits}"
                                 + $" in {totalBits} bits, {(signed ? "signed" : "unsigned")}",
                    ["raw"] = fixedPoint.Raw.ToString(),
                    ["raw_hex"] = "0x" + fixedPoint.Raw.Abs().ToRadixString(16).ToUpperInvariant(),
                    ["represented_value"] = fixedPoint.Represented.Stringize(),
                    ["absolute_error"] = fixedPoint.Error.ToString(),
                    ["relative_error"] = fixedPoint.RelativeError.ToString(),
                    ["resolution"] = fixedPoint.Resolution.ToString(),
                    ["representable_range"] =
                        $"[{fixedPoint.Min}, {fixedPoint.Max}] raw",
                    ["saturated"] = fixedPoint.Saturated,
                };

                if (fixedPoint.Saturated)
                    response["note"] = "The value does not fit and was clamped to the limit. " +
                        "On most processors this is a design-time bug rather than something " +
                        "to accept — widen the word, or move the binary point.";
                else
                    response["note"] = "'represented_value' is exact: an integer over a power " +
                        "of two is a rational, so there is no second rounding. Use " +
                        "am_compare_numeric to see what this quantisation costs across your " +
                        "operating range rather than at a single point.";

                return response;
            });
    }

    private static JsonObject RepresentFromFixed(JsonObject args)
    {
        if (!TryDecimal(args, "value", out var value, out var error)) return error!;
        if (!args.TryGetPropertyValue("fraction_bits", out var fb) || fb is null)
            return Fail("'fraction_bits' is required for 'from_fixed_point'");
        var fractionBits = fb.GetValue<int>();
        if (fractionBits is < 0 or > 128) return Fail("'fraction_bits' must be between 0 and 128");

        var raw = value.ToEInteger();

        return FromOutcome(
            Guard.Run(() => Representations.FromFixed(raw, fractionBits)),
            v => new JsonObject
            {
                ["status"] = "solved",
                ["operation"] = "from_fixed_point",
                ["raw"] = raw.ToString(),
                ["fraction_bits"] = fractionBits,
                ["result"] = v.Stringize(),
                ["approximate"] = v.EvaluableNumerical
                    ? v.EvalNumerical().RealPart.EDecimal.ToDouble().ToString("R")
                    : null,
            });
    }

    private static JsonObject RepresentIeee(JsonObject args)
    {
        if (!TryDecimal(args, "value", out var value, out var error)) return error!;
        var single = (S(args, "precision") ?? "double") == "single";

        return FromOutcome(
            Guard.Run(() => Representations.Decompose(value, single)),
            ieee => new JsonObject
            {
                ["status"] = "solved",
                ["operation"] = "ieee754",
                ["precision"] = single ? "single (32-bit)" : "double (64-bit)",
                ["bits"] = ieee.Bits,
                ["hex"] = ieee.Hex,
                ["sign"] = ieee.Sign,
                ["exponent_raw"] = ieee.RawExponent,
                ["exponent_unbiased"] = ieee.UnbiasedExponent,
                ["mantissa_hex"] = ieee.MantissaHex,
                ["exact_value"] = ieee.ExactValue,
                ["error_vs_input"] = ieee.Error.ToString(),
                ["classification"] = ieee.Classification,
                ["note"] = "'exact_value' is what the format actually stores, not a rounded " +
                    "display of it. That difference is the whole reason 0.1 + 0.2 does not " +
                    "equal 0.3.",
            });
    }

    private static JsonObject RepresentPolar(JsonObject args)
    {
        if (!TryParse(S(args, "value"), "value", false,
                out var e, out var warnings, out var error))
            return error!;

        return FromOutcome(Guard.Run(() =>
        {
            var evaluated = e.Evaled;
            if (evaluated is not Number.Complex complex)
                throw new ArgumentException($"'{e.Stringize()}' is not a number");

            return Representations.ToPolar(complex.RealPart, complex.ImaginaryPart);
        }), polar =>
        {
            var response = new JsonObject
            {
                ["status"] = "solved",
                ["operation"] = "polar",
                ["magnitude"] = polar.Magnitude.Stringize(),
                ["phase_radians"] = polar.Phase.Stringize(),
                ["phase_degrees"] = polar.PhaseDegrees.Stringize(),
                ["note"] = "Phase is quadrant-corrected — arctan alone would report the same " +
                    "angle for 1+i and -1-i.",
            };
            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject RepresentRectangular(JsonObject args)
    {
        if (!TryParse(S(args, "magnitude"), "magnitude", false,
                out var magnitude, out var warnings, out var magnitudeError))
            return magnitudeError!;
        if (!TryParse(S(args, "phase"), "phase", false,
                out var phase, out var more, out var phaseError))
            return phaseError!;
        warnings.AddRange(more);

        return FromOutcome(
            Guard.Run(() => Representations.FromPolar(magnitude, phase)),
            rectangular =>
            {
                var response = new JsonObject
                {
                    ["status"] = "solved",
                    ["operation"] = "rectangular",
                    ["result"] = rectangular.Stringize(),
                    ["latex"] = rectangular.Latexize(),
                };
                if (warnings.Count > 0) response["warnings"] = Warn(warnings);
                return response;
            });
    }

    private static JsonObject MatrixOp(JsonObject args)
    {
        var operation = S(args, "operation");
        if (string.IsNullOrWhiteSpace(operation)) return Fail("'operation' is required");

        args.TryGetPropertyValue("matrix", out var aNode);
        var a = Matrices.Parse(aNode, "matrix");
        if (a.Value is null) return Fail(a.Error!);

        var needsSecond = operation is "multiply" or "add" or "subtract" or "tensor_product";
        Matrix? b = null;
        var warnings = new List<string>(a.Warnings);

        if (needsSecond)
        {
            args.TryGetPropertyValue("matrix_b", out var bNode);
            var parsedB = Matrices.Parse(bNode, "matrix_b");
            if (parsedB.Value is null)
                return Fail(parsedB.Error ?? $"'{operation}' needs 'matrix_b'");
            b = parsedB.Value;
            warnings.AddRange(parsedB.Warnings);
        }

        var exponent = args.TryGetPropertyValue("exponent", out var e) && e is not null
            ? e.GetValue<int>() : -1;
        if (operation == "power" && exponent < 0)
            return Fail("'power' needs a non-negative 'exponent'");

        var guards = new List<string>();

        return FromOutcome(Guard.Run<object?>(() => operation switch
        {
            "determinant" => a.Value.Determinant is { } det ? CleanScalar(det, guards) : null,
            "inverse" => a.Value.Inverse is { } inv ? CleanGrid(Simplified(inv), guards) : null,
            "transpose" => Simplified(a.Value.T),
            "rank" => a.Value.Rank,
            "rref" => Simplified(a.Value.ReducedRowEchelonForm),
            "trace" => a.Value.Trace?.Simplify(),
            "multiply" => Simplified(a.Value * b!),
            "add" => Simplified(a.Value + b!),
            "subtract" => Simplified(a.Value - b!),
            "tensor_product" => Simplified(Matrix.TensorProduct(a.Value, b!)),
            "power" => Simplified(a.Value.Pow(exponent)),
            _ => throw new ArgumentException($"unknown operation '{operation}'"),
        }), value =>
        {
            var response = new JsonObject
            {
                ["status"] = value is null ? "declined" : "solved",
                ["operation"] = operation,
                ["shape"] = $"{a.Value.RowCount}x{a.Value.ColumnCount}",
            };

            switch (value)
            {
                case Matrix m:
                    response["result"] = Matrices.Render(m);
                    response["shape_out"] = $"{m.RowCount}x{m.ColumnCount}";
                    response["pretty"] = m.ToString(multilineFormat: true);
                    response["latex"] = m.Latexize();
                    break;
                case Entity scalar:
                    response["result"] = scalar.Stringize();
                    response["latex"] = scalar.Latexize();
                    break;
                case int rank:
                    response["result"] = rank;
                    break;
                case null:
                    response["note"] = operation switch
                    {
                        "determinant" or "trace" => "The matrix is not square.",
                        "inverse" => "No inverse: the matrix is singular or not square.",
                        _ => "The operation produced no result.",
                    };
                    break;
            }

            if (guards.Count > 0)
            {
                var dropped = new JsonArray();
                foreach (var g in guards.Distinct()) dropped.Add(g);
                response["dropped_guards"] = dropped;
                response["dropped_guards_note"] = Matrices.GuardNote;
                if (operation == "inverse")
                    response["inverse_condition"] =
                        "The inverse exists exactly when the determinant is non-zero; that " +
                        "is the real condition, not the pivot guards listed above.";
            }

            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });

        static Entity CleanScalar(Entity e, List<string> sink)
        {
            var (value, found) = Matrices.Clean(e);
            sink.AddRange(found);
            return value;
        }

        static Matrix CleanGrid(Matrix m, List<string> sink)
        {
            var (value, found) = Matrices.Clean(m);
            sink.AddRange(found);
            return value;
        }

        static Matrix Simplified(Matrix m)
        {
            // Products and tensor products come back littered with `1/sqrt(2) * 1` and the
            // like — correct, unreadable, and awkward to compare. Tidy each entry, but never
            // at the cost of losing one.
            var grid = new Entity[m.RowCount, m.ColumnCount];
            for (var i = 0; i < m.RowCount; i++)
                for (var j = 0; j < m.ColumnCount; j++)
                {
                    try { grid[i, j] = m[i, j].Simplify(); }
                    catch { grid[i, j] = m[i, j]; }
                }
            return MathS.Matrix(grid);
        }
    }

    private static JsonObject Eigenvalues(JsonObject args)
    {
        args.TryGetPropertyValue("matrix", out var node);
        var parsed = Matrices.Parse(node, "matrix");
        if (parsed.Value is null) return Fail(parsed.Error!);

        var a = parsed.Value;
        if (a.RowCount != a.ColumnCount)
            return Fail($"eigenvalues need a square matrix; this one is {a.RowCount}x{a.ColumnCount}");

        return FromOutcome(Guard.Run(() => Matrices.Compute(a, Var("lambda"))), eigen =>
        {
            var raw = eigen.Roots?.Stringize();
            var declined = eigen.Roots is null
                           || raw is null
                           || Guard.IsDeclined(raw)
                           || raw.Contains("solve(", StringComparison.Ordinal);

            var response = new JsonObject
            {
                ["status"] = declined ? "declined" : "solved",
                ["shape"] = $"{a.RowCount}x{a.ColumnCount}",
                ["characteristic_polynomial"] = eigen.CharacteristicPolynomial.Stringize(),
                ["characteristic_polynomial_latex"] = eigen.CharacteristicPolynomial.Latexize(),
                ["eigenvalues"] = raw,
            };

            if (eigen.Roots is Set.FiniteSet finite)
            {
                var values = new JsonArray();
                foreach (var root in finite) values.Add(root.Stringize());
                response["eigenvalue_list"] = values;
                response["count"] = finite.Count;
                response["note"] = "Repeated roots appear once — this is the set of distinct " +
                    "eigenvalues, not a list with algebraic multiplicity.";
            }

            if (declined)
                response["note"] = "The characteristic polynomial could not be solved in " +
                    "closed form. Above degree 4 with symbolic entries that is expected and " +
                    "not a defect: no general radical solution exists (Abel-Ruffini). The " +
                    "polynomial itself is returned and is still useful.";

            if (eigen.DroppedGuards.Count > 0)
            {
                var dropped = new JsonArray();
                foreach (var g in eigen.DroppedGuards) dropped.Add(g);
                response["dropped_guards"] = dropped;
                response["dropped_guards_note"] =
                    "The determinant routine divides by pivots and leaves a `provided` guard " +
                    "for each. On a symbolic matrix those are artefacts of the algorithm, not " +
                    "mathematics — [[0,J],[J,0]] would otherwise report its eigenvalues as " +
                    "'J provided not J = 0', excluding a perfectly valid case. They are " +
                    "listed here rather than silently discarded.";
            }

            if (parsed.Warnings.Count > 0) response["warnings"] = Warn(parsed.Warnings);
            return response;
        });
    }

    private static JsonObject CompareNumeric(JsonObject args)
    {
        if (!TryParse(S(args, "reference"), "reference", false,
                out var reference, out var warnings, out var referenceError))
            return referenceError!;
        if (!TryParse(S(args, "approximation"), "approximation", false,
                out var approximation, out var moreWarnings, out var approximationError))
            return approximationError!;
        warnings.AddRange(moreWarnings);

        var variable = S(args, "variable");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");

        if (!args.TryGetPropertyValue("from", out var f) || f is null) return Fail("'from' is required");
        if (!args.TryGetPropertyValue("to", out var t) || t is null) return Fail("'to' is required");
        var from = f.GetValue<double>();
        var to = t.GetValue<double>();
        if (double.IsNaN(from) || double.IsNaN(to)) return Fail("'from' and 'to' must be numbers");
        if (to < from) return Fail("'to' must not be less than 'from'");

        var samples = args.TryGetPropertyValue("samples", out var s) && s is not null
            ? s.GetValue<int>() : 200;
        if (samples is < 2 or > 10000) return Fail("'samples' must be between 2 and 10000");

        return FromOutcome(
            Guard.Run(() => Numeric.Diverge(reference, approximation, Var(variable!),
                from, to, samples)),
            divergence =>
            {
                if (divergence is null)
                    return new JsonObject
                    {
                        ["status"] = "declined",
                        ["note"] = "Neither expression could be evaluated at any sample " +
                            "point in that interval. Check for free variables other than " +
                            $"'{variable}', or an interval where both are undefined.",
                    };

                var response = new JsonObject
                {
                    ["status"] = "solved",
                    ["interval"] = $"[{from}, {to}]",
                    ["samples_used"] = divergence.Sampled,
                    ["samples_skipped"] = divergence.Skipped,
                    ["max_absolute_error"] = divergence.MaxAbsolute,
                    ["max_absolute_error_at"] = divergence.MaxAbsoluteAt,
                    ["max_relative_error"] = divergence.MaxRelative,
                    ["max_relative_error_at"] = divergence.MaxRelativeAt,
                    ["rms_error"] = divergence.RootMeanSquare,
                    ["samples_complex"] = divergence.Complex,
                };

                if (divergence.Complex > 0)
                    response["complex_note"] =
                        $"{divergence.Complex} of {divergence.Sampled} evaluated points were " +
                        "NOT real — a logarithm or even root took its principal branch on a " +
                        "negative argument. The errors above compare complex magnitudes, " +
                        "which is probably not what you meant if this stands for a physical " +
                        "quantity. Narrow the interval, or run am_domain_check first.";

                if (divergence.Skipped > 0)
                    response["skipped_note"] =
                        $"{divergence.Skipped} of {samples} points were undefined for one " +
                        "side or the other and were excluded. A large count here means the " +
                        "interval crosses a singularity or leaves the reals — the error " +
                        "figures describe only the part that was evaluable.";

                response["interpretation_note"] =
                    "Sampling proves nothing between the samples. A smooth approximation is " +
                    "well characterised by this; one with a narrow spike is not. Raise " +
                    "'samples', or use am_domain_check to find where the reference misbehaves.";

                if (warnings.Count > 0) response["warnings"] = Warn(warnings);
                return response;
            });
    }

    private static JsonObject Substitute(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        if (args["substitutions"] is not JsonObject subs || subs.Count == 0)
            return Fail("'substitutions' must be a non-empty map of variable -> expression");

        var replacements = new List<(string Name, Entity Value)>();
        foreach (var (key, value) in subs)
        {
            if (value is null) continue;
            if (!TryParse(value.GetValue<string>(), $"substitutions.{key}", false,
                    out var replacement, out var more, out var subError))
                return subError!;
            replacements.Add((key, replacement));
            warnings.AddRange(more);
        }

        var alsoSimplify = B(args, "simplify");

        return FromOutcome(Guard.Run(() =>
        {
            var result = e;
            foreach (var (name, value) in replacements)
                result = result.Substitute(Var(name), value);

            // Substitution alone is deliberately structural — no InnerSimplified, no
            // Evaled. Seeing the unreduced shape is the entire reason to use this rather
            // than am_evaluate.
            return alsoSimplify ? result.Simplify() : result;
        }), r =>
        {
            var response = Respond(e, r, warnings);
            var applied = new JsonArray();
            foreach (var (name, value) in replacements)
                applied.Add($"{name} := {value.Stringize()}");
            response["applied"] = applied;

            if (!alsoSimplify)
                response["note"] = "Substituted structurally, not simplified — pass " +
                    "simplify: true, or send the result to am_simplify, if you want it reduced.";

            var remaining = new JsonArray();
            foreach (var v in r.Vars) remaining.Add(v.Stringize());
            response["free_variables"] = remaining;

            return response;
        });
    }

    private static JsonObject Rewrite(JsonObject args, bool expand)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        return FromOutcome(
            Guard.Run(() => expand ? e.Expand() : e.Factorize()),
            r => Respond(e, r, warnings));
    }

    private static JsonObject Series(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var variable = S(args, "variable");
        if (string.IsNullOrWhiteSpace(variable)) return Fail("'variable' is required");

        if (!args.TryGetPropertyValue("degree", out var d) || d is null)
            return Fail("'degree' is required");
        var degree = d.GetValue<int>();
        if (degree is < 1 or > 30) return Fail("'degree' must be between 1 and 30");

        var aroundText = S(args, "around");
        Entity? around = null;
        if (!string.IsNullOrWhiteSpace(aroundText))
        {
            if (!TryParse(aroundText, "around", false, out var point, out _, out var pointError))
                return pointError!;
            around = point;
        }

        return FromOutcome(Guard.Run(() =>
        {
            var v = Var(variable!);
            var raw = around is null
                ? MathS.Series.Maclaurin(e, degree, v)
                : MathS.Series.Taylor(e, degree, (v, around));

            // The raw series keeps factorials unevaluated (`... / 3!`), so it is unreadable
            // until reduced.
            return raw.InnerSimplified.Simplify();
        }), r =>
        {
            var response = Respond(e, r, warnings);
            response["degree"] = degree;
            response["around"] = around?.Stringize() ?? "0";
            response["kind"] = around is null ? "Maclaurin" : "Taylor";
            response["truncation_note"] =
                "This is a truncated series: it agrees with the original near the expansion " +
                "point and diverges away from it. There is no error bound here — treat it as " +
                "an approximation whose validity you must argue separately.";
            return response;
        });
    }

    private static JsonObject NumberTheory(JsonObject args)
    {
        var operation = S(args, "operation");
        if (string.IsNullOrWhiteSpace(operation)) return Fail("'operation' is required");

        if (!TryParse(S(args, "value"), "value", false, out var e, out var warnings, out var error))
            return error!;

        if (e.Evaled is not Number.Integer a)
            return Fail($"'value' must evaluate to an integer; got '{e.Stringize()}'");

        Number.Integer? b = null;
        if (operation == "gcd")
        {
            if (!TryParse(S(args, "value_b"), "value_b", false,
                    out var second, out var moreWarnings, out var secondError))
                return secondError!;
            if (second.Evaled is not Number.Integer parsedB)
                return Fail("'value_b' must evaluate to an integer");
            b = parsedB;
            warnings.AddRange(moreWarnings);
        }

        return FromOutcome(Guard.Run<object>(() => operation switch
        {
            "factorize" => MathS.NumberTheory.Factorize(a).ToList(),
            "totient" => MathS.NumberTheory.Phi(a).Evaled,
            "gcd" => MathS.NumberTheory.GreatestCommonDivisor(a, b!),
            "count_divisors" => MathS.NumberTheory.CountDivisors(a),
            "is_prime" => a.IsPrime,
            _ => throw new ArgumentException($"unknown operation '{operation}'"),
        }), value =>
        {
            var response = new JsonObject
            {
                ["status"] = "solved",
                ["operation"] = operation,
                ["input"] = a.Stringize(),
            };

            if (value is List<(Number.Integer Prime, Number.Integer Power)> factors)
            {
                var listed = new JsonArray();
                foreach (var (prime, power) in factors)
                    listed.Add(new JsonObject
                    {
                        ["prime"] = prime.Stringize(),
                        ["power"] = power.Stringize(),
                    });
                response["factors"] = listed;
                response["result"] = string.Join(" * ", factors.Select(f =>
                    f.Power.Stringize() == "1"
                        ? f.Prime.Stringize()
                        : $"{f.Prime.Stringize()}^{f.Power.Stringize()}"));
            }
            else
            {
                response["result"] = value.ToString();
            }

            if (warnings.Count > 0) response["warnings"] = Warn(warnings);
            return response;
        });
    }

    private static JsonObject ClassifyFormula(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var (guesses, signatures, features) = Classify.Analyse(e);

        var ranked = new JsonArray();
        foreach (var g in guesses)
            ranked.Add(new JsonObject
            {
                ["field"] = g.Field,
                ["score"] = g.Score,
                ["because"] = g.Because,
            });

        var recognised = new JsonArray();
        foreach (var s in signatures) recognised.Add(s);

        var observed = new JsonArray();
        foreach (var f in features) observed.Add(f);

        var symbols = new JsonArray();
        foreach (var v in e.Vars) symbols.Add(v.Stringize());

        var response = new JsonObject
        {
            ["status"] = "solved",
            ["parsed"] = e.Stringize(),
            ["symbols"] = symbols,
            ["features"] = observed,
            ["recognised_forms"] = recognised,
            ["likely_fields"] = ranked,
            ["caveat"] = "Inferred from symbol naming conventions, not from meaning. The " +
                "same letter means different things in different fields; report this as a " +
                "guess, not a fact.",
        };

        if (guesses.Count == 0)
            response["note"] = "Nothing distinctive enough to guess from. The symbols are " +
                "too generic, or the formula is pure mathematics with no domain markers.";

        if (warnings.Count > 0) response["warnings"] = Warn(warnings);
        return response;
    }

    private static JsonObject ToSympy(JsonObject args)
    {
        if (!TryParse(S(args, "expression"), "expression", false,
                out var e, out var warnings, out var error))
            return error!;

        var response = new JsonObject
        {
            ["status"] = "solved",
            ["parsed"] = e.Stringize(),
            ["sympy"] = MathS.ToSympyCode(e),
        };
        if (warnings.Count > 0) response["warnings"] = Warn(warnings);
        return response;
    }
}
