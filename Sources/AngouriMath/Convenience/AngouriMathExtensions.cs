//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using NumericsComplex = System.Numerics.Complex;
using PeterO.Numbers;
using AngouriMath.Core.Exceptions;
using System;
using System.Collections;

namespace AngouriMath.Extensions
{
    using static AngouriMath.Entity.Set;
    using static Entity;
    using static Entity.Number;

    /// <summary>
    /// Class for some convenient extensions
    /// </summary>
    public static partial class AngouriMathExtensions
    {
        /// <summary>
        /// Concatenates the argument matrix to the right of the current.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var a = MathS.Matrix(new Entity[,] { { 1, 2 }, { 3, 4 } });
        /// var b = MathS.Matrix(new Entity[,] { { 5 }, { 6 } });
        /// Console.WriteLine(a.ConcatToTheRight(b));
        /// </code>
        /// Prints
        /// <code>
        /// [[1, 2, 5], [3, 4, 6]]
        /// </code>
        /// </example>
        public static Matrix ConcatToTheRight(this Matrix a, Matrix b)
            => MathS.Matrices.Concat(MathS.Matrices.Direction.Horizontal, a, b);
        
        /// <summary>
        /// Concatenates the argument matrix to the bottom of the current.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// var a = MathS.Matrix(new Entity[,] { { 1, 2 }, { 3, 4 } });
        /// var b = MathS.Matrix(new Entity[,] { { 5, 6 } });
        /// Console.WriteLine(a.ConcatToTheBottom(b));
        /// </code>
        /// Prints
        /// <code>
        /// [[1, 2], [3, 4], [5, 6]]
        /// </code>
        /// </example>
        public static Matrix ConcatToTheBottom(this Matrix a, Matrix b)
            => MathS.Matrices.Concat(MathS.Matrices.Direction.Vertical, a, b);
    
        /// <summary>
        /// Converts a given sequence of elements into a vector,
        /// which is a one-column matrix
        /// </summary>
        /// <example>
        /// The flat printed form looks like a row, but the shape is one column:
        /// <code>
        /// var v = new Entity[] { "x", "y", "z" }.ToVector();
        /// Console.WriteLine(v);
        /// Console.WriteLine(v.RowCount);
        /// Console.WriteLine(v.ColumnCount);
        /// </code>
        /// Prints
        /// <code>
        /// [x, y, z]
        /// 3
        /// 1
        /// </code>
        /// </example>
        public static Matrix ToVector(this IEnumerable<Entity> elements)
            => MathS.Vector(elements.ToArray());

        /// <summary>
        /// Sums all the given terms and returns the resulting expression
        /// new Entity[]{ 1, 2, 3 }.SumAll() -&gt; "1 + 2 + 3"
        /// </summary>
        /// <example>
        /// The terms are hung into one expression, not added up:
        /// <code>
        /// Console.WriteLine(new Entity[] { 1, 2, 3, 4 }.SumAll());
        /// Console.WriteLine(new Entity[] { 1, 2, 3, 4 }.SumAll().Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 1 + 2 + 3 + 4
        /// 10
        /// </code>
        /// </example>
        /// <remarks>
        /// The empty sum is <c>0</c>, which is what makes
        /// <c>xs.Concat(ys).SumAll() == xs.SumAll() + ys.SumAll()</c> hold for every pair
        /// including the empty one. It used to reach <c>MultiHangBinary</c>'s genuine
        /// precondition and throw an <c>AngouriBugException</c> — whose message asks the caller
        /// to report a bug against this repository, for a list their own <c>Where</c> happened
        /// to filter to nothing.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1028">#1028</a>
        /// </remarks>
        public static Entity SumAll(this IEnumerable<Entity> terms)
            => terms.ToArray() is { Length: > 0 } array
                ? TreeAnalyzer.MultiHangBinary(array, (a, b) => a + b)
                : Entity.Number.Integer.Zero;

        /// <summary>
        /// Multiplies all the given terms and returns the resulting expression
        /// new Entity[]{ 1, 2, 3 }.MultiplyAll() -&gt; "1 * 2 * 3"
        /// </summary>
        /// <example>
        /// <code>
        /// var factorial = Enumerable.Range(1, 5).Select(i =&gt; (Entity)i).MultiplyAll();
        /// Console.WriteLine(factorial);
        /// Console.WriteLine(factorial.EvalNumerical());
        /// </code>
        /// Prints
        /// <code>
        /// 1 * 2 * 3 * 4 * 5
        /// 120
        /// </code>
        /// </example>
        /// <remarks>
        /// The empty product is <c>1</c>, for the reason the empty sum is <c>0</c> — see
        /// <see cref="SumAll"/>.
        /// </remarks>
        public static Entity MultiplyAll(this IEnumerable<Entity> terms)
            => terms.ToArray() is { Length: > 0 } array
                ? TreeAnalyzer.MultiHangBinary(array, (a, b) => a * b)
                : Entity.Number.Integer.One;

        /// <summary>
        /// Converts an <see cref="IEnumerable"/> into a piecewise function
        /// </summary>
        /// <returns>A Piecewise node</returns>
        /// <example>
        /// The absolute value, written as two guarded branches:
        /// <code>
        /// var abs = new[]
        /// {
        ///     new Providedf("x", "x &gt; 0"),
        ///     new Providedf("-x", "x &lt;= 0")
        /// }.ToPiecewise();
        /// Console.WriteLine(abs);
        /// Console.WriteLine(abs.Substitute("x", -3).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// piecewise(x provided (x &gt; 0), (-x) provided (x &lt;= 0))
        /// 3
        /// </code>
        /// </example>
        public static Piecewise ToPiecewise(this IEnumerable<Providedf> cases)
            => new Piecewise(cases);

        /// <summary>
        /// Converts a tuple of an expression and its predicate to a 
        /// Provided node
        /// </summary>
        /// <returns>Providedf node</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine(((Entity)"1 / x", (Entity)"x &lt;&gt; 0").ToProvided());
        /// </code>
        /// Prints
        /// <code>
        /// 1 / x provided not x = 0
        /// </code>
        /// </example>
        public static Providedf ToProvided(this (Entity expr, Entity pred) @this)
            => new Providedf(@this.expr, @this.pred);

        /// <summary>
        /// Converts your <see cref="IEnumerable"/> into a set of unique values.
        /// </summary>
        /// <returns>A Set</returns>
        /// <example>
        /// Elements are deduplicated by the expression they are, not by the value they
        /// have, so two ways of writing the same number both survive:
        /// <code>
        /// Console.WriteLine(new Entity[] { 1, 2, 2, "x", "x" }.ToSet());
        /// Console.WriteLine(new Entity[] { "x + x", "2x" }.ToSet());
        /// </code>
        /// Prints
        /// <code>
        /// { 1, 2, x }
        /// { x + x, 2 * x }
        /// </code>
        /// </example>
        public static FiniteSet ToSet(this IEnumerable<Entity> elements)
            => new FiniteSet(elements);

        /// <summary>
        /// Unites your <see cref="IEnumerable"/> into one <see cref="Set"/>.
        /// Applies the "or" operator on those nodes
        /// </summary>
        /// <returns>A set of unique elements</returns>
        /// <example>
        /// The union node is built but not collapsed; <see cref="Entity.Simplify"/> collapses it:
        /// <code>
        /// var union = new Set[] { "[0; 1]", "[1; 2]" }.Unite();
        /// Console.WriteLine(union);
        /// Console.WriteLine(union.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// [0; 1] \/ [1; 2]
        /// [0; 2]
        /// </code>
        /// </example>
        public static Set Unite(this IEnumerable<Set> sets)
            => sets.Any() ? sets.Aggregate((a, b) => MathS.Union(a, b)) : Empty;

        /// <summary>
        /// Computes the intersection of your <see cref="IEnumerable"/>'s and makes it one <see cref="Set"/>.
        /// Applies the "and" operator on those nodes
        /// </summary>
        /// <returns>A set of unique elements</returns>
        /// <example>
        /// An empty sequence gives the empty set rather than the universe, so the identity of
        /// this fold is not the mathematical one:
        /// <code>
        /// var meet = new Set[] { "{ 1, 2 }", "{ 2, 3 }" }.Intersect();
        /// Console.WriteLine(meet);
        /// Console.WriteLine(meet.Simplify());
        /// Console.WriteLine(new Set[] { }.Intersect());
        /// </code>
        /// Prints
        /// <code>
        /// { 1, 2 } /\ { 2, 3 }
        /// { 2 }
        /// {  }
        /// </code>
        /// </example>
        public static Set Intersect(this IEnumerable<Set> sets)
            => sets.Any() ? sets.Aggregate((a, b) => MathS.Intersection(a, b)) : Empty;

        /// <summary>
        /// Parses the expression into <see cref="Entity"/>.
        /// Synonymical to <see cref="MathS.FromString(string)"/>
        /// </summary>
        /// <returns>Expression</returns>
        /// <example>
        /// Parsing builds the tree and computes nothing; note also that juxtaposition of a
        /// variable and a number is a power, so <c>x2</c> is <c>x ^ 2</c>:
        /// <code>
        /// Console.WriteLine("2 + 3".ToEntity());
        /// Console.WriteLine("2 + 3".ToEntity().Evaled);
        /// Console.WriteLine("x2".ToEntity());
        /// </code>
        /// Prints
        /// <code>
        /// 2 + 3
        /// 5
        /// x ^ 2
        /// </code>
        /// </example>
        public static Entity ToEntity(this string expr) => MathS.FromString(expr);

        /// <summary>
        /// Takes a tuple of four and builds an interval
        /// </summary>
        /// <example>
        /// The two flags say whether each end belongs to the interval:
        /// <code>
        /// var iv = ((Entity)0, false, (Entity)"pi", true).ToEntity();
        /// Console.WriteLine(iv);
        /// Console.WriteLine(iv.Contains(0));
        /// Console.WriteLine(iv.Contains("pi"));
        /// </code>
        /// Prints
        /// <code>
        /// (0; pi]
        /// False
        /// True
        /// </code>
        /// </example>
        public static Interval ToEntity(this (Entity left, bool leftClosed, Entity right, bool rightClosed) arg)
            => new Interval(arg.left, arg.leftClosed, arg.right, arg.rightClosed);

        /// <summary>
        /// Parses this and simplifies by running <see cref="Entity.Simplify"/>
        /// </summary>
        /// <returns>Simplified expression</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("sin(x) ^ 2 + cos(x) ^ 2".Simplify());
        /// Console.WriteLine("(x + 1) ^ 2 - (x - 1) ^ 2".Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 1
        /// 4 * x
        /// </code>
        /// </example>
        public static Entity Simplify(this string expr) => expr.ToEntity().Simplify();

        /// <summary>
        /// Parses this and simplifies by running <see cref="Entity.Simplify"/>
        /// </summary>
        /// <returns>Simplified expression</returns>
        /// <example>
        /// The level is how many rewriting rounds are allowed, and level 0 is none of them,
        /// so it hands the parsed expression straight back:
        /// <code>
        /// Console.WriteLine("a / (b / c)".Simplify(0));
        /// Console.WriteLine("a / (b / c)".Simplify(1));
        /// </code>
        /// Prints
        /// <code>
        /// a / (b / c)
        /// a * c / b
        /// </code>
        /// </example>
        public static Entity Simplify(this string expr, int level) => expr.ToEntity().Simplify(level);

        /// <summary>
        /// Parses this and evals into a number by running <see cref="Entity.EvalNumerical"/>
        /// </summary>
        /// <exception cref="CannotEvalException">
        /// This thrown when the given expression is boolean, tensoric, or contains variables.
        /// First, check whether it can be evaled: <see cref="Entity.EvaluableNumerical"/>
        /// </exception>
        /// <returns>Collapses into one expression</returns>
        /// <example>
        /// Integers stay exact however large they get, and a real answer is not forced where
        /// the value is complex:
        /// <code>
        /// Console.WriteLine("2 ^ 100".EvalNumerical());
        /// Console.WriteLine("sqrt(-1)".EvalNumerical());
        /// </code>
        /// Prints
        /// <code>
        /// 1267650600228229401496703205376
        /// i
        /// </code>
        /// </example>
        public static Complex EvalNumerical(this string expr) => expr.ToEntity().EvalNumerical();

        /// <summary>
        /// Parses this and evals into a boolean by running <see cref="Entity.EvalBoolean"/>
        /// </summary>
        /// <exception cref="CannotEvalException">
        /// This thrown when the given expression is numerical, tensoric, or contains variables.
        /// First, check whether it can be evaled: <see cref="Entity.EvaluableBoolean"/>
        /// </exception>
        /// <returns>Collapses into one expression</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("true implies false".EvalBoolean());
        /// Console.WriteLine("3 &gt; 2 and 2 &gt; 1".EvalBoolean());
        /// </code>
        /// Prints
        /// <code>
        /// False
        /// True
        /// </code>
        /// </example>
        public static Boolean EvalBoolean(this string expr) => expr.ToEntity().EvalBoolean();

        /// <summary>
        /// Parses and expands the given expression so that as many parentheses as possible
        /// get expanded into a linear expression.
        /// </summary>
        /// <returns>An expanded expression</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("(x + 1) ^ 3".Expand());
        /// Console.WriteLine("(x + 1)(x + 2)".Expand());
        /// </code>
        /// Prints
        /// <code>
        /// 1 + 3 * x + 3 * x ^ 2 + x ^ 3
        /// x ^ 2 + 3 * x + 2
        /// </code>
        /// </example>
        public static Entity Expand(this string expr) => expr.ToEntity().Expand();

        /// <summary>
        /// Parses and factorizes the given expression so that as few powers as possible remain,
        /// and the expression is represented as a product of multipliers
        /// </summary>
        /// <returns>A factorized expression</returns>
        /// <example>
        /// Where no factorization is found the expression comes back unchanged, which is how
        /// this says "no answer" rather than "irreducible":
        /// <code>
        /// Console.WriteLine("a2 - b2".Factorize());
        /// Console.WriteLine("x2 + 2x + 1".Factorize());
        /// </code>
        /// Prints
        /// <code>
        /// (a - b) * (a + b)
        /// x ^ 2 + 2 * x + 1
        /// </code>
        /// </example>
        public static Entity Factorize(this string expr) => expr.ToEntity().Factorize();

        /// <summary>
        /// Parses the given expression and writes it as a single fraction: one numerator over one
        /// denominator, nothing cancelled. See <see cref="Entity.AsSingleFraction"/>.
        /// </summary>
        /// <returns>The expression as one fraction, or unchanged where it has no division in it</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("a + b/c".AsSingleFraction());
        /// </code>
        /// Prints
        /// <code>
        /// (a * c + b) / c
        /// </code>
        /// </example>
        public static Entity AsSingleFraction(this string expr) => expr.ToEntity().AsSingleFraction();

        /// <summary>
        /// Subsitutes a variable by replacing all its occurances with the given value
        /// </summary>
        /// <param name="expr">The expression where to substitute the variables</param>
        /// <param name="var">A variable to substitute</param>
        /// <param name="value">A value to substitute <paramref name="var"/></param>
        /// <returns>Expression with substituted the variable</returns>
        /// <example>
        /// Substitution replaces nodes and computes nothing:
        /// <code>
        /// Console.WriteLine("x ^ 2 + x".Substitute("x", 3));
        /// Console.WriteLine("x ^ 2 + x".Substitute("x", 3).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 3 ^ 2 + 3
        /// 12
        /// </code>
        /// </example>
        public static Entity Substitute(this string expr, Variable var, Entity value)
            => expr.ToEntity().Substitute(var, value);

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2
        /// </summary>
        /// <example>
        /// The replacements happen one after another and each sees the result of the last, so
        /// this does not swap two variables — the first substitution puts <c>y</c> where
        /// <c>x</c> was, and the second then rewrites every <c>y</c>:
        /// <code>
        /// Console.WriteLine("x - y".Substitute(("x", "y"), ("y", "x")));
        /// </code>
        /// Prints
        /// <code>
        /// x - x
        /// </code>
        /// </example>
        public static Entity Substitute(this string expr, (Entity x1, Entity x2) x, (Entity v1, Entity v2) value)
            => expr.ToEntity().Substitute(x.x1, value.v1).Substitute(x.x2, value.v2);

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2 and
        /// x.x3 with value.v3
        /// </summary>
        /// <example>
        /// Instantiating the coefficients of a quadratic and then solving it:
        /// <code>
        /// var quadratic = "a x2 + b x + c".Substitute(("a", "b", "c"), (1, -3, 2));
        /// Console.WriteLine(quadratic);
        /// Console.WriteLine(quadratic.SolveEquation("x"));
        /// </code>
        /// Prints
        /// <code>
        /// 1 * x ^ 2 + (-3) * x + 2
        /// { 1, 2 }
        /// </code>
        /// </example>
        public static Entity Substitute(this string expr, (Entity x1, Entity x2, Entity x3) x, (Entity v1, Entity v2, Entity v3) value)
            => expr.ToEntity().Substitute(x.x1, value.v1).Substitute(x.x2, value.v2).Substitute(x.x3, value.v3);

        /// <summary>
        /// Replaces x.x1 with value.v1 and
        /// x.x2 with value.v2 and
        /// x.x3 with value.v3 and
        /// x.x4 with value.v4
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine("a + b + c + d".Substitute(("a", "b", "c", "d"), (1, 2, 3, 4)));
        /// Console.WriteLine("a + b + c + d".Substitute(("a", "b", "c", "d"), (1, 2, 3, 4)).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 1 + 2 + 3 + 4
        /// 10
        /// </code>
        /// </example>
        public static Entity Substitute(this string expr, (Entity x1, Entity x2, Entity x3, Entity x4) x, (Entity v1, Entity v2, Entity v3, Entity v4) value)
            => expr.ToEntity().Substitute(x.x1, value.v1).Substitute(x.x2, value.v2).Substitute(x.x3, value.v3).Substitute(x.x4, value.v4);

        /// <summary>
        /// Solves the given equation
        /// </summary>
        /// <param name="expr">The function of <paramref name="x"/> that is assumed to be 0</param>
        /// <param name="x">The variable to solve over</param>
        /// <returns>A <see cref="Set"/> of roots</returns>
        /// <example>
        /// The expression is what is set to zero, so this asks for the cube roots of unity and
        /// gets all three of them rather than only the real one:
        /// <code>
        /// Console.WriteLine("x ^ 3 - 1".SolveEquation("x"));
        /// </code>
        /// Prints
        /// <code>
        /// { 1, -1/2 + i * 1/2 * sqrt(3), -1/2 + i * -1/2 * sqrt(3) }
        /// </code>
        /// </example>
        public static Set SolveEquation(this string expr, Variable x)
            => expr.ToEntity().SolveEquation(x);

        /// <summary>
        /// Solves the statement. The given expression must be boolean type,
        /// for example, equality, or boolean operators.
        /// </summary>
        /// <param name="expr">The statement of <paramref name="var"/> that is assumed to be true</param>
        /// <param name="var">The variables over which to solve</param>
        /// <returns>A <see cref="Set"/> of roots</returns>
        /// <example>
        /// Unlike <see cref="SolveEquation(string, Variable)"/> this takes a statement, so it
        /// also answers inequalities, and then the solution set need not be finite:
        /// <code>
        /// Console.WriteLine("x2 = 4".Solve("x"));
        /// Console.WriteLine("x &gt; 1 and x &lt; 3".Solve("x"));
        /// </code>
        /// Prints
        /// <code>
        /// { 2, -2 }
        /// (1; 3)
        /// </code>
        /// </example>
        public static Set Solve(this string expr, Variable var)
            => expr.ToEntity().Solve(var);

        /// <summary>
        /// Converts an <see cref="int"/> into an AM's understandable <see cref="Integer"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Integer"/></returns>
        /// <example>
        /// Arithmetic on the result is exact, so a quotient of two integers is a rational
        /// rather than a rounded or floating result:
        /// <code>
        /// Console.WriteLine(5.ToNumber() / 2.ToNumber());
        /// Console.WriteLine((5.ToNumber() / 2.ToNumber()).GetType().Name);
        /// </code>
        /// Prints
        /// <code>
        /// 5/2
        /// Rational
        /// </code>
        /// </example>
        public static Integer ToNumber(this int value) => Integer.Create(value);

        /// <summary>
        /// Converts an <see cref="long"/> into an AM's understandable <see cref="Integer"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Integer"/></returns>
        /// <example>
        /// Once converted the value no longer overflows, so a product that does not fit into a
        /// <see cref="long"/> is still exact:
        /// <code>
        /// Console.WriteLine(long.MaxValue.ToNumber());
        /// Console.WriteLine((long.MaxValue.ToNumber() * long.MaxValue.ToNumber()).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 9223372036854775807
        /// 85070591730234615847396907784232501249
        /// </code>
        /// </example>
        public static Integer ToNumber(this long value) => Integer.Create(value);

        /// <summary>
        /// Converts PeterO's <see cref="EInteger"/> into an AM's understandable <see cref="Integer"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Integer"/></returns>
        /// <example>
        /// <code>
        /// Console.WriteLine(EInteger.FromString("2").Pow(128).ToNumber());
        /// </code>
        /// Prints
        /// <code>
        /// 340282366920938463463374607431768211456
        /// </code>
        /// </example>
        public static Integer ToNumber(this EInteger value) => Integer.Create(value);

        /// <summary>
        /// Converts an <see cref="float"/> into an AM's understandable <see cref="Real"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Real"/></returns>
        /// <example>
        /// The conversion is exact, which means it keeps the value the <see cref="float"/>
        /// actually holds rather than the decimal literal it was written as:
        /// <code>
        /// Console.WriteLine(0.1f.ToNumber());
        /// </code>
        /// Prints
        /// <code>
        /// 0.100000001490116119384765625
        /// </code>
        /// </example>
        public static Real ToNumber(this float value) => Real.Create(EDecimal.FromSingle(value));

        /// <summary>
        /// Converts an <see cref="double"/> into an AM's understandable <see cref="Real"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Real"/></returns>
        /// <example>
        /// As for <see cref="float"/>, the binary value is kept exactly; a value that a
        /// <see cref="double"/> does represent exactly comes back as a rational:
        /// <code>
        /// Console.WriteLine(0.1.ToNumber());
        /// Console.WriteLine(0.5.ToNumber());
        /// </code>
        /// Prints
        /// <code>
        /// 0.1000000000000000055511151231257827021181583404541015625
        /// 1/2
        /// </code>
        /// </example>
        public static Real ToNumber(this double value) => Real.Create(EDecimal.FromDouble(value));

        /// <summary>
        /// Converts an <see cref="decimal"/> into an AM's understandable <see cref="Real"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Real"/></returns>
        /// <example>
        /// <see cref="decimal"/> is a decimal type, so a decimal literal survives it exactly
        /// where the same literal as a <see cref="double"/> does not:
        /// <code>
        /// Console.WriteLine(0.1m.ToNumber());
        /// Console.WriteLine(0.1.ToNumber());
        /// </code>
        /// Prints
        /// <code>
        /// 1/10
        /// 0.1000000000000000055511151231257827021181583404541015625
        /// </code>
        /// </example>
        public static Real ToNumber(this decimal value) => Real.Create(EDecimal.FromDecimal(value));

        /// <summary>
        /// Converts PeterO's <see cref="EDecimal"/> into an AM's understandable <see cref="Real"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Real"/></returns>
        /// <example>
        /// <code>
        /// Console.WriteLine(EDecimal.FromString("0.1").ToNumber());
        /// </code>
        /// Prints
        /// <code>
        /// 1/10
        /// </code>
        /// </example>
        public static Real ToNumber(this EDecimal value) => Real.Create(value);

        /// <summary>
        /// Converts Numerics's <see cref="NumericsComplex"/> into an AM's understandable <see cref="Complex"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Complex"/></returns>
        /// <example>
        /// <code>
        /// var z = new System.Numerics.Complex(3, 4).ToNumber();
        /// Console.WriteLine(z);
        /// Console.WriteLine(z.Abs().Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 3 + 4i
        /// 5
        /// </code>
        /// </example>
        public static Complex ToNumber(this NumericsComplex complex)
            => Complex.Create(EDecimal.FromDouble(complex.Real), EDecimal.FromDouble(complex.Imaginary));

        /// <summary>
        /// Converts an <see cref="bool"/> into an AM's understandable <see cref="Boolean"/>
        /// which can be hung with others
        /// </summary>
        /// <returns>AM's <see cref="Boolean"/></returns>
        /// <example>
        /// The operators build a boolean expression rather than deciding it, so the answer is
        /// a node until it is evaluated:
        /// <code>
        /// Console.WriteLine(true.ToBoolean() &amp; false.ToBoolean());
        /// Console.WriteLine((true.ToBoolean() &amp; false.ToBoolean()).EvalBoolean());
        /// </code>
        /// Prints
        /// <code>
        /// True and False
        /// False
        /// </code>
        /// </example>
        public static Boolean ToBoolean(this bool value) => Boolean.Create(value);
        
        /// <summary>
        /// Builds a LaTeX code from an expression
        /// </summary>
        /// <returns>A <see cref="string"/> which can be rendered into pretty output</returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("sqrt(x) / 2".Latexize());
        /// Console.WriteLine("integral(x ^ 2, x)".Latexize());
        /// </code>
        /// Prints
        /// <code>
        /// \frac{\sqrt{x}}{2}
        /// \int {x}^{2}\,\mathrm{d}x
        /// </code>
        /// </example>
        public static string Latexize(this string str) => str.ToEntity().Latexize();

        /// <summary>
        /// Compiles an expression into a special compiled code that runs via
        /// AM's virtual machine. Soon will be deprecated and replaced with compilation to
        /// delegate
        /// </summary>
        /// <param name="str">From which function to compile</param>
        /// <param name="variables">The array of variables should cover all variables from the expression</param>
        /// <returns>A compiled expression</returns>
        /// <example>
        /// The compiled function evaluates over the complex plane, so the same code answers at
        /// a root of the expression that is not real:
        /// <code>
        /// var f = "x ^ 2 + 1".Compile("x");
        /// Console.WriteLine(f.Call(3).Real);
        /// Console.WriteLine(f.Call(new System.Numerics.Complex(0, 1)).Real);
        /// </code>
        /// Prints
        /// <code>
        /// 10
        /// 0
        /// </code>
        /// </example>
        public static FastExpression Compile(this string str, params Variable[] variables)
            => str.ToEntity().Compile(variables);



        /// <summary>
        /// Finds the symbolical derivative of the given expression
        /// </summary>
        /// <param name="str">
        /// The expression to be parsed and differentiated
        /// </param>
        /// <param name="x">
        /// Over which variable to find the derivative
        /// </param>
        /// <returns>
        /// The derived expression which might contain <see cref="Derivativef"/> nodes,
        /// or the initial one
        /// </returns>
        /// <example>
        /// <code>
        /// Console.WriteLine("x ^ 3".Differentiate("x"));
        /// Console.WriteLine("sin(x) / x".Differentiate("x"));
        /// </code>
        /// Prints
        /// <code>
        /// 3 * x ^ 2
        /// (cos(x) * x - sin(x)) / x ^ 2
        /// </code>
        /// </example>
        public static Entity Differentiate(this string str, Variable x)
            => str.ToEntity().Differentiate(x);

        /// <summary>
        /// Integrates indefinitely the given expression over the `x` variable, if can.
        /// May return an unresolved <see cref="Integralf"/> node.
        /// </summary>
        /// <param name="str">
        /// The expression to be parsed and integrated
        /// </param>
        /// <param name="x">Over which variable to integrate</param>
        /// <returns>
        /// An integrated expression. It might remain the same or be transformed into nodes with no integrals.
        /// </returns>
        /// <example>
        /// The constant of integration is carried as the variable <c>C</c>; an integral with no
        /// antiderivative written in the library's functions comes back as the node itself,
        /// which is how this says it could not settle the question:
        /// <code>
        /// Console.WriteLine("1 / x".Integrate("x"));
        /// Console.WriteLine("e ^ (x ^ 2)".Integrate("x"));
        /// Console.WriteLine("x ^ x".Integrate("x"));
        /// </code>
        /// Prints
        /// <code>
        /// ln(x) + C
        /// sqrt(pi) / 2 * erfi(x) + C
        /// integral(x ^ x, x)
        /// </code>
        /// </example>
        public static Entity Integrate(this string str, Variable x)
            => str.ToEntity().Integrate(x);

        /// <summary>
        /// Integrates definitely the given expression over the `x` variable, if can.
        /// May return an unresolved <see cref="Integralf"/> node.
        /// </summary>
        /// <param name="str">
        /// The expression to be parsed and integrated
        /// </param>
        /// <param name="x">Over which variable to integrate</param>
        /// <param name="from">The lower bound for integrating</param>
        /// <param name="to">The upper bound for integrating</param>
        /// <returns>
        /// An integrated expression. It might remain the same or be transformed into nodes with no integrals.
        /// </returns>
        /// <example>
        /// The bounds are substituted into the antiderivative and the difference is left
        /// standing, so the number wanted usually takes one more step:
        /// <code>
        /// Console.WriteLine("sin(x)".Integrate("x", 0, "pi"));
        /// Console.WriteLine("sin(x)".Integrate("x", 0, "pi").Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// -cos(pi) - -cos(0)
        /// 2
        /// </code>
        /// </example>
        public static Entity Integrate(this string str, Variable x, Entity from, Entity to)
            => str.ToEntity().Integrate(x, from, to);

        /// <summary>
        /// Finds the limit of the given expression over the given variable
        /// </summary>
        /// <param name="str">
        /// The expression to be parsed and whose limit to be computed
        /// </param>
        /// <param name="x">
        /// The variable to be approaching
        /// </param>
        /// <param name="destination">
        /// A value where the variable approaches. It might be a symbolic
        /// expression, a finite number, or an infinite number, for example,
        /// "sqrt(x2 + x) / (3x + 3)".Limit("x", "+oo", ApproachFrom.BothSides)
        /// </param>
        /// <param name="side">
        /// From where to approach it: from the left, from the right,
        /// or BothSides, implying that if limits from either are not
        /// equal, there is no limit
        /// </param>
        /// <returns>
        /// A result or the <see cref="Limitf"/> node if the limit
        /// cannot be determined
        /// </returns>
        /// <example>
        /// The two one-sided limits of <c>1 / x</c> at zero differ, so the two-sided one does
        /// not exist, and NaN is the claim that it does not exist rather than that it was not
        /// computed:
        /// <code>
        /// Console.WriteLine("1 / x".Limit("x", 0, ApproachFrom.Left));
        /// Console.WriteLine("1 / x".Limit("x", 0, ApproachFrom.Right));
        /// Console.WriteLine("1 / x".Limit("x", 0, ApproachFrom.BothSides));
        /// </code>
        /// Prints
        /// <code>
        /// -oo
        /// +oo
        /// NaN
        /// </code>
        /// </example>
        public static Entity Limit(this string str, Variable x, Entity destination, ApproachFrom side)
            => str.ToEntity().Limit(x, destination, side);

        /// <summary>
        /// Finds the limit of the given expression over the given variable
        /// </summary>
        /// <param name="str">The expression to be parsed and whose limit to be found</param>
        /// <param name="x">
        /// The variable to be approaching
        /// </param>
        /// <param name="destination">
        /// A value where the variable approaches. It might be a symbolic
        /// expression, a finite number, or an infinite number, for example,
        /// "sqrt(x2 + x) / (3x + 3)".Limit("x", "+oo")
        /// </param>
        /// <returns>
        /// A result or the <see cref="Limitf"/> node if the limit
        /// cannot be determined
        /// </returns>
        /// <example>
        /// The destination may be infinite, which is how the two classical limits below are
        /// asked for:
        /// <code>
        /// Console.WriteLine("sin(x) / x".Limit("x", 0));
        /// Console.WriteLine("(1 + 1/x) ^ x".Limit("x", "+oo"));
        /// </code>
        /// Prints
        /// <code>
        /// 1
        /// e
        /// </code>
        /// </example>
        public static Entity Limit(this string str, Variable x, Entity destination)
            => str.ToEntity().Limit(x, destination);
    }
}
