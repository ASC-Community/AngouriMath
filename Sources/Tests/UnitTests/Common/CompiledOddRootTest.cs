//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A negative real to a rational power with an odd denominator below ten is the real root to the
    /// numerator, in evaluation: <c>(-8)^(1/3)</c> is <c>-2</c>. Both compilers took the principal
    /// power, <c>1 + 1.73i</c>, and <c>NaN</c> over <c>double</c>; and since Newton's method runs on
    /// the compiled function, the roots that need the real root were not found:
    /// <c>(3 - 4x)^(1/3) + sqrt(4 + 4x) = 3</c> was answered without <c>8.379</c>. Found by a
    /// cross-check of equations in roots against a Newton search; no issue was filed.
    /// </summary>
    [Trait("Area", "Compilation")]
    public sealed class CompiledOddRootTest
    {
        [Theory]
        [InlineData("x^(1/3)", -8)]
        [InlineData("x^(2/3)", -8)]
        [InlineData("x^(-1/3)", -8)]
        [InlineData("x^(4/3)", -8)]
        [InlineData("x^(3/5)", -32)]
        [InlineData("(1 + x)^(1/3) + 2", -9)]
        public void TheCompiledFormsTakeTheRealRoot(string expression, double at)
        {
            var entity = expression.ToEntity();
            var value = entity.Substitute("x", (Entity)(int)at).EvalNumerical();
            Assert.True(value.ImaginaryPart.IsZero, $"{expression} at {at} evaluates to {value}");
            var expected = value.RealPart.EDecimal.ToDouble();
            Assert.Equal(expected, entity.Compile("x").Call(new NumericsComplex(at, 0)).Real, 12);
            Assert.Equal(0, entity.Compile("x").Call(new NumericsComplex(at, 0)).Imaginary, 12);
            Assert.Equal(expected, entity.Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(at, 0)).Real, 12);
            Assert.Equal(0, entity.Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(at, 0)).Imaginary, 12);
            Assert.Equal(expected, entity.Compile<double, double>("x")(at), 12);
        }

        /// <summary>
        /// The rest is the principal power, as in evaluation: a base off the real line, a positive one,
        /// an even denominator and one of ten or more.
        /// </summary>
        [Theory]
        [InlineData("x^(1/3)", -8, 1)]
        [InlineData("x^(1/3)", 8, 0)]
        [InlineData("x^(1/2)", -4, 0)]
        [InlineData("x^(1/11)", -1, 0)]
        public void TheRestIsThePrincipalPower(string expression, double re, double im)
        {
            var entity = expression.ToEntity();
            var value = entity.Substitute("x", Number.Complex.Create(re, im)).EvalNumerical().ToNumerics();
            var compiled = entity.Compile("x").Call(new NumericsComplex(re, im));
            Assert.True((compiled - value).Magnitude < 1e-12, $"{expression} at {re} + {im}i: evaluated {value}, compiled {compiled}");
            var linq = entity.Compile<NumericsComplex, NumericsComplex>("x")(new NumericsComplex(re, im));
            Assert.True((linq - value).Magnitude < 1e-12, $"{expression} at {re} + {im}i: evaluated {value}, LINQ {linq}");
        }

        /// <summary>The root Newton's method could not see.</summary>
        [Fact]
        public void TheSearchFindsTheRootThatNeedsTheRealRoot()
        {
            var answer = Assert.IsType<FiniteSet>("(3 - 4x)^(1/3) + sqrt(4 + 4x) = 3".ToEntity().Solve("x"));
            Assert.Contains(answer, root => System.Math.Abs(root.EvalNumerical().RealPart.EDecimal.ToDouble() - 8.3785553513) < 1e-8);
        }
    }
}
