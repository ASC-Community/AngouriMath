//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <c>factorization(n)</c>, the map from each prime to its exponent in a positive integer
    /// (mathlib's <c>Nat.factorization</c>), and <c>primefactors(n)</c>, its support. Both names
    /// were read as products, <c>factorization * 360</c>.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2449">#2449</a>
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class PrimeFactorizationNodeTest
    {
        private static PrimeFactorizationf Evaluated(string source)
            => Assert.IsType<PrimeFactorizationf>(source.ToEntity().Evaled);

        private static (string Prime, string Power)[] Pairs(PrimeFactorizationf factorization)
            => Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<(Number.Integer Prime, Number.Integer Power)>>(factorization.Factors)
                .Select(factor => (factor.Prime.ToString(), factor.Power.ToString())).ToArray();

        [Fact]
        public void TheMapOf360()
            => Assert.Equal(new[] { ("2", "3"), ("3", "2"), ("5", "1") }, Pairs(Evaluated("factorization(360)")));

        [Fact]
        public void OneIsTheEmptyMap() => Assert.Empty(Pairs(Evaluated("factorization(1)")));

        [Fact]
        public void APrimeIsItselfOnce() => Assert.Equal(new[] { ("97", "1") }, Pairs(Evaluated("factorization(97)")));

        [Fact]
        public void PastTheLongs()
            => Assert.Equal(new[] { ("274177", "1"), ("67280421310721", "1") }, Pairs(Evaluated("factorization(2^64 + 1)")));

        [Fact]
        public void TheArgumentIsEvaluatedAndTheNodeKept()
            => Assert.Equal("factorization(360)".ToEntity(), "factorization(2^3 * 45)".ToEntity().Evaled);

        // Not a number, so not equal to the number it factorises: the equation is left as written.
        [Fact]
        public void TheFactorizationIsNotTheNumber()
            => Assert.IsNotType<Boolean>("factorization(360) = 360".ToEntity().Evaled);

        [Theory]
        [InlineData("factorization(0)")]
        [InlineData("factorization(-12)")]
        [InlineData("factorization(-1)")]
        [InlineData("factorization(1/2)")]
        [InlineData("factorization(2.5)")]
        [InlineData("primefactors(0)")]
        [InlineData("primefactors(-12)")]
        [InlineData("primefactors(1/2)")]
        public void NaNOffThePositiveIntegers(string source) => Assert.Equal(MathS.NaN, source.ToEntity().Evaled);

        [Theory]
        [InlineData("factorization(n)", typeof(PrimeFactorizationf))]
        [InlineData("primefactors(n)", typeof(Set.PrimeFactorsf))]
        public void LeftAsWrittenForASymbol(string source, System.Type node)
        {
            Assert.IsType(node, source.ToEntity().Evaled);
            Assert.Equal("n in ZZ+".ToEntity().Evaled, source.ToEntity().DomainCondition.Evaled);
        }

        [Theory]
        [InlineData("primefactors(360)", "{ 2, 3, 5 }")]
        [InlineData("primefactors(1)", "{ }")]
        [InlineData("primefactors(97)", "{ 97 }")]
        [InlineData("primefactors(2^64 + 1)", "{ 274177, 67280421310721 }")]
        [InlineData("3 in primefactors(360)", "True")]
        [InlineData("7 in primefactors(360)", "False")]
        [InlineData("card(primefactors(360)) = primenu(360)", "True")]
        public void TheSupport(string source, string expected) => Assert.Equal(expected.ToEntity().Evaled, source.ToEntity().Evaled);

        // The defining identity: the product over the support of p to its exponent is n.
        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("97")]
        [InlineData("360")]
        [InlineData("1024")]
        [InlineData("9699690")]
        [InlineData("2^64 + 1")]
        [InlineData("3^40 * 7^3")]
        public void TheProductOverTheSupportIsN(string n)
            => Assert.Equal(n.ToEntity().Evaled, $"product(p ^ valuation({n}, p), p in primefactors({n}))".ToEntity().Evaled);

        [Fact]
        public void SubstitutionAndFreeVariables()
        {
            var node = "factorization(n)".ToEntity();
            Assert.Equal(new[] { MathS.Var("n") }, node.FreeVariables);
            Assert.Equal("factorization(360)".ToEntity(), node.Substitute("n", 360).Evaled);
            Assert.Equal("{ 2, 3, 5 }".ToEntity().Evaled, "primefactors(n)".ToEntity().Substitute("n", 360).Evaled);
        }

        [Fact]
        public void Printing()
        {
            Assert.Equal("factorization(360)", MathS.NumberTheory.Factorization(360).ToString());
            Assert.Equal("primefactors(n)", MathS.NumberTheory.PrimeFactors("n").ToString());
            Assert.Equal("factorization(n)".ToEntity(), "factorization(n)".ToEntity().ToString().ToEntity());
            Assert.Equal("primefactors(n)".ToEntity(), "primefactors(n)".ToEntity().ToString().ToEntity());
            Assert.Equal(Evaluated("factorization(360)"), Evaluated("factorization(360)").ToString().ToEntity().Evaled);
        }

        [Theory]
        [InlineData("factorization(360)", @"2^{3} \cdot 3^{2} \cdot 5")]
        [InlineData("factorization(97)", "97")]
        [InlineData("factorization(8)", "2^{3}")]
        [InlineData("factorization(1)", "1")]
        [InlineData("factorization(n)", @"\operatorname{factorization}\left(n\right)")]
        [InlineData("primefactors(n)", @"\operatorname{primefactors}\left(n\right)")]
        public void Latex(string source, string latex) => Assert.Equal(latex, source.ToEntity().Evaled.Latexize());

        // A power of a factorisation is bracketed as a product is.
        [Fact]
        public void LatexBracketsAProduct()
            => Assert.Contains(@"\left(2^{3} \cdot 3^{2} \cdot 5\right)", MathS.Pow(Evaluated("factorization(360)"), 2).Latexize());

        [Fact]
        public void Codomain()
        {
            Assert.Equal(Domain.Any, "factorization(n)".ToEntity().Codomain);
            Assert.Equal(Domain.Any, "primefactors(n)".ToEntity().Codomain);
        }

        [Fact]
        public void SymPy()
        {
            Assert.Contains("sympy.factorint(360)", MathS.ToSympyCode("factorization(360)".ToEntity()));
            Assert.Contains("sympy.FiniteSet(*sympy.primefactors(360))", MathS.ToSympyCode("primefactors(360)".ToEntity()));
            Assert.Throws<NotSufficientlySupportedException>(() => MathS.ToSympyCode("factorization(n)".ToEntity()));
        }

        [Theory]
        [InlineData("factorization(x)", "prime factorisation")]
        [InlineData("factorization(360)", "prime factorisation")]
        [InlineData("primefactors(x)", "prime factors")]
        public void BothCompilersRefuseNamingIt(string source, string named)
        {
            Assert.Contains(named, Assert.Throws<UncompilableNodeException>(() => source.ToEntity().Compile("x")).Message);
            Assert.Contains(named, Assert.Throws<UncompilableNodeException>(() => source.ToEntity().Compile<double, double>("x")).Message);
        }

        [Fact]
        public void Serialization()
        {
            foreach (var source in new[] { "factorization(n)", "primefactors(n)" })
            {
                var entity = source.ToEntity();
                Assert.Equal(entity, System.Text.Json.JsonSerializer.Deserialize<Entity>(System.Text.Json.JsonSerializer.Serialize(entity)));
            }
        }
    }
}
