//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using PeterO.Numbers;
using AngouriMath.Functions.Continuous.Solvers.SetSolver;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    internal static class StatementSolver
    {
        private static Entity Minus(Entity left, Entity right)
        {
            if (left.Evaled == 0)
                return -right;
            if (right.Evaled == 0)
                return left;
            return left - right;
        }

        /// <summary>
        /// Substitutes each root back into the equation it came from and drops the ones
        /// that do not satisfy it.
        /// </summary>
        /// <remarks>
        /// Several of the rewrites the solvers rely on widen the domain of the equation.
        /// <c>ln(a) + ln(b) = ln(a * b)</c> is the plainest: solving
        /// <c>ln(x) + ln(x+1) = 0</c> goes through <c>x^2 + x - 1 = 0</c> and hands back
        /// both of its roots, but at -1.618... the original is 2*pi*i, not 0. The
        /// individual rewrites cannot always carry a condition that survives the chain of
        /// substitutions that follows, so the answers are checked once, here, against the
        /// equation as the caller wrote it.
        /// <para>
        /// A root with only exact numbers in it that the check keeps, and that an interval enclosing
        /// its residual shows is none, leaves the equation unsolved; see
        /// <see cref="RootVerdict.ShownNotARoot"/>.
        /// </para>
        /// </remarks>
        internal static Set WithoutSpuriousRoots(Set roots, Entity equation, Variable x)
        {
            if (roots is not FiniteSet finite)
                return roots;
            List<Entity>? survivors = null;
            var index = 0;
            foreach (var root in finite)
            {
                switch (Verdict(equation, x, root))
                {
                    case RootVerdict.ShownNotARoot:
                        return Unsolved(equation, x);
                    case RootVerdict.Spurious:
                        survivors ??= finite.Take(index).ToList();
                        break;
                    default:
                        survivors?.Add(root);
                        break;
                }
                index++;
            }
            if (survivors is null)
                return roots;
            // An equation that is a polynomial as written has no root a rewrite could have added, so a
            // candidate that fails it is the solver's number, not a spurious root, and the roots that
            // pass are not known to be all of them. Where every candidate fails, the empty set left
            // would claim there is no root, which a polynomial of degree one or more always has over
            // the complex numbers: (x + 1)^3 = 2 was { } while Cardano's C was 0 for a shifted cube,
            // and 31x^4 - 62x^3 + 33x^2 - 2x - 1 = 0 while a biquadratic quartic was shifted by
            // -b a / 4. And where some pass, they are an answer with roots missing: a degree-twelve
            // polynomial with roots near 1e-12 had eleven numerical candidates fail and was answered
            // with the twelfth alone. Left unsolved, which says only that the roots were not found.
            if (IsAPolynomialWithARoot(equation, x))
                return Unsolved(equation, x);
            return survivors.ToSet();
        }

        /// <summary><paramref name="equation"/> as a condition on <paramref name="x"/>: the roots were not found.</summary>
        private static Set Unsolved(Entity equation, Variable x)
            => new ConditionalSet(x, equation is Minusf(var left, var right) ? left.Equalizes(right) : equation.Equalizes(0));

        /// <summary>
        /// Whether <paramref name="equation"/> is a polynomial in <paramref name="x"/> with numbers
        /// for coefficients and a degree of one or more, read over the complex numbers, where such a
        /// polynomial has a root.
        /// </summary>
        private static bool IsAPolynomialWithARoot(Entity equation, Variable x)
            // Read through a condition the equation carries, as a polynomial's does once simplified.
            => MathS.Settings.Codomain.Value != Core.Domain.Real
                && TreeAnalyzer.TryGetPolynomial(Functions.PartialFractions.Bare(equation), x, out var monomials)
                && monomials.Keys.All(power => power.Sign >= 0)
                && monomials.Where(monomial => monomial.Key.Sign > 0)
                    .Any(monomial => monomial.Value.Evaled is Number.Complex { IsZero: false })
                && monomials.Values.All(coefficient => coefficient.Evaled is Number.Complex { IsFinite: true });

        /// <summary>
        /// Answers the condition itself where a root denies the independence the calculus
        /// operators in the equation were evaluated under.
        /// </summary>
        /// <remarks>
        /// <c>derivative(y, x) + y - x</c> was answered <c>{ x }</c>: the derivative went to
        /// zero because <c>y</c> is not <c>x</c>, and the root then says that it is. Putting
        /// it back gives <c>derivative(x, x) + x - x</c>, which is 1 — so the set named a
        /// member that is not a root. A root free of that name is untouched, because nothing
        /// was assumed that it goes on to deny: <c>derivative(y * x, x) + y - 1</c> is still
        /// <c>{ 1/2 }</c>.
        ///
        /// The equation is not thereby unsatisfiable, so the empty set would be a second
        /// false claim in place of the first. What holds is the condition as written, and
        /// solving it needs a differential-equation solver this library does not have.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/964">#964</a>,
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
        /// </remarks>
        internal static Set UnsolvedWhereIndependenceIsDenied(Set roots, Entity condition, Variable x)
        {
            if (roots is not FiniteSet finite)
                return roots;
            var assumed = CalculusOperator.NamesAssumedFreeOf(condition, x);
            return assumed.Count > 0 && finite.Any(root => assumed.Any(root.ContainsNode))
                ? new ConditionalSet(x, condition)
                : roots;
        }

        /// <summary>
        /// How large the residual has to be next to the terms it is the sum of before it
        /// counts as evidence against a root.
        /// </summary>
        /// <remarks>
        /// A residual that is merely non-zero is not evidence. A root found numerically is
        /// only as accurate as the search that found it, and the terms of the equation at
        /// such a root cancel to within that accuracy rather than exactly. Judging the
        /// residual on its own size threw away every root of
        /// <c>1/210 - 17x/210 + 101x^2/210 - 247x^3/210 + x^4</c>, whose four roots come
        /// back as decimals and leave 5.2e-6 against terms of about 1.2e-2, and answered
        /// that quartic with the empty set.
        ///
        /// The two cases are far apart, which is what makes a threshold possible at all.
        /// A spurious root is not a near miss: it comes from a rewrite that widened the
        /// domain, so the residual is a whole quantity in its own right -- 2*pi*i for the
        /// logarithm sum, or the difference between -3 and 9 for an exponential. Over the
        /// equations measured, every genuine root sits at 4.5e-4 or below and most at
        /// 1e-15, while every spurious one sits between 1.5 and 2. This is a hundredth:
        /// twenty times above the worst genuine root and a hundred and fifty times below
        /// the mildest spurious one.
        ///
        /// It is a bound for roots found numerically. An equation and a root that are exact numbers
        /// are held to the working precision instead, in <see cref="IsEvidenceAgainst"/>.
        /// </remarks>
        [ConstantField] private static readonly EDecimal RelativeResidualTolerance = EDecimal.FromString("0.01");

        /// <summary>
        /// <paramref name="answer"/> without the real points at which <paramref name="expr"/>, as
        /// written, has no value because a denominator in it is zero; <paramref name="answer"/> as
        /// it is where a denominator's zeros are not found as finitely many numbers.
        /// </summary>
        /// <remarks>
        /// The inequality solver reads the expression after a factor common to a numerator and a
        /// denominator has cancelled, so its answer had the points where that factor is zero, at
        /// which the quotient is 0/0 and has no value: (4x - x^3)/x > 0 was (-2; 2), with 0 in it,
        /// and x/x > 0 was RR. A pole that has not cancelled is outside the answer already, since a
        /// quotient is solved as the product of its numerator and denominator.
        /// </remarks>
        private static Set? WithoutPoles(Set? answer, Entity expr, Variable x)
        {
            if (answer is null)
                return null;
            var poles = new List<Entity>();
            foreach (var node in expr.Nodes)
            {
                var denominator = node switch
                {
                    Divf(_, var divisor) => divisor,
                    Powf(var @base, Integer { EInteger.Sign: < 0 }) => @base,
                    _ => null,
                };
                if (denominator is null || !denominator.ContainsNode(x))
                    continue;
                if (AnalyticalEquationSolver.Solve(denominator, x) is not FiniteSet zeros || zeros.Any(zero => zero.Vars.Any()))
                    return answer;
                poles.AddRange(zeros.Where(zero => zero.Evaled is Number.Real));
            }
            return poles.Count == 0 ? answer : (Set)MathS.SetSubtraction(answer, new FiniteSet(poles)).InnerSimplified;
        }

        /// <summary>
        /// Whether a root demonstrably fails the equation. Anything that cannot be
        /// evaluated to a number -- a root still carrying a symbol of the equation's, or one
        /// whose residual is not finite -- is kept, so that a root is only ever dropped on
        /// positive evidence against it. A family, <c>pi + 2 * pi * n_1</c>, is judged by a member. A function
        /// undefined at exact arguments is such evidence; see <see cref="IsUndefinedExactly"/>.
        /// </summary>
        private static RootVerdict Verdict(Entity equation, Variable x, Entity root)
        {
            // A root that still mentions the variable it solves for is the equation
            // rearranged, not an answer to it: x = sqrt(-1 - x) says nothing about x. This
            // is decided before the parameter exemption below, which would otherwise keep
            // such a root unconditionally -- it carries a variable, so substituting it back
            // leaves an expression rather than a residual, and there is no evidence to drop
            // it on. That is how (x^2 + x + 1)^2 = 0 came to be answered with two of them.
            //
            // No equation reaches this today: the routes that produced such a root are
            // fixed at their source in AnalyticalEquationSolver, which is what makes those
            // equations answer correctly rather than emptily. It is kept because Invert
            // requires the variable to occur once and only one of its five callers checks
            // that, so the next caller to forget is caught here rather than in a bug report.
            // https://github.com/asc-community/AngouriMath/issues/744
            if (root.ContainsNode(x))
                return RootVerdict.Spurious;
            // A family of roots, whose parameters are whole numbers, is judged by one of its
            // members, as the principal root alone was before the family was written: a member
            // that fails is evidence against the family, and one that passes proves nothing
            // about the rest, so the family is kept. https://github.com/asc-community/AngouriMath/issues/2319
            if (root.Vars.Any())
                return SolutionParameters.AtAMember(root) is { } member && !member.Vars.Any() && Verdict(equation, x, member) is var verdict and not RootVerdict.Kept
                    ? verdict
                    : RootVerdict.Kept;
            var substituted = equation.Substitute(x, root);
            if (IsUndefinedExactly(substituted))
                return RootVerdict.Spurious;
            if (IsEvidenceAgainstUnlessEnclosed(substituted))
                return RootVerdict.Spurious;
            return !substituted.Vars.Any() && IsAboveTheIntervalFloor(equation, x, root) && IsShownNotZero(substituted)
                ? RootVerdict.ShownNotARoot
                : RootVerdict.Kept;
        }

        /// <summary>What <see cref="WithoutSpuriousRoots"/> does with a root.</summary>
        private enum RootVerdict
        {
            /// <summary>Kept: nothing shows it is not a root.</summary>
            Kept,
            /// <summary>Dropped: it came of a rewrite that widened the domain, and fails the equation as written.</summary>
            Spurious,
            /// <summary>
            /// Declines the equation. The root has only exact numbers in it, so it is a root only
            /// where its residual is exactly zero, and an interval enclosing the residual leaves
            /// zero out, however small the residual is next to the terms. That the check above
            /// passed it means rounding reached the solve, which can have lost a true root as well
            /// as made this one, so it is not dropped, which would answer an incomplete set. In
            /// <c>x^2 + (P - Q sqrt(2)) x - 3</c>, with <c>(1 + sqrt(2))^70 = P + Q sqrt(2)</c>,
            /// downcasting reads <c>b^2 + 12</c> as <c>12</c>, and the roots came back as
            /// <c>(-b +- 2 sqrt(3)) / 2</c>, each leaving <c>-b^2 / 4</c>, about <c>-6.4e-55</c>, against
            /// terms near <c>3</c>. Rounding reaches an exact solve through downcasting, which v3
            /// removes; the decline is to be reconsidered then, and the check kept.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/2448">#2448</a>,
            /// <a href="https://github.com/asc-community/AngouriMath/issues/1019#issuecomment-5754734225">#1019</a>
            /// </summary>
            ShownNotARoot,
        }

        /// <summary>Whether <paramref name="entity"/> has no number in it that is not exact.</summary>
        internal static bool IsExact(Entity entity)
            => !entity.Nodes.Any(node => node is Number { IsExact: false });

        /// <summary>
        /// Whether an interval enclosing <paramref name="residual"/>, a number with no symbol in
        /// it, leaves out zero. Interval arithmetic rounds each bound outward, so that is a proof
        /// and not an estimate; an interval that holds zero, or that cannot be worked out, proves
        /// nothing either way.
        /// </summary>
        /// <remarks>
        /// To as many decimal places as evaluation works to significant digits: a residual under
        /// that is what the tolerance takes for zero whatever the terms are, and one that
        /// evaluation took for zero above it is under about half of the digits next to its terms.
        /// The digits are significant ones, so where the terms are large the first interval is wider than that,
        /// and it is worked out once more with the digits the terms take besides: with
        /// <c>(1 + sqrt(2))^140</c> in the coefficients the terms are near <c>1e107</c>, and the
        /// residual <c>-2</c> is inside an interval of a hundred digits.
        /// </remarks>
        internal static bool IsShownNotZero(Entity residual) => ExcludesZero(residual) is true;

        /// <summary>
        /// Whether an interval enclosing <paramref name="residual"/> leaves out zero, as
        /// <see cref="IsShownNotZero"/> works it out; <see langword="false"/> where it holds zero,
        /// and <see langword="null"/> where no interval can be worked out.
        /// </summary>
        private static bool? ExcludesZero(Entity residual)
        {
            var digits = IntervalDigits();
            if (Numerics.PreciseEvaluation.Of(residual, digits) is not { } enclosure)
                return null;
            if (Excludes(enclosure))
                return true;
            var width = PeterO.Numbers.EDecimal.Max(
                enclosure.Re.High.Subtract(enclosure.Re.Low), enclosure.Im.High.Subtract(enclosure.Im.Low));
            if (width.IsZero)
                return false;
            // The decimal exponent of the width: how many more digits put it under 10^-digits.
            // Ten or fewer are what rounding leaves of terms near one, which is every residual whose
            // terms are not large, and are not worth a second evaluation.
            var magnitude = width.Exponent.Add(width.UnsignedMantissa.GetDigitCountAsEInteger()).Subtract(1);
            var more = magnitude.Add(digits);
            if (more.CompareTo(10) <= 0)
                return false;
            if (!more.CanFitInInt32() || more.ToInt32Unchecked() > MostMoreDigits)
                more = MostMoreDigits;
            return Numerics.PreciseEvaluation.Of(residual, digits + more.ToInt32Unchecked()) is { } finer && Excludes(finer);

            static bool Excludes(Numerics.PreciseComplexInterval enclosure)
                => enclosure.Re.Mignitude.Sign > 0 || enclosure.Im.Mignitude.Sign > 0;
        }

        /// <summary>
        /// The decimal places <see cref="IsShownNotZero"/> first encloses a residual to: as many as
        /// evaluation works to significant digits.
        /// </summary>
        private static int IntervalDigits()
        {
            var precision = MathS.Settings.DecimalPrecisionContext.Value.Precision;
            return precision.IsZero || !precision.CanFitInInt32() ? 100 : System.Math.Max(precision.ToInt32Unchecked(), 30);
        }

        /// <summary>
        /// Whether <paramref name="root"/> and <paramref name="equation"/> have only exact numbers in
        /// them, and the residual, evaluated without downcasting, is further from zero than rounding
        /// leaves of a true root's: only such a residual is enclosed by <see cref="IsShownNotZero"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The evaluation is what keeps the common case cheap. An interval evaluation of each exact
        /// root was twice the allocation of solving <c>x^2 + x + 1</c>, whose roots leave <c>0</c>
        /// here and are not enclosed; this costs about a tenth of that.
        /// </para>
        /// <para>
        /// Without downcasting, because downcasting is what made the roots wrong, and it reads what
        /// such a root leaves as zero too: <c>-b^2 / 4</c> evaluates to <c>0</c> with it, and to
        /// <c>-6.4e-55</c> without. A fresh tree, since <see cref="Entity.Evaled"/> keeps a value
        /// with the precision it was computed at and not with this setting. With the digits of the
        /// largest number in the tree besides, where those are more than ten: at
        /// <c>(1 + sqrt(2))^140 = P + Q sqrt(2)</c>, <c>P - Q sqrt(2)</c> is <c>2.6e-54</c> with
        /// <c>P</c> near <c>1.9e53</c>, which takes 108 digits to tell from zero.
        /// </para>
        /// </remarks>
        private static bool IsAboveTheIntervalFloor(Entity equation, Variable x, Entity root)
        {
            var substituted = equation.Substitute(x, root);
            var mostDigits = 0;
            foreach (var node in substituted.Nodes)
                switch (node)
                {
                    case Number { IsExact: false }:
                        return false;
                    case Rational rational:
                        mostDigits = System.Math.Max(mostDigits, DigitCount(rational));
                        break;
                    case Complex { RealPart: Rational re, ImaginaryPart: Rational im }:
                        mostDigits = System.Math.Max(mostDigits, System.Math.Max(DigitCount(re), DigitCount(im)));
                        break;
                }
            var digits = IntervalDigits();
            Entity value;
            using (MathS.Settings.DowncastingEnabled.Set(false))
                if (mostDigits > 10)
                    using (MathS.Settings.DecimalPrecisionContext.Set(
                        MathS.Settings.DecimalPrecisionContext.Value.WithPrecision(digits + System.Math.Min(mostDigits, MostMoreDigits))))
                        value = substituted.Evaled;
                else
                    value = substituted.Evaled;
            if (value is not Complex residual)
                return false;
            var floor = EDecimal.Create(EInteger.One, EInteger.FromInt32(TrueRootFloorDigits - digits));
            return residual.RealPart.EDecimal.Abs().GreaterThan(floor) || residual.ImaginaryPart.EDecimal.Abs().GreaterThan(floor);

            static int DigitCount(Rational rational)
            {
                var count = EInteger.Max(rational.ERational.Numerator.Abs().GetDigitCountAsEInteger(),
                    rational.ERational.Denominator.GetDigitCountAsEInteger());
                return count.CanFitInInt32() ? count.ToInt32Unchecked() : int.MaxValue;
            }
        }

        /// <summary>
        /// How many of <see cref="IntervalDigits"/> rounding takes from a true root's residual,
        /// by <see cref="IsAboveTheIntervalFloor"/>: the roots of <c>x^2 + 3x - 5</c> leave
        /// <c>1e-98</c> at a hundred digits.
        /// </summary>
        private const int TrueRootFloorDigits = 5;

        /// <summary>
        /// The most digits <see cref="IsShownNotZero"/> adds for large terms, which bounds what one
        /// residual costs: a thousand digits is terms past <c>1e900</c>.
        /// </summary>
        private const int MostMoreDigits = 1000;

        /// <summary>
        /// Whether <paramref name="substituted"/>, with no symbol left in it, has no value because
        /// some function in it is undefined at its arguments: a node whose arguments are all exact
        /// numbers and whose own value is not a finite number. <c>x^i + x</c> at 0 is one, since
        /// <c>0^i</c> is undefined, so 0 is not a root of <c>x^i + x = 0</c> however small the
        /// compiled function Newton's method iterated on is there.
        /// </summary>
        /// <remarks>
        /// A residual that is not finite is otherwise no evidence against a root: a root found in
        /// decimals carries rounding, and a function taken at it next to the edge of its domain, or
        /// a term that overflows, may have no value where the root itself gives one. Exact arguments
        /// have neither to come from. https://github.com/asc-community/AngouriMath/issues/2424
        /// </remarks>
        internal static bool IsUndefinedExactly(Entity substituted)
        {
            static bool IsExact(Entity argument)
                => argument.Evaled is Complex value && value.RealPart is Rational && value.ImaginaryPart is Rational;
            foreach (var node in substituted.Nodes)
                if (node.DirectChildren.Count > 0 && node.DirectChildren.All(IsExact)
                    && node.Evaled is Complex { IsFinite: false })
                    return true;
            return false;
        }

        /// <summary>
        /// Whether <paramref name="substituted"/>, an equation with what was found for its
        /// unknowns put in and no symbol left, shows that what was put in is not a solution:
        /// it evaluates to a number, and one that is large next to the terms it is the sum of.
        /// See <see cref="RelativeResidualTolerance"/> for why the size alone is not enough.
        /// </summary>
        internal static bool IsEvidenceAgainst(Entity substituted)
        {
            if (substituted.Evaled is not Number.Complex residual || !residual.IsFinite)
                return false;
            var size = residual.Abs().EDecimal;
            // Exact numbers in sums, products, quotients and rational powers are evaluated to the
            // working precision, so what they leave is either what rounding leaves of zero or a whole
            // quantity, and a percent is no bound for it. (3x - 4) sqrt(x) + 3 + 3x - 4x^2 = 0, raised
            // to a quartic, has a root of the quartic in radicals at 1.3233 that leaves 0.069 against
            // terms of 7, which a percent of them kept.
            if (IsExactAndAlgebraic(substituted))
            {
                var share = EDecimal.Create(EInteger.One, MathS.Settings.DecimalPrecisionContext.Value.Precision.Divide(2).Negate());
                // No more than share^2, which a true root usually leaves, is under the bound whatever
                // the terms are, so they are not evaluated again for their size: that cost nearly a
                // tenth more allocation on solving x^2 + x + 1.
                if (size.CompareTo(share.Multiply(share)) <= 0)
                    return false;
                var unused = false;
                return size.GreaterThan(EDecimal.Max(LargestTerm(substituted, ref unused), share).Multiply(share));
            }
            var cancels = false;
            var largest = LargestTerm(substituted, ref cancels);
            // Where nothing cancels, as in an equation that is one function's value, the residual
            // is the only term and its size against itself says nothing, so its size alone is
            // read. Where terms cancel the comparison is with them and nothing else: an absolute
            // floor took a residual of 3.46e-12 against terms of 2e-12 for none, so the equation
            // times 10^-12 kept two candidates that cubing it had added.
            if (!cancels)
                return !size.LessThan(MathS.Settings.PrecisionErrorCommon);
            return size.GreaterThan(largest.Multiply(RelativeResidualTolerance));
        }

        /// <summary>
        /// <see cref="IsEvidenceAgainst"/>, weighed against an interval where
        /// <paramref name="substituted"/> is exact numbers in sums, products, quotients and rational
        /// powers, whose enclosures are tight: one that holds zero says the residual is what
        /// rounding left, and is no evidence.
        /// </summary>
        /// <remarks>
        /// The small root of <c>x^2 + b x - 3</c>, with <c>b = (1 + sqrt(2))^70</c> near
        /// <c>6e26</c>, is <c>-b/2 + sqrt(b^2 + 12)/2</c>, and at a hundred digits that difference
        /// keeps 46 digits of the root's <c>4.8e-27</c>, which leaves <c>5.9e-48</c> against terms
        /// of <c>3</c>: over the bound for exact numbers, and rounding.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2448">#2448</a>
        /// </remarks>
        internal static bool IsEvidenceAgainstUnlessEnclosed(Entity substituted)
            => IsEvidenceAgainst(substituted) && !(IsExactAndAlgebraic(substituted) && ExcludesZero(substituted) is false);

        /// <summary>
        /// Whether <paramref name="expression"/> is exact numbers in sums, products, quotients and
        /// rational powers, which evaluation carries to the working precision. A function, a constant
        /// such as <c>pi</c> or a decimal is not: a special function may be evaluated in doubles, and
        /// a decimal is only as accurate as what found it.
        /// </summary>
        /// <remarks>
        /// Walked by hand rather than through <see cref="Entity.Nodes"/>: the tree is a fresh
        /// substitution, so the lazy enumerators behind that are built anew for every root checked,
        /// and they were a percent more allocation on solving <c>x^2 + x + 1</c>.
        /// </remarks>
        private static bool IsExactAndAlgebraic(Entity expression)
            => expression switch
            {
                Complex number => number.RealPart is Rational && number.ImaginaryPart is Rational,
                Sumf(var augend, var addend) => IsExactAndAlgebraic(augend) && IsExactAndAlgebraic(addend),
                Minusf(var minuend, var subtrahend) => IsExactAndAlgebraic(minuend) && IsExactAndAlgebraic(subtrahend),
                Mulf(var multiplier, var multiplicand) => IsExactAndAlgebraic(multiplier) && IsExactAndAlgebraic(multiplicand),
                Divf(var dividend, var divisor) => IsExactAndAlgebraic(dividend) && IsExactAndAlgebraic(divisor),
                Powf(var @base, Rational) => IsExactAndAlgebraic(@base),
                _ => false,
            };

        /// <summary>
        /// The largest of the terms the equation adds up to at the root, which is the scale
        /// the residual has to be read against, and in <paramref name="cancels"/> whether there
        /// are terms at all: whether a sum was split.
        /// </summary>
        /// <remarks>
        /// A product or a quotient by a number is multiplied out first. The terms of
        /// <c>(sin(x) - x) 10^-12</c> are <c>sin(x) 10^-12</c> and <c>x 10^-12</c>; read as one
        /// term, it was its own scale, and every root Newton's method found for it was thrown out
        /// at <c>10^12</c>, where its residual is past the floor.
        /// </remarks>
        private static EDecimal LargestTerm(Entity substituted, ref bool cancels)
        {
            switch (substituted)
            {
                case Sumf or Minusf:
                    cancels = true;
                    var largest = EDecimal.Zero;
                    foreach (var term in Sumf.LinearChildren(substituted))
                        largest = EDecimal.Max(largest, LargestTerm(term, ref cancels));
                    return largest;
                case Mulf(var multiplier, var multiplicand):
                    return LargestTerm(multiplier, ref cancels).Multiply(LargestTerm(multiplicand, ref cancels));
                case Divf(var dividend, var divisor) when divisor.Evaled is Number.Complex { IsFinite: true } value
                                                          && !value.Abs().EDecimal.IsZero:
                    return LargestTerm(dividend, ref cancels).Divide(value.Abs().EDecimal, MathS.Settings.DecimalPrecisionContext);
                default:
                    return substituted.Evaled is Number.Complex { IsFinite: true } number ? number.Abs().EDecimal : EDecimal.Zero;
            }
        }

        /// <summary>
        /// What <c>not a</c> is as a statement about <paramref name="x"/>: the negation pushed
        /// inward as far as there is an arm for it, and named as a set-builder where there is not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// There was no arm for <see cref="Notf"/> at all, so every negation fell to
        /// <see cref="Set.Empty"/> — <c>not (x = 1)</c>, <c>not (x &gt; 1)</c> and
        /// <c>not (x in RR)</c> each answered "no x satisfies this", which is a positive claim and
        /// false of all three. That is the defect
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1036">#1036</a> fixed for
        /// equations, left standing for negation.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1127">#1127</a>
        /// </para>
        /// <para>
        /// Pushing the negation inward is unambiguous <i>here</i> in a way it is not in the
        /// simplifier, which is why it is done here and not as a rule: this switch has arms for
        /// the connectives and for the comparisons and none for <c>not</c>, so inward is the
        /// direction that reaches one. A negated comparison is a comparison, and
        /// <see cref="Core.Transformations.RewriteRules.InequalityEquality"/> is where that is
        /// already written down — asking it rather than restating it keeps the two from drifting.
        /// </para>
        /// <para>
        /// What is left over is answered as written rather than as nothing: <c>not (x in RR)</c>
        /// is <c>{ x : not x in RR }</c>, which names the non-real complex numbers exactly and
        /// asserts of them only that they are what the statement says.
        /// </para>
        /// </remarks>
        private static Set? Negation(Entity statement, Entity operand, Variable x)
        {
            switch (operand)
            {
                // not not a = a
                case Notf(var inner):
                    return Solve(inner, x);
                // De Morgan, in the direction that reaches an arm.
                case Andf(var left, var right):
                    return Solve(!left, x) is { } notLeft && Solve(!right, x) is { } notRight ? (Set)MathS.Union(notLeft, notRight) : null;
                case Orf(var left, var right):
                    return Solve(!left, x) is { } neitherLeft && Solve(!right, x) is { } neitherRight ? Conjunction(neitherLeft, neitherRight, statement, x) : null;
            }

            var asAComparison = Core.Transformations.RewriteRules.InequalityEquality.ApplyOnce(statement);
            if (asAComparison is not Notf)
                return Solve(asAComparison, x);

            return new ConditionalSet(x, statement);
        }

        /// <summary>
        /// <c>a implies b</c> holds where <c>a</c> fails or where <c>b</c> holds, and the first
        /// half of that is a set-builder rather than a complement.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This used to read <c>expr.Codomain \ Solve(a) \/ Solve(b)</c>, taking the
        /// complement inside the <b>statement node's</b> codomain. That is
        /// <see cref="Domain.Boolean"/> for every <see cref="Impliesf"/>, so
        /// <c>(x = 1) implies (x = 2)</c> was answered <c>{ 2 } \/ BB</c> — a solution set for
        /// a numeric question containing <c>True</c> and <c>False</c>. That is exactly the
        /// confusion between a codomain and a set that
        /// <a href="https://github.com/asc-community/AngouriMath/issues/996">#996</a> is about,
        /// and a <c>TODO</c> here asked for a universal set to subtract from instead.
        /// </para>
        /// <para>
        /// <b>Neither is needed.</b> "The values of <c>x</c> where <c>a</c> does not hold" is
        /// <c>{ x : not a }</c>, which names no universe at all — a set-builder is already this
        /// library's unconstrained set, and a complement written that way is right whatever
        /// <c>x</c> ranges over. Which is #996's answer: what the solver wanted was the
        /// difference, and the difference is expressible without the universe.
        /// </para>
        /// <para>
        /// It does not make the implication solver complete. <c>Solve(b, x)</c> is still
        /// <see cref="Set.Empty"/> where <c>b</c> does not mention <c>x</c>, so
        /// <c>A implies True</c> comes back as <c>{ A : not A }</c> and not as <c>BB</c> — as it
        /// did before, where the answer was <c>BB \ { True }</c>. What this stops is answering
        /// with a set the question was never asked over.
        /// </para>
        /// </remarks>
        private static Set? Implication(Entity left, Entity right, Variable x)
            => Solve(right, x) is { } solved ? (Set)MathS.Union(new ConditionalSet(x, !left), solved) : null;

        /// <summary>
        /// Where both sides of a conjunction were settled, its solution set is the
        /// intersection of theirs.
        /// </summary>
        /// <remarks>
        /// Where one of them is a condition nothing settled, it is not: intersecting a
        /// finite set with one keeps an element whose membership could not be decided, so
        /// <c>x^6 + x*y + 1 = 0 and x - 1 = 0</c> comes back as <c>{ 1 }</c> — and 1 is a
        /// root of the first only when <c>y</c> is -2. The conjunction as written asserts
        /// exactly what is known about it and nothing more.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1036">#1036</a>
        /// </remarks>
        private static Set Conjunction(Set left, Set right, Entity statement, Variable x)
            => (left, right) switch
            {
                (ConditionalSet condition, FiniteSet listed) => Filtered(listed, condition, statement, x),
                (FiniteSet listed, ConditionalSet condition) => Filtered(listed, condition, statement, x),
                _ => (Set)MathS.Intersection(left, right),
            };

        /// <summary>
        /// The members of a listed solution set at which the other side's condition is
        /// decided: kept where it evaluates to <c>True</c>, dropped where <c>False</c> -- so
        /// <c>x^2 = 4 and not x = 2</c> is <c>{ -2 }</c> -- and the conjunction as written the
        /// moment one member is undecided, which is #1036's case, where a search left the
        /// condition open and 1 was a root only when y was -2.
        /// </summary>
        private static Set Filtered(FiniteSet listed, ConditionalSet condition, Entity statement, Variable x)
        {
            if (condition.Var is not Variable bound)
                return new ConditionalSet(x, statement);
            var kept = new List<Entity>();
            foreach (var member in listed)
            {
                // Evaluated, and simplified where evaluation leaves it open: the solution of
                // 2 v + 1 = 2 u + 1 arrives as -(1 - 2u - 1)/2, and `not that = u` is decided
                // by the simplifier and not by evaluation.
                var at = condition.Predicate.Substitute(bound, member);
                var decided = at.Evaled;
                if (decided is not Entity.Boolean)
                    decided = at.Simplify();
                if (decided == Entity.Boolean.True)
                    kept.Add(member);
                else if (decided != Entity.Boolean.False)
                    return new ConditionalSet(x, statement);
            }
            return new FiniteSet(kept);
        }

        /// <summary>
        /// The solutions of <c>difference = 0 (mod modulus)</c> in <paramref name="x"/>, where
        /// the difference is a polynomial in it with whole coefficients: solved outright when
        /// linear, and by trying every residue when not and the modulus is small.
        /// </summary>
        private static Set? Congruence(Entity difference, Integer modulus, Variable x)
        {
            var simplified = difference.InnerSimplified;
            if (simplified.Vars.Any(v => v != x))
                return null;
            var indices = new Dictionary<Variable, int> { [x] = 0 };
            if (MultivariatePolynomial.TryParse(simplified, indices) is not { HasIntegerCoefficients: true } polynomial)
                return null;
            var n = modulus.EInteger.Abs();
            if (polynomial.DegreeIn(0) <= 1)
            {
                var coefficients = polynomial.CoefficientsIn(0);
                var a = coefficients.TryGetValue(1, out var slope) ? slope.Terms.Single().Value.Numerator : EInteger.Zero;
                var b = coefficients.TryGetValue(0, out var constant) ? constant.Terms.SingleOrDefault().Value?.Numerator ?? EInteger.Zero : EInteger.Zero;
                return ResidueClasses.SolveLinear(x, a, -b, n);
            }
            if (n.IsZero || n.CompareTo(EInteger.FromInt32(LargestModulusTried)) > 0)
                return null;
            Entity? classes = null;
            for (var residue = EInteger.Zero; residue.CompareTo(n) < 0; residue += 1)
                if (polynomial.ValueModulo(new[] { residue }, n) is { IsZero: true })
                {
                    Entity one = new Congruentf(x, Integer.Create(residue), Integer.Create(n));
                    classes = classes is null ? one : classes | one;
                }
            return classes is null ? Set.Empty : new ConditionalSet(x, x.In(MathS.Sets.Z) & classes);
        }

        /// <summary>How large a modulus is tried residue by residue for a congruence that is not linear.</summary>
        private const int LargestModulusTried = 4096;

        /// <summary>
        /// The equation as it was written where the solver left the whole of it unsolved: the
        /// solver works on <c>left - right = 0</c>, and <c>{ x : x! = 6 }</c> reads better than
        /// <c>{ x : x! - 6 = 0 }</c>, which says the same.
        /// </summary>
        private static Set AsWritten(Set solved, Entity difference, Entity equation, Variable x)
            => solved is ConditionalSet { Predicate: Equalsf(var rearranged, Integer { IsZero: true }) } && rearranged == difference.InnerSimplified
                ? new ConditionalSet(x, equation)
                : solved;

        /// <summary>
        /// <c>a = b</c> between two statements, as the statement it is: <c>a</c> where <c>b</c> is
        /// <c>True</c>, <c>not a</c> where it is <c>False</c>, and the two agreeing otherwise.
        /// </summary>
        private static Entity Equivalent(Entity left, Entity right)
            => (left, right) switch
            {
                (_, Entity.Boolean truth) => truth == Entity.Boolean.True ? left : !left,
                (Entity.Boolean truth, _) => truth == Entity.Boolean.True ? right : !right,
                _ => (left & right) | (!left & !right),
            };

        /// <summary>
        /// The solution set as <see cref="Entity.Solve(Variable)"/> gives it, simplified, or
        /// <see langword="null"/> where the solver declines the statement: an inequality it
        /// cannot read. The public method reports that by throwing, and the library's own
        /// callers read it as not solved. https://github.com/asc-community/AngouriMath/issues/1540
        /// </summary>
        internal static Set? Solved(Entity statement, Variable x)
            => Solve(statement, x)?.InnerSimplified as Set;

        /// <summary>The solution set, or <see langword="null"/> where a part of the statement is declined.</summary>
        internal static Set? Solve(Entity expr, Variable x)
            => expr switch
            {
                // A statement equal to a truth value is the statement or its negation, and two
                // statements equal are equivalent: (x > 2) = (3 > 1) holds where x > 2 does. The
                // arm for an equation of numbers subtracted them, and answered { }.
                // https://github.com/asc-community/AngouriMath/issues/1549
                Equalsf(Statement left, Statement right) => Solve(Equivalent(left, right), x),

                Equalsf(var left, var right) when left is Set || right is Set
                    => AnalyticalSetSolver.Solve(left, right, x),

                Equalsf(var left, var right) when left is not Set && right is not Set
                    => UnsolvedWhereIndependenceIsDenied(
                        WithoutSpuriousRoots(AsWritten(AnalyticalEquationSolver.Solve(left - right, x), left - right, expr, x), left - right, x),
                        expr, x),

                Equalsf => Empty,

                // x in ZZ and an inequality with an exponential or a factorial in x: a window
                // searched and its tails proved by induction, so that { n in ZZ : 2^n > n^2 }
                // is {0, 1} \/ ZZ /\ [5; +oo) rather than a refusal from the inequality solver.
                // https://github.com/asc-community/AngouriMath/issues/1409
                Andf(Inf(var name, Set integers), ComparisonSign inequality) when name == x && ThresholdSearch.Solve(x, integers, inequality) is { } thresholds => thresholds,
                Andf(ComparisonSign inequality, Inf(var name, Set integers)) when name == x && ThresholdSearch.Solve(x, integers, inequality) is { } thresholds => thresholds,
                Andf(var left, var right) =>
                    Solve(left, x) is { } leftSolved && Solve(right, x) is { } rightSolved ? Conjunction(leftSolved, rightSolved, expr, x) : null,
                Orf(var left, var right) =>
                    Solve(left, x) is { } leftSolved && Solve(right, x) is { } rightSolved ? (Set)MathS.Union(leftSolved, rightSolved) : null,
                Impliesf(var left, var right) => Implication(left, right, x),
                Notf(var operand) => Negation(expr, operand, x),

                Greaterf(var left, var right) =>
                    WithoutPoles(AnalyticalInequalitySolver.Solve(Minus(left, right), x), Minus(left, right), x),
                LessOrEqualf(var left, var right) =>
                    WithoutPoles(AnalyticalInequalitySolver.Solve(Minus(right, left), x)
                        ?.Unite(AnalyticalEquationSolver.Solve(Minus(left, right), x)), Minus(left, right), x),
                GreaterOrEqualf(var left, var right) =>
                    WithoutPoles(AnalyticalInequalitySolver.Solve(Minus(left, right), x) is { } above
                        ? (Set)MathS.Union(above, AnalyticalEquationSolver.Solve(Minus(left, right), x)) : null, Minus(left, right), x),

                Lessf(var left, var right) =>
                    WithoutPoles(AnalyticalInequalitySolver.Solve(Minus(right, left), x), Minus(left, right), x),

                Variable when expr == x => new FiniteSet(true),

                Inf(var var, Set set) when var == x => set,
                // f(x) in S: the members of a listed S each as an equation, an interval as its
                // two bounds, and anything else left as the set of x with the property, since
                // a solver that answered "no x" here was answering wrongly -- x^2 in (0; 1) was
                // the empty set. https://github.com/asc-community/AngouriMath/issues/1409
                Inf(var member, Set set) when member.ContainsNode(x) => Membership(member, set, x) switch
                {
                    (Set solved, _) => solved,
                    (null, Read: true) => null,
                    _ => new ConditionalSet(x, expr),
                },

                // a x + b = c (mod n): one residue class, or none, by the gcd; a congruence of
                // higher degree by the residues that satisfy it, where the modulus is small
                // enough to list them. https://github.com/asc-community/AngouriMath/issues/1409
                Congruentf(var left, var right, Integer modulus) when Congruence(left - right, modulus, x) is { } classes => classes,
                
                Providedf(var e, var predicate) => TreeAnalyzer.Assuming(predicate, () => Solve(e, x)) is { } solved ? EquationSolver.FilterRoots(solved, predicate, x) : null,
                Piecewise p => EquationSolver.SolvePiecewiseOrNull(p, x, Solve),

                // A statement the solver has no arm for is the set of x with the property, left
                // as written -- not the empty set, which claims there is no such x. A statement
                // that does not mention x is one of everything or of nothing, and the set
                // builder over it evaluates to that.
                _ => new ConditionalSet(x, expr)
            };

        /// <summary>
        /// The <c>x</c> with <c>f(x)</c> in a set: for a listed set, the union of the equations
        /// <c>f(x) = s</c>; for an interval, the conjunction of its bounds, each strict or not as
        /// the end is. <c>Read</c> is false for a set that is neither, and the solution set is
        /// <see langword="null"/> there and where the solver declines what it was read as.
        /// </summary>
        private static (Set? Solved, bool Read) Membership(Entity member, Set set, Variable x)
        {
            switch (set)
            {
                case FiniteSet listed:
                    Set? union = null;
                    foreach (var element in listed)
                    {
                        if (Solve(member.Equalizes(element), x) is not { } solutions)
                            return (null, true);
                        union = union is null ? solutions : MathS.Union(union, solutions);
                    }
                    return (union ?? Set.Empty, true);
                case Interval interval:
                    Entity? condition = null;
                    if (interval.Left.Evaled != Real.NegativeInfinity)
                        condition = interval.LeftClosed ? member >= interval.Left : member > interval.Left;
                    if (interval.Right.Evaled != Real.PositiveInfinity)
                    {
                        var upper = interval.RightClosed ? member <= interval.Right : member < interval.Right;
                        condition = condition is null ? upper : condition & upper;
                    }
                    return (condition is null ? MathS.Sets.R : Solve(condition, x), true);
                default:
                    return (null, false);
            }
        }
    }
}
