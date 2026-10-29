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
    /// <see cref="AngouriMath.Core.FastExpression"/> compiles a statement to its truth value, 1, 0,
    /// or NaN where it has none, and a provided and a piecewise to the case whose predicate holds.
    /// Each was refused. <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class FastExpressionStatementTest
    {
        public static readonly TheoryData<string> Expressions = new()
        {
            "x > 1", "x < 1", "x >= 1", "x <= 1", "x = 1",
            "not (x > 1)", "x > 1 or true", "x > 1 and false", "x > 1 or x < 2", "x > 0 and x < 2",
            "x > 1 xor true", "x > 1 implies false", "false implies x > 1", "(x > 1) = (x < 2)",
            "x provided x > 1", "x^2 provided x > 0 and x < 3",
            "piecewise(1 provided x > 0, -1 provided x < 0)", "piecewise(x provided x > 1, 2 x provided x > 0, 0)",
            "iverson(x > 1) * x", "sin(x) + iverson(x >= 0)",
        };

        private static readonly (Entity At, Complex Value)[] points =
            { (2, 2), (1, 1), ("1/2", 0.5), (-3, -3), ("i", Complex.ImaginaryOne), ("1 + i", new(1, 1)), (MathS.NaN, double.NaN) };

        /// <summary>
        /// What evaluation gives: True is 1, False is 0, and a statement with no truth value, NaN.
        /// </summary>
        private static Complex Interpreted(string expression, Entity at)
            => expression.ToEntity().Substitute("x", at).Evaled switch
            {
                Entity.Boolean value => (bool)value ? 1 : 0,
                Entity.Number.Complex number => number.ToNumerics(),
                var other => throw new Xunit.Sdk.XunitException($"{expression} at {at} is {other}")
            };

        [Theory]
        [MemberData(nameof(Expressions))]
        public void ItIsWhatEvaluationSays(string expression)
        {
            var compiled = expression.ToEntity().Compile("x");
            foreach (var (at, value) in points)
            {
                var expected = Interpreted(expression, at);
                var actual = compiled.Call(value);
                Assert.True(double.IsNaN(expected.Real) ? double.IsNaN(actual.Real) : Complex.Abs(expected - actual) < 1e-12,
                    $"{expression} at {at}: {expected} interpreted, {actual} compiled");
            }
        }

        /// <summary>The cases of a piecewise are tried in order: the first whose predicate holds is the value.</summary>
        [Fact]
        public void TheFirstCaseThatHoldsIsTheValue()
        {
            var compiled = "piecewise(1 provided x > 0, 2 provided x > -1, 3)".ToEntity().Compile("x");
            Assert.Equal(new Complex(1, 0), compiled.Call(5));
            Assert.Equal(new Complex(2, 0), compiled.Call(-0.5));
            Assert.Equal(new Complex(3, 0), compiled.Call(-5));
            Assert.Equal(new Complex(3, 0), compiled.Call(Complex.ImaginaryOne));
        }
    }
}
