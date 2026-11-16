//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Reads the dimension off an expression tree, bottom up, given the dimension of each
    /// variable. The rules are the ones a physicist checks by eye: a sum and a comparison need
    /// their sides to agree, a product multiplies, a power needs a pure-number exponent and,
    /// on a dimensional base, a rational one, and a transcendental function needs a pure
    /// number to act on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here simplifies. The check is about the expression as written, so
    /// <c>x - x</c> with <c>x</c> a length is a length, and <c>sin(x) - sin(x)</c> with the
    /// same <c>x</c> is inconsistent although it simplifies to <c>0</c>: what was written
    /// is meaningless before any rewriting makes it vanish.
    /// </para>
    /// <para>
    /// Every arm of <see cref="Of"/> names a concrete node type, and none matches an abstract
    /// one such as <see cref="Function"/> or <see cref="Statement"/>. A node type added to the
    /// library later therefore reaches the last arm and is reported
    /// <see cref="DimensionStatus.Undetermined"/>, rather than being read as a pure number
    /// because it happens to derive from a class whose other members are.
    /// <c>DimensionsTest.EveryNodeTypeHasADecidedReading</c> lists the verdict on each node type
    /// and fails on one it does not list.
    /// </para>
    /// <para>
    /// A set is not a quantity, so <see cref="Of"/> does not give one a dimension. Where a set
    /// says what a variable ranges over — <c>x in [0; l]</c>, <c>max(f, x in S)</c>, a sum over
    /// a set — <see cref="Elements"/> reads the dimension its members share.
    /// </para>
    /// </remarks>
    internal static class DimensionAnalysis
    {
        internal static DimensionCheck Of(Entity expr, IReadOnlyDictionary<Variable, Dimension> dims)
            => expr switch
            {
                // A constant (pi, e, eulergamma) is a pure number. It is matched before
                // Variable, which it derives from.
                Constant => Pure,
                Variable v => dims.TryGetValue(v, out var d)
                    ? DimensionCheck.Ok(d)
                    : DimensionCheck.Unknown(v, $"no dimension is declared for {v}"),
                // Integer, Rational and Real all derive from Complex.
                Number.Complex => Pure,
                Entity.Boolean => Pure,

                Sumf(var a, var b) => Agree(expr, "added", dims, a, b),
                Minusf(var a, var b) => Agree(expr, "subtracted", dims, a, b),
                Mulf(var a, var b) => Combine(dims, a, b, (x, y) => x * y),
                Divf(var a, var b) => Combine(dims, a, b, (x, y) => x / y),
                Powf(var b, var p) => Power(expr, dims, b, p),
                Modf(var a, var b) => Agree(expr, "taken modulo each other", dims, a, b),
                Maxf(var a, var b) => Agree(expr, "compared by max", dims, a, b),
                Minf(var a, var b) => Agree(expr, "compared by min", dims, a, b),

                // These keep the dimension of their argument: |3 m| is 3 m, and so are its
                // conjugate and its real part. The imaginary part of 3 m + 4i m is 4 m.
                Absf(var a) => Of(a, dims),
                Conjugatef(var a) => Of(a, dims),
                Ref(var a) => Of(a, dims),
                Imf(var a) => Of(a, dims),
                // These read only a direction or a sign, which is a pure number whatever the
                // argument measures.
                Signumf(var a) => Then(Of(a, dims), _ => Pure),
                Argf(var a) => Then(Of(a, dims), _ => Pure),

                // A function defined by a series, an integral or a recurrence in its argument
                // adds powers of that argument, so the argument has to be a pure number and so
                // is the value: sin, ln, erf, the gamma family, zeta, W. Rounding and the
                // functions of number theory are defined on numbers, and floor(3.7 m) depends
                // on the unit, which is the same failure. A truth value is a pure number too,
                // so the connectives and the Iverson bracket are read here.
                Sinf or Cosf or Tanf or Cotanf or Secantf or Cosecantf
                    or Arcsinf or Arccosf or Arctanf or Arccotanf or Arcsecantf or Arccosecantf
                    or Logf or Eif or Sif or Cif or Shif or Chif or Lif
                    or Erff or Erfcf or Erfif or LambertWf or Polygammaf or Zetaf or Betaf or IncompleteGammaf
                    or Factorialf or DoubleFactorialf or Binomialf or FallingFactorialf or RisingFactorialf
                    or CatalanNumberf or HarmonicNumberf
                    or Floorf or Ceilf or Roundf
                    or Gcdf or Lcmf or Phif or Mobiusf or DivisorSigmaf or PrimeNuf or PrimeOmegaf or PrimePif
                    or Primef or Valuationf or PrimeFactorizationf or Alephf
                    or Iversonf or Andf or Orf or Notf or Xorf or Impliesf or Dividesf or Congruentf
                    => AllPure(expr, dims),

                // A count of members is a pure number, whatever the members measure.
                Cardf(var s) => Elements(s, dims) switch
                {
                    { Check.Status: DimensionStatus.Inconsistent } and var (check, _) => check,
                    { Any: true } => Pure,
                    var (check, _) => Then(check, _ => Pure),
                },

                // A vector or a matrix is read when its entries share a dimension, which is then
                // the dimension of the whole: a velocity is [vx, vy] with both a speed. A state
                // vector [x, v] mixes a length and a speed, which is meaningful and has no one
                // dimension, so it is undetermined rather than inconsistent.
                Matrix m => Entries(m, dims),
                DotProductf(var a, var b) => Combine(dims, a, b, (x, y) => x * y),
                CrossProductf(var a, var b) => Combine(dims, a, b, (x, y) => x * y),
                Normf(var a, var p) => Then(MustBePure(expr, p, dims), _ => Of(a, dims)),
                Tracef(var a) => Of(a, dims),
                Transposef(var a) => Of(a, dims),
                MatrixEntryf(var a, var i, var j) => Then(MustBePure(expr, i, dims), _ => Then(MustBePure(expr, j, dims), _ => Of(a, dims))),
                Determinantf(var a) => Determinant(expr, dims, a),

                Derivativef(var f, var v, var n) => Then(Of(f, dims), df => Then(Of(v, dims), dv =>
                    DimensionCheck.Ok(df / dv.Pow(ERational.FromInt32(n))))),
                PartialDerivativef(var f, var v, var n) => Then(Of(f, dims), df => Then(Of(v, dims), dv =>
                    DimensionCheck.Ok(df / dv.Pow(ERational.FromInt32(n))))),
                Integralf(var f, var v, var range) => Integral(dims, f, v, range),
                Limitf(var f, var v, var to, _) => Then(Of(v, dims), dv =>
                    Then(Matches(to, dv, dims, "a limit point"), _ => Of(f, dims))),
                Summationf(var f, var k, var from, var to) => Index(expr, dims, f, k, from, to, product: false),
                Productf(var f, var k, var from, var to) => Index(expr, dims, f, k, from, to, product: true),
                SumOverSetf(var f, var k, var s) => Bind(expr, dims, k, s, inner => Of(f, inner)),
                ProductOverSetf(var f, var k, var s) => Bind(expr, dims, k, s, inner => Then(Of(f, inner), df =>
                    df.IsDimensionless ? Pure
                    : s is FiniteSet finite ? DimensionCheck.Ok(df.Pow(ERational.FromInt32(finite.Count)))
                    : DimensionCheck.Unknown(expr, $"a product of terms of dimension {df} has that dimension to the number of terms, and the number of members of {s} is not known"))),
                Maximumf(var f, var k, var s) => Bind(expr, dims, k, s, inner => Of(f, inner)),
                Minimumf(var f, var k, var s) => Bind(expr, dims, k, s, inner => Of(f, inner)),

                Providedf(var f, var condition) => Then(MustBePure(expr, condition, dims), _ => Of(f, dims)),
                Piecewise pw => Cases(expr, dims, pw),

                Equalsf or Lessf or Greaterf or LessOrEqualf or GreaterOrEqualf
                    => Then(Agree(expr, "compared", dims, expr.DirectChildren.ToArray()), _ => Pure),
                Inf(var x, var s) => Then(Elements(s, dims) switch
                    {
                        { Check.Status: DimensionStatus.Inconsistent } and var (check, _) => check,
                        { Any: true } => Of(x, dims),
                        var (check, _) => Then(check, ds => Matches(x, ds, dims, $"an element of {s}")),
                    }, _ => Pure),
                Subsetf(var a, var b) => AgreeElements(expr, dims, a, b) switch
                {
                    { Check.Status: DimensionStatus.Inconsistent } and var (check, _) => check,
                    { Any: true } => Pure,
                    var (check, _) => Then(check, _ => Pure),
                },
                Forallf(var k, var s, var body) => Bind(expr, dims, k, s, inner => MustBePure(expr, body, inner)),
                Existsf(var k, var s, var body) => Bind(expr, dims, k, s, inner => MustBePure(expr, body, inner)),
                ExistsUniquef(var k, var s, var body) => Bind(expr, dims, k, s, inner => MustBePure(expr, body, inner)),

                // A set, argmax and argmin among them, is not a quantity. Where one says what a
                // variable ranges over, Elements reads it. Lambda and Application are functions
                // and their applications, whose dimensions nothing declares. Anything else is a
                // node this check has not been taught, and is not guessed at.
                _ => NotRead(expr),
            };

        [ConstantField] private static readonly DimensionCheck Pure = DimensionCheck.Ok(Dimension.Dimensionless);

        private static DimensionCheck NotRead(Entity expr)
            => DimensionCheck.Unknown(expr, $"{expr.GetType().Name} is not read by the dimension check");

        private static DimensionCheck Then(DimensionCheck first, Func<Dimension, DimensionCheck> next)
            => first.IsConsistent ? next(first.Dimension!) : first;

        // Zero and the infinities are the same quantity in every unit, so `v = 0` and
        // `x -> +oo` are meaningful whatever v and x measure. Only a literal is exempt: a
        // subexpression that happens to vanish is held to what it was written as.
        private static bool IsScaleFree(Entity e)
            => e is Real r && (r.EDecimal.IsZero || r.EDecimal.IsInfinity());

        private static DimensionCheck Agree(Entity at, string how, IReadOnlyDictionary<Variable, Dimension> dims, params Entity[] operands)
            => AgreeOrAny(at, how, dims, operands).Check;

        // Any is set where every operand was a scale-free literal: 0 + 0 is a pure number, and
        // the interval [0; +oo) is one whose members can measure anything.
        private static (DimensionCheck Check, bool Any) AgreeOrAny(Entity at, string how, IReadOnlyDictionary<Variable, Dimension> dims, IEnumerable<Entity> operands)
        {
            Dimension? common = null;
            Entity? first = null;
            foreach (var operand in operands)
            {
                var check = Of(operand, dims);
                if (!check.IsConsistent)
                    return (check, false);
                if (IsScaleFree(operand))
                    continue;
                if (common is null)
                    (common, first) = (check.Dimension, operand);
                else if (common != check.Dimension)
                    return (DimensionCheck.Bad(at, $"{first} has dimension {common} and {operand} has dimension {check.Dimension}, so they cannot be {how}"), false);
            }
            return (DimensionCheck.Ok(common ?? Dimension.Dimensionless), common is null);
        }

        private static DimensionCheck Matches(Entity e, Dimension expected, IReadOnlyDictionary<Variable, Dimension> dims, string what)
            => IsScaleFree(e) ? DimensionCheck.Ok(expected)
             : Then(Of(e, dims), d => d == expected ? DimensionCheck.Ok(d)
                 : DimensionCheck.Bad(e, $"{what} has dimension {d} where {expected} is needed"));

        private static DimensionCheck MustBePure(Entity at, Entity operand, IReadOnlyDictionary<Variable, Dimension> dims)
            => Then(Of(operand, dims), d => d.IsDimensionless ? Pure
                : DimensionCheck.Bad(at, $"{operand} has dimension {d}, and {Name(at)} only takes a pure number there"));

        private static string Name(Entity at) => at.GetType().Name.TrimEnd('f');

        private static DimensionCheck Combine(IReadOnlyDictionary<Variable, Dimension> dims, Entity a, Entity b, Func<Dimension, Dimension, Dimension> op)
            => Then(Of(a, dims), da => Then(Of(b, dims), db => DimensionCheck.Ok(op(da, db))));

        private static DimensionCheck Power(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity b, Entity p)
            => Then(Of(b, dims), db => Then(Of(p, dims), dp =>
            {
                if (!dp.IsDimensionless)
                    return DimensionCheck.Bad(p, $"the exponent {p} has dimension {dp}, and an exponent has to be a pure number");
                if (db.IsDimensionless)
                    return Pure;
                // L^q is a dimension only for a rational q: L^pi is not any combination of base units.
                var value = p is Number ? p : p.Vars.Any() ? null : p.Evaled;
                return value switch
                {
                    Rational q => DimensionCheck.Ok(db.Pow(q)),
                    Number => DimensionCheck.Bad(at, $"{b} has dimension {db} and is raised to {p}, which is not rational"),
                    _ => DimensionCheck.Unknown(at, $"{b} has dimension {db} and is raised to {p}, whose value is not known"),
                };
            }));

        private static DimensionCheck Integral(IReadOnlyDictionary<Variable, Dimension> dims, Entity f, Entity v, (Entity from, Entity to)? range)
            => Then(Of(f, dims), df => Then(Of(v, dims), dv =>
                range is var (from, to)
                    ? Then(Matches(from, dv, dims, "a lower bound"), _ => Then(Matches(to, dv, dims, "an upper bound"),
                        _ => DimensionCheck.Ok(df * dv)))
                    : DimensionCheck.Ok(df * dv)));

        // The index of a sum or product counts terms, so it is a pure number unless declared
        // otherwise. A product of n terms of dimension D is D^n, which is a dimension only when
        // n is a known number, and is left undetermined rather than guessed otherwise.
        private static DimensionCheck Index(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity f, Entity k, Entity from, Entity to, bool product)
        {
            var extended = k is Variable kv && !dims.ContainsKey(kv) ? With(dims, kv, Dimension.Dimensionless) : dims;
            return Then(Of(k, extended), dk => !dk.IsDimensionless
                ? DimensionCheck.Bad(k, $"the index {k} has dimension {dk}, and an index counts terms")
                : Then(Matches(from, dk, extended, "a bound of the index"), _ => Then(Matches(to, dk, extended, "a bound of the index"),
                    _ => Then(Of(f, extended), df =>
                        !product || df.IsDimensionless ? DimensionCheck.Ok(df)
                        : (from, to) is (Integer a, Integer b)
                            ? DimensionCheck.Ok(df.Pow(ERational.FromEInteger(EInteger.Max(b.EInteger - a.EInteger + 1, EInteger.Zero))))
                            : DimensionCheck.Unknown(at, $"a product of terms of dimension {df} has that dimension to the number of terms, and {to} - {from} + 1 is not a known number")))));
        }

        // A variable bound to range over a set takes the dimension the set's members share. One
        // declared by the caller has to agree with it; one not declared is given it, for the
        // body only.
        private static DimensionCheck Bind(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity k, Entity s, Func<IReadOnlyDictionary<Variable, Dimension>, DimensionCheck> body)
        {
            if (k is not Variable kv)
                return NotRead(at);
            var (members, any) = Elements(s, dims);
            if (members.Status is DimensionStatus.Inconsistent)
                return members;
            if (dims.TryGetValue(kv, out var declared))
                return any ? body(dims)
                    : !members.IsConsistent ? members
                    : declared == members.Dimension ? body(dims)
                    : DimensionCheck.Bad(at, $"{kv} has dimension {declared} and ranges over {s}, whose members have dimension {members.Dimension}");
            return members.IsConsistent ? body(With(dims, kv, members.Dimension!)) : members;
        }

        private static IReadOnlyDictionary<Variable, Dimension> With(IReadOnlyDictionary<Variable, Dimension> dims, Variable v, Dimension d)
            => new Dictionary<Variable, Dimension>(dims.ToDictionary(p => p.Key, p => p.Value)) { [v] = d };

        /// <summary>
        /// What the members of a set measure. <c>Check</c> is the dimension a variable ranging
        /// over the set takes when the caller declared none for it. <c>Any</c> is set where the
        /// set allows a declared variable of any dimension: <c>[0; +oo)</c>, whose ends are the
        /// same in every unit, and the number sets, since <c>l in RR</c> is how a length is said
        /// to be real. A variable nobody declared that ranges over a number set is a pure
        /// number, as the index of a sum is; one ranging over <c>[0; +oo)</c> is undetermined.
        /// </summary>
        internal static (DimensionCheck Check, bool Any) Elements(Entity s, IReadOnlyDictionary<Variable, Dimension> dims)
            => s switch
            {
                SpecialSet.Integers or SpecialSet.Rationals or SpecialSet.Reals or SpecialSet.Complexes
                    or SpecialSet.NonNegativeIntegers or SpecialSet.PositiveIntegers or SpecialSet.Primes or SpecialSet.Booleans
                    => (Pure, true),
                FiniteSet { Count: 0 } => (DimensionCheck.Unknown(s, "the empty set has no members to read a dimension from"), true),
                FiniteSet finite => ScaleFree(s, AgreeOrAny(s, "members of one set", dims, finite)),
                Interval(var left, _, var right, _) => ScaleFree(s, AgreeOrAny(s, "ends of one interval", dims, new[] { left, right })),
                Unionf(var a, var b) => AgreeElements(s, dims, a, b),
                Intersectionf(var a, var b) => AgreeElements(s, dims, a, b),
                SetMinusf(var a, var b) => AgreeElements(s, dims, a, b),
                // The prime factors of a number are numbers, and only of a number.
                PrimeFactorsf(var n) => (MustBePure(s, n, dims), false),
                // { x | P(x) } has members of x's dimension, once P is a consistent statement.
                ConditionalSet(var x, var predicate) => (Then(Of(x, dims), dx => Then(MustBePure(s, predicate, dims), _ => DimensionCheck.Ok(dx))), false),
                // The points where f is largest are values of the variable.
                Argmaxf(var f, var k, var over) => (Bind(s, dims, k, over, inner => Then(Of(f, inner), _ => Of(k, inner))), false),
                Argminf(var f, var k, var over) => (Bind(s, dims, k, over, inner => Then(Of(f, inner), _ => Of(k, inner))), false),
                IndexedUnionf(var k, var over, var body) => (Bind(s, dims, k, over, inner => Elements(body, inner).Check), false),
                IndexedIntersectionf(var k, var over, var body) => (Bind(s, dims, k, over, inner => Elements(body, inner).Check), false),
                // A variable standing for a set, a power set, whose members are sets, and
                // anything not listed: not read.
                _ => (NotRead(s), false),
            };

        private static (DimensionCheck Check, bool Any) ScaleFree(Entity s, (DimensionCheck Check, bool Any) read)
            => read.Any ? (DimensionCheck.Unknown(s, $"the members of {s} can measure anything"), true) : read;

        private static (DimensionCheck Check, bool Any) AgreeElements(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity a, Entity b)
        {
            var (ca, anyA) = Elements(a, dims);
            if (ca.Status is DimensionStatus.Inconsistent) return (ca, false);
            var (cb, anyB) = Elements(b, dims);
            if (cb.Status is DimensionStatus.Inconsistent) return (cb, false);
            if (anyA && anyB) return (ca.IsConsistent ? ca : cb, true);
            if (anyA) return (cb, false);
            if (anyB) return (ca, false);
            if (!ca.IsConsistent) return (ca, false);
            if (!cb.IsConsistent) return (cb, false);
            return ca.Dimension == cb.Dimension ? (ca, false)
                : (DimensionCheck.Bad(at, $"the members of {a} have dimension {ca.Dimension} and those of {b} have dimension {cb.Dimension}"), false);
        }

        private static DimensionCheck Entries(Matrix m, IReadOnlyDictionary<Variable, Dimension> dims)
        {
            Dimension? common = null;
            // The entries themselves: enumerating a matrix gives its rows, and a row of a
            // row vector is the vector again.
            foreach (var entry in from i in Enumerable.Range(0, m.RowCount) from j in Enumerable.Range(0, m.ColumnCount) select m[i, j])
            {
                var check = Of(entry, dims);
                if (!check.IsConsistent)
                    return check;
                if (IsScaleFree(entry))
                    continue;
                if (common is null)
                    common = check.Dimension;
                else if (common != check.Dimension)
                    return DimensionCheck.Unknown(m, $"the entries of {m} have different dimensions, {common} and {check.Dimension}, and a matrix is read only when its entries share one");
            }
            return DimensionCheck.Ok(common ?? Dimension.Dimensionless);
        }

        // The determinant of an n by n matrix of entries of dimension D is a sum of products of
        // n entries, so it has dimension D^n. That needs n, which only a written matrix shows.
        private static DimensionCheck Determinant(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity a)
            => Then(Of(a, dims), da =>
                da.IsDimensionless ? Pure
                : a is Matrix { RowCount: var rows, ColumnCount: var columns } && rows == columns
                    ? DimensionCheck.Ok(da.Pow(ERational.FromInt32(rows)))
                    : DimensionCheck.Unknown(at, $"the determinant of a matrix of entries of dimension {da} has that dimension to the order of the matrix, and the order of {a} is not known"));

        private static DimensionCheck Cases(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Piecewise pw)
        {
            foreach (var c in pw.Cases)
            {
                var condition = MustBePure(at, c.Predicate, dims);
                if (!condition.IsConsistent)
                    return condition;
            }
            return Agree(at, "cases of one piecewise", dims, pw.Cases.Select(c => c.Expression).ToArray());
        }

        private static DimensionCheck AllPure(Entity at, IReadOnlyDictionary<Variable, Dimension> dims)
        {
            foreach (var child in at.DirectChildren)
            {
                var check = MustBePure(at, child, dims);
                if (!check.IsConsistent)
                    return check;
            }
            return Pure;
        }
    }
}
