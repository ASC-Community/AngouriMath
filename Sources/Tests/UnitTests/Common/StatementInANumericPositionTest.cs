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
    /// A statement where a number is expected has no value, as evaluation leaves <c>sin(True)</c> as written, and both
    /// compilers refuse it. FastExpression answered with the truth value taken as a number, and the LINQ compiler threw
    /// <c>KeyNotFoundException</c> or <c>AngouriBugException</c>. <c>iverson</c> takes a statement as a number.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2227">#2227</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class StatementInANumericPositionTest
    {
        [Theory]
        [InlineData("sin(x > 1)")]
        [InlineData("(x > 1)^2")]
        [InlineData("max(x > 1, 0)")]
        [InlineData("(x > 1) + 2")]
        [InlineData("2 * (x in [0; 1])")]
        [InlineData("(x > 1) = 2")]
        [InlineData("(x > 1) < 2")]
        [InlineData("sin(x > 1 provided x > 0)")]
        [InlineData("x^(3 divides x)")]
        [InlineData("piecewise(x > 1 provided x > 0, x) + 1")]
        public void IsRefused(string expression)
        {
            Assert.Throws<UncompilableNodeException>(() => expression.ToEntity().Compile("x"));
            Assert.Throws<UncompilableNodeException>(() => expression.ToEntity().Compile<double, double>("x"));
            Assert.Throws<UncompilableNodeException>(() => expression.ToEntity().Compile<Complex, Complex>("x"));
        }

        [Theory]
        [InlineData("iverson(x > 1) + 2", 2, 3)]
        [InlineData("iverson(x in ZZ) * x", 2, 2)]
        [InlineData("x > 1 and x < 3", 2, 1)]
        [InlineData("(x > 1) = (x < 3)", 4, 0)]
        [InlineData("not x > 1", 2, 0)]
        [InlineData("x provided x > 0", 2, 2)]
        [InlineData("piecewise(x provided x > 0, -x)", -2, 2)]
        [InlineData("x in { t : t > 0 }", 2, 1)]
        public void AStatementWhereOneIsTakenCompiles(string expression, double x, double expected)
        {
            Assert.Equal(new Complex(expected, 0), expression.ToEntity().Compile("x").Call(x));
            Assert.Equal(new Complex(expected, 0), expression.ToEntity().Compile<Complex, Complex>("x")(x));
        }
    }
}
