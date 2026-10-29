//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Antlr
{
    internal static class ParsingHelpers
    {
        internal static Matrix TryBuildingMatrix(List<Entity> elements)
        {
            if (!elements.Any())
                return MathS.Vector(elements.ToArray());
            var first = elements.First();
            if (first is not Matrix { IsVector: true } firstVec)
                return MathS.Vector(elements.ToArray());
            var tb = new MatrixBuilder(firstVec.RowCount);
            foreach (var row in elements)
            {
                if (row is not Matrix { IsVector: true } rowVec)
                    return MathS.Vector(elements.ToArray());
                if (rowVec.RowCount != firstVec.RowCount)
                    return MathS.Vector(elements.ToArray());
                tb.Add(rowVec);
            }
            return tb.ToMatrix() ?? throw new AngouriBugException("Should've been checked already");
        }

        // det, transpose, dot, cross and resultant are computed from what they are applied to, and
        // there is no node for any of them: det(A) of a symbol A has nothing to be. So an argument
        // has to be a matrix written out, or an expression that is one once its operations are
        // carried out, as 2 [[1, 2], [3, 4]] is.
        // https://github.com/asc-community/AngouriMath/issues/2147
        private static Matrix MatrixArgument(string function, Entity argument, string example)
            => argument as Matrix ?? argument.InnerSimplified as Matrix
                ?? throw new InvalidArgumentParseException(
                    $"{function} is computed from the entries of a matrix or a vector written out, " +
                    $"as in {example}, and {argument} is not one");

        /// <summary>The entries of a vector, written as a column or as a row.</summary>
        private static Entity[] Components(string function, Entity argument)
        {
            var vector = MatrixArgument(function, argument, $"{function}([1, 2, 3], [4, 5, 6])");
            if (vector.ColumnCount == 1)
                return Enumerable.Range(0, vector.RowCount).Select(i => vector[i, 0]).ToArray();
            if (vector.RowCount == 1)
                return Enumerable.Range(0, vector.ColumnCount).Select(j => vector[0, j]).ToArray();
            throw new InvalidArgumentParseException(
                $"{function} is taken of vectors, and {vector} is a {vector.RowCount} by {vector.ColumnCount} matrix");
        }

        /// <summary><c>det(M)</c>: the determinant of a square matrix.</summary>
        internal static Entity Determinant(Entity argument)
        {
            var matrix = MatrixArgument("det", argument, "det([[1, 2], [3, 4]])");
            return matrix.Determinant ?? throw new InvalidArgumentParseException(
                $"det is taken of a square matrix, and {matrix} is {matrix.RowCount} by {matrix.ColumnCount}");
        }

        /// <summary><c>transpose(M)</c>, which <c>]T</c> writes only after a bracket.</summary>
        internal static Entity Transposed(Entity argument)
            => MatrixArgument("transpose", argument, "transpose([[1, 2], [3, 4]])").T;

        /// <summary>
        /// <c>dot(a, b)</c>: the sum of the products of the components, without conjugating either
        /// side, as SymPy's <c>dot</c> and Mathematica's <c>Dot</c> are.
        /// </summary>
        internal static Entity Dot(Entity left, Entity right)
        {
            var a = Components("dot", left);
            var b = Components("dot", right);
            if (a.Length != b.Length)
                throw new InvalidArgumentParseException(
                    $"dot is taken of two vectors of one length, and these have {a.Length} and {b.Length} components");
            return Enumerable.Range(0, a.Length).Select(i => a[i] * b[i]).Aggregate((sum, term) => sum + term);
        }

        /// <summary>
        /// <c>cross(a, b)</c>, the vector product in three dimensions, and <c>cross(a)</c>, the vector
        /// in the plane that <c>a</c> turns into through a right angle anticlockwise: <c>(-a_2, a_1)</c>,
        /// as Mathematica's <c>Cross</c> of one vector is. So <c>dot(cross(a), b)</c> is the plane's
        /// pseudo-scalar product <c>a_1 b_2 - a_2 b_1</c>.
        /// </summary>
        internal static Entity Cross(List<Entity> arguments)
        {
            if (arguments.Count == 1)
            {
                var a = Components("cross", arguments[0]);
                if (a.Length != 2)
                    throw new InvalidArgumentParseException(
                        $"cross of one vector is its turn through a right angle in the plane, and this vector has {a.Length} components");
                return MathS.Vector(-a[1], a[0]);
            }
            var u = Components("cross", arguments[0]);
            var v = Components("cross", arguments[1]);
            if (u.Length != 3 || v.Length != 3)
                throw new InvalidArgumentParseException(
                    $"cross of two vectors is the vector product in three dimensions, and these have {u.Length} and {v.Length} components");
            return MathS.Vector(
                u[1] * v[2] - u[2] * v[1],
                u[2] * v[0] - u[0] * v[2],
                u[0] * v[1] - u[1] * v[0]);
        }

        /// <summary><c>resultant(p, q, x)</c>, eliminating <c>x</c> between two polynomials.</summary>
        internal static Entity Resultant(Entity left, Entity right, Entity eliminate)
        {
            if (eliminate is not Variable { IsConstant: false } variable)
                throw new InvalidArgumentParseException(
                    $"resultant's third argument is the variable to eliminate, and {eliminate} is not a variable");
            return MathS.Polynomials.Resultant(left, right, variable) ?? throw new InvalidArgumentParseException(
                $"resultant is taken of two polynomials in {variable} with rational coefficients, and {left} and {right} are not both");
        }

        // laplacian, jacobian and hessian are written in the derivatives they are made of, which
        // stay derivatives until they are evaluated: the operators are taken in the variables
        // listed, a vector of names written out, [x, y, z], or one name for a single variable.
        // https://github.com/asc-community/AngouriMath/issues/2182
        private static Variable[] Variables(string function, Entity argument, string example)
        {
            var names = argument is Variable ? new[] { argument }
                : argument is Matrix ? Components(function, argument)
                : throw new InvalidArgumentParseException(
                    $"{function} is taken in the variables listed as a vector, as in {example}, and {argument} is not one");
            var variables = new Variable[names.Length];
            for (var i = 0; i < names.Length; i++)
                variables[i] = names[i] is Variable { IsConstant: false } variable && !variables.Take(i).Contains(variable) ? variable
                    : throw new InvalidArgumentParseException(
                        $"{function} is taken in distinct variables, as in {example}, and {names[i]} is {(names[i] is Variable { IsConstant: false } ? "listed twice" : "not a variable")}");
            return variables;
        }

        /// <summary>The function a scalar operator is taken of, refused where it is a vector or a matrix.</summary>
        private static Entity ScalarFunction(string function, Entity argument, string example)
            => argument is Matrix
                ? throw new InvalidArgumentParseException($"{function} is taken of a scalar function, as in {example}, and {argument} is a matrix")
                : argument;

        /// <summary>
        /// <c>laplacian(f, [x, y, z])</c>: the sum of the second derivatives of a scalar <c>f</c> in
        /// each variable, <c>∇²f</c>.
        /// </summary>
        internal static Entity Laplacian(Entity function, Entity variables)
        {
            const string example = "laplacian(x^2 y, [x, y])";
            var f = ScalarFunction("laplacian", function, example);
            return Variables("laplacian", variables, example)
                .Select(x => MathS.Derivative(f, x, 2))
                .Aggregate((sum, term) => sum + term);
        }

        /// <summary>
        /// <c>jacobian(F, [x, y])</c>: the matrix of the first derivatives of the components of
        /// <c>F</c>, a row for each component and a column for each variable, so that the entry in
        /// row <c>i</c> and column <c>j</c> is <c>∂F_i/∂x_j</c>. A scalar <c>F</c> is one row.
        /// </summary>
        internal static Entity Jacobian(Entity function, Entity variables)
        {
            var x = Variables("jacobian", variables, "jacobian([x y, x + y], [x, y])");
            var components = function is Matrix ? Components("jacobian", function) : new[] { function };
            return MathS.Matrix(components.Length, x.Length, (i, j) => MathS.Derivative(components[i], x[j]));
        }

        /// <summary>
        /// <c>hessian(f, [x, y])</c>: the matrix of the second derivatives of a scalar <c>f</c>, the
        /// entry in row <c>i</c> and column <c>j</c> being <c>∂²f/∂x_i∂x_j</c>, the derivative in
        /// <c>x_i</c> of the derivative in <c>x_j</c>.
        /// </summary>
        internal static Entity Hessian(Entity function, Entity variables)
        {
            const string example = "hessian(x^2 y, [x, y])";
            var f = ScalarFunction("hessian", function, example);
            var x = Variables("hessian", variables, example);
            return MathS.Matrix(x.Length, x.Length,
                (i, j) => i == j ? MathS.Derivative(f, x[i], 2) : MathS.Derivative(MathS.Derivative(f, x[j]), x[i]));
        }

        /// <summary>
        /// <c>multinomial(k_1, ..., k_m)</c>: <c>(k_1 + ... + k_m)! / (k_1! ... k_m!)</c>, the number of
        /// ways to put <c>k_1 + ... + k_m</c> things into groups of those sizes. It is written in the
        /// factorial, which is the gamma function away from the whole numbers, so it is symmetric in its
        /// arguments everywhere and has no value where an argument or their sum is a negative integer.
        /// It is not the product of binomial coefficients the multinomial theorem factors it into:
        /// <c>binomial</c> is 0 at a negative whole lower index, and that product depends on the order
        /// of the arguments there.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </summary>
        internal static Entity Multinomial(List<Entity> arguments)
        {
            var sum = arguments.Skip(1).Aggregate(arguments[0], (total, next) => total + next);
            var denominator = arguments.Skip(1).Aggregate((Entity)MathS.Factorial(arguments[0]), (product, next) => product * MathS.Factorial(next));
            return MathS.Factorial(sum) / denominator;
        }

        /// <summary>
        /// <c>forwarddifference(f, k)</c>: <c>f(k + 1) - f(k)</c>, the forward difference of
        /// <c>f</c> in <c>k</c> with a step of 1, as Mathematica's <c>DifferenceDelta[f, k]</c>. It is
        /// written so when it is parsed: <c>forwarddifference(k^2, k)</c> is <c>(k + 1)^2 - k^2</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </summary>
        internal static Entity ForwardDifference(Entity function, Entity variable)
            => variable is Variable { IsConstant: false } k
                ? function.Substitute(k, k + 1) - function
                : throw new InvalidArgumentParseException(
                    $"forwarddifference is taken in a variable, as in forwarddifference(k^2, k), and {variable} is not one");

        /// <summary>
        /// The <see cref="Number.Rational"/> that a quotient of two integer literals denotes, or
        /// the node unchanged where it denotes none.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A <see cref="Number.Rational"/> prints as <c>7/2</c> and there is no rational literal
        /// in the grammar, so re-parsing gave a <see cref="Divf"/> and the round trip was not an
        /// identity — the value survived and the node did not. That is
        /// https://github.com/asc-community/AngouriMath/issues/946's neighbour,
        /// https://github.com/asc-community/AngouriMath/issues/873, answered there with "if you
        /// have two integers on both sides of the division, it is reasonable to try to parse it
        /// as a rational".
        /// </para>
        /// <para>
        /// <b>A quotient that reduces to an integer is left alone</b>, deliberately. Parsing is
        /// not simplification: turning <c>4/2</c> into <c>2</c> would discard what the caller
        /// wrote, and <c>4/2</c> already round-trips, being a <see cref="Divf"/> before and after.
        /// Only the non-integer case is what a <see cref="Number.Rational"/> can print as, so only
        /// it is what the round trip needs.
        /// </para>
        /// <para>
        /// This agrees with the normalisation rather than anticipating it:
        /// <c>Divf(1, 2).InnerSimplified</c> was already a <see cref="Number.Rational"/>, so the
        /// parser was the one step that disagreed.
        /// </para>
        /// <para>
        /// The codomain is not read here and not carried. Every caller either has no codomain to
        /// carry — the sweep over a finished tree, where an annotation cannot have reached a
        /// quotient — or is <c>domain(...)</c> itself, which applies its own afterwards.
        /// </para>
        /// </remarks>
        internal static Entity RationalLiteral(Entity node)
        {
            if (node is not Divf(Number.Integer numerator, Number.Integer denominator))
                return node;
            if (denominator.EInteger.IsZero)
                return node;
            var value = Number.Rational.Create(numerator.EInteger, denominator.EInteger);
            return value is Number.Integer ? node : value;
        }
    }
}
