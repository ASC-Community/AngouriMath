//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    partial class Patterns
    {
        /// <summary>a ^ (-1) => 1 / a</summary>
        [AddressableRules]
        internal static Entity InvertNegativePowers(Entity expr) =>
            expr is Powf(var @base, Integer { IsNegative: true } pow)
            ? 1 / MathS.Pow(@base, -1 * pow)
            : expr;
        /// <summary>1 + (-x) => 1 - x, and -(a - b) => b - a</summary>
        [AddressableRules]
        internal static Entity InvertNegativeMultipliers(Entity expr) => expr switch
        {
            Sumf(var any1, Mulf(Real { IsNegative: true } const1, var any2))
                => any1 - (-1 * const1) * any2,

            // -(a - b) => b - a. A unary minus parses as (-1) * x, so this is the shape a negated
            // difference arrives in, and turning it round removes the multiplication and the
            // negative constant together: five nodes become three, and the metric charges four for
            // a negative real on top of that.
            //
            // What matters is where it runs rather than what it does. Expand already produced this
            // form for a whole expression, and Expand is offered for the root only and does not
            // descend into an exponent, a function's argument or a matrix -- so `-(5 - sqrt(-11))`
            // simplified while `2 ^ (-(5 - sqrt(-11)))`, `sgn(-(5 - sqrt(-11)))` and the same entry
            // inside a matrix did not. A rule runs everywhere.
            // https://github.com/asc-community/AngouriMath/issues/882
            Mulf(Integer(-1), Minusf(var subtrahend, var minuend))
                => minuend - subtrahend,

            _ => expr
        };

        /// <summary>
        /// Whether <c>(c / a)^d</c> may be written <c>c^d * a^(-d)</c> whatever <c>a</c> is.
        /// </summary>
        /// <remarks>
        /// For a whole <c>d</c> it is a quotient multiplied by itself, and holds wherever
        /// <c>a</c> is not zero. For a rational <c>d</c> with an odd denominator it holds over a
        /// positive <c>c</c>, because a negative base takes the real root there and real roots
        /// multiply: <c>(1/(-8))^(1/3)</c> is <c>-1/2</c>, and so is <c>(-8)^(-1/3)</c>. For any
        /// other <c>d</c> a negative <c>a</c> moves the branch -- <c>(1/(-2))^(1/2)</c> is
        /// <c>0.707i</c> and <c>(-2)^(-1/2)</c> is <c>-0.707i</c> -- and a negative or complex
        /// <c>c</c> does the same to the odd root, so the rule refuses there rather than assume
        /// the sign of <c>a</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1734
        /// </remarks>
        internal static bool AReciprocalPowerSplits(Number c, Number d) =>
            d is Integer || IsWholeOrAnOddRoot(d) && c is Real { IsPositive: true };

        /// <summary>
        /// A whole number, or a rational whose denominator is odd: an exponent under which a
        /// negative base takes its real root, so that powers of it combine as they do on the
        /// positive axis.
        /// </summary>
        internal static bool IsWholeOrAnOddRoot(Number n) =>
            n is Integer || n is Rational r && !r.ERational.Denominator.IsEven;

        [AddressableRules]
        internal static Entity PowerRules(Entity x) => x switch
        {
            // {} / {} = 1 provided not {} = 0
            // Two assumptions, not one: that a is not zero, and that a has a value at all.
            // https://github.com/asc-community/AngouriMath/issues/1174
            Divf(var any1, var any1a) when any1 == any1a =>
                new Providedf(1, !any1.EqualTo(0) & any1.DomainCondition),

            // {1}^({2} / log({3}, {1})) = {3}^{2}
            Powf(var any1, Divf(var any2, Logf(var any3, var any1a))) when any1 == any1a => new Powf(any3, any2),

            // {} ^ n * {}
            Mulf(Powf(var any1, var any2), var any1a) when any1 == any1a => new Powf(any1, any2 + 1),
            Mulf(var any1, Powf(var any1a, var any2)) when any1 == any1a => new Powf(any1, any2 + 1),

            // {} ^ n * {} ^ m = {} ^ (n + m)
            Mulf(Powf(var any1, var any2), Powf(var any1a, var any3)) when any1 == any1a => new Powf(any1, any2 + any3),

            // {} ^ n / {} ^ m = {} ^ (n - m)
            Divf(Powf(var any1, var any2), Powf(var any1a, var any3)) when any1 == any1a => new Powf(any1, any2 - any3),

            // ({} ^ {}) ^ {} = {} ^ ({} * {})
            //
            // True for a positive base whatever the exponents, and for any base when the
            // outer exponent is a whole number -- (a^b)^3 is a^b * a^b * a^b however a is
            // signed. Outside those two it is false, and it was applied unconditionally:
            //
            //     sqrt(x^2)      came back as x,    which at -0.63 is -0.63 where it is 0.63
            //     (x^2)^(3/2)    came back as x^3,  which at -2 is -8 where it is 8
            //
            // https://github.com/asc-community/AngouriMath/issues/752
            Powf(Powf(var any1, var any2), var any3)
                when any3 is Integer || any1.Evaled is Real { IsPositive: true }
                => new Powf(any1, any2 * any3),

            // {1} ^ n * {2} ^ n = ({1} * {2}) ^ n
            //
            // The same condition the ({}^{})^{} rule above carries, and missing here for the
            // same reason -- both were written unconditionally and #752 only looked at one of
            // them. True for a whole n whatever the signs, since a^3 b^3 is (ab)(ab)(ab), and
            // for positive real bases whatever the exponent. Outside those two the two sides
            // can differ by a full turn of the argument:
            //
            //     sqrt(x) * sqrt(y)   came back as sqrt(x * y), and at x = y = -1 the first
            //                         is i * i = -1 while the second is sqrt(1) = 1
            //
            // https://github.com/asc-community/AngouriMath/issues/801
            // And declines the one shape the numeric-factor arm below undoes: that one takes a
            // numeric factor back out of `(c * a) ^ d` when d is numeric too, so
            // `1 ^ (-2) * x ^ (-2)` collected and expanded for ever. Where *both* bases are
            // numeric there is nothing to take out afterwards and this still fires, so
            // `2 ^ 2 * 3 ^ 2` collects and `3 ^ (-1/2) * 2 ^ (-1/2)` is still `sqrt(6) / 6`.
            // https://github.com/asc-community/AngouriMath/issues/1171
            Mulf(Powf(var any1, var any3), Powf(var any2, var any3a))
                when any3 == any3a
                     && (any3 is Integer
                         || (any1.Evaled is Real { IsPositive: true }
                             && any2.Evaled is Real { IsPositive: true }))
                     && !(any3 is Number && any1 is Number != any2 is Number)
                => new Powf(any1 * any2, any3),
            // Same condition, same reason -- sqrt(2) / sqrt(-3) is -0.8165i where
            // (2 / -3)^(1/2) is +0.8165i. https://github.com/asc-community/AngouriMath/issues/802
            //
            // This gathering was what let the limit machinery read a 1^oo out of a quotient,
            // so guarding it here used to cost `(x^2 + 1)^x / (x^2)^x` its limit. The limit
            // reader now recognises the quotient itself, where the identity is checkable
            // because there is a destination to be near and the bases can be required to be
            // positive on the way to it -- see ApplySecondRemarkable.
            Divf(Powf(var any1, var any3), Powf(var any2, var any3a))
                when any3 == any3a
                     && (any3 is Integer
                         || (any1.Evaled is Real { IsPositive: true }
                             && any2.Evaled is Real { IsPositive: true }))
                => new Powf(any1 / any2, any3),

            // {1} ^ n / {2} ^ (c * n) = ({1} / {2} ^ c) ^ n, and the same the other way up.
            //
            // The rule above pairs two powers by their exponents, and the ({}^{})^{} rule
            // further up rewrites (b^c)^n as b^(c*n) -- on the child, on the way up, so it
            // has already happened by the time the pair is looked at. Where it applies to
            // only one of the two, which is whenever only one base is itself a power, the
            // exponents stop matching and the pair is lost: (a^2)^x / (b^2)^x gathers,
            // because both sides moved together, while (a^2)^x / b^x does not.
            // These read that pair back. Restricted to a whole c so that nothing gains a
            // root it did not have -- b^c goes into the base, rather than the exponent
            // being divided. https://github.com/asc-community/AngouriMath/issues/740
            Divf(Powf(var any1, var any3), Powf(var any2, Mulf(Integer { IsPositive: true } const1, var any3a)))
                when any3 == any3a => new Powf(any1 / new Powf(any2, const1), any3),
            Divf(Powf(var any1, Mulf(Integer { IsPositive: true } const1, var any3)), Powf(var any2, var any3a))
                when any3 == any3a => new Powf(new Powf(any1, const1) / any2, any3),

            // x / x^n
            Divf(var any1, Powf(var any1a, var any2)) when any1 == any1a => new Powf(any1, 1 - any2),

            // x^n / x
            Divf(Powf(var any1, var any2), var any1a) when any1 == any1a => new Powf(any1, any2 - 1),

            // c ^ log(c, a) = a for a number other than 0 and 1, which are refused: 1 ^ log(1, a)
            // is 1 rather than a.
            Powf(Number const1, Logf(Number const1a, var any1)) when const1 == const1a && const1 != Integer.Zero && const1 != Integer.One => any1,

            // The same through a constant -- e, whose ln(a) is stored as log(e, a), and pi --
            // which is decidably neither 0 nor 1, so nothing is left to discharge. Not restricted
            // to the positive reals: on the principal branch ln(-3) is ln(3) + i*pi and
            // e^(ln(3) + i*pi) is -3; and at a = 0 both sides are 0, since ln(0) is -oo here and
            // e^(-oo) is 0. https://github.com/asc-community/AngouriMath/issues/994
            Powf(Constant base1, Logf(Constant base1a, var any1))
                when base1 == base1a => any1,

            // And through a symbol, where b != 0 and b != 1 cannot be decided and travel with the
            // answer as its condition: the logarithm's own domain condition, which the rewrite
            // would otherwise drop.
            Powf(var base1, Logf(var base1a, var any1))
                when base1 == base1a && base1 is not Number and not Constant
                => any1.Provided(!base1.EqualTo(0) & !base1.EqualTo(1)),

            // c ^ (k log(c, a)) = a ^ k, the same three ways, with the multiplier on either side
            // of the logarithm. https://github.com/asc-community/AngouriMath/issues/994
            Powf(Number const1, Mulf(var k, Logf(Number const1a, var any1))) when const1 == const1a && const1 != Integer.Zero && const1 != Integer.One => new Powf(any1, k),
            Powf(Number const1, Mulf(Logf(Number const1a, var any1), var k)) when const1 == const1a && const1 != Integer.Zero && const1 != Integer.One => new Powf(any1, k),
            Powf(Constant base1, Mulf(var k, Logf(Constant base1a, var any1))) when base1 == base1a => new Powf(any1, k),
            Powf(Constant base1, Mulf(Logf(Constant base1a, var any1), var k)) when base1 == base1a => new Powf(any1, k),
            Powf(var base1, Mulf(var k, Logf(var base1a, var any1)))
                when base1 == base1a && base1 is not Number and not Constant
                => new Powf(any1, k).Provided(!base1.EqualTo(0) & !base1.EqualTo(1)),
            Powf(var base1, Mulf(Logf(var base1a, var any1), var k))
                when base1 == base1a && base1 is not Number and not Constant
                => new Powf(any1, k).Provided(!base1.EqualTo(0) & !base1.EqualTo(1)),

            Mulf(Powf(var any1, var any3), Mulf(var any1a, var any2)) when any1 == any1a => new Powf(any1, any3 + 1) * any2,
            Mulf(Powf(var any1, var any3), Mulf(var any2, var any1a)) when any1 == any1a => new Powf(any1, any3 + 1) * any2,
            Mulf(Mulf(var any1, var any2), Powf(var any1a, var any3)) when any1 == any1a => new Powf(any1, any3 + 1) * any2,
            Mulf(Mulf(var any2, var any1), Powf(var any1a, var any3)) when any1 == any1a => new Powf(any1, any3 + 1) * any2,

            // (a * x) ^ c = a^c * x^c
            //
            // Taking a factor out from under a root needs that factor to be positive, or the
            // root to be a whole power. With a negative one it moves the branch: sqrt(-x)
            // became sqrt(-1) * sqrt(x) = i * sqrt(x), and at x = -0.63 the first is 0.7937
            // while the second is -0.7937 -- the negation, not the value. A positive constant
            // is safe whatever the sign of x, which is why the rule is narrowed rather than
            // removed. https://github.com/asc-community/AngouriMath/issues/752
            // And declines when the other factor is numeric as well, because then there is nothing
            // to take the factor out *of*: the shared-exponent arm above collects those back
            // together, and the two looped. `(2 * 3) ^ 2` folds to 36 on its own.
            // https://github.com/asc-community/AngouriMath/issues/1171
            Powf(Mulf(Number const1, var any1), Number const2)
                when (const2 is Integer || const1 is Real { IsPositive: true })
                     && any1 is not Number =>
                new Powf(const1, const2) * new Powf(any1, const2),

            // {1} ^ (-1) = 1 / {1}
            Powf(var any1, Integer(-1)) => 1 / any1,

            // (a / {})^b * {} = a^b * {}^(1-b)
            //
            // Splitting the power of the quotient is (c / a)^d = c^d * a^(-d), which is not true
            // on the whole plane: see AReciprocalPowerSplits. sqrt(1/x) * sqrt(x) came back as 1,
            // and at x = -2 it is -1.
            Mulf(Powf(Divf(Number const1, var any1), Number const2), var any1a)
                when any1 == any1a && AReciprocalPowerSplits(const1, const2) =>
                new Powf(const1, const2) * new Powf(any1, 1 - const2),
            Mulf(Powf(Divf(Number const1, var any1), Number const2), Powf(var any1a, Number const3))
                when any1 == any1a && AReciprocalPowerSplits(const1, const2) && (const2 is Integer || IsWholeOrAnOddRoot(const3))
                => new Powf(const1, const2) * new Powf(any1, const3 - const2),

            // {1} / {2} / {2}
            Divf(Divf(var any1, var any2), var any2a) when any2 == any2a =>
                any1 / new Powf(any2, 2),
            Divf(Divf(var any1, Powf(var any2, var any3)), var any2a) when any2 == any2a =>
                any1 / new Powf(any2, any3 + 1),
            Divf(Divf(var any1, var any2), Powf(var any2a, var any3)) when any2 == any2a =>
                any1 / new Powf(any2, any3 + 1),
            Divf(Divf(var any1, Powf(var any2, var any4)), Powf(var any2a, var any3)) when any2 == any2a =>
                any1 / new Powf(any2, any3 + any4),

            // x * {} ^ {} = {} ^ {} * x
            Mulf(Variable var1, Powf(var any1, var any2)) => new Powf(any1, any2) * var1,

            // log_b(a^c) = c * log_b(a) holds where c * ln(a) stays inside the strip
            // Im in (-pi, pi] that ln maps onto, and not in general: ln(e^(3*pi*i)) is pi*i
            // while 3*pi*i is not, the two differing by exactly the 2*pi*i the principal branch
            // discards. A base that is a positive real makes ln(a) real, and a real exponent then
            // keeps the product real, so there is nothing to discard. Anything else is left as
            // written -- including a symbolic exponent under the default complex reading, where
            // the question is not decidable.
            // https://github.com/asc-community/AngouriMath/issues/902
            // Or the limit machinery is reading the expression towards a destination and has
            // established the base holds a positive sign on the way to it, which is the same
            // second way in that the logarithm gathering below takes.
            Logf(var any1, Powf(var any2, var any3))
                when MayTakeLogOfPower(any2, any3) => any3 * MathS.Log(any1, any2),
            // The same identity for a rational literal that is a power without saying so:
            // ln(16/9) is 2 ln(4/3), which is longer on its own and is what lets
            // ln(4/3) + ln(16/9) / 2 collect to 2 ln(4/3). A positive root only, so the
            // principal branch has nothing to discard; the metric decides whether the longer
            // form is kept. https://github.com/asc-community/AngouriMath/issues/1212
            Logf(var any1, Rational literal)
                when TreeAnalyzer.TryPerfectPower(literal, out var root, out var exponent)
                => Integer.Create(exponent) * MathS.Log(any1, root),
            // log_b(b) is 1 wherever it is defined at all, so the condition to carry is the
            // node's own and not one written out here. Asserting `any1 > 0` stated the real
            // reading inside the rule and was wrong in both directions at once: undefined at
            // x = -3, where log(-3, -3) evaluates to 1, and defined at x = 1, where
            // log(1, 1) is NaN. Reading the condition instead gets both, and follows the
            // reading rather than fixing it.
            // https://github.com/asc-community/AngouriMath/issues/721
            Logf(var any1, var any1a) logarithm when any1 == any1a
                => new Providedf(1, logarithm.DomainCondition),
            // ln(1/b) = -ln(b) is false on the negative reals, for the reason the pair of rules
            // below is guarded for: the principal argument does not negate with its logarithm.
            // At b = -0.63, ln(1/b) is 0.462 + pi*i and -ln(b) is 0.462 - pi*i. These three were
            // applied unconditionally while their neighbours ten lines down carried a guard, and
            // `Sources/Tests/Harnesses/RuleCheck` reports them once its corpus is wide enough to build the shape.
            //
            // The condition is the same one, and is asked through the same helper: ln(1/b) is
            // ln(1) - ln(b), so a reciprocal is the difference case with a numerator of 1. Both
            // ways of earning it carry over -- the operand is decidably a positive real, or the
            // limit machinery is reading towards a destination and has established the sign.
            // https://github.com/asc-community/AngouriMath/issues/721
            Logf(Divf(Integer(1), var any1), Divf(Integer(1), var any2))
                when MayGatherLogarithms(Integer.One, any1, isDifference: true)
                     && MayGatherLogarithms(Integer.One, any2, isDifference: true)
                => MathS.Log(any1, any2),
            Logf(var any1, Divf(Integer(1), var any2))
                when MayGatherLogarithms(Integer.One, any2, isDifference: true)
                => -MathS.Log(any1, any2),
            Logf(Divf(Integer(1), var any1), var any2)
                when MayGatherLogarithms(Integer.One, any1, isDifference: true)
                => -MathS.Log(any1, any2),
            

            // ln(a) + ln(b) = ln(a*b), and the difference likewise, are false off the positive
            // reals: at x = -3 the sum of ln(x) and ln(x+1) exceeds ln(x*(1+x)) by 2*pi*i, the
            // turn of the argument the principal branch discards. Both were applied
            // unconditionally, and that was the last disagreement `boundcheck` reported.
            //
            // Two ways to earn them. Either the operands are decidably positive here, or the
            // limit machinery is reading the expression towards a destination and has
            // established that they hold their sign on the way to it -- which is the only thing
            // that can discharge the condition for a symbol, and is what the identity is for.
            // Withdrawing it outright does not merely cost coverage: the limit machinery's own
            // expansion creates the pairs, so nothing puts them back and some limits stop
            // terminating. https://github.com/asc-community/AngouriMath/issues/721
            Sumf(Logf(var any3, var any1), Logf(var any3a, var any2)) when any3 == any3a
                && MayGatherLogarithms(any1, any2, isDifference: false) => any3.Log(any1 * any2),
            Minusf(Logf(var any3, var any1), Logf(var any3a, var any2)) when any3 == any3a
                && MayGatherLogarithms(any1, any2, isDifference: true) => any3.Log(any1 / any2),

            // sqrt(8) = 2 * sqrt(2), cbrt(54) = 3 * cbrt(2)
            Powf(Integer { IsPositive: true } radicand, Rational and not Integer and var power)
                when ReduceRadical(radicand, power) is { } reduced => reduced,

            // sqrt(5 + 2*sqrt(6)) = sqrt(2) + sqrt(3). The rule above takes a whole power out
            // from under one root; this one takes a root out from under another, which is the
            // nesting rather than the size.
            Powf(var radicand, Rational half)
                when half.ERational.Equals(ERational.Create(EInteger.One, EInteger.FromInt32(2)))
                    && DenestRadical(radicand) is { } denested => denested,

            _ => x
        };

        /// <summary>
        /// Gathers the factors of a product that are powers of one base, wherever they sit
        /// in it: <c>a^n * c * a^m</c> becomes <c>a^(n+m) * c</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="PowerRules"/> already has <c>{}^n * {}^m = {}^(n+m)</c>, but it pairs
        /// two sibling nodes, and a product is a tree rather than a list -- in
        /// <c>sin(x)^4 * (-6) * sin(x)^2</c> the constant sits between the two powers, so
        /// they are never siblings and the rule never fires. Full <see cref="Entity.Simplify"/>
        /// gets these because it reassociates and sorts the factors first; nothing that
        /// normalises without sorting does.
        /// </para>
        /// <para>
        /// <c>a^n * a^m = a^(n+m)</c> needs no condition: with <c>a^n</c> read as
        /// <c>e^(n Log a)</c> on the principal branch, the two sides are
        /// <c>e^(n Log a) * e^(m Log a)</c> and <c>e^((n+m) Log a)</c>, which are equal for
        /// every complex <c>n</c> and <c>m</c>. This is what makes it unlike
        /// <c>(a^b)^c = a^(b*c)</c>, which moves the branch and is guarded above.
        /// The one point it does not cover is <c>a = 0</c> with a negative exponent, where
        /// <c>0^2 * 0^(-1)</c> is undefined and <c>0^1</c> is 0 -- so this is applied to
        /// integrands, where an antiderivative may differ on a measure-zero set, and not in
        /// <see cref="PowerRules"/>. https://github.com/asc-community/AngouriMath/issues/781
        /// </para>
        /// </remarks>
        internal static Entity GatherPowersOfOneBase(Entity x)
        {
            if (x is not (Mulf or Divf))
                return x;

            // Exponent null marks a factor that is carried through untouched.
            var factors = new List<(Entity Base, Entity? Exponent)>();
            var merged = false;
            foreach (var factor in Mulf.LinearChildren(x))
            {
                var (@base, exponent) = Decompose(factor);
                // Numeric factors are left to evaluation, which folds 2 * 2 into 4. Gathering
                // them here would write it as 2^2 and call that progress.
                if (@base is Number)
                {
                    factors.Add((factor, null));
                    continue;
                }
                var found = false;
                for (var i = 0; i < factors.Count; i++)
                    if (factors[i].Exponent is { } already && factors[i].Base == @base)
                    {
                        // Only the exponent is folded, never the product around it.
                        // InnerSimplified on the whole expression rewrites x^(-2) back into
                        // 1/x^2, and SolveAsPolynomialTerm turns a 1/x^n it is handed into
                        // Pow(x, -n) again -- the two normalisations chase each other until
                        // the stack runs out.
                        factors[i] = (@base, (already + exponent).InnerSimplified);
                        merged = found = true;
                        break;
                    }
                if (!found)
                    factors.Add((@base, exponent));
            }

            // Rebuilding unconditionally would rewrite every quotient in the tree as a
            // negative power, since LinearChildren flattens a / b into a * b^(-1).
            if (!merged)
                return x;

            Entity? result = null;
            foreach (var (@base, exponent) in factors)
            {
                // An exponent read off a factor below the bar is a tree, `3/2 * (-1)`, and is
                // written as the number it is: every integration rule reads a fractional
                // power as a `Powf` of a `Rational`, and `(tan(x) tan(2x))^(3/2)` below the bar
                // came through here as a power of `3/2 * (-1)` that none of them read --
                // Timofeev's 606, eight seconds of declines for what the rules answer in one.
                var written = exponent is null or Number ? exponent : exponent.Evaled is Number folded ? folded : exponent;
                var factor = written switch
                {
                    null => @base,
                    Integer(1) => @base,
                    _ => new Powf(@base, written)
                };
                result = result is null ? factor : result * factor;
            }
            return result ?? x;
        }

        /// <summary>
        /// Reads a factor of a product as a base and an exponent.
        /// </summary>
        /// <remarks>
        /// The nested case is not cosmetic: <see cref="Mulf.LinearChildren"/> writes a
        /// divisor as <c>(...)^(-1)</c>, so <c>x^3 / x^2</c> arrives as
        /// <c>x^3 * (x^2)^(-1)</c>, whose second factor has base <c>x^2</c> rather than
        /// <c>x</c> and would be read as an unrelated base. Unwrapping it is
        /// <c>(a^b)^n = a^(b*n)</c>, which holds for a whole <c>n</c> whatever the sign of
        /// <c>a</c> -- the same guard the <c>({}^{})^{}</c> rule above carries, and for the
        /// same reason. https://github.com/asc-community/AngouriMath/issues/752
        /// </remarks>
        /// <summary>
        /// Whether <paramref name="entity"/> is a real strictly above zero, decided rather than
        /// assumed. <c>ln</c> of such a number is a real, so a real multiple of it stays on the
        /// real line and inside <c>ln</c>'s principal strip.
        /// </summary>
        /// <remarks>
        /// Finiteness is checked separately because <see cref="Real.IsPositive"/> is
        /// <c>!IsNegative &amp;&amp; !IsZero</c>, which <c>NaN</c> and <c>+oo</c> both satisfy.
        /// </remarks>
        private static bool IsPositiveReal(Entity entity)
            => entity.Evaled is Real { EDecimal.IsFinite: true } value && value.IsPositive;

        /// <summary>
        /// Whether the exponent may be pulled out of a logarithm: because the base is a decidably
        /// positive number and the exponent may be taken as real, or because a limit is being read
        /// and the base holds a positive sign on the approach to its destination while the exponent
        /// is real along it.
        /// </summary>
        internal static bool MayTakeLogOfPower(Entity @base, Entity exponent)
            => (IsPositiveReal(@base) && MayBeTakenAsReal(exponent))
               || Algebra.LimitFunctional.MayTakeLogOfPowerHere(@base, exponent);

        /// <summary>
        /// Whether two antilogarithms may be gathered into one: because both are decidably
        /// positive numbers, or because a limit is being read and they hold their sign on the
        /// approach to its destination.
        /// </summary>
        internal static bool MayGatherLogarithms(Entity left, Entity right, bool isDifference)
            => (IsPositiveReal(left) && IsPositiveReal(right))
               || Algebra.LimitFunctional.MayGatherLogarithmsHere(left, right, isDifference);

        /// <summary>
        /// Whether this operand may be taken as real: because the expression is being read as a
        /// real-valued one, because the node's own declared codomain says so, or because its value
        /// is a real to begin with.
        /// </summary>
        /// <remarks>
        /// The first two are the disjunction <c>Patterns.EqualityInequality.cs</c> uses to ask the
        /// same question. A bare <see cref="Variable"/> is <c>Domain.Any</c>, so a symbol under the
        /// default complex reading answers <see langword="false"/> here -- which is the point.
        /// </remarks>
        private static bool MayBeTakenAsReal(Entity entity)
            => MathS.Settings.Codomain.Value is AngouriMath.Core.Domain.Real
               || IsKnownReal(entity)
               || entity.Evaled is Real { EDecimal.IsFinite: true };

        private static (Entity Base, Entity Exponent) Decompose(Entity factor)
        {
            if (factor is not Powf(var @base, var exponent))
                return (factor, 1);
            while (@base is Powf(var inner, var innerExponent)
                   && (exponent is Integer || inner.Evaled is Real { IsPositive: true }))
            {
                exponent = innerExponent * exponent;
                @base = inner;
            }
            return (@base, exponent);
        }

        /// <summary>
        /// Largest divisor tried when reducing a radical. Reducing sqrt(n) exactly would
        /// mean factoring n, which is not something a simplification pass can afford to
        /// do on every node. Trial division by small divisors catches every radical that
        /// turns up in practice; anything left stays under the root, which is still a
        /// correct answer, just not a fully reduced one.
        /// </summary>
        private const int MaxRadicalTrialDivisor = 1000;

        /// <summary>
        /// Rewrites <c>n ^ (p/q)</c> as <c>a ^ p * b ^ (p/q)</c> where <c>n = a^q * b</c>,
        /// i.e. pulls every q-th power out from under the root.
        /// </summary>
        /// <returns>
        /// <see langword="null"/> when nothing can be pulled out, so that the rule does
        /// not fire and rewriting terminates.
        /// </returns>
        internal static Entity? ReduceRadical(Integer radicand, Rational power)
        {
            var denominator = power.ERational.Denominator;
            if (denominator < 2 || denominator > 64)
                return null;
            var root = denominator.ToInt32Checked();

            var inside = radicand.EInteger;
            if (inside <= 1)
                return null;

            var outside = EInteger.One;
            for (int divisor = 2; divisor <= MaxRadicalTrialDivisor; divisor++)
            {
                var divisorPower = EInteger.FromInt32(divisor).Pow(root);
                if (divisorPower > inside)
                    break;
                while (inside.Remainder(divisorPower).IsZero)
                {
                    inside /= divisorPower;
                    outside *= EInteger.FromInt32(divisor);
                }
            }

            if (outside.Equals(EInteger.One))
                return null; // already reduced; firing here would loop forever

            // n^(p/q) = (a^q * b)^(p/q) = a^p * b^(p/q)
            return new Powf(Integer.Create(outside), Integer.Create(power.ERational.Numerator))
                 * new Powf(Integer.Create(inside), power);
        }

        /// <summary>
        /// The exact square root of a non-negative rational, or <see langword="null"/> where it
        /// has none. <c>4/9</c> gives <c>2/3</c>; <c>2</c> gives nothing.
        /// </summary>
        /// <remarks>
        /// A rational in lowest terms is a square exactly when its numerator and denominator both
        /// are, since a square's prime factorisation has even exponents throughout and the two
        /// share no prime. Asked of each separately, which is why there is no gcd here.
        /// </remarks>
        private static ERational? ExactSquareRoot(ERational value)
        {
            if (value.IsNegative || !value.IsFinite)
                return null;
            var numerator = value.Numerator.Sqrt();
            if (!numerator.Multiply(numerator).Equals(value.Numerator))
                return null;
            var denominator = value.Denominator.Sqrt();
            if (!denominator.Multiply(denominator).Equals(value.Denominator))
                return null;
            return ERational.Create(numerator, denominator);
        }

        /// <summary>
        /// <c>sqrt(a + b*sqrt(c))</c> written without the nesting, as <c>sqrt(x) +- sqrt(y)</c>,
        /// or <see langword="null"/> where it has no such form.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Squaring <c>sqrt(x) + sqrt(y)</c> gives <c>x + y + 2*sqrt(x*y)</c>, so matching it
        /// against <c>a + b*sqrt(c)</c> asks for <c>x + y = a</c> and <c>4*x*y = b^2*c</c> —
        /// which makes <c>x</c> and <c>y</c> the two roots of <c>t^2 - a*t + b^2*c/4</c>, namely
        /// <c>(a +- sqrt(a^2 - b^2*c))/2</c>. They are rational exactly when
        /// <c>d = a^2 - b^2*c</c> is the square of one, and that is the whole test: a decidable
        /// question in exact arithmetic rather than a search.
        /// </para>
        /// <para>
        /// <b>The sign of <c>b</c> chooses between the two forms.</b> Squaring gives
        /// <c>a + |b|*sqrt(c)</c> whichever way, since <c>2*sqrt(x*y)</c> is not negative — so a
        /// negative <c>b</c> is the difference <c>sqrt(x) - sqrt(y)</c>, which needs
        /// <c>x >= y</c> and gets it, <c>sqrt(d)</c> being non-negative.
        /// </para>
        /// <para>
        /// <b>Every condition reduces to two.</b> The identity needs <c>x</c> and <c>y</c>
        /// non-negative and the radicand itself non-negative; given <c>a >= 0</c> and
        /// <c>d >= 0</c>, all of them follow — <c>sqrt(d) &lt;= a</c> makes <c>y</c>
        /// non-negative, and <c>a - |b|*sqrt(c) >= 0</c> is <c>d >= 0</c> restated. So the rule
        /// asks for a non-negative <c>a</c> and a <c>d</c> that is a rational square, and needs
        /// nothing else.
        /// </para>
        /// <para>
        /// Returns null rather than the input where it does not apply, so the rule does not fire
        /// and the rewriting terminates — the same contract as
        /// <see cref="ReduceRadical(Integer, Rational)"/> above.
        /// </para>
        /// </remarks>
        internal static Entity? DenestRadical(Entity radicand)
        {
            // a + b*sqrt(c), read off the sum's terms: exactly one rational, exactly one
            // rational multiple of a root. Anything else is not this shape.
            ERational? rational = null;
            ERational? coefficient = null;
            ERational? under = null;
            foreach (var term in Sumf.LinearChildren(radicand))
            {
                if (term.Evaled is Rational whole)
                {
                    if (rational is not null)
                        return null;
                    rational = whole.ERational;
                    continue;
                }
                if (coefficient is not null || !TryReadRootTerm(term, out var scale, out var inside))
                    return null;
                coefficient = scale;
                under = inside;
            }

            if (rational is not { } a || coefficient is not { } b || under is not { } c)
                return null;
            if (a.IsNegative)
                return null;

            var discriminant = a.Multiply(a).Subtract(b.Multiply(b).Multiply(c));
            if (ExactSquareRoot(discriminant) is not { } root)
                return null;

            var two = ERational.FromInt32(2);
            var x = a.Add(root).Divide(two);
            var y = a.Subtract(root).Divide(two);

            // Nothing was nested to begin with if the inner root is a square, and this would
            // then compete with `ReduceRadical` over the same expression rather than answering
            // a question it left.
            if (ExactSquareRoot(c) is not null)
                return null;

            Entity first = MathS.Sqrt(Rational.Create(x)).InnerSimplified;
            Entity second = MathS.Sqrt(Rational.Create(y)).InnerSimplified;
            return b.IsNegative ? first - second : first + second;
        }

        /// <summary>
        /// A term of the form <c>k * sqrt(m)</c> with both rational, as its two parts.
        /// <c>sqrt(6)</c> is read with a <c>k</c> of one.
        /// </summary>
        /// <remarks>
        /// <b>Any half-integer exponent counts, not only one half.</b> The rules that run
        /// alongside this one gather <c>2 * sqrt(2)</c> into <c>2 ^ (3/2)</c>, so by the time a
        /// radicand reaches here its root has usually been folded into its coefficient — and a
        /// reader that insists on a written <c>sqrt</c> sees <c>3 + 2 ^ (3/2)</c> and declines a
        /// radicand it can perfectly well denest. <c>m ^ (p/2)</c> for odd <c>p</c> is
        /// <c>m ^ ((p - 1) / 2) * sqrt(m)</c>, which is the same two parts; an even <c>p</c> is
        /// not a root at all and is read as an ordinary rational factor by the case below.
        /// </remarks>
        private static bool TryReadRootTerm(Entity term, out ERational scale, out ERational inside)
        {
            scale = ERational.One;
            inside = ERational.Zero;
            var seenRoot = false;
            foreach (var factor in Mulf.LinearChildren(term))
                switch (factor)
                {
                    case Powf(var under, Rational exponent)
                        when exponent.ERational.Denominator.Equals(EInteger.FromInt32(2))
                            && under.Evaled is Rational { ERational.Sign: > 0 } radicand:
                        if (seenRoot)
                            return false;
                        seenRoot = true;
                        inside = radicand.ERational;
                        // p/2 with p odd, the denominator being two in lowest terms. The whole
                        // part comes out as a factor and the half stays under the root.
                        var half = exponent.ERational.Numerator
                            .Subtract(EInteger.One).Divide(EInteger.FromInt32(2));
                        scale = scale.Multiply(Pow(radicand.ERational, half));
                        break;
                    case var other when other.Evaled is Rational multiplier:
                        scale = scale.Multiply(multiplier.ERational);
                        break;
                    default:
                        return false;
                }
            return seenRoot;
        }

        /// <summary>A rational raised to a whole power, negative exponents included.</summary>
        private static ERational Pow(ERational value, EInteger exponent)
        {
            if (exponent.IsZero)
                return ERational.One;
            var magnitude = exponent.Abs().ToInt32Checked();
            var result = ERational.One;
            for (var i = 0; i < magnitude; i++)
                result = result.Multiply(value);
            return exponent.Sign < 0 ? ERational.One.Divide(result) : result;
        }
    }
}
