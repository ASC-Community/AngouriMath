//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Common
{
    /// <summary>
    /// Regression tests for evaluation, limits and the equation solvers.
    /// Each test names the issue it locks down, so a future refactor that
    /// reintroduces the bug fails loudly.
    /// </summary>
    [Trait("Area", "Common")]
    public sealed class SolverRegressionTest
    {
        // https://github.com/asc-community/AngouriMath/issues/442
        // Equality of two constants used to compare their separately evaluated values
        // for exact digit equality. sqrt(i) and (1 + i) / sqrt(2) are the same number,
        // but evaluating each rounds it differently in the last digits, so the
        // comparison answered False -- a wrong answer, not merely an unsimplified one.
        [Theory]
        [InlineData("sqrt(i) = (1 + i) / sqrt(2)")]
        [InlineData("sqrt(2) * sqrt(3) = sqrt(6)")]
        [InlineData("2 ^ (1/2) = sqrt(2)")]
        [InlineData("e ^ (i * pi) = -1")]
        [InlineData("sin(pi / 4) = sqrt(2) / 2")]
        [InlineData("ln(e ^ 3) = 3")]
        public void Issue442_TrueConstantEqualitiesAreTrue(string statement) =>
            Assert.Equal(MathS.Boolean.True, statement.ToEntity().Simplify());

        // The other direction matters just as much: the fix must not collapse
        // genuinely different constants into equality.
        [Theory]
        [InlineData("sqrt(i) = (1 - i) / sqrt(2)")]
        [InlineData("sqrt(2) = sqrt(3)")]
        [InlineData("pi = 3")]
        [InlineData("e ^ (i * pi) = 1")]
        [InlineData("1 = 2")]
        [InlineData("sqrt(2) = 1.41")]
        public void Issue442_FalseConstantEqualitiesStayFalse(string statement) =>
            Assert.Equal(MathS.Boolean.False, statement.ToEntity().Simplify());

        // https://github.com/asc-community/AngouriMath/issues/632
        // Solving `a = 1/(b - c)` for c returned `1/a - b`, the negation of the correct
        // `b - 1/a`. Minusf's inverter had its two branches swapped with respect to its
        // own comments, so a subtraction containing the unknown inverted with the wrong
        // sign. Checked numerically so the assertion does not depend on output form.
        [Theory]
        // a = 1/(b - c)  =>  c = b - 1/a ; with a = 2, b = 5 that is 4.5
        [InlineData("2 = 1 / (5 - c)", "c", 4.5)]
        // a = 1/(b + c)  =>  c = 1/a - b ; with a = 2, b = 5 that is -4.5
        [InlineData("2 = 1 / (5 + c)", "c", -4.5)]
        // b - c = a  =>  c = b - a
        [InlineData("5 - c = 2", "c", 3.0)]
        // c - b = a  =>  c = a + b
        [InlineData("c - 5 = 2", "c", 7.0)]
        [InlineData("2 = 3 / (5 - c)", "c", 3.5)]
        public void Issue632_SubtractionInvertsWithTheRightSign(string statement, string variable, double expected)
        {
            var roots = (Entity.Set.FiniteSet)statement.ToEntity().Solve(variable);
            var actual = Assert.Single(roots).EvalNumerical().RealPart.EDecimal.ToDouble();
            Assert.Equal(expected, actual, 9);
        }

        [Fact]
        public void Issue632_SymbolicFormIsCorrect()
        {
            var roots = (Entity.Set.FiniteSet)"a = 1 / (b - c)".ToEntity().Solve("c");
            var root = Assert.Single(roots);
            // Must equal b - 1/a, not 1/a - b. The difference carries the domain
            // condition `not a = 0`, which is correct and not what is under test here.
            var difference = (root - "b - 1/a".ToEntity()).Simplify();
            if (difference is Entity.Providedf(var expression, _)) difference = expression;
            Assert.Equal(Entity.Number.Integer.Create(0), difference);
        }

        // https://github.com/asc-community/AngouriMath/issues/662
        // A 1.3 -> 1.4 regression. The Euclidean norm of a vector was built and then
        // InnerSimplified even when the caller asked for numeric evaluation, so the
        // norm stayed as `sqrt(369)` and EvalNumerical refused it. It only worked when
        // the norm happened to be a perfect square.
        [Fact]
        public void Issue662_VectorNormEvaluatesNumerically() =>
            Assert.Equal(19.2093727122985, "(|[12,15] - [0,0]|)".ToEntity()
                .EvalNumerical().RealPart.EDecimal.ToDouble(), 10);

        [Theory]
        [InlineData("(|[3,4]|)", 5.0)]              // perfect square: worked before too
        [InlineData("(|[12,15]|)", 19.2093727122985)]
        [InlineData("(|[1,1]|)", 1.4142135623731)]
        [InlineData("(|[2,3,6]|)", 7.0)]
        public void Issue662_VectorNorms(string input, double expected) =>
            Assert.Equal(expected, input.ToEntity().EvalNumerical().RealPart.EDecimal.ToDouble(), 10);

        [Fact]
        public void Issue662_EvaledIsANumber() =>
            Assert.IsAssignableFrom<Entity.Number>("(|[12,15]|)".ToEntity().Evaled);

        // The exact path must keep its symbolic answer -- this fix is only about the
        // numeric one. The radicand comes out reduced (369 = 9 * 41) since perfect powers
        // are pulled out from under a root:
        // https://github.com/asc-community/AngouriMath/issues/281
        [Fact]
        public void Issue662_ExactPathStaysSymbolic() =>
            Assert.Equal("3 * sqrt(41)".ToEntity(), "(|[12,15]|)".ToEntity().InnerSimplified);

        // Not filed upstream; found while measuring solver coverage.
        // `lim x->0 (1+x)^(1/x)` answered 1 instead of e. The second remarkable limit
        // only fired when the exponent had a two-sided infinite limit, and 1/x at 0
        // has none (it goes to -oo on the left and +oo on the right) even though its
        // magnitude diverges, which is all the rule needs.
        [Theory]
        [InlineData("(1 + x) ^ (1/x)", "0", "e")]
        [InlineData("(1 + 2 * x) ^ (1/x)", "0", "e ^ 2")]
        [InlineData("(1 + x) ^ (3/x)", "0", "e ^ 3")]
        // The x -> +oo form already worked; keep it pinned so the fix does not regress it.
        [InlineData("(1 + 1/x) ^ x", "+oo", "e")]
        [InlineData("(1 + 2/x) ^ x", "+oo", "e ^ 2")]
        public void SecondRemarkableLimitAppliesWhenOnlyTheMagnitudeDiverges(
            string expression, string approach, string expected)
        {
            var limit = expression.ToEntity().Limit("x", approach.ToEntity()).Simplify();
            Assert.Equal(Entity.Number.Integer.Create(0),
                (limit - expected.ToEntity()).Simplify());
        }

        [Fact]
        public void Issue442_InfinitiesAreNotConflated()
        {
            Assert.Equal(MathS.Boolean.True, "+oo = +oo".ToEntity().Simplify());
            Assert.Equal(MathS.Boolean.False, "+oo = -oo".ToEntity().Simplify());
            Assert.Equal(MathS.Boolean.False, "+oo = 5".ToEntity().Simplify());
        }

        // Not in the tracker; found by the solver corpus. Solving a sum of logarithms
        // goes through log(a) + log(b) = log(a * b), which widens the domain: the roots
        // of x^2 + x - 1 include -1.618..., where the original equation is 2*pi*i and not
        // 0. Every root is now checked against the equation it came from.
        [Theory]
        [InlineData("ln(x) + ln(x + 1) = 0", "(-1 + sqrt(5)) / 2")]
        [InlineData("ln(x) + ln(x - 3) = ln(4)", "4")]
        public void SolverDropsRootsOutsideTheDomainOfTheOriginalEquation(string equation, string expected)
        {
            var roots = (Entity.Set.FiniteSet)equation.ToEntity().Solve("x");
            var root = Assert.Single(roots);
            Assert.Equal(expected.ToEntity().EvalNumerical().RealPart.EDecimal.ToDouble(),
                root.EvalNumerical().RealPart.EDecimal.ToDouble(), 9);
        }

        // Every root must still survive verification -- the check drops answers only on
        // positive evidence, never merely because it could not evaluate them.
        [Theory]
        [InlineData("x ^ 2 - 2 = 0", 2)]
        [InlineData("x ^ 2 + 1 = 0", 2)]
        [InlineData("sin(x) = 0", 2)]          // parametric family, nothing to substitute
        [InlineData("x ^ 3 - 1 = 0", 3)]
        [InlineData("x + ln(x) = 0", 1)]
        public void RootVerificationKeepsEveryGenuineRoot(string equation, int expected) =>
            Assert.Equal(expected, ((Entity.Set.FiniteSet)equation.ToEntity().Solve("x")).Count);

        // https://github.com/asc-community/AngouriMath/issues/115
        // The numerical solver starts from a two-dimensional grid, so its resolution
        // along the real axis is only the square root of what the grid costs: the
        // default 10 x 10 over [-10, 10] lays real starting points 2 apart. All three
        // roots of the reporter's equation lie within [-1/2, 1/2], so they shared a
        // single starting point and only 0 ever came back. Every root was inside the
        // region already being searched, which is what makes this a defect rather than
        // a limit of the method.
        [Theory]
        [InlineData("arcsin(x) - x * pi / 3", 0.5)]
        [InlineData("arcsin(x) - 1.2 * x", 0.8556152428091417)]
        public void Issue115_NewtonFindsRootsCloserTogetherThanTheGridSpacing(
            string equation, double outer)
        {
            var roots = (Entity.Set.FiniteSet)equation.ToEntity().SolveEquation("x");
            var found = roots.Select(root => root.EvalNumerical().RealPart.EDecimal.ToDouble())
                             .OrderBy(value => value).ToList();
            Assert.Equal(3, found.Count);
            Assert.Equal(-outer, found[0], 9);
            Assert.Equal(0d, found[1], 9);
            Assert.Equal(outer, found[2], 9);
        }

        // Not in the tracker. Simplification leaves domain conditions behind, and the
        // Newton solver compiled the simplified form without stripping them, so an
        // internal UncompilableNodeException escaped through the public Solve.
        [Fact]
        public void NewtonSolverDoesNotLeakAnUncompilableNodeException()
        {
            var roots = (Entity.Set.FiniteSet)"x + ln(x) = 0".ToEntity().Solve("x");
            var root = Assert.Single(roots);
            // The omega constant: the root of x = -ln(x).
            Assert.Equal(0.5671432904097838, root.EvalNumerical().RealPart.EDecimal.ToDouble(), 9);
        }

        // abs(x) = a was inverted to the circle a * e ^ (i * r), which has modulus |a| and
        // so only solves the equation where a is real and not negative. The condition was
        // missing, so a negative right-hand side came back with a set of non-solutions
        // rather than with nothing. https://github.com/asc-community/AngouriMath/issues/812
        [Theory]
        [InlineData("abs(x) = -1")]
        [InlineData("abs(x) = -3/2")]
        [InlineData("abs(x) + 1 = 0")]
        public void Issue812_AnAbsoluteValueCannotEqualANegative(string equation)
        {
            var solutions = (Entity.Set.FiniteSet)equation.ToEntity().Solve("x");
            Assert.Empty(solutions);
        }

        // The guard must not cost the answers that were right. Each solution is checked by
        // substituting it back, with the free parameter of the circle given a value, rather
        // than by comparing the printed form.
        [Theory]
        [InlineData("3", 3.0)]
        [InlineData("1/2", 0.5)]
        [InlineData("0", 0.0)]
        public void Issue812_ANonNegativeRightHandSideStillSolves(string rhs, double modulus)
        {
            var solutions = (Entity.Set.FiniteSet)$"abs(x) = {rhs}".ToEntity().Solve("x");
            var solution = Assert.Single(solutions);
            var parameter = solution.Vars.Single(v => v.Name != "x");
            foreach (var point in new[] { 0.0, 1.0, -2.5 })
            {
                var candidate = solution.Substitute(parameter, point);
                while (candidate is Entity.Providedf(var inner, _)) candidate = inner;
                Assert.Equal(modulus,
                    MathS.Abs(candidate).EvalNumerical().RealPart.EDecimal.ToDouble(), 9);
            }
        }

        // A symbolic right-hand side cannot be decided either way, so the condition is
        // carried rather than resolved -- which is what #318 asked this inversion to do.
        [Fact]
        public void Issue812_ASymbolicRightHandSideCarriesTheCondition()
        {
            var printed = "abs(x) = a".ToEntity().Solve("x").Stringize();
            Assert.Contains("a >= 0", printed);
        }
        // https://github.com/asc-community/AngouriMath/issues/2425
        // Over the reals a positive base to a real power is positive, so e^x = t has a real
        // root only for a positive t. e^x + 1 = 0 was answered { ln(-1) }, which is i pi, and
        // e^x - 2e^(-x) - 1 = 0, a quadratic in e^x with the roots 2 and -1, kept ln(-1) beside
        // ln(2). Each root is checked to be real and to satisfy the equation, and the count says
        // that none was lost.
        [Theory]
        [InlineData("e^x + 1 = 0", 0)]
        [InlineData("3 e^x + 5 = 0", 0)]
        [InlineData("2^x = -3", 0)]
        [InlineData("e^(2x + 1) = -3", 0)]
        [InlineData("e^x - 2e^(-x) - 1 = 0", 1)]
        [InlineData("e^(2x) + e^x - 2 = 0", 1)]
        [InlineData("e^x - e^(-x) = 3", 1)]
        [InlineData("e^x + e^(-x) = 1", 0)]
        [InlineData("e^x + e^(-x) = 3", 2)]
        [InlineData("e^(2x) - 3e^x + 2 = 0", 2)]
        [InlineData("e^(x^2) = -1", 0)]
        [InlineData("e^(x^2) + e^(-x^2) = 1", 0)]
        // A replacement that is not real at a real x is solved over the complex numbers:
        // e^(i x) = -1 has the real root pi.
        [InlineData("e^(i x) = -1", 1)]
        public void Issue2425_AnExponentialEquationOverTheRealsHasOnlyRealRoots(string equation, int count)
        {
            using var _ = MathS.Settings.Codomain.Set(AngouriMath.Core.Domain.Real);
            var statement = (Entity.Equalsf)equation.ToEntity();
            var roots = Assert.IsType<Entity.Set.FiniteSet>(statement.Solve("x"));
            Assert.Equal(count, roots.Count);
            foreach (var root in roots)
            {
                var value = Assert.IsAssignableFrom<Entity.Number.Complex>(root.Evaled);
                Assert.True(value.ImaginaryPart.EDecimal.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1E-30")) < 0,
                    $"{root} = {value} is not real");
                var residual = Assert.IsAssignableFrom<Entity.Number.Complex>(
                    (statement.Left - statement.Right).Substitute("x", root).Evaled);
                Assert.True(residual.Abs().EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromString("1E-30")) < 0,
                    $"{root} leaves {residual}");
            }
        }

        // The complex reading is untouched: e^x + 1 = 0 has the root i pi there.
        [Fact]
        public void Issue2425_OverTheComplexNumbersTheRootIsKept()
        {
            var statement = (Entity.Equalsf)"e^x + 1 = 0".ToEntity();
            var root = Assert.Single(Assert.IsType<Entity.Set.FiniteSet>(statement.Solve("x")));
            var value = Assert.IsAssignableFrom<Entity.Number.Complex>(root.Evaled);
            Assert.Equal(System.Math.PI, value.ImaginaryPart.EDecimal.ToDouble(), 12);
        }
    }
}
