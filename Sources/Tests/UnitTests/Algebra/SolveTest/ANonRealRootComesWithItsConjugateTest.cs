//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// An equation in real numbers has its non-real roots in conjugate pairs, and Newton's method
    /// reached 7.6545 + 11.9537i of 2^x = x^2 and not 7.6545 - 11.9537i.
    /// https://github.com/asc-community/AngouriMath/issues/2546
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class ANonRealRootComesWithItsConjugateTest
    {
        [Theory]
        [InlineData("2^x = x^2")]
        [InlineData("cosh(x) = x")]
        [InlineData("5^x = x^4 + 2")]
        [InlineData("x^x = 2")]
        public void EveryNonRealRootHasItsConjugate(string equation)
        {
            var roots = Assert.IsType<FiniteSet>(((Set)equation.ToEntity().Solve("x")).InnerSimplified)
                .Select(root => Assert.IsAssignableFrom<Number.Complex>(root.Evaled).ToNumerics())
                .ToList();
            Assert.Contains(roots, root => root.Imaginary != 0);
            foreach (var root in roots.Where(root => root.Imaginary != 0))
                Assert.Contains(roots, other => System.Numerics.Complex.Abs(other - System.Numerics.Complex.Conjugate(root)) < 1e-9 * (1 + System.Numerics.Complex.Abs(root)));
        }

        [Fact]
        public void AnEquationInNonRealNumbersIsNotGivenConjugates()
        {
            var roots = Assert.IsType<FiniteSet>(((Set)"2^x = i x^2".ToEntity().Solve("x")).InnerSimplified)
                .Select(root => ((Number.Complex)root.Evaled).ToNumerics()).ToList();
            foreach (var root in roots)
                Assert.DoesNotContain(roots, other => other != root
                    && System.Numerics.Complex.Abs(other - System.Numerics.Complex.Conjugate(root)) < 1e-9);
        }
    }
}
