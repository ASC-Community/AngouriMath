//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Core.FastExpression;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// Compile function so you can evaluate numerical value 15x faster,
        /// than subsitution
        /// </summary>
        /// <param name="variables">
        /// List string names of variables in the same order as you will list them when evaluating.
        /// Constants, i.e. <see cref="MathS.pi"/> and <see cref="MathS.e"/> will be ignored.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// A statement compiles to its truth value: 1 where it holds, 0 where it does not, and NaN
        /// where it has none, as <c>i &gt; 1</c> has none. A <c>provided</c> or a <c>piecewise</c> is NaN
        /// where no predicate holds. https://github.com/asc-community/AngouriMath/issues/2195
        /// </remarks>
        public FastExpression Compile(params Variable[] variables) => Compiler.Compile(this, variables);

        /// <summary>
        /// Compile function so you can evaluate numerical value 15x faster,
        /// than subsitution
        /// </summary>
        /// <param name="variables">
        /// List string names of variables in the same order as you will list them when evaluating.
        /// Constants, i.e. <see cref="MathS.pi"/> and <see cref="MathS.e"/> will be ignored.
        /// </param>
        /// <returns></returns>
        /// <remarks>A statement compiles to its truth value, as <see cref="Compile(Variable[])"/> says.</remarks>
        public FastExpression Compile(params string[] variables) =>
            Compiler.Compile(this, variables.Select(x => (Variable)x));
    }
}