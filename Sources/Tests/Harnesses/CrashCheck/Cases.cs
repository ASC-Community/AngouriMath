//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AngouriMath;
using AngouriMath.Extensions;

namespace CrashCheck;

/// <summary>What is run against what. A case is one expression put through one operation.</summary>
sealed record Case(string Expression, string Operation)
{
    public override string ToString() => $"{Operation}::{Expression}";
}

/// <summary>
/// The case list, built rather than written down.
/// </summary>
/// <remarks>
/// The expressions come from the <see cref="Entity"/> node types found by reflection, so a node
/// added later is covered without anyone having to remember this file -- which is the whole
/// reason a node's first appearance in a release has twice been the thing that crashed. They are
/// then composed to a small depth, because what kills the process is nearly always a
/// composition rather than a leaf: the recorded stack overflow was integration by parts feeding
/// itself, not any one node.
/// </remarks>
static class Cases
{
    /// The operations a case can name. Each is a whole pipeline rather than a single rule.
    public static readonly string[] Operations =
    {
        "simplify", "expand", "factorize", "eval", "innersimplify",
        "differentiate", "integrate", "limit", "solve", "alternate",
        "stringize-roundtrip", "latexize", "compile", "domain",
    };

    /// <summary>Every concrete <see cref="Entity"/> node the library has, one instance each,
    /// printed so that the child process can parse it back from its argument.</summary>
    public static List<string> NodeInstances(out List<string> uninstantiable)
    {
        var leaves = new Entity[] { "x", 2, "1/2", "-3", "y", MathS.pi, MathS.i };
        var printed = new List<string>();
        uninstantiable = new List<string>();

        foreach (var type in ConcreteEntityTypes())
        {
            var built = false;
            // The shortest constructor first: a node wants as few arguments as it can take,
            // and the ones with extra flags -- an interval's open/closed pair, for instance --
            // are covered by the written shapes below instead.
            foreach (var constructor in type.GetConstructors()
                         .OrderBy(c => c.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0 || parameters.Length > 3) continue;
                if (!parameters.All(p => p.ParameterType == typeof(Entity)
                                         || typeof(Entity).IsAssignableFrom(p.ParameterType))) continue;
                try
                {
                    var arguments = parameters
                        .Select((p, i) => Convert(leaves[i % leaves.Length], p.ParameterType))
                        .ToArray();
                    if (arguments.Any(a => a is null)) continue;
                    var node = (Entity)constructor.Invoke(arguments);
                    var text = node.Stringize();
                    // It has to survive the round trip through text, since that is how it
                    // reaches the child. A node whose printed form does not parse is itself a
                    // finding, and StringizeRoundTripTest in the library is where that belongs.
                    _ = text.ToEntity();
                    printed.Add(text);
                    built = true;
                    break;
                }
                catch
                {
                    // Try the next constructor.
                }
            }
            if (!built) uninstantiable.Add(type.Name);
        }
        return printed.Distinct().ToList();
    }

    static object Convert(Entity leaf, Type wanted)
    {
        if (wanted == typeof(Entity)) return leaf;
        if (wanted == typeof(Entity.Variable)) return (Entity.Variable)"x";
        try
        {
            return wanted.IsInstanceOfType(leaf) ? leaf : null;
        }
        catch
        {
            return null;
        }
    }

    static IEnumerable<Type> ConcreteEntityTypes()
    {
        var pending = new Queue<Type>();
        pending.Enqueue(typeof(Entity));
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var nested in current.GetNestedTypes(BindingFlags.Public))
            {
                pending.Enqueue(nested);
                if (!nested.IsAbstract && typeof(Entity).IsAssignableFrom(nested))
                    yield return nested;
            }
        }
    }

    /// <summary>
    /// Shapes that a constructor cannot reach, or that are worth having because something has
    /// already died on their kind: recursion that feeds itself, a binder, a set builder, a
    /// condition, and the deep nesting that turns a linear pass into an exponential one.
    /// </summary>
    public static readonly string[] WrittenShapes =
    {
        // Integration by parts feeding itself is the recorded crash.
        "x * ln(x)", "x2 * ln(x)", "x3 * ln(x)", "x * e ^ x", "x2 * sin(x)",
        "x ^ 4 * ln(x) * sin(x)", "ln(ln(ln(x)))", "e ^ e ^ e ^ x",
        // A rewrite whose output re-matches its own pattern.
        "1 / (1 / (1 / x))", "sqrt(sqrt(sqrt(x)))", "((x ^ 2) ^ 2) ^ 2",
        "(x + 1) ^ 8", "(x + y + 1) ^ 5", "(a + b + c + d) ^ 4",
        // The mathematical constants, free and bound. A bound one is a variable and a free one is
        // not, which is what Entity.Constant is for -- and only the free one is reachable by
        // construction, so the node type is otherwise uncovered here. #984
        "pi", "e", "sin(pi) + ln(e)", "sum(pi, pi, 1, 3)", "derivative(e ^ 2, e)", "{ e : e > 0 }",
        // Binders and conditions, where #878 found a condition escaping its scope.
        "{ x : x > 0 }", "{ x : x = x }", "{ x : 1 = 1 }", "{ x : sin(x) > 0 and x in RR }",
        "x provided x > 0", "x provided false", "(x provided x > 0) + 1",
        "piecewise(1 provided x > 0, 2 provided x <= 0)",
        // Sets and intervals.
        "{ 1, 2, 3 }", "[0; 1]", "(0; 1)", "RR", "CC", "ZZ", "QQ", "BB", "{ 1, 2 } \\/ [0; 5]",
        "x in ZZ", "x in QQ", "x in BB", "[0; 1) \\/ (1; 2]",
        "{ 1, 2 } /\\ [0; 5]", "{ x : x in RR } \\ { 0 }",
        // Matrices, including the shapes that made Simplify stop descending.
        "[[1, 2], [3, 4]]", "[1, 2, 3]", "[[x, y], [y, x]]",
        // Lambdas and applications, added late and with no parser support for years.
        "x -> x + 1", "(x -> x + 1) applied to 2",
        // Nodes whose evaluation reaches for a limit, which reaches for evaluation.
        "limit(x!, x, +oo)", "limit(x ^ x, x, 0)", "limit(floor(x), x, 0)",
        "limit(sgn(x) / x, x, 0)", "derivative(abs(x), x)", "integral(1 / x, x)",
        "integral(integral(x, x), x)", "derivative(derivative(sin(x), x), x)",
        // The power form of the derivative, where a derivative that cannot be taken survives
        // every pass. Derivativef's simplification decides by asking for the derivative and
        // keeping the node when a Derivativef comes back -- so anything that simplifies on the
        // way to that test asks the same node again and never stops. Cost me a stack overflow
        // at 3214 frames. https://github.com/asc-community/AngouriMath/issues/1002
        "derivative(x!, x, 2)", "derivative(x!, x, 3)", "derivative(sin(x!) + x, x, 2)",
        "derivative(apply(f, x), x, -1)", "derivative(apply(f, x), x, -2)",
        "derivative(abs(x), x, 2)", "derivative(sgn(x), x, 2)",
        // The degenerate arithmetic that a rule's assumption is usually about.
        "0 ^ 0", "1 ^ (+oo)", "(+oo) - (+oo)", "0 * (+oo)", "log(1, 1)", "x / x", "x - x",
        "0 / 0", "(-8) ^ (1/3)", "(-1) ^ (1/2)", "abs(sgn(x))",
        // Factorials and gamma, which have their own recursion.
        "x!", "(x + 1)! / x!", "(x!)!", "gamma(gamma(x))",
        // Big and small numbers, where the decimal context saturates.
        "2 ^ 1000", "2 ^ (-1000)", "10 ^ 10000", "1 / 10 ^ 200",
        // Booleans, where excluded middle was decided wrongly.
        "x > 0 or x <= 0", "not (x > 0) or (x > 0)", "a and not a", "a xor a",
    };
}
