//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    /// <summary>
    /// The Buckingham π theorem made constructive: the products of powers of the given
    /// variables that are pure numbers form a lattice, and a basis of it is read off the
    /// null space of the dimension matrix.
    /// </summary>
    /// <remarks>
    /// Row reduction pivots on the leftmost columns it can, so the variables listed first are
    /// the ones that recur across the groups and each later one appears, to the first power,
    /// in exactly one group. That is how a physicist chooses "repeating variables", and the
    /// order of the list is how a caller chooses them here.
    /// </remarks>
    internal static class DimensionlessGroups
    {
        internal static IReadOnlyList<Entity> Of(IReadOnlyList<Variable> variables, IReadOnlyDictionary<Variable, Dimension> dims)
        {
            var n = variables.Count;
            var missing = variables.FirstOrDefault(v => !dims.ContainsKey(v));
            if (missing is not null)
                throw new ArgumentException($"No dimension is declared for {missing}.", nameof(dims));
            if (variables.Distinct().Count() != n)
                throw new ArgumentException("A variable is listed twice.", nameof(variables));

            const int rows = 7;
            var m = new ERational[rows, n];
            for (var r = 0; r < rows; r++)
                for (var c = 0; c < n; c++)
                    m[r, c] = dims[variables[c]].Exponent(r);

            // Reduced row echelon form, exactly.
            var pivotOfRow = new List<int>();
            var row = 0;
            for (var col = 0; col < n && row < rows; col++)
            {
                var p = row;
                while (p < rows && m[p, col].IsZero)
                    p++;
                if (p == rows)
                    continue;
                for (var c = 0; c < n; c++)
                    (m[row, c], m[p, c]) = (m[p, c], m[row, c]);
                var lead = m[row, col];
                for (var c = 0; c < n; c++)
                    m[row, c] /= lead;
                for (var r = 0; r < rows; r++)
                {
                    if (r == row || m[r, col].IsZero)
                        continue;
                    var factor = m[r, col];
                    for (var c = 0; c < n; c++)
                        m[r, c] -= factor * m[row, c];
                }
                pivotOfRow.Add(col);
                row++;
            }

            var groups = new List<Entity>();
            foreach (var free in Enumerable.Range(0, n).Except(pivotOfRow))
            {
                var x = Enumerable.Repeat(ERational.Zero, n).ToArray();
                x[free] = ERational.One;
                for (var r = 0; r < pivotOfRow.Count; r++)
                    x[pivotOfRow[r]] = -m[r, free];

                // Clear denominators and common factors, so that the group reads with the
                // smallest whole powers: rho v L / mu rather than sqrt of its square.
                var lcm = x.Aggregate(EInteger.One, (acc, q) => acc * q.Denominator / acc.Gcd(q.Denominator));
                var ints = x.Select(q => q.Numerator * (lcm / q.Denominator)).ToArray();
                var gcd = ints.Aggregate(EInteger.Zero, (acc, k) => acc.Gcd(k.Abs()));
                ints = ints.Select(k => k / gcd).ToArray();

                var group = Product(variables, ints);
                var check = DimensionAnalysis.Of(group, dims);
                if (!check.IsConsistent || !check.Dimension!.IsDimensionless)
                    throw new AngouriBugException($"The dimensionless group {group} has dimension {check}");
                groups.Add(group);
            }
            return groups;
        }

        private static Entity Product(IReadOnlyList<Variable> variables, EInteger[] powers)
        {
            static Entity Factor(Variable v, EInteger k) => k.Equals(EInteger.One) ? v : new Powf(v, Integer.Create(k));
            Entity? Side(Func<EInteger, bool> which, Func<EInteger, EInteger> magnitude)
                => variables.Zip(powers, (v, k) => (v, k)).Where(t => which(t.k))
                    .Select(t => Factor(t.v, magnitude(t.k)))
                    .Aggregate((Entity?)null, (acc, f) => acc is null ? f : new Mulf(acc, f));
            var numerator = Side(k => k.Sign > 0, k => k);
            var denominator = Side(k => k.Sign < 0, k => -k);
            return (numerator, denominator) switch
            {
                (null, null) => Integer.One,
                (var num, null) => num,
                (null, var den) => new Divf(Integer.One, den),
                (var num, var den) => new Divf(num, den),
            };
        }
    }
}
