//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity.Set;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// <paramref name="interval"/> scaled by <paramref name="factor"/>, or
        /// <see langword="null"/> where the factor's sign is not known.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Scaling is monotone in the factor's sign and in nothing else, so this is the whole of
        /// it: a positive factor carries both ends where they were, and a negative one
        /// <b>reflects the interval</b> — the ends swap, and their openness swaps with them,
        /// exactly as subtracting an interval does. <c>(0; 1] * -2</c> is <c>[-2; 0)</c>: the
        /// included 1 becomes the included lower end -2, and the excluded 0 becomes the excluded
        /// upper end 0.
        /// </para>
        /// <para>
        /// <b>An unknown sign is answered by not answering.</b> <c>(0; 1) * k</c> for a symbolic
        /// <c>k</c> is one interval when <c>k</c> is positive and the reflected one when it is
        /// negative, and picking either would be choosing which. It is left alone, which is what
        /// an unevaluated node means.
        /// </para>
        /// <para>
        /// Zero never reaches here: <c>Mulf</c> answers a multiplication by zero before this is
        /// consulted, and gives the number <c>0</c> rather than the set <c>{ 0 }</c>. That is
        /// older than this and is left as it is — the arm it comes from is over every
        /// <see cref="Entity"/> and not only intervals.
        /// </para>
        /// </remarks>
        private static Entity? ScaledInterval(Interval interval, Entity factor, bool isExact)
            => factor.Evaled is Real { IsFinite: true } real && !real.IsZero
                ? real.IsPositive
                    ? interval.New(
                        (interval.Left * factor).InnerSimplified(isExact), interval.LeftClosed,
                        (interval.Right * factor).InnerSimplified(isExact), interval.RightClosed)
                    : interval.New(
                        (interval.Right * factor).InnerSimplified(isExact), interval.RightClosed,
                        (interval.Left * factor).InnerSimplified(isExact), interval.LeftClosed)
                : null;

        /// <summary>
        /// The value of <paramref name="expr"/> where it has one, with the condition it holds
        /// under, or <see langword="null"/> where there is no value to read.
        /// </summary>
        /// <remarks>
        /// The condition is what separates an argument that is nonzero *throughout its domain*
        /// from one that can itself vanish. <c>x / x</c> evaluates to <c>1 provided not x = 0</c>,
        /// so it is nonzero wherever it is defined at all and a rule may rely on that, carrying
        /// the condition forward; a bare <c>x</c> has no value and no such promise. Reading only
        /// the argument's <see cref="DomainCondition"/> conflates the two, which is how
        /// <c>sgn(abs(x))</c> came to be <c>1</c>
        /// (https://github.com/asc-community/AngouriMath/issues/892).
        /// </remarks>
        private static (Number.Complex Value, Entity Condition)? ValueWithCondition(Entity expr)
            => expr.Evaled switch
            {
                Number.Complex value => (value, Boolean.True),
                Providedf(Number.Complex value, var condition) => (value, condition),
                _ => null,
            };

        public partial record Number
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) => this;
        }

        // Each function and operator processing
        public partial record Sumf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Augend, Addend,
                    (augend, addend) => (augend, addend) switch
                    { 
                        (Complex a, Complex b) when !isExact => a + b,
                        (var n1, Integer(0)) => n1,
                        (Integer(0), var n2) => n2,
                        // A sum of sizes, one of them infinite, is the larger: aleph(0) + 1 is
                        // aleph(0), the Hilbert hotel. https://github.com/asc-community/AngouriMath/issues/1409
                        _ when Core.Sets.Cardinality.Absorbed(augend, addend) is { } larger => larger,
                        (Interval inter, var n2) when n2 is not Set => inter.New((inter.Left + n2).InnerSimplified(isExact), (inter.Right + n2).InnerSimplified(isExact)),
                        (var n2, Interval inter) when n2 is not Set => inter.New((n2 + inter.Left).InnerSimplified(isExact), (n2 + inter.Right).InnerSimplified(isExact)),
                        _ => null
                    },
                    (@this, a, b) => ((Sumf)@this).New(a, b), isExact);
        }
        public partial record Minusf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Minuend, Subtrahend,
                    (augend, addend) => (augend, addend) switch
                    {
                        (Complex a, Complex b) when !isExact => a - b,
                        (var n1, Integer(0)) => n1,
                        (Integer(0), var n2) => -n2,
                        // Sizes are not subtracted: aleph(0) - aleph(0) has no value.
                        // https://github.com/asc-community/AngouriMath/issues/1409
                        _ when Core.Sets.Cardinality.HasNoValue(augend, addend) => MathS.NaN,
                        (Interval inter, var n2) when n2 is not Set => inter.New((inter.Left - n2).InnerSimplified(isExact), (inter.Right - n2).InnerSimplified(isExact)),
                        // Subtracting an interval turns it round, so the ends swap and their
                        // openness swaps with them. `5 - (0; 1]` is `[4; 5)`: the excluded 1
                        // becomes the excluded lower end 4, and the included 0 becomes the
                        // included upper end 5. Written as `New(n2 - Left, n2 - Right)` it came
                        // back as `(5; 4]` -- an interval whose left end is above its right, which
                        // is empty, so `4.5 in (5 - (0; 1))` answered False.
                        (var n2, Interval inter) when n2 is not Set => inter.New(
                            (n2 - inter.Right).InnerSimplified(isExact), inter.RightClosed,
                            (n2 - inter.Left).InnerSimplified(isExact), inter.LeftClosed),
                        _ => null
                    },
                    (@this, a, b) => ((Minusf)@this).New(a, b), isExact);
        }
        public partial record Mulf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Multiplier, Multiplicand,
                    (a, b) => (a, b) switch
                    {
                        (Matrix m1, Matrix m2) when m1.ColumnCount == m2.RowCount => (m1 * m2).InnerSimplified(isExact),
                        (Matrix m1, Matrix m2) => a * b,
                        (Integer(-1), Mulf(Integer(-1), var any1)) => any1,
                        (Matrix m, Integer(0)) => m.With((_, _, _) => 0),
                        (Integer(0), Matrix m) => m.With((_, _, _) => 0),
                        (Complex n1, Complex n2) when !isExact => n1 * n2,
                        ({ DomainCondition: var condition }, Integer(0)) => Integer.Zero.Provided(condition),
                        (Integer(0), { DomainCondition: var condition }) => Integer.Zero.Provided(condition),
                        (var n1, Integer(1)) => n1,
                        (Integer(1), var n2) => n2,
                        // A product of sizes, one of them infinite and neither zero, is the
                        // larger. https://github.com/asc-community/AngouriMath/issues/1409
                        _ when Core.Sets.Cardinality.Absorbed(a, b) is { } larger => larger,
                        (var n1, var n2) when n1 == n2 => new Powf(n1, 2).InnerSimplified(isExact),
                        // After the zero arms above, which answer a multiplication by zero for
                        // every Entity and not only for an interval.
                        (Interval one, Interval another) => IntervalArithmetic.Product(one, another, isExact),
                        (Interval inter, var n2) when n2 is not Set => ScaledInterval(inter, n2, isExact),
                        (var n2, Interval inter) when n2 is not Set => ScaledInterval(inter, n2, isExact),
                        _ => null
                    },
                    (@this, a, b) => ((Mulf)@this).New(a, b), isExact);
        }
        public partial record Divf
        {
            // Division is undefined when the divisor equals zero
            private protected override Entity IntrinsicCondition => !Divisor.EqualTo(0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Dividend, Divisor,
                (a, b) => (a, b) switch
                {
                    (Integer(0), var n0) =>
                        n0.Evaled is Complex c
                        ? c.IsZero ? MathS.NaN : 0
                        : new Providedf(0, new Notf(new Equalsf(n0, 0))),
                    (_, Integer(0)) => Real.NaN,
                    (Complex n1, Complex n2) when !isExact => n1 / n2,
                    (var n1, Integer(1)) => n1,
                    // Sizes are not divided: aleph(0) / 2 has no value.
                    // https://github.com/asc-community/AngouriMath/issues/1409
                    _ when Core.Sets.Cardinality.HasNoValue(a, b) => MathS.NaN,
                    // A quotient by minus one is the negation, and a negation of a negation is
                    // the thing itself: the solver of `b - x = 0` answered `-b / (-1)` and the
                    // README printed it so.
                    (var n1, Integer(-1)) => (-n1).InnerSimplified(isExact),
                    // A radical in the denominator is the reciprocal power asked for outright,
                    // and answering it there is what makes 1/sqrt(2) and sqrt(1/2) the same
                    // thing on the page as well as in value.
                    // https://github.com/asc-community/AngouriMath/issues/205
                    (var n1, Powf(Rational radicand, Rational exponent)) when
                        RootExtraction.PullOutOfDenominator(radicand, exponent) is { } rationalized
                        => (n1 * rationalized).InnerSimplified(isExact),
                    // An interval over a constant is that constant's reciprocal scaling it. Only
                    // this direction: a constant over an interval that straddles zero is two
                    // unbounded pieces rather than one interval, so it is not an Interval at all
                    // and is left alone rather than answered wrongly.
                    (Interval inter, var n2) when n2 is not Set
                        => ScaledInterval(inter, (1 / n2).InnerSimplified(isExact), isExact),
                    // And the other direction where the divisor does not contain zero, which is
                    // when its reciprocal is an interval. https://github.com/asc-community/AngouriMath/issues/322
                    (Interval dividend, Interval divisor) => IntervalArithmetic.Quotient(dividend, divisor, isExact),
                    (var n1, Interval divisor) when n1 is not Set && IntervalArithmetic.Reciprocal(divisor, isExact) is Interval reciprocal
                        => ScaledInterval(reciprocal, n1, isExact),
                    _ => null
                },
                (@this, a, b) => ((Divf)@this).New(a, b), isExact);
        }
        public partial record Modf
        {
            // Like division, undefined when the divisor is zero
            private protected override Entity IntrinsicCondition => !Divisor.EqualTo(0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Dividend, Divisor,
                (a, b) => (a, b) switch
                {
                    (_, Integer(0)) => Real.NaN,
                    // Only for reals: the remainder of a complex number by another is not
                    // one thing, and guessing at it would be worse than leaving it alone.
                    //
                    // The operator is the floored remainder, which is this node's own reading of
                    // mod, so there is nothing left here to do by hand. It was not always: until
                    // https://github.com/asc-community/AngouriMath/issues/708 the three numeric
                    // types answered % three different ways, and this had to compute the answer
                    // rather than ask for it.
                    (Real n1, Real n2) when !isExact => n1 % n2,
                    (Integer(0), var n0) =>
                        n0.Evaled is Complex c
                        ? c.IsZero ? MathS.NaN : 0
                        : new Providedf(0, new Notf(new Equalsf(n0, 0))),
                    (var n1, Integer(1)) when n1.Evaled is Integer => 0,
                    // a % a = 0 wherever a % a means anything at all, which is wherever a is
                    // not zero -- and where it is, the whole node is undefined in any case.
                    (var n1, var n2) when n1 == n2 => new Providedf(0, new Notf(new Equalsf(n2, 0))),
                    // (a % b) % b = a % b: the first remainder already lies strictly between
                    // -|b| and |b|, so taking it again changes nothing.
                    (Modf(_, var inner) whole, var n2) when inner == n2 => whole,
                    _ => null
                },
                (@this, a, b) => ((Modf)@this).New(a, b), isExact);
        }
        public partial record Powf
        {
            // Power is undefined in two cases:
            // - 0^0 is indeterminate
            // - 0^(negative) is undefined (division by zero)
            //
            // And over the reals there is a third, which is the whole of what `sqrt` is: an even
            // root of a negative number is not real. `x ^ (1/2)` needs `x >= 0` and `x ^ (1/3)`
            // needs nothing, so the exponent's *denominator* decides it and only a literal
            // rational has one to read. A symbolic exponent is left with the condition above
            // rather than given a guess: too strict is a wrong answer here, since this is what a
            // rewrite consults before firing.
            // https://github.com/asc-community/AngouriMath/issues/721
            private protected override Entity IntrinsicCondition =>
                Codomain < Domain.Complex && IsEvenRoot
                ? Base >= 0 & (!Base.EqualTo(0) | Exponent > 0)
                : (!Base.EqualTo(0) | Exponent > 0);

            /// <summary>
            /// Whether the exponent is a literal rational whose denominator is even — the case
            /// where a negative base leaves the real line.
            /// </summary>
            private bool IsEvenRoot
                => Exponent is Rational and not Integer and Rational ratio
                   && ratio.ERational.Denominator.Remainder(2).IsZero;
            
            private static bool TryPower(Matrix m, int exp, out Entity res)
            {
                // Only a square matrix has powers; Pow throws for any other.
                res = m.IsSquare ? m.Pow(exp) : 0;
                return m.IsSquare;
            }

            // Re(x) = x/2 * (1 + 1/sgn(x)^2)
            internal static Entity Re(Entity x) => x / 2 * (1 + 1 / new Signumf(x).Pow(2));

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Base, Exponent,
                (a, b) => (a, b) switch
                {
                    (Matrix m, Integer(var exp)) when exp is { } expNotNull && TryPower(m, expNotNull, out var res) => res.InnerSimplified(isExact),
                    // An interval raised to a number, and a number raised to an interval, are
                    // the images of the interval under a monotone function where the function
                    // is one. https://github.com/asc-community/AngouriMath/issues/322
                    (Interval interval, var exponent) when exponent is not Set && IntervalArithmetic.Power(interval, exponent, isExact) is { } image => image,
                    (var @base, Interval interval) when @base is not Set && IntervalArithmetic.Exponential(@base, interval, isExact) is { } image => image,
                    (Integer(0), var x) =>
                        (isExact ? x.Evaled : x) is Complex c
                        ? c.RealPart.IsPositive
                          ? 0
                          : MathS.NaN
                        : new Providedf(0, Re(x) > 0),
                    (Complex n1, Complex n2) when !isExact => Number.Pow(n1, n2), // returns 1 for 0^0 and 0 for 0^(negative real part), we need to handle these cases above
                    (Integer(1), { DomainCondition: var condition }) => Integer.One.Provided(condition),
                    (var n1, Integer(-1)) => (1 / n1).InnerSimplified(isExact),
                    (var x, Integer(0)) =>
                        x.Evaled is Complex c
                        ? c.IsZero
                          ? throw new AngouriBugException("Should have already been handled by the above case")
                          : 1
                        : new Providedf(1, !x.EqualTo(0)),
                    (var n1, Integer(1)) => n1,
                    // https://github.com/asc-community/AngouriMath/issues/281
                    (Rational radicand, Rational exponent) when
                        RootExtraction.PullOutOfRadical(radicand, exponent) is { } pulled => pulled.InnerSimplified(isExact),
                    _ => null
                },
                (@this, a, b) => ((Powf)@this).New(a, b), isExact);
        }

        public partial record Logf
        {
            // Which of the two conditions below applies is the reading, exactly as it is
            // for Arcsinf and its siblings. This node alone stated the real one as though
            // it were the only one, so the library declared an expression undefined and
            // gave it a value at the same time: log(-3, -3) evaluates to 1 while its
            // DomainCondition was False.
            //
            // Over the reals the base must be positive and not 1, and the antilogarithm
            // positive. Over the complex plane log_b(a) is ln(a)/ln(b), which asks only
            // that both logarithms exist and that the denominator is not zero: no complex
            // number has 0 as its exponential, so a and b must be nonzero, and ln(b) = 0
            // is b = 1. Negative and non-real arguments are fine -- log(-3, -3) is 1 and
            // log(2, -8) is 3 + 4.53i -- which is the whole of what the real condition was
            // wrongly forbidding.
            //
            // The zero cases are stated rather than read off evaluation, which returns the
            // extended-real -oo for ln(0) and so would call it defined. -oo is not a
            // complex number, and taking it for one loses conditions that are needed
            // downstream: `ln(x) * 0` is 0 only away from x = 0, since -oo * 0 is NaN.
            // https://github.com/asc-community/AngouriMath/issues/721
            private protected override Entity IntrinsicCondition =>
                Codomain < Domain.Complex
                ? Base > 0 & !Base.EqualTo(1) & Antilogarithm > 0
                : !Base.EqualTo(0) & !Base.EqualTo(1) & !Antilogarithm.EqualTo(0);
            
            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) => 
                ExpandOnTwoArguments(Base, Antilogarithm,
                    (a, b) => (a, b) switch
                    {
                        (Complex n1, Complex n2) when !isExact => Number.Log(n1, n2),
                        // The image of an interval of positive numbers, monotone for any base.
                        // https://github.com/asc-community/AngouriMath/issues/322
                        (var @base, Interval interval) when @base is not Set && IntervalArithmetic.Logarithm(@base, interval, isExact) is { } image => image,

                        // log_b(0) is ln(0)/ln(b), that is -oo/ln(b), so the sign of the answer
                        // is the sign of ln(b): -oo for a base above 1 and +oo for one between
                        // 0 and 1. This answered -oo for every base, which is wrong below 1 --
                        // and for a base this cannot place on one side of 1 there is no signed
                        // answer to give, so the node is left as written.
                        // https://github.com/asc-community/AngouriMath/issues/890
                        ({ DomainCondition: var condition }, Integer(0))
                            when a.Evaled is Real above && above > 1
                            => Real.NegativeInfinity.Provided(condition),
                        ({ DomainCondition: var condition }, Integer(0))
                            when a.Evaled is Real below && below > 0 && below < 1
                            => Real.PositiveInfinity.Provided(condition),

                        // log_b(1) is 0/ln(b), which is 0 for every base but 1, where it is
                        // 0/0 -- and every division by zero here is NaN. The condition belongs
                        // on the answer rather than gating the rule, because at b = 1 the
                        // expression is genuinely undefined and not merely something else.
                        ({ DomainCondition: var condition }, Integer(1))
                            => Integer.Zero.Provided(condition).Provided(!a.EqualTo(1)),

                        _ => null
                    },
                    (@this, a, b) => ((Logf)@this).New(a, b), isExact);
        }
        
        public partial record Factorialf
        {
            // Factorial is defined for non-negative integers in the traditional sense,
            // but extends to complex numbers via the gamma function: n! = Γ(n+1)
            // The gamma function has poles at negative integers, so factorial is undefined there
            private protected override Entity IntrinsicCondition => 
                Argument.In(MathS.Sets.R) & (Argument >= 0 | !Argument.In(MathS.Sets.Z));

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex c when !isExact => Number.Factorial(c),
                        Rational({ Numerator: var num, Denominator: var den }) when den.Equals(2) && (num + 1) / 2 is var en => (
                            en > 0
                            // (+n - 1/2)! = (2n-1)!/(2^(2n-1)(n-1)!)*sqrt(pi)
                            // also 2n-1 is the numerator
                            ? Rational.Create(num.Factorial(), PeterO.Numbers.EInteger.FromInt32(2).Pow(num) * (en - 1).Factorial())
                            // (-n - 1/2)! = (-4)^n*n!/(2n)!*sqrt(pi)
                            : Rational.Create(PeterO.Numbers.EInteger.FromInt32(-4).Pow(-en) * (-en).Factorial(), (2 * -en).Factorial())
                        ) * MathS.Sqrt(MathS.pi),
                        _ => null
                    },
                    (@this, a) => ((Factorialf)@this).New(a), isExact);
        }

        public partial record Binomialf
        {
            // Defined for every whole k, whatever n is, by the falling factorial; for any other
            // k it is Γ(n + 1)/(Γ(k + 1) Γ(n - k + 1)), which has no value where n is a negative
            // whole number and a value everywhere else, the reciprocal gamma being entire.
            private protected override Entity IntrinsicCondition =>
                Lower.In(MathS.Sets.Z) | !(Upper.In(MathS.Sets.Z) & Upper < 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Upper, Lower,
                    (n, k) => (n, k) switch
                    {
                        (_, Integer { EInteger.Sign: < 0 }) => Integer.Create(0),
                        (Integer top, Integer bottom) when top.EInteger.Sign >= 0 && bottom.EInteger.CompareTo(top.EInteger) > 0 => Integer.Create(0),
                        // The falling factorial over k!, exact for any number n and any whole k,
                        // negative n included: binomial(-1, 3) = (-1)(-2)(-3)/3! = -1.
                        (Number top, Integer bottom) when bottom.EInteger.CanFitInInt32() => FallingFactorialOver(top, bottom.EInteger.ToInt32Unchecked()),
                        (_, Integer { IsZero: true }) => Integer.Create(1),
                        // Through the gamma function only numerically: there is no exact form.
                        (Complex top, Complex bottom) when !isExact
                            && top is not Integer && Number.Factorial(top) is var topF && Number.Factorial(bottom) is var bottomF
                            && Number.Factorial(top - bottom) is var restF && !topF.IsNaN && !bottomF.IsNaN && !restF.IsNaN
                            => topF / (bottomF * restF),
                        _ => null
                    },
                    (@this, a, b) => ((Binomialf)@this).New(a, b), isExact);

            private static Entity FallingFactorialOver(Number n, int k)
            {
                Entity product = Integer.Create(1);
                for (var i = 0; i < k; i++)
                    product = (product * (n - Integer.Create(i))).Evaled;
                return (product / Integer.Create(PeterO.Numbers.EInteger.FromInt32(k).Factorial())).Evaled;
            }
        }

        public partial record Iversonf
        {
            // 1 or 0 for every statement. A number is not one, and is left as written, as a
            // connective leaves one: 1 is not true. NaN is how a statement with no truth value
            // is spelled -- i > 0 is one -- and its bracket has no value either.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Boolean(true) => Integer.One,
                        Boolean(false) => Integer.Zero,
                        Number { IsNaN: true } => a,
                        _ => null
                    },
                    (@this, a) => ((Iversonf)@this).New(a), isExact);
        }

        public partial record Alephf
        {
            // Indexed by the whole numbers from zero: there is no aleph(-1) and no aleph(1/2).
            private protected override Entity IntrinsicCondition => Index.In(MathS.Sets.NonNegativeIntegers);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Index,
                    a => a switch
                    {
                        Integer { EInteger.Sign: >= 0 } => null,
                        Number => MathS.NaN,
                        _ => null
                    },
                    (@this, a) => ((Alephf)@this).New(a), isExact);
        }

        public partial record Erff
        {
            // Entire, so defined for every argument. https://github.com/asc-community/AngouriMath/issues/1501
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Erf(n),
                        Integer { IsZero: true } => Integer.Zero,
                        Real r when r.EDecimal.IsPositiveInfinity() => Integer.One,
                        Real r when r.EDecimal.IsNegativeInfinity() => Integer.MinusOne,
                        // Increasing on the real line.
                        Set.Interval interval when IntervalArithmetic.Monotone(interval, increasing: true, MathS.Erf, isExact) is { } image => image,
                        _ => null
                    },
                    (@this, a) => ((Erff)@this).New(a), isExact);
        }

        public partial record Erfcf
        {
            // Entire, so defined for every argument.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Erfc(n),
                        Integer { IsZero: true } => Integer.One,
                        Real r when r.EDecimal.IsPositiveInfinity() => Integer.Zero,
                        Real r when r.EDecimal.IsNegativeInfinity() => Integer.Create(2),
                        // Decreasing on the real line.
                        Set.Interval interval when IntervalArithmetic.Monotone(interval, increasing: false, MathS.Erfc, isExact) is { } image => image,
                        _ => null
                    },
                    (@this, a) => ((Erfcf)@this).New(a), isExact);
        }

        public partial record Erfif
        {
            // Entire, so defined for every argument.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Erfi(n),
                        Integer { IsZero: true } => Integer.Zero,
                        Real r when r.EDecimal.IsPositiveInfinity() => Real.PositiveInfinity,
                        Real r when r.EDecimal.IsNegativeInfinity() => Real.NegativeInfinity,
                        // Increasing on the real line.
                        Set.Interval interval when IntervalArithmetic.Monotone(interval, increasing: true, MathS.Erfi, isExact) is { } image => image,
                        _ => null
                    },
                    (@this, a) => ((Erfif)@this).New(a), isExact);
        }

        public partial record Eif
        {
            // Defined everywhere but at 0, where ln is not: real on either side of it.
            // https://github.com/asc-community/AngouriMath/issues/1501
            private protected override Entity IntrinsicCondition => !Argument.EqualTo(0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Ei(n),
                        // li(y) is Ei(ln y), by its definition.
                        Logf(var @base, var antilogarithm) when @base == MathS.e => MathS.Li(antilogarithm),
                        Real r when r.EDecimal.IsPositiveInfinity() => Real.PositiveInfinity,
                        Real r when r.EDecimal.IsNegativeInfinity() => Integer.Zero,
                        _ => null
                    },
                    (@this, a) => ((Eif)@this).New(a), isExact);
        }

        public partial record Lif
        {
            // Defined everywhere but at 1, where ln is 0; on the real line, where the logarithm
            // is real, that is x >= 0, and li(0) = 0.
            private protected override Entity IntrinsicCondition =>
                Codomain < Domain.Complex
                ? Argument >= 0 & !Argument.EqualTo(1)
                : !Argument.EqualTo(1);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Li(n),
                        Integer { IsZero: true } => Integer.Zero,
                        Real r when r.EDecimal.IsPositiveInfinity() => Real.PositiveInfinity,
                        _ => null
                    },
                    (@this, a) => ((Lif)@this).New(a), isExact);
        }

        public partial record Sif
        {
            // Entire, so defined for every argument. https://github.com/asc-community/AngouriMath/issues/1501
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Si(n),
                        Integer { IsZero: true } => Integer.Zero,
                        Real r when r.EDecimal.IsPositiveInfinity() => MathS.pi / 2,
                        Real r when r.EDecimal.IsNegativeInfinity() => -MathS.pi / 2,
                        _ => null
                    },
                    (@this, a) => ((Sif)@this).New(a), isExact);
        }

        public partial record Cif
        {
            // Defined everywhere but at 0, where ln is not; on the real line, where it is real, x > 0.
            private protected override Entity IntrinsicCondition =>
                Codomain < Domain.Complex ? Argument > 0 : !Argument.EqualTo(0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Ci(n),
                        Real r when r.EDecimal.IsPositiveInfinity() => Integer.Zero,
                        // Ci(-x) = Ci(x) + i pi for x > 0.
                        Real r when r.EDecimal.IsNegativeInfinity() => MathS.pi * MathS.i,
                        _ => null
                    },
                    (@this, a) => ((Cif)@this).New(a), isExact);
        }

        public partial record Shif
        {
            // Entire, so defined for every argument.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Shi(n),
                        Integer { IsZero: true } => Integer.Zero,
                        Real r when r.EDecimal.IsPositiveInfinity() => Real.PositiveInfinity,
                        Real r when r.EDecimal.IsNegativeInfinity() => Real.NegativeInfinity,
                        // Increasing on the real line: its derivative is sinh(x)/x > 0.
                        Set.Interval interval when IntervalArithmetic.Monotone(interval, increasing: true, MathS.Shi, isExact) is { } image => image,
                        _ => null
                    },
                    (@this, a) => ((Shif)@this).New(a), isExact);
        }

        public partial record Chif
        {
            // Defined everywhere but at 0; on the real line, where it is real, x > 0.
            private protected override Entity IntrinsicCondition =>
                Codomain < Domain.Complex ? Argument > 0 : !Argument.EqualTo(0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Complex n when !isExact => Number.Chi(n),
                        Real r when r.EDecimal.IsPositiveInfinity() => Real.PositiveInfinity,
                        _ => null
                    },
                    (@this, a) => ((Chif)@this).New(a), isExact);
        }

        public partial record Signumf
        {
            // Signum is defined everywhere in the complex plane
            // sgn(0) = 0, sgn(z) = z/|z| for z ≠ 0
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Real n => n.EDecimal.Sign,
                        Complex n when !isExact => Number.Signum(n),
                        // sgn(|z|) is 1 wherever z is nonzero and 0 where it is not: |z| is never
                        // negative, so the only question is whether it vanishes. This answered 1
                        // for every argument -- so sgn(abs(x)) simplified to 1, where at x = 0 the
                        // value is 0, because sgn(0) is 0 as the comment above says.
                        //
                        // A condition is not the fix. The expression is defined at zero and equal
                        // to 0 there, so `1 provided not z = 0` would replace a wrong value with a
                        // wrong domain. Where the argument's vanishing cannot be decided the node
                        // is left as written instead.
                        // https://github.com/asc-community/AngouriMath/issues/892
                        Absf(var magnitudeOf) when ValueWithCondition(magnitudeOf) is { } known
                            => (known.Value.IsZero ? Integer.Zero : Integer.One)
                                .Provided(known.Condition),
                        Signumf signum => signum,
                        _ => null
                    },
                    (@this, a) => ((Signumf)@this).New(a), isExact);
        }

        public partial record Floorf
        {
            // Defined everywhere in the complex plane, taken componentwise.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        // An integer is already its own floor, and it stays exact.
                        Integer n => n,
                        // An infinity is its own floor -- there is no greatest integer below
                        // +oo -- and NaN propagates. Neither survives EInteger, which refuses
                        // both, and the exception was reaching the caller:
                        // https://github.com/asc-community/AngouriMath/issues/830
                        Real { IsFinite: false } n => n,
                        // From the numerator and the denominator: through EDecimal a rational of more
                        // digits than the precision was rounded onto the integer beside it, and
                        // floor(10^120 - 1/3) came out 10^120.
                        // https://github.com/asc-community/AngouriMath/issues/1807
                        Rational n => Integer.Create(RoundedQuotient(n.ERational, up: false)),
                        Real n when !isExact => Integer.Create(n.EDecimal.Floor().ToEInteger()),
                        Complex n when !isExact => Complex.Create(
                            n.RealPart.EDecimal.Floor(), n.ImaginaryPart.EDecimal.Floor()),
                        // Idempotent: the floor of an integer is that integer, and floor
                        // always produces one.
                        Floorf or Ceilf => a,
                        // What the quantifiers around it establish about the argument:
                        // https://github.com/asc-community/AngouriMath/issues/1409
                        _ => Functions.Boolean.QuantifierFacts.Rounded(a, up: false, isExact)
                    },
                    (@this, a) => ((Floorf)@this).New(a), isExact);
        }

        /// <summary>
        /// The floor, or with <paramref name="up"/> the ceiling, of a rational, exactly: the quotient of
        /// its numerator by its denominator, which is positive, rounded toward zero by the division and
        /// moved one step where that went the wrong way.
        /// </summary>
        private static PeterO.Numbers.EInteger RoundedQuotient(PeterO.Numbers.ERational value, bool up)
        {
            var division = value.Numerator.DivRem(value.Denominator);
            var (quotient, remainder) = (division[0], division[1]);
            if (remainder.IsZero)
                return quotient;
            return up ? (value.Numerator.Sign > 0 ? quotient + PeterO.Numbers.EInteger.One : quotient)
                      : (value.Numerator.Sign < 0 ? quotient - PeterO.Numbers.EInteger.One : quotient);
        }

        public partial record Ceilf
        {
            // Defined everywhere in the complex plane, taken componentwise.
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Integer n => n,
                        // As in Floorf: https://github.com/asc-community/AngouriMath/issues/830
                        Real { IsFinite: false } n => n,
                        Rational n => Integer.Create(RoundedQuotient(n.ERational, up: true)),
                        Real n when !isExact => Integer.Create(n.EDecimal.Ceiling().ToEInteger()),
                        Complex n when !isExact => Complex.Create(
                            n.RealPart.EDecimal.Ceiling(), n.ImaginaryPart.EDecimal.Ceiling()),
                        Floorf or Ceilf => a,
                        // As in Floorf: https://github.com/asc-community/AngouriMath/issues/1409
                        _ => Functions.Boolean.QuantifierFacts.Rounded(a, up: true, isExact)
                    },
                    (@this, a) => ((Ceilf)@this).New(a), isExact);
        }

        public partial record Roundf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Integer n => n,
                        Rational n => Integer.Create(n.EDecimal.RoundHalfEven().ToEInteger()),
                        Real n when !isExact => Integer.Create(n.EDecimal.RoundHalfEven().ToEInteger()),
                        Complex n when !isExact => Complex.Create(
                            n.RealPart.EDecimal.RoundHalfEven(), n.ImaginaryPart.EDecimal.RoundHalfEven()),
                        // Anything that already produced an integer is left alone.
                        Floorf or Ceilf or Roundf => a,
                        _ => null
                    },
                    (@this, a) => ((Roundf)@this).New(a), isExact);
        }

        public partial record Minf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        // Only ordered arguments compare. A complex one is left alone rather
                        // than guessed at, which is what SymPy's Min does too.
                        (Real l, Real r) => l.EDecimal.CompareTo(r.EDecimal) <= 0 ? l : r,
                        var (l, r) when l == r => l,
                        _ => null
                    },
                    (@this, a, b) => ((Minf)@this).New(a, b), isExact);
        }

        public partial record Maxf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        (Real l, Real r) => l.EDecimal.CompareTo(r.EDecimal) >= 0 ? l : r,
                        var (l, r) when l == r => l,
                        _ => null
                    },
                    (@this, a, b) => ((Maxf)@this).New(a, b), isExact);
        }

        public partial record Gcdf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        // gcd is non-negative by convention, and gcd(0, n) is |n|.
                        (Integer l, Integer r) => Integer.Create(l.EInteger.Gcd(r.EInteger)),
                        // gcd(a/b, c/d) = gcd(a, c) / lcm(b, d) -- which is what SymPy gives,
                        // so gcd(1/2, 1/3) is 1/6 rather than an error.
                        (Rational l, Rational r) => Rational.Create(
                            l.Numerator.EInteger.Gcd(r.Numerator.EInteger),
                            l.Denominator.EInteger / l.Denominator.EInteger.Gcd(r.Denominator.EInteger) * r.Denominator.EInteger),
                        var (l, r) when l == r => l,
                        // The polynomial case is left to the node: PolynomialGcd already
                        // computes one for the cancellation rule, and wiring it in here is
                        // its own change rather than something to guess at.
                        _ => null
                    },
                    (@this, a, b) => ((Gcdf)@this).New(a, b), isExact);
        }

        public partial record Lcmf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        // lcm is non-negative by the gcd's convention, and lcm(0, n) is 0: zero is
                        // the only common multiple there is.
                        (Integer l, Integer r) => l.EInteger.IsZero || r.EInteger.IsZero
                            ? Integer.Zero
                            : Integer.Create((l.EInteger * r.EInteger).Abs() / l.EInteger.Gcd(r.EInteger)),
                        // lcm(a/b, c/d) = lcm(a, c) / gcd(b, d), the mirror of the gcd's rule and
                        // what SymPy gives: lcm(1/2, 1/3) is 1.
                        (Rational l, Rational r) => l.Numerator.EInteger.IsZero || r.Numerator.EInteger.IsZero
                            ? Integer.Zero
                            : Rational.Create(
                                (l.Numerator.EInteger * r.Numerator.EInteger).Abs() / l.Numerator.EInteger.Gcd(r.Numerator.EInteger),
                                l.Denominator.EInteger.Gcd(r.Denominator.EInteger)),
                        var (l, r) when l == r => l,
                        _ => null
                    },
                    (@this, a, b) => ((Lcmf)@this).New(a, b), isExact);
        }

        public partial record Absf
        {
            // Absolute value is defined everywhere in the complex plane
            // For complex z, |z| = sqrt(Re(z)^2 + Im(z)^2)
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <summary>
            /// The Euclidean norm of a vector.
            /// </summary>
            /// <remarks>
            /// This used to always finish with <see cref="Entity.InnerSimplified"/>, even
            /// when the caller had asked for numeric evaluation. A norm like sqrt(369)
            /// has no exact simplification, so it survived as a <see cref="Powf"/> and
            /// <see cref="Entity.EvalNumerical"/> then rejected it as "not a simple
            /// number" -- it only worked when the norm was a perfect square.
            /// </remarks>
            private static Entity VectorNorm(Matrix vector, bool isExact)
            {
                var norm = Sumf.Sum(vector.Select(component => component.Pow(2))).Pow(0.5);
                return isExact ? norm.InnerSimplified : norm.Evaled;
            }

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Matrix m when m.IsVector => VectorNorm(m, isExact),
                        Complex n when !isExact => Number.Abs(n),
                        Absf abs => abs,
                        // The interval itself from zero on, its reflection up to zero, and
                        // folded across zero otherwise. https://github.com/asc-community/AngouriMath/issues/322
                        Interval interval when IntervalArithmetic.Absolute(interval, isExact) is { } image => image,
                        // |sgn(z)| has unit modulus wherever z is nonzero and is 0 at zero, the
                        // same question as sgn(|z|) above and with the same answer. See there for
                        // why this decides rather than attaching a condition.
                        // https://github.com/asc-community/AngouriMath/issues/892
                        Signumf(var signOf) when ValueWithCondition(signOf) is { } known
                            => (known.Value.IsZero ? Integer.Zero : Integer.One)
                                .Provided(known.Condition),

                        // |x| is x where the argument is a non-negative real and -x where it is
                        // negative, which is the definition rather than an identity needing an
                        // assumption. A Number folded already; what this reaches is an argument
                        // whose *value* is a known real without its node being a number --
                        // abs(-sqrt(6)) and abs(-pi) stayed as written, so a concrete quadratic
                        // inequality answered with abs(-sqrt(6)) / 2 in it.
                        //
                        // An argument off the real line has to decline: sqrt(-4) evaluates to 2i,
                        // and |2i| is 2, which is neither the argument nor its negation. Nothing
                        // is assumed about the sign of a symbol either -- there the value cannot
                        // be read at all and the node is left alone.
                        // https://github.com/asc-community/AngouriMath/issues/881
                        var argument when argument.Evaled is Real { EDecimal.IsFinite: true } value
                            => value.EDecimal.IsNegative
                                ? (-argument).InnerSimplified(isExact)
                                : argument,

                        _ => null
                    },
                    (@this, a) => ((Absf)@this).New(a), isExact);
        }
    }
}
