//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Calculus
{
    /// <summary>
    /// A limit of a derivative is the limit of the function the derivative is, and the variable a derivative or a limit
    /// is taken in is not a value to put a limit in. Read as nodes, derivative(ln(x y), y) at 0 became the derivative of
    /// the constant ln(0) and was answered 0, derivative(sqrt(x), x) at 0 came back derivative(0, 0), and
    /// limit(sqrt(x) cos(x) / x, x, 0) at 1 came back limit(cos(1), 1, 0). Found by probing a slow limit; no issue
    /// was filed.
    /// </summary>
    [Trait("Area", "Calculus")]
    public sealed class ALimitOfABinderTest
    {
        [Theory]
        [InlineData("derivative(sqrt(x), x)", "0", ApproachFrom.Right, "+oo")]
        [InlineData("derivative(ln(sqrt(x) * cos(x)), x)", "0", ApproachFrom.Right, "+oo")]
        [InlineData("derivative(abs(x), x)", "0", ApproachFrom.Right, "1")]
        [InlineData("derivative(sin(x) / x, x)", "0", ApproachFrom.BothSides, "0")]
        [InlineData("derivative(ln(x), x)", "0", ApproachFrom.Left, "-oo")]
        [InlineData("derivative(x^3, x, 2)", "1", ApproachFrom.BothSides, "6")]
        public void ADerivativeIsTakenBeforeTheLimit(string input, string destination, ApproachFrom side, string expected)
            => Assert.Equal(MathS.FromString(expected), MathS.FromString(input).Limit("x", destination, side));

        // 1 / y, where x y is not 0; whether it is not 0 near x = 0 depends on y, so the limit is left alone, not 0.
        [Fact]
        public void ADerivativeByAnotherVariableIsNotTheDerivativeOfAConstant()
            => Assert.IsType<Limitf>(MathS.FromString("derivative(ln(x * y), y)").Limit("x", 0, ApproachFrom.Right));

        // The inner limit binds x, so it does not depend on the outer one; its value, if any, is not limit(cos(1), 1, 0).
        [Fact]
        public void ALimitInItsOwnVariableIsNotSubstitutedInto()
            => Assert.DoesNotContain(MathS.FromString("limit(sqrt(x) * cos(x) / x, x, 0)").Limit("x", 1, ApproachFrom.Right).Nodes,
                node => node is Limitf { Var: not Variable });
    }
}
