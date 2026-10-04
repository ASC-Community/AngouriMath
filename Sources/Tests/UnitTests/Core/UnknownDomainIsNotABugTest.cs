//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core;
using AngouriMath.Core.Exceptions;
using Xunit;

namespace UnitTests.Core
{
    /// <summary>
    /// https://github.com/asc-community/AngouriMath/issues/1028 -- a caller who names a domain
    /// this library does not have was answered with AngouriBugException, which asks them to
    /// report their own input to the issue tracker.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class UnknownDomainIsNotABugTest
    {
        [Theory]
        [InlineData("NN")]
        [InlineData("Naturals")]
        [InlineData("rr")]
        [InlineData("")]
        public void AnUnknownDomainNameIsTheCallersInput(string domain)
        {
            var ex = Assert.Throws<UnrecognizedDomainException>(
                () => Entity.Set.SpecialSet.Create(domain));
            Assert.Contains(domain, ex.Message);
            Assert.IsNotType<AngouriBugException>(ex);
        }

        [Theory]
        [InlineData("BB")]
        [InlineData("ZZ")]
        [InlineData("QQ")]
        [InlineData("RR")]
        [InlineData("CC")]
        [InlineData("Booleans")]
        [InlineData("Integers")]
        [InlineData("Rationals")]
        [InlineData("Reals")]
        [InlineData("Complexes")]
        public void EveryKnownDomainNameStillAnswers(string domain)
            => Assert.NotNull(Entity.Set.SpecialSet.Create(domain));

        /// <summary>
        /// Domain.Any is a documented member of the enum and means "no restriction", which is
        /// not a set this library has a node for. Domains.IsWithinDomain answers it before ever
        /// reaching Create, so nothing in the library hits this -- but the method is public.
        /// </summary>
        [Fact]
        public void ThereIsNoSetForAny()
        {
            var ex = Assert.Throws<NotSufficientlySupportedException>(
                () => Entity.Set.SpecialSet.Create(Domain.Any));
            Assert.Contains("Any", ex.Message);
        }

        [Fact]
        public void ThereIsNoSetForAValueOutsideTheEnum()
            => Assert.Throws<NotSufficientlySupportedException>(
                () => Entity.Set.SpecialSet.Create((Domain)99));

        [Theory]
        [InlineData(Domain.Boolean)]
        [InlineData(Domain.Integer)]
        [InlineData(Domain.Rational)]
        [InlineData(Domain.Real)]
        [InlineData(Domain.Complex)]
        public void EveryDomainThatIsASetStillAnswers(Domain domain)
            => Assert.NotNull(Entity.Set.SpecialSet.Create(domain));

        /// <summary>
        /// Every one of these remains catchable by the one exception the documentation tells a
        /// caller to catch.
        /// </summary>
        [Fact]
        public void BothRemainUnderTheBaseException()
        {
            Assert.Throws<UnrecognizedDomainException>(() => Entity.Set.SpecialSet.Create("NN"));
            Assert.IsAssignableFrom<AngouriMathBaseException>(
                Record.Exception(() => Entity.Set.SpecialSet.Create("NN")));
            Assert.IsAssignableFrom<AngouriMathBaseException>(
                Record.Exception(() => Entity.Set.SpecialSet.Create(Domain.Any)));
        }
    }
}
