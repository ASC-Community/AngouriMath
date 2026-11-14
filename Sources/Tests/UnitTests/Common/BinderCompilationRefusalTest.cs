//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The calculus nodes and the binders have no compiled form, and both compilers refuse them with
    /// <see cref="UncompilableNodeException"/>, naming the node and saying to evaluate first. The LINQ compiler
    /// said the bound variable was not among the arguments.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class BinderCompilationRefusalTest
    {
        [Theory]
        [InlineData("derivative(x^2, x)", "derivative(x ^ 2, x)")]
        [InlineData("integral(x^2, x)", "integral(x ^ 2, x)")]
        [InlineData("integral(x t, t, 0, 1)", "integral(x * t, t, 0, 1)")]
        [InlineData("limit(sin(x t)/t, t, 0)", "limit(sin(x * t) / t, t, 0)")]
        [InlineData("sum(x^k, k, 1, 3)", "sum(x ^ k, k, 1, 3)")]
        [InlineData("product(x + k, k, 1, 3)", "product(x + k, k, 1, 3)")]
        [InlineData("sum(x k, k in {1, 2})", "sum(x * k, k in { 1, 2 })")]
        [InlineData("max(x k, k in {1, 2})", "max(x * k, k in { 1, 2 })")]
        [InlineData("min(x k, k in {1, 2})", "min(x * k, k in { 1, 2 })")]
        [InlineData("argmax(x k, k in {1, 2})", "argmax(x * k, k in { 1, 2 })")]
        [InlineData("argmin(x k, k in {1, 2})", "argmin(x * k, k in { 1, 2 })")]
        [InlineData("apply(lambda(t, t + 1), x)", "apply(lambda(t, t + 1), x)")]
        [InlineData("lambda(t, t + x)", "lambda(t, t + x)")]
        [InlineData("1 + sin(derivative(x^2, x))", "derivative(x ^ 2, x)")]
        public void EachCompilerRefusesItAndSaysToEvaluateFirst(string expression, string node)
        {
            var entity = expression.ToEntity();
            AssertRefused(Assert.Throws<UncompilableNodeException>(() => entity.Compile("x")), node);
            AssertRefused(Assert.Throws<UncompilableNodeException>(() => entity.Compile<Complex, Complex>("x")), node);
            AssertRefused(Assert.Throws<UncompilableNodeException>(() => entity.Compile<double, double>("x")), node);
        }

        private static void AssertRefused(UncompilableNodeException thrown, string node)
        {
            Assert.Contains($"`{node}`", thrown.Message);
            Assert.Contains(".Evaled", thrown.Message);
            Assert.Contains("Simplify()", thrown.Message);
        }

        /// <summary>What the message advises compiles, and agrees with evaluation.</summary>
        [Theory]
        [InlineData("derivative(x^3, x)")]
        [InlineData("integral(x t, t, 0, 1)")]
        [InlineData("limit(sin(x t)/t, t, 0)")]
        [InlineData("sum(x^k, k, 1, 3)")]
        [InlineData("product(x + k, k, 1, 3)")]
        [InlineData("apply(lambda(t, t^2 + 1), x)")]
        public void TheEvaluatedExpressionCompiles(string expression)
        {
            var evaled = expression.ToEntity().Evaled;
            var expected = ((Entity.Number.Complex)evaled.Substitute("x", 3).Evaled).ToNumerics();
            Assert.True(Complex.Abs(evaled.Compile("x").Call(3) - expected) < 1e-9);
            Assert.True(Complex.Abs(evaled.Compile<Complex, Complex>("x")(3) - expected) < 1e-9);
        }
    }
}
