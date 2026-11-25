//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using System.Numerics;
using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// <c>dot(a, b)</c> and <c>cross(a, b)</c> of a name that stands for a vector: nodes, computed once both operands are
    /// vectors, by simplification, evaluation or a compiled delegate. Substitution puts the vectors in and computes
    /// nothing, as in every other node. Every value is checked against one computed here from the components, at real,
    /// negative and non-real vectors.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/105">#105</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class DotAndCrossNodesTest
    {
        private static readonly Complex[][] Vectors =
        {
            new Complex[] { 1, 2, 3 },
            new Complex[] { -1.5, 0.25, -4 },
            new Complex[] { new(1, 2), -3, new(0, -1) },
            new Complex[] { new(2.5, 0.5), new(-1, 1), 2 },
        };

        private static Entity.Matrix Column(Complex[] v) => MathS.Vector(v.Select(c => (Entity)c).ToArray());

        private static Entity.Matrix Row(Complex[] v) => MathS.Matrix(1, v.Length, (_, c) => (Entity)v[c]);

        private static Complex Value(Entity e) => e.EvalNumerical().ToNumerics();

        private static void Near(Complex expected, Complex actual)
            => Assert.True(Complex.Abs(expected - actual) <= 1e-9 * Math.Max(1, Complex.Abs(expected)), $"expected {expected}, got {actual}");

        private static void Near(Complex[] expected, Complex[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
                Near(expected[i], actual[i]);
        }

        // The definitions, not the library's code: neither side of the scalar product is conjugated.
        private static Complex Dot(Complex[] a, Complex[] b) => a.Zip(b, (x, y) => x * y).Aggregate(Complex.Zero, (s, t) => s + t);

        private static Complex[] Cross(Complex[] u, Complex[] v)
            => new[] { u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0] };

        private static Complex[] Components(Entity value)
        {
            var column = Assert.IsType<Entity.Matrix>(value);
            Assert.Equal(1, column.ColumnCount);
            return Enumerable.Range(0, column.RowCount).Select(i => Value(column[i, 0])).ToArray();
        }

        [Theory]
        [InlineData("dot(a, b)", typeof(Entity.DotProductf))]
        [InlineData("cross(a, b)", typeof(Entity.CrossProductf))]
        [InlineData("dot([1, 2, 3], b)", typeof(Entity.DotProductf))]
        [InlineData("cross(a, [1, 2, 3])", typeof(Entity.CrossProductf))]
        [InlineData("dot(a + b, c)", typeof(Entity.DotProductf))]
        [InlineData("cross(cross(a, b), c)", typeof(Entity.CrossProductf))]
        [InlineData("dot(transpose(A) * a, b)", typeof(Entity.DotProductf))]
        // A name beside a written vector may stand for a matrix, so the operand is not read as a number.
        [InlineData("dot(x * [1, 2], [3, 4])", typeof(Entity.DotProductf))]
        public void ANameForAVectorParsesToTheNode(string input, Type node)
            => Assert.IsType(node, input.ToEntity());

        [Theory]
        [InlineData("dot(a, b)")]
        [InlineData("cross(a, b)")]
        [InlineData("cross(b, a) * 2 + dot(a, c) * d")]
        [InlineData("dot(cross(a, b), c)")]
        [InlineData("cross(a, [1, 2, 3])")]
        [InlineData("dot(x * [1, 2], [3, 4])")]
        [InlineData("M * cross(a, b)")]
        public void PrintingRoundTrips(string input)
        {
            var parsed = input.ToEntity();
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
        }

        // Vectors written out are computed as they are parsed, as they were without the nodes, and print as they did.
        [Theory]
        [InlineData("dot([1, 2, 3], [4, 5, 6])", "1 * 4 + 2 * 5 + 3 * 6", "32")]
        [InlineData("dot([x, y, z], [1, 0, 0])", "x * 1 + y * 0 + z * 0", "x")]
        [InlineData("dot([1, i], [i, 1])", "1 * i + i * 1", "2i")]
        [InlineData("dot([1, 2]T, [3, 4])", "1 * 3 + 2 * 4", "11")]
        [InlineData("cross([0, 0, w], [x, y, z])", "[0 * z - w * y, w * x - 0 * z, 0 * y - 0 * x]", "[-w * y, w * x, 0]")]
        [InlineData("cross([1, 2, 3], [4, 5, 6])", "[2 * 6 - 3 * 5, 3 * 4 - 1 * 6, 1 * 5 - 2 * 4]", "[-3, 6, -3]")]
        public void VectorsWrittenOutAreComputedWhenParsed(string input, string parsed, string simplified)
        {
            var entity = input.ToEntity();
            Assert.Equal(parsed, entity.Stringize());
            Assert.Equal(simplified, entity.InnerSimplified.Stringize());
            Assert.Equal(simplified, entity.Simplify().Stringize());
        }

        [Fact]
        public void TheScalarProductOfSubstitutedVectorsIsTheirScalarProduct()
        {
            Assert.Equal(32, "dot(a, b)".ToEntity().Substitute("a", "[1, 2, 3]").Substitute("b", "[4, 5, 6]").Simplify());
            foreach (var a in Vectors)
                foreach (var b in Vectors)
                {
                    var expected = Dot(a, b);
                    Near(expected, Value("dot(a, b)".ToEntity().Substitute("a", Column(a)).Substitute("b", Column(b)).Evaled));
                    // A row is a vector too, as it is written out.
                    Near(expected, Value("dot(a, b)".ToEntity().Substitute("a", Row(a)).Substitute("b", Column(b)).Simplify()));
                }
        }

        // Substitution computes nothing, in these nodes as in a determinant or a sine: the vectors put in are multiplied
        // out by Simplify, Evaled or a compiled delegate.
        [Fact]
        public void SubstitutionPutsTheVectorsInAndLeavesTheProductToSimplification()
        {
            var dot = "dot([0, 0, 1], v)".ToEntity().Substitute("v", "[x, y, z]");
            Assert.Equal(new Entity.DotProductf("[0, 0, 1]", "[x, y, z]"), dot);
            Assert.Equal("z".ToEntity(), dot.Simplify());
            Assert.Equal("z".ToEntity(), dot.InnerSimplified);
            var cross = "cross(W, r)".ToEntity().Substitute("W", "[0, 0, w]").Substitute("r", "[x, y, z]");
            Assert.Equal(new Entity.CrossProductf("[0, 0, w]", "[x, y, z]"), cross);
            Assert.Equal("[-w * y, w * x, 0]".ToEntity(), cross.Simplify());
            Assert.Equal(new Entity.Determinantf("[[1, 2], [3, 4]]"), "det(A)".ToEntity().Substitute("A", "[[1, 2], [3, 4]]"));
        }

        [Fact]
        public void TheVectorProductOfSubstitutedVectorsIsTheirVectorProduct()
        {
            var crossed = "cross(W, r)".ToEntity().Substitute("W", "[0, 0, w]").Substitute("r", "[x, y, z]").Simplify();
            Assert.Equal("[-w * y, w * x, 0]".ToEntity(), crossed);
            foreach (var u in Vectors)
                foreach (var v in Vectors)
                {
                    Near(Cross(u, v), Components("cross(a, b)".ToEntity().Substitute("a", Column(u)).Substitute("b", Column(v)).Evaled));
                    Near(Cross(u, v), Components("cross(a, b)".ToEntity().Substitute("a", Row(u)).Substitute("b", Row(v)).Simplify()));
                }
        }

        // The vector product is perpendicular to both its operands, the scalar product being taken without conjugation.
        [Fact]
        public void TheVectorProductIsPerpendicularToBothOperands()
        {
            foreach (var u in Vectors)
                foreach (var v in Vectors)
                {
                    Entity At(string e) => e.ToEntity().Substitute("a", Column(u)).Substitute("b", Column(v)).Evaled;
                    Near(0, Value(At("dot(cross(a, b), a)")));
                    Near(0, Value(At("dot(b, cross(a, b))")));
                }
        }

        // A number is not a vector, a matrix that is not one is refused, and a vector written out must be able to have a
        // value whatever the other operand turns out to be.
        [Theory]
        [InlineData("dot(5, a)")]
        [InlineData("dot(a, x = 1)")]
        [InlineData("cross(a, {1, 2})")]
        [InlineData("dot([[1, 2], [3, 4]], a)")]
        [InlineData("cross(a, [1, 2])")]
        [InlineData("cross([1, 2, 3, 4], a)")]
        [InlineData("dot([1, 2], [1, 2, 3])")]
        [InlineData("cross([1, 2], [3, 4])")]
        [InlineData("cross(a)")]
        public void WhatIsNoVectorOrHasNoProductIsRefused(string input)
            => Assert.Throws<InvalidArgumentParseException>(() => input.ToEntity());

        // No product: the node is left, never a number, as a determinant of a matrix that is not square is.
        [Theory]
        [InlineData("dot(a, b)", "[1, 2, 3]", "[4, 5]")]
        [InlineData("dot(a, b)", "[[1, 2], [3, 4]]", "[4, 5]")]
        [InlineData("dot(a, b)", "5", "[4, 5]")]
        [InlineData("cross(a, b)", "[1, 2]", "[4, 5]")]
        [InlineData("cross(a, b)", "[1, 2, 3]", "[4, 5, 6, 7]")]
        [InlineData("cross(a, b)", "[1, 2, 3]", "7")]
        public void WhatHasNoProductIsLeftUnevaluated(string input, string a, string b)
        {
            var node = input.ToEntity().GetType();
            var substituted = input.ToEntity().Substitute("a", a).Substitute("b", b);
            Assert.IsType(node, substituted.Evaled);
            Assert.IsType(node, substituted.InnerSimplified);
            Assert.IsType(node, substituted.Simplify());
        }

        // cross(b, a) is -cross(a, b): nothing may put the operands in the other order, or read the two as one.
        [Fact]
        public void TheOperandsOfTheVectorProductKeepTheirOrder()
        {
            var ab = "cross(a, b)".ToEntity();
            var ba = "cross(b, a)".ToEntity();
            Assert.NotEqual(ab, ba);
            Assert.Equal(ab, ab.Simplify());
            Assert.Equal(ab, ab.InnerSimplified);
            Assert.Equal(ba, ba.Simplify());
            Assert.Equal(ba, ba.InnerSimplified);
            Assert.NotEqual(ab.Simplify(), ba.Simplify());

            var sum = "cross(a, b) + cross(b, a)".ToEntity();
            foreach (var form in new[] { sum.Simplify(), sum.InnerSimplified, sum.Evaled })
            {
                Assert.NotEqual("2 * cross(a, b)".ToEntity(), form);
                Assert.NotEqual("2 * cross(b, a)".ToEntity(), form);
                Assert.Contains(ab, form.Nodes);
                Assert.Contains(ba, form.Nodes);
                // And the sum is the zero vector once the operands are vectors.
                Near(new Complex[] { 0, 0, 0 }, Components(form.Substitute("a", Column(Vectors[2])).Substitute("b", Column(Vectors[3])).Evaled));
            }
            Assert.Equal(Entity.Number.Integer.Zero, "cross(a, b) - cross(a, b)".ToEntity().Simplify());
        }

        // A vector product is a vector, and does not commute with a matrix: M cross(a, b) has a value where
        // cross(a, b) M has none, and simplification must keep the product in its order.
        [Theory]
        [InlineData("M * cross(a, b)")]
        [InlineData("2 * M * cross(a, b)")]
        [InlineData("M * cross(a, b) * k")]
        [InlineData("M * cross(a, b) - cross(b, a)")]
        public void AProductWithAVectorProductKeepsItsOrder(string input)
        {
            var expr = input.ToEntity();
            var m = MathS.Matrix(new Entity[,] { { 1, -2, 0.5 }, { 3, "1 + i".ToEntity(), -1 }, { 0, 2, -4 } });
            Entity At(Entity e) => e.Substitute("M", m).Substitute("a", Column(Vectors[1])).Substitute("b", Column(Vectors[2])).Substitute("k", 3).Evaled;
            var expected = Components(At(expr));
            foreach (var form in new[] { expr.Simplify(), expr.InnerSimplified })
                Near(expected, Components(At(form)));
        }

        // Linear in each operand: the product rule, the operands kept in their order.
        [Fact]
        public void TheProductsAreDifferentiatedByTheProductRule()
        {
            var (a, b) = (Vectors[1], Vectors[2]);
            foreach (var x in new Complex[] { 2, -1.5 })
            {
                Entity At(Entity e) => e.Substitute("a", Column(a)).Substitute("b", Column(b)).Substitute("x", x).Evaled;
                // d/dx dot(x^2 a, x b) = 3 x^2 dot(a, b)
                Near(3 * x * x * Dot(a, b), Value(At("dot(x^2 * a, x * b)".ToEntity().Differentiate("x"))));
                // d/dx cross(x^2 a, x b) = 3 x^2 cross(a, b)
                Near(Cross(a, b).Select(c => 3 * x * x * c).ToArray(), Components(At("cross(x^2 * a, x * b)".ToEntity().Differentiate("x"))));
            }
            Assert.Equal(Entity.Number.Integer.Zero, "dot(a, b)".ToEntity().Differentiate("x"));
            Assert.Equal("cross(a, b)".ToEntity(), "cross(x * a, b)".ToEntity().Differentiate("x").Simplify());
        }

        [Fact]
        public void TheNodesPrintToLatexAndSympy()
        {
            Assert.Equal(@"a \cdot b", "dot(a, b)".ToEntity().Latexize());
            Assert.Equal(@"a \times b", "cross(a, b)".ToEntity().Latexize());
            Assert.Equal(@"\left(a+b\right) \cdot c", "dot(a + b, c)".ToEntity().Latexize());
            var sympy = MathS.ToSympyCode("dot(a, b) + cross(a, b)");
            Assert.Contains("(a).dot(b)", sympy);
            Assert.Contains("(a).cross(b)", sympy);
        }

        [Fact]
        public void TheConstructorsBuildTheNodes()
        {
            Assert.Equal("dot(a, b)".ToEntity(), MathS.Matrices.DotProduct("a", "b"));
            Assert.Equal("cross(a, b)".ToEntity(), MathS.Matrices.CrossProduct("a", "b"));
            // Over vectors written out the node is computed when simplified, to the value parsing gives.
            Assert.Equal("dot([1, 2], [3, 4])".ToEntity().Simplify(), MathS.Matrices.DotProduct("[1, 2]", "[3, 4]").Simplify());
            Assert.Equal("cross([1, 2, 3], [4, 5, 6])".ToEntity().Simplify(), MathS.Matrices.CrossProduct("[1, 2, 3]", "[4, 5, 6]").Simplify());
        }

        // Compiled, each compared with evaluation at the same vectors.
        [Fact]
        public void TheProductsCompile()
        {
            var dotOverVectors = "dot(a, b)".ToEntity().Compile<Complex[], Complex[], Complex>("a", "b");
            var crossOverVectors = "cross(a, b)".ToEntity().Compile<Complex[], Complex[], Complex[]>("a", "b");
            var dotOverColumns = "dot(a, b)".ToEntity().Compile<Complex[,], Complex[,], Complex>("a", "b");
            foreach (var u in Vectors)
                foreach (var v in Vectors)
                {
                    Entity At(string e) => e.ToEntity().Substitute("a", Column(u)).Substitute("b", Column(v)).Evaled;
                    Near(Value(At("dot(a, b)")), dotOverVectors(u, v));
                    Near(Components(At("cross(a, b)")), crossOverVectors(u, v));
                    var column = new Complex[u.Length, 1];
                    var row = new Complex[1, v.Length];
                    for (var i = 0; i < u.Length; i++)
                        (column[i, 0], row[0, i]) = (u[i], v[i]);
                    Near(Dot(u, v), dotOverColumns(column, row));
                }
            var overReals = "dot(a, b)".ToEntity().Compile<double[], double[], double>("a", "b");
            Assert.Equal(32, overReals(new double[] { 1, 2, 3 }, new double[] { 4, 5, 6 }), 12);
            var crossOverReals = "cross(a, b)".ToEntity().Compile<double[,], double[,], double[,]>("a", "b");
            Assert.Equal(new double[,] { { -6 }, { 2 }, { 0 } }, crossOverReals(new double[,] { { 0 }, { 0 }, { 2 } }, new double[,] { { 1 }, { 3 }, { 5 } }));
            // Of a matrix argument and a vector written out, and of a name beside one, read as a number by the delegate.
            Assert.Equal(10, "dot(M * a, a)".ToEntity().Compile<double[,], double[], double>("M", "a")(new double[,] { { 1, 2 }, { 3, 4 } }, new double[] { 1, 1 }), 12);
            Assert.Equal(22, "dot(x * [1, 2], [3, 4])".ToEntity().Compile<double, double>("x")(2), 12);
            // And at a number that is not whole, which the written vector's whole numbers truncated to 0.
            Assert.Equal(5.5, "dot(x * [1, 2], [3, 4])".ToEntity().Compile<double, double>("x")(0.5), 12);
            Assert.Equal(new Complex(5.5, 0), "dot(x * [1, 2], [3, 4])".ToEntity().Compile("x").Call(0.5));
        }

        [Fact]
        public void WhatCannotBeComputedIsRefusedWhenCompilingOrCalled()
        {
            var dot = "dot(a, b)".ToEntity().Compile<double[], double[], double>("a", "b");
            Assert.Throws<InvalidMatrixOperationException>(() => dot(new double[] { 1, 2, 3 }, new double[] { 4, 5 }));
            var cross = "cross(a, b)".ToEntity().Compile<double[], double[], double[]>("a", "b");
            Assert.Throws<InvalidMatrixOperationException>(() => cross(new double[] { 1, 2 }, new double[] { 4, 5 }));
            var dotOfMatrices = "dot(a, b)".ToEntity().Compile<double[,], double[,], double>("a", "b");
            Assert.Throws<InvalidMatrixOperationException>(() => dotOfMatrices(new double[,] { { 1, 2 }, { 3, 4 } }, new double[,] { { 1, 2 }, { 3, 4 } }));
            // Shapes written into the expression are checked when compiling.
            Assert.Throws<InvalidMatrixOperationException>(() => MathS.Matrices.DotProduct("[x, 2]", "[1, 2, 3]").Compile<double, double>("x"));
            Assert.Throws<InvalidMatrixOperationException>(() => MathS.Matrices.CrossProduct("[x, 2]", "a").Compile<double, double[], double[]>("x", "a"));
            // A number is not a vector operand.
            Assert.Throws<UncompilableNodeException>(() => "dot(a, b)".ToEntity().Compile<double, double[], double>("a", "b"));
            Assert.Throws<UncompilableNodeException>(() => "cross(a, b)".ToEntity().Compile<double, double, double>("a", "b"));
        }
    }
}
