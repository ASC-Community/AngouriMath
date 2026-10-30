//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity;

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
    /// Nothing here simplifies. The check is about the expression as written, so
    /// <c>x - x</c> with <c>x</c> a length is a length, and <c>sin(x) - sin(x)</c> with the
    /// same <c>x</c> is inconsistent although it simplifies to <c>0</c>: what was written
    /// is meaningless before any rewriting makes it vanish.
    /// </remarks>
    internal static class DimensionAnalysis
    {
        internal static DimensionCheck Of(Entity expr, IReadOnlyDictionary<Variable, Dimension> dims)
            => expr switch
            {
                // A constant such as pi or e is a pure number. It is matched before Variable,
                // which it derives from.
                Constant => DimensionCheck.Ok(Dimension.Dimensionless),
                Variable v => dims.TryGetValue(v, out var d)
                    ? DimensionCheck.Ok(d)
                    : DimensionCheck.Unknown(v, $"no dimension is declared for {v}"),
                Number => DimensionCheck.Ok(Dimension.Dimensionless),
                Entity.Boolean => DimensionCheck.Ok(Dimension.Dimensionless),

                Sumf(var a, var b) => Agree(expr, "added", dims, a, b),
                Minusf(var a, var b) => Agree(expr, "subtracted", dims, a, b),
                Mulf(var a, var b) => Combine(dims, a, b, (x, y) => x * y),
                Divf(var a, var b) => Combine(dims, a, b, (x, y) => x / y),
                Powf(var b, var p) => Power(expr, dims, b, p),
                Modf(var a, var b) => Agree(expr, "taken modulo each other", dims, a, b),
                Maxf(var a, var b) => Agree(expr, "compared by max", dims, a, b),
                Minf(var a, var b) => Agree(expr, "compared by min", dims, a, b),

                // These keep the dimension of their argument: |3 m| is 3 m.
                Absf(var a) => Of(a, dims),
                Conjugatef(var a) => Of(a, dims),
                Ref(var a) => Of(a, dims),
                Imf(var a) => Of(a, dims),
                // These read only a direction or a sign, which is a pure number whatever the
                // argument measures.
                Signumf(var a) => Then(Of(a, dims), _ => DimensionCheck.Ok(Dimension.Dimensionless)),
                Argf(var a) => Then(Of(a, dims), _ => DimensionCheck.Ok(Dimension.Dimensionless)),

                Derivativef(var f, Variable v, var n) => Then(Of(f, dims), df => Then(Of(v, dims), dv =>
                    DimensionCheck.Ok(df / dv.Pow(ERational.FromInt32(n))))),
                Integralf(var f, Variable v, var range) => Integral(dims, f, v, range),
                Limitf(var f, Variable v, var to, _) => Then(Of(v, dims), dv =>
                    Then(Matches(to, dv, dims, "a limit point"), _ => Of(f, dims))),
                Summationf(var f, var k, var from, var to) => Index(expr, dims, f, k, from, to, product: false),
                Productf(var f, var k, var from, var to) => Index(expr, dims, f, k, from, to, product: true),
                Maximumf(var f, _, _) => Of(f, dims),
                Minimumf(var f, _, _) => Of(f, dims),

                Providedf(var f, var condition) => Then(Of(condition, dims), _ => Of(f, dims)),
                Piecewise pw => Cases(expr, dims, pw),

                ComparisonSign => Agree(expr, "compared", dims, expr.DirectChildren.ToArray()) switch
                {
                    { IsConsistent: true } => DimensionCheck.Ok(Dimension.Dimensionless),
                    var failed => failed,
                },
                // A connective of statements: each must be consistent, and the whole is a truth value.
                Statement when expr.DirectChildren.All(c => c is Statement) => AllDimensionless(expr, dims, mustBePure: false),

                // Any other function of numbers is transcendental or combinatorial: sin, log,
                // erf, gamma, floor, n!. None of them means anything on a quantity with a
                // unit, so each argument has to be a pure number, and so is the result.
                CalculusOperator => DimensionCheck.Unknown(expr, $"{expr.GetType().Name} is not read by the dimension check"),
                Function => AllDimensionless(expr, dims, mustBePure: true),

                _ => DimensionCheck.Unknown(expr, $"{expr.GetType().Name} is not read by the dimension check"),
            };

        private static DimensionCheck Then(DimensionCheck first, Func<Dimension, DimensionCheck> next)
            => first.IsConsistent ? next(first.Dimension!) : first;

        // Zero and the infinities are the same quantity in every unit, so `v = 0` and
        // `x -> +oo` are meaningful whatever v and x measure. Only a literal is exempt: a
        // subexpression that happens to vanish is held to what it was written as.
        private static bool IsScaleFree(Entity e)
            => e is Real r && (r.EDecimal.IsZero || r.EDecimal.IsInfinity());

        private static DimensionCheck Agree(Entity at, string how, IReadOnlyDictionary<Variable, Dimension> dims, params Entity[] operands)
        {
            Dimension? common = null;
            Entity? first = null;
            foreach (var operand in operands)
            {
                var check = Of(operand, dims);
                if (!check.IsConsistent)
                    return check;
                if (IsScaleFree(operand))
                    continue;
                if (common is null)
                    (common, first) = (check.Dimension, operand);
                else if (common != check.Dimension)
                    return DimensionCheck.Bad(at, $"{first} has dimension {common} and {operand} has dimension {check.Dimension}, so they cannot be {how}");
            }
            // Only scale-free literals: 0 + 0 is a pure number.
            return DimensionCheck.Ok(common ?? Dimension.Dimensionless);
        }

        private static DimensionCheck Matches(Entity e, Dimension expected, IReadOnlyDictionary<Variable, Dimension> dims, string what)
            => IsScaleFree(e) ? DimensionCheck.Ok(expected)
             : Then(Of(e, dims), d => d == expected ? DimensionCheck.Ok(d)
                 : DimensionCheck.Bad(e, $"{what} has dimension {d} where {expected} is needed"));

        private static DimensionCheck Combine(IReadOnlyDictionary<Variable, Dimension> dims, Entity a, Entity b, Func<Dimension, Dimension, Dimension> op)
            => Then(Of(a, dims), da => Then(Of(b, dims), db => DimensionCheck.Ok(op(da, db))));

        private static DimensionCheck Power(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity b, Entity p)
            => Then(Of(b, dims), db => Then(Of(p, dims), dp =>
            {
                if (!dp.IsDimensionless)
                    return DimensionCheck.Bad(p, $"the exponent {p} has dimension {dp}, and an exponent has to be a pure number");
                if (db.IsDimensionless)
                    return DimensionCheck.Ok(Dimension.Dimensionless);
                // L^q is a dimension only for a rational q: L^pi is not any combination of base units.
                var value = p is Number ? p : p.Vars.Any() ? null : p.Evaled;
                return value switch
                {
                    Rational q => DimensionCheck.Ok(db.Pow(q)),
                    Number => DimensionCheck.Bad(at, $"{b} has dimension {db} and is raised to {p}, which is not rational"),
                    _ => DimensionCheck.Unknown(at, $"{b} has dimension {db} and is raised to {p}, whose value is not known"),
                };
            }));

        private static DimensionCheck Integral(IReadOnlyDictionary<Variable, Dimension> dims, Entity f, Variable v, (Entity from, Entity to)? range)
            => Then(Of(f, dims), df => Then(Of(v, dims), dv =>
                range is var (from, to)
                    ? Then(Matches(from, dv, dims, "a lower bound"), _ => Then(Matches(to, dv, dims, "an upper bound"),
                        _ => DimensionCheck.Ok(df * dv)))
                    : DimensionCheck.Ok(df * dv)));

        // The index of a sum or product counts terms, so it is a pure number unless declared
        // otherwise; a product of n terms of dimension D is D^n, which is a dimension only
        // when n is a known number, and is left undetermined rather than guessed otherwise.
        private static DimensionCheck Index(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Entity f, Entity k, Entity from, Entity to, bool product)
        {
            var extended = dims;
            if (k is Variable kv && !dims.ContainsKey(kv))
                extended = new Dictionary<Variable, Dimension>(dims.ToDictionary(p => p.Key, p => p.Value)) { [kv] = Dimension.Dimensionless };
            return Then(Of(k, extended), dk => !dk.IsDimensionless
                ? DimensionCheck.Bad(k, $"the index {k} has dimension {dk}, and an index counts terms")
                : Then(Matches(from, dk, extended, "a bound of the index"), _ => Then(Matches(to, dk, extended, "a bound of the index"),
                    _ => Then(Of(f, extended), df => !product || df.IsDimensionless
                        ? DimensionCheck.Ok(df)
                        : DimensionCheck.Unknown(at, $"a product of terms of dimension {df} has that dimension to the number of terms")))));
        }

        private static DimensionCheck Cases(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, Piecewise pw)
        {
            foreach (var c in pw.Cases)
            {
                var condition = Of(c.Predicate, dims);
                if (!condition.IsConsistent)
                    return condition;
            }
            return Agree(at, "cases of one piecewise", dims, pw.Cases.Select(c => c.Expression).ToArray());
        }

        private static DimensionCheck AllDimensionless(Entity at, IReadOnlyDictionary<Variable, Dimension> dims, bool mustBePure)
        {
            foreach (var child in at.DirectChildren)
            {
                var check = Of(child, dims);
                if (!check.IsConsistent)
                    return check;
                if (mustBePure && !check.Dimension!.IsDimensionless)
                    return DimensionCheck.Bad(at, $"{child} has dimension {check.Dimension}, and {at.GetType().Name.TrimEnd('f')} only takes a pure number");
            }
            return DimensionCheck.Ok(Dimension.Dimensionless);
        }
    }
}
