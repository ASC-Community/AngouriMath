//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath
{
    using AngouriMath.Core.Exceptions;
    using AngouriMath.Core.Multithreading;
    using Core;
    using System.Linq;
    using static Functions.Algebra.LimitFunctional;
    partial record Entity
    {
        partial record Number
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) => this;
        }

        partial record Variable
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side);
        }

        partial record Matrix
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                null; // TODO
        }

        partial record Sumf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                var (augend, addend) =
                    (Augend.ComputeLimitDivideEtImpera(x, dist, side), Addend.ComputeLimitDivideEtImpera(x, dist, side)) switch
                    {
                        ({ } lim1, { } lim2) when IsDeterminate(New(lim1, lim2), x) => (lim1, lim2),
                        var (lim1, lim2) => (lim1 is { IsFinite: true } ? lim1 : Augend,
                                             lim2 is { IsFinite: true } ? lim2 : Addend)
                    };
                return ComputeLimitImpl(New(augend, addend), x, dist, side);
            }
        }

        partial record Minusf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                var (minuend, subtrahend) =
                    (Minuend.ComputeLimitDivideEtImpera(x, dist, side), Subtrahend.ComputeLimitDivideEtImpera(x, dist, side)) switch
                    {
                        ({ } lim1, { } lim2) when IsDeterminate(New(lim1, lim2), x) => (lim1, lim2),
                        var (lim1, lim2) => (lim1 is { IsFinite: true } ? lim1 : Minuend,
                                             lim2 is { IsFinite: true } ? lim2 : Subtrahend)
                    };
                return ComputeLimitImpl(New(minuend, subtrahend), x, dist, side);
            }
        }

        partial record Mulf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                else
                {
                    var multiplierLimit = Multiplier.ComputeLimitDivideEtImpera(x, dist, side);
                    var multiplicandLimit = Multiplicand.ComputeLimitDivideEtImpera(x, dist, side);
                    if (AnInfinityTimesASign(Multiplier, Multiplicand, multiplicandLimit, dividing: false, x, dist, side) is { } byMultiplier)
                        return byMultiplier;
                    if (AnInfinityTimesASign(Multiplicand, Multiplier, multiplierLimit, dividing: false, x, dist, side) is { } byMultiplicand)
                        return byMultiplicand;
                    // 0 * oo from one side, with a quotient for one factor, is asked as that quotient
                    // with the other factor in its dividend, its whole limit once. Read as a product,
                    // nothing settles the form, and the rewriting solvers took x * (ln(x) / ln(2 + x)) at
                    // 0 from the right through l'Hopital's rule for longer than any caller waits, where
                    // (x ln(x)) / ln(2 + x) is 0 at once. One-sided only, and not while the sides of a
                    // two-sided limit are compared: two-sided, x * ln(x) / ln(2 + x) at 0 is NaN, its
                    // left side being complex, and regrouped there the same product written with
                    // brackets came back 0.
                    if (side is not ApproachFrom.BothSides && !IsComparingSides
                        && multiplierLimit is { } zeroOrInfinity1 && multiplicandLimit is { } zeroOrInfinity2
                        && IsZeroTimesAnInfinity(zeroOrInfinity1, zeroOrInfinity2)
                        && (Multiplicand, Multiplier) switch
                        {
                            (Divf(var dividend, var divisor), var other) => new Divf(other * dividend, divisor),
                            (var other, Divf(var dividend, var divisor)) => new Divf(dividend * other, divisor),
                            _ => null
                        } is { } regrouped
                        && AsTheWholeLimit(regrouped, x, dist, side) is { } overOneDivisor
                        && overOneDivisor.Evaled != MathS.NaN)
                        return overOneDivisor;
                    // So, with a sum or a difference for one factor, is the product taken term by term:
                    // l'Hopital's rule took x * (ln(-tanh(x)) + 1) at 0 from the left through four and a
                    // half seconds of nested limits, where x ln(-tanh(x)) + x is 0 at once.
                    if (side is not ApproachFrom.BothSides && !IsComparingSides
                        && multiplierLimit is { } zeroOrInfinity3 && multiplicandLimit is { } zeroOrInfinity4
                        && IsZeroTimesAnInfinity(zeroOrInfinity3, zeroOrInfinity4)
                        && (Multiplicand, Multiplier) switch
                        {
                            (Sumf(var augend, var addend), var other) => other * augend + other * addend,
                            (var other, Sumf(var augend, var addend)) => augend * other + addend * other,
                            (Minusf(var minuend, var subtrahend), var other) => other * minuend - other * subtrahend,
                            (var other, Minusf(var minuend, var subtrahend)) => minuend * other - subtrahend * other,
                            _ => null
                        } is { } distributed
                        && AsTheWholeLimit(distributed, x, dist, side) is { } termByTerm
                        && termByTerm.Evaled != MathS.NaN)
                        return termByTerm;
                    var (mp, md) =
                        (multiplierLimit, multiplicandLimit) switch
                        {
                            ({ } lim1, { } lim2) when IsDeterminate(New(lim1, lim2), x) => (lim1, lim2),
                            ({ IsFinite: true } lim1, { IsFinite: true } lim2) => (lim1, lim2),
                            (_, { } l2) when !Multiplier.ContainsNode(x) => (Multiplier, l2),
                            ({ } l1, _) when !Multiplicand.ContainsNode(x) => (l1, Multiplicand),
                            ({ IsFinite: true } lim1, { } exp) => (lim1, exp),
                            ({ } bas, { IsFinite: true } lim2) => (bas, lim2),
                            _ => (Multiplier, Multiplicand)
                        };
                    var substituted = ComputeLimitImpl(New(mp, md), x, dist, side);
                    // Any other 0 * oo, read off the parts, is a form with no value, which says there is
                    // no limit where the descent only cannot tell. Its whole limit is asked once, as a
                    // quotient's is: x^pi ln(x) at 0 from the right is 0, and the whole limit says so,
                    // but the descent handed on 0 * (-oo), and x^pi ln(x) / 2 and sin(x^pi ln(x)) were
                    // left unevaluated. Under the gates above, and only a value is taken.
                    if ((substituted is null || substituted.Evaled == MathS.NaN)
                        && side is not ApproachFrom.BothSides && !IsComparingSides
                        && multiplierLimit is { } zeroOrInfinity5 && multiplicandLimit is { } zeroOrInfinity6
                        && IsZeroTimesAnInfinity(zeroOrInfinity5, zeroOrInfinity6)
                        && AsTheWholeLimit(this, x, dist, side) is { } asked && asked.Evaled is Number { IsNaN: false } value)
                        return asked.InnerSimplified is var folded && folded.Nodes.Any(static node => node is Number { IsFinite: false })
                            ? value : folded;
                    return substituted;
                }
            }
        }

        partial record Divf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // Over a quotient, as a quotient of products: a / (b / c) is a c / b wherever it has a value. Read as written,
                // x^(-pi) / (ln(2) / ln(x)) at +oo is 0/0, and l'Hopital's rule makes each step of it another 0 * oo with a
                // higher power of ln(x), so it was left unevaluated where x^(-pi) ln(x) / ln(2) is 0 at once. Only a value is
                // taken from it. x ln(x) at 0 comes back as 0 * (-oo), which is no answer, and from both sides that was read
                // as NaN, where x / log(x, 2) read as written is 0. And only over a constant: x^x / (x / x) as a product is
                // x^x x / x, which took twenty times as long and answered from both sides of 0 where x^x itself is NaN.
                if (Divisor is Divf(var over, var under) && under.ContainsNode(x) && !over.ContainsNode(x)
                    && New(Dividend * under, over).ComputeLimitDivideEtImpera(x, dist, side) is { } asProducts
                    && !asProducts.ContainsNode(x) && asProducts.Evaled != MathS.NaN)
                    return asProducts;
                // A form with no value -- 0 * +oo / 0, which is what -x / (sqrt(1 - x^2) * arcsin(x)) at 0
                // from the right comes back as -- is the claim that there is no limit, and the parts may
                // say otherwise. It is kept for the end, where it was answered before, and the parts are
                // read first.
                var whole = ComputeLimitImpl(this, x, dist, side);
                if (whole is { } lim && lim.Evaled != MathS.NaN)
                    return lim;
                else
                {

                    var dividendLimit = Dividend.ComputeLimitDivideEtImpera(x, dist, side);
                    var divisorLimit = Divisor.ComputeLimitDivideEtImpera(x, dist, side);
                    if (AnInfinityTimesASign(Dividend, Divisor, divisorLimit, dividing: true, x, dist, side) is { } byDividend)
                        return byDividend;
                    var (dividend, divisor) =
                        (dividendLimit, divisorLimit) switch
                        {
                            ({ } lim1, { } lim2) when IsDeterminate(New(lim1, lim2), x) => (lim1, lim2),
                            ({ } lim1, { } lim2) when lim1.InnerSimplified.IsFinite && lim2.InnerSimplified.IsFinite && lim2.InnerSimplified != 0 => (lim1, lim2),
                            ({ IsFinite: true } lim1, { IsFinite: true } lim2) => (lim1, lim2),
                            (_, { } l2) when !Dividend.ContainsNode(x) => (Dividend, l2),
                            ({ } l1, _) when !Divisor.ContainsNode(x) => (l1, Divisor),
                            ({ IsFinite: true } lim1, { } exp) => (lim1, exp),
                            ({ } bas, { IsFinite: true } lim2) => (bas, lim2),
                            _ => (Dividend, Divisor)
                        };
                    var substituted = ComputeLimitImpl(New(dividend, divisor), x, dist, side);
                    if (substituted is { } found && found.Evaled != MathS.NaN)
                        return found;
                    // Putting the two parts' limits in place of the parts loses the one thing
                    // that decides a quotient whose divisor vanishes: 1 / 0 says nothing about
                    // which side the divisor vanishes from, and so comes back NaN. Only where
                    // nothing above answered, and whatever it did answer is kept if this finds
                    // nothing better.
                    if (DivergesAtAVanishingDivisor(Dividend, Divisor, x, dist, side) is { } diverges)
                        return diverges;
                    // An indeterminate form, 0/0 or oo/oo, read off the parts is NaN, which says there
                    // is no limit, where the descent only cannot tell: sin(x)/x at 0 is 1, which the
                    // whole limit reaches through the first remarkable limit, and a function of it --
                    // (sin(x)/x)^2, exp(sin(x)/x), exp(ln(cos(x))/x^2) -- came out NaN, its argument
                    // asked only of the descent. The whole limit of the quotient is asked once, and
                    // only a value is taken, and as the number where the form it came back in holds the
                    // infinity it was taken at: -4/9 * 0 * (1 - (1 + (1/+oo)^2)^(1/3)), handed on as
                    // written, sent a definite integral astray. Folded, it is 0, and ln(2) * 2^(1/+oo) is
                    // ln(2), not its decimal. Where it has no
                    // value, what was answered is kept.
                    // Two infinite parts are not put in place of the parts above, so the substitution is not
                    // of the parts' limits, and the form is read off the parts' own limits instead:
                    // exp(ln(arcsin(x)) / ln(1/x)) at 0 from the right, whose exponent is -oo / +oo and
                    // tends to -1, was left unevaluated, and so were a sine of it and, after ten seconds, a
                    // hyperbolic sine.
                    if ((substituted?.Evaled == MathS.NaN && IsAnIndeterminateQuotient(dividend, divisor)
                            || (substituted is null || substituted.Evaled == MathS.NaN)
                               && dividendLimit is { } partLimit1 && divisorLimit is { } partLimit2
                               && IsAnIndeterminateQuotient(partLimit1, partLimit2))
                        && AsTheWholeLimit(this, x, dist, side) is { } asked && asked.Evaled is Number { IsNaN: false } value)
                        return asked.InnerSimplified is var folded && folded.Nodes.Any(static node => node is Number { IsFinite: false })
                            ? value : folded;
                    return whole ?? substituted;
                }
            }
        }

        /// <summary>
        /// The limit of an infinity that does not depend on <paramref name="x"/> times, or over,
        /// <paramref name="other"/>: that infinity, with the sign of <paramref name="other"/> on the
        /// way in, wherever the sign does not change there; otherwise <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// An infinity is not a coefficient. Taken as one, the descent put the other part's limit in
        /// its place, and +oo * x at 0 from the right became +oo * 0, which folds to 0, while -oo / x
        /// at +oo became -oo / +oo and was answered 0 as well. At every point on the way in, though,
        /// +oo * x is +oo and -oo / x is -oo. This is how ln(0) / ln(x), the logarithm of 0 to the
        /// base x, was answered 0 at 0 from the right, where it is +oo all the way in.
        /// Only where the product or quotient of the two limits leaves the answer open: the
        /// other part tends to 0, or, for a quotient, to an infinity. Elsewhere the descent
        /// below already answers it.
        /// </remarks>
        private static Entity? AnInfinityTimesASign(Entity constant, Entity other, Entity? otherLimit, bool dividing,
            Variable x, Entity dist, ApproachFrom side)
        {
            if (constant.ContainsNode(x) || constant.Evaled is not Real { IsFinite: false, IsNaN: false } infinity)
                return null;
            if (otherLimit?.Evaled is not Real { IsNaN: false } limit)
                return null;
            if (!limit.IsZero && (!dividing || limit.IsFinite))
                return null;
            var sign = limit.IsZero
                ? ComputeLimit(MathS.Signum(other), x, dist, side)?.Evaled
                : limit.IsNegative ? Integer.MinusOne : Integer.One;
            return sign switch
            {
                Integer one when one == 1 => infinity,
                Integer minusOne when minusOne == -1 => infinity.IsNegative ? Real.PositiveInfinity : Real.NegativeInfinity,
                _ => null,
            };
        }

        /// <summary>Whether two parts' limits make a quotient 0/0 or oo/oo.</summary>
        private static bool IsAnIndeterminateQuotient(Entity dividend, Entity divisor)
            => (dividend.Evaled, divisor.Evaled) switch
            {
                (Complex { IsZero: true }, Complex { IsZero: true }) => true,
                (Number { IsFinite: false, IsNaN: false }, Number { IsFinite: false, IsNaN: false }) => true,
                _ => false,
            };

        /// <summary>Whether two factors' limits make a product 0 * oo.</summary>
        private static bool IsZeroTimesAnInfinity(Entity first, Entity second)
            => (first.Evaled, second.Evaled) switch
            {
                (Complex { IsZero: true }, Number { IsFinite: false, IsNaN: false }) => true,
                (Number { IsFinite: false, IsNaN: false }, Complex { IsZero: true }) => true,
                _ => false,
            };

        /// <summary>The quotient whose whole limit is being asked from inside the descent, so that no other is asked inside it.</summary>
        [System.ThreadStatic] private static HashSet<Entity>? quotientsAskedWhole;

        private static Entity? AsTheWholeLimit(Entity quotient, Variable x, Entity dist, ApproachFrom side)
        {
            // One level: a whole limit asked from here descends again, and a retry inside it asked another, so that a
            // definite integral's antiderivative at its bounds -- 1/((1 + x^2)^(1/3) (9 + x^2)) across 0 -- went from four
            // seconds to past two minutes and was left unevaluated.
            // Nor under a caller's budget of limits, which every limit asked here spends: fifty asked on the side of a
            // definite integral's bounds left the bounds none, and the integral unevaluated.
            if (Functions.Algebra.LimitFunctional.UnderALimitBudget)
                return null;
            // Nor of the whole question, which the path that began the descent asks the rest of the
            // machinery of once the descent has nothing to say. Asked here as well, it descended
            // again and simplified the same expression twice: (1 - tanh(x)^2) * x * (1 + x^2) / tanh(x)
            // at 0 from the right spent nine seconds on each before l'Hopital's rule answered 1.
            if (Functions.Algebra.LimitFunctional.IsTheWholeQuestion(quotient))
                return null;
            quotientsAskedWhole ??= new();
            if (quotientsAskedWhole.Count > 0)
                return null;
            // The expression whose two sides a two-sided limit is comparing, asked whole from one side, is that side's
            // own question rather than one asked inside another, and does not use the one level up. Counted, it left
            // that side nothing to ask of its own regrouping: x^pi / log(x, 2) at 0 from the right reads as
            // x^pi * ln(x), asked whole, in a fifth of a second alone, and from inside the comparison it was refused
            // and the side spent ten seconds on other readings that did not answer.
            if (Functions.Algebra.LimitFunctional.IsTheComparedQuestion(quotient))
                return Functions.Algebra.LimitFunctional.ComputeLimit(quotient, x, dist, side);
            if (!quotientsAskedWhole.Add(quotient))
                return null;
            try { return Functions.Algebra.LimitFunctional.ComputeLimit(quotient, x, dist, side); }
            finally { quotientsAskedWhole.Remove(quotient); }
        }

        partial record Modf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (Dividend.ComputeLimitDivideEtImpera(x, dist, side) is not { } dividend
                    || Divisor.ComputeLimitDivideEtImpera(x, dist, side) is not { } divisor)
                    return null;
                var substituted = New(dividend, divisor);
                if (substituted.Evaled is not Number { IsFinite: true } value)
                    return null;
                // The remainder is continuous except where the dividend reaches a non-zero
                // multiple of the divisor, and there it jumps: x % 3 tends to 3 approaching 3
                // from the left and to 0 from the right. Which of the two a one-sided limit
                // takes depends on the direction the dividend crosses in, not only on what it
                // tends to, so the jumps are left unanswered rather than answered with the
                // value at the point -- that value is one of the two one-sided limits and not
                // the other, and is no two-sided limit at all.
                //
                // Zero is not one of those points: the remainder takes the sign of the dividend,
                // so x % 3 is x on either side of 0 and passes through it continuously.
                if (value == 0 && dividend.Evaled != 0)
                    return null;
                return substituted;
            }
        }

        /// <summary>
        /// A part's limit with the destination folded out of it. A one-sided limit at a finite
        /// point moves the variable out to infinity, and the value read off there keeps the
        /// infinity in it: x^2 + x - 1 from the left at 0 is -1 - 1/+oo + 1/+oo^2, which is -1
        /// but is not finite to the check above, so sin(x^2 + x - 1) from the left at 0 was left
        /// unevaluated. Only for the trigonometric functions, which have no cut, and so are
        /// continuous at every finite argument short of a pole. A function with a cut is not:
        /// arctan and the logarithm decline there by their own reading, and folding their
        /// argument's limit would answer with the value on the cut.
        /// </summary>
        private static Entity? WithoutTheDestination(Entity? limit)
            => limit is { } found && found.Nodes.Any(node => node is Real { IsFinite: false })
               && found.InnerSimplified is var folded && !folded.Nodes.Any(node => node is Real { IsFinite: false })
                ? folded : limit;

        partial record Sinf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cosf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Secantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cosecantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Arcsecantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // Asked in front of the substitution, which for a diverging argument does answer
                // -- with arcsec(+oo), a right angle written as a function of an infinity rather
                // than as the right angle it is.
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        partial record Arccosecantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // A diverging argument first, as for the arcsecant: substituting it leaves the
                // function of an infinity rather than the angle it tends to.
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        partial record Tanf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cotanf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    WithoutTheDestination(Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Logf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                else
                {
                    var baseLimit = Base.ComputeLimitDivideEtImpera(x, dist, side);
                    var antilogarithmLimit = Antilogarithm.ComputeLimitDivideEtImpera(x, dist, side);
                    // The logarithm of a limit on the cut is the limit only from above or along the
                    // real line. https://github.com/asc-community/AngouriMath/issues/2266
                    if (ArrivesOnTheCutOffTheRealLine(Base, baseLimit, x, dist)
                        || ArrivesOnTheCutOffTheRealLine(Antilogarithm, antilogarithmLimit, x, dist))
                        return null;
                    var @base = baseLimit is { IsFinite: true } lim1 && lim1 != 0 ? lim1 : Base;
                    var power = antilogarithmLimit is { IsFinite: true } lim2 && lim2 != 0 ? lim2 : Antilogarithm;
                    MultithreadingFunctional.ExitIfCancelled();
                    return ComputeLimitImpl(New(@base, power), x, dist, side);
                }
            }
        }

        partial record Powf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                var baseLimit = Base.ComputeLimitDivideEtImpera(x, dist, side);
                var exponentLimit = Exponent.ComputeLimitDivideEtImpera(x, dist, side);
                // A power to an exponent that is not whole has the logarithm's cut, for the reason
                // the logarithm above declines it. https://github.com/asc-community/AngouriMath/issues/2266
                if (exponentLimit?.Evaled is not Integer && ArrivesOnTheCutOffTheRealLine(Base, baseLimit, x, dist))
                    return null;
                // A base that is never negative and vanishes, to an exponent tending to a negative
                // number, tends to +oo: 0 to a negative power has no value, and abs(x)^(-3/2) at 0
                // was left unevaluated from each side for it. Where the base vanishes, it does from
                // above. So does one that is positive near the point on each side approached, though it
                // may be negative elsewhere: (1/x)^(2 ((x - 1)/x^2 - 2)) at +oo was read as 0^(-4), which
                // has no value, and left unevaluated.
                if (baseLimit?.Evaled is Real { IsZero: true } && exponentLimit?.Evaled is Real { IsNegative: true, IsFinite: true }
                    && (IsNeverNegative(Base, x) || SignOnEachSide(Base, x, dist, side) is 1))
                    return Real.PositiveInfinity;
                // A base that keeps a sign as it vanishes is real and not 0 near the point, so to a
                // negative whole power it is 1 over a power tending to 0 from that sign's side: +oo for
                // an even power, and +oo or -oo by the sign for an odd one. sin(x)^(-2) at 0 was left
                // unevaluated from each side and was NaN two-sided, where 1/sin(x)^2 was +oo.
                // The exponent has to be that whole number near the point and not only tend to it, and
                // x - 1 - (x + 1) is -2 only once Simplify has cancelled x; it is asked only where the
                // exponent's limit already is one.
                if (baseLimit?.Evaled is Real { IsZero: true } && exponentLimit?.Evaled is Integer { IsNegative: true }
                    && dist.IsFinite
                    && (Exponent.ContainsNode(x) ? Exponent.Simplify() : Exponent).Evaled is Integer { IsNegative: true } wholeExponent
                    && SignOfAVanishingBase(wholeExponent.EInteger.IsEven) is { } sign)
                    return sign > 0 ? Real.PositiveInfinity : Real.NegativeInfinity;
                // An indeterminate pair, 0^0 among them, is read through the exponent, by the rule
                // the top level asks of a whole expression and asked of the power here, so that a
                // power under a function is read too: x^x at 0+ is 1, and sin(x^x) there was
                // unevaluated. One-sided, or at an infinity, as the top level asks it, and only where
                // the base is positive on the side approached, so that the power is real there: x^x on
                // the left of 0 is complex, and the two-sided limit there stays NaN. Where it settles
                // nothing, the pair is substituted as before, and 0^0 stays NaN. Not in the descents
                // a two-sided limit compares, where it would settle (-x)^x at 0 and leave x^x NaN, and
                // not for the whole of the question, which the path above asks once the descent is done.
                if (baseLimit is { } pairBase && exponentLimit is { } pairExponent
                    && (side is not ApproachFrom.BothSides || !dist.IsFinite)
                    && MayReadAPowerThroughItsExponent(this)
                    && (EvalAssumingContinuous(pairBase) == 0 && EvalAssumingContinuous(pairExponent) == 0
                        || IsIndeterminatePowerForm(pairBase, pairExponent))
                    && (side is ApproachFrom.BothSides ? ApproachFrom.Left : side) is var pairSide
                    && SignNear(Base, x, dist, pairSide) is 1
                    && SolveAsIndeterminatePower(this, x, dist, pairSide) is { } byExponent)
                    return byExponent;
                var substituted =
                    (baseLimit, exponentLimit)
                    switch {
                        ({ IsFinite: true } lim1, { IsFinite: true } lim2) => New(lim1, lim2),
                        (_, { } l2) when !Base.ContainsNode(x) => New(Base, l2),
                        ({ } l1, _) when !Exponent.ContainsNode(x) => New(l1, Exponent),
                        ({ IsFinite: true } lim1, { } exp) => New(lim1, exp),
                        ({ } bas, { IsFinite: true } lim2) => New(bas, lim2),
                        _ => New(Base, Exponent)
                    };
                // Putting each part's own limit in place of the part leaves a power that says
                // nothing whenever the two of them meet in an indeterminate form. The sum, the
                // product and the quotient are already guarded against this by IsDeterminate
                // above; the power was not, so lim x->+oo (x!)^(1/x) assembled (+oo)!^0 and
                // read 1 off it, where the limit is +oo. Declining hands the expression to the
                // rules behind the descent, and leaves it unevaluated -- "not settled" --
                // rather than wrong if none of them answers either.
                // https://github.com/asc-community/AngouriMath/issues/754
                if (substituted is Powf(var substitutedBase, var substitutedExponent)
                    && !substituted.ContainsNode(x)
                    && IsIndeterminatePowerForm(substitutedBase, substitutedExponent))
                    return null;
                return ComputeLimitImpl(substituted, x, dist, side);

                // The sign the power keeps near the point, or null where a side's base has no sign
                // or, for an odd power, the two sides disagree.
                int? SignOfAVanishingBase(bool even)
                {
                    var sides = side is ApproachFrom.BothSides ? new[] { ApproachFrom.Left, ApproachFrom.Right } : new[] { side };
                    int? sign = null;
                    foreach (var oneSide in sides)
                    {
                        // A sign is a question about a real value: x^x - 1 on the left of 0 is complex,
                        // and its reciprocal square goes off the real line. Where the base's real region
                        // is not known, as arcsin's is not, its form can still say it is real there.
                        if (!Functions.TreeAnalyzer.IsRealValued(Base, x)
                            && !(Functions.TreeAnalyzer.WhereRealValued(Base, x) is { } whereReal && HoldsNear(whereReal, x, dist, oneSide) is true)
                            && !IsRealOnBothSides(Base, x, dist))
                            return null;
                        if (SignNear(Base, x, dist, oneSide) is not { } baseSign)
                            return null;
                        var powerSign = even ? 1 : baseSign;
                        if (sign is { } earlier && earlier != powerSign)
                            return null;
                        sign = powerSign;
                    }
                    return sign;
                }
            }
        }

        partial record Arcsinf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        partial record Arccosf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        /// <summary>
        /// The limit of an error function or an exponential or trigonometric integral, given its
        /// argument's limit: the value it tends to at an infinity, and the function of a finite limit
        /// where it is continuous there.
        /// </summary>
        /// <remarks>
        /// None of these had a limit at all, so erf(x) at +oo, which is 1, was left unevaluated, and
        /// so was erf(x) at 1. A logarithm of something holding one came to a stop that took more than
        /// forty seconds: log(x + pi + erf(x), x^2 + tanh(1)) at +oo, which is 2. At an infinity:
        /// erf to 1 and -1, erfc to 0 and 2, erfi, Shi to +oo and -oo, Si to pi/2 and -pi/2, Ci to 0,
        /// Chi and li to +oo, and Ei to +oo and 0. Erf, erfc, erfi, Si and Shi are entire, so a finite
        /// limit is put in as it is. Ci, Chi, Ei and li have a logarithm's singularity at 0 and its cut
        /// below it, and li another at 1, so only a positive real limit other than li's 1 is put in;
        /// elsewhere nothing is claimed. Each value at an infinity was checked against mpmath at 10^6 and
        /// -10^6, and at 50 and -50 for erfi, Shi and Chi.
        /// </remarks>
        private static Entity? OfAnErrorOrIntegralFunction(Entity node, Entity argument, Variable x, Entity dist, ApproachFrom side)
        {
            if (argument.ComputeLimitDivideEtImpera(x, dist, side) is not { } limit)
                return null;
            if (limit.Evaled is Real { IsFinite: false, IsNaN: false } infinity)
            {
                var upwards = !infinity.IsNegative;
                return node switch
                {
                    Erff => upwards ? Integer.One : Integer.MinusOne,
                    Erfcf => upwards ? Integer.Zero : Integer.Create(2),
                    Erfif or Shif => upwards ? Real.PositiveInfinity : Real.NegativeInfinity,
                    Sif => upwards ? MathS.pi / 2 : -MathS.pi / 2,
                    Cif => upwards ? Integer.Zero : null,
                    Chif or Lif => upwards ? Real.PositiveInfinity : null,
                    Eif => upwards ? Real.PositiveInfinity : Integer.Zero,
                    _ => null
                };
            }
            if (limit.Evaled is not Number { IsFinite: true, IsNaN: false } value)
                return null;
            var entire = node is Erff or Erfcf or Erfif or Sif or Shif;
            if (!entire && !(value is Real { IsPositive: true } && !(node is Lif && value == Integer.One)))
                return null;
            return node switch
            {
                Erff => new Erff(limit),
                Erfcf => new Erfcf(limit),
                Erfif => new Erfif(limit),
                Sif => new Sif(limit),
                Cif => new Cif(limit),
                Shif => new Shif(limit),
                Chif => new Chif(limit),
                Eif => new Eif(limit),
                Lif => new Lif(limit),
                _ => null
            };
        }

        partial record Erff
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Erfcf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Erfif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Sif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Cif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Shif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Chif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Eif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Lif
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => OfAnErrorOrIntegralFunction(this, Argument, x, dist, side);
        }

        partial record Arctanf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // A diverging argument first, as for the arcsecant: substituting it leaves the
                // function of an infinity rather than the angle it tends to.
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        partial record Arccotanf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // A diverging argument first, as for the arcsecant: substituting it leaves the
                // function of an infinity rather than the angle it tends to.
                if (InverseTrigonometryAtInfinity(this, Argument.ComputeLimitDivideEtImpera(x, dist, side)) is { } atInfinity)
                    return atInfinity;
                return ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                    : ComputeLimitImpl(New(
                        Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                        x, dist, side);
            }
        }

        partial record Factorialf
        {
            // Not at a pole: the factorial of a negative integer has no value, and the factorial tends
            // to an infinity there whose sign depends on the side, so putting the argument's limit in
            // would only write the pole down. x! at -1 came back (-1)! once that limit arrived as -1.
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                var argumentLimit = Argument.ComputeLimitDivideEtImpera(x, dist, side);
                if (argumentLimit?.Evaled is Integer { EInteger.Sign: < 0 })
                    return null;
                return ComputeLimitImpl(New(argumentLimit is { IsFinite: true } lim1 ? lim1 : Argument), x, dist, side);
            }
        }

        // The upper incomplete gamma function, continuous in z away from 0, and at 0 for Re a > 0, where it is Gamma(a): the
        // limit is the function at its argument's limit wherever that value is a finite number. An infinite one is not taken,
        // since it can differ by side: gamma(-1, z) is about 1/z. An answer in it from 0 is 0 times an infinity there, which a
        // definite integral takes the limit of. https://github.com/asc-community/AngouriMath/issues/2075
        partial record IncompleteGammaf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (Parameter.ContainsNode(x))
                    return null;
                // By continuity alone: the value at a point where it is infinite is not the limit there -- gamma(-1, 0) is +oo,
                // the integral diverging, where gamma(-1, z) is about 1/z and tends to -oo from the left.
                return LowerLimit.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } limit && !limit.ContainsNode(x)
                    && New(Parameter, limit) is var value && value.Evaled is Number.Complex { IsFinite: true, IsNaN: false }
                    ? value
                    : null;
            }
        }

        // A derivative that closes is taken before the limit (WithTheDerivativesTaken). One that does not is not
        // read through its parts: the limit of what is differentiated says nothing about the limit of its derivative,
        // and the variable differentiated by is not a value to put a limit in. derivative(sin(x) / x, x) at 0 came
        // back derivative(1, 0).
        partial record Derivativef
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side);
        }

        partial record Integralf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Expression.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Expression,
                    Var.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim2 ? lim2 : Var,
                    Range is var (from, to)
                    ? (from.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim3 ? lim3 : from,
                       to.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim4 ? lim4 : to)
                    : null),
                    x, dist, side);
        }

        // The variable a limit is taken in is bound by it. Where that is the variable approaching, the inner limit does
        // not depend on it, and is its own limit; it is never a value to put a limit in: limit(sqrt(x) cos(x) / x, x, 0)
        // at 1 came back limit(cos(1), 1, 0).
        partial record Limitf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                if (Var == x)
                    return Destination.ContainsNode(x) ? null : this;
                return ComputeLimitImpl(New(
                    Expression.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Expression,
                    Var,
                    Destination.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim3 ? lim3 : Destination,
                    ApproachFrom),
                x, dist, side);
            }
        }

        partial record Signumf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (Argument.ComputeLimitDivideEtImpera(x, dist, side) is not { } argument)
                    return null;
                // The sign is constant on either side of zero, so wherever the argument tends
                // to anything but zero the limit is simply the sign of that -- including the
                // infinities, where it is 1 and -1. At zero the sign is 1 on one side and -1 on
                // the other, and which one a one-sided limit takes depends on the direction the
                // argument approaches from rather than only on what it tends to.
                //
                // Where that direction cannot be read, nothing is returned rather than an
                // unevaluated limit of this very expression. That is what used to be here, and it
                // does not merely fail to
                // answer -- the two-sided path compares its two one-sided results by evaluating
                // them, evaluating a limit computes it, and computing it arrives back here. The
                // recursion ends by overflowing the stack, which kills the process rather than
                // raising anything a caller could catch:
                // https://github.com/asc-community/AngouriMath/issues/704. Null says the same
                // thing to the caller, which hands back an unevaluated limit of its own.
                if (argument.Evaled is not Number value || value.Evaled == MathS.NaN)
                    return null;
                // At zero the side the argument arrives from is the sign, where it can be read:
                // sgn(x^3 - x) at 0+ is -1, from below. A two-sided limit is its two sides, each
                // asked of this, and is not answered here.
                // https://github.com/asc-community/AngouriMath/issues/2266
                if (value == 0)
                    return SignNear(Argument, x, dist, side) is { } arrivesFrom ? Integer.Create(arrivesFrom) : null;
                return new Signumf(argument);
            }
        }

        partial record Absf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                var direct = Argument.ComputeLimitDivideEtImpera(x, dist, side)?.Abs();
                if (direct is { } found && found.Evaled != MathS.NaN)
                    return found;
                // The modulus of a quotient is the quotient of the moduli, and of a product the product,
                // for complex values as for real ones. Read that way, a divisor that vanishes vanishes from
                // above: abs(1/x^pi) at 0 from the left had no limit, x^pi being complex there and its
                // sign unreadable, so the two-sided limit was NaN where it is +oo, as 1/abs(x)^pi is.
                Entity? split = Argument switch
                {
                    Divf(var dividend, var divisor) => dividend.Abs() / divisor.Abs(),
                    Mulf(var multiplier, var multiplicand) => multiplier.Abs() * multiplicand.Abs(),
                    _ => null
                };
                if (split?.ComputeLimitDivideEtImpera(x, dist, side) is { } bySplitting && bySplitting.Evaled != MathS.NaN)
                    return bySplitting;
                // And the argument's whole limit, where the descent reads a form off its parts: x * ln(x) is
                // 0 * -oo there, which is NaN, so abs(x * ln(x)) at 0 had no limit from either side, and the
                // two-sided limit was NaN, where it is 0 -- from the left too, |x| |ln|x| + i pi| tending to 0.
                // The modulus of a value is taken; x * ln(x) itself keeps the two-sided convention.
                if (direct?.Evaled == MathS.NaN
                    && AsTheWholeLimit(Argument, x, dist, side) is { } whole && whole.Evaled is Number { IsNaN: false })
                    return whole.Abs().InnerSimplified;
                return direct;
            }
        }

        /// <summary>
        /// The limit of a function that is constant between consecutive integers and jumps at
        /// each of them, given the limit of its argument.
        /// </summary>
        /// <remarks>
        /// Away from the jumps the function is locally constant, so the limit is simply the
        /// function of the argument's limit. On a jump the value differs on the two sides of it,
        /// and which side the argument arrives from is not decided by the side
        /// <paramref name="x"/> approaches its destination from -- lim(x -> 0+) floor(2 - x^2)
        /// reaches 2 from below and lim(x -> 0+) floor(2 + x^2) from above, and both are limits
        /// from the right. It is the sign the argument less the jump keeps on that side, which
        /// <see cref="SignNear"/> reads off the first derivative that does not vanish, and
        /// <paramref name="atAJump"/> gives the value from it. A two-sided limit is asked of
        /// each side, where the sign is not one sign, and is not answered here.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2266">#2266</a>
        /// <para/>
        /// Null is returned where the sign cannot be read, rather than an unevaluated limit of
        /// the very expression being asked about. The latter is what the inherited default does,
        /// and it does not merely
        /// fail to answer: the two-sided path compares its one-sided results by evaluating them,
        /// evaluating a limit computes it, and computing it arrives back here. The recursion
        /// ends by overflowing the stack, which kills the process rather than raising anything a
        /// caller could catch.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/829">#829</a> is that
        /// fault on these two nodes and
        /// <a href="https://github.com/asc-community/AngouriMath/issues/704">#704</a> was the
        /// same one on <see cref="Signumf"/>.
        /// </remarks>
        private static Entity? LimitOfAStepFunction(Entity argument, Variable x, Entity dist,
            ApproachFrom side, System.Func<Entity, Entity> rebuild, System.Func<Integer, int, Entity> atAJump)
        {
            if (argument.ComputeLimitDivideEtImpera(x, dist, side) is not { } limit)
                return null;
            if (limit.Evaled is not Number value || value.IsNaN)
                return null;
            // An infinity is its own floor and its own ceil, so it is not a jump.
            if (!value.IsFinite)
                return rebuild(limit);
            // Taken componentwise, as the evaluation is, so a jump in either part is a jump --
            // except that a real value's imaginary part is identically zero rather than
            // tending to zero, and the floor of a constant zero is a constant zero. Only a
            // genuinely complex limit has an imaginary part that can arrive at an integer from
            // one side or the other.
            //
            // The Absf and Signumf overrides above read the argument as real-valued along the path.
            // This one asks: an argument that is complex near the destination and real at it is
            // read below by the side its imaginary part arrives from.
            if (value is not Complex { RealPart: var real, ImaginaryPart: var imaginary })
                return null;
            // A real limit of an argument that is not real near the destination has an imaginary
            // part tending to 0, which is a jump of its own: floor(4^((-1)^x)) at 1 from the right
            // tends to 1/4 from below the real line, where every value's floor is -i, and the
            // floor of the limit, 0, was the answer. Where the argument is real near the
            // destination -- every argument this read as real before, sqrt(x) at 4 among them --
            // nothing is added; where it is not, the side the imaginary part arrives from decides
            // the step, and where that is not decided the limit is declined.
            Entity imaginaryStep = Integer.Zero;
            if (value is Real)
            {
                if (ImaginaryStep() is not { } step)
                    return null;
                imaginaryStep = step;
            }
            Entity WithTheImaginaryStep(Entity realAnswer)
                => imaginaryStep is Integer { IsZero: true } ? realAnswer : realAnswer + imaginaryStep * MathS.i;
            if (SitsOnAJump(real))
                return value is Integer jump && SignNear(argument - jump, x, dist, side) is { } arrivesFrom
                    ? WithTheImaginaryStep(atAJump(jump, arrivesFrom))
                    : null;
            if (value is not Real && SitsOnAJump(imaginary))
                return null;
            return WithTheImaginaryStep(rebuild(limit));

            // Real near the destination where it is real at a point just off it on each side
            // approached: realness is not decided for every shape -- x^x is outside what
            // TreeAnalyzer lists -- and a sample only ever sends the limit to the question of the
            // imaginary part's side, or to declining, never to an answer of its own.
            Entity? ImaginaryStep()
            {
                if (Functions.TreeAnalyzer.IsRealValued(argument, x) || IsRealJustOff())
                    return Integer.Zero;
                return SignNear(MathS.Im(argument), x, dist, side) is { } arrivesFrom && arrivesFrom != 0
                    ? atAJump(Integer.Zero, arrivesFrom)
                    : null;
            }

            bool IsRealJustOff()
            {
                var offsets = side switch
                {
                    ApproachFrom.Left => new[] { -1 },
                    ApproachFrom.Right => new[] { 1 },
                    _ => new[] { -1, 1 },
                };
                if (dist == Real.PositiveInfinity || dist == Real.NegativeInfinity)
                    return AtALargeDecimal(dist == Real.PositiveInfinity ? argument : argument.Substitute(x, -x), x) is Real;
                foreach (var offset in offsets)
                    if (argument.Substitute(x, dist + Rational.Create(offset, 1000000)).Evaled is not Real)
                        return false;
                return true;
            }

            static bool SitsOnAJump(Real part) =>
                part is Integer || part.EDecimal.CompareTo(part.EDecimal.Floor()) == 0;
        }

        partial record Floorf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => LimitOfAStepFunction(Argument, x, dist, side, static a => new Floorf(a),
                    static (jump, arrivesFrom) => arrivesFrom > 0 ? jump : jump - 1);
        }

        partial record Ceilf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => LimitOfAStepFunction(Argument, x, dist, side, static a => new Ceilf(a),
                    static (jump, arrivesFrom) => arrivesFrom > 0 ? jump + 1 : jump);
        }

        partial record Providedf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                var lim = Expression.ComputeLimitDivideEtImpera(x, dist, side);
                if (lim is null)
                    return null;
                return New(lim, Predicate);
            }
        }

        partial record Piecewise
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                // Close enough to the destination one case is the whole of the expression, so the
                // limit is that case's limit -- and which case that is is decided the same way
                // evaluation decides it, by the first predicate that holds once every earlier one
                // has failed. A limit of the cases with their predicates carried along was what
                // used to be built here, and it answers nothing: the predicates still speak about
                // x, which the limit has just got rid of.
                foreach (var (expression, predicate) in Cases)
                    switch (HoldsNear(predicate, x, dist, side))
                    {
                        case true: return ComputeLimit(expression, x, dist, side);
                        case false: continue;
                        // A case that may or may not hold near the destination leaves it open
                        // which expression the limit is of, and the ones after it unreachable.
                        default: return null;
                    }
                // Every case fails throughout the neighbourhood, so the expression is undefined
                // on the whole of the way in and there is nothing for it to tend to.
                return MathS.NaN;
            }
        }

        partial record Application
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => null;
        }

        partial record Lambda
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
                => null;
        }
    }
}
