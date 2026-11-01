//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// Equations in moduli: where one is real for every complex <c>x</c>, how one in moduli of
    /// polynomials is solved over the reals, and how one in and out of moduli, conjugates, real
    /// and imaginary parts is solved over the complex numbers.
    /// </summary>
    /// <remarks>
    /// Over the complex numbers <c>|x - 2| = |x - 3|</c> holds on the whole line
    /// <c>Re x = 5/2</c>: both sides are real whatever <c>x</c> is, so the equation is one real
    /// condition on the two real unknowns <c>x</c> is made of, and what solves it is a curve. A
    /// numerical search finds points of it and would answer them as the set. Over the reals the
    /// same equation is <c>x = 5/2</c>, found by cases: between two neighbouring kinks every
    /// modulus has one sign, and the equation there has no moduli.
    /// https://github.com/asc-community/AngouriMath/issues/1573
    /// </remarks>
    internal static class ModulusSolver
    {
        /// <summary>
        /// Whether <paramref name="expr"/> is real for every complex <paramref name="x"/>, read off
        /// its structure: <paramref name="x"/> only ever inside a modulus, and around the moduli
        /// arithmetic with real numbers. A numerical probe could not tell this from a function
        /// that is real at the points it tried.
        /// </summary>
        internal static bool IsRealForEveryComplex(Entity expr, Variable x)
            => expr.ContainsNode(x) && RealWhereverItIs(expr, x);

        /// <summary>
        /// Whether <paramref name="x"/> is both inside a function that is not holomorphic -- a
        /// modulus, a signum, a conjugate, a real or imaginary part, an argument -- and outside every
        /// such function somewhere in <paramref name="expr"/>.
        /// </summary>
        /// <remarks>
        /// Newton's method assumes a holomorphic function, and over the complex numbers
        /// <c>|u| = +-u</c> only where <c>u</c> is real, which a root need not make it:
        /// <c>x^2 + 1 = |x|</c> has roots at <c>+-0.618 i</c>, where <c>|x|</c> is <c>0.618</c>. So
        /// neither the search nor a split by the sign finds every root, and the search's near-copies
        /// of a root came back as roots: <c>x^2/|1 - x^2| = x</c> had <c>1.618 + 1.7e-9 i</c> beside
        /// <c>1.618</c>. https://github.com/asc-community/AngouriMath/issues/2139
        /// </remarks>
        internal static bool IsInsideAndOutsideAFunctionThatIsNotHolomorphic(Entity expr, Variable x)
            => expr.Nodes.Any(node => IsNotHolomorphic(node) && node.ContainsNode(x)) && IsOutsideThem(expr, x);

        private static bool IsNotHolomorphic(Entity node)
            => node is Absf or Signumf or Conjugatef or Ref or Imf or Argf;

        private static bool IsOutsideThem(Entity expr, Variable x)
            => expr == x || !IsNotHolomorphic(expr) && expr.DirectChildren.Any(child => IsOutsideThem(child, x));

        private static bool RealWhereverItIs(Entity expr, Variable x)
            => expr switch
            {
                _ when !expr.ContainsNode(x) => expr.Evaled is Real || expr.Evaled is Complex { ImaginaryPart.IsZero: true },
                Absf => true,
                Sumf(var augend, var addend) => RealWhereverItIs(augend, x) && RealWhereverItIs(addend, x),
                Minusf(var minuend, var subtrahend) => RealWhereverItIs(minuend, x) && RealWhereverItIs(subtrahend, x),
                Mulf(var multiplier, var multiplicand) => RealWhereverItIs(multiplier, x) && RealWhereverItIs(multiplicand, x),
                Divf(var dividend, var divisor) => RealWhereverItIs(dividend, x) && RealWhereverItIs(divisor, x),
                Powf(var @base, Integer) => RealWhereverItIs(@base, x),
                _ => false,
            };

        /// <summary>
        /// The real <paramref name="x"/> at which <paramref name="expr"/> is zero, where every
        /// modulus in it with <paramref name="x"/> inside is of a polynomial with real
        /// coefficients whose real roots are found; <see langword="null"/> otherwise, or where a
        /// piece is not solved.
        /// </summary>
        /// <remarks>
        /// A polynomial keeps one sign between two neighbouring real roots, so its real roots are
        /// where a modulus of it can change from the argument to its negation, as a linear one's
        /// single root is. <c>x^2/|1 - x^2| = x</c> has kinks at -1 and 1, and on each piece an
        /// equation with no modulus, solved exactly; it was answered by a numerical search, in
        /// decimals and with two non-real numbers that solve nothing.
        /// https://github.com/asc-community/AngouriMath/issues/2139
        /// </remarks>
        internal static Set? SolveOverTheReals(Entity expr, Variable x)
        {
            var moduli = new List<Entity>();
            var kinks = new List<(Entity Exact, Real Value)>();
            foreach (var node in expr.Nodes)
            {
                if (node is not Absf(var argument) || !argument.ContainsNode(x))
                    continue;
                if (RealRootsOf(argument, x) is not { } roots)
                    return null;
                moduli.Add(argument);
                kinks.AddRange(roots);
            }
            if (moduli.Count == 0)
                return null;
            var ends = kinks.GroupBy(kink => kink.Value).Select(same => same.First())
                .OrderBy(kink => kink.Value.EDecimal).Select(kink => kink.Exact).ToList();

            // The pieces between neighbouring kinks, closed where they end at one, and on each the
            // sign every modulus takes, read at a point inside it.
            Set answer = Set.Empty;
            for (var i = 0; i <= ends.Count; i++)
            {
                Entity left = i == 0 ? Real.NegativeInfinity : ends[i - 1];
                Entity right = i == ends.Count ? Real.PositiveInfinity : ends[i];
                Entity inside = ends.Count == 0 ? Integer.Zero
                    : i == 0 ? ends[0] - 1 : i == ends.Count ? ends[^1] + 1 : (ends[i - 1] + ends[i]) / 2;
                var signs = new Dictionary<Entity, bool>();
                foreach (var argument in moduli)
                    if (!signs.ContainsKey(argument))
                    {
                        if (argument.Substitute(x, inside).Evaled is not Real value)
                            return null;
                        signs[argument] = !value.IsNegative;
                    }
                var withoutModuli = expr.Replace(node => node is Absf(var argument) && signs.TryGetValue(argument, out var nonNegative)
                    ? (nonNegative ? argument : -argument)
                    : node).InnerSimplified;
                Set piece = new Interval(left, i > 0, right, i < ends.Count);
                if (!withoutModuli.ContainsNode(x))
                {
                    if (withoutModuli.Evaled is Complex { IsZero: true })
                        answer = answer.Unite(piece);
                    continue;
                }
                var solved = AnalyticalEquationSolver.Solve(withoutModuli, x);
                // An equation that holds everywhere on the piece has the whole piece for solutions:
                // |x - 1| + |x - 2| = 1 is every x from 1 to 2, the solver answering such an
                // identity with every number.
                if (solved == MathS.Sets.C)
                {
                    answer = answer.Unite(piece);
                    continue;
                }
                if (solved is not FiniteSet roots)
                    return null;
                foreach (var root in roots)
                    if (root.Evaled is Real value && Within(value, left, right))
                        answer = answer.Unite(new FiniteSet(root));
            }
            return (Set)answer.InnerSimplified;
        }

        /// <summary>
        /// The real roots of <paramref name="argument"/>, exact and with their values, where it is a
        /// polynomial in <paramref name="x"/> with real numeric coefficients whose roots the solver
        /// finds; <see langword="null"/> otherwise.
        /// </summary>
        private static List<(Entity Exact, Real Value)>? RealRootsOf(Entity argument, Variable x)
        {
            if (TreeAnalyzer.TryGetPolyLinear(argument, x, out var slope, out var intercept))
                return !intercept.ContainsNode(x) && slope.Evaled is Real { IsZero: false }
                    && (-intercept / slope).InnerSimplified is Real kink
                    ? new() { (kink, kink) }
                    : null;
            if (!TreeAnalyzer.TryGetPolynomial(argument, x, out var monomials)
                || monomials.Count == 0
                || monomials.Keys.Any(power => power.Sign < 0)
                || monomials.Values.Any(coefficient => coefficient.ContainsNode(x) || coefficient.Evaled is not Real))
                return null;
            if (AnalyticalEquationSolver.Solve(argument, x) is not FiniteSet roots)
                return null;
            var real = new List<(Entity, Real)>();
            foreach (var root in roots)
                switch (root.Evaled)
                {
                    case Real value:
                        real.Add((root, value));
                        break;
                    case Complex { ImaginaryPart.IsZero: true } value:
                        real.Add((root, value.RealPart));
                        break;
                }
            return real;
        }

        private static bool Within(Real value, Entity left, Entity right)
            => left.Evaled is Real low && right.Evaled is Real high
                && low.EDecimal.CompareTo(value.EDecimal) <= 0 && value.EDecimal.CompareTo(high.EDecimal) <= 0;

        /// <summary>
        /// The complex <paramref name="x"/> at which <paramref name="expr"/> is zero, where it is
        /// made by arithmetic and whole powers from <paramref name="x"/>, rational numbers, and
        /// moduli, signums, conjugates, real and imaginary parts of such expressions;
        /// <see langword="null"/> otherwise, or where the roots are not finitely many or not
        /// found exactly.
        /// </summary>
        /// <remarks>
        /// <para>
        /// With <c>x = a + b i</c> for real <c>a</c> and <c>b</c>, and each modulus <c>|u|</c> a
        /// real <c>t</c>, not negative, with <c>t^2 = (Re u)^2 + (Im u)^2</c>, the real and the
        /// imaginary part of the equation and one equation for each modulus are polynomial
        /// equations in as many unknowns, solved exactly by a Groebner basis. The roots are the
        /// solutions with <c>a</c>, <c>b</c> and every <c>t</c> real and no <c>t</c> negative.
        /// <c>x |x| = 4</c> is <c>a t = 4</c>, <c>b t = 0</c> and <c>t^2 = a^2 + b^2</c>, so
        /// <c>t = 2</c> and <c>x = 2</c>; and <c>x^2 + 1 = |x|</c> has the roots <c>+-0.618 i</c>,
        /// which no split by the sign of <c>x</c> finds.
        /// https://github.com/asc-community/AngouriMath/issues/2139
        /// </para>
        /// <para>
        /// A signum <c>sgn(u)</c> is <c>u / |u|</c> where <c>u</c> is not zero and <c>0</c> where
        /// it is, so each is solved in both cases: <c>x + sgn(x) = 3</c> is <c>{ 2 }</c> where
        /// <c>x</c> is not zero, and has no root where it is. An argument is not taken, since it
        /// is not a polynomial in the parts.
        /// </para>
        /// </remarks>
        internal static Set? SolveOverTheComplexNumbers(Entity expr, Variable x)
        {
            if (expr.Vars.Any(variable => variable != x))
                return null;
            var signums = expr.Nodes.OfType<Signumf>().Select(signum => signum.Argument).Distinct().ToList();
            if (signums.Count > MostSignums)
                return null;
            var roots = new List<Entity>();
            for (var zero = 0; zero < 1 << signums.Count; zero++)
            {
                var zeroed = signums.Where((_, i) => (zero >> i & 1) == 1).ToList();
                if (RootsWhereZero(expr, x, zeroed) is not { } found)
                    return null;
                roots.AddRange(found);
            }
            return (Set)new FiniteSet(roots).InnerSimplified;
        }

        /// <summary>The most signums an equation solved in the parts of x has, each doubling the systems to solve.</summary>
        private const int MostSignums = 2;

        /// <summary>
        /// The roots of <paramref name="expr"/> at which every argument of a signum in
        /// <paramref name="zeroed"/> is zero and every other is not; <see langword="null"/> where
        /// they are not found.
        /// </summary>
        private static List<Entity>? RootsWhereZero(Entity expr, Variable x, List<Entity> zeroed)
        {
            var taken = new List<Variable> { x };
            var a = Variable.CreateTemp(taken);
            taken.Add(a);
            var b = Variable.CreateTemp(taken);
            taken.Add(b);
            var splitter = new Splitter(x, a, b, taken);
            var where = expr.Replace(node => node is Signumf(var argument) && zeroed.Contains(argument) ? Integer.Zero : node);
            if (splitter.Split(where) is not { } parts)
                return null;
            var equations = new List<Entity>(splitter.Equations) { parts.Real, parts.Imaginary };
            foreach (var argument in zeroed)
            {
                if (splitter.Split(argument) is not { } zero)
                    return null;
                equations.Add(zero.Real);
                equations.Add(zero.Imaginary);
            }
            var unknowns = new List<Variable>(splitter.Moduli.Values) { b, a };
            if (!IsSmallEnoughToSolve(equations, unknowns))
                return null;
            if (!Groebner.GroebnerSystemSolver.TrySolve(equations, unknowns, out var solutions))
                return null;
            var roots = new List<Entity>();
            if (solutions is null)
                return roots;
            var ofSignums = new HashSet<Variable>(splitter.Signums.Select(argument => splitter.Moduli[argument]));

            for (var row = 0; row < solutions.RowCount; row++)
            {
                var kept = true;
                for (var column = 0; column < unknowns.Count; column++)
                {
                    if (!TryReadReal(solutions[row, column], out var value))
                        return null;
                    // Each unknown is real, and a modulus is not negative, nor zero where it is the
                    // modulus a signum divides by.
                    if (value is null)
                        kept = false;
                    else if (column < unknowns.Count - 2 && (value.IsNegative || value.IsZero && ofSignums.Contains(unknowns[column])))
                    {
                        if (!value.IsZero && !IsClearOfZero(value.EDecimal, value.EDecimal))
                            return null;
                        kept = false;
                    }
                }
                if (!kept)
                    continue;
                var real = solutions[row, unknowns.Count - 1];
                var imaginary = solutions[row, unknowns.Count - 2];
                var root = (imaginary.Evaled is Integer { IsZero: true } ? real : real + imaginary * Complex.ImaginaryOne).Simplify();
                // The system has the equation with its denominators cleared, so a root of it can
                // be a pole of the equation as written, where it has no value.
                switch (expr.Substitute(x, root).Evaled)
                {
                    case Rational { IsZero: true }:
                    case Complex { IsFinite: true } residual when residual is not Rational && Number.IsZero(residual):
                        roots.Add(root);
                        break;
                    case Real { IsNaN: true }:
                    case Complex { IsFinite: false }:
                        break;
                    default:
                        return null;
                }
            }
            return roots;
        }

        /// <summary>The most solutions, counted by Bezout's bound, of a system solved for the parts of x.</summary>
        /// <remarks>
        /// The system for <c>x^2/|1 - x^2| = x</c> has degrees 2, 2 and 4, a bound of 16, and is
        /// solved in a tenth of a second; the one for <c>x^5 = |x| + 1</c> has 5, 5 and 2, a
        /// bound of 50, and spent 1.3 seconds before it was declined, and <c>x^7 = |x| + 1</c>
        /// more than twenty.
        /// </remarks>
        private const int MostSolutions = 32;

        /// <summary>
        /// Whether <paramref name="equations"/> are polynomials over <c>Q</c> in
        /// <paramref name="unknowns"/> with a product of degrees no more than
        /// <see cref="MostSolutions"/>.
        /// </summary>
        private static bool IsSmallEnoughToSolve(List<Entity> equations, List<Variable> unknowns)
        {
            var index = new Dictionary<Variable, int>();
            for (var i = 0; i < unknowns.Count; i++)
                index[unknowns[i]] = i;
            var bound = 1;
            foreach (var equation in equations)
            {
                if (Functions.MultivariatePolynomial.TryParse(equation, index) is not { } polynomial)
                    return false;
                if (polynomial.IsZero)
                    continue;
                bound *= polynomial.Degree;
                if (bound > MostSolutions)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Reads <paramref name="value"/> as a real number, <see langword="null"/> where it is not
        /// one; <see langword="false"/> where that is not decided, as where its imaginary part is
        /// too small to be told from rounding.
        /// </summary>
        private static bool TryReadReal(Entity value, out Real? real)
        {
            real = null;
            switch (value.Evaled)
            {
                case Real { IsFinite: true } number:
                    real = number;
                    return true;
                case Complex { IsFinite: true } complex when complex.ImaginaryPart.IsZero:
                    real = complex.RealPart;
                    return true;
                case Complex { IsFinite: true } complex:
                    return IsClearOfZero(complex.ImaginaryPart.EDecimal, complex.RealPart.EDecimal);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether <paramref name="part"/> is too large, beside <paramref name="scale"/>, to be
        /// what rounding leaves of a zero.
        /// </summary>
        private static bool IsClearOfZero(PeterO.Numbers.EDecimal part, PeterO.Numbers.EDecimal scale)
            => part.Abs().CompareTo(PeterO.Numbers.EDecimal.FromString("1E-30").Multiply(PeterO.Numbers.EDecimal.Max(PeterO.Numbers.EDecimal.One, scale.Abs()))) > 0;

        /// <summary>
        /// An expression as <c>(Real + Imaginary i) / Denominator</c>, each of the three real
        /// wherever the real unknowns are.
        /// </summary>
        private sealed record Parts(Entity Real, Entity Imaginary, Entity Denominator);

        /// <summary>
        /// Writes an expression in <c>x = a + b i</c> as its <see cref="Parts"/>, polynomials in
        /// <c>a</c>, <c>b</c> and one real unknown for each modulus, with the equation that
        /// defines each in <see cref="Equations"/>.
        /// </summary>
        private sealed class Splitter
        {
            /// <summary>The highest whole power taken apart, past which the parts are too large to solve.</summary>
            private const int HighestPower = 16;

            private readonly Variable x;
            private readonly Parts ofX;
            private readonly List<Variable> taken;

            internal Splitter(Variable x, Variable a, Variable b, List<Variable> taken)
            {
                this.x = x;
                ofX = new(a, b, Integer.One);
                this.taken = taken;
            }

            /// <summary>The unknown standing for each modulus, by its argument.</summary>
            internal Dictionary<Entity, Variable> Moduli { get; } = new();

            internal List<Entity> Equations { get; } = new();

            /// <summary>The arguments of the signums, each split as itself over its modulus.</summary>
            internal HashSet<Entity> Signums { get; } = new();

            internal Parts? Split(Entity expr)
                => expr switch
                {
                    _ when expr == x => ofX,
                    Variable => null,
                    Rational => new(expr, Integer.Zero, Integer.One),
                    Complex { RealPart: Rational real, ImaginaryPart: Rational imaginary } => new(real, imaginary, Integer.One),
                    Sumf(var augend, var addend) => Split(augend) is { } left && Split(addend) is { } right ? Add(left, right, Plus) : null,
                    Minusf(var minuend, var subtrahend) => Split(minuend) is { } left && Split(subtrahend) is { } right ? Add(left, right, Less) : null,
                    Mulf(var multiplier, var multiplicand) => Split(multiplier) is { } left && Split(multiplicand) is { } right ? Multiply(left, right) : null,
                    Divf(var dividend, var divisor) => Split(dividend) is { } left && Split(divisor) is { } right ? Divide(left, right) : null,
                    Powf(var @base, Integer exponent) when exponent.EInteger.Abs().CompareTo(HighestPower) <= 0
                        => Split(@base) is { } parts ? Power(parts, exponent.EInteger.ToInt32Checked()) : null,
                    Absf(var argument) => Split(argument) is { } parts ? Modulus(argument, parts) : null,
                    // Where its argument is not zero, which is the case it is split in.
                    Signumf(var argument) => Split(argument) is { } parts ? Divide(parts, Signum(argument, parts)) : null,
                    Conjugatef(var argument) => Split(argument) is { } parts ? new(parts.Real, Less(Integer.Zero, parts.Imaginary), parts.Denominator) : null,
                    Ref(var argument) => Split(argument) is { } parts ? new(parts.Real, Integer.Zero, parts.Denominator) : null,
                    Imf(var argument) => Split(argument) is { } parts ? new(parts.Imaginary, Integer.Zero, parts.Denominator) : null,
                    _ when !expr.ContainsNode(x) && expr.Evaled is Rational value => new(value, Integer.Zero, Integer.One),
                    _ => null,
                };

            private Parts Signum(Entity argument, Parts parts)
            {
                Signums.Add(argument);
                return Modulus(argument, parts);
            }

            private Parts Modulus(Entity argument, Parts parts)
            {
                if (!Moduli.TryGetValue(argument, out var modulus))
                {
                    modulus = Variable.CreateTemp(taken);
                    taken.Add(modulus);
                    Moduli[argument] = modulus;
                    Equations.Add(Less(
                        Times(Times(modulus, modulus), Times(parts.Denominator, parts.Denominator)),
                        Plus(Times(parts.Real, parts.Real), Times(parts.Imaginary, parts.Imaginary))));
                }
                return new(modulus, Integer.Zero, Integer.One);
            }

            private static Parts Add(Parts left, Parts right, System.Func<Entity, Entity, Entity> add)
                => left.Denominator == right.Denominator
                    ? new(add(left.Real, right.Real), add(left.Imaginary, right.Imaginary), left.Denominator)
                    : new(
                        add(Times(left.Real, right.Denominator), Times(right.Real, left.Denominator)),
                        add(Times(left.Imaginary, right.Denominator), Times(right.Imaginary, left.Denominator)),
                        Times(left.Denominator, right.Denominator));

            private static Parts Multiply(Parts left, Parts right)
                => new(
                    Less(Times(left.Real, right.Real), Times(left.Imaginary, right.Imaginary)),
                    Plus(Times(left.Real, right.Imaginary), Times(left.Imaginary, right.Real)),
                    Times(left.Denominator, right.Denominator));

            // (p + q i) / d divided by (r + s i) / e is (p + q i)(r - s i) e / (d (r^2 + s^2)).
            private static Parts Divide(Parts left, Parts right)
                => IsZero(right.Imaginary)
                    ? new(Times(left.Real, right.Denominator), Times(left.Imaginary, right.Denominator), Times(left.Denominator, right.Real))
                    : new(
                        Times(Plus(Times(left.Real, right.Real), Times(left.Imaginary, right.Imaginary)), right.Denominator),
                        Times(Less(Times(left.Imaginary, right.Real), Times(left.Real, right.Imaginary)), right.Denominator),
                        Times(left.Denominator, Plus(Times(right.Real, right.Real), Times(right.Imaginary, right.Imaginary))));

            private static Parts Power(Parts parts, int exponent)
            {
                Parts result = new(Integer.One, Integer.Zero, Integer.One);
                var square = parts;
                for (var remaining = System.Math.Abs(exponent); remaining > 0; remaining >>= 1)
                {
                    if ((remaining & 1) == 1)
                        result = Multiply(result, square);
                    if (remaining > 1)
                        square = Multiply(square, square);
                }
                return exponent < 0 ? Divide(new(Integer.One, Integer.Zero, Integer.One), result) : result;
            }

            private static bool IsZero(Entity part) => part is Integer { IsZero: true };

            private static Entity Times(Entity left, Entity right)
                => IsZero(left) || IsZero(right) ? Integer.Zero
                    : left == Integer.One ? right
                    : right == Integer.One ? left
                    : left * right;

            private static Entity Plus(Entity left, Entity right)
                => IsZero(left) ? right : IsZero(right) ? left : left + right;

            private static Entity Less(Entity left, Entity right)
                => IsZero(right) ? left : left - right;
        }
    }
}
