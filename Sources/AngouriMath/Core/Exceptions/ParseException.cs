//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;

namespace AngouriMath.Core.Exceptions
{
    /// <summary>Thrown when trying to parse an invalid string</summary>
    public abstract class ParseException : MathSException { internal ParseException(string msg) : base(msg) { } }

    /// <summary>Can only occur in the ANTLR parser when it is unclear what exactly happened</summary>
    public sealed class UnhandledParseException : ParseException { internal UnhandledParseException(string msg) : base(msg) { } }

    /// <summary>Thrown when an invalid argument passed to a AM's function</summary>
    public sealed class InvalidArgumentParseException : ParseException { internal InvalidArgumentParseException(string msg) : base(msg) { } }

    /// <summary>
    /// Is thrown only when the explicit parsing mode is enabled and the input misses
    /// some operator, for example, 2x should be 2 * x with explicit parsing mode.
    /// </summary>
    public sealed class MissingOperatorParseException : ParseException { internal MissingOperatorParseException(string msg) : base(msg) { } }
    
    /// <summary>
    /// Thrown when a name that is not a function is applied to arguments, in one of the
    /// cases where reading it as a product instead would be a silent surprise.
    /// </summary>
    /// <example>
    /// <code>
    /// "arsinh(x)".ToEntity();  // the inverse hyperbolic sine
    /// "arcsinh(x)".ToEntity(); // thrown: there is no such function
    /// </code>
    /// </example>
    public sealed class UnrecognizedFunctionParseException : ParseException
    {
        internal UnrecognizedFunctionParseException(string msg) : base(msg) { }
    }

    /// <summary>Thrown when non-known domain is passed to the domain function</summary>
    public sealed class UnrecognizedDomainException : ParseException
    {
        internal UnrecognizedDomainException(string dom) : base($"Unrecognized domain {dom}") { } 
    }

    /// <summary>
    /// May be thrown when trying to parse an instance from a string
    /// </summary>
    /// <example>
    /// <code>
    /// Set set = "{ 1, 2 }"; // not thrown
    /// Set set = "1 + 2"; // thrown
    /// </code>
    /// </example>
    public sealed class CannotParseInstanceException : ParseException
    {
        internal CannotParseInstanceException(Type type, string expr)
            : base($"Cannot parse an instance of {type.Name} from `{expr}`") { }
        internal CannotParseInstanceException(Type type, string expr, string why)
            : base($"Cannot parse an instance of {type.Name} from `{expr}`: {why}") { }
    }

    /// <summary>
    /// Thrown when a wrong number of arguments are encountered when parsing a function
    /// from a string.
    /// </summary>
    public sealed class FunctionArgumentCountException : ParseException
    {
        internal FunctionArgumentCountException(string msg) : base(msg) { }
        private static string CountArguments(int count, bool isAre) =>
            $"{count} argument{(count == 1 ? "" : "s")}{(isAre ? count == 1 ? " is" : " are" : "")}";
        internal static void Assert(string function, int expected, int actual)
        {
            if (expected != actual) throw new FunctionArgumentCountException(
                $"{function} should have exactly {CountArguments(expected, false)} but {CountArguments(actual, true)} provided");
        }
        internal static bool Assert(string function, (int, int) expected, int actual)
        {
            if (expected.Item1 == actual) return true;
            if (expected.Item2 == actual) return false;
            throw new FunctionArgumentCountException(
                $"{function} should have exactly {CountArguments(expected.Item1, false)} or {CountArguments(expected.Item2, false)} but {CountArguments(actual, true)} provided");
        }
        /// <summary>
        /// For the functions that take any number of arguments and fold over them —
        /// <c>min</c>, <c>max</c> and <c>gcd</c>, which have no fixed arity anywhere else
        /// either.
        /// </summary>
        internal static void AssertAtLeast(string function, int least, int actual)
        {
            if (actual < least) throw new FunctionArgumentCountException(
                $"{function} should have at least {CountArguments(least, false)} but {CountArguments(actual, true)} provided");
        }
    }
}
