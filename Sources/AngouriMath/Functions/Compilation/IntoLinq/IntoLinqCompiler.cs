//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Compilation.IntoLinq
{
    internal static class IntoLinqCompiler
    {
        internal static TDelegate Compile<TDelegate>(
            Entity expr,
            Type? returnType,
            CompilationProtocol protocol,
            IEnumerable<(Type type, Variable variable)> typesAndNames
            ) where TDelegate : Delegate
        {
            bool IsMatrixArgument(Variable variable) => typesAndNames.Any(c => c.variable == variable && TensorCompilation.IsMatrixType(c.type));
            Type? TypeOf(Variable variable) => typesAndNames.FirstOrDefault(c => c.variable == variable).type;
            // A matrix has no compiled form, but an expression built out of matrices often
            // has a value that is an ordinary number -- [0, 1]T * [[a, b], [c, d]] * [1, 0]
            // is c -- and there is nothing to stop that being compiled. Nothing simplified
            // before compiling, so those failed along with the ones that genuinely cannot
            // be compiled (https://github.com/asc-community/AngouriMath/issues/425).
            // Only expressions that mention a matrix pay for this.
            if (expr.Nodes.Any(node => node is Entity.Matrix))
                expr = TensorCompilation.SimplifiedAroundMatrixArguments(expr, IsMatrixArgument);
            expr = CompiledMembership.Rewritten(expr);
            expr = Functions.ExponentialQuotients.Rewritten(expr);
            StatementPositions.Check(expr);
            // Two matrices whose shapes are written into the expression and do not fit are refused here, rather than by
            // a delegate that throws at every point it is called at. https://github.com/asc-community/AngouriMath/issues/526
            Func<Variable, (int Rows, int Columns)?> argumentShape = variable => TensorCompilation.ShapeOfArgument(TypeOf(variable));
            TensorCompilation.CheckShapes(expr, argumentShape);
            CheckResultShape(expr, returnType, argumentShape);
            if (MathS.Settings.RefuseNonRealCompilation && returnType is { } real && IsRealType(real)
                && typesAndNames.All(c => IsRealType(c.type)) && !IsRealWhereverDefined(expr, typesAndNames))
                throw new UncompilableNodeException(
                    $"`{expr.Stringize()}` is not real wherever it is defined, and MathS.Settings.RefuseNonRealCompilation is on: compile it over Complex");

            var subexpressionsCache = typesAndNames.ToDictionary(c => (Entity)c.variable, c => Expression.Parameter(c.type));
            var functionArguments = subexpressionsCache.Select(c => c.Value).ToArray(); // copying
            var localVars = new List<ParameterExpression>();
            var variableAssignments = new List<Expression>();
            // A matrix passed in as a two-dimensional array is read into the tensor the arithmetic runs on, once.
            foreach (var (_, variable) in typesAndNames)
                if (subexpressionsCache[variable] is var argument && TensorCompilation.IsMatrixArray(argument.Type))
                {
                    var asTensor = TensorCompilation.FromArrayArgument(argument);
                    var local = Expression.Variable(asTensor.Type);
                    localVars.Add(local);
                    variableAssignments.Add(Expression.Assign(local, asTensor));
                    subexpressionsCache[variable] = local;
                }

            // Linq.Expression refuses a mismatch by throwing its own exceptions -- an
            // InvalidOperationException for `not x` where x is a double, an ArgumentException
            // for a Providedf whose condition is not boolean -- and those were reaching the
            // caller unwrapped, so a CAS raised exceptions outside its own documented hierarchy
            // for input it simply cannot compile. The mismatch is real and the answer is the
            // exception written down for it.
            // https://github.com/asc-community/AngouriMath/issues/894
            try
            {
                // Over Complex a constant is a Complex too, so that a function of it is taken over Complex: as a long,
                // sqrt(-1) widened only to double and was NaN. https://github.com/asc-community/AngouriMath/issues/2235
                // A matrix over Complex, as an argument or as the result, is a Complex the same way.
                static bool IsComplex(Type? type)
                    => type == typeof(System.Numerics.Complex) || (type is not null && TensorCompilation.ElementOfMatrixType(type) == typeof(System.Numerics.Complex));
                // Over a matrix argument of one element type, a written matrix beside it is over that type too, so that
                // the two combine: [[1, 0]] * A over double is not a matrix of long times a matrix of double.
                var matrixElements = typesAndNames.Select(c => TensorCompilation.ElementOfMatrixType(c.type)).OfType<Type>().Distinct().ToList();
                var constantType = typesAndNames.Any(c => IsComplex(c.type)) || IsComplex(returnType)
                    ? typeof(System.Numerics.Complex)
                    : matrixElements.Count == 1 ? matrixElements[0] : null;
                // Only a matrix argument can be a vector whose shape is not written out: abs of a written one is its norm
                // after simplification already.
                if (typesAndNames.Any(c => TensorCompilation.IsMatrixType(c.type)))
                    BuildNorms(expr, argumentShape, subexpressionsCache, variableAssignments, localVars, protocol, constantType);
                var tree = BuildTree(expr, subexpressionsCache, variableAssignments, localVars, protocol, constantType);
                // A matrix known to be 1 x 1 asked for as a number is its entry, as evaluation has it. Any other matrix
                // is refused as a number by ConvertType.
                if (TensorCompilation.IsTensor(tree.Type) && returnType is not null && !TensorCompilation.IsMatrixType(returnType)
                    && TensorCompilation.IsOneByOne(expr, argumentShape))
                    tree = TensorCompilation.ToScalar(tree);
                var treeWithLocals = Expression.Block(localVars, variableAssignments.Append(tree));
                Expression entireExpression = returnType is not null ? protocol.ConvertType(treeWithLocals, returnType) : treeWithLocals;
                var finalLambda = Expression.Lambda<TDelegate>(entireExpression, functionArguments);

                return finalLambda.Compile();
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException
                                          or NotSupportedException
                                      && e is not AngouriMathBaseException)
            {
                throw new UncompilableNodeException(
                    $"`{expr.Stringize()}` has no compiled form for the types requested: {e.Message}");
            }
        }

        /// <summary>
        /// Every <c>abs</c> of a column vector, compiled to its norm ahead of the rest and cached, so that the tree
        /// built after it reads the number. Of a vector, <c>abs</c> is a number, and of any other matrix a matrix,
        /// and only the shape known when compiling can tell which.
        /// https://github.com/asc-community/AngouriMath/issues/2423
        /// </summary>
        private static void BuildNorms(Entity expr, Func<Variable, (int Rows, int Columns)?> argumentShape,
            Dictionary<Entity, ParameterExpression> cachedSubexpressions, List<Expression> variableAssignments,
            List<ParameterExpression> newLocalVars, CompilationProtocol protocol, Type? constantType)
        {
            foreach (var norm in TensorCompilation.Norms(expr, argumentShape))
            {
                if (cachedSubexpressions.ContainsKey(norm))
                    continue;
                var vector = BuildTree(norm.Argument, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType);
                if (!TensorCompilation.IsTensor(vector.Type))
                    continue;
                var value = Expression.Variable(typeof(double));
                variableAssignments.Add(Expression.Assign(value, TensorCompilation.Norm(vector)));
                newLocalVars.Add(value);
                cachedSubexpressions[norm] = value;
            }
        }

        /// <summary>
        /// A matrix known when compiling not to be a column, asked for as a vector, is refused then rather than by a
        /// delegate that throws at every call.
        /// </summary>
        private static void CheckResultShape(Entity expr, Type? returnType, Func<Variable, (int Rows, int Columns)?> argumentShape)
        {
            if (returnType is null || !TensorCompilation.IsVectorArray(returnType) || TensorCompilation.Shape(expr, argumentShape) is not { } shape)
                return;
            if (shape.Columns > 1)
                throw new InvalidMatrixOperationException(
                    $"`{expr.Stringize()}` is a matrix of {shape.Columns} columns, not a column vector, and cannot be compiled as {returnType}");
        }

        private static bool IsRealType(Type type)
            => type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long)
               || type == typeof(System.Numerics.BigInteger);

        /// <summary>
        /// Whether <paramref name="expr"/> is real wherever it is defined with every argument real, by
        /// <see cref="Functions.TreeAnalyzer.IsRealValued"/>. That reads one variable as real, and reads its shapes
        /// alone and never the values, so each argument is written as one fresh variable and its verdict is the
        /// verdict for them all. https://github.com/asc-community/AngouriMath/issues/2286
        /// </summary>
        private static bool IsRealWhereverDefined(Entity expr, IEnumerable<(Type type, Variable variable)> typesAndNames)
        {
            var real = Variable.CreateTemp(expr.Vars);
            var renamed = typesAndNames.Aggregate(expr, (e, argument) => e.Substitute(argument.variable, real));
            return Functions.TreeAnalyzer.IsRealValued(renamed, real);
        }

        internal static Expression BuildTree(
            Entity expr,
            Dictionary<Entity, ParameterExpression> cachedSubexpressions,
            List<Expression> variableAssignments,
            List<ParameterExpression> newLocalVars,
            CompilationProtocol protocol,
            Type? constantType = null)
        {
            if (cachedSubexpressions.TryGetValue(expr, out var readyVar))
                return readyVar;

            Expression subTree = expr switch
            {
                Variable { IsConstant: true } c
                    => BuildTree(c.Evaled, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType),

                // A compiled expression is a function of the variables it was compiled over,
                // so one it does not mention is not something it can be given a value for.
                // Reaching into the cache and letting the lookup fail said only that some
                // key was not present in some dictionary.
                Variable x => cachedSubexpressions.TryGetValue(x, out var argument)
                    ? argument
                    : throw new UncompilableNodeException(
                        $"{x} is not among the variables the expression is being compiled over" +
                        (cachedSubexpressions.Keys.OfType<Variable>().ToList() is { Count: > 0 } over
                            ? $", which are {string.Join(", ", over)}"
                            : ", which are none")),

                Number when constantType is { } type => protocol.ConvertType(protocol.ConvertConstant(expr), type),
                Entity.Boolean or Number => protocol.ConvertConstant(expr),

                IUnaryNode oneArg
                    => protocol.ConvertUnaryNode(
                        BuildTree(oneArg.NodeChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType),
                        expr),

                // A membership in a set of numbers, the set not being a value: only the element is computed.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Set.Inf(var element, Set.SpecialSet special) when AngouriMath.Numerics.NumberStatements.Of(special) is { } set
                    => protocol.ConvertMembership(
                        BuildTree(element, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType), set),

                // The value of a case is computed only where it is taken, inside the conditional, so that a case
                // that is undefined, or costly, where it is not taken is not computed there. Every subexpression is
                // otherwise a local assigned before the expression's value is, which would compute each case.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Providedf(var value, var predicate)
                    => protocol.ConvertBinaryNode(
                        Conditional(value, cachedSubexpressions, protocol, constantType),
                        BuildTree(predicate, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType),
                        expr),

                // The first predicate is always computed; the others only where every one before them fails.
                Piecewise piecewise
                    => protocol.ConvertOtherNode(piecewise.DirectChildren.Select((child, i) => i == 1
                        ? BuildTree(child, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType)
                        : Conditional(child, cachedSubexpressions, protocol, constantType)).ToArray(), expr),

                IBinaryNode twoArg
                    => protocol.ConvertBinaryNode(
                        BuildTree(twoArg.NodeFirstChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType), 
                        BuildTree(twoArg.NodeSecondChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType), 
                        expr),

                var other => protocol.ConvertOtherNode(other.DirectChildren.Select(c => BuildTree(c, cachedSubexpressions, variableAssignments, newLocalVars, protocol, constantType)), expr)
            };

            var newVar = Expression.Variable(subTree.Type);
            variableAssignments.Add(Expression.Assign(newVar, subTree));
            cachedSubexpressions[expr] = newVar;
            newLocalVars.Add(newVar);
            return newVar;
        }

        /// <summary>
        /// <paramref name="expr"/> as a block of its own, computed only where the block is: what it computes is
        /// cached inside the block and not outside it, where the block may not have been computed. What was
        /// cached before it is read as it is everywhere else.
        /// </summary>
        private static Expression Conditional(
            Entity expr,
            Dictionary<Entity, ParameterExpression> cachedSubexpressions,
            CompilationProtocol protocol,
            Type? constantType)
        {
            var cached = new Dictionary<Entity, ParameterExpression>(cachedSubexpressions);
            var locals = new List<ParameterExpression>();
            var assignments = new List<Expression>();
            var tree = BuildTree(expr, cached, assignments, locals, protocol, constantType);
            return assignments.Count == 0 ? tree : Expression.Block(locals, assignments.Append(tree));
        }
    }
}
