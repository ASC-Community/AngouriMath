//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// Where a statement may stand in an expression that is compiled: at the root, as an operand of a connective, on
    /// both sides of an equality of statements, in an Iverson bracket, as a provided's or a piecewise's value or
    /// predicate, and in a set or a quantifier. Anywhere a number is expected a statement has no value, as evaluation
    /// leaves <c>sin(True)</c> as written; FastExpression, which carries a statement as its truth value, answered
    /// <c>sin(1)</c>. Its truth value as a number is <c>iverson</c>'s, and a statement anywhere else is refused, by
    /// both compilers alike.
    /// https://github.com/asc-community/AngouriMath/issues/2227
    /// </summary>
    internal static class StatementPositions
    {
        internal static void Check(Entity expression)
        {
            foreach (var node in expression.Nodes)
            {
                if (TakesStatements(node))
                    continue;
                foreach (var child in node.DirectChildren)
                    if (IsStatementValued(child))
                        throw new UncompilableNodeException(
                            $"`{child.Stringize()}` is a statement where `{node.Stringize()}` expects a number, and has no value there. "
                            + $"Its truth value as a number is iverson({child.Stringize()}).");
            }
        }

        private static bool TakesStatements(Entity node)
            => node is Andf or Orf or Xorf or Impliesf or Notf or Iversonf or Providedf or Piecewise or Set or Quantifier
               || node is Equalsf(var left, var right) && IsStatementValued(left) == IsStatementValued(right);

        private static bool IsStatementValued(Entity entity) => entity switch
        {
            Statement => true,
            Providedf(var value, _) => IsStatementValued(value),
            Piecewise piecewise => piecewise.Cases.Any(@case => IsStatementValued(@case.Expression)),
            _ => false
        };
    }
}
