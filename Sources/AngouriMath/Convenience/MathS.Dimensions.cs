//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//



namespace AngouriMath
{
    using static Entity;

    partial class MathS
    {
        /// <summary>
        /// Dimensional analysis: whether a formula is physically meaningful, what kind of
        /// quantity it produces, and which combinations of its variables are pure numbers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A variable is a symbol, and nothing in an <see cref="Entity"/> says that <c>v</c>
        /// is a speed. The caller says so, with a map from each variable to its
        /// <see cref="Dimension"/>, and the check reads the formula against it. The map is
        /// passed rather than attached to the variables, so the same expression can be read
        /// under different assignments, and nothing about how an expression prints, parses or
        /// simplifies changes.
        /// </para>
        /// <para>
        /// This complements a units library rather than replacing one. UnitsNet and its kind
        /// check a <i>value</i>: a length divided by a time is a speed. Neither can look at
        /// <c>sqrt(2 g h)</c> and say whether it is one, and that is what this does.
        /// </para>
        /// <para>
        /// <b>Undetermined is not a verdict.</b> An undeclared variable, or a node the check does
        /// not read, gives <see cref="DimensionStatus.Undetermined"/>, never a guess.
        /// </para>
        /// </remarks>
        public static class Dimensions
        {
            /// <summary>
            /// The dimension of <paramref name="expr"/> as written, or the subexpression where
            /// it stops making physical sense.
            /// </summary>
            /// <param name="expr">The formula, an expression or a statement.</param>
            /// <param name="dimensions">The dimension of each variable it mentions. The constants
            /// <c>pi</c> and <c>e</c> and every number are pure numbers and need no entry.</param>
            /// <returns>
            /// <see cref="DimensionStatus.Consistent"/> with the dimension of the result — a
            /// statement such as an equation is a pure number once its sides agree;
            /// <see cref="DimensionStatus.Inconsistent"/> with the subexpression at fault and why;
            /// or <see cref="DimensionStatus.Undetermined"/> where it could not be settled.
            /// </returns>
            /// <remarks>
            /// <para>
            /// A literal <c>0</c> or <c>oo</c> fits any dimension where it is added or compared,
            /// since it is the same quantity in every unit: <c>v = 0</c> is meaningful.
            /// </para>
            /// <para>
            /// Nothing is simplified first. <c>sin(x) - sin(x)</c> with <c>x</c> a length is
            /// inconsistent, although it simplifies to <c>0</c>.
            /// </para>
            /// </remarks>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Collections.Generic;
            /// using AngouriMath;
            /// using AngouriMath.Core;
            ///
            /// var acceleration = Dimension.Length / Dimension.Time.Pow(2);
            /// var dims = new Dictionary&lt;Entity.Variable, Dimension&gt;
            /// {
            ///     ["g"] = acceleration, ["h"] = Dimension.Length, ["t"] = Dimension.Time,
            /// };
            /// Console.WriteLine(MathS.Dimensions.Of("sqrt(2 g h)", dims));
            /// Console.WriteLine(MathS.Dimensions.Of("h - g t^2 / 2 = 0", dims));
            /// Console.WriteLine(MathS.Dimensions.Of("h + g t", dims));
            /// </code>
            /// Prints
            /// <code>
            /// Consistent: L T^-1
            /// Consistent: 1
            /// Inconsistent at h + g * t: h has dimension L and g * t has dimension L T^-1, so they cannot be added
            /// </code>
            /// </example>
            public static DimensionCheck Of(Entity expr, IReadOnlyDictionary<Variable, Dimension> dimensions)
                => DimensionAnalysis.Of(expr, dimensions);

            /// <summary>
            /// A basis of the dimensionless products of powers of <paramref name="variables"/>:
            /// the Buckingham π groups.
            /// </summary>
            /// <param name="variables">
            /// The variables of the problem, each declared in <paramref name="dimensions"/>.
            /// Their order chooses the basis: the earlier ones are the repeating variables, and
            /// each later variable appears to the first power in exactly one group.
            /// </param>
            /// <param name="dimensions">The dimension of each variable.</param>
            /// <returns>
            /// One group per variable beyond the rank of the dimension matrix, each written with
            /// the smallest whole powers and each checked to be a pure number before it is
            /// returned. An empty list means no product of these variables is dimensionless.
            /// </returns>
            /// <remarks>
            /// A model whose output depends on these variables depends on them only through
            /// the groups, which is what makes a scale model of a ship mean anything, and what
            /// reduces the parameters a simulation has to be tuned over.
            /// </remarks>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Collections.Generic;
            /// using AngouriMath;
            /// using AngouriMath.Core;
            ///
            /// // Flow in a pipe: density, speed, diameter, viscosity.
            /// var dims = new Dictionary&lt;Entity.Variable, Dimension&gt;
            /// {
            ///     ["rho"] = Dimension.Mass / Dimension.Length.Pow(3),
            ///     ["v"] = Dimension.Length / Dimension.Time,
            ///     ["d"] = Dimension.Length,
            ///     ["mu"] = Dimension.Mass / (Dimension.Length * Dimension.Time),
            /// };
            /// foreach (var group in MathS.Dimensions.DimensionlessGroups(new Entity.Variable[] { "rho", "v", "d", "mu" }, dims))
            ///     Console.WriteLine(group);
            /// </code>
            /// Prints
            /// <code>
            /// mu / (rho * v * d)
            /// </code>
            /// </example>
            public static IReadOnlyList<Entity> DimensionlessGroups(IReadOnlyList<Variable> variables, IReadOnlyDictionary<Variable, Dimension> dimensions)
                => AngouriMath.Functions.DimensionlessGroups.Of(variables, dimensions);
        }
    }
}
