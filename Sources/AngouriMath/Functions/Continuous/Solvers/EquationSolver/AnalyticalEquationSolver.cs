//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Multithreading;
using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra;
using System.Linq.Expressions;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath
{
    partial record Entity : ILatexizeable
    {
        /// <summary>
        /// Attempt to find analytical roots of a custom equation.
        /// It solves the given expression assuming that it is
        /// equal to zero. No need to make it equal to 0 yourself;
        /// however, if you prefer so, consider using the .Solve()
        /// method instead
        /// </summary>
        /// <param name="x">
        /// The variable over which to solve the equation
        /// </param>
        /// <example>
        /// <code>
        /// Entity expr = "x + 8 - 4";
        /// Console.WriteLine(expr.SolveEquation("x"));
        /// </code>
        /// Will print "{ -4 }"
        /// </example>
        /// <returns>
        /// Returns <see cref="Set"/>
        /// </returns>
        public Set SolveEquation(Variable x) => EquationSolver.Solve(this, x);
    }
}

namespace AngouriMath.Functions
{
    internal static partial class TreeAnalyzer
    {
        /// <summary>
        /// Searches for a subtree containing `ent` and being minimal possible size.
        /// For example, for expr = MathS.Sqr(x) + 2 * (MathS.Sqr(x) + 3) the result
        /// will be MathS.Sqr(x) while for MathS.Sqr(x) + x the minimum subtree is x.
        /// Further, it will be used for solving with variable replacing, for example,
        /// there's no pattern for solving equation like sin(x)^2 + sin(x) + 1 = 0,
        /// but we can first solve t^2 + t + 1 = 0, and then root = sin(x).
        /// </summary>
        public static Entity GetMinimumSubtree(Entity expr, Variable x)
        {
            if (!expr.ContainsNode(x))
                throw new AngouriBugException($"{nameof(expr)} must contain {nameof(x)}");

            // The idea is the following:
            // We must get a subtree that has more occurances than 1,
            // But at the same time it should cover all references to `ent`
            var xs = expr.Nodes.Count(child => child == x);
            return
                expr.Nodes
                .TakeWhile(e => e != x) // Requires Entity enumeration to be depth-first!!
                .Where(e => e.ContainsNode(x)) // e.g. when expr is sin((x+1)^2)+3, this step results in [sin((x+1)^2)+3, sin((x+1)^2), (x+1)^2, x+1]
                .LastOrDefault(sub => expr.Nodes.Count(child => child == sub) * sub.Nodes.Count(child => child == x) == xs)
                // if `expr` contains 2 `sub`s and `sub` contains 3 `x`s, then there should be 6 `x`s in `expr` (6 == `xs`)
                ?? x;
        }
    }
}

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    internal static class AnalyticalEquationSolver
    {

        /// <summary>Equation solver</summary>
        /// <param name="compensateSolving">
        /// Compensate solving is needed when you formatted an equation to (something - const)
        /// and compensateSolving "compensates" this by applying expression inverter,
        /// aka compensating the equation formed by the previous solver
        /// </param>
        /// <param name="expr">Expression</param>
        /// <param name="x">Variable to solve over</param>
        internal static Set Solve(Entity expr, Variable x, bool compensateSolving = false)
            // An inversion that could not write its preimage leaves this equation unsolved, as
            // the set of x for which it holds. At this level and no further out, so the roots
            // found beside it stay: (x - 1) x! = 0 is { 1 } and the x with x! = 0.
            => SolveInverting(expr, x, compensateSolving)
               ?? new ConditionalSet(x, (compensateSolving ? expr : expr.InnerSimplified).Equalizes(0));

        /// <summary>
        /// The roots, or <see langword="null"/> where an inversion on the way has no written
        /// preimage.
        /// </summary>
        private static Set? SolveInverting(Entity expr, Variable x, bool compensateSolving)
        {
            if (!compensateSolving) expr = expr.InnerSimplified; // don't simplify away the 0 on the right hand side of the subtraction
            if (expr == x)
                return new Entity[] { 0 }.ToSet();

            // An equation that does not mention x at all has either no solutions or every
            // solution, and which of the two is decided by whether what is left is zero.
            // Where that is not decidable the honest answer is the condition itself: `a = b`
            // answered `{ }`, which asserts that no x satisfies it -- and every x does,
            // whenever a happens to equal b.
            // https://github.com/asc-community/AngouriMath/issues/278
            if (!compensateSolving && !expr.ContainsNode(x))
            {
                if (expr.Evaled is Complex evaluated)
                    return evaluated.IsZero ? (Set)MathS.Sets.C : Set.Empty;
                // `a - a` is zero and does not look it until the simplifier has been asked,
                // and answering the condition there would be as wrong as answering the empty
                // set -- it would say "provided a = a" where the answer is every x. Only an
                // equation with no x in it at all reaches this, so the simplification is paid
                // for once and never inside a loop.
                var residual = expr.Simplify();
                if (residual.Evaled is Complex simplified)
                    return simplified.IsZero ? (Set)MathS.Sets.C : Set.Empty;
                return new ConditionalSet(x, residual.Equalizes(0));
            }

            // Over the reals an equation in moduli of linear functions is solved by cases, between
            // the kinks: |x - 2| = |x - 3| is x = 5/2, Sullivan and Mackey's Prob 1.5.16.
            // https://github.com/asc-community/AngouriMath/issues/1573
            if (MathS.Settings.Codomain.Value == Core.Domain.Real && ModulusSolver.SolveOverTheReals(expr, x) is { } overTheReals)
                return overTheReals;

            // Over the complex numbers, one with x inside a modulus, a conjugate, a real or an
            // imaginary part and outside it as well is solved in the real and imaginary parts of
            // x, exactly: x |x| = 4 is { 2 }. Asked here, of the equation as it was asked, rather
            // than of each spelling below, every one of which would be the same system to solve.
            // https://github.com/asc-community/AngouriMath/issues/2139
            if (!compensateSolving && MathS.Settings.Codomain.Value != Core.Domain.Real
                && ModulusSolver.IsInsideAndOutsideAFunctionThatIsNotHolomorphic(expr, x)
                && ModulusSolver.SolveOverTheComplexNumbers(expr, x) is { } overTheComplexNumbers)
                return overTheComplexNumbers;

            // Whether a candidate root really is one. The loose tolerance that guesses the
            // rational must not also be what decides this: at 1e-7, x^41 + 6x + 1 accepted
            // -1/6, whose residual is -1.25e-32 -- small, but the difference between an
            // answer and a decoration (https://github.com/asc-community/AngouriMath/issues/235).
            // Where the residual comes out as an exact ratio, which is the case whenever the
            // equation and the candidate are both rational, it is required to be exactly zero.
            // Where it does not -- an equation carrying pi, say -- there is nothing to be exact
            // about and the ordinary tolerance still decides.
            static bool IsGenuineRoot(Complex residual)
                => residual is Rational ratio ? ratio.ERational.IsZero : IsZero(residual);

            // Applies an attempt to downcast roots
            static Entity TryDowncast(Entity equation, Variable x, Entity root)
            {
                if (root.Evaled is not Complex preciseValue)
                    return root;
                using var _ = MathS.Settings.FloatToRationalIterCount.Set(20);
                using var __ = MathS.Settings.PrecisionErrorZeroRange.Set(1e-7m);
                var downcasted = Complex.Create(preciseValue.RealPart, preciseValue.ImaginaryPart);
                if (equation.Substitute(x, downcasted).Evaled is not Complex error)
                    return root;
                return IsGenuineRoot(error) && downcasted.RealPart is Rational && downcasted.ImaginaryPart is Rational
                       ? downcasted : root.InnerSimplified;
            }

            MultithreadingFunctional.ExitIfCancelled();

            switch (expr)
            {
                case Minusf(var subtrahend, var minuend) when !minuend.ContainsNode(x) && compensateSolving:
                    if (subtrahend == x)
                        return new[] { minuend }.ToSet();
                    Entity? lastChild = null;
                    foreach (var child in subtrahend.DirectChildren)
                        if (child.ContainsNode(x))
                            if (lastChild is null)
                                lastChild = child;
                            else goto default;
                    if (lastChild is null)
                        goto default;
                    // TODO: optimize?
                    return subtrahend.Invert(minuend, lastChild)?.Select(result => Solve(lastChild - result, x, compensateSolving: true)).Unite();
                // A power is zero exactly where its base is, so f(x)^n = 0 has the roots of
                // f(x) = 0 and no others. Routing it that way rather than leaving it to the
                // replacement machinery below is what makes it answer *identically* to the
                // equation it is: without this (x^2 + x + 1)^2 = 0 is answered
                // { -1/2 + -sqrt(-3/4), -1/2 + sqrt(-3/4) } where x^2 + x + 1 = 0 gives
                // { (-1 - sqrt(-3))/2, (-1 + sqrt(-3))/2 }, and takes about four times as
                // long to get there. https://github.com/asc-community/AngouriMath/issues/744
                //
                // Only for an exponent that is a positive number and free of x. A negative
                // one is never zero, and one that moves with x is a different equation; both
                // are left to the inversion below, which is what has always answered them.
                case Powf(var @base, var power)
                    when @base.ContainsNode(x) && !power.ContainsNode(x)
                         && power.Evaled is Real { IsPositive: true }:
                    return Solve(@base, x, compensateSolving);
                // Inverting isolates x only where x occurs once, which is what Invert's own
                // documentation requires and what this case did not check. Given
                // sin(x^2 + x) = 0 it inverted the sine, then the sum, then the square, and
                // handed back { +-sqrt(2*pi*n_1 - x), ... } -- the equation rearranged rather
                // than solved, since the x left on the right stayed where it stood. Where the
                // variable occurs more than once, the replacement machinery below is what
                // answers: it solves sin(t) = 0 and then t = x^2 + x for each root, which is
                // how the same equation written cos(x^2 + x + 1) = 1 was answered correctly
                // all along -- a right-hand side of zero is the only reason this case was
                // reached at all. https://github.com/asc-community/AngouriMath/issues/744
                case Function when expr.Nodes.Count(node => node == x) == 1:
                    return expr.Invert(0, x)?.Select(ent => TryDowncast(expr, x, ent)).ToSet();
                // Solved knowing the condition, as a provided expression is differentiated: the
                // roots are filtered by it anyway, and what holds under it can be stated by it.
                // https://github.com/asc-community/AngouriMath/issues/212
                case Providedf(var expression, var predicate):
                    return TreeAnalyzer.Assuming(predicate, () => Solve(expression, x, compensateSolving)).Filter(predicate, x);
                case Piecewise p:
                    return EquationSolver.SolvePiecewise(p, x, (e, x) => Solve(e, x, compensateSolving));
                // A product is zero exactly where one of its factors is. Without this the
                // factors are expanded back into a polynomial and the equation is answered
                // by the general formula for whatever degree that turns out to be, so
                // (x - 1)(x^2 - 3) = 0 -- handed over already factored -- came back as two
                // nested cube roots of 26 + 18i rather than as 1 and +-sqrt(3).
                // https://github.com/asc-community/AngouriMath/issues/272
                //
                // A factor free of x contributes no roots. It could still be zero itself,
                // which would make every x a solution, but that is a reading the solver
                // does not take anywhere else either: a * (x^2 - 3) = 0 already answered
                // +-sqrt(3) by dividing a out, so nothing changes here.
                case Mulf(var left, var right) when left.ContainsNode(x) || right.ContainsNode(x):
                {
                    var factors = new List<Entity>(2);
                    if (left.ContainsNode(x)) factors.Add(left);
                    if (right.ContainsNode(x)) factors.Add(right);
                    var product = expr;
                    // Zeroing one factor makes the product zero only where the others are
                    // defined: x * (1/x) is undefined at 0, not zero there. This has to be
                    // checked here rather than left to the verification every solver shares,
                    // because that deliberately keeps a root whose residual does not
                    // evaluate to a number at all -- a root is only dropped on positive
                    // evidence, and NaN is not evidence (see SolveStatement.IsSpurious).
                    bool ProductIsDefinedAt(Entity root)
                        => root.Vars.Any()
                           || product.Substitute(x, root).Evaled is not Complex value
                           || value.IsFinite;
                    var fromFactors = factors.Select(factor => Solve(factor, x, compensateSolving)).Unite();
                    // Collapsed first: uniting sets builds a Unionf, and the roots cannot be
                    // looked at one at a time until it is a finite set again.
                    return fromFactors.InnerSimplified is FiniteSet found
                        ? found.Where(ProductIsDefinedAt).ToSet()
                        : fromFactors;
                }
                default:
                    break;
            }

            // The same equation written out as a sum. Where a rational root can be divided
            // out, what is left is a polynomial of lower degree, and the factors are then
            // solved one at a time by the case above -- which is how x^3 - x^2 - 3x + 3 is
            // answered exactly, and how a quintic is answered at all.
            // https://github.com/asc-community/AngouriMath/issues/272
            if (Functions.PolynomialFactoring.TrySplitOffRationalRoots(expr, x, out var split)
                && split is Mulf)
                return Solve(split, x, compensateSolving);

            // A polynomial with no rational root at all may still factor, and then each
            // factor is a lower-degree equation the product case above answers exactly.
            // x^5 + 2x^3 - 2x^2 - 4 is (x^2 + 2)(x^3 - 2): three of its five roots are
            // cube roots of two, which no search for a rational root can reach and which
            // the general machinery below reaches only numerically, if at all.
            // Degree four and up, and not two-termed -- see IsWorthFactoringToSolve for why
            // anything smaller has already been dealt with by the line above.
            // https://github.com/asc-community/AngouriMath/issues/746
            if (Functions.PolynomialFactorization.IsWorthFactoringToSolve(expr, x)
                && Functions.PolynomialFactorization.TryFactorIntoIrreducibles(expr, x, out var factored)
                && factored is Mulf)
                return Solve(factored, x, compensateSolving);

            if (PolynomialSolver.SolveAsPolynomial(expr, x, out var isIdentity) is { } poly)
                return poly.Select(e => TryDowncast(expr, x, e.InnerSimplified)).ToSet();
            if (isIdentity)
                return MathS.Sets.C;

            // A linear function times an exponential of another, by the Lambert W function: x e^x = 2
            // is lambertw(2), on every branch over the complex numbers. Answered with no roots only
            // where none is real. Ahead of the spellings and the exponential solvers, which reach
            // x 2^x = 5 through a decimal; it needs an exponential, so no polynomial is answered here,
            // and a factor linear in x, so a sum of exponentials is still theirs. https://github.com/asc-community/AngouriMath/issues/2182
            if (LambertWSolver.Solve(expr, x) is FiniteSet lambert)
                return (Set)lambert.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;

            // If the replacement isn't one-variable one,
            // then solving over replacements is already useless,
            // so we skip this part and go to other solvers
            if (!compensateSolving)
            {
                var newVar = Variable.CreateTemp(expr.Vars);
                // Here we find all possible replacements and find one that has at least one solution
                Set? unsettled = null;
                foreach (var alt in SpellingsToSolveOver(expr))
                {
                    MultithreadingFunctional.ExitIfCancelled();
                    if (!alt.ContainsNode(x))
                        // There are either 0 or +oo solutions, and which of the two it is
                        // is decided by whether what is left of the equation is zero.
                        return alt.Evaled is Complex { IsZero: true } ? MathS.Sets.C : Set.Empty;
                    var minimumSubtree = TreeAnalyzer.GetMinimumSubtree(alt, x);
                    if (minimumSubtree == x)
                        continue;
                    // Here we are trying to solve for this replacement
                    var solutionsSet = Solve(alt.Substitute(minimumSubtree, newVar), newVar).InnerSimplified;
                    if (solutionsSet is FiniteSet { IsSetEmpty: false } enums)
                    {
                        var solutions = enums.Select(solution => Solve(minimumSubtree - solution, x, compensateSolving: true)).Unite().InnerSimplified;
                        if (solutions is FiniteSet els)
                            return els.Select(ent => TryDowncast(expr, x, ent)).ToSet();
                        else if (solutions is Set { IsSetEmpty: false } set)
                            // A set that is not finite is an answer nothing settled -- a
                            // condition, or a union with one in it -- and another spelling
                            // may settle it: x^4 * x^y - 2 as written comes back as a
                            // condition, and the spelling x^(4 + y) - 2 comes back as a root.
                            // Kept in case no spelling does better.
                            unsettled ??= set;
                    }
                }
                if (unsettled is { } condition)
                    return condition;
                // // //
            }

            // An equation in both sin(u) and cos(u), where one of them appears only at even
            // powers, is a polynomial in the other once cos^2 is written as 1 - sin^2 -- and
            // the replacement machinery above then solves it as one. This has to come before
            // the trigonometric solver, which writes both in terms of e^(i u) and so answers
            // a quadratic in sin(a x) with a quartic in e^(i a x).
            // https://github.com/asc-community/AngouriMath/issues/270
            // Solved without compensation, whatever this call was given. The rewrite keeps
            // the whole left-hand side of `... = 0` rather than peeling a constant off it,
            // so there is nothing to compensate -- and compensating skips the replacement
            // machinery, which is the only thing that can solve what the rewrite produces.
            // Passed `true`, this fired, produced `1 - sin(x)^2 + sin(x)` and then answered
            // it through the exponential solver anyway, which is the route it exists to
            // avoid. It cannot recurse: what comes back mentions only one of the two
            // functions, so the rewrite declines it.
            if (TrigonometricSolver.TryRewriteInOneFunction(expr, x, out var inOneFunction)
                && Solve(inOneFunction, x) is FiniteSet { IsSetEmpty: false } elsPythagorean)
                return (Set)elsPythagorean.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;

            // if no replacement worked, try trigonometric solver
            if (TrigonometricSolver.TrySolveLinear(expr, x, out var trig) && trig is FiniteSet elsTrig)
                return (Set)elsTrig.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            MultithreadingFunctional.ExitIfCancelled();

            // Two powers of numeric bases, by logarithms -- before the multiplicative solver,
            // which reaches the same equations by a substitution that goes through a decimal.
            // https://github.com/asc-community/AngouriMath/issues/1007
            if (ExponentialSolver.SolveTwoPowersOfNumericBases(expr, x) is { } expTwo
                && expTwo is FiniteSet elsExpTwo && elsExpTwo.Any())
                return (Set)elsExpTwo.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            // if no trigonometric rules helped, try exponential-multiplicative solver
            if (ExponentialSolver.SolveMultiplicative(expr, x) is { } expMul && expMul is FiniteSet elsExpMul)
                return (Set)elsExpMul.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            // if no exponential-multiplicative rules helped, try exponential-linear solver
            if (ExponentialSolver.SolveLinear(expr, x) is { } expLin && expLin is FiniteSet elsExpLin)
                return (Set)elsExpLin.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            // if no exponential-linear rules helped, common denominator might help
            if (CommonDenominatorSolver.TrySolveGCD(expr, x, out var commonDenom) && commonDenom is FiniteSet elsCd)
                return (Set)elsCd.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            // if we have fractioned polynomials
            if (FractionedPolynoms.TrySolve(expr, x, out var fractioned) && fractioned is FiniteSet elsFracs)
                return (Set)elsFracs.Select(ent => TryDowncast(expr, x, ent)).ToSet().InnerSimplified;
            // // //

            // TODO: Solve factorials (Needs Lambert W function)
            // https://mathoverflow.net/a/28977

            // An identity is satisfied by every x, and not by the points a numerical search
            // starts from: `sqrt(x)^2 - x` is zero wherever it is evaluated, so every start of
            // Newton's grid converged at once and each was kept, two hundred numbers for an
            // equation every number satisfies. Asked of the simplifier here, where every
            // analytical route has declined, since it is a whole simplification; and only an
            // unconditional zero, since `(x^2 - 1)/(x - 1) - x - 1` is zero for every x but 1.
            // https://github.com/asc-community/AngouriMath/issues/1420
            if (expr.Vars.Count == 1 && expr.Simplify() is Integer { IsZero: true })
                return MathS.Sets.C;

            // An expression real for every complex x -- x only ever inside a modulus -- is one
            // real condition on the two real unknowns x is made of, so what solves it is a
            // curve: |x - 2| = |x - 3| is the line Re x = 5/2. Newton's method would find points
            // of it and answer them as the set. Left unsolved; over the reals it is solved by
            // cases, above. https://github.com/asc-community/AngouriMath/issues/1573
            if (ModulusSolver.IsRealForEveryComplex(expr, x))
                return Unsolved(expr, x);

            // Over the complex numbers, x inside a modulus or another function that is not
            // holomorphic and outside it as well is not the search's either: Newton's method
            // assumes a holomorphic function, and its near-copies of a root came back as roots.
            // Left unsolved where it is not solved in the parts of x, above; over the reals it is
            // solved by cases. https://github.com/asc-community/AngouriMath/issues/2139
            if (MathS.Settings.Codomain.Value != Core.Domain.Real && ModulusSolver.IsInsideAndOutsideAFunctionThatIsNotHolomorphic(expr, x))
                return Unsolved(expr, x);

            // A numerical search that comes back with nothing has not shown there is
            // nothing to find: Newton's method is started from finitely many points inside
            // a bounded region, so an empty result is a fact about the search rather than
            // about the equation. Where it does find roots the answer is theirs, unchanged.
            // And one the compiler has no form for -- `floor(x)`, or the derivative `max(x, 1)`
            // has -- is not the search's to run, and is left unsolved rather than thrown out of
            // `Solve`. https://github.com/asc-community/AngouriMath/issues/1603
            if (MathS.Settings.AllowNewton && expr.Vars.Count == 1)
                return NumericalSolving.NewtonSolver.TrySolveNt(expr, x, MathS.Settings.NewtonSolver) is { } roots
                       && roots.Select(ent => TryDowncast(expr, x, ent)).ToSet() is FiniteSet { IsSetEmpty: false } found
                    ? found : Unsolved(expr, x);

            return Unsolved(expr, x);
        }

        /// <summary>
        /// The spellings of an equation the replacement machinery tries, in order: the
        /// equation as it stands, and then every alternative the simplifier can offer.
        /// </summary>
        /// <remarks>
        /// The alternatives are built only if they are reached. <see cref="Entity.Alternate"/>
        /// is a whole level-4 simplification of the equation -- the same search
        /// <see cref="Entity.Simplify(int)"/> runs, sorted -- and the replacement loop asked
        /// for it before trying anything, at every depth of its own recursion, when in the
        /// ordinary case the equation as written is the spelling that solves. On SolveHard
        /// that search was 268 MB of the top-level call and a third of each level below it,
        /// to produce a first candidate the loop then solved and never looked past.
        /// https://github.com/asc-community/AngouriMath/issues/746
        /// </remarks>
        private static IEnumerable<Entity> SpellingsToSolveOver(Entity expr)
        {
            yield return expr;
            foreach (var alt in expr.Alternate(4))
                if (alt != expr)
                    yield return alt;
        }

        /// <summary>
        /// The answer when every solver above has declined: the equation itself, as the set
        /// of the <paramref name="x"/> that satisfy it.
        /// </summary>
        /// <remarks>
        /// The empty set is a positive claim -- that no <paramref name="x"/> satisfies
        /// <paramref name="equation"/> -- and returning it from an exhausted search asserts
        /// something nothing here established. <c>x^6 + x*y + 1 = 0</c> has six roots for
        /// every <c>y</c> and was answered <c>{ }</c>. A condition asserts only what was
        /// established, which is that these are the roots, whichever they are; the set is
        /// still exactly the solution set, so nothing downstream is told anything false.
        ///
        /// The spelling is the one <see cref="StatementSolver.UnsolvedWhereIndependenceIsDenied"/>
        /// and the <a href="https://github.com/asc-community/AngouriMath/issues/278">#278</a>
        /// case above already use, rather than a second way of saying the same thing.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1036">#1036</a>,
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
        /// </remarks>
        private static Set Unsolved(Entity equation, Variable x)
            // Written back as an equation between the two sides where there are two, rather
            // than as the residual the solvers pass around. A compensated call is handed its
            // equation unsimplified on purpose, so the difference survives for them to read,
            // and stating it as it stands leaves the reader `x^5 + 3*x - (-1) = 0` where the
            // question was whether `x^5 + 3*x` is -1. The two admit the same x.
            => new ConditionalSet(x, equation is Minusf(var lhs, var rhs)
                ? lhs.Equalizes(rhs)
                : equation.Equalizes(0));
    }
}
