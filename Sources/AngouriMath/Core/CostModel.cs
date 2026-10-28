//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Core
{
    /// <summary>
    /// What "simpler" means, as a named value rather than as an anonymous function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="MathS.Settings.ComplexityCriteria"/> has always accepted any
    /// <see cref="Func{Entity, Double}"/>, so the cost of an expression was already the caller's
    /// to choose. What it could not do is be <i>named</i>: two callers wanting "the smallest tree"
    /// each wrote the same lambda, neither could say which one they used, and nothing could list
    /// what the alternatives are. <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
    /// v2.0 asks for a cost model that is data for exactly that reason, and names the examples —
    /// smallest tree, fewest radicals — that <see cref="All"/> now holds.
    /// </para>
    /// <para>
    /// <b>A model counts one of two things, and which one decides what it may read.</b>
    /// <see cref="SmallestTree"/> and <see cref="Default"/> count <i>how the expression is
    /// written</i>: <c>y ^ (1 * (-1)) * x</c> really is a bigger tree than <c>x / y</c>, so
    /// reading the node as written is not an approximation, it is the question. The others count a
    /// <i>mathematical feature</i> — is there a division here, is there a radical — which is a
    /// property of the value and not of the spelling, so they ask what an exponent
    /// <see cref="Entity.Evaled"/> to rather than what it was typed as.
    /// </para>
    /// <para>
    /// The distinction is worth stating because getting it wrong is silent and backwards:
    /// <see cref="FewestDivisions"/> used to test the exponent's node, so
    /// <c>y ^ (1 * (-1)) * x</c> — a division written where the test could not see it — scored
    /// <i>cheaper</i> than <c>x / y</c>, under the one criterion whose whole job is to remove
    /// divisions. That is
    /// <a href="https://github.com/asc-community/AngouriMath/issues/950">#950</a>. <b>A caller
    /// writing their own model wants to decide which of the two kinds it is</b>, because nothing
    /// here can decide it for them: an expression handed to a cost model has not necessarily been
    /// evaluated, and on an e-graph — where every writing of a value is a member of one class at
    /// once — it certainly has not.
    /// </para>
    /// <para>
    /// <b>Every model here counts nodes a little, even the ones that are about something else.</b>
    /// A criterion that counts only its own feature ties constantly, and a tie is settled by
    /// whichever candidate the search happened to generate first — which is an accident rather
    /// than a preference. The node term is small enough not to overturn the feature it is added to
    /// and large enough to decide between candidates the feature cannot separate.
    /// </para>
    /// <example>
    /// <code>
    /// using var _ = MathS.Settings.ComplexityCriteria.Set(CostModel.FewestDivisions.Cost);
    /// Console.WriteLine("a / b + b / c".ToEntity().Simplify());   // (a * c + b ^ 2) / (b * c)
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="Name">A short name, so a report can say which model produced an answer.</param>
    /// <param name="Description">What this model prefers, in a sentence.</param>
    /// <param name="Cost">
    /// The cost of an expression; lower is simpler. Pass this to
    /// <see cref="MathS.Settings.ComplexityCriteria"/>.
    /// </param>
    public sealed record CostModel(string Name, string Description, Func<Entity, double> Cost)
    {
        /// <summary>
        /// What <see cref="MathS.Settings.ComplexityCriteria"/> uses when nothing is set: a
        /// weighted count that prefers few nodes, few divisions, few negative powers, and a
        /// rationalised denominator.
        /// </summary>
        /// <remarks>
        /// This is the same function the setting is initialised with, not a copy of it, so the
        /// two cannot drift apart. Setting it explicitly changes nothing.
        /// </remarks>
        public static CostModel Default { get; } = new(
            nameof(Default),
            "A weighted count, preferring few nodes, few divisions and no root in a denominator.",
            DefaultCost);

        /// <summary>Prefers the expression with fewest nodes, counting every node alike.</summary>
        /// <remarks>
        /// The plainest possible notion of simple, and a useful contrast with
        /// <see cref="Default"/>: it has no opinion about which node is worse, so it will accept
        /// a division or a negative power that <see cref="Default"/> would pay to remove.
        /// </remarks>
        public static CostModel SmallestTree { get; } = new(
            nameof(SmallestTree),
            "Fewest nodes, with no preference between them.",
            static expr => expr.Nodes.Count());

        /// <summary>Prefers the expression with fewest divisions, then fewest nodes.</summary>
        /// <remarks>
        /// A negative power is a division written differently, so it counts too — otherwise the
        /// model would merely move divisions rather than remove them. For the same reason the
        /// exponent is read by value: <c>y ^ (1 * (-1))</c> is a division however it is spelled,
        /// and a test that missed it would rate the unevaluated writing cheapest.
        /// </remarks>
        public static CostModel FewestDivisions { get; } = new(
            nameof(FewestDivisions),
            "Fewest divisions, counting a negative power as one, then fewest nodes.",
            static expr => Feature(expr, static node =>
                node is Divf
                || node is Powf(_, var exponent) && exponent.Evaled is Real { IsNegative: true }));

        /// <summary>Prefers the expression with fewest radicals, then fewest nodes.</summary>
        /// <remarks>
        /// A radical is a power by a non-integer rational — <c>sqrt(x)</c> is <c>x ^ (1/2)</c>
        /// here, and there is no separate root node to count. The exponent is read by value, so
        /// <c>x ^ (2 ^ (-1))</c> counts as the radical it is.
        /// </remarks>
        public static CostModel FewestRadicals { get; } = new(
            nameof(FewestRadicals),
            "Fewest fractional powers, then fewest nodes.",
            static expr => Feature(expr, static node =>
                node is Powf(_, var exponent)
                && exponent.Evaled is Rational and not Integer));

        /// <summary>
        /// Every model here, so a caller can offer the choice rather than hard-code one.
        /// </summary>
        public static IReadOnlyList<CostModel> All { get; } =
            new[] { Default, SmallestTree, FewestDivisions, FewestRadicals };

        /// <summary>The name, which is what a report wants.</summary>
        public override string ToString() => Name;

        /// <summary>
        /// The count of nodes satisfying <paramref name="predicate"/>, plus a small term in the
        /// total size so that expressions the predicate cannot separate are still ordered.
        /// </summary>
        private static double Feature(Entity expr, Func<Entity, bool> predicate)
            => expr.Nodes.Count(predicate) + 0.001 * expr.Nodes.Count();

        // The default criteria, as a method rather than a lambda so that
        // MathS.Settings.ComplexityCriteria and CostModel.Default are one function and not two.
        // Those weights are of the 2nd power to avoid problems with floating numbers.
        private const double TinyWeight = 0.5;
        private const double MinorWeight = 1.0;
        private const double Weight = 2.0;
        private const double MajorWeight = 4.0;
        private const double HeavyWeight = 8.0;
        private const double ExtraHeavyWeight = 12.0;

        // Recurses through Entity.DefaultCostCached rather than through itself, so a subtree is
        // costed once per instance however many candidates share it; and walks children by
        // index rather than through Nodes or a LINQ Sum, so that costing a node allocates
        // nothing. The arithmetic is the same expression in the same order, so every rate is
        // the same double it was -- the selection between candidates cannot move.
        internal static double DefaultCost(Entity expr) => expr switch
        {
            // Weigh provided predicates much less but nested provideds heavy
            Providedf(var inner, var predicate) =>
                inner.DefaultCostCached + 0.1 * predicate.DefaultCostCached
                + ExtraHeavyWeight * (ProvidedsIn(inner) + ProvidedsIn(predicate)),
            Piecewise { Cases: var cases } =>
                cases.Sum(@case =>
                    @case.Expression.DefaultCostCached + 0.1 * @case.Predicate.DefaultCostCached
                    + ExtraHeavyWeight * (ProvidedsIn(@case.Expression) + ProvidedsIn(@case.Predicate))),
            Variable => Weight, // Number of variables
            // A root in a denominator, which the rationalising rule clears out.
            // Without a weight here the two forms tie -- 1 / (sqrt(3) + 5) and
            // (sqrt(3) - 5) / (-22) are the same rate -- and a tie is settled by
            // whichever candidate was generated first, which is not a preference
            // so much as an accident. This states the preference instead.
            // https://github.com/asc-community/AngouriMath/issues/205
            Divf(_, var divisor) when HasFractionalPower(divisor)
                => MinorWeight + Weight + ChildrenCost(expr),
            Divf => MinorWeight + ChildrenCost(expr), // Number of divides
            Rational(Integer(1 or -1), _) and not Integer => Weight + ChildrenCost(expr), // Number of rationals with unit numerator
            Powf(_, Real { IsNegative: true }) => HeavyWeight + ChildrenCost(expr), // Number of negative powers
            Logf => TinyWeight + ChildrenCost(expr), // Number of logarithms
            Phif => ExtraHeavyWeight + ChildrenCost(expr), // Number of phi functions
            Mobiusf or DivisorSigmaf or PrimeNuf or PrimeOmegaf or PrimePif => ExtraHeavyWeight + ChildrenCost(expr),
            Primef => ExtraHeavyWeight + ChildrenCost(expr),
            Valuationf => ExtraHeavyWeight + ChildrenCost(expr),
            Real { IsNegative: true } => MajorWeight + ChildrenCost(expr), // Number of negative reals
            ComparisonSign when expr.DirectChildren[0] == 0 => Weight + ChildrenCost(expr), // 0 < x is bad. x > 0 is good.
            Notf(Equalsf eq) => -Weight + eq.DefaultCostCached, // (not x = 0) is equally complex as (x = 0)
            _ => ChildrenCost(expr)
        } + Weight; // Number of nodes

        /// <summary>The children's costs, summed in order -- what <c>DirectChildren.Sum(DefaultCost)</c> summed.</summary>
        private static double ChildrenCost(Entity expr)
        {
            var children = expr.DirectChildren;
            var sum = 0d;
            for (var i = 0; i < children.Count; i++)
                sum += children[i].DefaultCostCached;
            return sum;
        }

        /// <summary>How many nodes of the tree, the root included, are a <c>Providedf</c> -- what <c>Nodes.Count(n => n is Providedf)</c> counted.</summary>
        private static int ProvidedsIn(Entity expr)
        {
            var count = expr is Providedf ? 1 : 0;
            var children = expr.DirectChildren;
            for (var i = 0; i < children.Count; i++)
                count += ProvidedsIn(children[i]);
            return count;
        }

        /// <summary>Whether any node of the tree, the root included, is a power to a non-whole rational -- what the <c>Nodes.Any</c> asked.</summary>
        private static bool HasFractionalPower(Entity expr)
        {
            if (expr is Powf(_, Rational and not Integer)) return true;
            var children = expr.DirectChildren;
            for (var i = 0; i < children.Count; i++)
                if (HasFractionalPower(children[i])) return true;
            return false;
        }
    }
}
