//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Multithreading;
using PeterO.Numbers;

namespace AngouriMath.Functions.Algebra
{
    using static Entity;
    using static Entity.Number;
    internal static class LimitSolvers
    {
        internal static Dictionary<EDecimal, Entity>? ParseAsPolynomial(Entity expr, Variable x)
        {
            var children = TreeAnalyzer.GatherLinearChildrenOverSumAndExpand(
                 expr, entity => entity.ContainsNode(x)
            );

            if (children is null)
                return null;

            var monomials = AnalyticalSolving.PolynomialSolver.GatherMonomialInformation
                <EDecimal, TreeAnalyzer.PrimitiveDecimal>(children, x, aPositiveVariable: true);
            if (monomials is null) return null;
            var filteredDictionary = new Dictionary<EDecimal, Entity>();
            foreach (var monomial in monomials)
            {
                var simplified = monomial.Value.InnerSimplified;
                if (simplified != Integer.Zero)
                {
                    filteredDictionary.Add(monomial.Key, simplified);
                }
            }
            return filteredDictionary;
        }
        [ConstantField] private static readonly Real Infinity = Real.PositiveInfinity;
        internal static Entity? SolveBySubstitution(Entity expr, Variable x)
        {
            var res = expr.Substitute(x, Infinity);
            // Reading the value at the destination is the limit only where the expression is continuous there, and a step
            // function whose argument arrives on one of its jumps is not: floor(x) - 1 from the left of 0 is floor(-1/t) - 1
            // at t = +oo here, whose -1/oo evaluates to 0, and the floor of that is 0 where it is -1 all the way in. The
            // nodes decline a jump themselves (LimitOfAStepFunction, Signumf's override), and this is where the declined
            // node was answered anyway, as part of the expression it is in.
            // https://github.com/asc-community/AngouriMath/issues/2266
            if (res.Nodes.Any(SitsOnAJump))
                return null;
            if (res.Evaled is Complex limit)
            {
                MultithreadingFunctional.ExitIfCancelled();
                if (limit == Real.NaN) return null;
                // Reading the value at the destination is the limit only where the expression
                // is continuous there, and an indeterminate form is exactly where it is not.
                // Every other one of them already declines on the line above, because this
                // library's arithmetic answers 0 * oo, oo - oo, oo / oo and 0^0 with NaN. The
                // two power forms do not: (+oo)^0 and 1^oo evaluate to 1 -- the same
                // convention SymPy and IEEE 754's pow use, and not one this touches -- so a
                // limit that assembled one of them read that 1 off as its answer, and
                // lim x->+oo (x!)^(1/x) came back 1 where it is +oo.
                // https://github.com/asc-community/AngouriMath/issues/754
                if (LimitFunctional.HoldsAnIndeterminatePowerForm(res)) return null;
                if (!limit.RealPart.IsFinite)
                    return limit.RealPart; // TODO: sometimes we get { oo + value * i } so we assume it is just infinity
                if (limit == Integer.Zero) return limit;

                return res;
            }
            return null;
        }

        /// <summary>
        /// Whether <paramref name="node"/> is a step function whose argument evaluates to one of its jumps: an integer for
        /// <c>floor</c> and <c>ceil</c>, a half-integer for <c>round</c>, 0 for <c>sgn</c>. Taken on the real part, and on the
        /// imaginary part of a value that is not real, as LimitOfAStepFunction takes it.
        /// </summary>
        private static bool SitsOnAJump(Entity node)
        {
            static bool Whole(Real part) => part is Integer || part.EDecimal.IsFinite && part.EDecimal.CompareTo(part.EDecimal.Floor()) == 0;
            static bool OnAJump(Entity argument, System.Func<Real, bool> jump)
                => argument.Evaled is Complex { RealPart: var real, ImaginaryPart: var imaginary } value
                   && (jump(real) || value is not Real && jump(imaginary));
            return node switch
            {
                Floorf(var argument) => OnAJump(argument, Whole),
                Ceilf(var argument) => OnAJump(argument, Whole),
                Roundf(var argument) => OnAJump(argument, part => Whole((Real)(part + Rational.Create(1, 2)).Evaled)),
                Signumf(var argument) => argument.Evaled is Complex { IsZero: true },
                _ => false,
            };
        }

        internal static Entity? SolveAsPolynomial(Entity expr, Variable x)
        {
            if (ParseAsPolynomial(expr, x) is { } mono)
            {
                MultithreadingFunctional.ExitIfCancelled();
                var maxPower = mono.Keys.Max() ?? throw new AngouriBugException("No null expected");
                return
                    maxPower.IsZero
                    ? mono[maxPower]
                    : maxPower.IsNegative
                    ? 0
                    : mono[maxPower].Evaled is Complex power
                    ? Infinity * power
                    : Infinity * mono[maxPower];
            }
            else return null;
        }

        internal static Entity? SolvePolynomialDivision(Entity expr, Variable x)
        {
            if (expr is Divf(var P, var Q))
            {
                var monoP = ParseAsPolynomial(P, x);
                var monoQ = ParseAsPolynomial(Q, x);

                if (monoP is { } && monoQ is { })
                {
                    var maxPowerP = monoP.Keys.Max() ?? throw new AngouriBugException("No null expected");
                    var maxPowerQ = monoQ.Keys.Max() ?? throw new AngouriBugException("No null expected");
                    MultithreadingFunctional.ExitIfCancelled();
                    var maxTermP = monoP[maxPowerP];
                    var maxTermQ = monoQ[maxPowerQ];
                    if (maxPowerP.CompareTo(maxPowerQ) > 0)
                    {
                        var term = maxTermP / maxTermQ;
                        if (term.Evaled is Number eval)
                        {
                            var result = Infinity * eval;
                            return result == Real.NaN ? null : (Entity)result;
                        }
                        else return Infinity * term;
                    }
                    else if (maxPowerP.CompareTo(maxPowerQ) == 0)
                    {
                        MultithreadingFunctional.ExitIfCancelled();
                        var termPSimplified = maxTermP.InnerSimplified;
                        var termQSimplified = maxTermQ.InnerSimplified;
                        return termPSimplified / termQSimplified;
                    }
                    else return 0;
                }
            }
            return null;
        }

        internal static Entity? SolveAsLogarithm(Entity expr, Variable x)
        {
            if (expr is Logf(var logBase, var logArgument))
            {
                if (logBase.ContainsNode(x))
                    return SolveAsLogarithmDivision(MathS.Ln(logArgument) / MathS.Ln(logBase), x);
                else
                {
                    MultithreadingFunctional.ExitIfCancelled();
                    var innerLimit = LimitFunctional.ComputeLimit(logArgument, x, Real.PositiveInfinity);
                    if (innerLimit is null) return null;
                    // do same as wolframalpha: https://www.wolframalpha.com/input/?i=ln%28-inf%29
                    if (innerLimit == Real.NegativeInfinity) return Real.PositiveInfinity;
                    if (innerLimit == Real.PositiveInfinity) return Real.PositiveInfinity;
                    if (innerLimit == Integer.Zero) return Real.NegativeInfinity;
                    return MathS.Log(logBase, innerLimit);
                }
            }
            else return null;
        }

        /// <summary>
        /// The squeeze theorem, in the one shape that does not need the bounded factor's own
        /// limit: a factor bounded near the destination times one that vanishes there tends to
        /// zero, however wildly the bounded one oscillates. Also written as a quotient, where a
        /// bounded dividend over a diverging divisor is the same statement.
        /// </summary>
        /// <remarks>
        /// Every other rule reads a limit as a value -- the descent puts each part's own limit in
        /// place of the part, l'Hopital's rule wants a determinate quotient, Gruntz compares rates
        /// of growth. <c>sin(x)</c> has no limit at infinity, so each of them correctly declines
        /// and the product is left indeterminate in the shape <c>(no limit) * 0</c>. What the
        /// theorem needs of the factor is not its limit but its boundedness, which is a weaker
        /// fact and one that can be read off the shape --
        /// https://github.com/asc-community/AngouriMath/issues/723.
        /// </remarks>
        internal static Entity? SolveAsBoundedTimesVanishing(Entity expr, Variable x)
        {
            switch (expr)
            {
                case Mulf(var multiplier, var multiplicand):
                    if (IsBoundedAtInfinity(multiplier, x) && VanishesAtInfinity(multiplicand, x))
                        return Integer.Zero;
                    if (IsBoundedAtInfinity(multiplicand, x) && VanishesAtInfinity(multiplier, x))
                        return Integer.Zero;
                    return null;
                case Divf(var dividend, var divisor):
                    if (IsBoundedAtInfinity(dividend, x) && DivergesAtInfinity(divisor, x))
                        return Integer.Zero;
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// That a sine or a cosine has no limit where its argument grows without bound: it takes
        /// every value in its range infinitely often on the way in and settles on none of them.
        /// </summary>
        /// <remarks>
        /// NaN is the claim that there is no limit, which is a different and stronger statement
        /// than leaving the node unevaluated -- that one says only that none of the rules found
        /// one. The distinction is worth making where it can be: it is the one this library draws
        /// between a limit it has decided against and a limit it has not reached.
        /// <para/>
        /// Only for a real argument, as with boundedness above, and for the same reason:
        /// sin(i * t) diverges rather than oscillating.
        /// </remarks>
        internal static Entity? SolveAsOscillationWithoutLimit(Entity expr, Variable x)
            => expr is Sinf or Cosf
               && IsRealValued(expr.DirectChildren[0], x)
               && DivergesAtInfinity(expr.DirectChildren[0], x)
                ? Real.NaN
                : null;

        private static bool VanishesAtInfinity(Entity expr, Variable x)
            => LimitFunctional.ComputeLimit(expr, x, Real.PositiveInfinity) is { } limit
               && limit.Evaled is Real { IsZero: true };

        private static bool DivergesAtInfinity(Entity expr, Variable x)
            => LimitFunctional.ComputeLimit(expr, x, Real.PositiveInfinity) is { } limit
               && limit.Evaled is Real { IsFinite: false, IsNaN: false };

        /// <summary>
        /// Whether the expression stays within some finite bound as x grows, established without
        /// asking what it tends to. Nothing is claimed for a shape not listed here.
        /// </summary>
        /// <remarks>
        /// Deliberately not "has a finite limit": that is the case the rest of the machinery
        /// already covers, and asking for it here would only repeat work while missing the
        /// factors this exists for, which have no limit at all.
        /// </remarks>
        private static bool IsBoundedAtInfinity(Entity expr, Variable x)
        {
            if (!expr.ContainsNode(x))
                return expr.Evaled is Number { IsFinite: true };
            switch (expr)
            {
                // A sine and a cosine never exceed 1 in magnitude and a sign never exceeds 1;
                // an arctangent and an arccotangent stay within an interval of angles. Each of
                // those is a fact about a real argument, and none of them holds otherwise:
                // sin(i * t) is i * sinh(t), which grows without bound.
                case Sinf or Cosf or Signumf or Arctanf or Arccotanf:
                    return IsRealValued(expr.DirectChildren[0], x);
                case Absf(var argument):
                    return IsBoundedAtInfinity(argument, x);
                case Mulf(var multiplier, var multiplicand):
                    return IsBoundedAtInfinity(multiplier, x) && IsBoundedAtInfinity(multiplicand, x);
                case Sumf(var augend, var addend):
                    return IsBoundedAtInfinity(augend, x) && IsBoundedAtInfinity(addend, x);
                case Minusf(var minuend, var subtrahend):
                    return IsBoundedAtInfinity(minuend, x) && IsBoundedAtInfinity(subtrahend, x);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether the expression is real wherever it is defined, given that x is. The question
        /// differentiation asks about a modulus and a sign, so it is answered in one place.
        /// </summary>
        private static bool IsRealValued(Entity expr, Variable x)
            => TreeAnalyzer.IsRealValued(expr, x);

        /// <summary>
        /// Whether the logarithm of something tending to <paramref name="argumentLimit"/>
        /// diverges, which is where the argument tends to zero or to an infinity.
        /// </summary>
        /// <remarks>
        /// Read off the value wherever the limit has one, and off its form only where it does
        /// not. A limit comes back in whatever form the reading that found it left it in, and
        /// the substitution leaves the expression with +oo put in for x: cos(1/x) tends to
        /// <c>cos(1 / +oo)</c>, which has an infinity in it and is 1. Read off the form, its
        /// logarithm diverged, so ln(x) / ln(cos(x)) at 0+ -- -oo over a divisor vanishing from
        /// below, which is +oo -- was taken for oo/oo and differentiated into
        /// (1/x) / (-tan(x)), and answered -oo. l'Hopital's rule says nothing about a quotient
        /// whose divisor vanishes and whose dividend does not.
        /// https://github.com/asc-community/AngouriMath/issues/2267
        /// <para/>
        /// A limit that is not a finite number -- <c>+oo * a</c>, or an infinity with an
        /// imaginary part -- is still read off its form.
        /// </remarks>
        private static bool LogarithmDiverges(Entity argumentLimit)
            => argumentLimit.Evaled is Complex { IsFinite: true } value
                ? value == Integer.Zero
                : argumentLimit.Nodes.Any(child => child == Real.PositiveInfinity || child == Real.NegativeInfinity);

        internal static Entity? SolveAsLogarithmDivision(Entity expr, Variable x)
        {
            if (expr is Divf(Logf(var upperLogBase, var upperLogArgument), Logf(var lowerLogBase, var lowerLogArgument)))
            {
                if (lowerLogBase.ContainsNode(x) || upperLogBase.ContainsNode(x)) return null;
                MultithreadingFunctional.ExitIfCancelled();
                var upperLogLimit = LimitFunctional.ComputeLimit(upperLogArgument, x, Real.PositiveInfinity);
                var lowerLogLimit = LimitFunctional.ComputeLimit(lowerLogArgument, x, Real.PositiveInfinity);
                if (upperLogLimit is null || lowerLogLimit is null) return null;
                MultithreadingFunctional.ExitIfCancelled();
                if (LogarithmDiverges(upperLogLimit) && LogarithmDiverges(lowerLogLimit))
                {
                    var upperDerivative = upperLogArgument.Differentiate(x).InnerSimplified;
                    var lowerDerivative = lowerLogArgument.Differentiate(x).InnerSimplified;
                    // A derivative the library cannot compute comes back *unevaluated* rather than
                    // as NaN (https://github.com/asc-community/AngouriMath/issues/958). Handing one
                    // to ComputeLimit asks the very question that could not be answered, and the
                    // factorial reaches here through Stirling, so this technique declines instead
                    // of recurring. NaN used to end the recursion by poisoning the quotient, which
                    // stopped the loop for the wrong reason -- it also asserted the derivative did
                    // not exist.
                    if (upperDerivative.Nodes.Any(node => node is Derivativef)
                        || lowerDerivative.Nodes.Any(node => node is Derivativef))
                        return null;
                    var p = (upperDerivative / upperLogArgument).InnerSimplified;
                    var q = (lowerDerivative / lowerLogArgument).InnerSimplified;
                    return LimitFunctional.ComputeLimit(p / q, x, Real.PositiveInfinity);
                }
                else

                {
                    var div = (MathS.Ln(upperLogLimit) / MathS.Ln(lowerLogLimit));
                    var divEvaled = div.Evaled;
                    return divEvaled switch
                    {
                        { Evaled.IsNaN: true } => null,
                        { } res when res.ContainsNode("+oo") || res.ContainsNode("-oo") => div.InnerSimplified,
                        { Evaled: Complex } limit => limit,
                        // The logarithm is continuous, so the quotient tends to the quotient of the
                        // limits' logarithms, not of the limits: ln(y + x) / ln(z + x) at 0 answered
                        // y / z -- https://github.com/asc-community/AngouriMath/issues/2271.
                        _ => div.InnerSimplified,
                    };
                }
            }

            return null;
        }
    }
}
