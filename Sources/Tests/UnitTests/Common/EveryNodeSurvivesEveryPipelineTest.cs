//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Every node survives every pipeline: no crash, no hang, no exception the caller has no
    /// reason to expect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generalisation of <c>LimitTerminatesOnEveryNodeTest</c>. Limits were where the
    /// hole showed — seven node types killed the process by inheriting a default that
    /// returned the node instead of <see langword="null"/> — but nothing made limits special.
    /// A node reaches a dozen pipelines and <c>Docs/Contributing/AddingNode.cs</c> lists
    /// them; anything missed there is missed silently, because the node that breaks is
    /// exactly the one nobody wrote a test for.
    /// </para>
    /// <para>
    /// So the node types are enumerated rather than listed, and each is put through every
    /// pipeline. What is asserted is only what every pipeline owes a caller: return, in
    /// finite time, either an answer or an <see cref="AngouriMath.Core.Exceptions.AngouriMathBaseException"/>.
    /// Not that the answer is right — the per-node tests are for that — and not that there
    /// is one at all, since an unevaluated node is a legitimate answer here.
    /// </para>
    /// <para>
    /// One pipeline owes more than that and gets its own assertion,
    /// <see cref="StringizeRoundTripsToTheSameNode"/>: printing is the inverse of parsing or it
    /// is nothing. The same enumeration answers it for every node type at once.
    /// </para>
    /// https://github.com/asc-community/AngouriMath/issues/829
    /// </remarks>
    [Trait("Area", "Common")]
    public sealed class EveryNodeSurvivesEveryPipelineTest
    {
        private static readonly Entity.Variable X = MathS.Var("x");

        /// <summary>
        /// Every node type there is: concrete, and an <see cref="Entity"/>. Abstract is the only
        /// exclusion — <see cref="Entity.Number.Complex"/>, <see cref="Entity.Number.Rational"/>
        /// and <see cref="Entity.Number.Real"/> are concrete but not sealed, and a filter for
        /// sealed types silently dropped all three.
        /// </summary>
        private static IEnumerable<Type> ConcreteNodeTypes =>
            typeof(Entity).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Entity).IsAssignableFrom(t))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

        /// <summary>
        /// One sample per node type that <see cref="EveryNodeType"/>'s reflective construction
        /// cannot reach: the constructor takes something that is not an <see cref="Entity"/>
        /// (a count, a flag, a range, a list), or there is no public one at all and the node
        /// comes from a factory.
        /// </summary>
        /// <remarks>
        /// A hand-written list is what the reflective half exists to avoid, so nothing relies on
        /// it being complete: <see cref="EveryNodeType"/> asserts that the two halves together
        /// cover <see cref="ConcreteNodeTypes"/>, and names whatever they miss. A new node type
        /// therefore fails this test the day it is added, rather than going untested in silence.
        /// </remarks>
        private static readonly Entity[] HandBuilt =
        {
            MathS.Apply(X, X),                                  // Application
            Entity.Boolean.True,                                // Boolean
            MathS.Derivative(X, X, 2),                          // Derivativef, with an iteration count
            MathS.PartialDerivative(X, X, 2),                   // PartialDerivativef, with an iteration count
            MathS.Integral(X, X),                               // Integralf, indefinite
            MathS.Integral(X, X, 0, 1),                         // Integralf, over a range
            MathS.Lambda(X, X),                                 // Lambda
            MathS.Limit(X, X, 0),                               // Limitf
            MathS.Vector(1, 2),                                 // Matrix, one row
            MathS.Matrix(new Entity[,] { { 1, 2 }, { 3, 4 } }), // Matrix, two by two
            3,                                                  // Integer
            Entity.Number.Rational.Create(1, 2),                // Rational
            MathS.pi.Evaled,                                    // Real — a decimal literal downcasts to Rational
            Entity.Number.Complex.Create(1, 2),                 // Complex
            MathS.Piecewise((X, X > 0), ((Entity)1, X <= 0)),   // Piecewise
            MathS.Sets.Finite(1, 2),                            // FiniteSet
            MathS.Interval(0, 1),                               // Interval
            MathS.Sets.C,                                       // Complexes
            MathS.Sets.R,                                       // Reals
            MathS.Sets.Q,                                       // Rationals
            MathS.Sets.Z,                                       // Integers
            MathS.Sets.NonNegativeIntegers,                     // NonNegativeIntegers, ZZ*
            MathS.Sets.PositiveIntegers,                        // PositiveIntegers, ZZ+
            MathS.Sets.Primes,                                  // Primes, PP
            Entity.Set.SpecialSet.Create(Domain.Boolean),       // Booleans
            X,                                                  // Variable
            MathS.pi,                                           // Constant
        };

        public static IEnumerable<object[]> EveryNodeType()
        {
            var built = new List<Entity>();
            foreach (var type in ConcreteNodeTypes)
            {
                var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Where(c => c.GetParameters().Length > 0
                                && c.GetParameters().All(p => p.ParameterType == typeof(Entity)))
                    .OrderBy(c => c.GetParameters().Length)
                    .FirstOrDefault();
                if (constructor is null)
                    continue;
                try
                {
                    built.Add((Entity)constructor.Invoke(
                        constructor.GetParameters().Select(_ => (object)(Entity)X).ToArray()));
                }
                catch (Exception) { /* a node that will not take a bare variable is not the subject */ }
            }
            built.AddRange(HandBuilt);

            var covered = new HashSet<Type>(built.Select(n => n.GetType()));
            var uncovered = ConcreteNodeTypes.Where(t => !covered.Contains(t)).ToList();
            Assert.True(uncovered.Count is 0,
                "no sample is built for these node types, so no pipeline is tested against them — "
                + $"add one to {nameof(HandBuilt)}:\n  " + string.Join("\n  ", uncovered.Select(t => t.FullName)));

            return built.Select(n => new object[] { n });
        }

        /// <summary>
        /// The pipelines a node reaches, named so a failure says which one broke.
        /// </summary>
        private static readonly (string Name, Action<Entity> Run)[] Pipelines =
        {
            ("Stringize",     e => e.Stringize()),
            ("Latexize",      e => e.Latexize()),
            ("ToString",      e => e.ToString()),
            ("InnerSimplified", e => { _ = e.InnerSimplified; }),
            ("Evaled",        e => { _ = e.Evaled; }),
            ("Simplify",      e => e.Simplify()),
            ("Expand",        e => e.Expand()),
            ("Factorize",     e => e.Factorize()),
            ("Differentiate", e => e.Differentiate(X)),
            ("Integrate",     e => e.Integrate(X)),
            ("Substitute",    e => e.Substitute(X, 2)),
            ("Vars",          e => { _ = e.Vars.Count; }),
            ("Nodes",         e => { _ = e.Nodes.Count(); }),
            ("Complexity",    e => { _ = e.Complexity; }),
            ("Solve",         e => e.SolveEquation(X)),
        };

        [Theory]
        [MemberData(nameof(EveryNodeType))]
        public void EveryPipelineReturns(Entity node)
        {
            var broken = new List<string>();
            foreach (var (name, run) in Pipelines)
            {
                // Each in a task with a timeout: a regression here is non-termination, and
                // the suite must fail rather than hang. A stack overflow cannot be caught at
                // all, so the signal for that one is the run aborting.
                string? failure = null;
                var finished = Task.Run(() =>
                {
                    try { run(node); }
                    catch (AngouriMath.Core.Exceptions.AngouriMathBaseException) { /* the library's own, and legitimate */ }
                    catch (NotSupportedException) { /* documented refusal, e.g. uncompilable shapes */ }
                    catch (Exception e) { failure = e.GetType().FullName + ": " + e.Message.Split('\n')[0]; }
                }).Wait(LimitTermination.Guard);

                if (!finished)
                    broken.Add($"{name} did not terminate");
                else if (failure is { })
                    broken.Add($"{name} threw {failure}");
            }

            Assert.True(broken.Count is 0,
                $"{node.Stringize()} [{node.GetType().Name}]:\n  " + string.Join("\n  ", broken));
        }

        /// <summary>
        /// The round trips that do not hold, keyed by what the node prints as, with the reason.
        /// </summary>
        /// <remarks>
        /// The entry is a number that prints as an operator: the printed form is the right one to
        /// read, and re-parsing it gives the operator rather than the number. So this is a
        /// statement about where the two forms part company, not a list of things to fix — what
        /// it buys is that no *other* node type may join it unnoticed.
        ///
        /// <c>Rational</c> was here too until
        /// https://github.com/asc-community/AngouriMath/issues/873, which taught the parser that a
        /// quotient of two integer literals is the rational it denotes. <c>Complex</c> is the same
        /// shape of defect and is still open: it prints as a sum, or as a difference when the
        /// imaginary part is negative, and both parse back as the operator.
        /// </remarks>
        private static readonly Dictionary<string, string> KnownRoundTripFailures = new(StringComparer.Ordinal)
        {
            ["1 + 2i"] = "a Complex prints as a sum, and a sum parses as Sumf(1, 2i)",
        };

        /// <summary>
        /// Printing a node and parsing the result gives the node back.
        /// </summary>
        /// <remarks>
        /// The pipelines above ask only that a node survives; this asks that it survives
        /// unchanged, which is the one property of <see cref="Entity.Stringize"/> a caller can
        /// build on — a printed expression is the library's own input format, so a node that
        /// prints as something else is a hole in it. Held strictly in both directions: a node
        /// listed in <see cref="KnownRoundTripFailures"/> that starts round tripping fails too,
        /// so the list cannot outlive what it describes.
        /// </remarks>
        [Theory]
        [MemberData(nameof(EveryNodeType))]
        public void StringizeRoundTripsToTheSameNode(Entity node)
        {
            var printed = node.Stringize();

            Entity? parsed = null;
            string? threw = null;
            try { parsed = MathS.FromString(printed); }
            catch (Exception e) { threw = e.GetType().Name + ": " + e.Message.Split('\n')[0]; }
            var roundTrips = threw is null && parsed == node;

            if (KnownRoundTripFailures.TryGetValue(printed, out var reason))
                Assert.False(roundTrips,
                    $"{printed} [{node.GetType().Name}] round trips now, so it is no longer a known "
                    + $"failure — drop it from {nameof(KnownRoundTripFailures)}, where it reads "
                    + $"\"{reason}\"");
            else
                Assert.True(roundTrips,
                    $"{printed} [{node.GetType().Name}] does not round trip: "
                    + (threw ?? $"parsed back as {parsed!.Stringize()} [{parsed.GetType().Name}]"));
        }
    }
}
