//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// A determinant, a transpose, a trace, an entry, a scalar product or a vector product, carried out for the
    /// compilers where every name in it is compiled as a number. Simplification leaves each of them as written where a
    /// name stands beside a written matrix, since the name may stand for a matrix: <c>dot([0, 0, 1], cos(p) * [x, y, z])</c>
    /// is left as it is. A variable compiled as a number is one, so for the compiler the operand is the vector
    /// <c>[cos(p) * x, cos(p) * y, cos(p) * z]</c>, and the product is carried out on that. Left as written, the
    /// FastExpression compiler had no form for the node and refused it, and simplification before compiling read a
    /// transpose or a vector product beside a written matrix as a number:
    /// <c>[1, 0]T * transpose(x * [[1, 2], [3, 4]]) * [0, 1]</c> compiled to 0, where it is <c>3 x</c>.
    /// https://github.com/asc-community/AngouriMath/issues/105
    /// https://github.com/asc-community/AngouriMath/issues/2147
    /// </summary>
    internal static class MatrixOperationsOverNumbers
    {
        internal static bool IsMatrixOperation(Entity node)
            => node is Determinantf or Transposef or Tracef or MatrixEntryf or DotProductf or CrossProductf;

        /// <summary>
        /// <paramref name="expression"/> with each matrix operation whose names are all numbers carried out, innermost
        /// first, so that a vector product inside a scalar product is a vector by the time the scalar product is taken.
        /// One that has no value even so -- vectors of different lengths, or an argument that is a number -- is left.
        /// </summary>
        /// <param name="expression">The expression about to be compiled, its named constants already substituted.</param>
        /// <param name="isNumber">Whether a variable is compiled as a number.</param>
        internal static Entity Rewritten(Entity expression, Func<Variable, bool> isNumber)
            => expression.Nodes.Any(IsMatrixOperation)
                ? expression.Replace(node => IsMatrixOperation(node) && node.Vars.All(isNumber) ? CarriedOut(node) : node)
                : expression;

        private static Entity CarriedOut(Entity node) => node switch
        {
            Determinantf determinant => determinant.CarriedOut(isExact: true, namesAreNumbers: true),
            Transposef transpose => transpose.CarriedOut(isExact: true, namesAreNumbers: true),
            Tracef trace => trace.CarriedOut(isExact: true, namesAreNumbers: true),
            MatrixEntryf entry => entry.CarriedOut(isExact: true, namesAreNumbers: true),
            DotProductf dot => dot.CarriedOut(isExact: true, namesAreNumbers: true),
            CrossProductf cross => cross.CarriedOut(isExact: true, namesAreNumbers: true),
            _ => node,
        };

        /// <summary>
        /// Refuses a matrix operation left by <see cref="Rewritten"/> whose names are all numbers and whose argument has
        /// no matrix in it: it is taken of a number, and has no value. Refused here, before simplification reads it as a
        /// number beside a written matrix and multiplies it into the entries, which gave
        /// <c>[1, 0]T * transpose(A) * [0, 1]</c> the value 0. With <paramref name="anyLeft"/>, every matrix operation
        /// left is refused: a FastExpression has numbers for its variables and its value, and nothing left can become one.
        /// </summary>
        /// <exception cref="UncompilableNodeException">Such an operation is left.</exception>
        internal static void Check(Entity expression, Func<Variable, bool> isNumber, bool anyLeft)
        {
            if (!expression.Nodes.Any(IsMatrixOperation))
                return;
            foreach (var node in expression.Nodes)
                if (IsMatrixOperation(node) && node.Vars.All(isNumber) && (anyLeft || TakenOfANumber(node)))
                    throw new UncompilableNodeException(
                        $"`{node.Stringize()}` is taken of {Operand(node)}, and "
                        + (TakenOfANumber(node)
                            ? (node is DotProductf or CrossProductf ? "an operand" : "its argument") + " is a number where its variables are compiled as numbers."
                            : "its operands do not have that shape, or an index is not a whole number known when compiling.")
                        + (anyLeft
                            ? " A FastExpression's variables and value are numbers: compile it with Compile<..., double[]>, Compile<..., double[,]> or their Complex forms to have vectors and matrices."
                            : " Compile it with a vector or a matrix type, such as double[] or double[,], for a name that stands for one."));
        }

        // The arguments a matrix is expected in: the first of an entry, and both operands of a product.
        private static bool TakenOfANumber(Entity node)
            => node is DotProductf or CrossProductf
                ? node.DirectChildren.Any(NoMatrixIn)
                : NoMatrixIn(node.DirectChildren[0]);

        private static bool NoMatrixIn(Entity argument) => !argument.Nodes.Any(n => n is Matrix);

        private static string Operand(Entity node) => node switch
        {
            DotProductf => "two vectors of one length",
            CrossProductf => "two vectors of three components",
            Determinantf or Tracef => "a square matrix",
            _ => "a matrix",
        };
    }
}
