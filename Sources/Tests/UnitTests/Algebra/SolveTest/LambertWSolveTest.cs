//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Extensions;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Algebra.SolveTest
{
    /// <summary>
    /// A linear function times an exponential of another is solved by the Lambert W function, on
    /// every branch over the complex numbers and on the real ones over the reals. It was answered with
    /// roots found by Newton's method.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2182">#2182</a>
    /// </summary>
    [Trait("Area", "Algebra")]
    public sealed class LambertWSolveTest
    {
        private static void Satisfies(string equation, Entity root)
        {
            var residual = equation.ToEntity() is Equalsf(var left, var right) ? left - right : equation.ToEntity();
            var value = (Number.Complex)residual.Substitute("x", root).EvalNumerical();
            Assert.True(value.Abs().EDecimal.CompareTo(EDecimal.FromString("1e-25")) < 0, $"{root.Stringize()} leaves {value.Stringize()}");
        }

        [Theory]
        [InlineData("x e^x = 2", "lambertw(2)")]
        [InlineData("3 x e^x = 6", "lambertw(2)")]
        [InlineData("x e^(2 x) = 3", "lambertw(6) / 2")]
        [InlineData("x * 2^x = 5", "lambertw(5 ln(2)) / ln(2)")]
        [InlineData("x e^x = -1/e", "-1")]
        public void OverTheRealsTheRealBranchIsTheRoot(string equation, string expected)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            var roots = Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x"));
            var root = Assert.Single(roots);
            Assert.Equal(Number.Integer.Zero, (root - expected.ToEntity()).Simplify());
            Satisfies(equation, root);
        }

        /// <summary>Between -1/e and 0 both real branches are roots, and below -1/e neither is.</summary>
        [Fact]
        public void BetweenMinusOneOverEAndZeroThereAreTwoRealRoots()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            var roots = Assert.IsType<Set.FiniteSet>("x e^x = -1/4".ToEntity().Solve("x"));
            Assert.Equal(2, roots.Count);
            foreach (var root in roots)
                Satisfies("x e^x = -1/4", root);
            Assert.Equal(Set.Empty, "x e^x = -1".ToEntity().Solve("x"));
        }

        /// <summary>
        /// An exponential equal to a line over the reals: e^x = x + 2 meets the line twice, e^x = e x
        /// touches it at 1, and e^x = 2 x does not meet it, since e^x - 2x is 2 - 2 ln 2 at its least.
        /// </summary>
        [Theory]
        [InlineData("e^x = x + 2", 2)]
        [InlineData("e^x = e x", 1)]
        [InlineData("e^x = 2 x", 0)]
        public void AnExponentialMeetsALineOverTheReals(string equation, int count)
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            var roots = Assert.IsType<Set.FiniteSet>(equation.ToEntity().Solve("x"));
            Assert.Equal(count, roots.Count);
            foreach (var root in roots)
                Satisfies(equation, root);
        }

        /// <summary>Over the reals a symbolic right side carries where each branch is real.</summary>
        [Fact]
        public void ASymbolicRightSideCarriesTheConditions()
        {
            using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            var roots = Assert.IsType<Set.FiniteSet>("x e^x = c".ToEntity().Solve("x"));
            Assert.Equal(2, roots.Count);
            Assert.All(roots, root => Assert.IsType<Providedf>(root));
        }

        /// <summary>Over the complex numbers every branch is a root, W_n for a whole n.</summary>
        [Theory]
        [InlineData("x e^x = 2")]
        [InlineData("(x + 1) e^x = 2")]
        [InlineData("(2 x - 1) e^(3 x + 1) = 4")]
        // An exponential equal to a line is the product written as a sum: e^x = x + 2 is (x + 2) e^(-x) = 1.
        [InlineData("e^x = x + 2")]
        [InlineData("e^x = 2 x")]
        [InlineData("3 e^(2 x) - x = 1")]
        [InlineData("2^x = x + 3")]
        public void OverTheComplexNumbersEveryBranchIsARoot(string equation)
        {
            // The branches are a union over the whole numbers.
            // https://github.com/asc-community/AngouriMath/issues/2319
            var family = Assert.IsType<Set.IndexedUnionf>(equation.ToEntity().Solve("x"));
            Assert.Equal(MathS.Sets.Z, family.Over);
            foreach (var root in family.Members(-2, 0, 1, 3))
                Satisfies(equation, root);
        }
    }
}
