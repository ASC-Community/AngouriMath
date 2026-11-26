//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;

namespace AngouriMath
{
    partial record Entity
    {
        /// <remarks>
        /// The exception says what the string parsed to instead, and for a name with a digit against it, which parses
        /// as a power, which spelling is the variable: <c>Entity.Variable v0 = "v0"</c> said only that it could not
        /// parse a variable from <c>v0</c>, which is <c>v ^ 0</c>.
        /// https://github.com/asc-community/AngouriMath/issues/2567
        /// </remarks>
        private static T FromString<T>(string expr) where T : Entity
        {
            var ent = expr.ToEntity();
            if (ent is T tEnt)
                return tEnt;
            var why = $"it parses as `{ent.Stringize()}`";
            if (typeof(T) == typeof(Variable) && ent is Powf(Variable(var name), Number.Integer power)
                && expr.Trim() == name + power.Stringize())
                why += $": a digit written against a name is its power, and `{name}_{power.Stringize()}` is the variable with that index";
            throw new CannotParseInstanceException(typeof(T), expr, why);
        }

#pragma warning disable SealedOrAbstract // Constant derives from it, as Real does from Complex
        partial record Variable
#pragma warning restore SealedOrAbstract
        {
            /// <summary>
            /// Converts from string to specifically variable
            /// </summary>
            /// <param name="expr">Its future name</param>
            public static implicit operator Variable(string expr)
                => FromString<Variable>(expr);
        }

        partial record Matrix
        {
            /// <summary>
            /// Converts from string to specifically tensor
            /// </summary>
            /// <param name="expr">From where to parse (available since 1.3)</param>
            public static implicit operator Matrix(string expr)
                => FromString<Matrix>(expr);
        }

        partial record Set
        {
            /// <summary>
            /// Converts from string to specifically set
            /// </summary>
            /// <param name="expr">From where to parse</param>

            public static implicit operator Set(string expr)
                => FromString<Set>(expr);

            partial record FiniteSet
            {
                /// <summary>
                /// Converts from string to specifically finite set
                /// </summary>
                /// <param name="expr">From where to parse</param>
                public static implicit operator FiniteSet(string expr)
                    => FromString<FiniteSet>(expr);
            }
        }
    }
}
