//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// The binomial sums of chapter 8 of Sullivan and Mackey's <i>An Introduction to Proofs</i>
    /// beyond the binomial theorem: a polynomial beside the coefficient (Prop 8.4.4, §8.4.5
    /// Try 2), Vandermonde (Prob 8.9.16) and <c>sum binomial(n, k)^2 = binomial(2n, n)</c>
    /// (Prob 8.9.34), the summation identity (Thm 8.4.6), the sums over the even or the
    /// odd indices (Ex 8.3.11), the trinomial revision summed (Prob 8.9.15 and Thm 8.4.6's
    /// second form), the parallel summation (Prob 8.9.19) and Vandermonde along the upper
    /// indices (Prob 8.9.18). <see href="https://github.com/asc-community/AngouriMath/issues/1409"/>
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class BinomialIdentitiesTest
    {
        /// <summary>The closed form against the sum written out, at small bounds and one past the expansion's hundred terms.</summary>
        [Theory]
        [InlineData("sum(k * binomial(n, k), k, 0, n)", "n * 2^(n - 1)")]
        [InlineData("sum(k^2 * binomial(n, k), k, 0, n)", "n * (n + 1) * 2^(n - 2)")]
        [InlineData("sum(k * binomial(n, k) * x^k, k, 0, n)", "n * x * (x + 1)^(n - 1)")]
        [InlineData("sum((k^2 - k) * binomial(n, k) * 2^k * 3^(n - k), k, 0, n)", "n * (n - 1) * 4 * 5^(n - 2)")]
        [InlineData("sum(n! / (k! * (n - k)!) * k, k, 0, n)", "n * 2^(n - 1)")]
        [InlineData("sum(binomial(n, k)^2, k, 0, n)", "binomial(2 n, n)")]
        [InlineData("sum(binomial(n, i) * binomial(n + 3, 5 - i), i, 0, 5)", "binomial(2 n + 3, 5)")]
        [InlineData("sum(binomial(i, 3), i, 0, n)", "binomial(n + 1, 4)")]
        [InlineData("sum(binomial(n, 2 * l), l, 0, floor(n / 2))", "2^(n - 1)")]
        [InlineData("sum(binomial(n, 2 * l + 1), l, 0, floor((n - 1) / 2))", "2^(n - 1)")]
        [InlineData("sum(binomial(n, i) * binomial(n - i, 3 - i), i, 0, 3)", "8 * binomial(n, 3)")]
        [InlineData("sum(binomial(n, i) * binomial(n - i, 3 - i), i, 0, n)", "8 * binomial(n, 3)")]
        [InlineData("sum(binomial(n, i) * binomial(i, 2), i, 2, n)", "2^(n - 2) * binomial(n, 2)")]
        [InlineData("sum(binomial(n, i) * binomial(i, 2), i, 0, n)", "2^(n - 2) * binomial(n, 2)")]
        [InlineData("sum(binomial(i + 3, i), i, 0, n)", "binomial(n + 4, n)")]
        [InlineData("sum(binomial(i + 3, 3), i, 0, n)", "binomial(n + 4, 4)")]
        [InlineData("sum(binomial(i - 1, 2) * binomial(n - i, 2), i, 1, n)", "binomial(n, 5)")]
        [InlineData("sum(binomial(j, 2) * binomial(n - j, 1), j, 0, n)", "binomial(n + 1, 4)")]
        public void AnIdentityIsClosed(string sum, string expected)
        {
            var closed = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(closed);
            foreach (var n in new[] { 1, 2, 3, 6, 11, 130 })
            {
                var want = expected.ToEntity().Substitute("n", n).Substitute("x", "3/7").Evaled;
                Assert.Equal(want, closed.Substitute("n", n).Substitute("x", "3/7").Evaled);
                Assert.Equal(want, sum.ToEntity().Substitute("n", n).Substitute("x", "3/7").Evaled);
            }
        }

        /// <summary>Vandermonde with both parameters free, and its range written to either of them.</summary>
        [Theory]
        [InlineData("sum(binomial(a, i) * binomial(b, k - i), i, 0, k)")]
        [InlineData("sum(binomial(a, i) * binomial(b, k - i), i, 0, a)")]
        public void VandermondeWithBothParametersFree(string sum)
        {
            var closed = sum.ToEntity().Simplify();
            Assert.IsNotType<Summationf>(closed);
            foreach (var (a, b, k) in new[] { (4, 5, 3), (7, 2, 6), (3, 3, 3), (10, 12, 8) })
            {
                var want = "binomial(a + b, k)".ToEntity().Substitute("a", a).Substitute("b", b).Substitute("k", k).Evaled;
                Assert.Equal(want, closed.Substitute("a", a).Substitute("b", b).Substitute("k", k).Evaled);
            }
        }

        /// <summary>Ex 8.3.11 at n = 0: the even sum is the one empty subset, the odd sum is empty.</summary>
        [Fact]
        public void TheParitySumsAtZero()
        {
            Assert.Equal(Number.Integer.One, "sum(binomial(n, 2 * l), l, 0, floor(n / 2))".ToEntity().Simplify().Substitute("n", 0).Evaled);
            Assert.Equal(Number.Integer.Zero, "sum(binomial(n, 2 * l + 1), l, 0, floor((n - 1) / 2))".ToEntity().Simplify().Substitute("n", 0).Evaled);
        }

        /// <summary>The identities the book proves, decided from the closed forms.</summary>
        [Theory]
        [InlineData("forall n in ZZ+ : sum(k * binomial(n, k), k, 0, n) = n * 2^(n - 1)")]
        [InlineData("forall n in ZZ* : sum(binomial(n, k)^2, k, 0, n) = binomial(2 n, n)")]
        [InlineData("forall n in ZZ* : sum(binomial(i, k), i, 0, n) = binomial(n + 1, k + 1)")]
        [InlineData("forall n in ZZ* : forall k in ZZ* : sum(binomial(n, i) * binomial(n - i, k - i), i, 0, k) = binomial(n, k) * 2^k")]
        [InlineData("forall n in ZZ* : forall k in ZZ* : binomial(n, k) * 2^(n - k) = sum(binomial(n, i) * binomial(i, k), i, k, n)")]
        [InlineData("forall n in ZZ* : forall r in ZZ* : sum(binomial(r + i, i), i, 0, n) = binomial(r + n + 1, n)")]
        [InlineData("forall n in ZZ+ : sum(binomial(i - 1, 2) * binomial(n - i, 2), i, 1, n) = binomial(n, 5)")]
        public void TheIdentityIsDecided(string statement) => Assert.Equal(Boolean.True, statement.ToEntity().Evaled);

        /// <summary>
        /// Identities between coefficients whose upper indices differ by whole numbers, each read
        /// over the lowest by Vandermonde's identity (Probs 8.9.20-8.9.22, Pascal's rule applied
        /// twice and three times), and a product of them read in factorials (the trinomial revision
        /// of §8.4.5) -- with a wrong one of each refuted.
        /// </summary>
        [Theory]
        [InlineData("forall n in ZZ+ : forall k in ZZ+ : binomial(n, k) - binomial(n - 2, k) = 2 * binomial(n - 2, k - 1) + binomial(n - 2, k - 2)", true)]
        [InlineData("forall n in ZZ+ : forall k in ZZ+ : binomial(n, k) - binomial(n - 2, k) = binomial(n - 1, k - 1) + binomial(n - 2, k - 1)", true)]
        [InlineData("forall n in ZZ+ : forall k in ZZ+ : binomial(n, k) - binomial(n - 3, k) = binomial(n - 1, k - 1) + binomial(n - 2, k - 1) + binomial(n - 3, k - 1)", true)]
        [InlineData("forall n in ZZ* : forall k in ZZ* : forall l in ZZ* : binomial(n, k) * binomial(k, l) = binomial(n, l) * binomial(n - l, k - l)", true)]
        [InlineData("forall n in ZZ+ : forall k in ZZ+ : binomial(n, k) - binomial(n - 2, k) = binomial(n - 2, k - 1) + binomial(n - 2, k - 2)", false)]
        [InlineData("forall n in ZZ* : forall k in ZZ* : forall l in ZZ* : binomial(n, k) * binomial(k, l) = binomial(n, l) * binomial(n - k, k - l)", false)]
        public void AnIdentityBetweenCoefficientsIsDecided(string statement, bool holds)
            => Assert.Equal(holds ? Boolean.True : Boolean.False, statement.ToEntity().Evaled);

        /// <summary>
        /// A factorial written out has no pole to share with the coefficient beside it, so the
        /// factorial reading is not taken: <c>binomial(n, k) (n - k)! k! = n!</c> fails at <c>k &gt; n</c>,
        /// where the coefficient is zero and <c>(n - k)!</c> is not a number.
        /// </summary>
        [Fact]
        public void AWrittenFactorialIsNotReadThroughTheCoefficient()
            => Assert.NotEqual(Boolean.True, "forall n in ZZ* : forall k in ZZ* : binomial(n, k) * (n - k)! * k! = n!".ToEntity().Evaled);

        /// <summary>
        /// The two new ranges start away from zero, and each is claimed only where it covers the
        /// whole of the identity's: Vandermonde along the upper indices needs the shifted index to
        /// start at zero, since below zero the first coefficient is not.
        /// </summary>
        [Theory]
        [InlineData("sum(binomial(i - 1, 2) * binomial(n - i, 2), i, 2, n)")]
        [InlineData("sum(binomial(i - 1, 2) * binomial(n - i, 2), i, 0, n)")]
        [InlineData("sum(binomial(n, i) * binomial(i, 2), i, 1, n)")]
        public void ARangeTheIdentityDoesNotCoverIsLeft(string sum)
            => Assert.IsType<Summationf>(sum.ToEntity().Simplify());
    }
}
