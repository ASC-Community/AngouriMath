//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core.Sets;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;
using static AngouriMath.Entity.Number;

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

        // Made at the first parameter minted, since most solves mint none and a scope is opened
        // for every one.
        private Dictionary<Variable, Set>? sets;

        /// <summary>The parameters minted in this scope, in the order they were.</summary>
        private List<Variable>? minted;

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
        internal Set? SetOf(Variable parameter) => sets is not null && sets.TryGetValue(parameter, out var set) ? set : null;

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
            var sets = scope.sets ??= new();
            while (sets.TryGetValue(parameter, out var other) && other != over)
            {
                taken += parameter;
                parameter = Variable.CreateUnique(taken, prefix);
            }
            if (!sets.ContainsKey(parameter))
                (scope.minted ??= new()).Add(parameter);
            sets[parameter] = over;
            return parameter;
        }

        /// <summary>
        /// The answer <paramref name="solve"/> gives to one equation, with each parameter it brings
        /// for a whole number bound by an indexed union, where no scope is open; as it is given
        /// inside a scope, where it is a step of a system's solve.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A family of roots is every value of its expression over the whole numbers, which is the
        /// indexed union <c>union({ pi/6 + 2 pi n, 5 pi/6 + 2 pi n }, n in ZZ)</c>. Written with a
        /// free <c>n_1</c> it was a finite set, which claims to be every root and lists an
        /// expression that is none of them, and nothing said what <c>n_1</c> ranges over: the
        /// union binds its name and decides membership, <c>4 pi in union({ 2 pi n }, n in ZZ)</c>
        /// being True. A system's rows are a matrix, whose entries cannot bind a name, so inside a
        /// system's scope the parameters stay free and are named with their sets there.
        /// https://github.com/asc-community/AngouriMath/issues/2319
        /// </para>
        /// <para>
        /// A parameter over a continuum, the <c>r_1</c> of <c>abs(x) = 2</c> or the <c>t_1</c> of
        /// <c>floor(x) = 1</c>, carries the set it ranges over as a condition on the root already,
        /// and is left as it is.
        /// </para>
        /// </remarks>
        internal static Set Bound<TQuestion>(TQuestion question, Func<TQuestion, Set> solve)
        {
            var scope = Open(out var opened);
            try
            {
                var answer = solve(question);
                return opened && scope.minted is { Count: > 0 } ? scope.BindWholeParameters(answer) : answer;
            }
            finally
            {
                if (opened)
                    scope.Close();
            }
        }

        /// <summary>Whether <paramref name="set"/> is the whole numbers or a part of them this class writes.</summary>
        private static bool IsWhole(Set set)
            => set == MathS.Sets.Z || set is SetMinusf(var from, _) && from == MathS.Sets.Z || set is Intersectionf(var of, _) && of == MathS.Sets.Z;

        /// <summary>
        /// The whole-number parameters minted in this scope, in the order they were, that
        /// <paramref name="root"/> mentions.
        /// </summary>
        private List<Variable> WholeParametersOf(Entity root)
        {
            var vars = root.Vars;
            return minted is null ? new() : minted.Where(parameter => IsWhole(sets![parameter]) && vars.Contains(parameter)).ToList();
        }

        /// <summary>
        /// <paramref name="answer"/> with each listed root that has whole parameters in it bound, and
        /// an intersection a family is now in simplified, where it meets a set of reals in its real
        /// members: <c>e ^ x = 2 and x in RR</c> is <c>{ ln(2) }</c>. The solve met
        /// <c>{ ln(2) + 2 pi i n_1 }</c> with the reals, which a root with a free parameter could not.
        /// </summary>
        private Set BindWholeParameters(Set answer)
            => (Set)answer.Replace(node => node switch
            {
                FiniteSet listed when listed.Any(root => WholeParametersOf(root).Count > 0) => Bind(listed),
                Intersectionf(Set left, Set right) when SetOperators.HasAFamily(left) || SetOperators.HasAFamily(right) => node.InnerSimplified,
                // Two pieces the intersection above made finite are one set.
                Unionf(FiniteSet, FiniteSet) => node.InnerSimplified,
                _ => node,
            });

        /// <summary>
        /// The numbers <c>k</c> other than 0 and <c>c</c> where <paramref name="value"/> is written
        /// <c>k n + c</c> in <paramref name="n"/>, read off its terms and their factors; otherwise
        /// <see langword="null"/>. Not by gathering a polynomial, which expands: the value of a power
        /// inverted inside a large equation is a large expression, and expanding it was most of what
        /// solving such an equation cost.
        /// </summary>
        internal static (Complex Slope, Complex Constant)? NumericLine(Entity value, Variable n)
        {
            Entity slope = Integer.Zero, constant = Integer.Zero;
            foreach (var term in Sumf.LinearChildren(value))
            {
                if (!term.ContainsNode(n))
                {
                    constant += term;
                    continue;
                }
                Entity coefficient = Integer.One;
                var occurrences = 0;
                foreach (var factor in Mulf.LinearChildren(term))
                    if (factor == n)
                        occurrences++;
                    else if (factor.ContainsNode(n))
                        return null;
                    else
                        coefficient *= factor;
                if (occurrences != 1)
                    return null;
                slope += coefficient;
            }
            return slope.Evaled is Complex { IsZero: false } k && constant.Evaled is Complex c ? (k, c) : null;
        }

        /// <summary>
        /// <paramref name="listed"/> as the roots with no whole parameter, united with one indexed
        /// union for the roots that bind the same names over the same sets: one level for each
        /// parameter, the first minted outermost, since the ones after it were minted for a value
        /// that it is already in. Each parameter is renamed to the first of <c>n</c>, <c>m</c>,
        /// <c>k</c>, <c>j</c> that the root has no other use for.
        /// </summary>
        private Set Bind(FiniteSet listed)
        {
            var plain = new List<Entity>();
            var families = new List<(List<(Variable Name, Set Over)> Indices, List<Entity> Roots)>();
            foreach (var root in listed)
            {
                var parameters = WholeParametersOf(root);
                if (parameters.Count == 0)
                {
                    plain.Add(root);
                    continue;
                }
                var others = new HashSet<Variable>(root.Vars.Where(name => !parameters.Contains(name)));
                var indices = new List<(Variable Name, Set Over)>();
                // All at once: a root can be thousands of nodes, and each substitution copies it.
                var renaming = new Dictionary<Variable, Entity>();
                foreach (var parameter in parameters)
                {
                    var name = IndexNames.Select(MathS.Var).FirstOrDefault(candidate => !others.Contains(candidate)) ?? parameter;
                    others.Add(name);
                    renaming[parameter] = name;
                    indices.Add((name, sets![parameter]));
                }
                var named = root.Substitute(renaming);
                if (families.FindIndex(family => family.Indices.SequenceEqual(indices)) is var at and >= 0)
                    families[at].Roots.Add(named);
                else
                    families.Add((indices, new List<Entity> { named }));
            }
            Set? answer = plain.Count > 0 ? new FiniteSet(plain) : null;
            foreach (var (indices, roots) in families)
            {
                Set family = new FiniteSet(roots);
                for (var level = indices.Count - 1; level >= 0; level--)
                    family = new IndexedUnionf(indices[level].Name, indices[level].Over, family);
                answer = answer is null ? family : new Unionf(answer, family);
            }
            return answer ?? listed;
        }

        [ConstantField] private static readonly string[] IndexNames = { "n", "m", "k", "j" };

        /// <summary>
        /// <paramref name="root"/> at one member of its family: each symbol in it a whole
        /// parameter minted in the scope open, put at the whole number nearest 0 in its set; or
        /// <see langword="null"/> where a symbol is anything else.
        /// </summary>
        internal static Entity? AtAMember(Entity root)
        {
            if (current is not { } scope)
                return null;
            foreach (var parameter in root.Vars.Distinct().ToList())
            {
                if (scope.SetOf(parameter) is not { } set || !IsWhole(set))
                    return null;
                Entity? value = null;
                foreach (var candidate in Nearest)
                    if (set.TryContains(candidate, out var contains) && contains)
                    {
                        value = candidate;
                        break;
                    }
                if (value is null)
                    return null;
                root = root.Substitute(parameter, value);
            }
            return root;
        }

        [ConstantField] private static readonly Integer[] Nearest = { 0, 1, -1, 2, -2 };

        /// <summary>
        /// Each root in <paramref name="roots"/>, with an index of an indexed union in it minted as
        /// a parameter over the union's index set, or <see langword="null"/> where
        /// <paramref name="roots"/> is not a finite set of roots or such unions of them. This is how
        /// a system takes apart the answer of one of its equations, whose rows cannot bind a name.
        /// </summary>
        internal static List<Entity>? Each(Entity roots, Entity taken)
        {
            switch (roots)
            {
                case FiniteSet listed:
                    return listed.ToList();
                case Unionf(var left, var right):
                    return Each(left, taken) is { } first && Each(right, taken) is { } second ? first.Concat(second).ToList() : null;
                case IndexedUnionf { Var: Variable index, Over: Set over, Body: var body }:
                    var parameter = Mint(taken + roots, index.SplitIndex() is var (prefix, _) ? prefix : index.Name, over);
                    return Each(body.Substitute(index, parameter), taken + parameter);
                default:
                    return null;
            }
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
