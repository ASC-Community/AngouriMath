//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    internal static partial class Patterns
    {
        [AddressableRules]
        internal static Entity DivisionPreparingRules(Entity x) => x switch
        {
            Mulf(var any1, Divf(Integer(1), var any2)) => any1 / any2,
            Divf(Mulf(Number const1, var any1), var any2) => const1 * (any1 / any2),
            Mulf(Divf(Number const1, var any1), var any2) => const1 * (any2 / any1),
            _ => x
        };

        [AddressableRules]
        internal static Entity NumericNeatRules(Entity x) => x switch
        {
            // (-a) + (-b) is -(a + b), and the operands are already negative here, so what is
            // added is their magnitudes. Written as -(left + right) this negated a sum that was
            // negative already and answered `2` for `-1 + -1` -- invisible through Simplify,
            // where evaluation folds a pair of numerals before this is ever consulted, and
            // plainly wrong through RewriteRules.NumericNeat, which is public.
            // https://github.com/asc-community/AngouriMath/issues/936
            Sumf(Real { IsNegative: true } left, Real { IsNegative: true } right)
                => -((-left) + (Entity)(-right)),
            Sumf(var any1, Real { IsNegative: true } right) => any1 - -right,
            Sumf(Real { IsNegative: true } left, var any1) => any1 - -left,

            // (-a) - (-b) is b - a, not a + b.
            Minusf(Real { IsNegative: true } left, Real { IsNegative: true } right)
                => (-right) - (Entity)(-left),
            Minusf(var any1, Real { IsNegative: true } right) => any1 + -right,
            Minusf(Real { IsNegative: true } left, var any1) => -(any1 + -left),

            Mulf(Real { IsNegative: true } num1, Real { IsNegative: true } num2) => (-num1) * (Entity)(-num2),
            Divf(Real { IsNegative: true } num1, Real { IsNegative: true } num2) => (-num1) / (Entity)(-num2),

            // Each of these eight declines a factor of -1: its magnitude is 1, so taking it out
            // leaves a literal `1 *` that says nothing, and the numerator arms and the denominator
            // arms then undo each other around it -- -x / (-y) grew by four nodes a pass for ever.
            // -1 is not a factor to take out, it is the sign, and the two arms above answer it.
            // https://github.com/asc-community/AngouriMath/issues/1167
            Mulf(Mulf(Real { IsNegative: true } num, var any1), var any2) when num != -1 => -((-num) * (any1 * any2)),
            Mulf(Mulf(var any1, Real { IsNegative: true } num), var any2) when num != -1 => -((-num) * (any1 * any2)),
            Divf(Mulf(Real { IsNegative: true } num, var any1), var any2) when num != -1 => -((-num) * (any1 / any2)),
            Divf(Mulf(var any1, Real { IsNegative: true } num), var any2) when num != -1 => -((-num) * (any1 / any2)),
            Mulf(var any2, Mulf(Real { IsNegative: true } num, var any1)) when num != -1 => -((-num) * (any1 * any2)),
            Mulf(var any2, Mulf(var any1, Real { IsNegative: true } num)) when num != -1 => -((-num) * (any1 * any2)),
            // A negative factor under the line comes out as a sign in front, and what is left
            // stays under the line: a / (-b * c) is -(a / (b * c)), not -(b * (c / a)). Written
            // the latter way -- as the four rules above it are, where the negative factor is in
            // the numerator and multiplying by it is right -- the quotient came back inverted,
            // and Simplify takes whichever candidate is shortest, so the inverted one won
            // wherever it was smaller: 1 / (x * (-1 - 1/x)) simplified to -(1 + x), which is its
            // reciprocal.
            Divf(var any2, Mulf(Real { IsNegative: true } num, var any1)) when num != -1 => -(any2 / ((-num) * any1)),
            Divf(var any2, Mulf(var any1, Real { IsNegative: true } num)) when num != -1 => -(any2 / ((-num) * any1)),

            _ => x
        };

        /// <summary>
        /// Whether this is <c>1/n</c> or <c>-1/n</c> for a whole <c>n</c> other than 1 — the
        /// coefficient the two "a rational factor is a division" rules key on.
        /// </summary>
        /// <remarks>
        /// Written once and asked by both the <c>switch</c> and <c>MatchedRules.Common</c>, so the
        /// two forms cannot come to read <c>Rational(var one, var den)</c> differently. An
        /// <see cref="Integer"/> deconstructs with a denominator of 1 and is excluded by it.
        /// </remarks>
        internal static bool IsWholeReciprocal(Entity entity, int numerator)
            => entity is Rational(var num, var den) && num == numerator && den != 1;

        /// <summary>The denominator of a rational, for the rules <see cref="IsWholeReciprocal"/> admits.</summary>
        internal static Integer DenominatorOf(Entity entity)
            => entity is Rational(_, var den) ? den : Integer.One;

        [AddressableRules]
        internal static Entity CommonRules(Entity x) => x switch
        {
            // (a * f(x)) * g(x) = a * (f(x) * g(x))
            Mulf(Mulf(Number const1, Function func1), Function func2) => func1 * func2 * const1,

            // (a/b) * (c/d) = (a*c)/(b*d)
            Mulf(Divf(var any1, var any2), Divf(var any3, var any4)) => any1 * any3 / (any2 * any4),

            // a / (b / c) = a * c / b
            Divf(var any1, Divf(var any2, var any3)) => any1 * any3 / any2,

            // a / b * c = a * c / b
            Mulf(Divf(var any1, var any2), var any3) => any1 * any3 / any2,

            // a * {1} / b
            //
            // Not for const1 = -1, which is not a numeric factor to collect but the sign, spelled
            // as a product because that is how the language spells a negation. Collecting it turns
            // -x / 2 into -1/2 * x, and `a-negated-reciprocal-rational-factor-is-a-negated-division`
            // turns that straight back -- the two are exact inverses on this shape, and the pair
            // is what made this set the one rule set with no fixed point.
            // https://github.com/asc-community/AngouriMath/issues/1056
            Divf(Mulf(Number const1, var any1), Number const2) when const1 != -1
                => const1 / const2 * any1,

            // a / b / c = a / (b * c)
            Divf(Divf(var any1, var any2), var any3) => any1 / (any2 * any3),

            // a * (b / c) = (a * b) / c
            Mulf(var any1, Divf(var any2, var any3)) => any1 * any2 / any3,

            // (a * f(x)) * b = (a * b) * f(x)
            Mulf(Mulf(Number const1, Function func1), Number const2) => const1 * const2 * func1,
            Mulf(Number const2, Mulf(Number const1, Function func1)) => const1 * const2 * func1,

            // (a * f(x)) * (b * g(x)) = (a * b) * (f(x) * g(x))
            Mulf(Mulf(Number const1, Function func1), Mulf(Number const2, Function func2)) =>
                func1 * func2 * (const1 * const2),

            // (f(x) + {}) + g(x) = (f(x) + g(x)) + {}
            Sumf(Sumf(Function func1, var any1), Function func2) => func1 + func2 + any1,

            // g(x) + (f(x) + {}) = (f(x) + g(x)) + {}
            Sumf(Function func2, Sumf(Function func1, var any1)) => func1 + func2 + any1,

            // x * a = a * x
            Mulf(Variable var1, Number const1) => const1 * var1,

            // a + x = x + a
            Sumf(Number const1, Variable var1) => var1 + const1,

            // f(x) * a = a * f(x)
            Mulf(Function func1, Number const1) => const1 * func1,

            // a + f(x) = f(x) + a
            Sumf(Number const1, Function func1) => func1 + const1,

            // a * x + b * x = (a + b) * x
            Sumf(Mulf(Number const1, Variable var1), Mulf(Number const2, Variable var1a))
                when var1 == var1a => (const1 + const2) * var1,

            // a * x - b * x = (a - b) * x
            Minusf(Mulf(Number const1, Variable var1), Mulf(Number const2, Variable var1a))
                when var1 == var1a => (const1 - const2) * var1,

            // {1} * {2} + {1} * {3} = {1} * ({2} + {3})
            Sumf(Mulf(var any1, var any2), Mulf(var any1a, var any3)) when any1 == any1a => any1 * (any2 + any3),
            Sumf(Mulf(var any2, var any1), Mulf(var any1a, var any3)) when any1 == any1a => any1 * (any2 + any3),
            Sumf(Mulf(var any1, var any2), Mulf(var any3, var any1a)) when any1 == any1a => any1 * (any2 + any3),
            Sumf(Mulf(var any2, var any1), Mulf(var any3, var any1a)) when any1 == any1a => any1 * (any2 + any3),
            Sumf(var any1, Mulf(var any1a, var any2)) when any1 == any1a => any1 * (1 + any2),
            Sumf(var any1, Mulf(var any2, var any1a)) when any1 == any1a => any1 * (1 + any2),
            Sumf(Mulf(var any1, var any2), var any1a) when any1 == any1a => any1 * (1 + any2),
            Sumf(Mulf(var any2, var any1), var any1a) when any1 == any1a => any1 * (1 + any2),
            Sumf(var any1, var any1a) when any1 == any1a => 2 * any1,

            Minusf(Mulf(var any1, var any2), Mulf(var any1a, var any3)) when any1 == any1a => any1 * (any2 - any3),
            Minusf(Mulf(var any2, var any1), Mulf(var any1a, var any3)) when any1 == any1a => any1 * (any2 - any3),
            Minusf(Mulf(var any1, var any2), Mulf(var any3, var any1a)) when any1 == any1a => any1 * (any2 - any3),
            Minusf(Mulf(var any2, var any1), Mulf(var any3, var any1a)) when any1 == any1a => any1 * (any2 - any3),
            Minusf(var any1, Mulf(var any1a, var any2)) when any1 == any1a => any1 * (1 - any2),
            Minusf(var any1, Mulf(var any2, var any1a)) when any1 == any1a => any1 * (1 - any2),
            Minusf(Mulf(var any1, var any2), var any1a) when any1 == any1a => any1 * (any2 - 1),
            Minusf(Mulf(var any2, var any1), var any1a) when any1 == any1a => any1 * (any2 - 1),
            Minusf(var any1, var any1a) when any1 == any1a => Integer.Create(0).Provided(any1.DomainCondition),

            // {1} / {2} + {1} * {3} = {1} * (1 / {2} + {3})
            Sumf(Divf(var any1, var any2), Mulf(var any1a, var any3)) when any1 == any1a => any1 * (1 / any2 + any3),
            Sumf(Divf(var any1, var any2), Mulf(var any3, var any1a)) when any1 == any1a => any1 * (1 / any2 + any3),
            Sumf(Mulf(var any2, var any1), Divf(var any1a, var any3)) when any1 == any1a => any1 * (any2 + 1 / any3),
            Sumf(Mulf(var any1, var any2), Divf(var any1a, var any3)) when any1 == any1a => any1 * (any2 + 1 / any3),
            Sumf(var anyButNot1 and not Integer(1), Divf(var anyButNot1a, var any2))
                when anyButNot1 == anyButNot1a => anyButNot1 * (1 + 1 / any2),
            Sumf(Divf(var anyButNot1 and not Integer(1), var any2), var anyButNot1a)
                when anyButNot1 == anyButNot1a => anyButNot1 * (1 + 1 / any2),

            // x * x = x ^ 2
            Mulf(var any1, var any1a) when any1 == any1a => new Powf(any1, 2),

            // (a * x) * b
            Mulf(Mulf(Number const1, Variable var1), Number const2) => const1 * const2 * var1,
            Mulf(Number const2, Mulf(Number const1, Variable var1)) => const1 * const2 * var1,

            // (x + a) + b
            Sumf(Sumf(Variable var1, Number const1), Number const2) => var1 + (const1 + const2),

            // b + (x + a)
            Sumf(Number const2, Sumf(Variable var1, Number const1)) => var1 + (const1 + const2),

            // x * a * x
            Mulf(var any1, Mulf(var any1a, var any2)) when any1 == any1a => new Powf(any1, 2) * any2,
            Mulf(var any1, Mulf(var any2, var any1a)) when any1 == any1a => new Powf(any1, 2) * any2,
            Mulf(Mulf(var any1, var any2), var any1a) when any1 == any1a => new Powf(any1, 2) * any2,
            Mulf(Mulf(var any2, var any1), var any1a) when any1 == any1a => new Powf(any1, 2) * any2,

            // -1 * {1} + {2} = {2} - {1}
            Sumf(Mulf(Integer(-1), var any1), var any2) => any2 - any1,
            Sumf(var any1, Mulf(Integer(-1), var any2)) => any1 - any2,

            // (x - {}) (x + {}) = x2 - {}2
            Mulf(Minusf(Variable var1, var any1), Sumf(Variable var1a, var any1a))
                when (var1, any1) == (var1a, any1a) => new Powf(var1, 2) - new Powf(any1, 2),
            Mulf(Sumf(Variable var1, var any1), Minusf(Variable var1a, var any1a))
                when (var1, any1) == (var1a, any1a) => new Powf(var1, 2) - new Powf(any1, 2),

            // a / a. Two assumptions, not one: that a is not zero, and that a has a value at all.
            // `ln(x) / ln(x)` is undefined at x = 0 and `1` is not, and `not ln(x) = 0` does not
            // exclude it -- ln(0) is -oo, so the comparison is a definite false and the condition
            // holds. https://github.com/asc-community/AngouriMath/issues/1174
            Divf(var any1, var any1a) when any1 == any1a =>
                Integer.One.Provided(!any1.EqualTo(Integer.Zero) & any1.DomainCondition),

            // (a * c) / c. Two assumptions, not one: that c is not zero, and that c has a value
            // at all. https://github.com/asc-community/AngouriMath/issues/1174
            Divf(Mulf(var any1, var any2), var any2a) when any2 == any2a =>
                any1.Provided(!any2.EqualTo(Integer.Zero) & any2.DomainCondition),
            Divf(Mulf(var any2, var any1), var any2a) when any2 == any2a =>
                any1.Provided(!any2.EqualTo(Integer.Zero) & any2.DomainCondition),
            Divf(Mulf(var any1, var any2), Mulf(var any2a, var any3)) when any2 == any2a => (any1 / any3).Provided(!any2.EqualTo(Integer.Zero)),
            Divf(Mulf(var any1, var any2), Mulf(var any3, var any2a)) when any2 == any2a => (any1 / any3).Provided(!any2.EqualTo(Integer.Zero)),
            Divf(Mulf(var any2, var any1), Mulf(var any2a, var any3)) when any2 == any2a => (any1 / any3).Provided(!any2.EqualTo(Integer.Zero)),
            Divf(Mulf(var any2, var any1), Mulf(var any3, var any2a)) when any2 == any2a => (any1 / any3).Provided(!any2.EqualTo(Integer.Zero)),

            // ({1} - {2}) / ({2} - {1})
            Divf(Minusf(var any1, var any2), Minusf(var any2a, var any1a) denom) when (any1, any2) == (any1a, any2a) => new Providedf(-1, !denom.EqualTo(0)),

            // ({1} + {2}) / ({2} + {1})
            Divf(Sumf(var any1, var any2), Sumf(var any2a, var any1a) denom) when (any1, any2) == (any1a, any2a) => new Providedf(1, !denom.EqualTo(0)),

            // a / (b * {1})
            Divf(Number const1, Mulf(Number const2, var any1)) => const1 / const2 / any1,
            Divf(Number const1, Mulf(var any1, Number const2)) => const1 / const2 / any1,

            // a * (b * {}) = (a * b) * {}
            Mulf(Number const1, Mulf(Number const2, var any1)) => const1 * const2 * any1,

            Sumf(var any1, Sumf(var any2, var any1a)) when any1 == any1a => 2 * any1 + any2,
            Sumf(var any1, Sumf(var any1a, var any2)) when any1 == any1a => 2 * any1 + any2,
            Sumf(Sumf(var any2, var any1), var any1a) when any1 == any1a => 2 * any1 + any2,
            Sumf(Sumf(var any1, var any2), var any1a) when any1 == any1a => 2 * any1 + any2,

            Minusf(Sumf(var any1, var any2), var any1a) when any1 == any1a => any2,
            Minusf(Sumf(var any2, var any1), var any1a) when any1 == any1a => any2,
            Minusf(var any1, Sumf(var any2, var any1a)) when any1 == any1a => -any2,
            Minusf(var any1, Sumf(var any1a, var any2)) when any1 == any1a => -any2,

            Sumf(var any1, Minusf(var any2, var any1a)) when any1 == any1a => any2,
            Sumf(var any1, Minusf(var any1a, var any2)) when any1 == any1a => 2 * any1 - any2,
            Sumf(Minusf(var any2, var any1), var any1a) when any1 == any1a => any2,
            Sumf(Minusf(var any1, var any2), var any1a) when any1 == any1a => 2 * any1 - any2,

            Minusf(var any1, Minusf(var any2, var any1a)) when any1 == any1a => 2 * any1 - any2,
            Minusf(var any1, Minusf(var any1a, var any2)) when any1 == any1a => any2,
            Minusf(Minusf(var any2, var any1), var any1a) when any1 == any1a => any2 - 2 * any1,
            Minusf(Minusf(var any1, var any2), var any1a) when any1 == any1a => -any2,

            Mulf(Absf(var any1), Absf(var any2)) => new Absf(any1 * any2),
            Divf(Absf(var any1), Absf(var any2)) => new Absf(any1 / any2),
            // sgn(a) * a / abs(a) is 1 only for a real a: at a = i it is -1.
            Divf(Mulf(Signumf(var any1), Mulf(var any2, var any1a)), Absf(var any1b)) when any1 == any1a && any1a == any1b && OrderedCondition(any1) == Entity.Boolean.True => any2.Provided(!any1.EqualTo(Integer.Zero)),
            Mulf(Signumf(var any1), Absf(var any1a)) when any1 == any1a => any1,
            Mulf(Absf(var any1a), Signumf(var any1)) when any1 == any1a => any1,

            Mulf(var ratio, var any1) when IsWholeReciprocal(ratio, 1) => any1 / DenominatorOf(ratio),
            Mulf(var any1, var ratio) when IsWholeReciprocal(ratio, 1) => any1 / DenominatorOf(ratio),

            Mulf(var ratio, var any1) when IsWholeReciprocal(ratio, -1) => -(any1 / DenominatorOf(ratio)),
            Mulf(var any1, var ratio) when IsWholeReciprocal(ratio, -1) => -(any1 / DenominatorOf(ratio)),

            // Parity. Each of these holds on the whole complex plane, and the poles of the odd
            // ones sit symmetrically about zero -- tan(-z) is undefined exactly where tan(z)
            // is -- so the domain neither widens nor narrows and no condition is owed.
            //
            // What is matched is a product with a negative real coefficient, which is what
            // both `-x` and `-2 * x` are: the first is a coefficient of -1, and -1 is exactly
            // what the multiple-angle expansion skips, since that asks for |n| >= 2. So
            // cos(-2 * x) folded while cos(-x) did not, sin folded neither, and
            // sin(-x) + sin(x) did not reach zero.
            // https://github.com/asc-community/AngouriMath/issues/929
            //
            // The inverse functions are deliberately absent. arcsin and arctan are odd and
            // arccos is not, and this library's arccotan has range (-pi/2, pi/2] rather than
            // the textbook (0, pi), so each of them wants measuring before it is written down.
            Cosf(Mulf(Real { IsNegative: true } neg, var rest)) => new Cosf((-neg) * rest),
            Secantf(Mulf(Real { IsNegative: true } neg, var rest)) => new Secantf((-neg) * rest),
            Absf(Mulf(Real { IsNegative: true } neg, var rest)) => new Absf((-neg) * rest),
            // A numeric factor that carries a phase comes out as its modulus, |i * x| = |x|: the
            // modulus is multiplicative on the whole plane, so nothing is assumed, and it is what
            // lets a derivative and a limit read |i / x| as 1 / |x|. Only where the modulus is
            // exact, and never a real factor, which carries no phase.
            // https://github.com/asc-community/AngouriMath/issues/1186
            Absf(var argument) when TreeAnalyzer.TryTakeNumericModulusOut(argument, out var withoutThePhase) => withoutThePhase,

            Sinf(Mulf(Real { IsNegative: true } neg, var rest)) => -new Sinf((-neg) * rest),
            Tanf(Mulf(Real { IsNegative: true } neg, var rest)) => -new Tanf((-neg) * rest),
            Cotanf(Mulf(Real { IsNegative: true } neg, var rest)) => -new Cotanf((-neg) * rest),
            Cosecantf(Mulf(Real { IsNegative: true } neg, var rest)) => -new Cosecantf((-neg) * rest),
            Signumf(Mulf(Real { IsNegative: true } neg, var rest)) => -new Signumf((-neg) * rest),

            _ => x
        };
    }
}
