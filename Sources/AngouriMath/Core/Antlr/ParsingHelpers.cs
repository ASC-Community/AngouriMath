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

        // cross of one vector and resultant are computed from what they are applied to where it is a
        // matrix written out, or an expression that is one once its operations are carried out, as
        // 2 [[1, 2], [3, 4]] is. https://github.com/asc-community/AngouriMath/issues/2147
        private static Matrix MatrixArgument(string function, Entity argument, string example)
            => argument as Matrix ?? argument.InnerSimplified as Matrix
                ?? throw new InvalidArgumentParseException(
                    $"{function} is computed from the entries of a matrix or a vector written out, " +
                    $"as in {example}, and {argument} is not one");

        // A matrix written out, or null where the argument may yet stand for one -- a name, or an
        // expression with a name in it beside a matrix, as A [[1, 2], [3, 4]] is, which simplifying
        // would read as a number -- and det, transpose, trace, an entry, dot and cross are left as
        // nodes over it. A number, a statement or a set is no matrix, and is refused as it was.
        private static Matrix? MatrixOrUnknown(string function, Entity argument, string example, string what = "a matrix")
            => (argument as Matrix ?? argument.InnerSimplified) switch
            {
                Matrix when argument is not Matrix && HasANameOutsideAWrittenMatrix(argument) => null,
                Matrix matrix => matrix,
                Number or Statement or Set => throw new InvalidArgumentParseException(
                    $"{function} is taken of {what}, as in {example}, and {argument} is not one"),
                _ => null,
            };

        // A vector written out, a column or a row, or null where the operand may yet stand for one, as
        // MatrixOrUnknown reads it. A matrix written out that is not a vector is refused.
        // https://github.com/asc-community/AngouriMath/issues/105
        private static Matrix? VectorOrUnknown(string function, Entity operand)
        {
            if (MatrixOrUnknown(function, operand, $"{function}([1, 2, 3], [4, 5, 6])", "vectors") is not { } vector)
                return null;
            if (VectorComponents(vector) is null)
                throw new InvalidArgumentParseException(
                    $"{function} is taken of vectors, and {vector} is a {vector.RowCount} by {vector.ColumnCount} matrix");
            return vector;
        }

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

        /// <summary><c>det(M)</c>: the determinant of a square matrix, or the node over a name for one.</summary>
        internal static Entity Determinant(Entity argument)
        {
            if (MatrixOrUnknown("det", argument, "det([[1, 2], [3, 4]])") is not { } matrix)
                return new Determinantf(argument);
            return matrix.Determinant ?? throw new InvalidArgumentParseException(
                $"det is taken of a square matrix, and {matrix} is {matrix.RowCount} by {matrix.ColumnCount}");
        }

        /// <summary><c>transpose(M)</c>, which <c>]T</c> writes only after a bracket, or the node over a name for a matrix.</summary>
        internal static Entity Transposed(Entity argument)
            => MatrixOrUnknown("transpose", argument, "transpose([[1, 2], [3, 4]])") is { } matrix
                ? matrix.T
                : new Transposef(argument);

        /// <summary><c>trace(M)</c>: the sum of the diagonal of a square matrix, or the node over a name for one.</summary>
        internal static Entity Trace(Entity argument)
        {
            if (MatrixOrUnknown("trace", argument, "trace([[1, 2], [3, 4]])") is not { } matrix)
                return new Tracef(argument);
            return matrix.IsSquare
                ? matrix.Trace
                : throw new InvalidArgumentParseException(
                    $"trace is taken of a square matrix, and {matrix} is {matrix.RowCount} by {matrix.ColumnCount}");
        }

        /// <summary>
        /// <c>A[i, j]</c>: the entry in row <c>i</c> and column <c>j</c>, both counted from 1. Taken at once of a
        /// matrix written out where both indices are whole numbers, and refused where they name no entry; a node
        /// otherwise.
        /// </summary>
        internal static Entity Entry(Entity argument, List<Entity> indices)
        {
            if (indices.Count != 2)
                throw new InvalidArgumentParseException(
                    $"An entry of a matrix is named by its row and its column, as in A[1, 2], and {indices.Count} indices are given");
            var (row, column) = (indices[0], indices[1]);
            if (MatrixOrUnknown("An index", argument, "A[1, 2]") is not { } matrix)
                return new MatrixEntryf(argument, row, column);
            int? Position(Entity index, int count, string what)
                => index.InnerSimplified switch
                {
                    Integer i when i >= 1 && i <= count => (int)i - 1,
                    Number => throw new InvalidArgumentParseException(
                        $"{matrix} has {count} {what}s, counted from 1, and {index} is not one of them"),
                    _ => null,
                };
            return (Position(row, matrix.RowCount, "row"), Position(column, matrix.ColumnCount, "column")) is (int i, int j)
                ? matrix[i, j]
                : new MatrixEntryf(matrix, row, column);
        }

        /// <summary>
        /// <c>dot(a, b)</c>: the sum of the products of the components, without conjugating either
        /// side, as SymPy's <c>dot</c> and Mathematica's <c>Dot</c> are; or the node, where an operand
        /// is a name that may stand for a vector.
        /// </summary>
        internal static Entity Dot(Entity left, Entity right)
        {
            var (a, b) = (VectorOrUnknown("dot", left), VectorOrUnknown("dot", right));
            if (a is null || b is null)
                return new DotProductf(left, right);
            return DotProductf.Of(a, b) ?? throw new InvalidArgumentParseException(
                $"dot is taken of two vectors of one length, and these have {VectorComponents(a)!.Length} and {VectorComponents(b)!.Length} components");
        }

        /// <summary>
        /// <c>cross(a, b)</c>, the vector product in three dimensions, or the node where an operand is a
        /// name that may stand for a vector; and <c>cross(a)</c> of a vector written out, the vector
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
            var (left, right) = (VectorOrUnknown("cross", arguments[0]), VectorOrUnknown("cross", arguments[1]));
            if (left is null || right is null)
            {
                // A vector written out has no vector product unless it has three components, whatever the other one is.
                if ((left ?? right) is { } written && VectorComponents(written)!.Length != 3)
                    throw new InvalidArgumentParseException(
                        $"cross of two vectors is the vector product in three dimensions, and {written} has {VectorComponents(written)!.Length} components");
                return new CrossProductf(arguments[0], arguments[1]);
            }
            return CrossProductf.Of(left, right) ?? throw new InvalidArgumentParseException(
                $"cross of two vectors is the vector product in three dimensions, and these have {VectorComponents(left)!.Length} and {VectorComponents(right)!.Length} components");
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

        // gradient, divergence and curl are taken in the variables listed, as laplacian is; without them
        // there is no telling which derivatives are meant, and a name alone before a bracket would be
        // a product, so one argument is refused. https://github.com/asc-community/AngouriMath/issues/2182
        private static void AssertVariablesGiven(string function, List<Entity> arguments, string example)
        {
            if (arguments.Count == 1)
                throw new FunctionArgumentCountException(
                    $"{function} is taken in the variables listed as a vector, as in {example}: give them as the second argument");
            FunctionArgumentCountException.Assert(function, 2, arguments.Count);
        }

        /// <summary>The components of a vector field written out, as many as the variables it is taken in.</summary>
        private static Entity[] VectorField(string function, Entity argument, int dimension, string example)
        {
            var components = argument is Matrix || argument.InnerSimplified is Matrix
                ? Components(function, argument)
                : throw new InvalidArgumentParseException(
                    $"{function} is taken of a vector field written out, as in {example}, and {argument} is not one");
            return components.Length == dimension ? components
                : throw new InvalidArgumentParseException(
                    $"{function} is taken of a vector field with a component for each variable, and {argument} has {components.Length} for {dimension} variables");
        }

        /// <summary>
        /// <c>gradient(f, [x, y, z])</c>: the column of the first derivatives of a scalar <c>f</c>, <c>∇f</c>.
        /// </summary>
        internal static Entity Gradient(List<Entity> arguments)
        {
            const string example = "gradient(x^2 y, [x, y])";
            AssertVariablesGiven("gradient", arguments, example);
            var f = ScalarFunction("gradient", arguments[0], example);
            return MathS.Vector(Variables("gradient", arguments[1], example).Select(x => MathS.Derivative(f, x)).ToArray());
        }

        /// <summary>
        /// <c>divergence(F, [x, y, z])</c>: the sum of the derivative of each component of <c>F</c> in its
        /// variable, <c>∇ · F</c>.
        /// </summary>
        internal static Entity Divergence(List<Entity> arguments)
        {
            const string example = "divergence([x y, y z, z x], [x, y, z])";
            AssertVariablesGiven("divergence", arguments, example);
            var x = Variables("divergence", arguments[1], example);
            var components = VectorField("divergence", arguments[0], x.Length, example);
            return components.Select((component, i) => MathS.Derivative(component, x[i])).Aggregate((sum, term) => sum + term);
        }

        /// <summary>
        /// <c>curl(F, [x, y, z])</c>: <c>∇ × F</c>, the column <c>[∂F_3/∂y - ∂F_2/∂z, ∂F_1/∂z - ∂F_3/∂x,
        /// ∂F_2/∂x - ∂F_1/∂y]</c>, for a field in three variables only, as the vector product is.
        /// </summary>
        internal static Entity Curl(List<Entity> arguments)
        {
            const string example = "curl([-y, x, 0], [x, y, z])";
            AssertVariablesGiven("curl", arguments, example);
            var x = Variables("curl", arguments[1], example);
            if (x.Length != 3)
                throw new InvalidArgumentParseException(
                    $"curl is taken in three variables, as the vector product is, as in {example}, and {x.Length} are given");
            var f = VectorField("curl", arguments[0], 3, example);
            return MathS.Vector(
                MathS.Derivative(f[2], x[1]) - MathS.Derivative(f[1], x[2]),
                MathS.Derivative(f[0], x[2]) - MathS.Derivative(f[2], x[0]),
                MathS.Derivative(f[1], x[0]) - MathS.Derivative(f[0], x[1]));
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
