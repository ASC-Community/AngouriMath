//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// The imaginary part of a value that does not exist has none, as its real part has none, where it was 0: <c>NaN</c>
    /// is a <see cref="Entity.Number.Real"/>, whose imaginary part is 0.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2253">#2253</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ImaginaryPartOfNaNTest
    {
        [Theory]
        [InlineData("Im(1/0)")]
        [InlineData("Im(arccos(pi) provided pi < 3)")]
        [InlineData("Im(1/0) + 1")]
        [InlineData("Re(1/0)")]
        public void ItHasNoValue(string expression)
        {
            Assert.True(expression.ToEntity().Evaled.IsNaN, $"{expression} evaluates to {expression.ToEntity().Evaled}");
            Assert.True(expression.ToEntity().Simplify().IsNaN, $"{expression} simplifies to {expression.ToEntity().Simplify()}");
        }

        [Fact]
        public void SubstitutedItHasNoValue()
            => Assert.True("Im(x)".ToEntity().Substitute("x", "1/0".ToEntity()).Evaled.IsNaN);

        [Theory]
        [InlineData("Im(2 + 3i)", "3")]
        [InlineData("Im(5)", "0")]
        [InlineData("Im(-oo)", "0")]
        public void ANumberHasItsImaginaryPart(string expression, string part)
            => Assert.Equal(part.ToEntity().Evaled, expression.ToEntity().Evaled);
    }
}
