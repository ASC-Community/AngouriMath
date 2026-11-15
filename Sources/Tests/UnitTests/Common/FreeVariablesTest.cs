//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using HonkSharp.Fluency;
using Xunit;

namespace AngouriMath.Tests.Common
{
    [Trait("Area", "Common")]
    public class FreeVariablesTest
    {
        private IEnumerable<Entity.Variable> SeqVar(params string[] vars)
            => vars.Select(MathS.Var);

        [Fact]
        public void Test1()
            => Assert.Equal(
                SeqVar("x", "y"),
                "x + y".ToEntity().FreeVariables
            );
        
        [Fact]
        public void Test2()
            => Assert.Equal(
                SeqVar("y"),
                "lambda(x, x + y)".ToEntity().FreeVariables
            );
        
        [Fact]
        public void Test3()
            => Assert.Equal(
                SeqVar(),
                "lambda(x, lambda(y, x + y))".ToEntity().FreeVariables
            );
            
        [Fact]
        public void Test4()
            => Assert.Equal(
                SeqVar(),
                "lambda(x, lambda(y, x + y))".ToEntity().FreeVariables
            );
            
        [Fact]
        public void Test5()
            => Assert.Equal(
                SeqVar(),
                "lambda(x, lambda(y, x + y))".ToEntity().FreeVariables
            );
            
        [Fact]
        public void Test6()
            => Assert.Equal(
                SeqVar("x", "y", "z"),
                "x + lambda(x, y + z + x)".ToEntity().FreeVariables
            );
        
        [Fact]
        public void Test7()
            => Assert.Equal(
                SeqVar("x", "y", "z"),
                "x + lambda(f, y + z + x)".ToEntity().FreeVariables
            );

        // A set builder's DirectChildren is its predicate with the bound name renamed
        // to a fresh one, so that two builders differing only in that name compare and
        // hash alike. Reading the answer off DirectChildren therefore reported a name
        // that is in no expression: not the bound name, not typeable, and different for
        // a different predicate. A set builder binds the name it declares exactly as a
        // lambda binds its parameter, so these mirror the lambda cases above.
        // https://github.com/asc-community/AngouriMath/issues/989
        [Theory]
        [InlineData("{ k : k > 0 }")]
        [InlineData("{ k : k > a }")]
        [InlineData("x + { k : k > a }")]
        public void ASetBuildersPlaceholderIsInNoAnswer(string exprRaw)
        {
            var expr = exprRaw.ToEntity();
            Assert.DoesNotContain(expr.FreeVariables, v => v.Name.StartsWith("%"));
            Assert.DoesNotContain(expr.Vars, v => v.Name.StartsWith("%"));
            Assert.DoesNotContain(expr.VarsAndConsts, v => v.Name.StartsWith("%"));
        }

        [Theory]
        [InlineData("{ k : k > 0 }")]
        [InlineData("lambda(k, k > 0)")]
        public void ASetBuilderBindsItsNameAsALambdaDoes(string exprRaw)
            => Assert.Equal(SeqVar(), exprRaw.ToEntity().FreeVariables);

        [Theory]
        [InlineData("{ k : k > a }")]
        [InlineData("lambda(k, k > a)")]
        public void ASetBuilderLeavesTheRestFree(string exprRaw)
            => Assert.Equal(SeqVar("a"), exprRaw.ToEntity().FreeVariables);

        [Theory]
        [InlineData("{ k : k > a }")]
        [InlineData("lambda(k, k > a)")]
        public void ASetBuildersOwnNameStillOccurs(string exprRaw)
            => Assert.Equal(
                SeqVar("k", "a").OrderBy(v => v.Name),
                exprRaw.ToEntity().Vars.OrderBy(v => v.Name));

        // and the rename it publishes is still doing its job: two builders that differ
        // only in the bound name are the same set
        [Fact]
        public void TwoSetBuildersDifferingOnlyInTheBoundNameAreStillEqual()
            => Assert.Equal(
                Entity.Boolean.True,
                "{ x : x > 0 } = { y : y > 0 }".ToEntity().Simplify());

        /// <summary>
        /// A summation and a product bind their index: the value of sum(k, k, 1, n) depends on n
        /// and not on any k outside it.
        /// https://github.com/asc-community/AngouriMath/issues/1019
        /// </summary>
        [Theory]
        [InlineData("sum(k, k, 1, n)")]
        [InlineData("product(k, k, 1, n)")]
        [InlineData("sum(k * 2, k, 1, n)")]
        public void ARangeBinderBindsItsIndex(string exprRaw)
            => Assert.Equal(SeqVar("n"), exprRaw.ToEntity().FreeVariables);

        /// <summary>
        /// The index is bound over the bounds as well as the body, which is what
        /// <see cref="AngouriMath.Core.Binding"/> says of itself.
        /// </summary>
        [Fact]
        public void TheIndexIsBoundOverTheBoundsToo()
            => Assert.Equal(SeqVar("n"), "sum(k, k, k, n)".ToEntity().FreeVariables);

        /// <summary>
        /// A definite integral binds its variable between its limits.
        /// </summary>
        [Theory]
        [InlineData("integral(t * b, t, 0, 1)")]
        [InlineData("integral(t, t, 0, b)")]
        public void ADefiniteIntegralBindsItsVariable(string exprRaw)
            => Assert.Equal(SeqVar("b"), exprRaw.ToEntity().FreeVariables);

        /// <summary>
        /// A limit binds its variable <b>always</b>, where the integral above binds only when it
        /// has limits to bind between — and the reason the two below do not bind is what makes
        /// the difference. An antiderivative and a derivative are still functions of the
        /// variable; a limit never is. <c>lim(t, t, 0)</c> is <c>0</c>.
        /// </summary>
        /// <remarks>
        /// The destination is where a limit's dependence goes, so it is bound over as well:
        /// <c>lim(t, t, b)</c> is a function of <c>b</c> and of nothing else. A one-sided limit
        /// binds the same way — the side is not a variable.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/989">#989</a>
        /// </remarks>
        [Theory]
        [InlineData("limit(t * b, t, 0)")]
        [InlineData("limit(t, t, b)")]
        [InlineData("limitleft(t * b, t, 0)")]
        [InlineData("limitright(t * b, t, 0)")]
        public void ALimitBindsItsVariable(string exprRaw)
            => Assert.Equal(SeqVar("b"), exprRaw.ToEntity().FreeVariables);

        /// <summary>
        /// And the two that look like the same shape and are not. The antiderivative of
        /// <c>t * b</c> over <c>t</c> is <c>b * t ^ 2 / 2 + C</c>, still a function of <c>t</c>;
        /// <c>d/dt</c> denotes a function of <c>t</c> as well. Neither binds, and a sweep that
        /// makes them bind makes them wrong.
        /// </summary>
        [Theory]
        [InlineData("integral(t * b, t)")]
        [InlineData("derivative(t * b, t)")]
        public void AnIndefiniteIntegralAndADerivativeDoNotBind(string exprRaw)
            => Assert.Equal(
                SeqVar("b", "t").OrderBy(v => v.Name),
                exprRaw.ToEntity().FreeVariables.OrderBy(v => v.Name));

        /// <summary>
        /// Binding the index does not hide it from <see cref="Entity.Vars"/>, which means every
        /// name occurring and says so.
        /// </summary>
        [Fact]
        public void ABoundIndexStillOccurs()
            => Assert.Equal(
                SeqVar("k", "n").OrderBy(v => v.Name),
                "sum(k, k, 1, n)".ToEntity().Vars.OrderBy(v => v.Name));

        /// <summary>Only inside the binder that declares it.</summary>
        [Fact]
        public void AnIndexOutsideItsBinderIsStillFree()
            => Assert.Equal(
                SeqVar("k", "n").OrderBy(v => v.Name),
                "sum(k, k, 1, n) + k".ToEntity().FreeVariables.OrderBy(v => v.Name));

        /// <summary>
        /// An indexed union or intersection binds its index as a quantifier does, throughout the
        /// family and the index set: <c>x in union(A_k, k in I)</c> is <c>exists k in I : x in A_k</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2458">#2458</a>
        /// </summary>
        [Theory]
        [InlineData("union({ 2*pi*k + y }, k in ZZ)", "pi y")]
        [InlineData("intersection({ k, y }, k in ZZ)", "y")]
        [InlineData("union([k; y], k in [0; n])", "n y")]
        [InlineData("union({ k*y }, k in { k, n })", "n y")]
        [InlineData("union({ 1/k }, k in ZZ)", "")]
        public void Issue2458_AnIndexedFamilyBindsItsIndex(string exprRaw, string free)
            => Assert.Equal(
                free.Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
                exprRaw.ToEntity().FreeVariables.Select(v => v.Name).OrderBy(n => n, System.StringComparer.Ordinal));

        /// <summary>Only inside the family that declares it, and it still occurs.</summary>
        [Fact]
        public void Issue2458_AnIndexOutsideItsFamilyIsStillFree()
        {
            var expr = "union({ 2*pi*k + y }, k in ZZ) \\/ { k }".ToEntity();
            Assert.Equal(new[] { "k", "pi", "y" }, expr.FreeVariables.Select(v => v.Name).OrderBy(n => n, System.StringComparer.Ordinal));
            Assert.Equal(new[] { "k", "y" }, "union({ 2*pi*k + y }, k in ZZ)".ToEntity().Vars.Select(v => v.Name).OrderBy(n => n, System.StringComparer.Ordinal));
        }
    }
}