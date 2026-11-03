//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading.Tasks;
using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A floor or a ceil whose argument is real at the destination and complex just off it declines its own limit since
    /// #2355, and the substitution that reads an expression's value at the destination answered the expression it is in
    /// anyway: <c>x / floor(arcsin(x))</c> at 1 from the right was <c>1</c>, the value at 1, where the floor all the way in
    /// is <c>1 - i</c> and the quotient <c>(1 + i)/2</c>. And #2355 asked whether such an argument was real at the integer
    /// 10^6 on the way to an infinity, which for <c>1/x!</c> built a number of five and a half million digits. Found by
    /// limitcheck at <c>DEPTH=3</c>, <c>SEED=19</c>; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ASubstitutionDeclinesAStepItsNodeDeclinesTest
    {
        [Theory]
        [InlineData("x / floor(arcsin(x))", "1", ApproachFrom.Right)]
        [InlineData("x / floor(arcsin(x))", "-1", ApproachFrom.Left)]
        public void IsNotTheValueAtTheDestination(string input, string destination, ApproachFrom side)
            => Assert.IsType<Limitf>(MathS.FromString(input).Limit("x", destination, side));

        [Theory]
        [InlineData("x / floor(x + 1/2)", "2", ApproachFrom.Left, "1")]
        [InlineData("floor(arctan(x))", "+oo", ApproachFrom.BothSides, "1")]
        [InlineData("floor(arctan(x))", "-oo", ApproachFrom.BothSides, "-2")]
        [InlineData("floor(x^x)", "2", ApproachFrom.Left, "3")]
        public void ARealStepIsReadAsBefore(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));

        // The cap is far from both: the limit takes about 0.2 s, and took 3.9 s building 10^6!.
        [Fact]
        public void AFactorialOnTheWayToInfinityIsNotBuilt()
        {
            var task = Task.Factory.StartNew(() => MathS.FromString("floor(1/x!)").Limit("x", "+oo", ApproachFrom.BothSides),
                TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(2)), "the limit of floor(1/x!) at +oo did not finish in two seconds");
        }
    }
}
