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
    /// A compiled statement has the truth value evaluation gives it, and none where it
    /// has none: <c>i &gt; 1</c> compares numbers that are not ordered, and <c>NaN &gt; 1</c> compares
    /// with no number. Over <see cref="Complex"/> a comparison was refused, and over
    /// <see cref="double"/> it was false at NaN, so that its negation was true.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/2195">#2195</a>
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class ThreeValuedCompilationTest
    {
        public static readonly TheoryData<string> Statements = new()
        {
            "x > 1", "x < 1", "x >= 1", "x <= 1", "x = 1",
            "not (x > 1)",
            "x > 1 or true", "x > 1 and false", "x > 1 or x < 2", "x > 0 and x < 2",
            "x > 1 xor true", "x > 1 implies false", "false implies x > 1",
            "(x > 1) = (x < 2)",
        };

        private static bool? Interpreted(string statement, Entity at)
            => statement.ToEntity().Substitute("x", at).Evaled switch
            {
                Entity.Boolean value => (bool)value,
                Entity.Number.Complex { IsNaN: true } => null,
                var other => throw new Xunit.Sdk.XunitException($"{statement} at {at} is {other}")
            };

        [Theory]
        [MemberData(nameof(Statements))]
        public void OverTheComplexNumbersItIsWhatEvalNumericalSays(string statement)
        {
            var compiled = statement.ToEntity().Compile<Complex, bool?>("x");
            foreach (var (at, value) in new (Entity, Complex)[] { (2, 2), (1, 1), ("1/2", 0.5), ("i", Complex.ImaginaryOne), ("1 + i", new(1, 1)), (MathS.NaN, double.NaN) })
                Assert.True(Interpreted(statement, at) == compiled(value), $"{statement} at {at}: {Interpreted(statement, at)?.ToString() ?? "none"} interpreted, {compiled(value)?.ToString() ?? "none"} compiled");
        }

        [Theory]
        [MemberData(nameof(Statements))]
        public void OverTheRealsItIsWhatEvalNumericalSays(string statement)
        {
            var compiled = statement.ToEntity().Compile<double, bool?>("x");
            foreach (var (at, value) in new (Entity, double)[] { (2, 2), (1, 1), ("1/2", 0.5), (MathS.NaN, double.NaN) })
                Assert.True(Interpreted(statement, at) == compiled(value), $"{statement} at {at}: {Interpreted(statement, at)?.ToString() ?? "none"} interpreted, {compiled(value)?.ToString() ?? "none"} compiled");
        }

        /// <summary>As a <see cref="bool"/>, a statement with no truth value is not one that holds.</summary>
        [Fact]
        public void AsABooleanAStatementWithNoTruthValueDoesNotHold()
        {
            Assert.False("x > 1".ToEntity().Compile<Complex, bool>("x")(Complex.ImaginaryOne));
            Assert.False("not (x > 1)".ToEntity().Compile<Complex, bool>("x")(Complex.ImaginaryOne));
            Assert.True("x > 1 or true".ToEntity().Compile<Complex, bool>("x")(Complex.ImaginaryOne));
            Assert.False("not (x > 1)".ToEntity().Compile<double, bool>("x")(double.NaN));
            Assert.True("not (x > 1)".ToEntity().Compile<double, bool>("x")(0));
        }

        /// <summary>A value provided a statement with no truth value has none, nor has its Iverson bracket.</summary>
        [Fact]
        public void AValueUnderAStatementWithNoTruthValueHasNone()
        {
            var provided = "x provided x > 1".ToEntity().Compile<Complex, Complex>("x");
            Assert.Equal(new Complex(2, 0), provided(2));
            Assert.True(double.IsNaN(provided(Complex.ImaginaryOne).Real));
            var piecewise = "piecewise(1 provided x > 0, -1 provided x < 0)".ToEntity().Compile<Complex, Complex>("x");
            Assert.Equal(new Complex(-1, 0), piecewise(-3));
            Assert.True(double.IsNaN(piecewise(Complex.ImaginaryOne).Real));
            var iverson = "iverson(x > 1)".ToEntity().Compile<Complex, Complex>("x");
            Assert.Equal(new Complex(1, 0), iverson(2));
            Assert.True(double.IsNaN(iverson(Complex.ImaginaryOne).Real));
        }
    }
}
