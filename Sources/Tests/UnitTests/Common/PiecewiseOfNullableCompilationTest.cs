//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Numerics;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// A piecewise whose case is an Iverson bracket or a provided integer compiles in the LINQ compiler, where it threw
    /// <c>KeyNotFoundException</c>: its case types, an <c>int?</c> and a <c>long?</c>, were not in the widening table.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2237">#2237</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class PiecewiseOfNullableCompilationTest
    {
        [Theory]
        [InlineData("sin(piecewise(iverson(x > 0) provided x > 0, sin(2)))", 2)]
        [InlineData("sin(piecewise(iverson(x > 0) provided x > 0, sin(2)))", -2)]
        [InlineData("exp(piecewise(2^x provided x > 0, iverson(x > 0)))", -1)]
        [InlineData("abs(piecewise((-1)^2 provided x > 0, iverson(-1 > 0)))", 1)]
        [InlineData("piecewise(iverson(sinh(3) > 0) provided x > 0, tan(-1))", 1)]
        [InlineData("piecewise(piecewise(-1 provided x > 0, 3) provided x > 0, x) + x", 1)]
        [InlineData("piecewise(piecewise(-1 provided x > 0, 3) provided x > 0, x) + x", -1)]
        public void ItIsWhatEvaluationGives(string expression, double at)
        {
            var expected = ((Entity.Number.Complex)expression.ToEntity().Substitute("x", at).Evaled).ToNumerics();
            var linq = expression.ToEntity().Compile<Complex, Complex>("x")(at);
            var real = expression.ToEntity().Compile<double, double>("x")(at);
            Assert.True(Complex.Abs(expected - linq) < 1e-12, $"{expression} at {at}: {expected} evaluated, {linq} by LINQ");
            Assert.True(System.Math.Abs(expected.Real - real) < 1e-12, $"{expression} at {at}: {expected} evaluated, {real} over double");
        }
    }
}
