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
                // An infinity is not a coefficient: -oo / x is -oo at every x on the way to +oo,
                // and read as a constant over a line it was answered 0. The descent's
                // AnInfinityTimesASign is what answers it. Nor is a NaN: 1 ^ log(1 - x, x - x) is 1
                // at every point near 1/2, and a coefficient evaluated to NaN answered NaN.
                if (simplified.Evaled is Number { IsFinite: false })
                    return null;
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
            // Nor where a step's argument is real at the destination and complex just short of it, which the node
            // declines too (LimitOfAStepFunction): x / floor(arcsin(x)) at 1 from the right is x / floor(arcsin(1 + 1/t))
            // here, whose arcsin(1) = pi/2 has the floor 1, where the floor all the way in is 1 - i, and the quotient was
            // answered 1 where it is (1 + i)/2.
            if (expr.Nodes.Any(node => node is Floorf or Ceilf or Roundf && node.ContainsNode(x)
                    && IsComplexJustShortOfInfinity(node.DirectChildren[0], x)))
                return null;
            if (res.Evaled is Complex limit)
            {
                MultithreadingFunctional.ExitIfCancelled();
                if (limit == Real.NaN) return null;
                // Nor where a logarithm's argument, or a power's base, arrives on the cut off the
                // real line, where the value there is the limit from above and not from below.
                // https://github.com/asc-community/AngouriMath/issues/2266
                if (expr.Nodes.Any(node => LimitFunctional.TakesItsValueOnACut(node, x, Infinity))) return null;
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

        /// <summary>Whether <paramref name="argument"/> is real at +oo and not at a large finite point short of it.</summary>
        private static bool IsComplexJustShortOfInfinity(Entity argument, Variable x)
            => argument.Substitute(x, Infinity).Evaled is Real { IsNaN: false }
               && LimitFunctional.AtALargeDecimal(argument, x) is Complex and not Real;

        /// <summary>
        /// Whether <paramref name="node"/> is a step function, or an arccotangent, whose argument evaluates to one of its jumps: an integer for
        /// <c>floor</c> and <c>ceil</c>, a half-integer for <c>round</c>, 0 for <c>sgn</c> and <c>arccot</c>. Taken on the real part, and on the
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
                // The arccotangent is arctan(1/t), with range (-pi/2, pi/2], and jumps at 0 from -pi/2 to pi/2:
                // arccot(-0.001) = -1.5698, arccot(0) = pi/2. Its override answers it from the side.
                Arccotanf(var argument) => argument.Evaled is Complex { IsZero: true },
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
                    if (LimitFunctional.ArrivesOnTheCutOffTheRealLine(logArgument, innerLimit, x, Real.PositiveInfinity)) return null;
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
        /// The other shapes the squeeze theorem's premise settles, where the bounded part is also
        /// kept away from 0, or is a term rather than a factor: a part vanishing over one bounded
        /// away from 0 tends to 0, a part diverging times or over one bounded away from 0 diverges
        /// with the sign the product takes, and a diverging term beside a bounded one diverges.
        /// </summary>
        /// <remarks>
        /// <c>e^x / e^(cos(x))</c> at -oo is 0, and was answered NaN: the descent put
        /// <c>e^(cos(x))</c>'s own limit, which does not exist, in place of the divisor, and a
        /// quotient of a limit and no limit read as no limit. The divisor has no limit and still
        /// lies in <c>[1/e, e]</c> all the way in, which is all the quotient needs of it. The
        /// bound is read off the shape (<see cref="Enclosure"/>), never off sampled values, and
        /// where the enclosure reaches 0 nothing is claimed: <c>x / (1 + cos(x))</c> stays
        /// unanswered, as it has poles all the way in.
        /// </remarks>
        internal static Entity? SolveWithABoundedPart(Entity expr, Variable x)
        {
            // Not over an expression with an infinity or a NaN written in it. Such a part "diverges"
            // only because the infinity is already there: tanh(log(x + 1, 0)) at -1 from the right
            // reaches e^(+oo / ln(x)) - 1, which the descent answers +oo, and its quotient by
            // e^(+oo / ln(x)) + 1, read as +oo / +oo, was answered 0 where the values settle at 1.
            if (expr.Nodes.Any(static node => node is Number { IsFinite: false }))
                return null;
            switch (expr)
            {
                case Mulf(var multiplier, var multiplicand):
                    return DivergingTimesAwayFromZero(multiplicand, multiplier, x)
                        ?? DivergingTimesAwayFromZero(multiplier, multiplicand, x);
                case Divf(var dividend, var divisor):
                    if (SignAwayFromZero(divisor, x) is not { } divisorSign)
                        return null;
                    // Only a real-valued dividend: 0 swallows a side that is not real, which has no
                    // limit under the convention x^x at 0 is held to. sinh(-1) (x^x - 1) at 0 is read
                    // from the left as ((-1/x)^(-1/x) - 1) (1 - e^2) / e, and came to 0 two-sided.
                    if (IsRealValued(dividend, x) && VanishesAtInfinity(dividend, x))
                        return Integer.Zero;
                    return InfinityAtInfinity(dividend, x) is { } dividendInfinity
                        ? Signed(dividendInfinity, divisorSign)
                        : null;
                case Sumf(var augend, var addend):
                    return DivergingBesideBounded(augend, addend, x, 1)
                        ?? DivergingBesideBounded(addend, augend, x, 1);
                case Minusf(var minuend, var subtrahend):
                    return DivergingBesideBounded(minuend, subtrahend, x, 1)
                        ?? DivergingBesideBounded(subtrahend, minuend, x, -1);
                default:
                    return null;
            }

            static Entity? DivergingTimesAwayFromZero(Entity diverging, Entity bounded, Variable x)
                => SignAwayFromZero(bounded, x) is { } sign && InfinityAtInfinity(diverging, x) is { } infinity
                    ? Signed(infinity, sign)
                    : null;

            // The diverging part, taken with the sign it carries in the sum, beside a bounded one.
            static Entity? DivergingBesideBounded(Entity diverging, Entity bounded, Variable x, int sign)
                => diverging.ContainsNode(x) && Enclosure(bounded, x) is { } && InfinityAtInfinity(diverging, x) is { } infinity
                    ? Signed(infinity, sign)
                    : null;

            static Entity Signed(Real infinity, int sign)
                => (infinity == Real.PositiveInfinity) == (sign > 0) ? Real.PositiveInfinity : Real.NegativeInfinity;
        }

        /// <summary>
        /// The sign of an expression whose <see cref="Enclosure"/> excludes 0, or
        /// <see langword="null"/> where it does not or there is none. A constant counts:
        /// x - cos(x) at -oo is read at +oo as -1 * (x + cos(x)).
        /// </summary>
        private static int? SignAwayFromZero(Entity expr, Variable x)
            => Enclosure(expr, x) is var (low, high)
                ? low > 0 ? 1 : high < 0 ? -1 : null
                : null;

        /// <summary>
        /// The real infinity the expression tends to, where it is real on the way in, or
        /// <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Real on the way in, and not only in its limit: a side on which the expression is not
        /// real has no limit under the convention x^x at 0 is held to, and a bounded part beside
        /// it must not give it one. tanh(x^cos(2)) at 0 from the left is read at +oo through
        /// (-1/x)^cos(2), complex for every x, and came to 1, and two-sided to 1 where it is NaN.
        /// </remarks>
        private static Real? InfinityAtInfinity(Entity expr, Variable x)
            => IsRealNearInfinity(expr, x)
               && LimitFunctional.ComputeLimit(expr, x, Real.PositiveInfinity) is { } limit
               && limit.Evaled is Real { IsFinite: false, IsNaN: false } infinity
                ? infinity
                : null;

        /// <summary>
        /// Whether the expression is real for every large enough x: real wherever it is defined,
        /// or real where the bases of its roots and the arguments of its logarithms are positive,
        /// and each of those tends to a positive number or to +oo. One tending to 0 is declined,
        /// since it says nothing about the side it arrives from.
        /// </summary>
        private static bool IsRealNearInfinity(Entity expr, Variable x)
            => IsRealValued(expr, x)
               || TreeAnalyzer.WhereRealValued(expr, x) is { } where
                  && where.Nodes.OfType<Greaterf>().ToList() is { Count: > 0 } conditions
                  && conditions.All(condition =>
                      LimitFunctional.ComputeLimit(condition.Left, x, Real.PositiveInfinity) is { } limit
                      && limit.Evaled is Real { IsNaN: false, IsPositive: true, IsZero: false });

        /// <summary>
        /// An interval the expression stays within for every real x, read off its shape: a
        /// sine, a cosine, a sign and an error function of a real argument lie in [-1, 1], an
        /// arctangent and an arccotangent in [-pi/2, pi/2], a positive constant to a power is
        /// monotone in the power, and sums, differences, products, quotients by an interval
        /// without 0, moduli and positive whole powers combine intervals. <see langword="null"/>
        /// for any other shape, including anything an x appears in bare: nothing is sampled.
        /// </summary>
        /// <remarks>
        /// In doubles, and widened outward by a relative 1e-12 at every step, so that rounding
        /// can only make the interval larger: 1 + cos(x) comes out reaching just below 0, and so
        /// is not taken to be bounded away from it, which it is not.
        /// </remarks>
        private static (double Low, double High)? Enclosure(Entity expr, Variable x)
        {
            if (!expr.ContainsNode(x))
                return expr.Evaled is Real { IsFinite: true, IsNaN: false } value
                    ? Widened(value.AsDouble(), value.AsDouble())
                    : null;
            switch (expr)
            {
                case Sinf or Cosf or Signumf or Erff when IsRealValued(expr.DirectChildren[0], x):
                    return (-1, 1);
                case Arctanf or Arccotanf when IsRealValued(expr.DirectChildren[0], x):
                    return Widened(-System.Math.PI / 2, System.Math.PI / 2);
                case Absf(var argument):
                    if (Enclosure(argument, x) is not var (low, high))
                        return null;
                    return Widened(low > 0 ? low : high < 0 ? -high : 0, System.Math.Max(System.Math.Abs(low), System.Math.Abs(high)));
                case Sumf(var augend, var addend):
                    if (Enclosure(augend, x) is not var (augendLow, augendHigh) || Enclosure(addend, x) is not var (addendLow, addendHigh))
                        return null;
                    return Widened(augendLow + addendLow, augendHigh + addendHigh);
                case Minusf(var minuend, var subtrahend):
                    if (Enclosure(minuend, x) is not var (minuendLow, minuendHigh) || Enclosure(subtrahend, x) is not var (subtrahendLow, subtrahendHigh))
                        return null;
                    return Widened(minuendLow - subtrahendHigh, minuendHigh - subtrahendLow);
                case Mulf(var multiplier, var multiplicand):
                    return Enclosure(multiplier, x) is { } left && Enclosure(multiplicand, x) is { } right
                        ? Product(left, right) : null;
                case Divf(var dividend, var divisor):
                    if (Enclosure(dividend, x) is not { } over || Enclosure(divisor, x) is not var (divisorLow, divisorHigh)
                        || divisorLow <= 0 && divisorHigh >= 0)
                        return null;
                    return Product(over, Widened(1 / divisorHigh, 1 / divisorLow));
                // e^g, and any positive constant to a power, is monotone in the power.
                case Powf(var @base, var exponent) when !@base.ContainsNode(x):
                    if (@base.Evaled is not Real { IsFinite: true, IsPositive: true } positive
                        || Enclosure(exponent, x) is not var (exponentLow, exponentHigh))
                        return null;
                    var (atLow, atHigh) = (System.Math.Pow(positive.AsDouble(), exponentLow), System.Math.Pow(positive.AsDouble(), exponentHigh));
                    return IsAFiniteDouble(atLow) && IsAFiniteDouble(atHigh)
                        ? Widened(System.Math.Min(atLow, atHigh), System.Math.Max(atLow, atHigh)) : null;
                case Powf(var @base, Integer { EInteger: var power }) when power.Sign > 0 && power.CompareTo(64) <= 0:
                    if (Enclosure(@base, x) is not { } baseEnclosure)
                        return null;
                    var result = baseEnclosure;
                    for (var i = 1; i < power.ToInt32Checked(); i++)
                        result = Product(result, baseEnclosure);
                    return IsAFiniteDouble(result.Low) && IsAFiniteDouble(result.High) ? result : null;
                default:
                    return null;
            }

            // double.IsFinite is not on netstandard2.0, which the library ships.
            static bool IsAFiniteDouble(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

            static (double Low, double High) Product((double Low, double High) left, (double Low, double High) right)
            {
                var corners = new[] { left.Low * right.Low, left.Low * right.High, left.High * right.Low, left.High * right.High };
                return Widened(corners.Min(), corners.Max());
            }

            static (double Low, double High) Widened(double low, double high)
                => (low - 1e-12 * (1 + System.Math.Abs(low)), high + 1e-12 * (1 + System.Math.Abs(high)));
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

        /// <summary>
        /// A step function of an unbounded argument times a factor that vanishes: the limit of
        /// <c>f * floor(g)</c> is the limit of <c>f * g</c>, and the same for <c>ceil</c> and
        /// <c>round</c>.
        /// </summary>
        /// <remarks>
        /// <c>floor(g) = g - r</c> with <c>0 &lt;= r &lt; 1</c> wherever <c>g</c> is real, <c>ceil(g) = g + r</c>
        /// with the same bound, and <c>round(g)</c> is within 1/2 of <c>g</c>. So
        /// <c>f * floor(g) - f * g</c> is <c>f</c> times something bounded by 1, which tends to 0
        /// wherever <c>f</c> does, and the two products have the same limit, a value or an
        /// infinity alike. Assumed, and checked: <c>g</c> is real-valued, and the other factors
        /// tend to 0. Nothing is assumed of <c>g</c>'s limit, which is the point -- the descent
        /// puts each part's own limit in place of the part, so <c>x floor(2/x)</c> at 0 was read
        /// as <c>0 * floor(+oo)</c>, a form it cannot settle, where it is 2 from either side.
        /// <para/>
        /// Only a limit is taken from it. A <c>NaN</c> for <c>f * g</c> would say the same of the
        /// step, but inside the machinery <c>NaN</c> can also be a form nothing read, so it is left
        /// to the rules that already decide those.
        /// </remarks>
        internal static Entity? SolveAsAStepOfAnUnboundedArgument(Entity expr, Variable x)
        {
            if (expr is not (Mulf or Divf) || !expr.Nodes.Any(static node => node is Floorf or Ceilf or Roundf))
                return null;
            var factors = Mulf.LinearChildren(expr);
            for (var i = 0; i < factors.Count; i++)
            {
                if (factors[i] is not (Floorf or Ceilf or Roundf) || !factors[i].ContainsNode(x))
                    continue;
                var argument = factors[i].DirectChildren[0];
                if (!IsRealValued(argument, x))
                    continue;
                var rest = factors.Where((_, j) => j != i).Aggregate((Entity)Integer.One, static (product, factor) => product * factor);
                if (!rest.ContainsNode(x) || !VanishesAtInfinity(rest, x))
                    continue;
                if (LimitFunctional.ComputeLimit((rest * argument).InnerSimplified, x, Real.PositiveInfinity) is { } limit
                    && limit.Evaled != MathS.NaN)
                    return limit;
            }
            return null;
        }

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
                    return Enclosure(expr, x) is { };
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
                    // Nor through a step that jumps all the way in, for the reason l'Hopital's rule
                    // itself declines it: ln(floor(x)) / ln(x) answered 0.
                    // https://github.com/asc-community/AngouriMath/issues/2281
                    if (LimitFunctional.HasAStepThatJumpsNear(upperLogArgument, x, Real.PositiveInfinity, ApproachFrom.BothSides)
                        || LimitFunctional.HasAStepThatJumpsNear(lowerLogArgument, x, Real.PositiveInfinity, ApproachFrom.BothSides))
                        return null;
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
                    // (u'/u) / (v'/v) asked as the one quotient u' v / (u v'): asked as a quotient of
                    // quotients, the limit of ln(x - 1) / ln(ln(x)) at 1 from the left took twelve
                    // seconds over (1/(x - 1)) / ((1/x) / ln(x)), where x ln(x) / (x - 1) takes a
                    // hundredth of one. It took 14 ms before #2275 sent it to this rule.
                    // A part that settles at a finite number other than 0 is that number in the limit,
                    // and left in it is differentiated with the rest: the derivative of x + tanh(x),
                    // 1 + 1/cosh(x)^2 written through exponentials, tends to 2, and multiplied into
                    // the quotient ln(x + tanh(x)) / ln(x - pi) did not finish in a minute.
                    var ratio = (Settled(upperDerivative) * Settled(lowerLogArgument)
                        / (Settled(upperLogArgument) * Settled(lowerDerivative))).InnerSimplified;
                    return LimitFunctional.ComputeLimit(ratio, x, Real.PositiveInfinity);
                    Entity Settled(Entity part)
                        => part.ContainsNode(x)
                           && LimitFunctional.ComputeLimit(part, x, Real.PositiveInfinity) is { } limit
                           && limit.Evaled is Real { IsFinite: true, IsNaN: false, IsZero: false }
                            ? limit
                            : part;
                }
                else

                {
                    if (LimitFunctional.ArrivesOnTheCutOffTheRealLine(upperLogArgument, upperLogLimit, x, Real.PositiveInfinity)
                        || LimitFunctional.ArrivesOnTheCutOffTheRealLine(lowerLogArgument, lowerLogLimit, x, Real.PositiveInfinity))
                        return null;
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
