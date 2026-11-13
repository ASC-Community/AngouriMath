//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// The norm of a vector is <c>sqrt(sum |v_k|^2)</c>. Squaring the components is that only on
    /// the real line: <c>|[1 + i, 2]|</c> was <c>NaN</c>, <c>|[1 + i, 1 - i]|</c> was 0, and
    /// <c>|[x, y]|</c> simplified to <c>sqrt(x^2 + y^2)</c>, which is 0 at <c>x = i, y = 0</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2423
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class TheNormOfAComplexVectorTest
    {
        private static double Value(Entity expression)
        {
            var value = expression.EvalNumerical();
            Assert.True(value.ImaginaryPart.IsZero, $"{expression} gave {value}, which is not real");
            return value.RealPart.EDecimal.ToDouble();
        }

        // Norms worked out by hand: |a + bi|^2 is a^2 + b^2, summed and rooted.
        [Theory]
        [InlineData("abs([3, 4])", 5.0)]
        [InlineData("abs([-3, 4])", 5.0)]
        [InlineData("abs([-2, -3, -6])", 7.0)]
        [InlineData("abs([1 + i, 2])", 2.449489742783178)]  // sqrt(2 + 4)
        [InlineData("abs([3 * i, 4])", 5.0)]                // sqrt(9 + 16)
        [InlineData("abs([1 + i, 1 - i])", 2.0)]            // sqrt(2 + 2)
        [InlineData("abs([-1 - 2 * i, 2 * i])", 3.0)]       // sqrt(5 + 4)
        [InlineData("abs([-i])", 1.0)]
        // A row vector is a vector too.
        [InlineData("abs([3, 4]T)", 5.0)]
        [InlineData("abs([1 + i, 2]T)", 2.449489742783178)]
        public void TheNormIsTheRootOfTheSumOfTheModuliSquared(string vector, double norm)
        {
            Assert.Equal(norm, Value(vector.ToEntity()), 12);
            Assert.Equal(norm, Value(vector.ToEntity().Simplify()), 12);
        }

        [Fact]
        public void AnExactNormStaysExact() =>
            Assert.Equal(MathS.Sqrt(6), "abs([1 + i, 2])".ToEntity().Simplify());

        // Every point here has a component off the real line except the last, which is the
        // real case that was right before and has to stay right.
        public static TheoryData<NumericsComplex, NumericsComplex> Points => new()
        {
            { new(0, 1), new(0, 0) },
            { new(1, 1), new(2, 0) },
            { new(1, 2), new(-3, -1) },
            { new(-2, -5), new(0, 3) },
            { new(-3, 0), new(4, 0) },
        };

        private static Entity At(Entity expression, NumericsComplex x, NumericsComplex y)
            => expression
                .Substitute("x", Entity.Number.Complex.Create(x.Real, x.Imaginary))
                .Substitute("y", Entity.Number.Complex.Create(y.Real, y.Imaginary));

        private static double Norm(NumericsComplex x, NumericsComplex y)
            => Math.Sqrt(x.Magnitude * x.Magnitude + y.Magnitude * y.Magnitude);

        [Theory]
        [MemberData(nameof(Points))]
        public void TheSimplifiedNormKeepsItsValueAtComplexPoints(NumericsComplex x, NumericsComplex y)
        {
            Entity norm = "abs([x, y])";
            Assert.Equal(Norm(x, y), Value(At(norm, x, y)), 10);
            Assert.Equal(Norm(x, y), Value(At(norm.Simplify(), x, y)), 10);
            Assert.Equal(Norm(x, y), Value(At(norm.Evaled, x, y)), 10);
        }

        [Theory]
        [MemberData(nameof(Points))]
        public void TheNormCompiledOverComplexIsTheNorm(NumericsComplex x, NumericsComplex y)
        {
            var compiled = "abs([x, y])".Compile<NumericsComplex, NumericsComplex, NumericsComplex>("x", "y")(x, y);
            Assert.Equal(Norm(x, y), compiled.Real, 10);
            Assert.Equal(0, compiled.Imaginary, 10);
        }

        [Theory]
        [InlineData(-3.0, 4.0)]
        [InlineData(1.5, -2.0)]
        [InlineData(0.0, -7.0)]
        public void TheNormCompiledOverDoubleIsTheNorm(double x, double y) =>
            Assert.Equal(Math.Sqrt(x * x + y * y), "abs([x, y])".Compile<double, double, double>("x", "y")(x, y), 12);

        // Declared real, a component's square is its modulus squared, so the norm is written
        // without a modulus, and is the norm at real points.
        [Theory]
        [InlineData(-3.0, 4.0)]
        [InlineData(1.5, -2.0)]
        public void ANormOfRealComponentsIsTheNormAtRealPoints(double x, double y)
        {
            Entity realX = "domain(x, RR)", realY = "domain(y, RR)";
            var norm = MathS.Abs(MathS.Vector(realX, realY)).Simplify();
            Assert.DoesNotContain(norm.Nodes, node => node is Entity.Absf);
            Assert.Equal(Math.Sqrt(x * x + y * y), Value(norm.Substitute(realX, x).Substitute(realY, y)), 12);
        }
    }
}
