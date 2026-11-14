//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// The calculus nodes and the binders -- a derivative, an integral, a limit, a sum, a product, an extremum over a
    /// set, a lambda and its application -- have no compiled form, and both compilers refuse them alike. Each is
    /// computed by evaluation, which compiling them would have to repeat at every call, as slowly as evaluating; so the
    /// caller evaluates once and compiles what comes back. The LINQ compiler reached the bound variable first and said
    /// it was not among the arguments, which named the symptom rather than the node.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class Binders
    {
        internal static void Check(Entity expression)
        {
            foreach (var node in expression.Nodes)
                if (Kind(node) is { } kind)
                    throw new UncompilableNodeException(
                        $"`{node.Stringize()}` is {kind}, which has no compiled form. "
                        + (node is Lambda
                            ? "Apply it to an argument, call .Evaled or Simplify() on that, and compile the result."
                            : "Call .Evaled or Simplify() on the expression first, and compile the result."));
        }

        private static string? Kind(Entity node) => node switch
        {
            Derivativef => "a derivative",
            Integralf { Range: null } => "an integral",
            Integralf => "a definite integral",
            Limitf => "a limit",
            Summationf => "a sum",
            Productf => "a product",
            SumOverSetf => "a sum over a set",
            Maximumf => "a maximum over a set",
            Minimumf => "a minimum over a set",
            Argmaxf => "the set of points where a maximum is taken",
            Argminf => "the set of points where a minimum is taken",
            CalculusOperator => "a binder",
            Application => "an application of a function",
            Lambda => "a function rather than a number",
            _ => null
        };
    }
}
