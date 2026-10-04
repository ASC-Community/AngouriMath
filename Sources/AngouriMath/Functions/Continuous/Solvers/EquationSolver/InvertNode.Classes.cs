//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Functions.Algebra;

namespace AngouriMath
{
    partial record Entity : ILatexizeable
    {
        partial record Number
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => throw new AngouriBugException("This function must contain " + nameof(x));
        }

        partial record Variable
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) => new[] { value };
        }

        partial record Matrix
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) => new[] { this };
        }

        // Each function and operator processing
        partial record Sumf
        {
            // x + a = value => x = value - a
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Augend.ContainsNode(x) ? Augend.Invert(value - Addend, x) : Addend.Invert(value - Augend, x);
        }

        partial record Minusf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Minuend.ContainsNode(x)
                // x - a = value => x = value + a
                ? Minuend.Invert(value + Subtrahend, x)
                // a - x = value => x = a - value. This used to return `value - Minuend`,
                // the negation of the right answer, which is why solving
                // `a = 1 / (b - c)` for c gave `1/a - b` instead of `b - 1/a`.
                : Subtrahend.Invert(Minuend - value, x);
        }

        partial record Mulf
        {
            // x * a = value => x = value / a
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Multiplier.ContainsNode(x)
                ? Multiplier.Invert(value / Multiplicand, x)
                : Multiplicand.Invert(value / Multiplier, x);
        }

        partial record Divf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Dividend.ContainsNode(x)
                // x / a = value => x = a * value
                ? Dividend.Invert(value * Divisor, x)
                // a / x = value => x = a / value
                : Divisor.Invert(Dividend / value, x);
        }

        partial record Modf
        {
            // x mod a = value has one solution per period, and is solved the way the
            // trigonometric inversions are, with a whole parameter n. The remainder is the
            // floored one, taking the sign of the divisor -- -1 mod 4 is 3, 5 mod (-4) is -3 and
            // 5/2 mod 2 is 1/2 -- so it takes each value in [0, a) once per period for a positive
            // a, and each in (a, 0] for a negative one. So x = value + |a| n for every whole n
            // where value is in that range -- the same family as value + a n, since n runs over
            // every whole number -- and there is no x where it is not: (x mod 4) + 1 = 3 is
            // x = 2 + 4 n, the congruence x = 2 (mod 4) written out. A divisor with x in it, or a
            // divisor or value that is not a number, leaves the equation unsolved, which is not
            // the same as claiming it has no solution.
            // https://github.com/asc-community/AngouriMath/issues/1629
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                if (Divisor.ContainsNode(x)
                    || Divisor.Evaled is not Real { IsFinite: true, IsZero: false } period
                    || value.Evaled is not Real { IsFinite: true } remainder)
                    return null;
                var inRange = period.IsNegative
                    ? (remainder.IsNegative || remainder.IsZero) && remainder > period
                    : !remainder.IsNegative && remainder < period;
                if (!inRange)
                    return Enumerable.Empty<Entity>();
                var step = period.IsNegative ? (-Divisor).InnerSimplified : Divisor;
                return Dividend.Invert(value + step * Variable.CreateUnique(this + value, "n"), x);
            }
        }

        partial record Powf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                if (Base.ContainsNode(x))
                {
                    if (Exponent is Integer { EInteger: var pow })
                        return Together(Number.GetAllRootsOf1(pow).Select(root => Base.Invert(root * MathS.Pow(value, 1 / Exponent), x)));
                    else
                        return Base.Invert(MathS.Pow(value, 1 / Exponent), x);
                }  
                // a ^ x = value => x = log(a, value)
                // TODO: determine how we should return periodic roots as
                // (e ^ x) ^ a != (e ^ a) ^ x for logs
                return Exponent.Invert(MathS.Log(Base, value), x);
            }
        }

        // TODO: Consider case when sin(sin(x)) where double-mention of n occures
        partial record Sinf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                // sin(x) = value => x = arcsin(value) + 2pi * n
                Argument.Invert(MathS.Arcsin(value) + 2 * MathS.pi * Variable.CreateUnique(this + value, "n"), x) is { } first
                // sin(x) = value => x = pi - arcsin(value) + 2pi * n
                && Argument.Invert(MathS.pi - MathS.Arcsin(value) + 2 * MathS.pi * Variable.CreateUnique(this + value, "n"), x) is { } second
                ? first.Concat(second) : null;
        }

        partial record Cosf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                // cos(x) = value => x = arccos(value) + 2pi * n
                Argument.Invert(MathS.Arccos(value) + 2 * MathS.pi * Variable.CreateUnique(this + value, "n"), x) is { } first
                // cos(x) = value => x = -arccos(value) + 2pi * n
                && Argument.Invert(-MathS.Arccos(value) + 2 * MathS.pi * Variable.CreateUnique(this + value, "n"), x) is { } second
                ? first.Concat(second) : null;
        }

        partial record Secantf
        {
            // sec(f(x)) = value
            // 1 / cos(f(x)) = value
            // 1 / value = cos(f(x))
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Argument.Cos().Invert(1 / value, x);
        }

        partial record Cosecantf
        {
            // csc(f(x)) = value
            // 1 / sin(f(x)) = value
            // 1 / value = sin(f(x))
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Argument.Sin().Invert(1 / value, x);
        }

        partial record Tanf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                // tan(x) = value => x = arctan(value) + pi * n
                Argument.Invert(MathS.Arctan(value) + MathS.pi * Variable.CreateUnique(this + value, "n"), x);
        }

        partial record Cotanf
        {
            // cotan(x) = value => x = arccotan(value) + pi * n
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Argument.Invert(MathS.Arccotan(value) + MathS.pi * Variable.CreateUnique(this + value, "n"), x);
        }

        partial record Logf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Base.ContainsNode(x)
                // log_x(a) = value => a = x ^ value => x = a ^ (1 / value)
                ? Base.Invert(MathS.Pow(Antilogarithm, 1 / value), x)
                // log_a(x) = value => x = a ^ value
                : Antilogarithm.Invert(MathS.Pow(Base, value), x);
        }

        partial record Arcsinf
        {
            [ConstantField] private static readonly Complex From = Complex.Create(-MathS.DecimalConst.pi / 2, Real.NegativeInfinity.EDecimal);
            [ConstantField] private static readonly Complex To = Complex.Create(MathS.DecimalConst.pi / 2, Real.PositiveInfinity.EDecimal);
            // arcsin(x) = value => x = sin(value)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                EntityInBounds(value, From, To) ? Argument.Invert(MathS.Sin(value), x) : Enumerable.Empty<Entity>();
        }

        partial record Arccosf
        {
            [ConstantField] private static readonly Complex From = Complex.Create(0, Real.NegativeInfinity.EDecimal);
            [ConstantField] private static readonly Complex To = Complex.Create(MathS.DecimalConst.pi, Real.PositiveInfinity.EDecimal);
            // arccos(x) = value => x = cos(value)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                EntityInBounds(value, From, To) ? Argument.Invert(MathS.Cos(value), x) : Enumerable.Empty<Entity>();
        }
        
        partial record Arctanf
        {
            [ConstantField] private static readonly Complex From = Complex.Create(-MathS.DecimalConst.pi / 2, Real.NegativeInfinity.EDecimal);
            [ConstantField] private static readonly Complex To = Complex.Create(MathS.DecimalConst.pi / 2, Real.PositiveInfinity.EDecimal);
            // arctan(x) = value => x = tan(value)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                EntityInBounds(value, From, To) ? Argument.Invert(MathS.Tan(value), x) : Enumerable.Empty<Entity>();
        }

        partial record Arccotanf
        {
            // TODO: Range should exclude Re(z) = 0
            [ConstantField] private static readonly Complex From = Complex.Create(-MathS.DecimalConst.pi / 2, Real.NegativeInfinity.EDecimal);
            [ConstantField] private static readonly Complex To = Complex.Create(MathS.DecimalConst.pi / 2, Real.PositiveInfinity.EDecimal);
            // arccotan(x) = value => x = cotan(value)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                EntityInBounds(value, From, To) ? Argument.Invert(MathS.Cotan(value), x) : Enumerable.Empty<Entity>();
        }

        partial record Arcsecantf
        {
            // arcsec(f(x)) = value
            // f(x) = sec(value)
            // x = f(x).Invert(sec(value))
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Argument.Invert(value.Sec(), x);
        }

        partial record Arccosecantf
        {
            // arccsc(f(x)) = value
            // f(x) = csc(value)
            // x = f(x).Invert(csc(value))
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Argument.Invert(value.Cosec(), x);
        }

        partial record Factorialf
        {
            // The factorial is the gamma function one along, and the gamma function has no
            // zeros anywhere in the complex plane, so x! = 0 has no roots. Every other value
            // has some -- x! = 6 at 3 -- and no node here writes them.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                value is Integer { IsZero: true } ? Enumerable.Empty<Entity>() : null;
        }

        // The error functions have no inverse among the library's nodes, so there is nothing to
        // write the preimage in -- and it is not empty: erf(x) = 1/2 at about 0.4769, and at
        // infinitely many complex points besides. https://github.com/asc-community/AngouriMath/issues/1501
        partial record Erff
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Erfcf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Erfif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        // Neither integral has an inverse among the library's nodes, and neither preimage is
        // empty: Ei(x) = 0 at about 0.3725, and li(x) = 0 at about 1.4513, Soldner's constant.
        // https://github.com/asc-community/AngouriMath/issues/1501
        partial record Eif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Lif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        // None of the four has an inverse among the library's nodes, and none of their preimages
        // is empty: Si(x) = 1 near 1.1654, Ci(x) = 0 near 0.6165.
        // https://github.com/asc-community/AngouriMath/issues/1501
        partial record Sif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Cif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Shif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Chif
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Binomialf
        {
            // The preimage of a binomial coefficient is not a function of either argument
            // that can be undone: binomial(n, k) = binomial(n, n - k), and 1 is taken at
            // every (n, 0) and (n, n). It has one all the same -- binomial(x, 2) = 3 at 3 and
            // at -2 -- so the equation is left unsolved rather than answered with none.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Iversonf
        {
            // Where an Iverson bracket is 1 is where its statement holds: a set, which the
            // statement solver answers and a list of values of x cannot. Left unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Alephf
        {
            // A size is no number an equation between numbers could be solved for.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Derivativef
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Expression.ContainsNode(x)
                ? Expression.Invert(MathS.Derivative(value, Var, -Iterations), x)
                : Enumerable.Empty<Entity>();
        }

        partial record Integralf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                Expression.ContainsNode(x)
                ? Range is { }
                  ? InnerSimplified is var integrated && integrated != this
                    ? integrated.Invert(value, x)
                    : Enumerable.Empty<Entity>()
                  : Expression.Invert(MathS.Derivative(value, Var), x)
                : Enumerable.Empty<Entity>();
        }

        partial record SumOverSetf
        {
            // The unknown sits under a binder; see Summationf below.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Maximumf
        {
            // The unknown sits under a binder; see Summationf below.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Minimumf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Argmaxf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Argminf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Summationf
        {
            // The unknown sits under a binder, and inverting would have to solve for it inside a
            // sum whose length may be symbolic. Declining is the honest answer -- and the wrong
            // one is on record: solving through an opaque Derivativef returns a non-solution,
            // https://github.com/asc-community/AngouriMath/issues/964. Declining is leaving the
            // equation unsolved; no roots would claim it has none.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Productf
        {
            // Same reasoning as Summationf above.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Limitf
        {
            // We can't just do a limit on the inverse function: https://math.stackexchange.com/q/3397326/627798
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x) =>
                null;
        }

        partial record Signumf
        {
            // signum(f(x)) = value
            // f(x) = value * pr
            // x = f(x).InvertNode(value * pr, x)
            // TODO: we need to make a piecewise for the case when signum(x) = n and |n| != 1
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => Argument.Invert(value * Variable.CreateUnique(Argument + value, "r"), x);
        }

        partial record Absf
        {
            // abs(f(x)) = value
            // f(x) = value * e ^ (i * n)
            // x = f(x).InvertNode(value * e ^ (i * n), x)
            //
            // value * e ^ (i * r) has modulus |value|, not value, so it solves the equation
            // only where the two agree -- that is, where value is real and not negative.
            // Without that condition abs(x) = -1 came back as { -e ^ (i * r) provided r in RR },
            // whose members have modulus 1 and solve nothing.
            // https://github.com/asc-community/AngouriMath/issues/812
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var @var = Variable.CreateUnique(value + Argument, "r");
                return Argument.Invert(value * MathS.e.Pow(MathS.i * @var), x)
                    ?.Select(c => c.Provided(@var.In(MathS.Sets.R) & new GreaterOrEqualf(value, 0)));
            }
        }

        partial record Floorf
        {
            // floor(f(x)) = value
            // solvable only where value is an integer, and then f(x) is anywhere in the
            // half-open interval [value, value + 1) -- so the preimage is a parameter t in
            // [0, 1) rather than a point, which is the same device Signumf and Absf use for
            // their own many-to-one inverses.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var t = Variable.CreateUnique(value + Argument, "t");
                return Argument.Invert(value + t, x)
                    ?.Select(c => c.Provided(
                        value.In(MathS.Sets.Z)
                        & t.In(MathS.Sets.R)
                        & new GreaterOrEqualf(t, 0)
                        & new Lessf(t, 1)));
            }
        }

        partial record Ceilf
        {
            // ceil(f(x)) = value
            // likewise integer-valued, with f(x) in (value - 1, value] -- so the parameter
            // is subtracted rather than added.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var t = Variable.CreateUnique(value + Argument, "t");
                return Argument.Invert(value - t, x)
                    ?.Select(c => c.Provided(
                        value.In(MathS.Sets.Z)
                        & t.In(MathS.Sets.R)
                        & new GreaterOrEqualf(t, 0)
                        & new Lessf(t, 1)));
            }
        }

        partial record Roundf
        {
            // round(f(x)) = value, solvable only for integer value, and then f(x) lies in
            // [value - 1/2, value + 1/2] -- closed at whichever end keeps the tie going to
            // the even integer, which depends on the parity of value. Rather than encode
            // that, the parameter covers the open interval and the two endpoints are left
            // out: every point named is a solution, which is the direction that matters.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var t = Variable.CreateUnique(value + Argument, "t");
                return Argument.Invert(value + t, x)
                    ?.Select(c => c.Provided(
                        value.In(MathS.Sets.Z)
                        & t.In(MathS.Sets.R)
                        & new Greaterf(t, Rational.Create(-1, 2))
                        & new Lessf(t, Rational.Create(1, 2))));
            }
        }

        partial record Minf
        {
            // min(a, b) = value says one of them is value and the other is no smaller, which
            // is a disjunction over which one it was rather than an inversion of a function
            // of x. Nothing here can express that, and inventing a branch would answer
            // confidently and wrongly, so this declines the way Factorialf and Phif do.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Maxf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Gcdf
        {
            // The preimage of a gcd is every pair whose greatest common divisor is the
            // value, which is not a function of x that can be undone.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Lcmf
        {
            // As for the gcd.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Boolean
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => throw new AngouriBugException("This function must contain " + nameof(x));
        }

        partial record Notf
        {
            // !f(x) = value
            // f(x) = !value
            // x = f(x).InvertNode(!value, x)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => Argument.Invert(!value, x);
        }

        partial record Andf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var (cont, notCont) = Left.ContainsNode(x) ? (Left, Right) : (Right, Left);

                if (cont.Invert(true, x) is not { } contInvTrue || cont.Invert(false, x) is not { } contInvFalse)
                    return null;

                // x and true = true => x = true
                var ifThoseTT = contInvTrue.Select(c => c.Provided(value & notCont));

                // x and true = false => x = false
                var ifThoseTF = contInvFalse.Select(c => c.Provided(notCont & !value));

                // x and false = false => x is any
                var ifThoseFF = contInvTrue.Concat(contInvFalse).Select(c => c.Provided(!notCont & !value));

                return ifThoseTT.Concat(ifThoseTF).Concat(ifThoseFF);
            }
        }

        partial record Orf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var (cont, notCont) = Left.ContainsNode(x) ? (Left, Right) : (Right, Left);

                if (cont.Invert(true, x) is not { } contInvTrue || cont.Invert(false, x) is not { } contInvFalse)
                    return null;

                // x and false = true => x = true
                var ifThoseFT = contInvTrue.Select(c => c.Provided(!notCont & value));

                // x and false = false => x = false
                var ifThoseFF = contInvFalse.Select(c => c.Provided(!notCont & !value));

                // x and true = true => x is any
                var ifThoseTT = contInvTrue.Concat(contInvFalse).Select(c => c.Provided(notCont & value));

                return ifThoseTT.Concat(ifThoseFT).Concat(ifThoseFF);
            }
        }

        partial record Xorf
        {
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                var (cont, notCont) = Left.ContainsNode(x) ? (Left, Right) : (Right, Left);
                if (cont.Invert(value, x) is not { } rootsIfBFalse || cont.Invert(!value, x) is not { } rootsIfBTrue)
                    return null;
                var ifBFalse = rootsIfBFalse.Select(c => c.Provided(!notCont));
                var ifBTrue = rootsIfBTrue.Select(c => c.Provided(notCont));
                return ifBFalse.Concat(ifBTrue);
            }
        }

        partial record Impliesf
        {
            
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                // f(x) implies b = value
                if (Assumption.ContainsNode(x))
                {
                    if (Assumption.Invert(false, x) is not { } rootsIfFXFalse || Assumption.Invert(true, x) is not { } rootsIfFXTrue)
                        return null;
                    // thoseIf{A}{B} is A == b, B == value

                    // b = true, value = true => f(x) can be any
                    var thoseIfTT = rootsIfFXFalse.Concat(rootsIfFXTrue).Select(c => c.Provided(Conclusion & value));

                    // b = false, value = true => f(x) is false
                    var thoseIfFT = rootsIfFXFalse.Select(c => c.Provided(!Conclusion & value));

                    // b = false, value = false => f(x) is true
                    var thoseIfFF = rootsIfFXTrue.Select(c => c.Provided(!Conclusion & !value));

                    return thoseIfTT.Concat(thoseIfFT).Concat(thoseIfFF);
                }
                // a implies f(x) = value
                else
                {
                    if (Conclusion.Invert(false, x) is not { } rootsIfFXFalse || Conclusion.Invert(true, x) is not { } rootsIfFXTrue)
                        return null;
                    // thoseIf{A}{B} is A == a, B == value

                    // a = true, value = true
                    var thoseIfTT = rootsIfFXTrue.Select(c => c.Provided(Assumption & value));

                    // a = false, value = true
                    var thoseIfFT = rootsIfFXFalse.Concat(rootsIfFXTrue).Select(c => c.Provided(!Conclusion & value));

                    // a = true, value = false
                    var thoseIfTF = rootsIfFXFalse.Select(c => c.Provided(Conclusion & !value));

                    return thoseIfTT.Concat(thoseIfFT).Concat(thoseIfTF);
                }
            }

        }

        partial record Equalsf
        {
            // A statement equal to a value has a set for its preimage, which the inverter cannot
            // write, so it declines, and the solver leaves the equation unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Greaterf
        {
            // A statement equal to a value has a set for its preimage, which the inverter cannot
            // write, so it declines, and the solver leaves the equation unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record GreaterOrEqualf
        {
            // A statement equal to a value has a set for its preimage, which the inverter cannot
            // write, so it declines, and the solver leaves the equation unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Lessf
        {
            // A statement equal to a value has a set for its preimage, which the inverter cannot
            // write, so it declines, and the solver leaves the equation unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record LessOrEqualf
        {
            // A statement equal to a value has a set for its preimage, which the inverter cannot
            // write, so it declines, and the solver leaves the equation unsolved.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Set
        {
            partial record FiniteSet
            {
                // set{,,,} = value
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => this == x ? new[] { value } : null;
            }

            partial record Interval
            {
                // [f(x), b] = value
                // [a, f(x)] = value
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                {
                    if (this == x)
                        return new[] { value };
                    if (x is FiniteSet fs && fs.Count == 1 && LeftClosed && RightClosed)
                        if (Left.ContainsNode(x))
                            return Left.Invert(Right, x)?.Select(c => c.Provided(MathS.Equality(Right, value)));
                        else
                            return Right.Invert(Left, x)?.Select(c => c.Provided(MathS.Equality(Left, value)));
                    return null;
                }
            }

            partial record ConditionalSet
            {
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => this == x ? new[] { value } : null;
            }

            partial record SpecialSet
            {
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => new[] { this };
            }

            partial record Unionf
            {
                // f(x) \/ A = value
                // f(x) = (value \ A) \/ PowerSet(A)
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                {
                    var (withX, withoutX) = Left.ContainsNode(x) ? (Left, Right) : (Right, Left);
                    if (value is FiniteSet valueFiniteSet && withoutX is FiniteSet A)
                    {
                        if (A.TryIsSubsetOf(valueFiniteSet, out var isSub) && isSub &&
                            FiniteSet.TryFullSubtract(valueFiniteSet, A, out var sub))
                        {
                            var answers = new List<Entity>();
                            foreach (var ans in A.GetPowerSet())
                            {
                                if (ans is not FiniteSet finiteSet)
                                    throw new AngouriBugException("PowerSet must return a set of sets");
                                if (withX.InvertNode(FiniteSet.Unite(sub, finiteSet), x) is not { } preimages)
                                    return null;
                                answers.AddRange(preimages);
                            }
                            return answers;
                        }
                        else return Empty;
                    }
                    return withX.InvertNode(value.SetSubtract(withoutX), x);
                }
            }

            partial record Intersectionf
            {
                // f(x) /\ A = value
                // Deciding the membership would need a piecewise condition, which is not written
                // here; declined.
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }

            partial record SetMinusf
            {
                // f(x) \ A = value
                // Deciding the membership would need a piecewise condition, which is not written
                // here; declined.
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }

            partial record Inf
            {
                // TODO: CSet is needed here => InvertNode to return a Set, not an IEnumerable
                // f(x) in A = value
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }

            partial record Subsetf
            {
                // A subset B = value asks for a set of sets, which the inverter cannot return.
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }

            partial record IndexedSetOperation
            {
                // The unknown sits under a binder; see Summationf.
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }

            partial record Powersetf
            {
                // powerset(f(x)) = value has no inverse the inverter can write.
                private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                    => null;
            }
        }

        partial record Valuationf
        {
            // valuation(n, p) = k has every multiple of p^k prime to p for a solution, a set the
            // inverter cannot hand back.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Primef
        {
            // prime(n) = p has the one solution n = pi(p) where p is prime and none otherwise,
            // and pi is not a node here; declined the way Phif is.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Phif
        {
            // We can't easily calculate (compute) all solutions there are for this function
            // There is an algorithm to find exactly one solution but we can do no more.
            // TODO: Mess with that...
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Cardf
        {
            // card(S) = n has every set of n elements for its solutions, which is not something
            // the inverter can hand back.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Dividesf
        {
            // (a divides x) = value has the multiples of a for its solutions -- a set, which the
            // inverter cannot return, the same as membership.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Congruentf
        {
            // (x = b (mod n)) = value has a residue class for its solutions -- a set, which the
            // inverter cannot return, the same as divisibility and membership.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Quantifier
        {
            // The unknown sits under a binder; see Summationf.
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Providedf
        {
            // (f(x) provided B) = value
            // f(x) = (value provided B)
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => Expression.InvertNode(value, x)?.Select(c => c.Provided(Predicate));
        }

        partial record Piecewise
        {            
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
            {
                Entity cond = true;
                var res = new List<Entity>();
                foreach (var c in Cases)
                {
                    if (c.Expression.InvertNode(value, x) is not { } preimages)
                        return null;
                    res.AddRange(preimages.Select(el => el.Provided(c.Predicate & cond)));
                    cond &= !c.Predicate;
                }
                return res;
            }
        }

        partial record Application
        {
            // TODO
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }

        partial record Lambda
        {
            // TODO
            private protected override IEnumerable<Entity>? InvertNode(Entity value, Entity x)
                => null;
        }
    }
}
