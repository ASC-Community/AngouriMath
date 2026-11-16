//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The p-norm of a vector, <c>norm(v, p) = (|v_1|^p + ... + |v_n|^p)^(1/p)</c> for <c>p &gt;= 1</c> and the largest
    /// modulus at <c>p = +oo</c>. It was a parse error; the 2-norm is <c>abs(v)</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class NormTest
    {
        /// <summary>Worked by hand: the moduli of the entries, real and complex, summed at the power and rooted.</summary>
        [Theory]
        [InlineData("norm([3, 4], 1)", "7")]
        [InlineData("norm([3, -4], 1)", "7")]
        [InlineData("norm([3, 4], +oo)", "4")]
        [InlineData("norm([-7, 4, 2], +oo)", "7")]
        [InlineData("norm([3, 4], 3)", "91^(1/3)")]
        [InlineData("norm([3, 4]T, 3)", "91^(1/3)")]
        [InlineData("norm([1, -2, 2], 2)", "3")]
        [InlineData("norm([1 + i, 2 - i, 3], 4)", "110^(1/4)")]
        [InlineData("norm([3 + 4i, 0], 1)", "5")]
        [InlineData("norm([3 + 4i, 12i], +oo)", "12")]
        [InlineData("norm([1, 1, 1, 1], 1/2 + 1)", "4^(2/3)")]
        [InlineData("norm([5], 7)", "5")]
        [InlineData("norm(-5, 3)", "5")]
        public void ItIsTheNormWorkedByHand(string written, string expected)
            => Assert.Equal(Number.Integer.Zero, (written.ToEntity().Simplify() - expected.ToEntity()).Simplify());

        /// <summary>At a p that is not a whole number, to the working precision, against the sum written out.</summary>
        [Theory]
        [InlineData("[1.5, -2.25, 0.75 + 2i]", "2.5")]
        [InlineData("[-3, 1/7, pi]", "1.1")]
        [InlineData("[0.001, 1000]", "7.75")]
        public void ItIsTheSumOfTheModuliAtThePowerRooted(string vector, string p)
        {
            var entries = (Matrix)vector.ToEntity();
            var pv = p.ToEntity();
            Entity sum = 0;
            foreach (var entry in entries)
                sum += MathS.Pow(MathS.Abs(entry), pv);
            var expected = (Number.Complex)MathS.Pow(sum, 1 / pv).EvalNumerical();
            var value = (Number.Complex)MathS.Norm(entries, pv).EvalNumerical();
            var error = ((Number.Complex)(value - expected).EvalNumerical()).Abs().EDecimal;
            Assert.True(error.CompareTo(expected.Abs().EDecimal.Multiply(EDecimal.FromString("1e-95"))) <= 0,
                $"norm({vector}, {p}) = {value.Stringize()}, expected {expected.Stringize()}");
        }

        /// <summary>The 2-norm is the absolute value of the vector, the same expression, and so the same value.</summary>
        [Theory]
        [InlineData("[3, 4]")]
        [InlineData("[1 + i, 2]")]
        [InlineData("[x, y]")]
        [InlineData("[x, 2 y, sqrt(2)]")]
        [InlineData("[a + b i, c]T")]
        [InlineData("[1.5, -2.25, 0.75 + 2i]")]
        public void TheTwoNormIsTheAbsoluteValue(string vector)
        {
            Assert.Equal($"abs({vector})".ToEntity().Simplify(), $"norm({vector}, 2)".ToEntity().Simplify());
            Assert.Equal($"abs({vector})".ToEntity().InnerSimplified, $"norm({vector}, 2)".ToEntity().InnerSimplified);
        }

        /// <summary>
        /// The norms of <c>[x, y]</c> written out: the moduli at the power, and the larger modulus at <c>+oo</c>; and at
        /// <c>x = 3 - i</c>, <c>y = -2</c> the values worked by hand.
        /// </summary>
        [Theory]
        [InlineData("1", "sqrt(10) + 2")]
        [InlineData("3", "(10 sqrt(10) + 8)^(1/3)")]
        [InlineData("+oo", "sqrt(10)")]
        public void ASymbolicVectorIsWrittenOut(string p, string atThePoint)
        {
            var norm = $"norm([x, y], {p})".ToEntity().Simplify();
            Assert.DoesNotContain(norm.Nodes, node => node is Normf);
            var value = norm.Substitute("x", "3 - i".ToEntity()).Substitute("y", -2);
            Assert.Equal(Number.Integer.Zero, (value - atThePoint.ToEntity()).Simplify());
        }

        /// <summary>
        /// Below <c>p = 1</c> it is not a norm, so a <c>p</c> that is a number there is refused, as is a <c>p</c> off the
        /// real line; and a matrix written out that is not a vector has other norms, not these.
        /// </summary>
        [Theory]
        [InlineData("norm(v, 1/2)", "p >= 1")]
        [InlineData("norm(v, 0)", "p >= 1")]
        [InlineData("norm([1, 2], -1)", "p >= 1")]
        [InlineData("norm(v, -oo)", "p >= 1")]
        [InlineData("norm(v, i)", "p >= 1")]
        [InlineData("norm([[1, 2], [3, 4]], 2)", "matrix")]
        [InlineData("norm(2 [[1, 2], [3, 4]], 1)", "matrix")]
        public void WhatIsNotAPNormOfAVectorIsRefused(string written, string message)
            => Assert.Contains(message, Assert.Throws<InvalidArgumentParseException>(() => written.ToEntity()).Message);

        /// <summary>
        /// A symbolic <c>p</c> keeps the node, its condition <c>p &gt;= 1</c>, so that a <c>p</c> below 1 given later
        /// has no value, and one above it the norm.
        /// </summary>
        [Fact]
        public void ASymbolicOrderKeepsTheNodeAndItsCondition()
        {
            var norm = "norm([3, 4], p)".ToEntity();
            Assert.IsType<Normf>(norm.Simplify());
            Assert.Equal(Boolean.False, norm.DomainCondition.Substitute("p", MathS.FromString("1/2")).Simplify());
            Assert.Equal(Boolean.True, norm.DomainCondition.Substitute("p", 3).Simplify());
            Assert.Equal(MathS.NaN, norm.Substitute("p", MathS.FromString("1/2")).Simplify());
            Assert.Equal(Number.Integer.Create(7), norm.Substitute("p", 1).Simplify());
            Assert.IsType<Normf>("norm(v, 3)".ToEntity().Simplify());
        }

        /// <summary>
        /// The name alone is a variable, and with a space before the bracket a product, as the other names
        /// refused by name are.
        /// </summary>
        [Theory]
        [InlineData("norm", "norm")]
        [InlineData("norm (x)", "norm * x")]
        [InlineData("norm * [3, 4]", "norm * [3, 4]")]
        public void TheNameWithoutACallIsWhatItWas(string written, string expected)
            => Assert.Equal(expected, written.ToEntity().Stringize());

        /// <summary>
        /// With one argument it is refused. It was the product <c>norm * v</c>, so <c>norm([3, 4])</c> simplified
        /// to <c>[3 * norm, 4 * norm]</c> with no error, where the length is 5.
        /// </summary>
        [Theory]
        [InlineData("norm([3, 4])")]
        [InlineData("norm(x)^2")]
        [InlineData("norm({1, 2})")]
        [InlineData("norm([0; 1))")]
        [InlineData("1 + norm(v)")]
        [InlineData("norm(norm(v, 2))")]
        public void OneArgumentIsRefused(string written)
        {
            var refused = Assert.Throws<FunctionArgumentCountException>(() => written.ToEntity());
            Assert.Contains("norm(v, 2)", refused.Message);
        }

        // A second argument inside a bracket of the first is not the call's own.
        [Fact]
        public void ACommaInsideTheFirstArgumentIsNotASecondArgument()
            => Assert.Throws<FunctionArgumentCountException>(() => "norm([3, 4])".ToEntity());

        [Fact]
        public void ItTakesTwoArguments()
        {
            Assert.Throws<FunctionArgumentCountException>(() => "norm(v, p, q)".ToEntity());
            Assert.ThrowsAny<ParseException>(() => "norm (v, p)".ToEntity());
        }

        [Theory]
        [InlineData("norm(v, p)", @"\left\| v \right\|_{p}", "(v).norm(p)")]
        [InlineData("norm(v, +oo)", @"\left\| v \right\|_{\infty }", "(v).norm(sympy.oo)")]
        public void ItIsWrittenAsANorm(string written, string latex, string sympy)
        {
            var parsed = written.ToEntity();
            Assert.Equal(written, parsed.Stringize());
            Assert.Equal(parsed, parsed.Stringize().ToEntity());
            Assert.Equal(latex, parsed.Latexize());
            Assert.Equal(sympy, parsed.ToSymPy());
        }

        /// <summary>
        /// A norm of a vector written out is the expression it simplifies to, which compiles; the node left where the
        /// vector is not known has no compiled form, and is refused, saying to simplify first.
        /// </summary>
        [Fact]
        public void ItCompilesOnceWrittenOut()
        {
            var compiled = "norm([x, 4], 3)".ToEntity().Simplify().Compile<double, double>("x");
            Assert.Equal(System.Math.Cbrt(27 + 64), compiled(-3), 12);
            Assert.Contains("p-norm", Assert.Throws<UncompilableNodeException>(() => "norm(x, 3)".ToEntity().Compile<double, double>("x")).Message);
        }
    }
}
