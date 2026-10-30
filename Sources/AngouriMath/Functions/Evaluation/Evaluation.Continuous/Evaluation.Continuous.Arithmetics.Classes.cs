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
        /// The operand a sum or a difference simplifies first, or <see langword="null"/> for any
        /// other node.
        /// </summary>
        private static Entity? LeftOperandOfASum(Entity node)
            => node switch { Sumf sum => sum.Augend, Minusf difference => difference.Minuend, _ => null };

        /// <summary>How far apart the nodes simplified ahead along a long left spine are.</summary>
        private const int SpineCheckpointSpacing = 32;

        /// <summary>
        /// Simplifies a long chain of sums from the bottom up before <paramref name="sum"/> is
        /// simplified, so that no recursion through it goes deeper than
        /// <see cref="SpineCheckpointSpacing"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A sum simplifies its left operand first, and that operand's left operand, and so on, so
        /// a chain of n sums nested to the left is simplified by a recursion n deep. A sum built
        /// one term at a time is that shape. <c>CollectLikeTerms</c> builds one, and so does
        /// anything that adds terms up in a loop. The integrals of
        /// <c>(q + p x)/(c + b x + a x^2)^3</c> simplified one about 900 deep, which overflowed a
        /// 1 MB stack in a Debug build and 384 KB in Release.
        /// https://github.com/asc-community/AngouriMath/issues/2166
        /// </para>
        /// <para>
        /// Every node's simplification is cached, so simplifying every
        /// <see cref="SpineCheckpointSpacing"/>th node of the spine first, deepest first, leaves
        /// each of those recursions stopping at the one below it, and then the sum's own. The
        /// same nodes are simplified as before, each once, in another order. Under twice the
        /// spacing nothing is done, so an ordinary sum pays for a walk of a few nodes and nothing
        /// else.
        /// </para>
        /// </remarks>
        private static void SimplifyALongLeftSpineFirst(Entity sum, bool isExact)
        {
            var depth = 0;
            for (var node = LeftOperandOfASum(sum); node is not null && depth < 2 * SpineCheckpointSpacing; node = LeftOperandOfASum(node))
                depth++;
            if (depth < 2 * SpineCheckpointSpacing)
                return;
            var checkpoints = new List<Entity>();
            depth = 0;
            for (var node = LeftOperandOfASum(sum); node is Sumf or Minusf; node = LeftOperandOfASum(node)!)
                if (++depth % SpineCheckpointSpacing == 0)
                    checkpoints.Add(node);
            for (var i = checkpoints.Count - 1; i >= 0; i--)
                _ = checkpoints[i].InnerSimplified(isExact);
        }

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
            protected override Entity InnerSimplify(bool isExact)
            {
                SimplifyALongLeftSpineFirst(this, isExact);
                return ExpandOnTwoArguments(Augend, Addend,
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
        }
        public partial record Minusf
        {
            private protected override Entity IntrinsicCondition => Boolean.True;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
            {
                SimplifyALongLeftSpineFirst(this, isExact);
                return ExpandOnTwoArguments(Minuend, Subtrahend,
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
                    // Dividing by a matrix multiplies by its inverse, as the wiki's Matrices page
                    // says division does. Left to the elementwise branch every operation shares,
                    // [[1, 2], [3, 4]] / [[1, 2], [3, 4]] was [[1, 1], [1, 1]]. A number over a
                    // matrix too: x / y is x * y^(-1), which Simplify rewrites freely, and y^(-1)
                    // of a matrix is its inverse, so 1 / M read elementwise disagreed with M^(-1).
                    // A divisor with no inverse, singular or not square, leaves the quotient
                    // written. A matrix over a number is still spread over the entries.
                    // https://github.com/asc-community/AngouriMath/issues/2141
                    (_, Matrix divisor) => Powf.MayHaveAnInverse(divisor) ? (a * divisor.Pow(-1)).InnerSimplified(isExact) : a / b,
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
                ? Base >= 0 & WhereZeroIsDefined
                : WhereZeroIsDefined;

            /// <summary>
            /// <c>0^w</c> is defined where <c>Re(w) &gt; 0</c>. For a non-real number <c>w</c>,
            /// <c>w &gt; 0</c> has no value, the complex numbers not being ordered, and made the
            /// whole condition NaN, so <c>0 b^i</c> was NaN; its real part is read instead.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/1865">#1865</a>
            /// </summary>
            private Entity WhereZeroIsDefined
                => (Exponent is Complex ? Exponent : Exponent.Vars.Any() ? null : Exponent.Evaled) is Complex exponent and not Real
                   ? exponent.RealPart.IsPositive ? Boolean.True : !Base.EqualTo(0)
                   : !Base.EqualTo(0) | Exponent > 0;

            /// <summary>
            /// Whether the exponent is a literal rational whose denominator is even — the case
            /// where a negative base leaves the real line.
            /// </summary>
            private bool IsEvenRoot
                => Exponent is Rational and not Integer and Rational ratio
                   && ratio.ERational.Denominator.Remainder(2).IsZero;
            
            private static bool TryPower(Matrix m, int exp, out Entity res)
            {
                // Only a square matrix has powers; Pow throws for any other. And only one with
                // an inverse has negative powers: Pow threw for a singular one, out of eval and
                // Simplify alike, and so did dividing by one once division used the inverse.
                // https://github.com/asc-community/AngouriMath/issues/2141
                var defined = m.IsSquare && (exp >= 0 || MayHaveAnInverse(m));
                res = defined ? m.Pow(exp) : 0;
                return defined;
            }

            /// <summary>
            /// Whether a square matrix may have an inverse: not where its determinant is zero.
            /// </summary>
            /// <remarks>
            /// Asked of the exact elimination, not of <see cref="Matrix.Inverse"/> or
            /// <see cref="Matrix.Determinant"/>, which can go through GenericTensor's compiled
            /// operations: those are not safe in a native build, and every node reaches this.
            /// Using them here broke the native build of every wrapper. A determinant the
            /// elimination cannot write is not known to be zero.
            /// </remarks>
            internal static bool MayHaveAnInverse(Matrix m)
                => m.IsSquare
                   && (Functions.PolynomialDeterminant.Of(m.RowCount, (row, column) => m[row, column]) is not { } determinant
                       || determinant.InnerSimplified != 0);

            // Re(x) = x/2 * (1 + 1/sgn(x)^2)
            internal static Entity Re(Entity x) => x / 2 * (1 + 1 / new Signumf(x).Pow(2));

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Base, Exponent,
                (a, b) => (a, b) switch
                {
                    (Matrix m, Integer(var exp)) when exp is { } expNotNull && TryPower(m, expNotNull, out var res) => res.InnerSimplified(isExact),
                    // Only a square matrix has powers, and only whole ones. Anything else fell
                    // through to the elementwise branch: [1, 2]^2 was [1, 4], which is no power
                    // of a column, and Simplify, which writes x * x as x^2, made the undefined
                    // product [1, 2] * [1, 2] into the same. It stays written.
                    // https://github.com/asc-community/AngouriMath/issues/2141
                    (Matrix, _) => MathS.Pow(a, b),
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
                    (Powf(var z, var inner), var exponent) when Functions.Boolean.QuantifierFacts.PrincipalPowerOfAPower(z, inner, exponent) is { } root => root,
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
                        // 0/0 -- and every division by zero here is NaN -- and 0, which is not a
                        // base (https://github.com/asc-community/AngouriMath/issues/2255). The
                        // condition belongs on the answer rather than gating the rule, because at
                        // those bases the expression is genuinely undefined and not merely
                        // something else.
                        ({ DomainCondition: var condition }, Integer(1))
                            => Integer.Zero.Provided(condition).Provided(!a.EqualTo(0) & !a.EqualTo(1)),

                        _ => null
                    },
                    (@this, a, b) => ((Logf)@this).New(a, b), isExact);
        }
        
        public partial record DoubleFactorialf
        {
            // Defined on the integers but the negative even ones.
            private protected override Entity IntrinsicCondition =>
                Argument.In(MathS.Sets.Z) & (Argument >= -1 | !new Dividesf(Integer.Create(2), Argument));

            // The largest argument multiplied out exactly. Past it the node is kept, as a whole power
            // with a huge exponent is: https://github.com/asc-community/AngouriMath/issues/1448
            private const int MaxExactArgument = 10000;

            // n (n - 2) ... down to 1 or 2, and 0!! = (-1)!! = 1. Below that n!! = (n + 2)!!/(n + 2), so
            // (-3)!! is -1 and (-5)!! is 1/3; a negative even n has none. Off the integers there is no
            // value: the gamma extension does not give 0!! = 1, so no extension is this function's.
            private static Entity? OfANumber(Complex number)
            {
                PeterO.Numbers.EInteger whole;
                if (number is Integer integer)
                    whole = integer.EInteger;
                else if (number is Real { EDecimal: var value } && value.IsFinite && value.IsInteger())
                    whole = value.ToEInteger();
                else
                    return Real.NaN;
                if (whole.Abs().CompareTo(PeterO.Numbers.EInteger.FromInt32(MaxExactArgument)) > 0)
                    return null;
                var n = whole.ToInt32Checked();
                if (n >= 0)
                {
                    var product = PeterO.Numbers.EInteger.One;
                    for (var k = n; k > 1; k -= 2)
                        product *= k;
                    return Integer.Create(product);
                }
                if (n % 2 == 0)
                    return Real.NaN;
                var denominator = PeterO.Numbers.EInteger.One;
                for (var j = n + 2; j <= -1; j += 2)
                    denominator *= j;
                return Rational.Create(PeterO.Numbers.EInteger.One, denominator);
            }

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument, a => a is Complex number ? OfANumber(number) : null,
                    (@this, a) => ((DoubleFactorialf)@this).New(a), isExact);
        }

        public partial record Factorialf
        {
            // Factorial is defined for non-negative integers in the traditional sense,
            // but extends to complex numbers via the gamma function: n! = Γ(n+1)
            // The gamma function has poles at negative integers, so factorial is undefined there,
            // and only there. The condition asked for a real argument as well, though i! is
            // 0.498 - 0.155i, so x!/x! simplified to 1 provided x in RR, which has no value at i.
            // Off the integers x >= 0 may have no truth value and x in ZZ is false, so the implication
            // is true. It is written as Simplify writes it, so that a quotient's x! = 0, which Simplify
            // reads as False under this same condition, folds into it.
            // https://github.com/asc-community/AngouriMath/issues/2256
            private protected override Entity IntrinsicCondition =>
                new Impliesf(Argument.In(MathS.Sets.Z), Argument >= 0);

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
                        // A k that is not whole has no value with a negative whole n, a pole of Γ(n + 1).
                        (Integer { EInteger.Sign: < 0 }, Complex bottom) when bottom is not Integer => MathS.NaN,
                        // And is 0 where n - k is a negative whole number, a pole of Γ(n - k + 1), where the
                        // reciprocal gamma is 0: binomial(1/2, 3/2) is 0.
                        (Number top, Number bottom) when bottom is not Integer && (top - bottom).Evaled is Integer { EInteger.Sign: < 0 } => Integer.Zero,
                        // Through the gamma function only numerically: there is no exact form. A whole n
                        // takes it too: binomial(3, 1/2) is 32/(5 pi).
                        (Complex top, Complex bottom) when !isExact
                            && Number.Factorial(top) is var topF && Number.Factorial(bottom) is var bottomF
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

        public partial record FallingFactorialf
        {
            // Defined for every whole order n >= 0; for a negative whole n where no factor of
            // (x + 1) (x + 2) ... (x - n) vanishes; for any other n where Γ(x + 1) is, which is
            // everywhere but the negative whole numbers.
            private protected override Entity IntrinsicCondition =>
                Order.In(MathS.Sets.Z) & (Order >= 0 | !(Argument.In(MathS.Sets.Z) & Argument >= Order & Argument <= -1))
                | !Order.In(MathS.Sets.Z) & !(Argument.In(MathS.Sets.Z) & Argument < 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Argument, Order,
                    (x, n) => (x, n) switch
                    {
                        (_, Integer { IsZero: true }) => Integer.One,
                        (Number value, Integer order) => FactorialPowers.OfAWholeOrder(value, order, falling: true),
                        // Γ(x + 1)/Γ(x - n + 1): no value at a pole of the numerator, and 0 at one of
                        // the denominator, whose reciprocal vanishes there.
                        (Integer { EInteger.Sign: < 0 }, Number) => MathS.NaN,
                        (Number value, Number order) when (value - order).Evaled is Integer { EInteger.Sign: < 0 } => Integer.Zero,
                        (Complex value, Complex order) when !isExact
                            && Number.Factorial(value) is var top && Number.Factorial(value - order) is var bottom && !top.IsNaN && !bottom.IsNaN
                            => top / bottom,
                        _ => null
                    },
                    (@this, x, n) => ((FallingFactorialf)@this).New(x, n), isExact);
        }

        public partial record RisingFactorialf
        {
            // Defined for every whole order n >= 0; for a negative whole n where no factor of
            // (x - 1) (x - 2) ... (x + n) vanishes; for any other n where Γ(x + n) is, which is
            // everywhere but where x + n is a whole number not above 0.
            private protected override Entity IntrinsicCondition =>
                Order.In(MathS.Sets.Z) & (Order >= 0 | !(Argument.In(MathS.Sets.Z) & Argument >= 1 & Argument <= -Order))
                | !Order.In(MathS.Sets.Z) & !((Argument + Order).In(MathS.Sets.Z) & Argument + Order <= 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Argument, Order,
                    (x, n) => (x, n) switch
                    {
                        (_, Integer { IsZero: true }) => Integer.One,
                        (Number value, Integer order) => FactorialPowers.OfAWholeOrder(value, order, falling: false),
                        // Γ(x + n)/Γ(x): no value at a pole of the numerator, and 0 at one of the
                        // denominator.
                        (Number value, Number order) when (value + order).Evaled is Integer { EInteger.Sign: <= 0 } => MathS.NaN,
                        (Integer { EInteger.Sign: <= 0 }, Number) => Integer.Zero,
                        (Complex value, Complex order) when !isExact
                            && Number.Factorial(value + order - 1) is var top && Number.Factorial(value - 1) is var bottom && !top.IsNaN && !bottom.IsNaN
                            => top / bottom,
                        _ => null
                    },
                    (@this, x, n) => ((RisingFactorialf)@this).New(x, n), isExact);
        }

        /// <summary>The falling and the rising factorial of a number to a whole order, as products.</summary>
        private static class FactorialPowers
        {
            /// <summary>How long a product is built before the node is left as written.</summary>
            private const int LongestProduct = 10000;

            /// <summary>
            /// <c>x (x - 1) ... (x - n + 1)</c> falling and <c>x (x + 1) ... (x + n - 1)</c> rising for
            /// <c>n &gt; 0</c>, and for <c>n &lt; 0</c> the reciprocal of <c>(x + 1) ... (x - n)</c>
            /// falling and of <c>(x - 1) ... (x + n)</c> rising, <c>NaN</c> where a factor vanishes.
            /// </summary>
            internal static Entity? OfAWholeOrder(Number x, Integer order, bool falling)
            {
                if (order.EInteger.Abs().CompareTo(PeterO.Numbers.EInteger.FromInt32(LongestProduct)) > 0)
                    return null;
                var n = order.EInteger.ToInt32Checked();
                var step = falling == n > 0 ? -1 : 1;
                Entity product = Integer.One;
                for (var i = 0; i < System.Math.Abs(n); i++)
                {
                    var factor = (x + Integer.Create(n > 0 ? step * i : step * (i + 1))).Evaled;
                    if (n < 0 && factor is Complex { IsZero: true })
                        return MathS.NaN;
                    product = (product * factor).Evaled;
                }
                return n > 0 ? product : (Integer.One / product).Evaled;
            }
        }

        public partial record CatalanNumberf
        {
            // Defined everywhere but at a negative half-integer, a pole of Γ(n + 1/2).
            private protected override Entity IntrinsicCondition =>
                !((Argument + Rational.Create(1, 2)).In(MathS.Sets.Z) & Argument < 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnOneArgument(Argument,
                    a => a switch
                    {
                        Integer { EInteger: var n } when n.Sign >= 0 && n.CompareTo(PeterO.Numbers.EInteger.FromInt32(LargestIndex)) <= 0
                            => Integer.Create(n.Multiply(2).Factorial() / (n.Factorial() * (n + 1).Factorial())),
                        Integer { EInteger: var n } when n.Sign >= 0 => null,
                        // 4^n Γ(n + 1/2) / (√π Γ(n + 2)): Γ(n + 2) has a pole at every whole number
                        // below -1, where its reciprocal vanishes, and at -1 the two poles cancel to -1/2.
                        Integer { EInteger: var n } when n.Equals(PeterO.Numbers.EInteger.FromInt32(-1)) => Rational.Create(-1, 2),
                        Integer => Integer.Zero,
                        Number value when (value + Rational.Create(1, 2)).Evaled is Integer { EInteger.Sign: <= 0 } => MathS.NaN,
                        Complex value when !isExact
                            && Number.Factorial((Complex)(value - Rational.Create(1, 2)).Evaled) is var top && Number.Factorial((Complex)(value + 1).Evaled) is var bottom
                            && !top.IsNaN && !bottom.IsNaN
                            => (MathS.Pow(4, value) * top / (MathS.Sqrt(MathS.pi) * bottom)).Evaled,
                        _ => null
                    },
                    (@this, a) => ((CatalanNumberf)@this).New(a), isExact);

            /// <summary>How far a Catalan number is computed exactly before the node is left as written.</summary>
            private const int LargestIndex = 1000;
        }

        public partial record HarmonicNumberf
        {
            // No value at a negative integer n, a pole of ζ(m, n + 1), but for a whole order m <= 0,
            // where it is a polynomial in n.
            private protected override Entity IntrinsicCondition =>
                !(Argument.In(MathS.Sets.Z) & Argument < 0) | (Order.In(MathS.Sets.Z) & Order <= 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Argument, Order,
                    (n, m) => (n, m) switch
                    {
                        // A whole n >= 0: the sum, exact for a rational order.
                        (Integer { EInteger: var whole }, Number order) when whole.Sign >= 0 && whole.CompareTo(PeterO.Numbers.EInteger.FromInt32(LongestSum)) <= 0
                            => Sum(whole.ToInt32Checked(), order).InnerSimplified(isExact),
                        (Integer { EInteger.Sign: >= 0 }, Number) => null,
                        (Integer { EInteger.Sign: < 0 }, Number order) when order is not Integer { EInteger.Sign: <= 0 } => MathS.NaN,
                        // A half-whole n, exactly where both polygamma values are: H(1/2) is 2 - 2 ln 2.
                        (Rational value, Integer { EInteger: var order }) when isExact && order.Sign > 0 && order.CompareTo(PeterO.Numbers.EInteger.FromInt32(LargestOrder)) <= 0
                            && Number.ExactHarmonicNumber(order.ToInt32Checked(), value.ERational) is { } exact => exact,
                        // Elsewhere, numerically: ψ(n + 1) - ψ(1) for m = 1, and through the polygamma
                        // function for a whole m >= 2, as ζ(m) - ζ(m, n + 1) is.
                        (Complex value, Integer { EInteger: var order }) when !isExact && order.Sign > 0 && order.CompareTo(PeterO.Numbers.EInteger.FromInt32(LargestOrder)) <= 0
                            => Numerically(value, order.ToInt32Checked()),
                        _ => null
                    },
                    (@this, n, m) => ((HarmonicNumberf)@this).New(n, m), isExact);

            private static Entity Sum(int n, Number order)
                => n == 0 ? Integer.Zero
                 : Enumerable.Range(1, n).Select(k => MathS.Pow(Integer.Create(k), -order)).Aggregate((sum, term) => sum + term);

            // (-1)^m / (m - 1)! (ψ^(m - 1)(1) - ψ^(m - 1)(n + 1)), which is ψ(n + 1) - ψ(1) at m = 1.
            private static Entity Numerically(Complex n, int m)
                => (MathS.Pow(-1, m) / MathS.Factorial(m - 1)
                    * (MathS.Polygamma(m - 1, Integer.One) - MathS.Polygamma(m - 1, n + 1))).Evaled;

            /// <summary>How long a sum is built before the node is left as written.</summary>
            private const int LongestSum = 10000;

            /// <summary>The largest order evaluated through the polygamma function.</summary>
            private const int LargestOrder = 64;
        }

        public partial record LambertWf
        {
            // Every whole branch, and every z but 0 off the principal branch, where W_k(z) goes to -oo as ln z does.
            private protected override Entity IntrinsicCondition =>
                Branch.In(MathS.Sets.Z) & (Branch.Equalizes(Integer.Zero) | !Argument.Equalizes(Integer.Zero));

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact) =>
                ExpandOnTwoArguments(Branch, Argument,
                    (k, z) => (k, z) switch
                    {
                        (Integer { IsZero: true }, Integer { IsZero: true }) => Integer.Zero,
                        (Integer, Integer { IsZero: true }) => Real.NegativeInfinity,
                        // c e^c on the branch whose real values c is among: W_0 for c >= -1, W_(-1) for c <= -1,
                        // so -1/e is -1 on both and e is 1.
                        (Integer { EInteger: var branch }, _) when TheExponentOfAProductWithItsExponential(z) is { } c
                            && (branch.IsZero && c.EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromInt32(-1)) >= 0
                                || branch.Equals(PeterO.Numbers.EInteger.FromInt32(-1)) && c.EDecimal.CompareTo(PeterO.Numbers.EDecimal.FromInt32(-1)) <= 0)
                            => c,
                        (Integer { EInteger: var branch }, Complex value) when !isExact => Number.LambertW(branch, value),
                        (Number, Number) when k is not Integer => MathS.NaN,
                        _ => null
                    },
                    (@this, k, z) => ((LambertWf)@this).New(k, z), isExact);

            // c where z is c e^c for a real number c, as -e^(-1), 2 e^2 and e are written.
            private static Real? TheExponentOfAProductWithItsExponential(Entity z)
                => z switch
                {
                    Variable { Name: "e" } => Integer.One,
                    Powf(Variable { Name: "e" }, Real exponent) when exponent == Integer.One => exponent,
                    Mulf(Real c, Powf(Variable { Name: "e" }, Real exponent)) when c == exponent => c,
                    Mulf(Powf(Variable { Name: "e" }, Real exponent), Real c) when c == exponent => c,
                    Divf(Real c, Variable { Name: "e" }) when c == Integer.MinusOne => c,
                    Divf(Real c, Powf(Variable { Name: "e" }, Real exponent)) when c == -exponent => c,
                    _ => null
                };
        }

        public partial record Ref
        {
            // Defined everywhere on the complex plane.
            private protected override Entity IntrinsicCondition => Boolean.True;

            // Real-linear, so carried through sums, differences, and products and quotients by a
            // real number; re(conj z) is re(z). What is known to be real is its own real part, and
            // has a value exactly where its real part has one.
            private static Entity? RealPart(Entity argument, bool isExact) => argument switch
            {
                Complex number => number.RealPart,
                _ when Conjugatef.IsKnownReal(argument) => argument,
                Conjugatef(var z) => new Ref(z).InnerSimplified(isExact),
                Sumf(var left, var right) => (new Ref(left) + new Ref(right)).InnerSimplified(isExact),
                Minusf(var left, var right) => (new Ref(left) - new Ref(right)).InnerSimplified(isExact),
                Mulf(Real factor, var z) => (factor * new Ref(z)).InnerSimplified(isExact),
                Mulf(var z, Real factor) => (new Ref(z) * factor).InnerSimplified(isExact),
                Divf(var z, Real divisor) => (new Ref(z) / divisor).InnerSimplified(isExact),
                _ => null
            };

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument, a => RealPart(a, isExact),
                    (@this, a) => ((Ref)@this).New(a), isExact);
        }

        public partial record Imf
        {
            // Defined everywhere on the complex plane.
            private protected override Entity IntrinsicCondition => Boolean.True;

            // Real-linear as the real part is, and im(conj z) is -im(z). Not 0 for what is known to
            // be real: a codomain says what a node's values are and not where it has one, and
            // im(prime(x)) has no value at x = -3.
            private static Entity? ImaginaryPart(Entity argument, bool isExact) => argument switch
            {
                // A number that does not exist has no parts. NaN is a Real, whose imaginary part is 0, so
                // im(1/0) was 0. https://github.com/asc-community/AngouriMath/issues/2253
                Complex { IsNaN: true } => Real.NaN,
                Complex number => number.ImaginaryPart,
                Conjugatef(var z) => (-new Imf(z)).InnerSimplified(isExact),
                Sumf(var left, var right) => (new Imf(left) + new Imf(right)).InnerSimplified(isExact),
                Minusf(var left, var right) => (new Imf(left) - new Imf(right)).InnerSimplified(isExact),
                Mulf(Real factor, var z) => (factor * new Imf(z)).InnerSimplified(isExact),
                Mulf(var z, Real factor) => (new Imf(z) * factor).InnerSimplified(isExact),
                Divf(var z, Real divisor) => (new Imf(z) / divisor).InnerSimplified(isExact),
                _ => null
            };

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument, a => ImaginaryPart(a, isExact),
                    (@this, a) => ((Imf)@this).New(a), isExact);
        }

        public partial record Argf
        {
            // Every angle fits 0 = 0 e^(i t), so arg(0) has no value.
            private protected override Entity IntrinsicCondition => !Argument.EqualTo(Integer.Zero);

            // Exact where the number is: the axes are 0, pi and +-pi/2, and anywhere else the
            // angle is arctan(b/a), turned by pi into the left half-plane. arg(-1) is pi, the end
            // of (-pi, pi] that is included, as the principal logarithm's imaginary part is.
            private static Entity OfANumber(Complex number, bool isExact)
            {
                if (number.IsNaN || number.IsZero)
                    return Real.NaN;
                if (!isExact)
                    return number.Phase();
                var (a, b) = (number.RealPart, number.ImaginaryPart);
                if (b.IsZero)
                    return a.IsNegative ? MathS.pi : Integer.Zero;
                if (a.IsZero)
                    return (b.IsNegative ? -MathS.pi / 2 : MathS.pi / 2).InnerSimplified;
                var angle = MathS.Arctan(b / a).InnerSimplified;
                if (a.IsPositive)
                    return angle;
                var turn = b.IsPositive ? 1 : -1;
                // A table value of the arctangent is a rational multiple of pi, and the turn is
                // gathered into it: arg(-1 + i) is 3/4 pi rather than -pi/4 + pi.
                if ((angle / MathS.pi).InnerSimplified is Rational multiple)
                    return ((multiple + turn) * MathS.pi).InnerSimplified;
                return (angle + turn * MathS.pi).InnerSimplified;
            }

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument,
                    // Numbers only. A codomain says what a node's values are and not where it has
                    // one: arg(prime(x)) is not 0 at x = -3, where prime(x) has no value.
                    a => a is Complex number ? OfANumber(number, isExact) : null,
                    (@this, a) => ((Argf)@this).New(a), isExact);
        }

        public partial record Conjugatef
        {
            // Defined everywhere on the complex plane.
            private protected override Entity IntrinsicCondition => Boolean.True;

            // Real where the node's own codomain says so: a modulus, or a symbol declared with
            // domain(x, RR). A bare Variable is Domain.Any, so conjugate(x) stays. Not the codomain
            // setting, since what this returns is cached on the node and the setting is not.
            internal static bool IsKnownReal(Entity entity)
                => entity.Codomain is Domain.Prime or Domain.PositiveInteger or Domain.NonNegativeInteger
                                   or Domain.Integer or Domain.Rational or Domain.Real;

            // A field automorphism, so carried through the arithmetic and the whole powers, which
            // keeps the conjugate on the symbols and lets conjugate(a + b) - conjugate(a) -
            // conjugate(b) meet as 0. Not through a root or a logarithm, whose cuts break the
            // reflection: conjugate(sqrt(-1)) is -i and sqrt(conjugate(-1)) is i.
            private static Entity? Conjugated(Entity argument, bool isExact) => argument switch
            {
                Complex number => number.Conjugate,
                _ when IsKnownReal(argument) => argument,
                Conjugatef(var inner) => inner,
                Sumf(var left, var right) => (new Conjugatef(left) + new Conjugatef(right)).InnerSimplified(isExact),
                Minusf(var left, var right) => (new Conjugatef(left) - new Conjugatef(right)).InnerSimplified(isExact),
                Mulf(var left, var right) => (new Conjugatef(left) * new Conjugatef(right)).InnerSimplified(isExact),
                Divf(var left, var right) => (new Conjugatef(left) / new Conjugatef(right)).InnerSimplified(isExact),
                Powf(var @base, Integer exponent) => new Powf(new Conjugatef(@base), exponent).InnerSimplified(isExact),
                _ => null
            };

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnOneArgument(Argument, a => Conjugated(a, isExact),
                    (@this, a) => ((Conjugatef)@this).New(a), isExact);
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

        public partial record Polygammaf
        {
            // A whole order n >= 0, and an argument other than the poles 0, -1, -2, ...
            // https://github.com/asc-community/AngouriMath/issues/171
            private protected override Entity IntrinsicCondition =>
                Order.In(MathS.Sets.Z) & Order >= 0 & (!Argument.In(MathS.Sets.Z) | Argument > 0);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Order, Argument,
                    (order, argument) => (order, argument) switch
                    {
                        (Integer { EInteger: var n }, Rational q) when isExact && Number.ExactPolygamma(n, q.ERational) is { } exact => exact,
                        (Integer { EInteger: var n }, Complex z) when !isExact && n.Sign >= 0 => Number.Polygamma(n, z),
                        (Integer { EInteger: var n }, Real r) when n.Sign >= 0 && r.EDecimal.IsPositiveInfinity() => n.IsZero ? Real.PositiveInfinity : Integer.Zero,
                        _ => null
                    },
                    (@this, order, argument) => ((Polygammaf)@this).New(order, argument), isExact);
        }

        public partial record IncompleteGammaf
        {
            // Defined but at z = 0 where Re a <= 0, where the integral diverges at its lower limit.
            // https://github.com/asc-community/AngouriMath/issues/2075
            private protected override Entity IntrinsicCondition =>
                !LowerLimit.EqualTo(0) | Parameter > 0;

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Parameter, LowerLimit,
                    (a, z) => (a, z) switch
                    {
                        (Complex n, Complex x) when !isExact => Number.IncompleteGamma(n, x),
                        (_, Real { EDecimal: var x }) when x.IsPositiveInfinity() => Integer.Zero,
                        // Gamma(a, 0) is Gamma(a) where the integral converges at 0.
                        (Real { IsPositive: true } positive, Integer { IsZero: true }) => MathS.Gamma(positive).InnerSimplified,
                        // A whole n >= 1 is elementary, (n - 1)! e^(-z) sum_(k < n) z^k/k! (DLMF 8.4.8), and a half is
                        // sqrt(pi) erfc(sqrt(z)) (DLMF 8.4.6).
                        (Integer { EInteger: var n }, _) when n.Sign > 0 && n.CompareTo(30) <= 0 => WholeOrder(n.ToInt32Checked(), z),
                        (Rational { ERational: var h }, _) when h.Numerator.Equals(PeterO.Numbers.EInteger.One) && h.Denominator.Equals(PeterO.Numbers.EInteger.FromInt32(2))
                            => MathS.Sqrt(MathS.pi) * MathS.Erfc(MathS.Sqrt(z)),
                        _ => null
                    },
                    (@this, a, z) => ((IncompleteGammaf)@this).New(a, z), isExact);

            private static Entity WholeOrder(int n, Entity z)
            {
                Entity sum = Integer.One;
                Entity term = Integer.One;
                for (var k = 1; k < n; k++)
                {
                    term = term * z / k;
                    sum += term;
                }
                return (Number.Factorial(Integer.Create(n - 1)) * MathS.Pow(MathS.e, -z) * sum).InnerSimplified;
            }
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
                        // NaN has no sign, and its signum no value: EDecimal's Sign of NaN is 1, which made
                        // sgn(-1 provided false) 1. https://github.com/asc-community/AngouriMath/issues/2241
                        Real nan when nan.EDecimal.IsNaN() => nan,
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
            // Defined where both arguments are real: the complex numbers are not ordered, and the node is
            // read over the reals, so min(i, i) is NaN. The condition said True, and min(x, x) simplified
            // to x, which has a value at x = i. https://github.com/asc-community/AngouriMath/issues/2256
            private protected override Entity IntrinsicCondition => Left.In(MathS.Sets.R) & Right.In(MathS.Sets.R);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        // Only ordered arguments compare. A complex one is left alone rather
                        // than guessed at, which is what SymPy's Min does too.
                        (Real l, Real r) => l.EDecimal.CompareTo(r.EDecimal) <= 0 ? l : r,
                        // min(a, a) is a where a is real, and has no value where it is not.
                        var (l, r) when l == r => l.Provided(l.In(MathS.Sets.R)).InnerSimplified(isExact),
                        _ => null
                    },
                    (@this, a, b) => ((Minf)@this).New(a, b), isExact);
        }

        public partial record Maxf
        {
            // As Minf's: defined where both arguments are real. https://github.com/asc-community/AngouriMath/issues/2256
            private protected override Entity IntrinsicCondition => Left.In(MathS.Sets.R) & Right.In(MathS.Sets.R);

            /// <inheritdoc/>
            protected override Entity InnerSimplify(bool isExact)
                => ExpandOnTwoArguments(Left, Right,
                    (a, b) => (a, b) switch
                    {
                        (Real l, Real r) => l.EDecimal.CompareTo(r.EDecimal) >= 0 ? l : r,
                        var (l, r) when l == r => l.Provided(l.In(MathS.Sets.R)).InnerSimplified(isExact),
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
                        (Integer l, Integer r) => Integer.Create(l.EInteger.GreatestCommonDivisor(r.EInteger)),
                        // gcd(a/b, c/d) = gcd(a, c) / lcm(b, d) -- which is what SymPy gives,
                        // so gcd(1/2, 1/3) is 1/6 rather than an error.
                        (Rational l, Rational r) => Rational.Create(
                            l.Numerator.EInteger.GreatestCommonDivisor(r.Numerator.EInteger),
                            l.Denominator.EInteger / l.Denominator.EInteger.GreatestCommonDivisor(r.Denominator.EInteger) * r.Denominator.EInteger),
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
                            : Integer.Create((l.EInteger * r.EInteger).Abs() / l.EInteger.GreatestCommonDivisor(r.EInteger)),
                        // lcm(a/b, c/d) = lcm(a, c) / gcd(b, d), the mirror of the gcd's rule and
                        // what SymPy gives: lcm(1/2, 1/3) is 1.
                        (Rational l, Rational r) => l.Numerator.EInteger.IsZero || r.Numerator.EInteger.IsZero
                            ? Integer.Zero
                            : Rational.Create(
                                (l.Numerator.EInteger * r.Numerator.EInteger).Abs() / l.Numerator.EInteger.GreatestCommonDivisor(r.Numerator.EInteger),
                                l.Denominator.EInteger.GreatestCommonDivisor(r.Denominator.EInteger)),
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
