//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>laplacian</c>, <c>jacobian</c>, <c>hessian</c>, <c>gradient</c>, <c>divergence</c> and <c>curl</c>, taken
    /// in the variables listed and made of derivatives when they are parsed. Each but <c>curl</c> was refused by name.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class VectorCalculusTest
    {
        private static void AssertSameValue(Entity expected, Entity actual)
            => Assert.Equal(Number.Integer.Zero, (actual - expected).Simplify());

        // A derivative of a logarithm or a quotient carries the condition that its argument is defined there.
        private static void AssertSameWhereDefined(Entity expected, Entity actual)
            => Assert.Equal(Number.Integer.Zero, (actual - expected).Simplify() is Providedf provided ? provided.Expression : (actual - expected).Simplify());

        private static void AssertSameMatricesWhereDefined(string expected, string actual)
        {
            var want = Assert.IsType<Matrix>(expected.ToEntity());
            var got = Assert.IsType<Matrix>(actual.ToEntity());
            Assert.Equal((want.RowCount, want.ColumnCount), (got.RowCount, got.ColumnCount));
            for (var i = 0; i < want.RowCount; i++)
                for (var j = 0; j < want.ColumnCount; j++)
                    AssertSameWhereDefined(want[i, j], got[i, j]);
        }

        private static void AssertSameMatrix(string expected, string actual)
        {
            var want = Assert.IsType<Matrix>(expected.ToEntity());
            var got = Assert.IsType<Matrix>(actual.ToEntity());
            Assert.Equal((want.RowCount, want.ColumnCount), (got.RowCount, got.ColumnCount));
            for (var i = 0; i < want.RowCount; i++)
                for (var j = 0; j < want.ColumnCount; j++)
                    AssertSameValue(want[i, j], got[i, j]);
        }

        [Theory]
        [InlineData("laplacian(x^2 y + y^3, [x, y])", "8 y")]
        [InlineData("laplacian(x^2 - y^2, [x, y])", "0")]
        [InlineData("laplacian(x^2 + y^2 + z^2, [x, y, z])", "6")]
        [InlineData("laplacian(sin(x), x)", "-sin(x)")]
        [InlineData("laplacian(e^x sin(y), [x, y])", "0")]
        public void TheLaplacianIsTheSumOfTheSecondDerivatives(string written, string expected)
            => AssertSameValue(expected.ToEntity(), written.ToEntity());

        /// <summary>
        /// 1/r is harmonic in space away from the origin. The derivatives are taken before a point
        /// is substituted, since substituting for the variable of a derivative leaves it as it is.
        /// </summary>
        [Fact]
        public void TheReciprocalOfTheDistanceIsHarmonic()
        {
            var laplacian = "laplacian(1 / sqrt(x^2 + y^2 + z^2), [x, y, z])".ToEntity().InnerSimplified;
            var at = laplacian.Substitute("x", 1).Substitute("y", 2).Substitute("z", 3).Evaled;
            Assert.True(at is Number.Complex value && Number.Abs(value).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-20")) < 0, at.Stringize());
        }

        [Theory]
        [InlineData("jacobian([x y, x + y], [x, y])", "[[y, x], [1, 1]]")]
        [InlineData("jacobian([x, y^2, x z], [x, y, z])", "[[1, 0, 0], [0, 2 y, 0], [z, 0, x]]")]
        [InlineData("jacobian(x^2 y, [x, y])", "[[2 x y, x^2]]")]
        [InlineData("jacobian([sin(t), cos(t)], t)", "[[cos(t)], [-sin(t)]]")]
        public void TheJacobianHasARowForEachComponentAndAColumnForEachVariable(string written, string expected)
            => AssertSameMatrix(expected, written);

        /// <summary>The polar map's Jacobian determinant is <c>r</c>.</summary>
        [Fact]
        public void ThePolarMapsJacobianDeterminantIsR()
            => AssertSameValue("r".ToEntity(), "det(jacobian([r cos(t), r sin(t)], [r, t]))".ToEntity());

        [Theory]
        [InlineData("hessian(x^3 y^2, [x, y])", "[[6 x y^2, 6 x^2 y], [6 x^2 y, 2 x^3]]")]
        [InlineData("hessian(x y z, [x, y, z])", "[[0, z, y], [z, 0, x], [y, x, 0]]")]
        [InlineData("hessian(x^4, x)", "[[12 x^2]]")]
        public void TheHessianIsTheMatrixOfTheSecondDerivatives(string written, string expected)
            => AssertSameMatrix(expected, written);

        /// <summary>The entry in row i and column j is the derivative in x_i of the derivative in x_j.</summary>
        [Fact]
        public void TheHessiansEntriesAreTakenInTheOrderTheyAreWritten()
        {
            var hessian = Assert.IsType<Matrix>("hessian(f, [x, y])".ToEntity());
            Assert.Equal(MathS.Derivative(MathS.Derivative("f", "y"), "x"), hessian[0, 1]);
            Assert.Equal(MathS.Derivative(MathS.Derivative("f", "x"), "y"), hessian[1, 0]);
            Assert.Equal(MathS.Derivative("f", "x", 2), hessian[0, 0]);
        }

        /// <summary>
        /// The variables are a vector of distinct names, or one name; a set has no order to give the
        /// columns. A Laplacian and a Hessian are taken of a scalar function.
        /// </summary>
        [Theory]
        [InlineData("jacobian(x y, {x, y})")]
        [InlineData("jacobian(x y, [x, 2])")]
        [InlineData("hessian(x y, [x, x])")]
        [InlineData("laplacian(x, pi)")]
        [InlineData("laplacian([x, y], [x, y])")]
        [InlineData("hessian([x, y], [x, y])")]
        [InlineData("jacobian([[x, y], [y, x]], [x, y])")]
        public void WhatIsNotAnOperatorsArgumentIsRefused(string written)
            => Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity());

        [Theory]
        [InlineData("laplacian(x)")]
        [InlineData("jacobian(x, [x], [y])")]
        [InlineData("gradient(x^2, [x], [y])")]
        public void EachTakesAFunctionAndItsVariables(string written)
            => Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity());

        [Theory]
        [InlineData("gradient(x^2 y + z, [x, y, z])", "[2 x y, x^2, 1]")]
        [InlineData("gradient(sin(x y), [x, y])", "[y cos(x y), x cos(x y)]")]
        [InlineData("gradient(x^3, x)", "[3 x^2]")]
        [InlineData("curl([-y, x, 0], [x, y, z])", "[0, 0, 2]")]
        [InlineData("curl([x y z, x^2, y sin(z)], [x, y, z])", "[sin(z), x y, 2 x - x z]")]
        public void TheGradientAndTheCurlAreColumns(string written, string expected)
            => AssertSameMatrix(expected, written);

        [Theory]
        [InlineData("divergence([x y, y z, z x], [x, y, z])", "x + y + z")]
        [InlineData("divergence([x^2, -2 x y], [x, y])", "0")]
        [InlineData("divergence([sin(x)], x)", "cos(x)")]
        public void TheDivergenceIsTheSumOfEachComponentsDerivativeInItsVariable(string written, string expected)
            => AssertSameValue(expected.ToEntity(), written.ToEntity());

        /// <summary>
        /// Against the Jacobian <c>J</c> of the same field: the gradient is the transpose of the Jacobian's one row, the
        /// divergence its trace, and the curl <c>[J_32 - J_23, J_13 - J_31, J_21 - J_12]</c>. And each is evaluated at
        /// points with negative and complex coordinates, where the identities <c>div curl F = 0</c> and
        /// <c>curl grad f = 0</c> hold.
        /// </summary>
        [Theory]
        [InlineData("[x^2 y z, e^(x - z), ln(y^2 + 1) x]", "x^3 sin(y) + z^2 y")]
        [InlineData("[y z^3, cos(x) y, x / (z^2 + 4)]", "e^(x y) z")]
        public void TheyAgreeWithTheJacobian(string field, string scalar)
        {
            var x = "[x, y, z]";
            var jacobian = Assert.IsType<Matrix>($"jacobian({field}, {x})".ToEntity());
            var curl = Assert.IsType<Matrix>($"curl({field}, {x})".ToEntity());
            AssertSameWhereDefined(jacobian[2, 1] - jacobian[1, 2], curl[0, 0]);
            AssertSameWhereDefined(jacobian[0, 2] - jacobian[2, 0], curl[1, 0]);
            AssertSameWhereDefined(jacobian[1, 0] - jacobian[0, 1], curl[2, 0]);
            AssertSameWhereDefined(jacobian.Trace, $"divergence({field}, {x})".ToEntity());
            AssertSameMatricesWhereDefined($"transpose(jacobian({scalar}, {x}))", $"gradient({scalar}, {x})");

            var divergenceOfCurl = $"divergence(curl({field}, {x}), {x})".ToEntity().InnerSimplified;
            var curlOfGradient = Assert.IsType<Matrix>($"curl(gradient({scalar}, {x}), {x})".ToEntity().InnerSimplified);
            foreach (var (px, py, pz) in new[] { ("-1.5", "2", "-0.25"), ("0.5 + i", "-3", "2 - 0.5i") })
            {
                Entity At(Entity e) => e.Substitute("x", px.ToEntity()).Substitute("y", py.ToEntity()).Substitute("z", pz.ToEntity());
                Assert.True(Number.Abs(At(divergenceOfCurl).EvalNumerical()).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-30")) < 0);
                for (var i = 0; i < 3; i++)
                    Assert.True(Number.Abs(At(curlOfGradient[i, 0]).EvalNumerical()).EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1e-30")) < 0);
            }
        }

        /// <summary>
        /// The gradient is taken of a scalar function, the divergence and the curl of a field written out with a component
        /// for each variable, and the curl in three variables only, as the vector product is.
        /// </summary>
        [Theory]
        [InlineData("gradient([x, y], [x, y])")]
        [InlineData("gradient(x y, {x, y})")]
        [InlineData("divergence(F, [x, y])")]
        [InlineData("divergence([x, y, z], [x, y])")]
        [InlineData("curl([x, y], [x, y])")]
        [InlineData("curl([x, y, z, w], [x, y, z, w])")]
        [InlineData("curl(F, [x, y, z])")]
        [InlineData("curl([x, y], [x, y, z])")]
        public void WhatIsNotAVectorOperatorsArgumentIsRefused(string written)
            => Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity());

        /// <summary>
        /// Without the variables there is no telling which derivatives are meant, so one argument is refused, saying to
        /// give them; <c>gradient(x^2)</c> and <c>divergence(x)</c> were refused by name before the operators existed,
        /// and <c>curl(x)</c> was the product <c>curl * x</c>.
        /// </summary>
        [Theory]
        [InlineData("gradient(x^2)")]
        [InlineData("divergence(x)")]
        [InlineData("curl([x, y, z])")]
        public void WithoutTheVariablesItIsRefused(string written)
            => Assert.Contains("give them as the second argument",
                Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity()).Message);

        /// <summary>
        /// No <c>grad</c> or <c>div</c>: each is still a variable, a product before a bracket, and a parse error with two
        /// arguments; <c>curl</c> alone is a variable too.
        /// </summary>
        [Theory]
        [InlineData("grad(f)", "grad * f")]
        [InlineData("div(F)", "div * F")]
        [InlineData("curl + 1", "curl + 1")]
        [InlineData("curl (x)", "curl * x")]
        public void GradAndDivAreNotNames(string written, string expected)
            => Assert.Equal(expected, written.ToEntity().Stringize());
    }
}
