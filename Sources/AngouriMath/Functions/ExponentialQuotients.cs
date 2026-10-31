//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A quotient of one power plus two constants, <c>(b^v + c1) / (b^v + c2)</c>, as
    /// <c>1 + (c1 - c2) / (b^v + c2)</c>. The two are the same value wherever the quotient has one,
    /// and the second does not divide an overflow by an overflow.
    /// </summary>
    /// <remarks>
    /// It is how <c>tanh</c> and <c>coth</c> are spelled: <see cref="MathS.Hyperbolic.Tanh"/> is
    /// <c>(e^(2x) - 1) / (e^(2x) + 1)</c>. Past the range of the decimals, or of a double, both sides
    /// of it are infinite, so <c>tanh(5568)</c> evaluated to NaN and <c>tanh(400)</c> compiled to NaN,
    /// where both are 1. At a real infinity the second form is the limit, as <c>tanh(-oo)</c> already
    /// was <c>-1</c> and <c>arctan(+oo)</c> is <c>pi / 2</c>: <c>tanh(+oo)</c> is 1.
    /// https://github.com/asc-community/AngouriMath/issues/2254
    /// </remarks>
    internal static class ExponentialQuotients
    {
        /// <summary>
        /// <c>1 + (c1 - c2) / divisor</c> where <paramref name="dividend"/> and
        /// <paramref name="divisor"/> are one power plus a constant each, and <see langword="null"/>
        /// otherwise.
        /// </summary>
        internal static Entity? Rescaled(Entity dividend, Entity divisor)
            => Split(dividend) is var (power, c1) && Split(divisor) is var (same, c2) && power == same
                ? 1 + (c1 - c2) / divisor
                : null;

        /// <summary>
        /// Every such quotient in <paramref name="expression"/> rewritten, for the compilers: a double
        /// cannot tell an overflow from an infinity, so the rewrite is made wherever the shape is.
        /// </summary>
        internal static Entity Rewritten(Entity expression)
            => expression.Nodes.Any(node => node is Divf(Sumf or Minusf, Sumf or Minusf))
                ? expression.Replace(node => node is Divf(var dividend, var divisor) && Rescaled(dividend, divisor) is { } rescaled ? rescaled : node)
                : expression;

        /// <summary>The power and the constant of <c>b^v + c</c>, <c>c + b^v</c> or <c>b^v - c</c>, with <c>b</c> and <c>c</c> constants.</summary>
        private static (Powf Power, Entity Constant)? Split(Entity sum) => sum switch
        {
            Sumf(Powf power, var constant) when IsConstant(power.Base) && IsConstant(constant) => (power, constant),
            Sumf(var constant, Powf power) when IsConstant(power.Base) && IsConstant(constant) => (power, constant),
            Minusf(Powf power, var constant) when IsConstant(power.Base) && IsConstant(constant) => (power, -constant),
            _ => null,
        };

        private static bool IsConstant(Entity expression) => !expression.Vars.Any();
    }
}
