//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Functions.Compilation.IntoSource;

namespace AngouriMath
{
    using static Entity;

    partial class MathS
    {
        /// <summary>
        /// Expressions printed as shader source: straight-line HLSL or GLSL that computes each
        /// output once, sharing every repeated subexpression.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The use is to derive on the CPU what is expensive or error-prone to derive by hand —
        /// an exact gradient, a Hessian, a closed form of a physical law — and run the result
        /// on the GPU. <see cref="Entity.Differentiate(Variable)"/> followed by one of these
        /// is an analytic normal for a signed distance field.
        /// </para>
        /// <para>
        /// The code computes the real-valued reading, the one <c>Compile&lt;double, …&gt;</c>
        /// computes; where that has no value the shader's is <c>NaN</c>, an infinity or
        /// unspecified, as the language's own <c>/</c>, <c>pow</c> and <c>sqrt</c> leave it.
        /// Three of those are worth naming, because a GPU does give them a value: a division by
        /// zero, <c>a / b</c> at <c>b = 0</c>, is <c>NaN</c> compiled and ±infinity in the
        /// shader; so is zero to a negative power, <c>x^(-2)</c> or <c>x^(-1/2)</c> at <c>x = 0</c>,
        /// printed <c>1.0 / (x * x)</c> and <c>rsqrt(x)</c>; and an unsimplified <c>x^0</c> is
        /// printed <c>1.0</c>, which is 1 at <c>x = 0</c> where compiled it is <c>NaN</c>. A
        /// branch on every quotient would cost more than the difference is worth, so the
        /// difference is documented rather than paid for. Nor is <c>pow</c> of a negative base
        /// defined in a shader, even where the exponent is a variable that happens to be whole.
        /// </para>
        /// <para>
        /// A node with no faithful form — a complex constant, a set, a piecewise with inputs it
        /// gives no value to, a function with no intrinsic and no straight-line form — is declined
        /// with the node named, never printed approximately; and what <c>Compile</c> refuses, a
        /// binder or a statement where a number is expected, is declined with <c>Compile</c>'s
        /// message.
        /// </para>
        /// <para>
        /// Nothing is simplified. Call <see cref="Entity.Simplify(int)"/> first if a smaller
        /// form is wanted: what is printed is the expression that was given. The only rewrites
        /// are the ones <c>Compile</c> makes as well: an operation on matrices written out is
        /// the number it gives, a membership in an interval or a finite set is the comparisons
        /// it stands for, and <c>(b^v + c1) / (b^v + c2)</c>, as <c>tanh</c> is spelled, is taken
        /// as <c>1 + (c1 - c2) / (b^v + c2)</c> so that it does not overflow.
        /// </para>
        /// <para>
        /// The page <c>Docs/Usage/Shaders.md</c> works an example through.
        /// </para>
        /// </remarks>
        public static class Shaders
        {
            /// <summary>
            /// HLSL statements assigning each output: <c>float</c> locals for the shared
            /// subexpressions, then one assignment per output, in the order given.
            /// </summary>
            /// <param name="outputs">The target of each assignment — a variable, an
            /// <c>out</c> parameter, a swizzle such as <c>n.x</c> — and the expression it gets.</param>
            /// <param name="names">
            /// What each variable is called in the shader, such as <c>p.x</c> for <c>x</c>. A
            /// variable not listed keeps its own name, which has to be an identifier.
            /// </param>
            /// <returns>The source, or which subexpression could not be printed faithfully and why.</returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Collections.Generic;
            /// using AngouriMath;
            ///
            /// Entity r = "sqrt(x^2 + y^2)";
            /// var source = MathS.Shaders.Hlsl(new Dictionary&lt;string, Entity&gt;
            /// {
            ///     ["g.x"] = r.Differentiate("x"),
            ///     ["g.y"] = r.Differentiate("y"),
            /// }.ToList(), new Dictionary&lt;Entity.Variable, string&gt; { ["x"] = "p.x", ["y"] = "p.y" });
            /// Console.Write(source.Code);
            /// </code>
            /// Prints
            /// <code>
            /// float am0 = (0.5 * rsqrt(((p.x * p.x) + (p.y * p.y))));
            /// g.x = (am0 * (2.0 * p.x));
            /// g.y = (am0 * (2.0 * p.y));
            /// </code>
            /// </example>
            public static GeneratedSource Hlsl(IReadOnlyList<KeyValuePair<string, Entity>> outputs, IReadOnlyDictionary<Variable, string>? names = null)
                => ShaderSource.Emit(SourceDialect.Hlsl, outputs, names);

            /// <summary>
            /// GLSL statements assigning each output, as <see cref="Hlsl"/> prints them, with
            /// GLSL's names for the intrinsics that differ.
            /// </summary>
            /// <param name="outputs">The target of each assignment and the expression it gets.</param>
            /// <param name="names">What each variable is called in the shader.</param>
            /// <returns>The source, or which subexpression could not be printed faithfully and why.</returns>
            public static GeneratedSource Glsl(IReadOnlyList<KeyValuePair<string, Entity>> outputs, IReadOnlyDictionary<Variable, string>? names = null)
                => ShaderSource.Emit(SourceDialect.Glsl, outputs, names);
        }
    }
}
