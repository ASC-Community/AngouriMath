//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Products with a transpose in them, kept out of the reach of simplification. A transpose is a matrix, and
    /// matrices do not commute: AᵀB is not BAᵀ, and AᵀB - BAᵀ is not 0. Every pass of the simplifier reads a product
    /// as commutative -- sorting, collecting like factors, reading it as a polynomial, printing it -- so such a
    /// product is set aside as a name of its own, the rest simplified around it, and the product put back with its
    /// factors simplified each on its own, in the order they were written. A name beside a transpose is kept in its
    /// place too, since it may stand for a matrix, and a name carries no type to say whether it does
    /// (https://github.com/asc-community/AngouriMath/issues/1237). The transpose of a matrix written out is not
    /// set aside: simplification carries it out. A determinant, a transpose, a trace or an entry of a name beside a
    /// written matrix is set aside whole, since simplifying its argument reads the name as a number.
    /// https://github.com/asc-community/AngouriMath/issues/2147
    /// </summary>
    internal static class ProductsOfMatrices
    {
        /// <summary>
        /// <paramref name="expr"/> with each product of a transpose and anything but a number in place of a fresh
        /// name, and the products by those names, innermost first; or null where there is no such product, which is
        /// found without allocating, since every simplification asks.
        /// </summary>
        internal static (Entity Frozen, List<(Variable Name, Entity Product)> Products)? Frozen(Entity expr)
        {
            if (!HasMatrixOperation(expr))
                return null;
            var products = new List<(Variable Name, Entity Product)>();
            var taken = new HashSet<Variable>(expr.Vars);
            var frozen = expr.Replace(node =>
            {
                if (!(node is Mulf or Divf && IsAProductOfMatrices(node, products) || ReadsANameAsANumber(node)))
                    return node;
                // One product written twice is one name, so that AᵀB - AᵀB is still 0.
                var same = products.FindIndex(p => p.Product == node);
                if (same >= 0)
                    return products[same].Name;
                var name = Variable.CreateTemp(taken);
                taken.Add(name);
                products.Add((name, node));
                return name;
            });
            return products.Count == 0 ? null : (frozen, products);
        }

        /// <summary>
        /// <paramref name="simplified"/> with the products put back, outermost first so that a product inside another
        /// is reached after it, each factor simplified on its own.
        /// </summary>
        internal static Entity Thawed(Entity simplified, List<(Variable Name, Entity Product)> products, int level)
        {
            for (var k = products.Count - 1; k >= 0; k--)
            {
                var (name, product) = products[k];
                var factorsSimplified = product switch
                {
                    Mulf(var left, var right) => (Entity)new Mulf(Factor(left, products, level), Factor(right, products, level)),
                    Divf(var left, var right) => new Divf(Factor(left, products, level), Factor(right, products, level)),
                    _ => product,
                };
                // Carried out where the factors have become matrices written out, and only there: with a name
                // beside a written matrix, carrying it out reads the name as a number.
                simplified = simplified.Substitute(name,
                    Entity.HasANameOutsideAWrittenMatrix(factorsSimplified) ? factorsSimplified : factorsSimplified.InnerSimplified);
            }
            return simplified;
        }

        // A factor that is itself one of the names stays a name, to be put back in its turn.
        private static Entity Factor(Entity factor, List<(Variable Name, Entity Product)> products, int level)
            => factor is Variable variable && products.Exists(p => p.Name == variable) ? factor : factor.Simplify(level);

        private static bool HasMatrixOperation(Entity node)
        {
            if (node is Transposef { Argument: not Matrix } or Determinantf { Argument: not Matrix }
                or Tracef { Argument: not Matrix } or MatrixEntryf { Argument: not Matrix })
                return true;
            var children = node.DirectChildren;
            for (var i = 0; i < children.Count; i++)
                if (HasMatrixOperation(children[i]))
                    return true;
            return false;
        }

        // An operation on a matrix whose argument has a name beside a written matrix, as det(A [[2, 0], [0, 3]]) has:
        // simplifying the argument would read the name as a number and multiply it into the entries, and the
        // operation would then be carried out on that. It is set aside whole until the name is a matrix.
        private static bool ReadsANameAsANumber(Entity node)
            => node is Transposef or Determinantf or Tracef or MatrixEntryf
               && node.DirectChildren[0] is not Matrix and var argument
               && argument.Nodes.Any(n => n is Matrix)
               && Entity.HasANameOutsideAWrittenMatrix(argument);

        // A transpose, or a product already set aside, among two or more factors that are not numbers.
        private static bool IsAProductOfMatrices(Entity product, List<(Variable Name, Entity Product)> products)
        {
            var factors = Mulf.LinearChildren(product);
            var (matrices, others) = (0, 0);
            foreach (var factor in factors)
                if (factor is Transposef { Argument: not Matrix } or Powf(Transposef { Argument: not Matrix }, _)
                    || factor is Variable variable && products.Exists(p => p.Name == variable)
                    || factor is Powf(Variable powered, _) && products.Exists(p => p.Name == powered))
                    matrices++;
                else if (factor is not Number)
                    others++;
            return matrices > 0 && matrices + others >= 2;
        }
    }
}
