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
        /// computes; where that has no value the shader's is <c>NaN</c> or unspecified, as the
        /// language's own <c>pow</c> and <c>sqrt</c> leave it. A node with no faithful form —
        /// a complex constant, a set, a piecewise with inputs it gives no value to — is declined
        /// with the node named, never printed approximately.
        /// </para>
        /// <para>
        /// Nothing is simplified. Call <see cref="Entity.Simplify(int)"/> first if a smaller
        /// form is wanted: what is printed is the expression that was given.
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
