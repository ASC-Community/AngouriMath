//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using HonkSharp.Laziness;

//[assembly: InternalsVisibleTo("Playground, PublicKey=")]

namespace AngouriMath.Core
{
#pragma warning disable CA1069 // Enums values should not be duplicated
    internal enum Priority
    { 
        KeywordOperation = 0x0000,

        Lambda   = 10 | KeywordOperation,
        /// <summary>
        /// <c>forall x in S : P</c>: the body runs to the end of the line, as a lambda's does, so
        /// a quantifier under any connective is written in brackets.
        /// </summary>
        Quantifier = 10 | KeywordOperation,
        Provided = 20 | KeywordOperation,

        BooleanOperation = 0x1000,

        Implication  = 10 | BooleanOperation,
        Disjunction  = 30 | BooleanOperation,
        XDisjunction = 50 | BooleanOperation,
        Conjunction  = 70 | BooleanOperation,
        Negation     = 90 | BooleanOperation,

        EqualitySignsOperation = 0x2000,

        Equal          = EqualitySignsOperation,
        /// <summary><c>a = b (mod n)</c> parses at the level of <c>=</c>.</summary>
        Congruent      = EqualitySignsOperation,
        LessThan       = EqualitySignsOperation,
        GreaterThan    = EqualitySignsOperation,
        LessOrEqual    = EqualitySignsOperation,
        GreaterOrEqual = EqualitySignsOperation,

        SetOperation = 0x3000,

        ContainsIn   = 10 | SetOperation,
        /// <summary><c>A subset B</c> parses at the level of <c>in</c>.</summary>
        Subset       = 10 | SetOperation,
        /// <summary><c>a divides b</c> parses at the level of <c>in</c>.</summary>
        Divides      = 10 | SetOperation,
        Union        = 20 | SetOperation,
        SetMinus     = 20 | SetOperation,
        Intersection = 40 | SetOperation,

        NumericalOperation = 0x4000,

        Sum = 20       | NumericalOperation,
        Minus = 20     | NumericalOperation,
        /// <summary>For text formats, <see cref="Func"/> is used instead.</summary>
        LatexCalculusOperation = 30 | NumericalOperation,
        Mul = 40       | NumericalOperation,
        Div = 40       | NumericalOperation,
        Pow = 60       | NumericalOperation,
        Factorial = 70 | NumericalOperation,
        Func = 80      | NumericalOperation,

        Leaf      = 100 | NumericalOperation,
    }
#pragma warning restore CA1069 // Enums values should not be duplicated

    /// <summary>
    /// Any class that supports converting to LaTeX format should implement this interface
    /// </summary>
    /// <example>
    /// <code>
    /// using System;
    /// using AngouriMath;
    /// using static AngouriMath.MathS;
    /// 
    /// Entity expr = "sqrt(a) + integral(sin(x), x)";
    /// Console.WriteLine(expr);
    /// Console.WriteLine(expr.Latexize());
    /// Entity expr2 = "a / b ^ limit(sin(x) - cosh(y), x, +oo)";
    /// Console.WriteLine(expr2);
    /// Console.WriteLine(expr2.Latexize());
    /// </code>
    /// Prints
    /// <code>
    /// sqrt(a) + integral(sin(x), x)
    /// \sqrt{a}+\int \left[\sin\left(x\right)\right] dx
    /// a / b ^ limit(sin(x) - (e ^ y + e ^ (-y)) / 2, x, +oo)
    /// \frac{a}{{b}^{\lim_{x\to \infty } \left[\sin\left(x\right)-\frac{{e}^{y}+{e}^{-y}}{2}\right]}}
    /// </code>
    /// </example>
    public interface ILatexizeable
    { 
        /// <summary>
        /// Converts the object to the LaTeX format
        /// That is, a string that can be later displayed and rendered as LaTeX
        /// </summary>
        public string Latexize(); 
    }
}

namespace AngouriMath
{

    /// <summary>
    /// This is the main class in AngouriMath.
    /// Every node, expression, or number is an <see cref="Entity"/>.
    /// However, you cannot create an instance of this class, look for the nested classes instead.
    /// </summary>
    public abstract partial record Entity : ILatexizeable
    {
        /// <summary>
        /// Two entities are equal when they are the same kind of node with equal parts, which
        /// each node's own record comparison decides; what this level of the record holds
        /// is caches -- the children, the variables, the evaluated value, a dozen
        /// <see cref="LazyPropertyA{T}"/> fields and the sort keys -- and every one of them
        /// is equal to every other by construction, so comparing them is a dozen calls into
        /// <see cref="EqualityComparer{T}"/> that can only answer yes, and the hash a dozen
        /// that can only add zero. The record's synthesized members made them; the solver,
        /// which keeps its candidates in sets keyed by the tree, took 112 ms on the hardest
        /// benchmark with them and 64 without
        /// (https://github.com/asc-community/AngouriMath/issues/1338).
        /// </summary>
        public virtual bool Equals(Entity? other)
            => ReferenceEquals(this, other) || (other is not null && EqualityContract == other.EqualityContract);

        /// <summary>The same for the hash: the node's kind, with each node's parts combined in by its own record.</summary>
        public override int GetHashCode() => EqualityContract.GetHashCode();

        /// <summary>
        /// Returns the array of the direct children. Will be
        /// only called once, so no need to cache anything.
        /// </summary>
        protected abstract Entity[] InitDirectChildren();
        
        /// <summary>
        /// Represents all direct children of a node
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// 
        /// Entity frac = "log(sqrt(x), a + b)";
        /// Console.WriteLine(frac);
        /// foreach (var child in frac.DirectChildren)
        ///     Console.WriteLine(child);
        /// </code>
        /// Prints
        /// <code>
        /// log(sqrt(x), a + b)
        /// sqrt(x)
        /// a + b
        /// </code>
        /// </example>
        public IReadOnlyList<Entity> DirectChildren => directChildren.GetValue(static @this => @this.InitDirectChildren(), this);
        private LazyPropertyA<IReadOnlyList<Entity>> directChildren;

        /// <summary>
        /// The parts a <see cref="Core.Transformations.Matching.MatchPattern"/> takes this node
        /// apart into. <see cref="DirectChildren"/> for every node but one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The two differ only where a node's traversal shape is not its written shape, and
        /// <see cref="Set.ConditionalSet"/> is the only node where that is so — measured across
        /// every binder the language has. A summation, a product, an integral, a derivative, a
        /// limit and a lambda all publish the name they bind as an ordinary child, un-renamed, so
        /// a pattern can already name it and a repeated hole already says "the same variable". A
        /// set builder publishes its predicate alone, with the bound name replaced by a
        /// placeholder invented per traversal, so a pattern could reach neither.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1074">#1074</a>
        /// </para>
        /// <para>
        /// This is the same split <see cref="VarsAndConsts"/> and <see cref="FreeVariables"/>
        /// already make, and for the same reason: what a set builder <em>is</em> is read off its
        /// declared parts, and what it publishes for traversal is a capture-avoiding rewriting of
        /// them. Anything asking a question about the written expression has to ask the first.
        /// </para>
        /// </remarks>
        internal virtual IReadOnlyList<Entity> MatchableChildren => DirectChildren;

        /// <remarks>A depth-first enumeration is required by
        /// <see cref="AngouriMath.Functions.TreeAnalyzer.GetMinimumSubtree"/></remarks>
        /// <summary>
        /// The list of all subnodes of the given expression, including its own. Traverses
        /// the tree the following order:
        /// <ol>
        /// <li>The node itself</li>
        /// <li>All nodes from all children</li>
        /// </ol>
        /// </summary>
        /// <example>
        /// <code>
        /// Entity expr = "a + b / 2 ^ 3";
        /// for (var node in expr.Nodes)
        ///     Console.WriteLine(node);
        /// </code>
        /// Output:
        /// <code>
        /// a + b / 2 ^ 3
        /// a
        /// b / 2 ^ 3
        /// b
        /// 2 ^ 3
        /// 2
        /// 3
        /// </code>
        /// </example>
        public IEnumerable<Entity> Nodes => nodes.GetValue(static @this => @this.DirectChildren.SelectMany(c => c.Nodes).Prepend(@this), this);
        private LazyPropertyA<IEnumerable<Entity>> nodes;

        /// <summary>
        /// Applies the given function to every node starting from the leaves
        /// </summary>
        /// <param name="func">
        /// The delegate that takes the current node as an argument and replaces the current node
        /// with the result of the delegate
        /// </param>
        /// <returns>Processed expression</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// using static AngouriMath.Entity;
        /// using static AngouriMath.Entity.Number;
        /// 
        /// static Entity MySimplify(Entity expr) => expr switch
        /// {
        ///     Sumf(Powf(Sinf(var e1), Integer(2)), Powf(Cosf(var e2), Integer(2))) when e1 == e2 => 1,
        ///     Sinf(Integer(0)) => 0,
        ///     var other => other
        /// };
        /// 
        /// var x = Var("x");
        /// Entity expr = (Sqr(Sin(x / 3)) + Sqr(Cos(x / 3))) + Sin(0);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Replace(MySimplify));
        /// </code>
        /// Prints
        /// <code>
        /// sin(x / 3) ^ 2 + cos(x / 3) ^ 2 + 0
        /// 1 + 0
        /// </code>
        /// </example>
        public abstract Entity Replace(Func<Entity, Entity> func);


        /// <summary>Replaces all <param name="x"/> with <param name="value"/></summary>
        /// <returns>A new expression</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, z) = Var("x", "y", "z");
        /// Entity expr = Sin(x);
        /// var substituted = expr.Substitute(x, pi / 3);
        /// 
        /// Console.WriteLine(expr);
        /// Console.WriteLine(substituted);
        /// Console.WriteLine(substituted.Simplify());
        /// 
        /// Console.WriteLine("-------------------------------");
        /// 
        /// var expr2 = Sin(x) + Cos(y + x) + Factorial(z);
        /// 
        /// var substituted2 =
        ///     expr2
        ///         .Substitute(x, 1)
        ///         .Substitute(y, 2)
        ///         .Substitute(z, 3);
        /// 
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(substituted2);
        /// 
        /// Console.WriteLine("-------------------------------");
        /// 
        /// var expr3 = Sin(x + y) + 1 / Sin(x + y);
        /// var substituted3 = expr3.Substitute(Sin(x + y), Cos(x + y));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(substituted3);
        /// </code>
        /// Prints
        /// <code>
        /// sin(x)
        /// sin(pi / 3)
        /// sqrt(3) / 2
        /// -------------------------------
        /// sin(x) + cos(y + x) + z!
        /// sin(1) + cos(2 + 1) + 3!
        /// -------------------------------
        /// sin(x + y) + 1 / sin(x + y)
        /// cos(x + y) + 1 / cos(x + y)
        /// </code>
        /// </example>
        public virtual Entity Substitute(Entity x, Entity value)
            => this == x ? value : this;

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2
        /// </summary>
        /// <example>
        /// See how <see cref="Substitute(AngouriMath.Entity,AngouriMath.Entity)"/> works.
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// var expr = Sin(x + y) + 1 / Sin(x + y);
        /// 
        /// var sub = expr
        ///     .Substitute((x, y), (5, 11));
        /// Console.WriteLine(sub);
        /// </code>
        /// </example>
        public Entity Substitute((Entity x1, Entity x2) x, (Entity v1, Entity v2) value)
            => Substitute(x.x1, value.v1).Substitute(x.x2, value.v2);

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2 and
        /// x.x3 with value.v3
        /// </summary>
        /// <example>
        /// See how <see cref="Substitute(AngouriMath.Entity,AngouriMath.Entity)"/> works.
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, z) = Var("x", "y", "z");
        /// 
        /// var expr = Sin(x + y) + 1 / Sin(x + y) + z;
        /// 
        /// var sub = expr
        ///     .Substitute((x, y, z), (5, 11, 115));
        /// Console.WriteLine(sub);
        /// </code>
        /// Prints
        /// <code>
        /// sin(5 + 11) + 1 / sin(5 + 11) + 115
        /// </code>
        /// </example>
        public Entity Substitute((Entity x1, Entity x2, Entity x3) x, (Entity v1, Entity v2, Entity v3) value)
            => Substitute(x.x1, value.v1).Substitute(x.x2, value.v2).Substitute(x.x3, value.v3);

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2 and
        /// x.x3 with value.v3 and
        /// x.x4 with value.v4
        /// </summary>
        /// <example>
        /// See how <see cref="Substitute(AngouriMath.Entity,AngouriMath.Entity)"/> works.
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, z, w) = Var("x", "y", "z", "w");
        /// 
        /// var expr = Sin(x + y) + 1 / Sin(x + y) + z / w;
        /// 
        /// var sub = expr
        ///     .Substitute((x, y, z, w), (5, 11, 115, Sqr(w)));
        /// Console.WriteLine(sub);
        /// </code>
        /// Prints
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, z, w) = Var("x", "y", "z", "w");
        /// 
        /// var expr = Sin(x + y) + 1 / Sin(x + y) + z / w;
        /// 
        /// var sub = expr
        ///     .Substitute((x, y, z, w), (5, 11, 115, Sqr(w)));
        /// Console.WriteLine(sub);
        /// </code>
        /// </example>
        public Entity Substitute((Entity x1, Entity x2, Entity x3, Entity x4) x, (Entity v1, Entity v2, Entity v3, Entity v4) value)
            => Substitute(x.x1, value.v1).Substitute(x.x2, value.v2).Substitute(x.x3, value.v3).Substitute(x.x4, value.v4);

        /// <summary>Replaces all <param name="replacements"/></summary>
        /// <example>
        /// See how <see cref="Substitute(AngouriMath.Entity,AngouriMath.Entity)"/> works.
        /// <code>
        /// using System;
        /// using System.Collections.Generic;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, z, w) = Var("x", "y", "z", "w");
        /// 
        /// var expr = Sin(x + y) + 1 / Sin(x + y) + z / w;
        /// 
        /// var subs = new Dictionary&lt;Entity, Entity&gt;()
        /// {
        ///     { x, 1 },
        ///     { y, 2 },
        ///     { z, z + Sin(y) }
        /// };
        /// 
        /// 
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Substitute(subs));
        /// </code>
        /// Prints
        /// <code>
        /// sin(x + y) + 1 / sin(x + y) + z / w
        /// sin(1 + 2) + 1 / sin(1 + 2) + (z + sin(y)) / w
        /// </code>
        /// </example>
        public Entity Substitute<TFrom, TTo>(IReadOnlyDictionary<TFrom, TTo> replacements) where TFrom : Entity where TTo : Entity
        {
            var res = this;
            foreach (var pair in replacements)
                res = res.Substitute(pair.Key, pair.Value);
            return res;
        }

        internal abstract Priority Priority { get; }

        /// <value>
        /// Whether both parts of the complex number are finite
        /// meaning that it could be safely used for calculations.
        /// By finite we mean that it is not a <see cref="MathS.NaN"/>
        /// and it is not positive or negative infinity.
        /// </value>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var x = Var("x");
        /// var expr1 = Sin(x);
        /// Console.WriteLine($"{expr1}, IsFinite: {expr1.IsFinite}");
        /// var expr2 = Sin(x + 5) + 5;
        /// Console.WriteLine($"{expr2}, IsFinite: {expr2.IsFinite}");
        /// var expr3 = Sin(x + 3) / 0;
        /// Console.WriteLine($"{expr3}, IsFinite: {expr3.IsFinite}");
        /// var expr4 = expr3.Evaled;
        /// Console.WriteLine($"{expr4}, IsFinite: {expr4.IsFinite}");
        /// var expr5 = Sin(x / 0);
        /// Console.WriteLine($"{expr5}, IsFinite: {expr5.IsFinite}");
        /// var expr6 = x + +oo;
        /// Console.WriteLine($"{expr6}, IsFinite: {expr6.IsFinite}");
        /// var expr7 = x + +oo * i;
        /// Console.WriteLine($"{expr7}, IsFinite: {expr7.IsFinite}");
        /// var expr8 = -oo + 6 * i;
        /// Console.WriteLine($"{expr8}, IsFinite: {expr8.IsFinite}");
        /// var expr9 = Sin(+oo) - Cos(pi / 3);
        /// Console.WriteLine($"{expr9}, IsFinite: {expr9.IsFinite}");
        /// var expr10 = Hyperbolic.Tanh(NaN + 3);
        /// Console.WriteLine($"{expr10}, IsFinite: {expr10.IsFinite}");
        /// </code>
        /// Prints
        /// <code>
        /// sin(x), IsFinite: True
        /// sin(x + 5) + 5, IsFinite: True
        /// sin(x + 3) / 0, IsFinite: True
        /// NaN, IsFinite: False
        /// sin(x / 0), IsFinite: True
        /// x + +oo, IsFinite: False
        /// x + +ooi, IsFinite: False
        /// -oo + 6i, IsFinite: False
        /// sin(+oo) - cos(pi / 3), IsFinite: False
        /// (e ^ (2 * (NaN + 3)) - 1) / (e ^ (2 * (NaN + 3)) + 1), IsFinite: False
        /// </code>
        /// </example>
        public bool IsFinite => isFinite.GetValue(static @this => @this.ThisIsFinite && @this.DirectChildren.All(x => x.IsFinite), this);
        private LazyPropertyA<bool> isFinite;

        /// <summary>
        /// Not NaN and not infinity
        /// </summary>
        protected virtual bool ThisIsFinite => true;       

        /// <value>Number of nodes in tree</value>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = x;
        /// Console.WriteLine($"{expr1}: {expr1.Complexity}");
        /// var expr2 = x * 2;
        /// Console.WriteLine($"{expr2}: {expr2.Complexity}");
        /// var expr3 = Sin(x);
        /// Console.WriteLine($"{expr3}: {expr3.Complexity}");
        /// var expr4 = Sin(x) + Cos(x + 2);
        /// Console.WriteLine($"{expr4}: {expr4.Complexity}");
        /// </code>
        /// Prints
        /// <code>
        /// x: 1
        /// x * 2: 3
        /// sin(x): 2
        /// sin(x) + cos(x + 2): 7
        /// </code>
        /// </example>
        /// <remarks>
        /// Saturated at <see cref="int.MaxValue"/>: a subtree counts once for every place it is
        /// used, so an expression built by sharing can be larger than that as a tree while being
        /// small in memory, and a caller asking whether it is too large to go on with is owed an
        /// answer rather than an <see cref="System.OverflowException"/>.
        /// https://github.com/asc-community/AngouriMath/issues/1600
        /// </remarks>
        public int Complexity => complexity.GetValue(static @this =>
        {
            // By index: a foreach over the interface allocates an enumerator for every node counted.
            var children = @this.DirectChildren;
            long total = 1;
            for (var i = 0; i < children.Count; i++)
                if ((total += children[i].Complexity) >= int.MaxValue)
                    return int.MaxValue;
            return (int)total;
        }, this);
        private LazyPropertyA<int> complexity;

        /// <summary>
        /// Set of unique variables, for example 
        /// it extracts <c>`x`</c>, <c>`goose`</c> from <c>(x + 2 * goose) - pi * x</c>
        /// </summary>
        /// <returns>
        /// Set of unique variables excluding mathematical constants
        /// such as <see cref="MathS.pi"/> and <see cref="MathS.e"/>
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Lambda(x, x * 2 + Sin(y * pi));
        /// Console.WriteLine(expr1);
        /// Console.WriteLine("Variables:");
        /// foreach (var var in expr1.Vars)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Variables and constants:");
        /// foreach (var var in expr1.VarsAndConsts)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Only free variables:");
        /// foreach (var var in expr1.FreeVariables)
        ///     Console.WriteLine($"  {var}");
        /// </code>
        /// Prints
        /// <code>
        /// x -> x * 2 + sin(y * pi)
        /// Variables:
        ///   x
        ///   y
        /// Variables and constants:
        ///   x
        ///   y
        ///   pi
        /// Only free variables:
        ///   y
        ///   pi
        /// </code>
        /// </example>
        public IReadOnlyList<Variable> Vars
            => vars.GetValue(static @this
                => @this.VarsAndConsts.Where(x => !x.IsConstant).ToList() /* needed to actually compute it and cache */, this);
        private LazyPropertyA<IReadOnlyList<Variable>> vars;

        /// <summary>
        /// Set of unique variables, for example 
        /// it extracts <c>`x`</c>, <c>`goose`</c>, <c>`pi`</c> from <c>(x + 2 * goose) - pi * x</c>
        /// </summary>
        /// <returns>
        /// Set of unique variables and mathematical constants
        /// such as <see cref="MathS.pi"/> and <see cref="MathS.e"/>
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Lambda(x, x * 2 + Sin(y * pi));
        /// Console.WriteLine(expr1);
        /// Console.WriteLine("Variables:");
        /// foreach (var var in expr1.Vars)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Variables and constants:");
        /// foreach (var var in expr1.VarsAndConsts)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Only free variables:");
        /// foreach (var var in expr1.FreeVariables)
        ///     Console.WriteLine($"  {var}");
        /// </code>
        /// Prints
        /// <code>
        /// x -> x * 2 + sin(y * pi)
        /// Variables:
        ///   x
        ///   y
        /// Variables and constants:
        ///   x
        ///   y
        ///   pi
        /// Only free variables:
        ///   y
        ///   pi
        /// </code>
        /// </example>
        public IReadOnlyCollection<Variable> VarsAndConsts => varsAndConsts.GetValue(
            static @this => new HashSet<Variable>(@this switch
                {
                    Variable v => new[] { v },
                    // A set builder's DirectChildren is its predicate with the bound name
                    // renamed to a fresh one, so that two set builders differing only in
                    // that name compare and hash alike. That name is invented, not written:
                    // it is not in the expression, it cannot be typed, and a different
                    // predicate gets a different one. Read the builder's own parts instead.
                    // https://github.com/asc-community/AngouriMath/issues/989
                    Set.ConditionalSet(var bound, var predicate)
                        => predicate.VarsAndConsts.Concat(bound.VarsAndConsts),
                    _ => @this.DirectChildren.SelectMany(x => x.VarsAndConsts)
                }), this);
        private LazyPropertyA<IReadOnlyCollection<Variable>> varsAndConsts;


        /// <summary>
        /// Returns a set of free variables.
        /// We call a bound variable a variable which is a parameter of some
        /// outer lambda. Then, all other variables are free.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Lambda(x, x * 2 + Sin(y * pi));
        /// Console.WriteLine(expr1);
        /// Console.WriteLine("Variables:");
        /// foreach (var var in expr1.Vars)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Variables and constants:");
        /// foreach (var var in expr1.VarsAndConsts)
        ///     Console.WriteLine($"  {var}");
        /// Console.WriteLine("Only free variables:");
        /// foreach (var var in expr1.FreeVariables)
        ///     Console.WriteLine($"  {var}");
        /// </code>
        /// Prints
        /// <code>
        /// x -> x * 2 + sin(y * pi)
        /// Variables:
        ///   x
        ///   y
        /// Variables and constants:
        ///   x
        ///   y
        ///   pi
        /// Only free variables:
        ///   y
        ///   pi
        /// </code>
        /// </example>
        public IReadOnlyCollection<Variable> FreeVariables =>
            freeVariables.GetValue(
                static @this =>
                    @this switch
                    {
                        Lambda(var par, var body)
                            => body.FreeVariables.Where(v => v != par).ToList(),
                        Variable v => new []{ v },
                        // Same as above: the renamed predicate that DirectChildren publishes
                        // would put an invented name in the answer. A set builder also binds
                        // the name it declares, exactly as a lambda binds its parameter, so
                        // that name is not free either.
                        // https://github.com/asc-community/AngouriMath/issues/989
                        Set.ConditionalSet(var bound, var predicate)
                            => predicate.FreeVariables.Where(v => !bound.VarsAndConsts.Contains(v)).ToList(),
                        // A summation and a product bind their index, so sum(k, k, 1, n) is a
                        // function of n alone. The index is bound over the bounds as well as the
                        // summand, which is what Binding says of itself: the name a binder is
                        // handed is honoured throughout it, through the summand and the bounds.
                        // https://github.com/asc-community/AngouriMath/issues/1019
                        Summationf(var body, var index, var from, var to)
                            => BoundBy(index, body, from, to),
                        Productf(var body, var index, var from, var to)
                            => BoundBy(index, body, from, to),
                        // A sum over a set binds the name that ranges over the set:
                        // sum(w * a, w in { w : w^2 = 2 }) is a function of a alone.
                        SumOverSetf(var body, var bound, var over) => BoundBy(bound, body, over),
                        // An extremum over a set binds the variable that ranges over the set:
                        // max(t^2 + a, t in [0; 1]) is a function of a alone.
                        Maximumf(var body, var bound, var over) => BoundBy(bound, body, over),
                        Minimumf(var body, var bound, var over) => BoundBy(bound, body, over),
                        Argmaxf(var body, var bound, var over) => BoundBy(bound, body, over),
                        Argminf(var body, var bound, var over) => BoundBy(bound, body, over),
                        // An integral binds its variable only when it has limits to bind it
                        // between. The indefinite one does not: the antiderivative of t * b over
                        // t is b * t ^ 2 / 2 + C, which is still a function of t. Nor does a
                        // derivative -- d/dt denotes a function of t. Their variable stays free
                        // on purpose, and a sweep that "fixes" that makes them wrong.
                        Integralf { Range: { } limits } integral
                            => BoundBy(integral.Var, integral.Expression, limits.from, limits.to),
                        // A limit binds its variable always, and this is the one calculus
                        // operator of the three for which that is unconditional. The reason the
                        // two above are conditional does not apply to it: an antiderivative and a
                        // derivative are still functions of the variable, and a limit never is.
                        // lim(k, k, 0) is 0, and no limit's value depends on the name it
                        // approaches along -- the destination is where the dependence goes.
                        // https://github.com/asc-community/AngouriMath/issues/989
                        Limitf limit
                            => BoundBy(limit.Var, limit.Expression, limit.Destination),
                        // A quantifier binds its name throughout its body and the set it ranges
                        // over: forall x in S : x < y is a statement about y and S.
                        // https://github.com/asc-community/AngouriMath/issues/1409
                        Quantifier quantifier
                            => BoundBy(quantifier.Var, quantifier.Body, quantifier.Over),
                        _ => new HashSet<Variable>(@this.DirectChildren.SelectMany(c => c.FreeVariables))
                    }
                ,
                this
            );
        private LazyPropertyA<IReadOnlyCollection<Variable>> freeVariables;

        /// <summary>
        /// The free variables of <paramref name="parts"/> taken together, less whatever
        /// <paramref name="binder"/> binds. <paramref name="binder"/> is an <see cref="Entity"/>
        /// rather than a <see cref="Variable"/> because a binder's name position accepts one and
        /// the parser is what settles which name it is.
        /// </summary>
        private static IReadOnlyCollection<Variable> BoundBy(Entity binder, params Entity[] parts)
        {
            var bound = binder.VarsAndConsts;
            var free = new HashSet<Variable>();
            foreach (var part in parts)
                foreach (var v in part.FreeVariables)
                    if (!bound.Contains(v))
                        free.Add(v);
            return free;
        }

        /// <summary>Checks if <paramref name="x"/> is a subnode inside this <see cref="Entity"/> tree.
        /// Optimized for <see cref="Variable"/>.</summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Sin(x + y) - Sqr(x);
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.ContainsNode(y + x));
        /// Console.WriteLine(expr1.ContainsNode(x + y));
        /// </code>
        /// Prints
        /// <code>
        /// sin(x + y) - x ^ 2
        /// False
        /// True
        /// </code>
        /// </example>
        public bool ContainsNode(Entity x) => x is Variable v ? VarsAndConsts.Contains(v) : Nodes.Contains(x);

        /// <summary>
        /// Implicit conversation from string to Entity. See <see cref="MathS.FromString(string,bool)"/>
        /// for how to parse an <see cref="Entity"/> from expression.
        /// </summary>
        /// <param name="expr">The source from which to parse</param>
        public static implicit operator Entity(string expr) => MathS.FromString(expr);

        /// <summary>
        /// Shows how simple the given expression is. The lower - the simpler the expression is.
        /// You might need it to pick the best expression to represent something. Unlike 
        /// <see cref="Complexity"/>, which shows the number of nodes, <see cref="SimplifiedRate"/> 
        /// shows how convenient it is to view the expression. This depends on 
        /// <see cref="MathS.Settings.ComplexityCriteria"/> which can be changed by user.
        /// See <see cref="MathS.Settings.ComplexityCriteria"/> for more details.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Cached for the default criteria only, and computed afresh for any other.</b> The
        /// cache is one slot per <see cref="Entity"/> instance and the criteria is an ambient
        /// setting, so the two do not agree about what the cached number is a rate <i>of</i>: a
        /// rate computed under one cost model was answered with under the next, and
        /// <c>Simplificator.PickSimplest</c> compares candidates by this property — so it would
        /// weigh one model's cached rate against another's freshly computed one and choose on the
        /// strength of it, with nothing anywhere to say the comparison was meaningless.
        /// </para>
        /// <para>
        /// The unset path keeps the cache, which is what nearly every call takes and what the
        /// slot was there for. A criteria anybody has scoped computes without caching, so it can
        /// neither be answered with somebody else's number nor leave one behind for them. The
        /// test is <c>IsOverriden</c> — the setting's own "nobody expressed an opinion", which is
        /// what <c>BudgetLedger.For</c> already asks of <see cref="MathS.Settings.Budget"/> — and
        /// not an equality against the default function: it is one ambient read rather than a
        /// read plus a delegate comparison, on a property <c>Simplificator.PickSimplest</c> reads
        /// once per candidate. A caller who scopes the default function explicitly gets the same
        /// number by the uncached route.
        /// </para>
        /// </remarks>
        public double SimplifiedRate
        {
            get
            {
                var criteria = MathS.Settings.ComplexityCriteria;
                return criteria.IsOverriden
                    ? criteria.Value(this)
                    : simplifiedRate.GetValue(criteria.Default, this);
            }
        }
        private LazyPropertyA<double> simplifiedRate;

        /// <summary>
        /// <see cref="CostModel.DefaultCost"/> of this node, computed once per instance -- the
        /// same slot <see cref="SimplifiedRate"/> fills on its unset path, so the two never
        /// disagree. <c>DefaultCost</c> recurses through this rather than through itself: a
        /// candidate the simplifier ranks shares most of its subtrees with the candidates
        /// before it, and re-walking a shared subtree for every candidate was 3.66 GB of
        /// allocation per <c>SimplifyHard</c>, some 315 whole-tree walks of 11.6 MB each.
        /// </summary>
        internal double DefaultCostCached
            => simplifiedRate.GetValue(MathS.Settings.ComplexityCriteria.Default, this);

        /// <summary>Checks whether the given expression contains variable</summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Sin(x + y) - Sqr(x);
        /// Console.WriteLine(expr1.IsSymbolic);
        /// var expr2 = Sin(5) - 22;
        /// Console.WriteLine(expr2.IsSymbolic);
        /// var expr3 = Sin(5) - 22 / x;
        /// Console.WriteLine(expr3.IsSymbolic);
        /// </code>
        /// Prints
        /// <code>
        /// True
        /// False
        /// True
        /// </code>
        /// </example>
        public bool IsSymbolic => Vars.Any();

        /// <summary>
        /// Checks whether the given expression is a finite constant leaf
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// Entity expr1 = 5;
        /// Console.WriteLine($"{expr1}, IsConstantLeaf: {expr1.IsConstantLeaf}");
        /// Entity expr2 = Sin(5);
        /// Console.WriteLine($"{expr2}, IsConstantLeaf: {expr2.IsConstantLeaf}");
        /// Entity expr3 = pi;
        /// Console.WriteLine($"{expr3}, IsConstantLeaf: {expr3.IsConstantLeaf}");
        /// Entity expr4 = 3 + 4 * i;
        /// Console.WriteLine($"{expr4}, IsConstantLeaf: {expr4.IsConstantLeaf}");
        /// Entity expr5 = (Entity)3 + 4 * i;
        /// Console.WriteLine($"{expr5}, IsConstantLeaf: {expr5.IsConstantLeaf}");
        /// var expr6 = expr5.InnerSimplified;
        /// Console.WriteLine($"{expr6}, IsConstantLeaf: {expr6.IsConstantLeaf}");
        /// var expr7 = GreaterThan(pi, e);
        /// Console.WriteLine($"{expr7}, IsConstantLeaf: {expr7.IsConstantLeaf}");
        /// var expr8 = expr7.Evaled;
        /// Console.WriteLine($"{expr8}, IsConstantLeaf: {expr8.IsConstantLeaf}");
        /// </code>
        /// Prints
        /// <code>
        /// 5, IsConstantLeaf: True
        /// sin(5), IsConstantLeaf: False
        /// pi, IsConstantLeaf: False
        /// 3 + 4i, IsConstantLeaf: True
        /// 3 + 4i, IsConstantLeaf: False
        /// 3 + 4i, IsConstantLeaf: True
        /// pi > e, IsConstantLeaf: False
        /// True, IsConstantLeaf: True
        /// </code>
        /// </example>
        public bool IsConstantLeaf => this is Boolean or Number or Set.SpecialSet;

        /// <summary>
        /// Checks whether this entity represents the undefined value (<see cref="MathS.NaN"/>).
        /// </summary>
        public bool IsNaN => this == Real.NaN;
    }
}
