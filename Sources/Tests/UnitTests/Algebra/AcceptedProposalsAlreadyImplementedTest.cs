//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Algebra
{
    /// <summary>
    /// Three accepted proposals that ask for behaviour the library already has. Each is
    /// pinned here by the example the proposal itself gives, so that closing it rests on a
    /// test rather than on a reading.
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class AcceptedProposalsAlreadyImplementedTest
    {
        /// <summary>
        /// "we need a logarithmic solver which converts equation with logarithms to
        /// polynomial, if possible".
        /// https://github.com/asc-community/AngouriMath/issues/246
        /// </summary>
        [Theory]
        [InlineData("log(2, x) + log(2, x - 1) = 1", "2")]
        [InlineData("log(3, x) = 2", "9")]
        [InlineData("ln(x) + ln(x + 1) = ln(6)", "2")]
        public void ALogarithmicEquationIsSolved(string equation, string root)
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>(equation.ToEntity().Solve("x").InnerSimplified);
            Assert.Contains(root.ToEntity(), roots);
            foreach (var found in roots)
                AssertSatisfies(equation, found);
        }

        /// <summary>
        /// The quadratic-in-a-logarithm case, which is the "converts to polynomial" half.
        /// </summary>
        [Fact]
        public void AQuadraticInALogarithmIsSolved()
        {
            var roots = Assert.IsType<Entity.Set.FiniteSet>(
                "ln(x) ^ 2 - 3 * ln(x) + 2 = 0".ToEntity().Solve("x").InnerSimplified);
            Assert.Contains(MathS.e, roots);
            Assert.Contains(MathS.e.Pow(2).InnerSimplified, roots);
        }

        /// <summary>
        /// "So that <c>a ^ f(x) + b ^ g(x) + ... = 0</c> could be solved."
        /// https://github.com/asc-community/AngouriMath/issues/214
        /// </summary>
        [Theory]
        [InlineData("2 ^ x + 4 ^ x = 6", "1")]
        [InlineData("3 ^ (2 * x) - 4 * 3 ^ x + 3 = 0", "1")]
        [InlineData("2 ^ x = 8", "3")]
        public void AnExponentialEquationIsSolved(string equation, string root)
        {
            // Each root is a family over the complex numbers, and the proposal's is its principal
            // member. https://github.com/asc-community/AngouriMath/issues/2319
            var solved = equation.ToEntity().Solve("x");
            Assert.Contains(root.ToEntity(), solved.Principal());
            foreach (var found in solved.Members(0))
                AssertSatisfies(equation, found);
            var difference = equation.ToEntity() is Entity.Equalsf(var lhs, var rhs) ? lhs - rhs : equation.ToEntity();
            foreach (var found in solved.Members(-1, 1))
                Assert.True(((Entity.Number.Complex)difference.Substitute("x", found).EvalNumerical()).Abs().EDecimal.ToDouble() < 1e-10,
                    $"{found.Stringize()} is no root of {equation}");
        }

        /// <summary>
        /// The proposal's own worked example: <c>e^x + sin(x) + 2e^x + 2sin(x)</c> should
        /// collect to <c>3e^x + 3sin(x)</c>, by treating each distinct non-polynomial term
        /// as an unknown of its own.
        /// https://github.com/asc-community/AngouriMath/issues/185
        /// </summary>
        [Fact]
        public void UnlikeTermsAreCollectedByTreatingEachAsAnUnknown()
        {
            var difference = ("e^x + sin(x) + 2*e^x + 2*sin(x)".ToEntity()
                              - "3*e^x + 3*sin(x)".ToEntity()).Simplify();
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        /// <summary>
        /// Every root is substituted back. A solver that answers the right root alongside a
        /// wrong one is not solving the equation, and the printed set does not say which it
        /// has done.
        /// </summary>
        private static void AssertSatisfies(string equation, Entity root)
        {
            var residual = equation.ToEntity() is Entity.Equalsf(var lhs, var rhs)
                ? lhs - rhs
                : equation.ToEntity();
            var at = residual.Substitute("x", root).Simplify();
            while (at is Entity.Providedf(var inner, _)) at = inner;
            Assert.Equal(Entity.Number.Integer.Create(0), at);
        }
    }
}
