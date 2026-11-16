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
            if (Refusal(expression) is { } refusal)
                throw new UncompilableNodeException(refusal.Message);
        }

        /// <summary>
        /// The first node in <paramref name="expression"/> that has no compiled form -- a binder, or a node
        /// whose value is not a number -- and why, or null where there is none.
        /// </summary>
        internal static (Entity Node, string Message)? Refusal(Entity expression)
        {
            foreach (var node in expression.Nodes)
                if (NotANumber(node) is { } what)
                    return (node,
                        $"`{node.Stringize()}` is {what}, not a number, so it has no compiled form, and evaluating it does not give one.");
                else if (Kind(node) is { } kind)
                    return (node,
                        $"`{node.Stringize()}` is {kind}, which has no compiled form. "
                        + (node is Lambda
                            ? "Apply it to an argument, call .Evaled or Simplify() on that, and compile the result."
                            : "Call .Evaled or Simplify() on the expression first, and compile the result."));
            return null;
        }

        /// <summary>
        /// A node whose value is not a number at all, so that evaluating first does not help: a prime
        /// factorisation is a map from the primes to the exponents, and its support a set of primes.
        /// https://github.com/asc-community/AngouriMath/issues/2449
        /// </summary>
        private static string? NotANumber(Entity node) => node switch
        {
            PrimeFactorizationf => "a prime factorisation, a map from the primes to their exponents",
            Set.PrimeFactorsf => "the set of the prime factors of an integer",
            _ => null
        };

        private static string? Kind(Entity node) => node switch
        {
            Derivativef => "a derivative",
            PartialDerivativef => "a partial derivative",
            Integralf { Range: null } => "an integral",
            Integralf => "a definite integral",
            Limitf => "a limit",
            Summationf => "a sum",
            Productf => "a product",
            SumOverSetf => "a sum over a set",
            ProductOverSetf => "a product over a set",
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
