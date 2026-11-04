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

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit at a finite point is taken by moving the variable out to infinity, and the expression was simplified there
    /// before its value was read: <c>x / (tanh(1/2) - tan(x))</c> at 1, a point where it is continuous, took 10 s, and
    /// <c>tanh(x) / (tanh(1/2) - tan(x))</c> 18 s, for a value the substitution reads in milliseconds. Found by
    /// limitcheck at <c>DEPTH=3</c>, <c>SEED=28</c>, as twelve limits that did not finish in 5 s; no issue was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class AContinuousPointIsReadBeforeSimplifyingTest
    {
        // The cap is far from both: each takes about 0.1 s, and took 10 to 18 s.
        [Theory]
        [InlineData("x / (tanh(1/2) - tan(x))", "1", ApproachFrom.BothSides)]
        [InlineData("tanh(x) / (tanh(1/2) - tan(x))", "1", ApproachFrom.BothSides)]
        [InlineData("tanh(x) / (tanh(1/2) - tan(x))", "-1", ApproachFrom.Left)]
        [InlineData("tanh(x) / (tanh(1/2) - tan(x))", "2", ApproachFrom.Right)]
        public void IsTheValueThereAndPrompt(string input, string destination, ApproachFrom side)
        {
            var expression = MathS.FromString(input);
            var task = Task.Factory.StartNew(() => expression.Limit("x", destination, side), TaskCreationOptions.LongRunning);
            Assert.True(task.Wait(TimeSpan.FromSeconds(5)), $"lim {side} x->{destination} {input} did not finish in five seconds");
            var difference = (task.Result - expression.Substitute("x", destination)).EvalNumerical();
            Assert.True(difference is Entity.Number.Complex value && Entity.Number.Complex.Abs(value) is Entity.Number.Real abs && abs < 1e-10,
                $"lim {side} x->{destination} {input} = {task.Result}");
        }

        // A pole of the factorial is not a value to read: x! at -1 has an infinity of each sign on its two sides.
        [Fact]
        public void TheFactorialAtAPoleIsNotItsValue()
        {
            Assert.IsType<Entity.Limitf>(MathS.FromString("x!").Limit("x", -1, ApproachFrom.Left));
            Assert.IsType<Entity.Limitf>(MathS.FromString("x!").Limit("x", -1, ApproachFrom.Right));
        }
    }
}
