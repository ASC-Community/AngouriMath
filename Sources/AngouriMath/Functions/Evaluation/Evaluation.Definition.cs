//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Transformations;
using HonkSharp.Laziness;
using PeterO.Numbers;

namespace AngouriMath
{
    internal static class EntityEvaluationExtension {
        /// <summary>Returns either <see cref="Entity.Evaled"/> or <see cref="Entity.InnerSimplified"/> based on <paramref name="isExact"/></summary>
        internal static Entity InnerSimplified(this Entity @this, bool isExact) => isExact ? @this.InnerSimplified : @this.Evaled;
    }

    partial record Entity
    {
        /// <summary>
        /// Returns the complete condition under which this expression is defined (has a valid value).
        /// This represents the mathematical "domain of definition" as a logical predicate.
        /// 
        /// For example:
        /// - For x/y: returns "y ≠ 0"
        /// - For sqrt(x) (over reals): returns "x ≥ 0"
        /// - For tan(x): returns "cos(x) ≠ 0" (or equivalently "x ≠ π/2 + πn")
        /// - For x + y: returns "true" (always defined)
        /// - For x/y + log(z): returns "y ≠ 0 and z > 0"
        /// 
        /// This combines the node's own definition condition (<see cref="IntrinsicCondition"/>) with all 
        /// its children's conditions using logical AND, propagating domain restrictions throughout the expression tree.
        /// 
        /// This is used by simplification patterns to preserve mathematical correctness by adding
        /// "provided" clauses when simplifications might hide singularities or undefined regions.
        /// For instance: (x-1)/(x-1) simplifies to "1 provided x ≠ 1", not just "1".
        /// </summary>
        /// <remarks>
        /// Mathematical concepts:
        /// - Domain of definition (the set where a function is defined)
        /// - Singularities and poles (points where a function is undefined)
        /// - Piecewise continuity (tracking where discontinuities occur)
        /// </remarks>
        public Entity DomainCondition => domainCondition.GetValue(static @this => ScopedToItsBinder(@this, @this.DirectChildren.Aggregate(@this.IntrinsicCondition, (accum, curr) =>
            (accum, curr.DomainCondition) switch {
                (Boolean(true), Boolean(true)) => Boolean.True,
                (var l, Boolean(true)) => l,
                (Boolean(true), var r) => r,
                (var l, var r) => l & r,
            })), this).InnerSimplified;
        private LazyPropertyA<Entity> domainCondition;

        /// <summary>
        /// <paramref name="condition"/> with each conjunct that mentions a name
        /// <paramref name="binder"/> binds read the way the binder reads it: required at every
        /// value the name ranges over. What a sum's body says about its index is a condition
        /// inside the sum, and outside the sum that name is free, or another expression's.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>0 * f</c> keeps the condition <c>f</c> is defined under, so the product rule carried
        /// <c>not x - w = 0</c> out of <c>sum(ln(x - w), w in { w : w^3 + w + 1 = 0 })</c> with
        /// <c>w</c> free in it, and the derivative could not be evaluated. Leaving the conjunct out
        /// would have made <c>0 * sum(1/(k - x), k, 1, n)</c> a plain 0 at <c>x = 1</c>, where the
        /// sum has no value.
        /// </para>
        /// <para>
        /// Over the roots of a polynomial <c>p</c> with rational coefficients, "<c>q(w) != 0</c> at
        /// every root" is <c>Res_w(p, q) != 0</c>, which mentions the other names alone -- here
        /// <c>not x^3 + x + 1 = 0</c>, where the logarithms are singular -- since <c>p</c>'s leading
        /// coefficient is a number that is not zero. A finite range of numbers is the conjunction
        /// over it. Anything else is kept as <c>forall</c> over the range: the set, the whole
        /// numbers from the lower bound to the upper, the interval between a definite integral's
        /// limits. A set builder admits the members its predicate is defined at, so its own
        /// condition on its name is part of whom it admits rather than of where the set is defined.
        /// A limit does not need its body defined throughout anything, and is left as it was.
        /// https://github.com/asc-community/AngouriMath/issues/1632
        /// </para>
        /// <para>
        /// For a definite integral this is sufficient for a value and not necessary: an integral
        /// whose integrand is undefined only at a point it still converges past, <c>ln(t)</c> over
        /// <c>[0; 1]</c>, is read as undefined here. That is the direction in which nothing is given
        /// a value it does not have, and a pointwise condition cannot tell a convergent improper
        /// integral from a divergent one. The integral itself, where it is worked out, is not
        /// affected: the derivative of <c>2 integral(x ln(t), t, 0, 1)</c> is -2.
        /// </para>
        /// </remarks>
        private static Entity ScopedToItsBinder(Entity binder, Entity condition)
        {
            if (condition is Boolean)
                return condition;
            (Entity Name, Entity? Range)? scope = binder switch
            {
                SumOverSetf(_, var name, var over) => (name, over),
                Maximumf(_, var name, var over) => (name, over),
                Minimumf(_, var name, var over) => (name, over),
                Argmaxf(_, var name, var over) => (name, over),
                Argminf(_, var name, var over) => (name, over),
                Quantifier quantifier => (quantifier.Var, quantifier.Over),
                Summationf(_, var index, var from, var to) => (index, WholeNumbersBetween(index, from, to)),
                Productf(_, var index, var from, var to) => (index, WholeNumbersBetween(index, from, to)),
                Integralf { Range: { } limits } integral => (integral.Var, Between(integral.Var, limits.from, limits.to)),
                Set.ConditionalSet(var name, _) => (name, null),
                _ => null
            };
            if (scope is not var (bound, range))
                return condition;
            var names = bound.VarsAndConsts;
            Entity kept = Boolean.True;
            foreach (var conjunct in Conjuncts(condition))
            {
                var scoped = !conjunct.FreeVariables.Any(names.Contains) ? conjunct
                    : range is null ? null
                    : OverTheRange(conjunct, bound, range);
                if (scoped is not null)
                    kept = kept is Boolean(true) ? scoped : kept & scoped;
            }
            return kept;
        }

        /// <summary>
        /// The segment between <paramref name="from"/> and <paramref name="to"/> in either order:
        /// an integral from 1 to 0 ranges over [0; 1], where <c>[1; 0]</c> is empty and would make
        /// any condition over it true. Two numbers are put in order; otherwise it is written as the
        /// set of values between the two, since <c>[a; b] \/ [b; a]</c> simplifies to <c>{ b }</c>.
        /// </summary>
        private static Entity Between(Entity name, Entity from, Entity to)
        {
            if (from is Number.Real low && to is Number.Real high)
                return low <= high ? MathS.Interval(low, high) : MathS.Interval(high, low);
            return new Set.ConditionalSet(name, (from <= name) & (name <= to) | (to <= name) & (name <= from));
        }

        /// <summary>The whole numbers from <paramref name="from"/> to <paramref name="to"/>, listed where there are a few of them.</summary>
        private static Entity WholeNumbersBetween(Entity index, Entity from, Entity to)
        {
            if (from is Number.Integer low && to is Number.Integer high
                && high.EInteger.Subtract(low.EInteger).CompareTo(16) < 0)
            {
                var members = new List<Entity>();
                for (var k = low.EInteger; k.CompareTo(high.EInteger) <= 0; k = k.Add(1))
                    members.Add(Number.Integer.Create(k));
                return new Set.FiniteSet(members);
            }
            return new Set.ConditionalSet(index, index.In(MathS.Sets.Z) & (from <= index) & (index <= to));
        }

        /// <summary><paramref name="conjunct"/> required at every value <paramref name="bound"/> takes in <paramref name="range"/>.</summary>
        private static Entity OverTheRange(Entity conjunct, Entity bound, Entity range)
        {
            if (bound is Variable name)
            {
                if (range is Set.ConditionalSet { Var: Variable root, Predicate: Equalsf(var left, var right) }
                    && AtEveryRoot(conjunct, name, (left - right).Substitute(root, name)) is { } exact)
                    return exact;
                if (range is Set.FiniteSet finite)
                {
                    Entity each = Boolean.True;
                    foreach (var member in finite.Elements)
                    {
                        var at = conjunct.Substitute(name, member);
                        each = each is Boolean(true) ? at : each & at;
                    }
                    return each;
                }
            }
            return new Forallf(bound, range, conjunct);
        }

        /// <summary>
        /// <c>not Res_w(p, q) = 0</c> for a conjunct <c>not g = h</c> whose <c>q = g - h</c> is a
        /// polynomial in <paramref name="name"/>: true exactly where <c>q</c> is not zero at any
        /// root of <paramref name="polynomial"/>, whose coefficients must be rational. Null for any
        /// other conjunct or polynomial.
        /// </summary>
        private static Entity? AtEveryRoot(Entity conjunct, Variable name, Entity polynomial)
        {
            if (conjunct is not Notf(Equalsf(var g, var h))
                || Functions.SumOverSet.SquareFreeParts(polynomial, name) is not { } parts
                || !Functions.TreeAnalyzer.TryGetPolynomial(g - h, name, out var terms))
                return null;
            // The roots once each: the product of the square-free parts.
            var p = Functions.IntegerPolynomial.Create(new[] { EInteger.One });
            foreach (var part in parts)
                if (p.Multiply(part.Factor) is { } product)
                    p = product;
                else
                    return null;
            var n = p.Degree;
            var m = 0;
            foreach (var power in terms.Keys)
            {
                if (power.Sign < 0 || !power.CanFitInInt32())
                    return null;
                m = System.Math.Max(m, power.ToInt32Unchecked());
            }
            if (m == 0 || n < 1)
                return null;
            Entity Q(int power) => terms.TryGetValue(EInteger.FromInt32(power), out var c) ? c : Number.Integer.Zero;
            Entity P(int power) => Number.Integer.Create(p[power]);
            Entity resultant;
            if (m == 1)
            {
                // a^n p(-b/a), written without dividing by a: sum of p_k (-b)^k a^(n - k). The
                // zeroth and first powers are written as themselves, since a power 0 of a base
                // that might be zero is 1 only where it is not, and that condition is not this one.
                var (a, b) = (Q(1), Q(0));
                static Entity Power(Entity @base, int exponent)
                    => exponent == 0 ? Number.Integer.One : exponent == 1 ? @base : MathS.Pow(@base, exponent);
                resultant = Number.Integer.Zero;
                for (var k = 0; k <= n; k++)
                    resultant += P(k) * Power(-b, k) * Power(a, n - k);
            }
            else
            {
                // Past a linear, only with rational coefficients, where the resultant is a number
                // and only whether it is zero matters: the Sylvester matrix in whole numbers, its
                // determinant by Bareiss's elimination. A symbolic q of higher degree is left to
                // forall. The determinant of a matrix of entities would reach GenericTensor, whose
                // operations native AOT cannot compile, from a property every node has.
                var denominators = EInteger.One;
                var rational = new ERational[m + 1];
                for (var power = 0; power <= m; power++)
                {
                    if (Q(power).Evaled is not Number.Rational coefficient)
                        return null;
                    rational[power] = coefficient.ERational;
                    denominators = denominators.Divide(denominators.Gcd(rational[power].Denominator)).Multiply(rational[power].Denominator);
                }
                var size = n + m;
                var sylvester = new EInteger[size, size];
                for (var row = 0; row < size; row++)
                    for (var column = 0; column < size; column++)
                    {
                        sylvester[row, column] = EInteger.Zero;
                        if (row < m && column - row >= 0 && column - row <= n)
                            sylvester[row, column] = p[n - (column - row)];
                        else if (row >= m && column - (row - m) >= 0 && column - (row - m) <= m)
                        {
                            var q = rational[m - (column - (row - m))];
                            sylvester[row, column] = q.Numerator.Multiply(denominators.Divide(q.Denominator));
                        }
                    }
                return WholeNumberDeterminant(sylvester).IsZero ? Boolean.False : Boolean.True;
            }
            return !resultant.Equalizes(Number.Integer.Zero);
        }

        /// <summary>The determinant of a square matrix of whole numbers, by Bareiss's fraction-free elimination.</summary>
        private static EInteger WholeNumberDeterminant(EInteger[,] matrix)
        {
            var size = matrix.GetLength(0);
            var a = (EInteger[,])matrix.Clone();
            var negated = false;
            var previous = EInteger.One;
            for (var k = 0; k < size - 1; k++)
            {
                if (a[k, k].IsZero)
                {
                    var swap = -1;
                    for (var i = k + 1; i < size && swap < 0; i++)
                        if (!a[i, k].IsZero)
                            swap = i;
                    if (swap < 0)
                        return EInteger.Zero;
                    for (var j = 0; j < size; j++)
                        (a[k, j], a[swap, j]) = (a[swap, j], a[k, j]);
                    negated = !negated;
                }
                for (var i = k + 1; i < size; i++)
                    for (var j = k + 1; j < size; j++)
                        a[i, j] = a[i, j].Multiply(a[k, k]).Subtract(a[i, k].Multiply(a[k, j])).Divide(previous);
                previous = a[k, k];
            }
            return negated ? a[size - 1, size - 1].Negate() : a[size - 1, size - 1];
        }

        private static IEnumerable<Entity> Conjuncts(Entity condition)
        {
            if (condition is Andf(var left, var right))
            {
                foreach (var conjunct in Conjuncts(left))
                    yield return conjunct;
                foreach (var conjunct in Conjuncts(right))
                    yield return conjunct;
            }
            else
                yield return condition;
        }
        
        /// <summary>
        /// Returns the intrinsic condition under which this specific operation is defined, 
        /// not including conditions from child expressions.
        /// 
        /// This represents the inherent domain restrictions of the operation itself.
        /// For example:
        /// - For division (x/y): returns "y ≠ 0" (the divisor must be non-zero)
        /// - For power (x^y): returns conditions for 0^0, 0^negative, etc.
        /// - For logarithm log(b, x): returns "b > 0 and b ≠ 1 and x > 0"
        /// - For addition (x + y): returns Boolean.True (no restrictions)
        /// - For tan(x): returns "cos(x) ≠ 0"
        /// 
        /// Child expression conditions are handled separately by the <see cref="DomainCondition"/> property.
        /// </summary>
        /// <remarks>
        /// This corresponds to the mathematical concept of a function's "natural domain" -
        /// the largest set of inputs for which the function's formula makes sense,
        /// independent of any restrictions on the input variables themselves.
        /// </remarks>
        private protected abstract Entity IntrinsicCondition { get; }

        /// <summary>
        /// The same as <see cref="DomainCondition"/>, but read in <paramref name="reading"/>
        /// rather than in whatever codomain each node happens to carry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The reading is a parameter of the question, not a property of the expression</b> —
        /// which is what <c>continuous_domain(f, x, S.Reals)</c> and
        /// <c>FunctionDomain[f, x, dom]</c> are in the two systems this was measured against, and
        /// what <a href="https://github.com/asc-community/AngouriMath/issues/721">#721</a> asks
        /// for. <c>arcsin</c> is defined on <c>|x| &lt;= 1</c> over the reals and everywhere over
        /// the complex plane; neither is *the* domain of <c>arcsin</c>, and an expression that
        /// answers only one of them cannot be asked the other.
        /// </para>
        /// <para>
        /// The per-node <see cref="Codomain"/> already selects between the two, and every node
        /// that has two answers already writes them both. What it could not do is answer for a
        /// <i>tree</i>: <c>WithCodomain</c> replaces the root's reading and leaves every child on
        /// its own, so <c>(arcsin(x) + arcsin(y)).WithCodomain(Real).DomainCondition</c> is
        /// <c>True</c> — the sum has no condition of its own and the two arcsines were never
        /// asked. Asking for the real reading and being given the complex one underneath is
        /// exactly the drift that issue is about.
        /// </para>
        /// <para>
        /// The reading is applied where a node's codomain is <b>wider</b> than it and nowhere
        /// else, so a variable declared over <c>ZZ</c> is not widened to <c>RR</c> by being asked
        /// a question about the reals.
        /// </para>
        /// </remarks>
        public Entity DomainConditionIn(Domain reading)
        {
            var read = Codomain > reading ? WithCodomain(reading) : this;
            var condition = read.IntrinsicCondition;
            foreach (var child in DirectChildren)
                condition = (condition, child.DomainConditionIn(reading)) switch
                {
                    (Boolean(true), Boolean(true)) => Boolean.True,
                    (var left, Boolean(true)) => left,
                    (Boolean(true), var right) => right,
                    (var left, var right) => left & right,
                };
            return condition.InnerSimplified;
        }

        /// <summary>
        /// This should NOT be called inside itself
        /// </summary>
        protected abstract Entity InnerSimplify(bool isExact);

        private Entity InnerSimplifyWithCheck(bool isExact)
        {
            var innerSimplified = InnerSimplify(isExact);
            if (innerSimplified.DirectChildren.Any(c => c == MathS.NaN))
                return MathS.NaN;
            if (DomainsFunctional.FitsDomainOrNonNumeric(innerSimplified, Codomain))
                return innerSimplified;
            else
                return MathS.NaN;
        }

        /// <summary>
        /// Represents the evaluated value of the given expression, allowing imprecise <see cref="Real"/> values unlike <see cref="InnerSimplified"/>.
        /// Unlike the result of <see cref="EvalNumerical"/> and
        /// <see cref="EvalBoolean"/>
        /// this is not constrained by any type.
        /// 
        /// It only performs an active operation in the first call,
        /// next time it is free to call it in terms of CPU usage. For
        /// consistency's sake, consider the call of this property
        /// as free as the addressing of a field.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = x + y;
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Evaled);
        /// Console.WriteLine(expr1.Evaled.GetType());
        /// Console.WriteLine("-----------------------------");
        /// var expr2 = 5 + x * i;
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Evaled);
        /// Console.WriteLine(expr2.Substitute(x, 3).Evaled);
        /// Console.WriteLine(expr2.Substitute(x, 3).Evaled.GetType());
        /// Console.WriteLine("-----------------------------");
        /// var expr3 = GreaterThan(5, 3);
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Evaled);
        /// Console.WriteLine(expr3.Evaled.GetType());
        /// </code>
        /// Prints
        /// <code>
        /// x + y
        /// x + y
        /// AngouriMath.Entity+Sumf
        /// -----------------------------
        /// 5 + x * i
        /// 5 + x * i
        /// 5 + 3i
        /// AngouriMath.Entity+Number+Complex
        /// -----------------------------
        /// 5 > 3
        /// True
        /// AngouriMath.Entity+Boolean
        /// </code>
        /// </example>
        public Entity Evaled
        {
            get
            {
                // Cached with the precision it was computed at, and read back only at that
                // precision: a hundred digits of pi are the wrong answer at five hundred. The
                // stamp is the precision setting's frame for this flow, compared by reference.
                // Frames are immutable, a scope opened and closed in order restores the very
                // frame that was under it, and a flow outside every scope has none. It was a
                // count of precision changes for the whole process, which could not tell one
                // flow's scope from another's, so a flow at a hundred digits read pi at three
                // hundred while another flow's scope was open.
                // https://github.com/asc-community/AngouriMath/issues/1505
                // The frame is read before the computation, so a scope opened and closed inside
                // it leaves the value stamped with the frame it was asked in.
                // https://github.com/asc-community/AngouriMath/issues/1367
                // And stamped with its owner: a record's `with` -- WithCodomain, and the
                // derivative's rewrites -- copies every field, this one included, and a value
                // computed for the original is not the copy's.
                // The value and its stamps are one object, published in one store, so that no
                // flow reads one flow's value under another's stamp.
                // Until a precision scope has been opened somewhere, no flow has a frame, and a
                // cached value is read without asking for one: the flow's frame is an async-local
                // read, several times the cost of the rest of this path.
                var precisionSetting = MathS.Settings.DecimalPrecisionContext;
                var precision = precisionSetting.EverScoped ? ((Convenience.ISettingState)precisionSetting).CurrentState : null;
                if (System.Threading.Volatile.Read(ref evaled) is { } cached
                    && ReferenceEquals(cached.Owner, this) && ReferenceEquals(cached.Precision, precision))
                    return cached.Value;
                var computed = InnerSimplifyWithCheck(false);
                System.Threading.Volatile.Write(ref evaled, new EvaledValue(computed, this, precision));
                return computed;
            }
        }

        /// <summary>A value of <see cref="Evaled"/>, with the node and the precision it was computed for.</summary>
        private sealed class EvaledValue
        {
            internal readonly Entity Value;
            internal readonly Entity Owner;
            internal readonly object? Precision;
            internal EvaledValue(Entity value, Entity owner, object? precision)
                => (Value, Owner, Precision) = (value, owner, precision);
        }
        private EvaledValue? evaled;

        /// <summary>
        /// This is the result of naive simplifications, but not creating imprecise <see cref="Real"/> values unlike <see cref="Evaled"/>. In other 
        /// symbolic algebra systems it is called "Automatic simplification".
        /// It only performs an active operation in the first call,
        /// next time it is free to call it in terms of CPU usage. For
        /// consistency's sake, consider the call of this property
        /// as free as the addressing of a field.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var x = Var("x");
        /// var expr = Sqr(Sin(x + 0)) + Sqr(Cos(x / 1));
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.InnerSimplified);
        /// Console.WriteLine(expr.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin(x + 0) ^ 2 + cos(x / 1) ^ 2
        /// sin(x) ^ 2 + cos(x) ^ 2
        /// 1
        /// </code>
        /// </example>
        public Entity InnerSimplified => innerSimplified.GetValue(static @this => @this.InnerSimplifyWithCheck(true), this);
        private LazyPropertyA<Entity> innerSimplified;

        /// <summary>
        /// Expands an equation trying to eliminate all the parentheses ( e. g. 2 * (x + 3) = 2 * x + 2 * 3 )
        /// </summary>
        /// <param name="level">
        /// The number of iterations (increase this argument in case if some parentheses remain)
        /// </param>
        /// <returns>
        /// An expanded Entity if it wasn't too complicated,
        /// current entity otherwise
        /// To change the limit use <see cref="MathS.Settings.MaxExpansionTermCount"/>
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// var expr = (x + 3) * (Sin(y) + 5);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Expand());
        /// Console.WriteLine("-----------------------------------");
        /// var expr2 = Pow(x + y, 8);
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Expand());
        /// </code>
        /// Prints
        /// <code>
        /// (x + 3) * (sin(y) + 5)
        /// x * sin(y) + x * 5 + 3 * sin(y) + 15
        /// -----------------------------------
        /// (x + y) ^ 8
        /// y ^ 8 + 8 * x * y ^ 7 + 28 * x ^ 2 * y ^ 6 + 56 * x ^ 3 * y ^ 5 + 70 * x ^ 4 * y ^ 4 + 56 * x ^ 5 * y ^ 3 + 28 * x ^ 6 * y ^ 2 + 8 * x ^ 7 * y + x ^ 8
        /// </code>
        /// </example> 

        public Entity Expand(int level = 2)
            => Transformation.ExpansionAtLevel(level).ApplyOrKeep(this);

        /// <summary>
        /// What <see cref="Expand(int)"/> does, reachable by
        /// <see cref="Transformation.ExpansionAtLevel(int)"/> without going back through
        /// the public method and round again.
        /// </summary>
        internal Entity ExpandOverSum(int level)
        {
            // A matrix is expanded entry by entry. What follows reads the expression as a sum, and
            // a matrix is not one, so it left through the escape at the bottom and came back as it
            // arrived: [[(x+1)^2, 1]] was not expanded while (x+1)^2 was. Factorize and
            // Differentiate both descend into a matrix, being built out of rewrite rules, and a
            // rule walks the tree -- so this was Expand being the odd one out rather than matrices
            // being held back on purpose.
            // https://github.com/asc-community/AngouriMath/issues/882
            if (this is Matrix matrix)
                return matrix.With((_, _, entry) => entry.Expand(level));

            static Entity Expand_(Entity e, int level) =>
                level <= 1
                ? e.Rewrite(RewriteRules.Expansion)
                : Expand_(e.Rewrite(RewriteRules.Expansion), level - 1);
            var expChildren = new List<Entity>();
            foreach (var linChild in Sumf.LinearChildren(this))
                if (TreeAnalyzer.SmartExpandOver(linChild, entity => true) is { } exp)
                    expChildren.AddRange(exp);
                else
                    return this; // if one is too complicated, return the current one
            return CollectLikeTerms(
                Expand_(TreeAnalyzer.MultiHangBinary(expChildren, (a, b) => new Sumf(a, b)), level).InnerSimplified);
        }

        /// <summary>
        /// Adds up the terms of an expanded sum that differ only by a numeric factor, so
        /// that expanding actually finishes: <c>(x+1)^2 * (x+1)^2</c> multiplied out gives
        /// sixteen terms, of which only five are distinct.
        /// </summary>
        /// <remarks>
        /// Deliberately not <see cref="Simplify(int)"/>, which is far too expensive to run
        /// inside <see cref="Expand(int)"/>. Each term is reduced to a coefficient and a
        /// product of powers, and terms whose products agree are added together --
        /// enough to collect like terms and nothing more.
        /// </remarks>
        /// <summary>
        /// Whether every node of the expression is one that monomial collection describes:
        /// numbers, variables, and the operations that build a polynomial out of them.
        /// </summary>
        /// <remarks>
        /// Opening a product to reach its numeric factor is what lets like terms meet, and
        /// it is also what stops a product cancelling as a whole -- lifting the 1/2 out of
        /// <c>(1/2 * sin(2t))^2 * csc(t)^2</c> costs the cancellation to <c>cos(t)^2</c>.
        /// Collection is a statement about polynomials, so it opens polynomials and leaves
        /// anything carrying a function intact for the rules that do know it.
        /// https://github.com/asc-community/AngouriMath/issues/855
        /// </remarks>
        private static bool IsPolynomialShaped(Entity expression)
            => expression.Nodes.All(node =>
                node is Number or Variable or Mulf or Powf or Sumf or Minusf or Divf);

        private static Entity CollectLikeTerms(Entity expanded)
        {
            if (expanded is not Sumf and not Minusf)
                return expanded;

            // A term carrying a domain condition must not be folded into another: the
            // conditions are what say where the answer holds, and adding coefficients
            // together loses them. (4a - 2)/(2x) + (1 - 2a)/x is 0 only where x is not 0,
            // and collecting it to a plain 0 would drop exactly that.
            if (expanded.Nodes.Any(node => node is Providedf))
                return expanded;

            var coefficients = new Dictionary<string, Entity>();
            var monomials = new Dictionary<string, Entity>();
            var order = new List<string>();

            foreach (var term in Sumf.LinearChildren(expanded))
            {
                Entity coefficient = 1;
                var exponents = new Dictionary<string, Entity>();
                var bases = new Dictionary<string, Entity>();

                var pending = new Stack<Entity>(Mulf.LinearChildren(term));
                while (pending.Count > 0)
                {
                    // PowerRules folds (x^2)^2 into x^4, without which the two would be
                    // counted as different monomials.
                    var reduced = pending.Pop().Rewrite(RewriteRules.Power).InnerSimplified;
                    if (reduced is Number)
                    {
                        coefficient = (coefficient * reduced).InnerSimplified;
                        continue;
                    }
                    // Reducing a factor can turn it into a product -- (2 * x)^2 becomes
                    // 4 * x^2 -- and taken whole that is a monomial of its own, keyed on
                    // "4 * x ^ 2" and unable to meet the plain x^2 terms it belongs with.
                    // Split it again so the 4 reaches the coefficient. Each child is
                    // smaller than the product it came from, so this terminates.
                    // https://github.com/asc-community/AngouriMath/issues/855
                    if (reduced is Mulf && IsPolynomialShaped(reduced))
                    {
                        foreach (var inner in Mulf.LinearChildren(reduced))
                            pending.Push(inner);
                        continue;
                    }
                    // The same one shape out: (2 * x * y)^2 is a power of a product that
                    // the rules above do not distribute. Only for an integer exponent,
                    // where (a * b)^n = a^n * b^n holds for every a and b; for any other
                    // exponent it is a statement about branches.
                    if (reduced is Powf(Mulf product, Integer wholePower) && IsPolynomialShaped(product))
                    {
                        foreach (var inner in Mulf.LinearChildren(product))
                            pending.Push(inner.Pow(wholePower));
                        continue;
                    }
                    var (@base, exponent) = reduced is Powf(var b, var e) ? (b, e) : (reduced, (Entity)1);
                    var key = @base.Stringize();
                    bases[key] = @base;
                    exponents[key] = exponents.TryGetValue(key, out var already)
                        ? (already + exponent).InnerSimplified
                        : exponent;
                }

                var monomialKey = string.Join(" ", (IEnumerable<string>)exponents.Keys.OrderBy(k => k, System.StringComparer.Ordinal)
                    .Select(k => k + "^" + exponents[k].Stringize()));

                if (coefficients.TryGetValue(monomialKey, out var running))
                    coefficients[monomialKey] = (running + coefficient).InnerSimplified;
                else
                {
                    order.Add(monomialKey);
                    coefficients[monomialKey] = coefficient;
                    Entity monomial = 1;
                    foreach (var key in exponents.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
                    {
                        // A factor that appeared once goes back as itself, not as base^1.
                        // Raising to the first power is an identity only where the power is
                        // defined at all: RR^1, ZZ^1 and true^1 are all NaN, so writing the
                        // factor that way turns an expression that had a value into one that
                        // does not. https://github.com/asc-community/AngouriMath/issues/851
                        var factor = exponents[key] == Integer.Create(1)
                            ? bases[key]
                            : bases[key].Pow(exponents[key]);
                        monomial = (monomial * factor).InnerSimplified;
                    }
                    monomials[monomialKey] = monomial;
                }
            }

            Entity result = 0;
            foreach (var key in order)
            {
                // A term whose coefficients cancelled is still written out rather than
                // dropped. Where the monomial is defined everywhere it simplifies to 0 and
                // costs nothing; where it is not -- x^-1, say -- InnerSimplified turns
                // 0 * x^-1 into `0 provided not x = 0`, and dropping the term would have
                // thrown that condition away. (4a - 2)/(2x) + (1 - 2a)/x is the case.
                var term = (coefficients[key] * monomials[key]).InnerSimplified;
                result = result == Integer.Create(0) ? term : result + term;
            }
            var collected = result.InnerSimplified;

            // Collecting is an improvement, never a requirement, so a collection that
            // introduced NaN is discarded rather than returned. The decomposition into
            // coefficient and monomial only describes a node that multiplies and takes
            // powers; a factor of any other kind -- a set raised above the first power,
            // say -- reassembles into something undefined. Keeping the expanded form is
            // always sound. https://github.com/asc-community/AngouriMath/issues/851
            if (collected.Nodes.Any(node => node == MathS.NaN)
                && !expanded.Nodes.Any(node => node == MathS.NaN))
                return expanded;

            return collected;
        }

        /// <summary>
        /// Factorizes an equation trying to eliminate as many power-uses as possible ( e.g. x * 3 + x * y = x * (3 + y) )
        /// </summary>
        /// <param name="level">
        /// The number of iterations (increase this argument if some factor operations are still available)
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// var expr1 = x * y + y + x + 1;
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Factorize());
        /// Console.WriteLine("-----------------------------------");
        /// var expr2 = x * y + y + (1 + x);
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Factorize());
        /// </code>
        /// Prints
        /// <code>
        /// x * y + y + x + 1
        /// y * (1 + x) + x + 1
        /// -----------------------------------
        /// x * y + y + 1 + x
        /// (1 + x) * (1 + y)
        /// </code>
        /// </example>
        /// <remarks>
        /// One pass is the perfect-square rules, then the factorisation rules, then
        /// <see cref="InnerSimplified"/> -- so that the factors come back finished, since
        /// the rules leave <c>x ^ 1</c> where they mean <c>x</c> and <c>sqrt(4)</c> where
        /// they mean <c>2</c> -- and <paramref name="level"/> is how many times that runs.
        /// Built out of <see cref="RewriteRules"/> by
        /// <see cref="Transformation.FactorizationAtLevel(int)"/>.
        /// </remarks>
        public Entity Factorize(int level = 2)
            => Transformation.FactorizationAtLevel(level).ApplyOrKeep(this);

        /// <summary>
        /// This expression written as a single fraction: one numerator over one denominator, with
        /// nothing divided inside either, as <c>a + b/c</c> is written <c>(a c + b)/c</c>. Any
        /// expression, functions included, and nothing cancelled or multiplied out, so the factors
        /// of the denominator stay as they were: <c>1/(t^2 + 1) + 1/(t + 1)</c> is
        /// <c>(2 + t + t^2) / ((t^2 + 1)(t + 1))</c>, and <c>x/x</c> stays <c>x/x</c>. The common
        /// denominator is the least common multiple of the denominators as they are written, so
        /// <c>1/x + 1/x^2</c> is <c>(x + 1)/x^2</c>; nothing is factorised to find it. A rational number
        /// counts as the fraction it is written as, so <c>2/3 + x/2</c> is <c>(4 + 3x)/6</c>. An
        /// expression with no division in it comes back as it was. Dividing by a fraction moves
        /// its denominator into the numerator, and the expression has no value where that
        /// denominator is zero, so the answer says so: <c>1/(1/x)</c> is <c>x provided not x = 0</c>.
        /// </summary>
        /// <remarks>
        /// <see cref="Simplify(int)"/> never does this, since one fraction is not always the
        /// simpler form: <c>1/x + 1/y</c> reads more easily than <c>(x + y)/(x y)</c>. For a
        /// rational function in lowest terms, the denominator multiplied out and monic, use
        /// <see cref="Transformation.RationalCanonicalization"/>, which is a canonical form and
        /// declines anything with a function in it.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1239">#1239</a>
        /// </remarks>
        /// <example>
        /// <code>
        /// Console.WriteLine("sin(x) + 1/cos(x)".ToEntity().AsSingleFraction());
        /// </code>
        /// Prints
        /// <code>
        /// (1 + cos(x) * sin(x)) / cos(x)
        /// </code>
        /// </example>
        public Entity AsSingleFraction()
            => Transformation.AsSingleFraction.ApplyOrKeep(this);

        /// <summary>
        /// Simplifies an equation ( e.g. (x - y) * (x + y) -> x^2 - y^2, but 3 * x + y * x = (3 + y) * x )
        /// </summary>
        /// <param name="level">
        /// Increase this argument if you think the equation should be simplified better
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, a) = Var("x", "y", "a");
        /// var expr = Sin(x) + y + a;
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// Console.WriteLine("---------------------");
        /// var expr1 = Sin(x - 3) / Tan(x - 3) + Sec(Sqrt(y)) * Cosec(Sqrt(y));
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Simplify());
        /// Console.WriteLine("---------------------");
        /// var expr2 = Sin(pi / 3) * 2;
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// Console.WriteLine("---------------------");
        /// var expr3 = (Pow(x, 3) + 3 * Sqr(x) * y + 3 * x * Sqr(y) + Pow(y, 3)) / (x + y);
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// Console.WriteLine("---------------------");
        /// var expr4 = Derivative(Sin(Sqr(x * y) + y * x), x);
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin(x) + y + a
        /// sin(x) + a + y
        /// ---------------------
        /// sin(x - 3) / tan(x - 3) + sec(sqrt(y)) * csc(sqrt(y))
        /// 2 * csc(2 * sqrt(y)) + cos(x - 3)
        /// ---------------------
        /// sin(pi / 3) * 2
        /// sqrt(3)
        /// ---------------------
        /// (x ^ 3 + 3 * x ^ 2 * y + 3 * x * y ^ 2 + y ^ 3) / (x + y)
        /// x ^ 2 + 2 * x * y + y ^ 2
        /// ---------------------
        /// derivative(sin((x * y) ^ 2 + y * x), x)
        /// cos((x * y) ^ 2 + x * y) * (2 * x * y ^ 2 + y)
        /// </code>
        /// </example>
        public Entity Simplify(int level = 2)
            => Transformation.SimplificationAtLevel(level).ApplyOrKeep(this);

        /// <summary>
        /// A canonical form for the commutative structure: two expressions differing only in
        /// how their sums, products, conjunctions, disjunctions and set operations are
        /// arranged or nested come out as the <b>identical tree</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is not <see cref="Simplify(int)"/> and is not trying to be. It makes an
        /// expression <i>comparable</i>, not shorter, and it may well make it longer. What it
        /// buys is that <c>a.Canonicalize() == b.Canonicalize()</c> is a real test of whether
        /// the two are the same expression, where comparing simplified forms is not.
        /// </para>
        /// <para>
        /// <b>Equal trees mean the expressions are equal; different trees mean nothing at
        /// all.</b> There is no canonical form for the whole language — deciding whether an
        /// expression is zero is undecidable once <c>pi</c>, the exponential, the trigonometric
        /// functions and <c>abs</c> are in play — so this canonicalises the part that can be:
        /// the arrangement of commutative operators. For rational functions over <c>Q</c>, where
        /// a complete canonical form does exist, use
        /// <see cref="CanonicalizeAsRationalFunction"/>.
        /// <c>Docs/Contributing/CanonicalForm.md</c> states the boundary and how far off the
        /// library is from it.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using static System.Console;
        ///
        /// WriteLine("x + y".ToEntity().Canonicalize() == "y + x".ToEntity().Canonicalize());
        /// WriteLine("(x + y) + a".ToEntity().Canonicalize() == "x + (y + a)".ToEntity().Canonicalize());
        /// </code>
        /// Prints
        /// <code>
        /// True
        /// True
        /// </code>
        /// </example>
        public Entity Canonicalize()
            => Transformation.Canonicalization.ApplyOrKeep(this);

        /// <summary>
        /// A canonical form for rational functions over <c>Q</c>, or <see langword="null"/>
        /// where this is not one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two rational functions are equal <b>exactly when</b> this form is identical, so on
        /// that sublanguage equality is decided by comparing nodes rather than by searching —
        /// which is what <c>(a - b).Simplify()</c> against zero can never be.
        /// </para>
        /// <para>
        /// <b>It answers only where it can, and says so by answering nothing.</b> Anything that
        /// is not a rational function over <c>Q</c> in its free variables gets
        /// <see langword="null"/>, because a form whose whole value is that equal trees mean
        /// equal expressions must not hand back a normalisation that merely resembles one.
        /// </para>
        /// <para>
        /// Cancelling a common factor widens the domain, so where one comes out the answer
        /// carries the condition that it is nonzero: <c>x/x</c> is <c>1 provided not x = 0</c>
        /// and not <c>1</c>, and <c>(x^2 - 1)/(x + 1)</c> is not the same function as
        /// <c>x - 1</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using static System.Console;
        ///
        /// WriteLine("1/x + 1/y".ToEntity().CanonicalizeAsRationalFunction());
        /// WriteLine("(x + y) / (x * y)".ToEntity().CanonicalizeAsRationalFunction());
        /// WriteLine("sin(x) / x".ToEntity().CanonicalizeAsRationalFunction() is null);
        /// </code>
        /// Prints
        /// <code>
        /// (x + y) / (x * y)
        /// (x + y) / (x * y)
        /// True
        /// </code>
        /// </example>
        public Entity? CanonicalizeAsRationalFunction()
            => Transformation.RationalCanonicalization.Apply(this).Output;

        /// <summary>Finds all alternative forms of an expression sorted by their complexity</summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, a) = Var("x", "y", "a");
        /// var expr = x * y + y + x + x / y + a * (x + y);
        /// foreach (var alt in expr.Alternate(level: 3))
        ///     Console.WriteLine(alt);
        /// </code>
        /// <code>
        /// a * y + (1 + a + 1 / y + y) * x + y
        /// a * (x + y) + x + x * (y + 1 / y) + y
        /// a * (x + y) + x + x * (1 / y + y) + y
        /// x * y + y + x + x / y + a * (x + y)
        /// a * (x + y) + x + x * y + x / y + y
        /// (x + y) * a + x + x * y + x / y + y
        /// a * (x + y) + x + x * 1 / y + x * y + y
        /// </code>
        /// </example>
        public IEnumerable<Entity> Alternate(int level) => Simplificator.Alternate(this, level);

        /// <summary>
        /// Determines whether a given element can be unambiguously used as a number or boolean
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = x + y;
        /// Console.WriteLine(expr1.IsConstant);
        /// Console.WriteLine(expr1.Evaled.IsConstant);
        /// Console.WriteLine("-----------------------------");
        /// var expr2 = 5 + x * i;
        /// Console.WriteLine(expr2.IsConstant);
        /// Console.WriteLine(expr2.Substitute(x, 3).IsConstant);
        /// Console.WriteLine("-----------------------------");
        /// var expr3 = GreaterThan(5, 3);
        /// Console.WriteLine(expr3.IsConstant);
        /// Console.WriteLine("-----------------------------");
        /// var expr4 = pi + 0 * e;
        /// Console.WriteLine(expr4.IsConstant);
        /// </code>
        /// Prints
        /// <code>
        /// False
        /// False
        /// -----------------------------
        /// False
        /// True
        /// -----------------------------
        /// True
        /// -----------------------------
        /// True
        /// </code>
        /// </example>
        public bool IsConstant => Evaled is Number.Complex or Boolean or Constant;
    }
}
