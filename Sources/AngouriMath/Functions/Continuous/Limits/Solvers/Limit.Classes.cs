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
                    var (mp, md) =
                        (Multiplier.ComputeLimitDivideEtImpera(x, dist, side), Multiplicand.ComputeLimitDivideEtImpera(x, dist, side)) switch
                        {
                            ({ } lim1, { } lim2) when IsDeterminate(New(lim1, lim2), x) => (lim1, lim2),
                            ({ IsFinite: true } lim1, { IsFinite: true } lim2) => (lim1, lim2),
                            (_, { } l2) when !Multiplier.ContainsNode(x) => (Multiplier, l2),
                            ({ } l1, _) when !Multiplicand.ContainsNode(x) => (l1, Multiplicand),
                            ({ IsFinite: true } lim1, { } exp) => (lim1, exp),
                            ({ } bas, { IsFinite: true } lim2) => (bas, lim2),
                            _ => (Multiplier, Multiplicand)
                        };
                    return ComputeLimitImpl(New(mp, md), x, dist, side);
                }
            }
        }

        partial record Divf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side)
            {
                if (ComputeLimitImpl(this, x, dist, side) is { } lim)
                    return lim;
                else
                {

                    var (dividend, divisor) =
                        (Dividend.ComputeLimitDivideEtImpera(x, dist, side), Divisor.ComputeLimitDivideEtImpera(x, dist, side)) switch
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
                    return DivergesAtAVanishingDivisor(Dividend, Divisor, x, dist, side) ?? substituted;
                }
            }
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

        partial record Sinf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cosf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Secantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cosecantf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
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
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
        }

        partial record Cotanf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
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
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Argument.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Argument),
                    x, dist, side);
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

        partial record Derivativef
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Expression.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Expression,
                    Var.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim2 ? lim2 : Var),
                    x, dist, side);
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

        partial record Limitf
        {
            internal override Entity? ComputeLimitDivideEtImpera(Variable x, Entity dist, ApproachFrom side) =>
                ComputeLimitImpl(this, x, dist, side) is { } lim ? lim
                : ComputeLimitImpl(New(
                    Expression.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim1 ? lim1 : Expression,
                    Var.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim2 ? lim2 : Var,
                    Destination.ComputeLimitDivideEtImpera(x, dist, side) is { IsFinite: true } lim3 ? lim3 : Destination,
                    ApproachFrom),
                x, dist, side);
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
                => Argument.ComputeLimitDivideEtImpera(x, dist, side)?.Abs();
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
            // Like the Absf and Signumf overrides above, this reads the argument as real-valued
            // along the path. An argument that is complex near the destination and real at it
            // can still be answered here when it should not be; that is the assumption those
            // two already make, and narrowing it wants a way to decide realness that this
            // library does not have yet -- https://github.com/asc-community/AngouriMath/issues/721.
            if (value is not Complex { RealPart: var real, ImaginaryPart: var imaginary })
                return null;
            if (SitsOnAJump(real))
                return value is Integer jump && SignNear(argument - jump, x, dist, side) is { } arrivesFrom
                    ? atAJump(jump, arrivesFrom)
                    : null;
            if (value is not Real && SitsOnAJump(imaginary))
                return null;
            return rebuild(limit);

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
