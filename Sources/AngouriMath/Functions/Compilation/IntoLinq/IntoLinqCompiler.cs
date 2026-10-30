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
            // A matrix has no compiled form, but an expression built out of matrices often
            // has a value that is an ordinary number -- [0, 1]T * [[a, b], [c, d]] * [1, 0]
            // is c -- and there is nothing to stop that being compiled. Nothing simplified
            // before compiling, so those failed along with the ones that genuinely cannot
            // be compiled (https://github.com/asc-community/AngouriMath/issues/425).
            // Only expressions that mention a matrix pay for this.
            if (expr.Nodes.Any(node => node is Entity.Matrix))
                expr = expr.InnerSimplified;
            expr = CompiledMembership.Rewritten(expr);

            var subexpressionsCache = typesAndNames.ToDictionary(c => (Entity)c.variable, c => Expression.Parameter(c.type));
            var functionArguments = subexpressionsCache.Select(c => c.Value).ToArray(); // copying
            var localVars = new List<ParameterExpression>();
            var variableAssignments = new List<Expression>();

            // Linq.Expression refuses a mismatch by throwing its own exceptions -- an
            // InvalidOperationException for `not x` where x is a double, an ArgumentException
            // for a Providedf whose condition is not boolean -- and those were reaching the
            // caller unwrapped, so a CAS raised exceptions outside its own documented hierarchy
            // for input it simply cannot compile. The mismatch is real and the answer is the
            // exception written down for it.
            // https://github.com/asc-community/AngouriMath/issues/894
            try
            {
                var tree = BuildTree(expr, subexpressionsCache, variableAssignments, localVars, protocol);
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

        internal static Expression BuildTree(
            Entity expr,
            Dictionary<Entity, ParameterExpression> cachedSubexpressions,
            List<Expression> variableAssignments,
            List<ParameterExpression> newLocalVars,
            CompilationProtocol protocol)
        {
            if (cachedSubexpressions.TryGetValue(expr, out var readyVar))
                return readyVar;

            Expression subTree = expr switch
            {
                Variable { IsConstant: true } c
                    => BuildTree(c.Evaled, cachedSubexpressions, variableAssignments, newLocalVars, protocol),

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

                Entity.Boolean or Number => protocol.ConvertConstant(expr),

                IUnaryNode oneArg
                    => protocol.ConvertUnaryNode(
                        BuildTree(oneArg.NodeChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol),
                        expr),

                // A membership in a set of numbers, the set not being a value: only the element is computed.
                // https://github.com/asc-community/AngouriMath/issues/2195
                Set.Inf(var element, Set.SpecialSet special) when AngouriMath.Numerics.NumberStatements.Of(special) is { } set
                    => protocol.ConvertMembership(
                        BuildTree(element, cachedSubexpressions, variableAssignments, newLocalVars, protocol), set),

                IBinaryNode twoArg
                    => protocol.ConvertBinaryNode(
                        BuildTree(twoArg.NodeFirstChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol), 
                        BuildTree(twoArg.NodeSecondChild, cachedSubexpressions, variableAssignments, newLocalVars, protocol), 
                        expr),

                var other => protocol.ConvertOtherNode(other.DirectChildren.Select(c => BuildTree(c, cachedSubexpressions, variableAssignments, newLocalVars, protocol)), expr)
            };

            var newVar = Expression.Variable(subTree.Type);
            variableAssignments.Add(Expression.Assign(newVar, subTree));
            cachedSubexpressions[expr] = newVar;
            newLocalVars.Add(newVar);
            return newVar;
        }
    }
}
