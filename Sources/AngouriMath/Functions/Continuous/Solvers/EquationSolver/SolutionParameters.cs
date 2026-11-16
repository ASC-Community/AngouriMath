//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// The parameters a solve brings into its answer -- <c>n_1</c> for a whole number of periods,
    /// <c>t_1</c> for an unknown nothing constrains, <c>r_1</c> for an argument -- each minted here
    /// with the set it ranges over, so that the set is said where the parameter is made and not
    /// read back from its name.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// While a system is being solved a scope is open, and within it one name stands for one
    /// set: a name already minted for another set is passed over for the next free one, so a
    /// <c>t</c> from <c>floor(x) = 1</c>, which ranges over <c>[0, 1)</c>, and the <c>t</c> of
    /// an unknown left free are never both <c>t_1</c> in one answer.
    /// </para>
    /// <para>
    /// Outside a scope, as in the solve of one equation, minting is
    /// <see cref="Variable.CreateUnique"/> and nothing more. A scope is a thread-static pointer
    /// read once per parameter minted; the solve it covers is synchronous.
    /// </para>
    /// </remarks>
    internal sealed class SolutionParameters
    {
        [ThreadStatic] private static SolutionParameters? current;

        private readonly Dictionary<Variable, Set> sets = new();

        private SolutionParameters() { }

        /// <summary>
        /// The scope open on this thread, opening one where none is; <paramref name="opened"/>
        /// says whether this call opened it and so is the one to <see cref="Close"/> it.
        /// </summary>
        internal static SolutionParameters Open(out bool opened)
        {
            opened = current is null;
            return current ??= new SolutionParameters();
        }

        /// <summary>The scope open on this thread, or <see langword="null"/>.</summary>
        internal static SolutionParameters? Current => current;

        /// <summary>Closes the scope this thread has open.</summary>
        internal void Close() => current = null;

        /// <summary>
        /// The set <paramref name="parameter"/> was minted for in this scope, or
        /// <see langword="null"/> where it was not minted here.
        /// </summary>
        internal Set? SetOf(Variable parameter) => sets.TryGetValue(parameter, out var set) ? set : null;

        /// <summary>
        /// The set an unknown that nothing constrains ranges over: the reals where the solve
        /// is over the reals, and the complex numbers otherwise.
        /// </summary>
        internal static Set Free => MathS.Settings.Codomain.Value == Core.Domain.Real ? MathS.Sets.R : MathS.Sets.C;

        /// <summary>
        /// A parameter named <paramref name="prefix"/> with an index, not used in
        /// <paramref name="taken"/>, ranging over <paramref name="over"/>.
        /// </summary>
        internal static Variable Mint(Entity taken, string prefix, Set over)
        {
            var parameter = Variable.CreateUnique(taken, prefix);
            if (current is not { } scope)
                return parameter;
            while (scope.sets.TryGetValue(parameter, out var other) && other != over)
            {
                taken += parameter;
                parameter = Variable.CreateUnique(taken, prefix);
            }
            scope.sets[parameter] = over;
            return parameter;
        }

        /// <summary>
        /// <paramref name="root"/>, the value one unknown was solved for, with each parameter it
        /// brings renamed where <paramref name="family"/>, the values solved for after it from
        /// <paramref name="below"/>, has a parameter of the same name that it minted itself. A
        /// new name avoids <paramref name="taken"/> as well as all of those.
        /// </summary>
        /// <remarks>
        /// A parameter of the root that reaches the equations below is the same parameter in the
        /// family, as <c>y = x</c> takes the <c>n_1</c> of <c>x = pi/4 + pi n_1</c>. One that does
        /// not reach them is independent of anything the family says, and a parameter the
        /// family minted with its name is a second one: <c>sin(x) = 1/2</c> and
        /// <c>sin(y) = 1/2</c> each mint an <c>n_1</c>, and the row
        /// <c>(pi/6 + 2 pi n_1, pi/6 + 2 pi n_1)</c> left out every solution with
        /// <c>x != y</c>.
        /// </remarks>
        internal static Entity Apart(Entity root, IReadOnlyList<Entity> family, IEnumerable<Entity> below, Entity taken)
        {
            HashSet<Variable>? fresh = null;
            foreach (var parameter in root.Vars)
            {
                if (fresh is null)
                {
                    var shared = new HashSet<Variable>(below.SelectMany(equation => equation.Vars));
                    fresh = new HashSet<Variable>(family.SelectMany(value => value.Vars).Where(name => !shared.Contains(name)));
                    if (fresh.Count == 0)
                        return root;
                }
                if (!fresh.Contains(parameter))
                    continue;
                taken += root;
                foreach (var value in family)
                    taken += value;
                foreach (var equation in below)
                    taken += equation;
                var renamed = Mint(taken, parameter.SplitIndex() is var (prefix, _) ? prefix : parameter.Name,
                    current?.SetOf(parameter) ?? Free);
                root = root.Substitute(parameter, renamed);
                fresh.Add(renamed);
            }
            return root;
        }
    }
}
