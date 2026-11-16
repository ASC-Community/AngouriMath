//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <see cref="MathS.Parse"/> does not throw: a call the grammar refuses, a name refused by name or a function given
    /// the wrong number of arguments, is the failure it describes. It threw the refusal's exception, and so did
    /// <c>Entity.TryParse</c>. <see cref="MathS.FromString(string)"/> still throws it, with its own type.
    /// https://github.com/asc-community/AngouriMath/issues/2498
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ParseReturnsARefusalTest
    {
        [Theory]
        [InlineData("factorize(360)", "factorize")]
        [InlineData("nextprime(10)", "nextprime")]
        [InlineData("norm(v, p, q)", "norm")]
        [InlineData("sin(x, y)", "sin")]
        public void ParseAnswersTheRefusal(string source, string named)
        {
            var reason = MathS.Parse(source).Switch(
                parsed => throw new Xunit.Sdk.XunitException($"{source} parsed as {parsed}"),
                failure => failure.Reason.Switch(
                    unknown => unknown.Reason,
                    missingOperator => throw new Xunit.Sdk.XunitException("a missing operator"),
                    internalError => throw new Xunit.Sdk.XunitException($"an internal error: {internalError.Details}")));
            Assert.Contains(named, reason);
        }

        [Theory]
        [InlineData("factorize(360)")]
        [InlineData("norm(v, p, q)")]
        public void TryParseSaysNo(string source)
            => Assert.False(Entity.TryParse(source, null, out _));

        [Fact]
        public void FromStringStillThrowsTheRefusal()
        {
            Assert.Throws<UnrecognizedFunctionParseException>(() => "factorize(360)".ToEntity());
            Assert.Throws<FunctionArgumentCountException>(() => "norm(v, p, q)".ToEntity());
        }

        [Fact]
        public void AValidInputStillParses()
            => Assert.Equal("sin(x) + 1".ToEntity(), MathS.Parse("sin(x) + 1").Switch(parsed => parsed, _ => null!));
    }
}
