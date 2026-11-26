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
    /// set aside: simplification carries it out. A vector product not yet carried out is a vector, and a product with one
    /// in it is set aside as one with a transpose is, and so is a product with a factor built from either, as
    /// transpose(A) + B is (<see cref="IsAMatrixNotWrittenOut"/>). A determinant, a transpose, a trace, an entry, or a scalar or a
    /// vector product of a name beside a written matrix is set aside whole, since simplifying its argument reads the name
    /// as a number.
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
                or Tracef { Argument: not Matrix } or MatrixEntryf { Argument: not Matrix }
                or DotProductf { Left: not Matrix } or DotProductf { Right: not Matrix } || IsAVectorProduct(node))
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
        // A scalar or a vector product is set aside the same way where either operand is such an argument.
        private static bool ReadsANameAsANumber(Entity node)
            => node is Transposef or Determinantf or Tracef or MatrixEntryf
                   && ReadsANameAsANumberIn(node.DirectChildren[0])
               || node is DotProductf or CrossProductf
                   && (ReadsANameAsANumberIn(node.DirectChildren[0]) || ReadsANameAsANumberIn(node.DirectChildren[1]));

        private static bool ReadsANameAsANumberIn(Entity argument)
            => argument is not Matrix
               && argument.Nodes.Any(n => n is Matrix)
               && Entity.HasANameOutsideAWrittenMatrix(argument);

        // A vector product that is not yet carried out, which is a vector, and so a matrix to a product it stands in:
        // M cross(a, b) is not cross(a, b) M. https://github.com/asc-community/AngouriMath/issues/105
        internal static bool IsAVectorProduct(Entity node)
            => node is CrossProductf { Left: not Matrix } or CrossProductf { Right: not Matrix };

        /// <summary>
        /// Whether <paramref name="expr"/> is a matrix that is not written out: a transpose or a vector product that
        /// simplification cannot carry out, its argument not a written matrix, or anything with one in it, as
        /// <c>2 transpose(A)</c>, <c>transpose(A) + B</c>, <c>transpose(A)^2</c> and <c>conj(transpose(A))</c> have.
        /// Not where it stands inside a written matrix, whose entries are entries; inside a determinant, a trace, an
        /// entry, a scalar product or a norm, which are numbers; or inside a statement or a set, which are neither.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Such a matrix is not an entry, and spread over the entries of a written one it was read as a number:
        /// <c>[1, 0]T * transpose(A) * [0, 1]</c> was <c>[[transpose(A), 0]] * [0, 1]</c>, and so 0, where it is the
        /// entry A[2, 1]. And a product with a sum of matrices in it was sorted as though it commuted, so Simplify
        /// answered 0 for <c>[1, 0]T * (transpose(A) + B) * [0, 1]</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2557
        /// </para>
        /// <para>
        /// A function of one is counted whatever the function, since not knowing it to be a number is reason enough
        /// not to read it as one: leaving an operation as written says that it is not settled, where reading a matrix
        /// as a number answers wrongly. <c>abs</c> is the length of a vector and is taken entry by entry over a larger
        /// matrix, so <c>abs(transpose(A))</c> is one or the other depending on A.
        /// </para>
        /// <para>
        /// A bare name is not counted. Beside a written matrix it is read as a number, so <c>x * [1, 2]</c> is
        /// <c>[x, 2 x]</c>: that is an older decision than this one, about a name, which carries no type
        /// (https://github.com/asc-community/AngouriMath/issues/1237), where a transpose and a vector product are
        /// matrices by what they are.
        /// </para>
        /// </remarks>
        /// <param name="expr">What is asked about.</param>
        /// <param name="setAside">
        /// The products already set aside under a name of their own, each of which counts as a matrix where that name
        /// stands; or null, where no name does.
        /// </param>
        internal static bool IsAMatrixNotWrittenOut(Entity expr, List<(Variable Name, Entity Product)>? setAside = null)
        {
            // The first child is walked by the loop and the others by recursion: a long sum or product is a chain
            // nested to the left, as deep as it has terms.
            while (true)
            {
                switch (expr)
                {
                    case Transposef { Argument: not Matrix }:
                        return true;
                    case CrossProductf when IsAVectorProduct(expr):
                        return true;
                    case Variable name:
                        return setAside is not null && IsSetAside(name, setAside);
                    case Matrix or Number or Statement or Set
                        or Determinantf or Tracef or MatrixEntryf or DotProductf or Normf:
                        return false;
                }
                var children = expr.DirectChildren;
                if (children.Count == 0)
                    return false;
                for (var i = 1; i < children.Count; i++)
                    if (IsAMatrixNotWrittenOut(children[i], setAside))
                        return true;
                expr = children[0];
            }
        }

        private static bool IsSetAside(Variable name, List<(Variable Name, Entity Product)> setAside)
        {
            for (var i = 0; i < setAside.Count; i++)
                if (setAside[i].Name == name)
                    return true;
            return false;
        }

        // A matrix not written out, a product already set aside counting as one, among two or more factors that are
        // not numbers.
        private static bool IsAProductOfMatrices(Entity product, List<(Variable Name, Entity Product)> products)
        {
            var factors = Mulf.LinearChildren(product);
            var (matrices, others) = (0, 0);
            foreach (var factor in factors)
                if (IsAMatrixNotWrittenOut(factor, products))
                    matrices++;
                else if (factor is not Number)
                    others++;
            return matrices > 0 && matrices + others >= 2;
        }
    }
}
