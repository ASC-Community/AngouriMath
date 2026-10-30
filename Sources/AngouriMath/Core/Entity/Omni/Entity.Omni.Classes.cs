//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core;
using System.Collections;
using AngouriMath.Core.HashCode;
using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Sets;
using AngouriMath.Functions.Boolean;
using HonkSharp.Laziness;
using Complex = AngouriMath.Entity.Number.Complex;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace AngouriMath
{
    partial record Entity
    {        
        public partial record Set : Entity
        {
            #region FiniteSet
            /// <summary>
            /// A finite set is a set whose elements can be counted and enumerated
            /// </summary>
            public sealed partial record FiniteSet : Set, IReadOnlyCollection<Entity>, IEquatable<FiniteSet?>
            {
                /// <summary>
                /// The IEnumerable of elements of a finite set
                /// Which is readonly
                /// </summary>
                public IEnumerable<Entity> Elements => elements.Values;

                private readonly Dictionary<Entity, Entity> elements;

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(Apply(el => el.Replace(func)));

                /// <summary>
                /// Applies a function to every element of a set
                /// and returns a set
                /// </summary>
                /// <param name="func">What we do with each element?</param>
                public FiniteSet Apply(Func<Entity, Entity> func)
                {
                    var hasAnythingChanged = false;
                    FiniteSetBuilder newElements = new();
                    foreach (var el in Elements)
                    {
                        var changed = func(el);
                        if (!ReferenceEquals(changed, el))
                            hasAnythingChanged = true;
                        if (changed != MathS.NaN) newElements.Add(changed);
                    }
                    if (hasAnythingChanged)
                        return newElements.ToFiniteSet();
                    else
                        return this;
                }

                internal override Priority Priority => Priority.Leaf;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => Elements.ToArray();

                private static Dictionary<Entity, Entity> BuildDictionaryFromElements(IEnumerable<Entity> elements, bool noCheck)
                {
                    var enumerable = elements as Entity[] ?? elements.ToArray();
                    Dictionary<Entity, Entity> dict = new(enumerable.Length);
                    foreach (var elem in enumerable)
                    {
                        if (elem == MathS.NaN)
                            continue;
                        if (noCheck)
                            dict[elem.Evaled] = elem;
                        else
                            dict[elem.Evaled] = dict.TryGetValue(elem.Evaled, out var value) ? Simplificator.PickSimplest(elem, value) : elem;
                    }
                    return dict;
                }

                /// <summary>
                /// Constructor of a finite set
                /// Use <see cref="Empty"/> to create an empty set
                /// </summary>
                public FiniteSet(IEnumerable<Entity> elements) : this(elements, noCheck: false) { }

                /// <summary>
                /// Constructor of a finite set
                /// Use <see cref="Empty"/> to create an empty set
                /// </summary>
                public FiniteSet(params Entity[] elements) : this((IEnumerable<Entity>)elements) { }

                private FiniteSet(IEnumerable<Entity> elements, bool noCheck)
                {
                    this.elements = BuildDictionaryFromElements(elements, noCheck);
                    Count = this.elements.Count;
                }

                /// <summary>
                /// Deconstructs as record
                /// </summary>
                public void Deconstruct(out IEnumerable<Entity> elements)
                    => elements = Elements;


                /// <summary> Represents number of entities in the current set </summary>
                public int Count { get; }

                /// <summary>
                /// Used for enumerating. Use "foreach" for iterating over elements
                /// </summary>
                public IEnumerator<Entity> GetEnumerator()
                    => Elements.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => Elements.GetEnumerator();

                internal static FiniteSet Unite(FiniteSet A, FiniteSet B)
                    => new FiniteSet(A.Elements.Concat(B.Elements));

                // It could be written with one chain request, but readability > one line
                internal static bool TryFullSubtract(FiniteSet A, FiniteSet B, [NotNullWhen(true)] out FiniteSet? resultSet)
                {
                    var constantSets = A.Vars.Count == 0 && B.Vars.Count == 0;
                    var dict = BuildDictionaryFromElements(A.Elements, noCheck: true);
                    foreach (var el in B)
                        if (!dict.Remove(el.Evaled) && !constantSets) { resultSet = null; return false; }
                    resultSet = new FiniteSet(dict.Values, noCheck: true); // we didn't add anything
                    return true;
                }

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    if (IsSetEmpty) { contains = false; return true; } // a in {} is unambiguously false
                    contains = elements.ContainsKey(entity.Evaled);
                    // x in { x, 2 } and 2 in { x, 2 } are both unambiguously true
                    if (contains) return true;
                    // however, 1 in { x, 2 }, y in { x, 2 } are both unknown
                    // meanwhile, 2 in { 1, 3 } is unambiguously false
                    return entity.Vars.Count == 0 && elements.Keys.All(el => el.Vars.Count == 0);
                }

                /// <inheritdoc/>
                public override int GetHashCode()
                    => hashCode.GetValue(static @this =>
                        @this.Elements.OrderBy(el => el.GetHashCode()).HashCodeOfSequence(HashCodeFunctional.HashCodeShifts.FiniteSet), 
                        this);
                private LazyPropertyA<int> hashCode;

                /// <summary>
                /// Checks that two FiniteSets are equal
                /// If one is not FiniteSet, the method returns false
                /// </summary>
                public bool Equals(FiniteSet? other)
                {
                    if (other is null)
                        return false;
                    if (other.Count != Count)
                        return false;
                    foreach (var pair in elements)
                        if (!other.TryContains(pair.Key, out var contains) || !contains)
                            return false;
                    return true;
                }

                /// <summary>
                /// Finds such a set that only contains all subsets of the given set
                /// </summary>
                public FiniteSet GetPowerSet() => powerSet ??= PowerSetWorkout();
                
                private FiniteSet? powerSet;

                private FiniteSet PowerSetWorkout()
                {
                    // The empty set has one subset, itself; the stepper below indexes the last
                    // element and has none to index. https://github.com/asc-community/AngouriMath/issues/1409
                    if (Count == 0)
                        return new FiniteSet(new Entity[] { Empty }, noCheck: true);
                    var sets = new List<Entity>();
                    var state = new bool[Count];
                    var elements = Elements.ToArray();
                    do
                    {
                        var innerSetElements = new List<Entity>();
                        for (int i = 0; i < Count; i++)
                            if (state[i])
                                innerSetElements.Add(elements[i]);
                        sets.Add(new FiniteSet(innerSetElements, noCheck: true));
                    }
                    while (BooleanSolver.Next(state));
                    return new FiniteSet(sets, noCheck: true);
                }

                /// <summary>
                /// Safely checks whether this is a subset of the argument,
                /// if successful, returns true AND stores the result in <paramref name="isSub"/>
                /// </summary>
                /// <returns>Whether it is possible to determine</returns>
                public bool TryIsSubsetOf(FiniteSet superSet, out bool isSub)
                {
                    isSub = false;
                    foreach (var el in this)
                        if (!superSet.TryContains(el, out var contains))
                            return false;
                        else if (!contains)
                            return true;

                    isSub = true;
                    return true;
                }

                /// <inheritdoc/>
                public override bool IsSetFinite => true;

                /// <inheritdoc/>
                public override bool IsSetEmpty => Count == 0;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over) => Apply(c => c.Provided(predicate.Substitute(over, c)));
            }
            #endregion


            #region Interval
            /// <summary>
            /// An interval represents all numbres in between two Entities
            /// <see cref="Interval.LeftClosed"/> stands for whether <see cref="Interval.Left"/> is included
            /// <see cref="Interval.RightClosed"/> stands for whether <see cref="Interval.Right"/> is included
            /// </summary>
            public sealed partial record Interval(Entity Left, bool LeftClosed, Entity Right, bool RightClosed) : Set, IEquatable<Interval?>
            {
                /// <summary>
                /// Checks whether the interval's ends are both numerical (convenient for some evaluations)
                /// </summary>
                public bool IsNumeric => !LeftReal.IsNaN && !RightReal.IsNaN;

                private Real LeftReal => fLeft.GetValue(static @this => @this.Left.EvaluableNumerical && @this.Left.Evaled is Real re ? re : Real.NaN, this);
                private LazyPropertyA<Real> fLeft;
                private Real RightReal => fRight.GetValue(static @this => @this.Right.EvaluableNumerical && @this.Right.Evaled is Real re ? re : Real.NaN, this);
                private LazyPropertyA<Real> fRight;

                private static bool IsALessThanB(Real A, Real B, bool closed)
                    => A < B || closed && A == B;

                internal Interval New(Entity left, Entity right)
                    => ReferenceEquals(Left, left) 
                    && ReferenceEquals(Right, right)
                    ? this : new Interval(left, LeftClosed, right, RightClosed) { Codomain = Codomain };

                internal Interval New(Entity left, bool leftClosed, Entity right, bool rightClosed)
                    => ReferenceEquals(Left, left) && ReferenceEquals(Right, right)
                    && LeftClosed == leftClosed && RightClosed == rightClosed
                    ? this : new Interval(left, leftClosed, right, rightClosed) { Codomain = Codomain };

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Left.Replace(func), Right.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    entity = entity.Evaled;
                    if (entity is Complex and not Real)
                    {
                        contains = false;
                        return true;
                    }
                    if (Left == Right && !(LeftClosed && RightClosed))
                    {
                        contains = false;
                        return true;
                    }
                    if (!IsNumeric)
                    {
                        if (entity == Left)
                        {
                            contains = LeftClosed;
                            return true;
                        }
                        if (entity == Right)
                        {
                            contains = RightClosed;
                            return true;
                        }
                    }
                    if (entity is not Real re)
                        return false;
                    if (!IsNumeric)
                        return false;
                    contains = IsALessThanB(LeftReal, re, LeftClosed) && IsALessThanB(re, RightReal, RightClosed);
                    return true;
                }

                internal override Priority Priority => Priority.Leaf;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Left, Right };

                /// <summary>
                /// Checks that two intervals are equal
                /// If one is not interval, false is returned
                /// </summary>
                public bool Equals(Interval? other)
                    => other is not null && (Left == other.Left
                        && Right == other.Right
                        && LeftClosed == other.LeftClosed
                        && RightClosed == other.RightClosed);

                /// <inheritdoc/>
                public override int GetHashCode()
                    => (Left, LeftClosed, Right, RightClosed).GetHashCode();

                /// <inheritdoc/>
                public override bool IsSetFinite => false;

                /// <inheritdoc/>
                public override bool IsSetEmpty => false;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over) => new ConditionalSet(over, predicate & over.In(this));
            }
            #endregion

            #region Conditional Set
            /// <summary>
            /// This is the most abstract set. Technically, you can describe
            /// any set within this one. Formally, you can define set A of
            /// a condition F(x) in the following way: for each element x in 
            /// the Universal x belongs to A if and only if F(x).
            /// </summary>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using static AngouriMath.Entity.Set;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.MathS.Sets;
            /// 
            /// var set1 = Finite(1, 2, 3);
            /// var set2 = Finite(2, 3, 4);
            /// var set3 = MathS.Interval(-6, 2);
            /// var set4 = new ConditionalSet("x", "100 &gt; x2 &gt; 81");
            /// Console.WriteLine(Union(set1, set2));
            /// Console.WriteLine(Union(set1, set2).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Union(set1, set3));
            /// Console.WriteLine(Union(set1, set3).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Union(set1, set4));
            /// Console.WriteLine(ElementInSet(3, Union(set1, set4)));
            /// Console.WriteLine(ElementInSet(3, Union(set1, set4)).Simplify());
            /// Console.WriteLine(ElementInSet(4, Union(set1, set4)));
            /// Console.WriteLine(ElementInSet(4, Union(set1, set4)).Simplify());
            /// Console.WriteLine(ElementInSet(9.5, Union(set1, set4)));
            /// Console.WriteLine(ElementInSet(9.5, Union(set1, set4)).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Intersection(set1, set2));
            /// Console.WriteLine(Intersection(set1, set2).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Intersection(set2, set3));
            /// Console.WriteLine(Intersection(set2, set3).Simplify());
            /// Console.WriteLine("----------------------");
            /// var set5 = MathS.Interval(-3, 11);
            /// Console.WriteLine(Intersection(set3, set5));
            /// Console.WriteLine(Intersection(set3, set5).Simplify());
            /// Console.WriteLine(Union(set3, set5));
            /// Console.WriteLine(Union(set3, set5).Simplify());
            /// Console.WriteLine(SetSubtraction(set3, set5));
            /// Console.WriteLine(SetSubtraction(set3, set5).Simplify());
            /// Console.WriteLine("----------------------");
            /// Entity syntax1 = @"{ 1, 2, 3 } /\ { 2, 3, 4 }";
            /// Console.WriteLine(syntax1);
            /// Console.WriteLine(syntax1.Simplify());
            /// Console.WriteLine("----------------------");
            /// Entity syntax2 = @"5 in ([1; +oo) \/ { x : x &lt; -4 })";
            /// Console.WriteLine(syntax2);
            /// Console.WriteLine(syntax2.Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), Q));
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), Q).Simplify());
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), R));
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), R).Simplify());
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), C));
            /// Console.WriteLine(Intersection(Finite(pi, e, 6, 5.5m, 1 + 3 * i), C).Simplify());
            /// </code>
            /// Prints
            /// <code>
            /// { 1, 2, 3 } \/ { 2, 3, 4 }
            /// { 1, 2, 3, 4 }
            /// ----------------------
            /// { 1, 2, 3 } \/ [-6; 2]
            /// { 3 } \/ [-6; 2]
            /// ----------------------
            /// { 1, 2, 3 } \/ { x : 100 &gt; x ^ 2 and x ^ 2 &gt; 81 }
            /// 3 in { 1, 2, 3 } \/ { x : 100 &gt; x ^ 2 and x ^ 2 &gt; 81 }
            /// True
            /// 4 in { 1, 2, 3 } \/ { x : 100 &gt; x ^ 2 and x ^ 2 &gt; 81 }
            /// False
            /// 19/2 in { 1, 2, 3 } \/ { x : 100 &gt; x ^ 2 and x ^ 2 &gt; 81 }
            /// True
            /// ----------------------
            /// { 1, 2, 3 } /\ { 2, 3, 4 }
            /// { 2, 3 }
            /// ----------------------
            /// { 2, 3, 4 } /\ [-6; 2]
            /// { 2 }
            /// ----------------------
            /// [-6; 2] /\ [-3; 11]
            /// [-3; 2]
            /// [-6; 2] \/ [-3; 11]
            /// [-6; 11]
            /// [-6; 2] \ [-3; 11]
            /// [-6; -3)
            /// ----------------------
            /// { 1, 2, 3 } /\ { 2, 3, 4 }
            /// { 2, 3 }
            /// ----------------------
            /// 5 in [1; +oo) \/ { x : x &lt; -4 }
            /// True
            /// ----------------------
            /// { pi, e, 6, 11/2, 1 + 3i } /\ QQ
            /// { 6, 11/2 }
            /// { pi, e, 6, 11/2, 1 + 3i } /\ RR
            /// { pi, e, 6, 11/2 }
            /// { pi, e, 6, 11/2, 1 + 3i } /\ CC
            /// { pi, e, 6, 11/2, 1 + 3i }
            /// </code>
            /// </example>
            public sealed partial record ConditionalSet(Entity Var, Entity Predicate) : Set, IEquatable<ConditionalSet?>
            {
                /// <summary>The name this set builder binds. See <see cref="Core.Binding"/>.</summary>
                public Entity Var { get; init; } = Binding.Of(BoundName(Var)).Name;

                /// <summary>
                /// What holds of <see cref="Var"/> exactly for the members of this set. A
                /// membership written in the name position, <c>{ x in S : p }</c>, is the first
                /// conjunct here: the set is <c>{ x : x in S and p }</c>, and prints as it was
                /// written.
                /// </summary>
                public Entity Predicate { get; init; } = Binding.Of(BoundName(Var)).In(WithDeclaredMembership(Var, Predicate));

                // `{ x in ZZ : x < 0 }` arrives with `x in ZZ` in the name position. Kept there,
                // the bound name was the membership statement itself: substituting a value for
                // `x` found nothing to substitute, so `-3 in { x in ZZ : x < 0 }` was never
                // decided, and `x` was not seen as bound. The name is `x`; the membership is a
                // conjunct of the predicate. https://github.com/asc-community/AngouriMath/issues/1409
                private static Entity BoundName(Entity var)
                    => var is Inf(var name, _) ? name : var;

                private static Entity WithDeclaredMembership(Entity var, Entity predicate)
                    => var is Inf(var name, var set) ? name.In(set) & predicate : predicate;

                /// <summary>
                /// The membership a set builder was written with, <c>{ x in S : p }</c>, and the
                /// rest of its predicate; <see langword="null"/> where it was written <c>{ x : p }</c>.
                /// </summary>
                internal (Entity set, Entity rest)? DeclaredMembership
                    => Predicate is Andf(Inf(var name, var set), var rest) && name == Var ? (set, rest) : null;

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Var, Predicate.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    var substituted = Predicate.Substitute(Var, entity);
                    substituted = substituted.Simplify();
                    if (substituted.EvaluableBoolean)
                    {
                        contains = substituted.EvalBoolean();
                        return true;
                    }
                    // A predicate that is False where its condition holds, and has no truth
                    // value where it does not, is not membership either way -- so this much is
                    // decided even though the condition is not. `x = a and x > a` at x := a is
                    // False for real a and undefined for the rest of the plane, and a is not a
                    // member of the set on any of it.
                    // https://github.com/asc-community/AngouriMath/issues/878
                    var core = substituted;
                    while (core is Providedf(var inner, _))
                        core = inner;
                    if (!core.EvaluableBoolean)
                        return false;
                    bool held = core.EvalBoolean();
                    return !held;
                }

                internal Entity New(Entity var, Entity predicate)
                    => ReferenceEquals(Var, var) && ReferenceEquals(Predicate, predicate) ?
                    this : new ConditionalSet(var, predicate) { Codomain = Codomain };

                internal override Priority Priority => Priority.Leaf;

                // The predicate is the child, but the set variable X is not the same as X out of the
                // set, so to avoid ambiguity it is renamed to one that nothing else can be called.
                //
                // CreateTemp, rather than a name derived from the predicate's hash code, which is
                // what this did until #891. That mapped the hash onto four lowercase letters, and
                // four lowercase letters can spell `true` -- which went through MathS.Var, and
                // MathS.Var *parses*, so the parser read a boolean and the conversion to Variable
                // threw. One name in 26^4 does that, and string hash codes are randomised per
                // process, so it surfaced as a CI failure that would not reproduce. The same four
                // letters could also coincide with a free variable of the predicate and capture it,
                // which is silent. CreateTemp is derived from the predicate's variables rather than
                // its hash: `%1` upward, skipping what is taken, so it is fresh by construction and
                // has no reading as anything but a variable.
                // https://github.com/asc-community/AngouriMath/issues/891
                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() =>
                    new[] { Predicate.Substitute(Var, Variable.CreateTemp(Predicate.Vars)) };

                /// <summary>
                /// The written parts, which is what a pattern has to be able to say. The rename
                /// above is what makes traversal alpha-invariant and it is exactly what puts the
                /// bound name out of a pattern's reach, so matching reads the pair as declared.
                /// <a href="https://github.com/asc-community/AngouriMath/issues/1074">#1074</a>
                /// </summary>
                /// <remarks>
                /// A pattern that reads these still cannot tell <c>{ x : x &gt; 0 }</c> from
                /// <c>{ y : y &gt; 0 }</c>, because the only pattern allowed in the name position
                /// is a hole — <see cref="Core.Transformations.Matching.MatchPattern"/>'s
                /// <c>Binder</c> refuses anything else. It is the hole being repeated in the body
                /// that says "the same variable", and that reads the same under either name.
                /// </remarks>
                internal override IReadOnlyList<Entity> MatchableChildren => new[] { Var, Predicate };

                /// <inheritdoc/>
                public override bool IsSetFinite => false;
                /// <inheritdoc/>
                public override bool IsSetEmpty => Predicate.Evaled == Boolean.False;

                /// <summary>
                /// Compares two ConditionalSets
                /// If one is not CSet, false is returned
                /// </summary>
                public bool Equals(ConditionalSet? other)
                {
                    if (other is null) // invalid cast
                        return false;
                    var (one, two) = SetOperators.MergeToOneVariable(this, other);
                    return one.Predicate == two.Predicate;
                }


                [ConstantField] private readonly static Variable universalVoidConstant = Variable.CreateVariableUnchecked("%");
                /// <inheritdoc/>
                public override int GetHashCode()
                    => Predicate.Substitute(Var, universalVoidConstant).GetHashCode();

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over) => new ConditionalSet(Var, Predicate & predicate.Substitute(over, Var));
            }
            #endregion

            #region Special Set
            /// <summary>
            /// Special set is something that cannot be easily expressed in other
            /// types of sets.
            /// </summary>
            public abstract partial record SpecialSet : Set
            {
                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(this);

                // Since there's a very small number of domains, it's wiser to
                // cache them all
                private static Dictionary<Domain, SpecialSet> InnerStorage => innerStorage ??= new();
                [ThreadStatic] private static Dictionary<Domain, SpecialSet>? innerStorage;

                /// <summary>
                /// Creates an instance of special set from a domain
                /// </summary>
                public static SpecialSet Create(Domain domain)
                { 
                    if (InnerStorage.TryGetValue(domain, out var res))
                        return res;
                    
                    SpecialSet result = domain switch
                    {
                        Domain.Boolean => new Booleans(),
                        Domain.Prime => new Primes(),
                        Domain.PositiveInteger => new PositiveIntegers(),
                        Domain.NonNegativeInteger => new NonNegativeIntegers(),
                        Domain.Integer => new Integers(),
                        Domain.Rational => new Rationals(),
                        Domain.Real => new Reals(),
                        Domain.Complex => new Complexes(),
                        // Domain.Any is a documented member of the enum and means "no restriction",
                        // which is not a set this library has a node for -- Domains.IsWithinDomain
                        // answers it before ever reaching here. Either that or a value cast in from
                        // outside the enum, and neither is the library's fault, so neither is told
                        // to file a bug report.
                        _ => throw new NotSufficientlySupportedException(
                            $"There is no special set for domain {domain}")
                    };
                    InnerStorage[domain] = result;
                    return result;
                }

                internal static Domain ToDomain(string domain)
                    => domain switch
                    {
                        "BB" or "Booleans" => Domain.Boolean,
                        "PP" or "Primes" => Domain.Prime,
                        "ZZ+" or "PositiveIntegers" => Domain.PositiveInteger,
                        "ZZ*" or "NonNegativeIntegers" => Domain.NonNegativeInteger,
                        "ZZ" or "Integers" => Domain.Integer,
                        "QQ" or "Rationals" => Domain.Rational,
                        "RR" or "Reals" => Domain.Real,
                        "CC" or "Complexes" => Domain.Complex,
                        // A name this switch does not know came from the caller: Create(string) is
                        // public, and the only other route in is a SPECIALSET token, every spelling
                        // of which is listed above. UnrecognizedDomainException has existed for
                        // this case since it was written and nothing threw it.
                        _ => throw new UnrecognizedDomainException(domain)
                    };

                /// <summary>
                /// Creates an instance of special set from a string
                /// </summary>
                public static SpecialSet Create(string domain)
                    => Create(ToDomain(domain));


                internal override Priority Priority => Priority.Leaf;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();

                /// <inheritdoc/>
                public override bool IsSetFinite => false;

                /// <inheritdoc/>
                public override bool IsSetEmpty => false;

                /// <summary>Checks whether the given element</summary>
                public abstract bool MayContain(Entity entity);

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    var evaled = entity.Evaled;
                    if (evaled.IsSymbolic)
                        return false;
                    // MayContain asks whether something *might* be a member, and so is
                    // permissive about everything that is not a constant leaf. That is what
                    // its other caller needs -- a codomain guard has to let through what it
                    // has not ruled out -- but membership is the opposite question, and
                    // answering it with the permissive result made a matrix a member of BB,
                    // ZZ, QQ, RR and CC at once, and of ZZ /\ BB with them.
                    // A closed non-leaf -- a matrix, a finite set, an interval -- is neither
                    // a number nor a boolean, so it is a member of none of these, and that
                    // is decided rather than unknown.
                    // https://github.com/asc-community/AngouriMath/issues/995
                    if (!evaled.IsConstantLeaf)
                        return true;
                    contains = MayContain(evaled);
                    return true;
                }

                internal abstract Domain ToDomain();

                /// <summary>
                /// A set of all booleans
                /// </summary>
                public sealed partial record Booleans : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Boolean || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.Boolean;
                }

                /// <summary>
                /// A set of all integers
                /// </summary>
                public sealed partial record Integers : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Integer || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.Integer;
                }

                /// <summary>
                /// The set of all non-negative integers, <c>{0, 1, 2, ...}</c>, written <c>ZZ*</c>.
                /// There is no set called "the natural numbers" here, because the name means
                /// this set to some authors and <see cref="PositiveIntegers"/> to others
                /// (MathWorld recommends these two names over "natural number" for that reason,
                /// https://mathworld.wolfram.com/N.html). A subset of <see cref="Integers"/>, and
                /// like every special set a <see cref="Domain"/> a node can be declared over:
                /// <c>domain(x, ZZ*)</c> is <c>NaN</c> at a negative integer.
                /// https://github.com/asc-community/AngouriMath/issues/1409
                /// </summary>
                public sealed partial record NonNegativeIntegers : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Integer { EInteger.Sign: >= 0 } || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.NonNegativeInteger;
                }

                /// <summary>
                /// The set of all positive integers, <c>{1, 2, 3, ...}</c>, written <c>ZZ+</c>.
                /// See <see cref="NonNegativeIntegers"/> for why neither is called the natural
                /// numbers. A subset of <see cref="Integers"/> and the <see cref="Domain"/>
                /// <see cref="Domain.PositiveInteger"/>.
                /// https://github.com/asc-community/AngouriMath/issues/1409
                /// </summary>
                public sealed partial record PositiveIntegers : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Integer { EInteger.Sign: > 0 } || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.PositiveInteger;
                }

                /// <summary>
                /// The set of all prime numbers, <c>{2, 3, 5, 7, ...}</c>, written <c>PP</c>
                /// (<c>\mathbb{P}</c>, SymPy's <c>S.Primes</c>). A subset of
                /// <see cref="PositiveIntegers"/> and the <see cref="Domain"/>
                /// <see cref="Domain.Prime"/>. Membership is decided for a whole number that fits
                /// a machine word, by trial division, and left open beyond that rather than
                /// answered slowly or wrongly. https://github.com/asc-community/AngouriMath/issues/1450
                /// </summary>
                public sealed partial record Primes : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Integer prime && IsDecidablyPrime(prime) is true || !entity.IsConstantLeaf;

                    /// <inheritdoc/>
                    public override bool TryContains(Entity entity, out bool contains)
                    {
                        contains = false;
                        var evaled = entity.Evaled;
                        if (evaled.IsSymbolic)
                            return false;
                        if (!evaled.IsConstantLeaf)
                            return true;
                        if (evaled is Integer whole)
                        {
                            if (IsDecidablyPrime(whole) is not { } decided)
                                return false;
                            contains = decided;
                            return true;
                        }
                        return true;
                    }

                    /// <summary>
                    /// Whether the whole number is prime, or <see langword="null"/> where it is not
                    /// decided: see <see cref="Functions.Primes.IsPrime"/>.
                    /// </summary>
                    internal static bool? IsDecidablyPrime(Integer whole)
                        => Functions.Primes.IsPrime(whole.EInteger);

                    internal override Domain ToDomain() => Domain.Prime;
                }

                /// <summary>
                /// A set of all rational numbers
                /// </summary>
                public sealed partial record Rationals : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Rational || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.Rational;
                }

                /// <summary>
                /// A set of all real numbers
                /// </summary>
                public sealed partial record Reals : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Real || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.Real;
                }

                /// <summary>
                /// A set of all complex numbers
                /// </summary>
                public sealed partial record Complexes : SpecialSet
                {
                    /// <inheritdoc/>
                    public override bool MayContain(Entity entity)
                        => entity is Complex || !entity.IsConstantLeaf;
                    internal override Domain ToDomain() => Domain.Complex;
                }

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over) => new ConditionalSet(over, predicate & over.In(this));
            }
            #endregion  

            #region Union
            /// <summary>
            /// Unites two sets
            /// It is true that an entity is in a union if it is at least in one of union's operands
            /// </summary>
            public sealed partial record Unionf(Entity Left, Entity Right) : Set
            {
                private Unionf New(Entity left, Entity right)
                    => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new Unionf(left, right);

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Left.Replace(func), Right.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    if (Left is not Set left || Right is not Set right) return false;
                    // Kleene's or: a member of either side is a member, and an element is outside only
                    // where both sides decide it is. One side undecided and the other excluding it leaves
                    // it undecided: 2^89 - 1 in PP \/ [0; 1] is not False, the number being prime.
                    // https://github.com/asc-community/AngouriMath/issues/2219
                    var leftDecided = left.TryContains(entity, out var inLeft);
                    if (leftDecided && inLeft)
                    {
                        contains = true;
                        return true;
                    }
                    var rightDecided = right.TryContains(entity, out var inRight);
                    if (rightDecided && inRight)
                    {
                        contains = true;
                        return true;
                    }
                    return leftDecided && rightDecided;
                }

                internal override Priority Priority => Priority.Union;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Left, Right };

                /// <inheritdoc/>
                public override bool IsSetFinite => isSetFinite.GetValue(static @this =>
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet finite2 && finite1.IsSetFinite && finite2.IsSetFinite, this);
                private LazyPropertyA<bool> isSetFinite;

                /// <inheritdoc/>
                public override bool IsSetEmpty => isSetEmpty.GetValue(static @this =>
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet finite2 && finite1.IsSetEmpty && finite2.IsSetEmpty, this);
                private LazyPropertyA<bool> isSetEmpty;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over)
                    => MathS.Union(
                        Left is Set setLeft ? setLeft.Filter(predicate, over) : Left,
                        Right is Set setRight ? setRight.Filter(predicate, over) : Right
                        );
            }
            #endregion

            #region Intersection
            /// <summary>
            /// Finds the intersection of two sets
            /// It is true that an entity is in an intersection if it is in both of intersection's operands
            /// </summary>
            public sealed partial record Intersectionf(Entity Left, Entity Right) : Set
            {
                private Intersectionf New(Entity left, Entity right)
                    => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new Intersectionf(left, right);

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Left.Replace(func), Right.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    if (Left is not Set left || Right is not Set right)
                        return false;
                    // Kleene's and: an element either side decides is outside is outside, whatever the
                    // other says, and a member only where both decide it is.
                    // https://github.com/asc-community/AngouriMath/issues/2219
                    var leftDecided = left.TryContains(entity, out var inLeft);
                    if (leftDecided && !inLeft)
                        return true;
                    var rightDecided = right.TryContains(entity, out var inRight);
                    if (rightDecided && !inRight)
                        return true;
                    contains = leftDecided && rightDecided;
                    return contains;
                }

                internal override Priority Priority => Priority.Intersection;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Left, Right };

                /// <inheritdoc/>
                public override bool IsSetFinite => isSetFinite.GetValue(static @this => 
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet finite2
                    && (finite1.IsSetFinite || finite2.IsSetFinite), this);
                private LazyPropertyA<bool> isSetFinite;

                /// <inheritdoc/>
                public override bool IsSetEmpty => isSetEmpty.GetValue(static @this =>
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet finite2
                    && (finite1.IsSetEmpty || finite2.IsSetEmpty), this);
                private LazyPropertyA<bool> isSetEmpty;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over)
                    => MathS.Intersection(
                        Left is Set setLeft ? setLeft.Filter(predicate, over) : Left,
                        Right is Set setRight ? setRight.Filter(predicate, over) : Right
                        );
            }
            #endregion

            #region Set Minus
            /// <summary>
            /// Finds A &amp; !B
            /// It is true that an entity is in SetMinus if it is in Left but not in Right
            /// </summary>
            public sealed partial record SetMinusf(Entity Left, Entity Right) : Set
            {
                private SetMinusf New(Entity left, Entity right)
                    => ReferenceEquals(Left, left) && ReferenceEquals(Right, right) ? this : new SetMinusf(left, right);

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Left.Replace(func), Right.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    if (Left is not Set left || Right is not Set right)
                        return false;
                    // In the left and not in the right, by Kleene's and: outside where the left decides it
                    // is not a member or the right decides it is, whatever the other says.
                    // https://github.com/asc-community/AngouriMath/issues/2219
                    var leftDecided = left.TryContains(entity, out var inLeft);
                    if (leftDecided && !inLeft)
                        return true;
                    var rightDecided = right.TryContains(entity, out var inRight);
                    if (rightDecided && inRight)
                        return true;
                    contains = leftDecided && rightDecided;
                    return contains;
                }

                internal override Priority Priority => Priority.SetMinus;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Left, Right };

                /// <inheritdoc/>
                public override bool IsSetFinite => isSetFinite.GetValue(static @this =>
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet && finite1.IsSetFinite, this);
                private LazyPropertyA<bool> isSetFinite;

                /// <inheritdoc/>
                public override bool IsSetEmpty => isSetEmpty.GetValue(static @this =>
                    @this.Left is FiniteSet finite1 && @this.Right is FiniteSet finite2
                    && (finite1.IsSetEmpty || finite1 == finite2), this);
                private LazyPropertyA<bool> isSetEmpty;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over)
                    => MathS.SetSubtraction(
                        Left is Set setLeft ? setLeft.Filter(predicate, over) : Left,
                        Right is Set setRight ? setRight.Filter(predicate, over) : Right
                        );
            }
            #endregion

            #region Power set
            /// <summary>
            /// The set of all subsets of a set: <c>powerset(A)</c>, the <c>P(A)</c> of set theory.
            /// Its members are sets, and a set is one of them exactly when it is a subset of
            /// <see cref="Argument"/>, which is how membership is answered whether or not the
            /// argument is finite: <c>{1, 3, 7} in powerset(ZZ)</c> is <c>True</c>. For a finite
            /// argument it evaluates to the list of its <c>2^n</c> subsets, the empty set and the
            /// whole among them; <c>powerset({})</c> is <c>{ {} }</c>, one member, not none.
            /// https://github.com/asc-community/AngouriMath/issues/1409
            /// </summary>
            public sealed partial record Powersetf(Entity Argument) : Set, IUnaryNode
            {
                /// <inheritdoc/>
                public Entity NodeChild => Argument;

                internal Powersetf New(Entity argument)
                    => ReferenceEquals(Argument, argument) ? this : new Powersetf(argument);

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Argument.Replace(func)));

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    if (Argument is not Set argument)
                        return false;
                    if (entity is not Set candidate)
                    {
                        // A number, a truth value or anything else that is not a set is not
                        // a subset of anything, so not a member; a symbol may stand for a set.
                        contains = false;
                        return entity.IsConstantLeaf || entity is Number or Boolean;
                    }
                    if (candidate.SubsetOf(argument).InnerSimplified is Boolean verdict)
                    {
                        contains = verdict == Boolean.True;
                        return true;
                    }
                    return false;
                }

                internal override Priority Priority => Priority.Func;

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Argument };

                /// <inheritdoc/>
                public override bool IsSetFinite => Argument is FiniteSet finite && finite.IsSetFinite;

                /// <inheritdoc/>
                // The empty set is a subset of every set, so a power set is never empty.
                public override bool IsSetEmpty => false;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over)
                    => new ConditionalSet(over, over.In(this) & predicate);
            }
            #endregion

            #region Indexed union and intersection
            /// <summary>
            /// A union or an intersection of a family of sets indexed by a name ranging over a
            /// set: <c>union(A_i, i in I)</c> is <c>⋃_{i ∈ I} A_i</c>, the set of what is in some
            /// <c>A_i</c>, and <c>intersection(A_i, i in I)</c> is <c>⋂_{i ∈ I} A_i</c>, what is in
            /// every one. A binder, like a quantifier: the name is bound throughout the body and
            /// the index set. Folded to the binary operators over a listed index set -- the
            /// reference's <c>⋃_{i=1}^{10} {i, 2i}</c> is <c>union({i, 2 i}, i in ZZ+ /\ [1; 10])</c>
            /// -- and an object that answers membership through the quantifiers otherwise:
            /// <c>x in union(A_i, i in I)</c> is <c>exists i in I : x in A_i</c>.
            /// https://github.com/asc-community/AngouriMath/issues/1409
            /// </summary>
            public abstract partial record IndexedSetOperation(Entity Var, Entity Over, Entity Body) : Set
            {
                /// <summary>The name this operation binds.</summary>
                public Entity Var { get; init; } = Binding.Of(Var).Name;

                /// <summary>The index set the bound name ranges over.</summary>
                public Entity Over { get; init; } = Binding.Of(Var).In(Over);

                /// <summary>The set indexed by the bound name.</summary>
                public Entity Body { get; init; } = Binding.Of(Var).In(Body);

                internal override Priority Priority => Priority.Func;

                /// <summary>The same operation over another name, index set and body.</summary>
                internal abstract IndexedSetOperation New(Entity var, Entity over, Entity body);

                /// <summary>The function this operation is written as: <c>union</c> or <c>intersection</c>.</summary>
                internal abstract string Keyword { get; }

                /// <summary>Whether <paramref name="element"/> is in the family's set for <b>some</b> index (a union) or <b>every</b> index (an intersection).</summary>
                private protected abstract Entity Membership(Entity element);

                /// <inheritdoc/>
                public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Var, Over.Replace(func), Body.Replace(func)));

                /// <inheritdoc/>
                protected override Entity[] InitDirectChildren() => new[] { Var, Over, Body };

                /// <inheritdoc/>
                public override bool TryContains(Entity entity, out bool contains)
                {
                    contains = false;
                    if (entity.ContainsNode(Var))
                        return false;
                    if (Membership(entity).InnerSimplified is Boolean verdict)
                    {
                        contains = verdict == Boolean.True;
                        return true;
                    }
                    return false;
                }

                /// <inheritdoc/>
                public override bool IsSetFinite => false;

                /// <inheritdoc/>
                public override bool IsSetEmpty => false;

                /// <inheritdoc/>
                public override Set Filter(Entity predicate, Variable over)
                    => new ConditionalSet(over, over.In(this) & predicate);
            }

            /// <summary><c>union(A_i, i in I)</c>: the members of some <c>A_i</c>. See <see cref="IndexedSetOperation"/>.</summary>
            public sealed partial record IndexedUnionf(Entity Var, Entity Over, Entity Body) : IndexedSetOperation(Var, Over, Body)
            {
                internal override IndexedSetOperation New(Entity var, Entity over, Entity body)
                    => ReferenceEquals(Var, var) && ReferenceEquals(Over, over) && ReferenceEquals(Body, body) ? this : new IndexedUnionf(var, over, body);
                internal override string Keyword => "union";
                private protected override Entity Membership(Entity element) => new Existsf(Var, Over, element.In(Body));
            }

            /// <summary><c>intersection(A_i, i in I)</c>: the members of every <c>A_i</c>. See <see cref="IndexedSetOperation"/>.</summary>
            public sealed partial record IndexedIntersectionf(Entity Var, Entity Over, Entity Body) : IndexedSetOperation(Var, Over, Body)
            {
                internal override IndexedSetOperation New(Entity var, Entity over, Entity body)
                    => ReferenceEquals(Var, var) && ReferenceEquals(Over, over) && ReferenceEquals(Body, body) ? this : new IndexedIntersectionf(var, over, body);
                internal override string Keyword => "intersection";
                private protected override Entity Membership(Entity element) => new Forallf(Var, Over, element.In(Body));
            }
            #endregion
        }

        /// <summary>
        /// Application of arguments to the given expression
        /// </summary>
        public sealed partial record Application(Entity Expression, LList<Entity> Arguments) : Entity
        {
            private Application New(Entity expr, LList<Entity> arguments)
                    => ReferenceEquals(Expression, expr)
                    && arguments.Count() == Arguments.Count()
                    && ((arguments, Arguments).SequencesAreEqualReferences())
                    ? this 
                    : new Application(expr, arguments);

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                    => func(New(Expression.Replace(func), Arguments.Map(a => a.Replace(func))));

            internal override Priority Priority => Priority.Func;

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => (Expression + Arguments).ToArray();
        }

        /// <summary>
        /// A lambda with the only parameter and its body
        /// </summary>
        public sealed partial record Lambda(Variable Parameter, Entity Body) : Entity
        {
            /// <summary>The name this lambda binds. See <see cref="Core.Binding"/>.</summary>
            public Variable Parameter { get; init; } = Binding.Of(Parameter).Name as Variable ?? Parameter;

            /// <summary>The body, with the bound name meaning what it means here.</summary>
            public Entity Body { get; init; } = Binding.Of(Parameter).In(Body);

            private Lambda New(Variable parameter, Entity body)
                => ReferenceEquals(Parameter, parameter) && ReferenceEquals(Body, body)
                    ? this
                    : new Lambda(parameter, body);

            /// <inheritdoc/>
            public override Entity Replace(Func<Entity, Entity> func)
                => func(New(Parameter, Body.Replace(func)));

            internal override Priority Priority => Priority.Lambda;

            /// <inheritdoc/>
            protected override Entity[] InitDirectChildren() => new [] { Parameter, Body };
        }
    }
}
