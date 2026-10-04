//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Runtime.CompilerServices;
using PeterO.Numbers;
using AngouriMath.Functions.Algebra;
using AngouriMath.Functions.Boolean;
using System.Diagnostics.CodeAnalysis;
using AngouriMath.Convenience;
using AngouriMath.Core.Multithreading;
using System.Threading;
using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Budgets;

namespace AngouriMath
{
    using static Entity;
    using static Entity.Number;
    using NumericsComplex = System.Numerics.Complex;
    using GenTensor = GenericTensor.Core.GenTensor<Entity, Entity.Matrix.EntityTensorWrapperOperations>;
    using static Entity.Set;
    using static AngouriMath.MathS.UnsafeAndInternal;

    /// <summary>Use functions from this class</summary>
    public static partial class MathS
    {
        /// <summary>Use it in order to explore further number theory</summary>
        public static class NumberTheory
        {
            /// <summary>
            /// Returns entity standing for Euler phi function
            /// <a href="https://en.wikipedia.org/wiki/Euler%27s_totient_function" >Wikipedia</a>
            /// If integer x is non-positive, the result will be 0
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.Phi(12));
            /// Console.WriteLine(MathS.NumberTheory.Phi(12).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// phi(12)
            /// 4
            /// </code>
            /// </example>
            public static Entity Phi(Entity integer) => new Phif(integer);

            /// <summary>
            /// The <c>n</c>-th prime, <c>prime(n)</c>: <c>prime(1)</c> is <c>2</c> and
            /// <c>prime(25)</c> is <c>97</c>. Defined on the positive whole numbers, and a
            /// function rather than an operation on the set <c>PP</c>, which carries no order.
            /// https://github.com/asc-community/AngouriMath/issues/1450
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.Prime(25).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// 97
            /// </code>
            /// </example>
            public static Entity Prime(Entity index) => new Primef(index);

            /// <summary>
            /// The <c>p</c>-adic valuation of a whole number, <c>valuation(n, p)</c>: the exponent
            /// of the prime <c>p</c> in <c>n</c>. <c>valuation(12, 2)</c> is <c>2</c>,
            /// <c>valuation(12, 5)</c> is <c>0</c>, and at <c>n = 0</c> it is <c>+oo</c>.
            /// https://github.com/asc-community/AngouriMath/issues/1450
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.Valuation(12, 2).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// 2
            /// </code>
            /// </example>
            public static Entity Valuation(Entity argument, Entity prime) => new Valuationf(argument, prime);

            /// <summary>
            /// The statement that <paramref name="divisor"/> divides <paramref name="dividend"/>,
            /// written <c>a divides b</c>: for integers, that <c>b mod a = 0</c>.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.Divides(3, 12).Evaled);
            /// Console.WriteLine(MathS.NumberTheory.Divides(5, 12).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// True
            /// False
            /// </code>
            /// </example>
            public static Entity Divides(Entity divisor, Entity dividend) => new Dividesf(divisor, dividend);

            /// <summary>
            /// The statement that two integers are congruent modulo a third: <c>a = b (mod n)</c>,
            /// which is <c>n divides a - b</c>. A relation, not the remainder operator <c>a mod n</c>.
            /// https://github.com/asc-community/AngouriMath/issues/1409
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.Congruent(15625, 1, 7).Evaled);
            /// Console.WriteLine("-1 = 9 (mod 10)".ToEntity().Evaled);
            /// Console.WriteLine("(n - a)^2 = a^2 (mod n)".ToEntity().Simplify());
            /// </code>
            /// Prints
            /// <code>
            /// True
            /// True
            /// True
            /// </code>
            /// </example>
            public static Entity Congruent(Entity left, Entity right, Entity modulus) => new Congruentf(left, right, modulus);

            /// <summary>
            /// The multiplicative inverse of <paramref name="a"/> modulo <paramref name="n"/>:
            /// the one representative in <c>[1, n - 1]</c> of the class of <c>x</c> with
            /// <c>a x = 1 (mod n)</c>, or <see langword="null"/> where there is none, which is
            /// exactly when <c>a</c> and <c>n</c> share a factor. A class, never the number
            /// <c>1/a</c>. https://github.com/asc-community/AngouriMath/issues/1409
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.ModularInverse(3, 10));
            /// Console.WriteLine(MathS.NumberTheory.ModularInverse(6, 15) is null);
            /// </code>
            /// Prints
            /// <code>
            /// 7
            /// True
            /// </code>
            /// </example>
            public static Integer? ModularInverse(Integer a, Integer n)
                => Functions.ResidueClasses.Inverse(a.EInteger, n.EInteger) is { } inverse ? Integer.Create(inverse) : null;

            /// <summary>
            /// Count of all divisors of an integer, including 1 or itself.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.CountDivisors(12));
            /// Console.WriteLine(MathS.NumberTheory.CountDivisors(13));
            /// </code>
            /// Prints
            /// <code>
            /// 6
            /// 2
            /// </code>
            /// </example>
            public static Integer CountDivisors(Integer integer) => integer.CountDivisors();

            /// <summary>
            /// Factorization of integer. Returns a sequence of unique primes
            /// and their corresponding power. Sorted by value of prime (starting
            /// from the smallest).
            /// </summary>
            /// <example>
            /// <code>
            /// var factors = MathS.NumberTheory.Factorize(1872);
            /// foreach (var (prime, power) in factors)
            ///     Console.WriteLine($"{prime} ^ {power}");
            /// </code>
            /// Prints
            /// <code>
            /// 2 ^ 4
            /// 3 ^ 2
            /// 13 ^ 1
            /// </code>
            /// </example>
            public static IEnumerable<(Integer prime, Integer power)> Factorize(Integer integer) => integer.Factorize();

            /// <summary>
            /// Finds the greatest common divisor
            /// of two integers
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.NumberTheory.GreatestCommonDivisor(1872, 169));
            /// </code>
            /// Prints
            /// <code>
            /// 13
            /// </code>
            /// </example>
            public static Integer GreatestCommonDivisor(Integer a, Integer b)
                => a.EInteger.Gcd(b.EInteger);

        }

        /// <summary>Use it to solve systems of equations</summary>
        /// <param name="equations">
        /// An array of <see cref="Entity"/> (or <see cref="string"/>s)
        /// the system consists of
        /// </param>
        /// <example>
        /// <code>
        /// var system = MathS.Equations(
        ///     "a + b",
        ///     "a^2 - b + c"
        /// );
        /// var solutions = system.Solve("a", "b");
        /// Console.WriteLine(solutions.ToString(multilineFormat: true));
        /// <br/>
        /// Console.WriteLine();
        /// <br/>
        /// for (int i = 0; i &lt; solutions.RowCount; i++)
        /// {
        ///     var (a, b) = (solutions[i, 0], solutions[i, 1]);
        ///     Console.WriteLine($"Solution #{i}: a = {a}, b = {b}");
        /// }
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// (-1 - sqrt(1 - 4 * c)) / 2    -(-1 - sqrt(1 - 4 * c)) / 2   
        /// (-1 + sqrt(1 - 4 * c)) / 2    -(-1 + sqrt(1 - 4 * c)) / 2   
        /// <br/>
        /// Solution #0: a = (-1 - sqrt(1 - 4 * c)) / 2, b = -(-1 - sqrt(1 - 4 * c)) / 2
        /// Solution #1: a = (-1 + sqrt(1 - 4 * c)) / 2, b = -(-1 + sqrt(1 - 4 * c)) / 2
        /// </code>
        /// </example>
        /// <returns>An <see cref="EquationSystem"/> which can then be solved</returns>
        public static EquationSystem Equations(params Entity[] equations) => new EquationSystem(equations);

        /// <summary>Use it to solve systems of equations</summary>
        /// <param name="equations">
        /// A sequence of <see cref="Entity"/> the system consists of
        /// </param>
        /// <example>
        /// <code>
        /// var equations = LList.Of&lt;Entity&gt;(
        ///     "a + b",
        ///     "a^2 - b + c"
        /// );
        /// var system = MathS.Equations(equations);
        /// var solutions = system.Solve("a", "b");
        /// Console.WriteLine(solutions.ToString(multilineFormat: true));
        /// <br/>
        /// Console.WriteLine();
        /// <br/>
        /// for (int i = 0; i &lt; solutions.RowCount; i++)
        /// {
        ///     var (a, b) = (solutions[i, 0], solutions[i, 1]);
        ///     Console.WriteLine($"Solution #{i}: a = {a}, b = {b}");
        /// }
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// (-1 - sqrt(1 - 4 * c)) / 2    -(-1 - sqrt(1 - 4 * c)) / 2   
        /// (-1 + sqrt(1 - 4 * c)) / 2    -(-1 + sqrt(1 - 4 * c)) / 2   
        /// <br/>
        /// Solution #0: a = (-1 - sqrt(1 - 4 * c)) / 2, b = -(-1 - sqrt(1 - 4 * c)) / 2
        /// Solution #1: a = (-1 + sqrt(1 - 4 * c)) / 2, b = -(-1 + sqrt(1 - 4 * c)) / 2
        /// </code>
        /// </example>
        /// <returns>An <see cref="EquationSystem"/> which can then be solved</returns>
        public static EquationSystem Equations(IEnumerable<Entity> equations) => new EquationSystem(equations);

        /// <summary>Solves one equation over one variable</summary>
        /// <param name="equation">An equation that is assumed to equal 0</param>
        /// <param name="var">Variable whose values we are looking for</param>
        /// <returns>A <see cref="Set"/> of possible values or intervals of values</returns>
        /// <example>
        /// <code>
        /// Entity eq1 = "x^2 - 1";
        /// Console.WriteLine(MathS.SolveEquation(eq1, "x"));
        /// 
        /// Entity eq2 = "sin(a u) - cos(a u)";
        /// Console.WriteLine(MathS.SolveEquation(eq2, "u"));
        /// </code>
        /// Prints
        /// { 1, -1 }
        /// { ln(-sqrt(-2) / (-1 - i)) / i / a, ln(sqrt(-2) / (-1 - i)) / i / a }
        /// </example>
        public static Set SolveEquation(Entity equation, Variable var) => EquationSolver.Solve(equation, var);

        /// <summary>
        /// Solves a first-order linear ordinary differential equation
        /// <a href="https://en.wikipedia.org/wiki/Linear_differential_equation"/> by its
        /// integrating factor, or returns <see langword="null"/> where it cannot.
        /// </summary>
        /// <param name="equation">
        /// The equation, read as equal to zero, written in terms of the unknown function applied
        /// to its variable. The unknown has to be an application — <c>apply(y, x)</c> — rather
        /// than a bare variable, because <c>derivative(y, x)</c> is <c>0</c>: a variable does not
        /// depend on <c>x</c>, and the library is right about that.
        /// </param>
        /// <param name="function">The name of the unknown function, <c>y</c> above.</param>
        /// <param name="variable">The name it is a function of, <c>x</c> above.</param>
        /// <returns>
        /// The general solution, carrying one arbitrary constant, or <see langword="null"/> where
        /// the equation is not first-order linear in the unknown, or where either of the two
        /// integrals the method needs has no closed form.
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using AngouriMath.Extensions;
        ///
        /// // y' + y = 1
        /// var equation = "derivative(apply(y, x), x) + apply(y, x) - 1".ToEntity();
        /// Console.WriteLine(MathS.SolveOde(equation, "y", "x"));
        /// </code>
        /// Prints
        /// <code>
        /// 1 + C_1 * e ^ (-x)
        /// </code>
        /// </example>
        /// <remarks>
        /// Declining is a legitimate answer here and a common one: the method is exact where it
        /// applies, and where either integral does not come out there is no approximation to
        /// offer in its place. <c>y' + y = e^(x^2)</c> is declined for exactly that reason.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/241">#241</a>
        /// </remarks>
        public static Entity? SolveOde(Entity equation, Variable function, Variable variable)
            => Functions.Algebra.OrdinaryDifferentialEquation
                .SolveFirstOrderLinear(equation, function, variable);

        /// <summary>
        /// Solves a boolean expression. That is, finds all values for
        /// <paramref name="variables"/> such that the expression turns into True when evaluated
        /// Uses a simple table of truth
        /// Use <see cref="Entity.SolveBoolean(Variable)"/> for smart solving
        /// </summary>
        /// <example>
        /// <code>
        /// Entity expr = "(a xor b or c) implies (a or c)";
        /// var sols = MathS.SolveBooleanTable(expr, "a", "b", "c");
        /// 
        /// if (sols is null)
        ///     Console.WriteLine("No solution found");
        /// else
        /// {
        ///     Console.WriteLine(sols.ToString(multilineFormat: true));
        ///     Console.WriteLine();
        ///     for (int i = 0; i &lt; sols.RowCount; i++)
        ///     {
        ///         var a = (bool)sols[i, 0].EvalBoolean();
        ///         var b = (bool)sols[i, 1].EvalBoolean();
        ///         var c = (bool)sols[i, 2].EvalBoolean();
        ///         Console.WriteLine($"Solution #{i}: {a}, {b}, {c}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public static Matrix? SolveBooleanTable(Entity expression, params Variable[] variables)
            => BooleanSolver.SolveTable(expression, variables);


        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of sine</param>
        /// <returns>Sine node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Sin("x").Pow(2) + Cos("x").Pow(2);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin(x) ^ 2 + cos(x) ^ 2
        /// 1
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Sin(Entity a) => new Sinf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of cosine</param>
        /// <returns>Cosine node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Sin("x").Pow(2) + Cos("x").Pow(2);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin(x) ^ 2 + cos(x) ^ 2
        /// 1
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Cos(Entity a) => new Cosf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of secant</param>
        /// <returns>Secant node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Sec("x") / Cosec("x");
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sec(x) / csc(x)
        /// tan(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Sec(Entity a) => new Secantf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of cosecant</param>
        /// <returns>Cosecant node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Sec("x") / Cosec("x");
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sec(x) / csc(x)
        /// tan(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Cosec(Entity a) => new Cosecantf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Logarithm"/></summary>
        /// <param name="base">Base node of logarithm</param>
        /// <param name="x">Argument node of logarithm</param>
        /// <returns>Logarithm node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Log(2, 16));
        /// Console.WriteLine(Log(2, 16).Evaled);
        /// Console.WriteLine(Log(3, 81));
        /// Console.WriteLine(Log(3, 81).Evaled);
        /// Console.WriteLine(Log(10, 1000));
        /// Console.WriteLine(Log(10, 1000).Evaled);
        /// Console.WriteLine(Log(1000));
        /// Console.WriteLine(Log(1000).Evaled);
        /// Console.WriteLine(Ln(e.Pow(16)));
        /// Console.WriteLine(Ln(e.Pow(16)).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// log(2, 16)
        /// 4
        /// log(3, 81)
        /// 4
        /// log(10, 1000)
        /// 3
        /// log(10, 1000)
        /// 3
        /// ln(e ^ 16)
        /// 16
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Log(Entity @base, Entity x) => new Logf(@base, x);

        /// <summary>
        /// This is 10-based logarithm.
        /// <a href="https://en.wikipedia.org/wiki/Logarithm"/></summary>
        /// <param name="x">Argument node of logarithm</param>
        /// <returns>Logarithm node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Log(2, 16));
        /// Console.WriteLine(Log(2, 16).Evaled);
        /// Console.WriteLine(Log(3, 81));
        /// Console.WriteLine(Log(3, 81).Evaled);
        /// Console.WriteLine(Log(10, 1000));
        /// Console.WriteLine(Log(10, 1000).Evaled);
        /// Console.WriteLine(Log(1000));
        /// Console.WriteLine(Log(1000).Evaled);
        /// Console.WriteLine(Ln(e.Pow(16)));
        /// Console.WriteLine(Ln(e.Pow(16)).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// log(2, 16)
        /// 4
        /// log(3, 81)
        /// 4
        /// log(10, 1000)
        /// 3
        /// log(10, 1000)
        /// 3
        /// ln(e ^ 16)
        /// 16
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Log(Entity x) => new Logf(10, x);

        /// <summary><a href="https://en.wikipedia.org/wiki/Power_function"/></summary>
        /// <param name="base">Base node of power</param>
        /// <param name="power">Argument node of power</param>
        /// <returns>Power node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Pow(2, 5));
        /// Console.WriteLine(Pow(2, 5).Simplify());
        /// Console.WriteLine(Pow(e, 2));
        /// Console.WriteLine(Pow(e, 2).Simplify());
        /// Console.WriteLine(Pow(e, 2).Evaled);
        /// Console.WriteLine(Sqrt(Sqr("x")));
        /// Console.WriteLine(Sqrt(Sqr("x")).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 3));
        /// Console.WriteLine(Pow(Cbrt("a"), 3).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 6));
        /// Console.WriteLine(Pow(Cbrt("a"), 6).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 2 ^ 5
        /// 32
        /// e ^ 2
        /// e ^ 2
        /// 7.389056098930650227230427460575007813180315570551847324087127822522573796079057763384312485079121792
        /// sqrt(x ^ 2)
        /// x
        /// a ^ (1/3) ^ 3
        /// a
        /// a ^ (1/3) ^ 6
        /// a ^ 2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Pow(Entity @base, Entity power) => new Powf(@base, power);

        /// <summary>
        /// The remainder after dividing one expression by another, that is, the node
        /// <paramref name="dividend"/> % <paramref name="divisor"/>.
        /// </summary>
        /// <remarks>
        /// The remainder takes the sign of the divisor -- the floored convention,
        /// <c>a - b * floor(a / b)</c>, under which <c>(-7) mod 3</c> is 2 and <c>7 mod (-3)</c>
        /// is -2. This is what a mathematician means by mod: it is the convention under which
        /// the residues modulo n are the numbers from 0 to n - 1, and it is what SymPy,
        /// Mathematica and Maxima all answer. C# truncates instead and so its <c>%</c> on two
        /// <see cref="int"/>s gives -1 there, but that is an operation on machine integers and
        /// not this one.
        /// <para/>
        /// Written <c>mod</c> when parsed from a string. <c>%</c> is left to mean percent.
        /// </remarks>
        /// <param name="dividend">The expression whose remainder is taken</param>
        /// <param name="divisor">The expression divided by; the result is undefined where it is zero</param>
        /// <returns>The node of the remainder</returns>
        public static Entity Mod(Entity dividend, Entity divisor) => new Modf(dividend, divisor);

        /// <summary>Special case of <a href="https://en.wikipedia.org/wiki/Power_function"/></summary>
        /// <param name="a">The argument of which square root will be taken</param>
        /// <returns>Power node with (1/2) as the power</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Pow(2, 5));
        /// Console.WriteLine(Pow(2, 5).Simplify());
        /// Console.WriteLine(Pow(e, 2));
        /// Console.WriteLine(Pow(e, 2).Simplify());
        /// Console.WriteLine(Pow(e, 2).Evaled);
        /// Console.WriteLine(Sqrt(Sqr("x")));
        /// Console.WriteLine(Sqrt(Sqr("x")).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 3));
        /// Console.WriteLine(Pow(Cbrt("a"), 3).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 6));
        /// Console.WriteLine(Pow(Cbrt("a"), 6).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 2 ^ 5
        /// 32
        /// e ^ 2
        /// e ^ 2
        /// 7.389056098930650227230427460575007813180315570551847324087127822522573796079057763384312485079121792
        /// sqrt(x ^ 2)
        /// x
        /// a ^ (1/3) ^ 3
        /// a
        /// a ^ (1/3) ^ 6
        /// a ^ 2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Sqrt(Entity a) => new Powf(a, Number.Rational.Create(1, 2));

        /// <summary>Special case of <a href="https://en.wikipedia.org/wiki/Power_function"/></summary>
        /// <param name="a">The argument of which cube root will be taken</param>
        /// <returns>Power node with (1/3) as the power</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Pow(2, 5));
        /// Console.WriteLine(Pow(2, 5).Simplify());
        /// Console.WriteLine(Pow(e, 2));
        /// Console.WriteLine(Pow(e, 2).Simplify());
        /// Console.WriteLine(Pow(e, 2).Evaled);
        /// Console.WriteLine(Sqrt(Sqr("x")));
        /// Console.WriteLine(Sqrt(Sqr("x")).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 3));
        /// Console.WriteLine(Pow(Cbrt("a"), 3).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 6));
        /// Console.WriteLine(Pow(Cbrt("a"), 6).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 2 ^ 5
        /// 32
        /// e ^ 2
        /// e ^ 2
        /// 7.389056098930650227230427460575007813180315570551847324087127822522573796079057763384312485079121792
        /// sqrt(x ^ 2)
        /// x
        /// a ^ (1/3) ^ 3
        /// a
        /// a ^ (1/3) ^ 6
        /// a ^ 2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Cbrt(Entity a) => new Powf(a, Number.Rational.Create(1, 3));

        /// <summary>Special case of <a href="https://en.wikipedia.org/wiki/Power_function"/></summary>
        /// <param name="a">Argument to be squared</param>
        /// <returns>Power node with 2 as the power</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Pow(2, 5));
        /// Console.WriteLine(Pow(2, 5).Simplify());
        /// Console.WriteLine(Pow(e, 2));
        /// Console.WriteLine(Pow(e, 2).Simplify());
        /// Console.WriteLine(Pow(e, 2).Evaled);
        /// Console.WriteLine(Sqrt(Sqr("x")));
        /// Console.WriteLine(Sqrt(Sqr("x")).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 3));
        /// Console.WriteLine(Pow(Cbrt("a"), 3).Simplify());
        /// Console.WriteLine(Pow(Cbrt("a"), 6));
        /// Console.WriteLine(Pow(Cbrt("a"), 6).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 2 ^ 5
        /// 32
        /// e ^ 2
        /// e ^ 2
        /// 7.389056098930650227230427460575007813180315570551847324087127822522573796079057763384312485079121792
        /// sqrt(x ^ 2)
        /// x
        /// a ^ (1/3) ^ 3
        /// a
        /// a ^ (1/3) ^ 6
        /// a ^ 2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Sqr(Entity a) => new Powf(a, 2);

        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which tangent will be taken</param>
        /// <returns>Tangent node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Tan("x") * Cotan("x");
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// var expr2 = Tan("x") * Cos("x");
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// tan(x) * cotan(x)
        /// 1
        /// sin(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Tan(Entity a) => new Tanf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which cotangent will be taken</param>
        /// <returns>Cotangent node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Tan("x") * Cotan("x");
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Simplify());
        /// var expr2 = Tan("x") * Cos("x");
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// tan(x) * cotan(x)
        /// 1
        /// sin(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Cotan(Entity a) => new Cotanf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arcsine will be taken</param>
        /// <returns>Arcsine node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arcsin(Entity a) => new Arcsinf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arccosine will be taken</param>
        /// <returns>Arccosine node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arccos(Entity a) => new Arccosf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arctangent will be taken</param>
        /// <returns>Arctangent node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arctan(Entity a) => new Arctanf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arccotangent will be taken</param>
        /// <returns>Arccotangent node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arccotan(Entity a) => new Arccotanf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arcsecant will be taken</param>
        /// <returns>Arccosine node with the reciprocal of the argument</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arcsec(Entity a) => new Arcsecantf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Inverse_trigonometric_functions"/></summary>
        /// <param name="a">Argument node of which arccosecant will be taken</param>
        /// <returns>Arcsine node with the reciprocal of the argument</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Arcsin("x") + Arccos("x");
        /// Console.WriteLine(expr);
        /// var expr2 = Arcsin(Sin("x")) + Arccos(Cos("y"));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// var expr3 = Arctan(Sin("a") / Cos("a"));
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Simplify());
        /// var expr4 = Arccotan(Cos("a") / Sin("a"));
        /// Console.WriteLine(expr4);
        /// Console.WriteLine(expr4.Simplify());
        /// var expr5 = Arcsec(Sec("aa")) + Arccosec(Cosec("bb"));
        /// Console.WriteLine(expr5);
        /// Console.WriteLine(expr5.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// arcsin(x) + arccos(x)
        /// arcsin(sin(x)) + arccos(cos(y))
        /// x + y
        /// arctan(sin(a) / cos(a))
        /// a
        /// arccotan(cos(a) / sin(a))
        /// a
        /// arcsec(sec(aa)) + arccsc(csc(bb))
        /// aa + bb
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Arccosec(Entity a) => new Arccosecantf(a);

        /// <summary>
        /// Is a special case of logarithm where the base equals
        /// <a href="https://en.wikipedia.org/wiki/E_(mathematical_constant)">e</a>:
        /// <a href="https://en.wikipedia.org/wiki/Natural_logarithm"/>
        /// </summary>
        /// <param name="a">Argument node of which natural logarithm will be taken</param>
        /// <returns>Logarithm node with base equal to e</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Log(2, 16));
        /// Console.WriteLine(Log(2, 16).Evaled);
        /// Console.WriteLine(Log(3, 81));
        /// Console.WriteLine(Log(3, 81).Evaled);
        /// Console.WriteLine(Log(10, 1000));
        /// Console.WriteLine(Log(10, 1000).Evaled);
        /// Console.WriteLine(Log(1000));
        /// Console.WriteLine(Log(1000).Evaled);
        /// Console.WriteLine(Ln(e.Pow(16)));
        /// Console.WriteLine(Ln(e.Pow(16)).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// log(2, 16)
        /// 4
        /// log(3, 81)
        /// 4
        /// log(10, 1000)
        /// 3
        /// log(10, 1000)
        /// 3
        /// ln(e ^ 16)
        /// 16
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        // The base is the value and not a reference to the name `e`, so a binder that declares
        // that name does not reach inside a logarithm. https://github.com/asc-community/AngouriMath/issues/984
        public static Entity Ln(Entity a) => new Logf(Entity.Constant.EulerIntrinsic, a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Factorial"/></summary>
        /// <param name="a">Argument node of which factorial will be taken</param>
        /// <returns>Factorial node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Factorial(5);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Evaled);
        /// var expr2 = Gamma(4);
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 5!
        /// 120
        /// (4 + 1)!
        /// 120
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Factorial(Entity a) => new Factorialf(a);

        /// <summary><a href="https://en.wikipedia.org/wiki/Gamma_function"/></summary>
        /// <param name="a">Argument node of which gamma function will be taken</param>
        /// <returns>Factorial node with one subtracted from the argument</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// var expr = Factorial(5);
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Evaled);
        /// var expr2 = Gamma(6);
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 5!
        /// 120
        /// (6 - 1)!
        /// 120
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Gamma(Entity a) => new Factorialf(a - 1);

        /// <summary>https://en.wikipedia.org/wiki/Sign_function</summary>
        /// <param name="a">Argument node of which Signum function will be taken</param>
        /// <returns>Signum node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Signum(-5));
        /// Console.WriteLine(Signum(-5).Evaled);
        /// Console.WriteLine(Signum(0));
        /// Console.WriteLine(Signum(0).Evaled);
        /// Console.WriteLine(Signum(5));
        /// Console.WriteLine(Signum(5).Evaled);
        /// Console.WriteLine(Signum(4 + 3 * i));
        /// Console.WriteLine(Signum(4 + 3 * i).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// sgn(-5)
        /// -1
        /// sgn(0)
        /// 0
        /// sgn(5)
        /// 1
        /// sgn(4 + 3i)
        /// 4/5 + (3/5)i
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Signum(Entity a) => new Signumf(a);

        /// <summary>https://en.wikipedia.org/wiki/Absolute_value</summary>
        /// <param name="a">Argument node of which Abs function will be taken</param>
        /// <returns>Abs node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Abs(-5));
        /// Console.WriteLine(Abs(-5).Evaled);
        /// Console.WriteLine(Abs(0));
        /// Console.WriteLine(Abs(0).Evaled);
        /// Console.WriteLine(Abs(5));
        /// Console.WriteLine(Abs(5).Evaled);
        /// Console.WriteLine(Abs(4 + 3 * i));
        /// Console.WriteLine(Abs(4 + 3 * i).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// abs(-5)
        /// 5
        /// abs(0)
        /// 0
        /// abs(5)
        /// 5
        /// abs(4 + 3i)
        /// 5
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Abs(Entity a) => new Absf(a);

        /// <summary>
        /// The logistic function, <c>1 / (1 + e^(-a))</c>.
        /// <a href="https://en.wikipedia.org/wiki/Sigmoid_function"/>
        /// </summary>
        /// <param name="a">The argument of the sigmoid.</param>
        /// <returns>
        /// The expression it is defined as, not a node of its own — so everything that already
        /// works on an exponential works on this: it differentiates, it evaluates, it prints.
        /// </returns>
        /// <remarks>
        /// One of the "non-kernel" functions of
        /// <a href="https://github.com/asc-community/AngouriMath/issues/321">#321</a>: things
        /// worth being able to <i>write</i>, which need no new node type because the kernel can
        /// already say them. Adding a node instead would mean the eleven steps of
        /// <c>Docs/Contributing/AddingNode.cs</c> and a new shape for every rule to ignore.
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Sigmoid("x"));
        /// Console.WriteLine(Sigmoid(0).EvalNumerical());
        /// </code>
        /// Prints
        /// <code>
        /// 1 / (1 + e ^ (-x))
        /// 1/2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Sigmoid(Entity a) => 1 / (1 + Pow(e, -a));

        /// <summary>
        /// The arithmetic mean of a matrix's cells.
        /// <a href="https://en.wikipedia.org/wiki/Arithmetic_mean"/>
        /// </summary>
        /// <param name="m">The matrix whose cells are averaged. A vector is a matrix here.</param>
        /// <returns>Their sum over their count.</returns>
        /// <remarks>
        /// Symbolic, so it averages what it is given rather than what it can evaluate:
        /// <c>Avg</c> of <c>[a, b]</c> is <c>(a + b) / 2</c>. See
        /// <a href="https://github.com/asc-community/AngouriMath/issues/321">#321</a>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown where the matrix is null.</exception>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Average(Vector(1, 2, 3)).Simplify());
        /// Console.WriteLine(Average(Vector("a", "b")).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 2
        /// (a + b) / 2
        /// </code>
        /// </example>
        public static Entity Average(Matrix m)
        {
            if (m is null)
                throw new System.ArgumentNullException(nameof(m));
            var cells = m.RowCount * m.ColumnCount;
            Entity sum = 0;
            for (var row = 0; row < m.RowCount; row++)
                for (var column = 0; column < m.ColumnCount; column++)
                    sum += m[row, column];
            return sum / cells;
        }

        /// <summary>
        /// The Euclidean length of a vector, <c>sqrt(sum of the squares of its cells)</c>.
        /// </summary>
        /// <param name="v">The vector whose length is taken.</param>
        /// <returns>Its length, as <see cref="Abs(Entity)"/> of the vector.</returns>
        /// <remarks>
        /// A name rather than a new capability: <c>abs([3, 4])</c> has always answered <c>5</c>,
        /// and this is that spelled so it can be found. Named on
        /// <a href="https://github.com/asc-community/AngouriMath/issues/321">#321</a>, where the
        /// observation that "VectorLength is <c>Abs</c>" is the whole implementation.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown where the vector is null.</exception>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(VectorLength(Vector(3, 4)).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 5
        /// </code>
        /// </example>
        public static Entity VectorLength(Matrix v)
            => v is null ? throw new System.ArgumentNullException(nameof(v)) : Abs(v);

        /// <summary>
        /// The greatest integer not above <paramref name="a"/>.
        /// </summary>
        /// <param name="a">The argument to take the floor of.</param>
        /// <returns>The floor of the argument.</returns>
        /// <remarks>
        /// Toward negative infinity, not toward zero: <c>floor(-3/2)</c> is <c>-2</c>. On a
        /// complex argument it is taken componentwise, which is what SymPy and Mathematica
        /// both do.
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Floor("3/2").Simplify());
        /// Console.WriteLine(Floor("-3/2").Simplify());
        /// Console.WriteLine(Floor("x"));
        /// </code>
        /// Prints
        /// <code>
        /// 1
        /// -2
        /// floor(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Floor(Entity a) => new Floorf(a);

        /// <summary>
        /// The least integer not below <paramref name="a"/>.
        /// </summary>
        /// <param name="a">The argument to take the ceiling of.</param>
        /// <returns>The ceiling of the argument.</returns>
        /// <remarks>
        /// Toward positive infinity, not away from zero: <c>ceil(-3/2)</c> is <c>-1</c>.
        /// Componentwise on a complex argument, as <see cref="Floor(Entity)"/> is.
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Ceil("3/2").Simplify());
        /// Console.WriteLine(Ceil("-3/2").Simplify());
        /// Console.WriteLine(Ceil("x"));
        /// </code>
        /// Prints
        /// <code>
        /// 2
        /// -1
        /// ceil(x)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Ceil(Entity a) => new Ceilf(a);

        /// <summary>
        /// The nearest integer to <paramref name="a"/>, a tie going to the even one.
        /// </summary>
        /// <param name="a">The argument to round.</param>
        /// <returns>The rounded argument.</returns>
        /// <remarks>
        /// Half to even, as Python, SymPy, Mathematica and IEEE 754 all round: <c>round(1/2)</c>
        /// is <c>0</c> and <c>round(3/2)</c> is <c>2</c>. It is therefore <b>not</b>
        /// <c>floor(x + 1/2)</c>, which differs at every tie.
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Round("1/2").Simplify());
        /// Console.WriteLine(Round("3/2").Simplify());
        /// Console.WriteLine(Round("5/2").Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 0
        /// 2
        /// 2
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Round(Entity a) => new Roundf(a);

        /// <summary>
        /// The lesser of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns>Whichever is smaller, or the unevaluated node where they cannot be compared.</returns>
        /// <remarks>
        /// Only ordered arguments compare, so a complex one is left alone rather than
        /// guessed at. For more than two, nest: <c>Min(Min(a, b), c)</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Min(Entity a, Entity b) => new Minf(a, b);

        /// <summary>
        /// The greater of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns>Whichever is larger, or the unevaluated node where they cannot be compared.</returns>
        /// <remarks>See <see cref="Min(Entity, Entity)"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Max(Entity a, Entity b) => new Maxf(a, b);

        /// <summary>
        /// The greatest common divisor of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns>The gcd where it can be computed, the unevaluated node otherwise.</returns>
        /// <remarks>
        /// Integers and rationals are computed — <c>gcd(1/2, 1/3)</c> is <c>1/6</c>, as
        /// SymPy gives. The polynomial case is left unevaluated for now, though this library
        /// does compute polynomial gcds elsewhere.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Gcd(Entity a, Entity b) => new Gcdf(a, b);

        /// <summary>
        /// The least common multiple of two numbers, <c>lcm(a, b)</c>: computed over integers
        /// and rationals, non-negative, <c>0</c> where either is <c>0</c>, and left as a node
        /// otherwise. https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(MathS.Lcm(4, 6).Evaled);
        /// Console.WriteLine("lcm(2, 3, 4)".ToEntity().Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// 12
        /// 12
        /// </code>
        /// </example>
        public static Entity Lcm(Entity a, Entity b) => new Lcmf(a, b);

        /// <summary>The binomial coefficient <c>binomial(n, k)</c>, "n choose k"</summary>
        /// <param name="n">The upper argument, which need not be a whole number</param>
        /// <param name="k">The lower argument</param>
        /// <returns>The <see cref="Entity.Binomialf"/> node</returns>
        /// <remarks>
        /// Whole arguments are computed, by the falling factorial where <c>n</c> is negative --
        /// <c>binomial(-1, 3)</c> is <c>-1</c> -- and a whole <c>k</c> below zero, or above a
        /// non-negative whole <c>n</c>, is <c>0</c>. A symbolic argument is kept as the node,
        /// which is the shape Pascal's identity and the binomial theorem read.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Binomial(Entity n, Entity k) => new Binomialf(n, k);

        /// <summary>The Iverson bracket <c>iverson(P)</c>: <c>1</c> where the statement holds, <c>0</c> where it does not</summary>
        /// <param name="statement">The statement</param>
        /// <returns>The <see cref="Entity.Iversonf"/> node</returns>
        /// <remarks>
        /// A statement that is not decided is kept as the node. A count is a sum of these:
        /// <c>sum(iverson(k divides 12), k, 1, 12)</c> is the number of divisors of <c>12</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1478
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Iverson("3 divides 12").Simplify());
        /// Console.WriteLine(Iverson("2 > 3").Simplify());
        /// Console.WriteLine(Iverson("x > 0").Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 1
        /// 0
        /// iverson(x > 0)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Iverson(Entity statement) => new Iversonf(statement);

        /// <summary>The aleph number <c>aleph(k)</c>, the size of an infinite set</summary>
        /// <param name="index">The index, a whole number from zero</param>
        /// <returns>The <see cref="Entity.Alephf"/> node</returns>
        /// <remarks>
        /// <c>aleph(0)</c> is the size of the whole numbers and of the rationals, and
        /// <c>card(ZZ)</c> is it. The size of the reals is <c>2^aleph(0)</c>, not a named aleph:
        /// which one it is, is the continuum hypothesis.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Aleph(Entity index) => new Alephf(index);

        /// <summary>The error function, <c>erf(z) = 2/sqrt(pi) int_0^z e^(-t^2) dt</c></summary>
        /// <param name="a">The argument, any complex number</param>
        /// <returns>The <see cref="Entity.Erff"/> node</returns>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/1501</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Erf(Entity a) => new Erff(a);

        /// <summary>The complementary error function, <c>erfc(z) = 1 - erf(z)</c></summary>
        /// <param name="a">The argument, any complex number</param>
        /// <returns>The <see cref="Entity.Erfcf"/> node</returns>
        /// <remarks>https://github.com/asc-community/AngouriMath/issues/1501</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Erfc(Entity a) => new Erfcf(a);

        /// <summary>The imaginary error function, <c>erfi(z) = -i erf(i z)</c></summary>
        /// <param name="a">The argument, any complex number</param>
        /// <returns>The <see cref="Entity.Erfif"/> node</returns>
        /// <remarks>
        /// On the real line <c>erfi(x) = 2/sqrt(pi) int_0^x e^(t^2) dt</c>, so it is what
        /// <c>int e^(x^2) dx</c> is in closed form.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Erfi(Entity a) => new Erfif(a);

        /// <summary>The exponential integral, <c>Ei(z) = gamma + (ln z - ln(1/z))/2 + sum z^k/(k k!)</c></summary>
        /// <param name="a">The argument, any complex number but 0</param>
        /// <returns>The <see cref="Entity.Eif"/> node</returns>
        /// <remarks>
        /// On the real line <c>Ei(x)</c> is the principal value of <c>int_-oo^x e^t/t dt</c>, real on
        /// either side of 0, so it is what <c>int e^x/x dx</c> is in closed form. Off it, the cut is
        /// the negative real axis, as for <c>ln</c>, but <c>Ei</c> keeps its real value on the cut
        /// itself: <c>Ei(-1)</c> is about <c>-0.2194</c>, and just above the axis it is <c>i pi</c> more.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Ei(Entity a) => new Eif(a);

        /// <summary>The logarithmic integral, <c>li(z) = Ei(ln z)</c></summary>
        /// <param name="a">The argument, any complex number but 1</param>
        /// <returns>The <see cref="Entity.Lif"/> node</returns>
        /// <remarks>
        /// On <c>x > 0</c>, <c>li(x)</c> is the principal value of <c>int_0^x dt/ln t</c>, so it is what
        /// <c>int 1/ln(x) dx</c> is in closed form: real, <c>0</c> at <c>0</c> and <c>-oo</c> at <c>1</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Li(Entity a) => new Lif(a);

        /// <summary>The sine integral, <c>Si(z) = int_0^z sin(t)/t dt</c></summary>
        /// <param name="a">The argument, any complex number</param>
        /// <returns>The <see cref="Entity.Sif"/> node</returns>
        /// <remarks>
        /// Entire and odd, so what <c>int sin(x)/x dx</c> is in closed form; <c>Si(+oo) = pi/2</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Si(Entity a) => new Sif(a);

        /// <summary>The cosine integral, <c>Ci(z) = gamma + ln z + int_0^z (cos t - 1)/t dt</c></summary>
        /// <param name="a">The argument, any complex number but 0</param>
        /// <returns>The <see cref="Entity.Cif"/> node</returns>
        /// <remarks>
        /// On <c>x > 0</c> it is <c>-int_x^oo cos(t)/t dt</c>, real, so what <c>int cos(x)/x dx</c> is in
        /// closed form. Its cut is the negative real axis, as for <c>ln</c>, on which it is the value
        /// from above: <c>Ci(-x) = Ci(x) + i pi</c>, as SymPy's, mpmath's and Mathematica's is.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Ci(Entity a) => new Cif(a);

        /// <summary>The hyperbolic sine integral, <c>Shi(z) = int_0^z sinh(t)/t dt</c></summary>
        /// <param name="a">The argument, any complex number</param>
        /// <returns>The <see cref="Entity.Shif"/> node</returns>
        /// <remarks>
        /// Entire and odd, so what <c>int sinh(x)/x dx</c> is in closed form.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Shi(Entity a) => new Shif(a);

        /// <summary>The hyperbolic cosine integral, <c>Chi(z) = gamma + ln z + int_0^z (cosh t - 1)/t dt</c></summary>
        /// <param name="a">The argument, any complex number but 0</param>
        /// <returns>The <see cref="Entity.Chif"/> node</returns>
        /// <remarks>
        /// Real on <c>x > 0</c>, so what <c>int cosh(x)/x dx</c> is in closed form; its cut is the
        /// negative real axis, <c>Chi(-x) = Chi(x) + i pi</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1501
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Chi(Entity a) => new Chif(a);

        /// <summary>Boolean negation
        /// <a href="https://en.wikipedia.org/wiki/Negation">Wikipedia</a></summary>
        /// <param name="a">Argument node of which Negation function will be taken</param>
        /// <returns>The Not node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Negation("x"));
        /// Console.WriteLine(Negation(Negation("x")));
        /// Console.WriteLine(Negation(Negation("x")).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// not x
        /// not not x
        /// x
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Negation(Entity a) => !a;

        /// <summary>
        /// This will be turned into <paramref name="expression"/> if the <paramref name="condition"/> is true,
        /// into NaN if <paramref name="condition"/> is false, and remain the same otherwise
        /// </summary>
        /// <param name="expression">The expression is extracted if the predicate is true</param>
        /// <param name="condition">Condition when the expression is defined</param>
        /// <returns>The Provided node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// Console.WriteLine(Provided("a = b", "pi = 4"));
        /// Console.WriteLine(Provided("a = b", "pi = 4").Simplify());
        /// Console.WriteLine();
        /// 
        /// var expr = Provided("1000", "a = b")
        ///         .Substitute("a", 5)
        ///         .Substitute("b", 130)
        ///     ;
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Evaled);
        /// 
        /// Console.WriteLine();
        /// 
        /// var expr1 = Provided("1000", "a = b")
        ///         .Substitute("a", 5)
        ///         .Substitute("b", 5)
        ///     ;
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// a = b provided pi = 4
        /// NaN
        /// 
        /// 1000 provided 5 = 130
        /// NaN
        /// 
        /// 1000 provided 5 = 5
        /// 1000
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
        public static Entity Provided(Entity expression, Entity condition)
            => expression.Provided(condition);

        /// <summary>
        /// The statement that every member of <paramref name="over"/> satisfies
        /// <paramref name="body"/>, written <c>forall var in over : body</c>. Decided over a
        /// finite set by evaluation at every member, over an infinite one by a counterexample
        /// or by solving where the solver reads the body, and left as written otherwise.
        /// https://github.com/asc-community/AngouriMath/issues/1409
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(MathS.ForAll("x", "RR", "x^2 &gt;= 0").Simplify());
        /// Console.WriteLine("forall n in ZZ : n^2 &lt;= n^3".ToEntity().Simplify());
        /// Console.WriteLine("forall x in {1, 2, 3, 4} : exists y in {3, 4, 5, 6, 7, 8} : x + y = 7".ToEntity().Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// True
        /// False
        /// True
        /// </code>
        /// </example>
        public static Entity ForAll(Entity var, Entity over, Entity body) => new Forallf(var, over, body);

        /// <summary>
        /// The statement that some member of <paramref name="over"/> satisfies
        /// <paramref name="body"/>, written <c>exists var in over : body</c>.
        /// See <see cref="ForAll(Entity, Entity, Entity)"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(MathS.Exists("x", "RR", "x^2 - 4 x + 4 = 0").Simplify());
        /// Console.WriteLine("exists x in RR : x^2 + 1 = 0".ToEntity().Simplify());
        /// Console.WriteLine("exists x in CC : x^2 + 1 = 0".ToEntity().Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// True
        /// False
        /// True
        /// </code>
        /// </example>
        public static Entity Exists(Entity var, Entity over, Entity body) => new Existsf(var, over, body);

        /// <summary>
        /// The statement that exactly one member of <paramref name="over"/> satisfies
        /// <paramref name="body"/>, written <c>exists! var in over : body</c>.
        /// See <see cref="ForAll(Entity, Entity, Entity)"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(MathS.ExistsUnique("x", "ZZ+", "x^2 = 4").Simplify());
        /// Console.WriteLine("exists! x in RR : x^2 = 4".ToEntity().Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// True
        /// False
        /// </code>
        /// </example>
        public static Entity ExistsUnique(Entity var, Entity over, Entity body) => new ExistsUniquef(var, over, body);

        /// <summary>
        /// This is a piecewisely defined function, which turns into a particular definition
        /// once there exists a case number N such that case[N].Predicate is turned into true and
        /// for all i less than N : case[i].Predicate is turned into false.
        /// 
        /// For example, Piecewise(new Providedf(a, b), new Providedf(d, false), new Providedf(f, true))
        /// will remain unchanged, because the first case is uncertain.
        /// 
        /// Piecewise(new Providedf(a, false), new Providedf(d, false), new Providedf(f, true))
        /// will turn into f
        /// 
        /// Piecewise(new Providedf(a, false), new Providedf(d, false), new Providedf(f, false))
        /// will turn into NaN
        /// </summary>
        /// <param name="cases">
        /// Cases, each of type Provided.
        /// </param>
        /// <param name="otherwise">
        /// An otherwise case. Will be intepreted as otherwise.Provided(true). Optional.
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Piecewise(
        ///     ("-1", "x &lt; 0"),
        ///     ("0", "x = 0"),
        ///     ("1", true)
        /// );
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Substitute("x", 10).Evaled);
        /// Console.WriteLine(expr.Substitute("x", 0).Evaled);
        /// Console.WriteLine(expr.Substitute("x", -14).Evaled);
        /// 
        /// Console.WriteLine();
        /// 
        /// var weirdSqrt = Piecewise(
        ///     ("sqrt(x)", "x &gt;= 0"),
        ///     ("-sqrt(-x)", true)
        /// );
        /// Console.WriteLine(weirdSqrt);
        /// Console.WriteLine(weirdSqrt.Substitute("x", 16).Evaled);
        /// Console.WriteLine(weirdSqrt.Substitute("x", -16).Evaled);
        /// 
        /// Console.WriteLine();
        /// 
        /// var thousandVisualizer = Piecewise(
        ///     ("a / 1000000 million", "a &gt; 1000000"),
        ///     ("a / 1000 thousand", "a &gt; 1000"),
        ///     ("a", "a >= 0")
        /// );
        /// 
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 19301).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 19301123).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 32).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", -24).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// (-1 if x &lt; 0, 0 if x = 0, 1 if True)
        /// 1
        /// 0
        /// -1
        /// 
        /// (sqrt(x) if x &gt;= 0, -sqrt(-x) if True)
        /// 4
        /// -4
        /// 
        /// 19301/1000 * thousand
        /// 19301123/1000000 * million
        /// 32
        /// NaN
        /// </code>
        /// </example>
        public static Entity Piecewise(IEnumerable<Providedf> cases, Entity? otherwise = null)
            => new Piecewise(otherwise is null ? cases : cases.Append(new Providedf(otherwise, true)));

        /// <summary>
        /// This is a piecewisely defined function, which turns into a particular definition
        /// once there exists a case number N such that case[N].Predicate is turned into true and
        /// for all i less than N : case[i].Predicate is turned into false.
        /// 
        /// For example, Piecewise((a, b), (d, false), (f, true))
        /// will remain unchanged, because the first case is uncertain.
        /// 
        /// Piecewise((a, false), (d, false), (f, true))
        /// will turn into f
        /// 
        /// Piecewise((a, false), (d, false), (f, false))
        /// will turn into NaN
        /// </summary>
        /// <param name="cases">
        /// Tuples of two expressions: an expression and a predicate
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = Piecewise(
        ///     ("-1", "x &lt; 0"),
        ///     ("0", "x = 0"),
        ///     ("1", true)
        /// );
        /// Console.WriteLine(expr);
        /// Console.WriteLine(expr.Substitute("x", 10).Evaled);
        /// Console.WriteLine(expr.Substitute("x", 0).Evaled);
        /// Console.WriteLine(expr.Substitute("x", -14).Evaled);
        /// 
        /// Console.WriteLine();
        /// 
        /// var weirdSqrt = Piecewise(
        ///     ("sqrt(x)", "x &gt;= 0"),
        ///     ("-sqrt(-x)", true)
        /// );
        /// Console.WriteLine(weirdSqrt);
        /// Console.WriteLine(weirdSqrt.Substitute("x", 16).Evaled);
        /// Console.WriteLine(weirdSqrt.Substitute("x", -16).Evaled);
        /// 
        /// Console.WriteLine();
        /// 
        /// var thousandVisualizer = Piecewise(
        ///     ("a / 1000000 million", "a &gt; 1000000"),
        ///     ("a / 1000 thousand", "a &gt; 1000"),
        ///     ("a", "a >= 0")
        /// );
        /// 
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 19301).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 19301123).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", 32).Evaled);
        /// Console.WriteLine(thousandVisualizer.Substitute("a", -24).Evaled);
        /// </code>
        /// Prints
        /// <code>
        /// (-1 if x &lt; 0, 0 if x = 0, 1 if True)
        /// 1
        /// 0
        /// -1
        /// 
        /// (sqrt(x) if x &gt;= 0, -sqrt(-x) if True)
        /// 4
        /// -4
        /// 
        /// 19301/1000 * thousand
        /// 19301123/1000000 * million
        /// 32
        /// NaN
        /// </code>
        /// </example>
        public static Entity Piecewise(params (Entity expression, Entity predicate)[] cases)
            => new Piecewise(cases.Select(c => new Providedf(c.expression, c.predicate)));

        /// <summary>
        /// Applies the list of arguments to the given expression
        /// </summary>
        /// <returns><see cref="Entity.Application"/></returns>
        /// <example>
        /// <code>
        /// Entity expr = "sin";
        /// Console.WriteLine(expr);
        /// var applied = expr.Apply(pi / 3);
        /// Console.WriteLine(applied);
        /// Console.WriteLine(applied.Simplify());
        /// Console.WriteLine(applied.Evaled);
        /// Console.WriteLine("------------------------------");
        /// var lambda = Lambda("x", "x ^ 3 + x");
        /// Console.WriteLine(lambda);
        /// Console.WriteLine(lambda.Apply("3"));
        /// Console.WriteLine(lambda.Apply("3").Evaled);
        /// Console.WriteLine("------------------------------");
        /// var lambda2 = Lambda("y", "y".ToEntity().Apply(pi / 3));
        /// Console.WriteLine(lambda2);
        /// Console.WriteLine(lambda2.Apply("sin").Simplify());
        /// Console.WriteLine(lambda2.Apply("cos").Simplify());
        /// Console.WriteLine(lambda2.Apply("tan").Simplify());
        /// Console.WriteLine("------------------------------");
        /// var lambda3 = Lambda("x", Lambda("y", Lambda("z", "x + y / z")));
        /// Console.WriteLine(lambda3);
        /// Console.WriteLine(lambda3.Apply(5));
        /// Console.WriteLine(lambda3.Apply(5).Simplify());
        /// Console.WriteLine(lambda3.Apply(5).Apply(10));
        /// Console.WriteLine(lambda3.Apply(5).Apply(10).Simplify());
        /// Console.WriteLine(lambda3.Apply(5, 10));
        /// Console.WriteLine(lambda3.Apply(5, 10).Simplify());
        /// Console.WriteLine(lambda3.Apply(5, 10, 7));
        /// Console.WriteLine(lambda3.Apply(5, 10, 7).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin
        /// sin (pi / 3)
        /// sqrt(3) / 2
        /// 1/2 * sqrt(3)
        /// ------------------------------
        /// x -> x ^ 3 + x
        /// (x -> x ^ 3 + x) 3
        /// 30
        /// ------------------------------
        /// y -> y (pi / 3)
        /// sqrt(3) / 2
        /// 1/2
        /// sqrt(3)
        /// ------------------------------
        /// x -> y -> z -> x + y / z
        /// (x -> y -> z -> x + y / z) 5
        /// y -> z -> 5 + y / z
        /// (x -> y -> z -> x + y / z) 5 10
        /// z -> 5 + 10 / z
        /// (x -> y -> z -> x + y / z) 5 10
        /// z -> 5 + 10 / z
        /// (x -> y -> z -> x + y / z) 5 10 7
        /// 45/7
        /// </code>
        /// </example>
        public static Entity Apply(Entity expr, params Entity[] arguments)
            => expr.Apply(arguments);

        /// <summary>
        /// Returns a lambda with the given parameter and body
        /// </summary>
        /// <example>
        /// <code>
        /// Entity expr = "sin";
        /// Console.WriteLine(expr);
        /// var applied = expr.Apply(pi / 3);
        /// Console.WriteLine(applied);
        /// Console.WriteLine(applied.Simplify());
        /// Console.WriteLine(applied.Evaled);
        /// Console.WriteLine("------------------------------");
        /// var lambda = Lambda("x", "x ^ 3 + x");
        /// Console.WriteLine(lambda);
        /// Console.WriteLine(lambda.Apply("3"));
        /// Console.WriteLine(lambda.Apply("3").Evaled);
        /// Console.WriteLine("------------------------------");
        /// var lambda2 = Lambda("y", "y".ToEntity().Apply(pi / 3));
        /// Console.WriteLine(lambda2);
        /// Console.WriteLine(lambda2.Apply("sin").Simplify());
        /// Console.WriteLine(lambda2.Apply("cos").Simplify());
        /// Console.WriteLine(lambda2.Apply("tan").Simplify());
        /// Console.WriteLine("------------------------------");
        /// var lambda3 = Lambda("x", Lambda("y", Lambda("z", "x + y / z")));
        /// Console.WriteLine(lambda3);
        /// Console.WriteLine(lambda3.Apply(5));
        /// Console.WriteLine(lambda3.Apply(5).Simplify());
        /// Console.WriteLine(lambda3.Apply(5).Apply(10));
        /// Console.WriteLine(lambda3.Apply(5).Apply(10).Simplify());
        /// Console.WriteLine(lambda3.Apply(5, 10));
        /// Console.WriteLine(lambda3.Apply(5, 10).Simplify());
        /// Console.WriteLine(lambda3.Apply(5, 10, 7));
        /// Console.WriteLine(lambda3.Apply(5, 10, 7).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// sin
        /// sin (pi / 3)
        /// sqrt(3) / 2
        /// 1/2 * sqrt(3)
        /// ------------------------------
        /// x -> x ^ 3 + x
        /// (x -> x ^ 3 + x) 3
        /// 30
        /// ------------------------------
        /// y -> y (pi / 3)
        /// sqrt(3) / 2
        /// 1/2
        /// sqrt(3)
        /// ------------------------------
        /// x -> y -> z -> x + y / z
        /// (x -> y -> z -> x + y / z) 5
        /// y -> z -> 5 + y / z
        /// (x -> y -> z -> x + y / z) 5 10
        /// z -> 5 + 10 / z
        /// (x -> y -> z -> x + y / z) 5 10
        /// z -> 5 + 10 / z
        /// (x -> y -> z -> x + y / z) 5 10 7
        /// 45/7
        /// </code>
        /// </example>
        public static Entity Lambda(Variable param, Entity body)
            => new Lambda(param, body);

        /// <summary>
        /// Represents a few hyperbolic functions
        /// </summary>
        public static class Hyperbolic
        {
            /// <summary>
            /// Hyperbolic sine:
            /// <code>(e^x - e^(-x)) / 2</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Sinh("x"));
            /// Console.WriteLine(Sinh("x").Differentiate("x").Simplify());
            /// Console.WriteLine(Sinh("x").Differentiate("x").Simplify() == Cosh("x"));
            /// Console.WriteLine("--------------------------");
            /// Console.WriteLine(Cosh("x"));
            /// Console.WriteLine(Cosh("x").Differentiate("x").Simplify());
            /// Console.WriteLine(Cosh("x").Differentiate("x").Simplify() == Sinh("x"));
            /// </code>
            /// Prints
            /// <code>
            /// (e ^ x - e ^ (-x)) / 2
            /// (e ^ x + e ^ (-x)) / 2
            /// True
            /// --------------------------
            /// (e ^ x + e ^ (-x)) / 2
            /// (e ^ x - e ^ (-x)) / 2
            /// True
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Sinh(Entity x) => (e.Pow(x) - e.Pow(-x)) / 2;

            /// <summary>
            /// Hyperbolic cosine:
            /// <code>(e^x + e^(-x)) / 2</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Sinh("x"));
            /// Console.WriteLine(Sinh("x").Differentiate("x").Simplify());
            /// Console.WriteLine(Sinh("x").Differentiate("x").Simplify() == Cosh("x"));
            /// Console.WriteLine("--------------------------");
            /// Console.WriteLine(Cosh("x"));
            /// Console.WriteLine(Cosh("x").Differentiate("x").Simplify());
            /// Console.WriteLine(Cosh("x").Differentiate("x").Simplify() == Sinh("x"));
            /// </code>
            /// Prints
            /// <code>
            /// (e ^ x - e ^ (-x)) / 2
            /// (e ^ x + e ^ (-x)) / 2
            /// True
            /// --------------------------
            /// (e ^ x + e ^ (-x)) / 2
            /// (e ^ x - e ^ (-x)) / 2
            /// True
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Cosh(Entity x) => (e.Pow(x) + e.Pow(-x)) / 2;

            /// <summary>
            /// Hyperbolic tangent:
            /// <code>(e ^ (2x) - 1) / (e ^ (2x) + 1)</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Tanh("x"));
            /// Console.WriteLine(Tanh("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Tanh("x").Substitute("x", 0.01).Evaled);
            /// 
            /// Console.WriteLine(Cotanh("x"));
            /// Console.WriteLine(Cotanh("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Cotanh("x").Substitute("x", 0.01).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// (e ^ (2 * x) - 1) / (e ^ (2 * x) + 1)
            /// 0.905148253644
            /// 0.009999666679
            /// (e ^ (2 * x) + 1) / (e ^ (2 * x) - 1)
            /// 1.104791392982
            /// 100.0033333111
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Tanh(Entity x) => (e.Pow(2 * x) - 1) / (e.Pow(2 * x) + 1);

            /// <summary>
            /// Hyperbolic cotangent:
            /// <code>(e ^ (2x) + 1) / (e ^ (2x) - 1)</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Tanh("x"));
            /// Console.WriteLine(Tanh("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Tanh("x").Substitute("x", 0.01).Evaled);
            /// Console.WriteLine("-----------------------------");
            /// Console.WriteLine(Cotanh("x"));
            /// Console.WriteLine(Cotanh("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Cotanh("x").Substitute("x", 0.01).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// (e ^ (2 * x) - 1) / (e ^ (2 * x) + 1)
            /// 0.905148253644
            /// 0.009999666679
            /// -----------------------------
            /// (e ^ (2 * x) + 1) / (e ^ (2 * x) - 1)
            /// 1.104791392982
            /// 100.0033333111
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Cotanh(Entity x) => (e.Pow(2 * x) + 1) / (e.Pow(2 * x) - 1);

            /// <summary>
            /// Hyperbolic secant:
            /// <code>1 / cosh x</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(Sech("x"));
            /// Console.WriteLine(Sech("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Sech("x").Substitute("x", 0.01).Evaled);
            /// Console.WriteLine("-----------------------------");
            /// Console.WriteLine(Cosech("x"));
            /// Console.WriteLine(Cosech("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Cosech("x").Substitute("x", 0.01).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// 1 / ((e ^ x + e ^ (-x)) / 2)
            /// 0.42509
            /// 0.99995
            /// -----------------------------
            /// 1 / ((e ^ x - e ^ (-x)) / 2)
            /// 0.46964
            /// 99.9983
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Sech(Entity x) => 1 / Cosh(x);

            /// <summary>
            /// Hyperbolic cosecant:
            /// <code>1 / sinh x</code>
            /// <a href="https://en.wikipedia.org/wiki/Hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(Sech("x"));
            /// Console.WriteLine(Sech("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Sech("x").Substitute("x", 0.01).Evaled);
            /// Console.WriteLine("-----------------------------");
            /// Console.WriteLine(Cosech("x"));
            /// Console.WriteLine(Cosech("x").Substitute("x", 1.5).Evaled);
            /// Console.WriteLine(Cosech("x").Substitute("x", 0.01).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// 1 / ((e ^ x + e ^ (-x)) / 2)
            /// 0.42509
            /// 0.99995
            /// -----------------------------
            /// 1 / ((e ^ x - e ^ (-x)) / 2)
            /// 0.46964
            /// 99.9983
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Cosech(Entity x) => 1 / Sinh(x);

            /// <summary>
            /// Inverse hyperbolic sine:
            /// <code>ln(x + sqrt(x ^ 2 + 1))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Arsinh(Entity x) => Ln(x + Sqrt(x.Pow(2) + 1));

            /// <summary>
            /// Inverse hyperbolic cosine:
            /// <code>ln(x + sqrt(x ^ 2 - 1))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Arcosh(Entity x) => Ln(x + Sqrt(x.Pow(2) - 1));

            /// <summary>
            /// Inverse hyperbolic tangent:
            /// <code>1/2 * ln((1 + x) / (1 - x))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Artanh(Entity x) => 0.5 * Ln((1 + x) / (1 - x));

            /// <summary>
            /// Inverse hyperbolic cotangent:
            /// <code>1/2 * ln((1 - x) / (1 + x))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Arcotanh(Entity x) => 0.5 * Ln((x + 1) / (x - 1));

            /// <summary>
            /// Inverse hyperbolic secant:
            /// <code>ln(1 / x + sqrt(1 / sqr(x) - 1))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Arsech(Entity x) => Ln(1 / x + Sqrt(1 / Sqr(x) - 1));

            /// <summary>
            /// Inverse hyperbolic cosecant:
            /// <code>ln(1 / x + sqrt(1 / sqr(x) + 1))</code>
            /// <a href="https://en.wikipedia.org/wiki/Inverse_hyperbolic_functions">Wikipedia</a>
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Hyperbolic;
            /// 
            /// Console.WriteLine(Arsinh("x"));
            /// Console.WriteLine(Arsinh(Sinh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosh("x"));
            /// Console.WriteLine(Arcosh(Cosh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Artanh("x"));
            /// Console.WriteLine(Artanh(Tanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcotanh("x"));
            /// Console.WriteLine(Arcotanh(Cotanh("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arsech("x"));
            /// Console.WriteLine(Arsech(Sech("x")).Substitute("x", 10).Evaled);
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(Arcosech("x"));
            /// Console.WriteLine(Arcosech(Cosech("x")).Substitute("x", 10).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// ln(x + sqrt(x ^ 2 + 1))
            /// 10
            /// ----------------------
            /// ln(x + sqrt(x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// 1/2 * ln((1 + x) / (1 - x))
            /// 10
            /// ----------------------
            /// 1/2 * ln((x + 1) / (x - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 - 1))
            /// 10
            /// ----------------------
            /// ln(1 / x + sqrt(1 / x ^ 2 + 1))
            /// 10
            /// </code>
            /// </example>
            [MethodImpl(MethodImplOptions.AggressiveInlining), NativeExport]
            public static Entity Arcosech(Entity x) => Ln(1 / x + Sqrt(1 / Sqr(x) + 1));
        }

        /// <summary>
        /// That is a collection of some series, expressed in a symbolic form
        /// </summary>
        public static class Series
        {
            /// <summary>
            /// Finds the symbolic expression of terms of the Maclaurin expansion of the given function,
            /// <a href="https://en.wikipedia.org/wiki/Taylor_series">Wikipedia</a>
            /// </summary>
            /// <param name="expr">
            /// The function to find the Maclaurin expansion of
            /// </param>
            /// <param name="degree">
            /// The degree of the resulting Maclaurin polynomial (and the variable in the resulting series)
            /// </param>
            /// <param name="exprVariables">
            /// The variable/s to take the series over (and the variable the series will be over)
            /// (e. g. if you have expr = Sin("t"), then you may want to use "t" for this argument)
            /// </param>
            /// <returns>
            /// An expression in the polynomial form over the expression variables given in <paramref name="exprVariables"/>
            /// </returns>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using System.Linq;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.MathS.Series;
            /// 
            /// var (x, y) = MathS.Var("x", "y");
            /// Console.WriteLine(Sin(x));
            /// Console.WriteLine(Maclaurin(Sin(x), 1, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 2, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 3, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 4, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 10, x).Simplify());
            /// Console.WriteLine("----------------------");
            /// var expr = Sin(x) + Cos(y);
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Maclaurin(expr, 6, x, y).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1), (y, 5)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, "z_1", 1), (y, "z_2", 5)));
            /// Console.WriteLine("----------------------");
            /// var first3Terms = 
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(3);
            /// var first6Terms =
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(6);
            /// foreach (var term in first6Terms)
            ///     Console.WriteLine($"Received {term}");
            /// </code>
            /// Prints
            /// <code>
            /// sin(x)
            /// 0
            /// 0 + x
            /// 0 + x + 0
            /// 0 + x + 0 + -x ^ 3 / 3!
            /// x ^ 9 / 362880 - x ^ 7 / 5040 + x ^ 5 / 120 - x ^ 3 / 6 + x
            /// ----------------------
            /// sin(x) + cos(y)
            /// 1 + (6 * x ^ 5 - 120 * x ^ 3) / 720 + x + (2 * y ^ 4 - 24 * y ^ 2) / 48
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(y) + cos(1) * (x - 1) + -sin(1) * (x - 1) ^ 2 / 2! + cos(1) * (-1) * (x - 1) ^ 3 / 3! + -sin(1) * (-1) * (x - 1) ^ 4 / 4! + cos(1) * (-1) * (-1) * (x - 1) ^ 5 / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (x - 1) + -sin(5) * (y - 5) + (-sin(1) * (x - 1) ^ 2 + cos(5) * (-1) * (y - 5) ^ 2) / 2! + (cos(1) * (-1) * (x - 1) ^ 3 + -sin(5) * (-1) * (y - 5) ^ 3) / 3! + (-sin(1) * (-1) * (x - 1) ^ 4 + cos(5) * (-1) * (-1) * (y - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (x - 1) ^ 5 + -sin(5) * (-1) * (-1) * (y - 5) ^ 5) / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (z_1 - 1) + -sin(5) * (z_2 - 5) + (-sin(1) * (z_1 - 1) ^ 2 + cos(5) * (-1) * (z_2 - 5) ^ 2) / 2! + (cos(1) * (-1) * (z_1 - 1) ^ 3 + -sin(5) * (-1) * (z_2 - 5) ^ 3) / 3! + (-sin(1) * (-1) * (z_1 - 1) ^ 4 + cos(5) * (-1) * (-1) * (z_2 - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (z_1 - 1) ^ 5 + -sin(5) * (-1) * (-1) * (z_2 - 5) ^ 5) / 5!
            /// ----------------------
            /// Received cos(1)
            /// Received x + -sin(1) * (y - 1)
            /// Received cos(1) * (-1) * (y - 1) ^ 2 / 2!
            /// Received (-x ^ 3 + -sin(1) * (-1) * (y - 1) ^ 3) / 3!
            /// Received cos(1) * (-1) * (-1) * (y - 1) ^ 4 / 4!
            /// Received (x ^ 5 + -sin(1) * (-1) * (-1) * (y - 1) ^ 5) / 5!
            /// </code>
            /// </example>
            public static Entity Maclaurin(Entity expr, int degree, params Variable[] exprVariables)
                => Functions.Series.TaylorExpansion(expr, degree, exprVariables.Select(v => (v, v, (Entity)0)).ToArray());

            /// <summary>
            /// Finds the symbolic expression of terms of the Taylor expansion of the given function,
            /// <a href="https://en.wikipedia.org/wiki/Taylor_series">Wikipedia</a>
            /// </summary>
            /// <param name="expr">
            /// The function to find the Taylor expansion of
            /// </param>
            /// <param name="degree">
            /// The degree of the resulting taylor polynomial
            /// </param>
            /// <param name="exprVariables">
            /// The variable/s to take the series over (and the variable in the resulting series),
            /// plus the variable values at which the Taylor polynomial will be found
            /// (e. g. if you want to find the taylor polynomial of Sin("t") around t=1, then you may want to use ("t","1") for this argument)
            /// </param>
            /// <returns>
            /// An expression in the polynomial form over the expression variable/s given in <paramref name="exprVariables"/>
            /// </returns>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using System.Linq;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.MathS.Series;
            /// 
            /// var (x, y) = MathS.Var("x", "y");
            /// Console.WriteLine(Sin(x));
            /// Console.WriteLine(Maclaurin(Sin(x), 1, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 2, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 3, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 4, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 10, x).Simplify());
            /// Console.WriteLine("----------------------");
            /// var expr = Sin(x) + Cos(y);
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Maclaurin(expr, 6, x, y).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1), (y, 5)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, "z_1", 1), (y, "z_2", 5)));
            /// Console.WriteLine("----------------------");
            /// var first3Terms = 
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(3);
            /// var first6Terms =
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(6);
            /// foreach (var term in first6Terms)
            ///     Console.WriteLine($"Received {term}");
            /// </code>
            /// Prints
            /// <code>
            /// sin(x)
            /// 0
            /// 0 + x
            /// 0 + x + 0
            /// 0 + x + 0 + -x ^ 3 / 3!
            /// x ^ 9 / 362880 - x ^ 7 / 5040 + x ^ 5 / 120 - x ^ 3 / 6 + x
            /// ----------------------
            /// sin(x) + cos(y)
            /// 1 + (6 * x ^ 5 - 120 * x ^ 3) / 720 + x + (2 * y ^ 4 - 24 * y ^ 2) / 48
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(y) + cos(1) * (x - 1) + -sin(1) * (x - 1) ^ 2 / 2! + cos(1) * (-1) * (x - 1) ^ 3 / 3! + -sin(1) * (-1) * (x - 1) ^ 4 / 4! + cos(1) * (-1) * (-1) * (x - 1) ^ 5 / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (x - 1) + -sin(5) * (y - 5) + (-sin(1) * (x - 1) ^ 2 + cos(5) * (-1) * (y - 5) ^ 2) / 2! + (cos(1) * (-1) * (x - 1) ^ 3 + -sin(5) * (-1) * (y - 5) ^ 3) / 3! + (-sin(1) * (-1) * (x - 1) ^ 4 + cos(5) * (-1) * (-1) * (y - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (x - 1) ^ 5 + -sin(5) * (-1) * (-1) * (y - 5) ^ 5) / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (z_1 - 1) + -sin(5) * (z_2 - 5) + (-sin(1) * (z_1 - 1) ^ 2 + cos(5) * (-1) * (z_2 - 5) ^ 2) / 2! + (cos(1) * (-1) * (z_1 - 1) ^ 3 + -sin(5) * (-1) * (z_2 - 5) ^ 3) / 3! + (-sin(1) * (-1) * (z_1 - 1) ^ 4 + cos(5) * (-1) * (-1) * (z_2 - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (z_1 - 1) ^ 5 + -sin(5) * (-1) * (-1) * (z_2 - 5) ^ 5) / 5!
            /// ----------------------
            /// Received cos(1)
            /// Received x + -sin(1) * (y - 1)
            /// Received cos(1) * (-1) * (y - 1) ^ 2 / 2!
            /// Received (-x ^ 3 + -sin(1) * (-1) * (y - 1) ^ 3) / 3!
            /// Received cos(1) * (-1) * (-1) * (y - 1) ^ 4 / 4!
            /// Received (x ^ 5 + -sin(1) * (-1) * (-1) * (y - 1) ^ 5) / 5!
            /// </code>
            /// </example>
            public static Entity Taylor(Entity expr, int degree, params (Variable exprVariable, Entity point)[] exprVariables)
                => Functions.Series.TaylorExpansion(expr, degree, exprVariables.Select(v => (v.exprVariable, v.exprVariable, v.point)).ToArray());

            /// <summary>
            /// Finds the symbolic expression of terms of the Taylor expansion of the given function,
            /// https://en.wikipedia.org/wiki/Taylor_series
            /// </summary>
            /// <param name="expr">
            /// The function to find the Taylor expansion of
            /// </param>
            /// <param name="degree">
            /// The degree of the resulting taylor polynomial
            /// </param>
            /// <param name="exprToPolyVars">
            /// The variable/s to take the series over, the variable the series will be over,
            /// and the variable values at which the Taylor polynomial will be found
            /// (e. g. if you want to find the taylor polynomial of Sin("t") around t=1, and want
            ///  n to take that place in the series, then you may want to use ("t","n","1") for this argument)
            /// </param>
            /// <returns>
            /// An expression in the polynomial form over the poly variable/s given in <paramref name="exprToPolyVars"/>
            /// </returns>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using System.Linq;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.MathS.Series;
            /// 
            /// var (x, y) = MathS.Var("x", "y");
            /// Console.WriteLine(Sin(x));
            /// Console.WriteLine(Maclaurin(Sin(x), 1, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 2, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 3, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 4, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 10, x).Simplify());
            /// Console.WriteLine("----------------------");
            /// var expr = Sin(x) + Cos(y);
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Maclaurin(expr, 6, x, y).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1), (y, 5)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, "z_1", 1), (y, "z_2", 5)));
            /// Console.WriteLine("----------------------");
            /// var first3Terms = 
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(3);
            /// var first6Terms =
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(6);
            /// foreach (var term in first6Terms)
            ///     Console.WriteLine($"Received {term}");
            /// </code>
            /// Prints
            /// <code>
            /// sin(x)
            /// 0
            /// 0 + x
            /// 0 + x + 0
            /// 0 + x + 0 + -x ^ 3 / 3!
            /// x ^ 9 / 362880 - x ^ 7 / 5040 + x ^ 5 / 120 - x ^ 3 / 6 + x
            /// ----------------------
            /// sin(x) + cos(y)
            /// 1 + (6 * x ^ 5 - 120 * x ^ 3) / 720 + x + (2 * y ^ 4 - 24 * y ^ 2) / 48
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(y) + cos(1) * (x - 1) + -sin(1) * (x - 1) ^ 2 / 2! + cos(1) * (-1) * (x - 1) ^ 3 / 3! + -sin(1) * (-1) * (x - 1) ^ 4 / 4! + cos(1) * (-1) * (-1) * (x - 1) ^ 5 / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (x - 1) + -sin(5) * (y - 5) + (-sin(1) * (x - 1) ^ 2 + cos(5) * (-1) * (y - 5) ^ 2) / 2! + (cos(1) * (-1) * (x - 1) ^ 3 + -sin(5) * (-1) * (y - 5) ^ 3) / 3! + (-sin(1) * (-1) * (x - 1) ^ 4 + cos(5) * (-1) * (-1) * (y - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (x - 1) ^ 5 + -sin(5) * (-1) * (-1) * (y - 5) ^ 5) / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (z_1 - 1) + -sin(5) * (z_2 - 5) + (-sin(1) * (z_1 - 1) ^ 2 + cos(5) * (-1) * (z_2 - 5) ^ 2) / 2! + (cos(1) * (-1) * (z_1 - 1) ^ 3 + -sin(5) * (-1) * (z_2 - 5) ^ 3) / 3! + (-sin(1) * (-1) * (z_1 - 1) ^ 4 + cos(5) * (-1) * (-1) * (z_2 - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (z_1 - 1) ^ 5 + -sin(5) * (-1) * (-1) * (z_2 - 5) ^ 5) / 5!
            /// ----------------------
            /// Received cos(1)
            /// Received x + -sin(1) * (y - 1)
            /// Received cos(1) * (-1) * (y - 1) ^ 2 / 2!
            /// Received (-x ^ 3 + -sin(1) * (-1) * (y - 1) ^ 3) / 3!
            /// Received cos(1) * (-1) * (-1) * (y - 1) ^ 4 / 4!
            /// Received (x ^ 5 + -sin(1) * (-1) * (-1) * (y - 1) ^ 5) / 5!
            /// </code>
            /// </example>
            public static Entity Taylor(Entity expr, int degree, params (Variable exprVariable, Variable polyVariable, Entity point)[] exprToPolyVars)
                => Functions.Series.TaylorExpansion(expr, degree, exprToPolyVars);

            /// <summary>
            /// Finds the symbolic expression of terms of the Taylor expansion of the given function,
            /// https://en.wikipedia.org/wiki/Taylor_series
            /// 
            /// Do NOT call ToArray() or anything like this on the result of this method. It is 
            /// infinite iterator. To get a finite result as in a sum of finite number of terms,
            /// call TaylorExpansion
            /// </summary>
            /// <param name="expr">
            /// The function to find the Taylor expansion of
            /// </param>
            /// <param name="exprToPolyVars">
            /// The variable/s to take the series over, the variable the series will be over,
            /// and the variable values at which the Taylor polynomial will be found
            /// (e. g. if you want to find the taylor polynomial of Sin("t") around t=1, and want
            ///  n to take that place in the series, then you may want to use ("t","n","1") for this argument)
            /// </param>
            /// <returns>
            /// An infinite iterator over the terms of Taylor series of the given expression.
            /// </returns>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using System.Linq;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.MathS.Series;
            /// 
            /// var (x, y) = MathS.Var("x", "y");
            /// Console.WriteLine(Sin(x));
            /// Console.WriteLine(Maclaurin(Sin(x), 1, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 2, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 3, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 4, x));
            /// Console.WriteLine(Maclaurin(Sin(x), 10, x).Simplify());
            /// Console.WriteLine("----------------------");
            /// var expr = Sin(x) + Cos(y);
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Maclaurin(expr, 6, x, y).Simplify());
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, 1), (y, 5)));
            /// Console.WriteLine("----------------------");
            /// Console.WriteLine(expr);
            /// Console.WriteLine(Taylor(expr, 6, (x, "z_1", 1), (y, "z_2", 5)));
            /// Console.WriteLine("----------------------");
            /// var first3Terms = 
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(3);
            /// var first6Terms =
            ///     TaylorTerms(expr, (x, x, 0), (y, y, 1))
            ///     .Take(6);
            /// foreach (var term in first6Terms)
            ///     Console.WriteLine($"Received {term}");
            /// </code>
            /// Prints
            /// <code>
            /// sin(x)
            /// 0
            /// 0 + x
            /// 0 + x + 0
            /// 0 + x + 0 + -x ^ 3 / 3!
            /// x ^ 9 / 362880 - x ^ 7 / 5040 + x ^ 5 / 120 - x ^ 3 / 6 + x
            /// ----------------------
            /// sin(x) + cos(y)
            /// 1 + (6 * x ^ 5 - 120 * x ^ 3) / 720 + x + (2 * y ^ 4 - 24 * y ^ 2) / 48
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(y) + cos(1) * (x - 1) + -sin(1) * (x - 1) ^ 2 / 2! + cos(1) * (-1) * (x - 1) ^ 3 / 3! + -sin(1) * (-1) * (x - 1) ^ 4 / 4! + cos(1) * (-1) * (-1) * (x - 1) ^ 5 / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (x - 1) + -sin(5) * (y - 5) + (-sin(1) * (x - 1) ^ 2 + cos(5) * (-1) * (y - 5) ^ 2) / 2! + (cos(1) * (-1) * (x - 1) ^ 3 + -sin(5) * (-1) * (y - 5) ^ 3) / 3! + (-sin(1) * (-1) * (x - 1) ^ 4 + cos(5) * (-1) * (-1) * (y - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (x - 1) ^ 5 + -sin(5) * (-1) * (-1) * (y - 5) ^ 5) / 5!
            /// ----------------------
            /// sin(x) + cos(y)
            /// sin(1) + cos(5) + cos(1) * (z_1 - 1) + -sin(5) * (z_2 - 5) + (-sin(1) * (z_1 - 1) ^ 2 + cos(5) * (-1) * (z_2 - 5) ^ 2) / 2! + (cos(1) * (-1) * (z_1 - 1) ^ 3 + -sin(5) * (-1) * (z_2 - 5) ^ 3) / 3! + (-sin(1) * (-1) * (z_1 - 1) ^ 4 + cos(5) * (-1) * (-1) * (z_2 - 5) ^ 4) / 4! + (cos(1) * (-1) * (-1) * (z_1 - 1) ^ 5 + -sin(5) * (-1) * (-1) * (z_2 - 5) ^ 5) / 5!
            /// ----------------------
            /// Received cos(1)
            /// Received x + -sin(1) * (y - 1)
            /// Received cos(1) * (-1) * (y - 1) ^ 2 / 2!
            /// Received (-x ^ 3 + -sin(1) * (-1) * (y - 1) ^ 3) / 3!
            /// Received cos(1) * (-1) * (-1) * (y - 1) ^ 4 / 4!
            /// Received (x ^ 5 + -sin(1) * (-1) * (-1) * (y - 1) ^ 5) / 5!
            /// </code>
            /// </example>
            public static IEnumerable<Entity> TaylorTerms(Entity expr, params (Variable exprVariable, Variable polyVariable, Entity point)[] exprToPolyVars)
            {
                if (exprToPolyVars.Length == 1)
                    return Functions.Series.SingleTaylorExpansionTerms(expr, exprToPolyVars[0].exprVariable, exprToPolyVars[0].polyVariable, exprToPolyVars[0].point);
                else
                    return Functions.Series.MultivariableTaylorExpansionTerms(expr, exprToPolyVars);
            }

        }


        /// <summary>https://en.wikipedia.org/wiki/Logical_disjunction</summary>
        /// <param name="a">The left argument node of which Disjunction function will be taken</param>
        /// <param name="b">The right argument node of which Disjunction function will be taken</param>
        /// <returns>Or node</returns>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var myXor = Disjunction(Conjunction(x, Negation(y)), Conjunction(y, Negation(x)));
        /// Console.WriteLine(myXor);
        /// Console.WriteLine(MathS.Boolean.BuildTruthTable(myXor, x, y).ToString(multilineFormat: true));
        /// Console.WriteLine("------------------");
        /// var expr = ExclusiveDisjunction(Implication(x, y), Implication(y, x));
        /// Console.WriteLine(expr);
        /// Console.WriteLine("------------------");
        /// var expr2 = Conjunction(x, Conjunction(x, y));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// x and not y or y and not x
        /// Matrix[4 x 3]
        /// False   False   False   
        /// False   True    True    
        /// True    False   True    
        /// True    True    False   
        /// ------------------
        /// (x implies y) xor (y implies x)
        /// ------------------
        /// x and x and y
        /// x and y
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Disjunction(Entity a, Entity b) => a | b;

        /// <summary>https://en.wikipedia.org/wiki/Logical_conjunction</summary>
        /// <param name="a">Left argument node of which Conjunction function will be taken</param>
        /// <param name="b">Right argument node of which Conjunction disjunction function will be taken</param>
        /// <returns>And node</returns>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var myXor = Disjunction(Conjunction(x, Negation(y)), Conjunction(y, Negation(x)));
        /// Console.WriteLine(myXor);
        /// Console.WriteLine(MathS.Boolean.BuildTruthTable(myXor, x, y).ToString(multilineFormat: true));
        /// Console.WriteLine("------------------");
        /// var expr = ExclusiveDisjunction(Implication(x, y), Implication(y, x));
        /// Console.WriteLine(expr);
        /// Console.WriteLine("------------------");
        /// var expr2 = Conjunction(x, Conjunction(x, y));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// x and not y or y and not x
        /// Matrix[4 x 3]
        /// False   False   False   
        /// False   True    True    
        /// True    False   True    
        /// True    True    False   
        /// ------------------
        /// (x implies y) xor (y implies x)
        /// ------------------
        /// x and x and y
        /// x and y
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Conjunction(Entity a, Entity b) => a & b;

        /// <summary>https://en.wikipedia.org/wiki/Material_implication_(rule_of_inference)</summary>
        /// <param name="assumption">The assumption node</param>
        /// <param name="conclusion">The conclusion node</param>
        /// <returns>Implies node</returns>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var myXor = Disjunction(Conjunction(x, Negation(y)), Conjunction(y, Negation(x)));
        /// Console.WriteLine(myXor);
        /// Console.WriteLine(MathS.Boolean.BuildTruthTable(myXor, x, y).ToString(multilineFormat: true));
        /// Console.WriteLine("------------------");
        /// var expr = ExclusiveDisjunction(Implication(x, y), Implication(y, x));
        /// Console.WriteLine(expr);
        /// Console.WriteLine("------------------");
        /// var expr2 = Conjunction(x, Conjunction(x, y));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// x and not y or y and not x
        /// Matrix[4 x 3]
        /// False   False   False   
        /// False   True    True    
        /// True    False   True    
        /// True    True    False   
        /// ------------------
        /// (x implies y) xor (y implies x)
        /// ------------------
        /// x and x and y
        /// x and y
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Implication(Entity assumption, Entity conclusion) => assumption.Implies(conclusion);

        /// <summary>https://en.wikipedia.org/wiki/Exclusive_or</summary>
        /// <param name="a">Left argument node of which Exclusive disjunction function will be taken</param>
        /// <param name="b">Right argument node of which Exclusive disjunction function will be taken</param>
        /// <returns>Xor node</returns>
        /// <example>
        /// <code>
        /// using AngouriMath;
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var myXor = Disjunction(Conjunction(x, Negation(y)), Conjunction(y, Negation(x)));
        /// Console.WriteLine(myXor);
        /// Console.WriteLine(MathS.Boolean.BuildTruthTable(myXor, x, y).ToString(multilineFormat: true));
        /// Console.WriteLine("------------------");
        /// var expr = ExclusiveDisjunction(Implication(x, y), Implication(y, x));
        /// Console.WriteLine(expr);
        /// Console.WriteLine("------------------");
        /// var expr2 = Conjunction(x, Conjunction(x, y));
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// x and not y or y and not x
        /// Matrix[4 x 3]
        /// False   False   False   
        /// False   True    True    
        /// True    False   True    
        /// True    True    False   
        /// ------------------
        /// (x implies y) xor (y implies x)
        /// ------------------
        /// x and x and y
        /// x and y
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity ExclusiveDisjunction(Entity a, Entity b) => a ^ b;

        /// <summary>
        /// Do NOT confuse it with Equation
        /// </summary>
        /// <param name="a">Left argument node of which Equality function will be taken</param>
        /// <param name="b">Right argument node of which Equality disjunction function will be taken</param>
        /// <returns>An Equals node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var equation = Equality(Sqrt(x), 4 * x - 3);
        /// Console.WriteLine(equation);
        /// Console.WriteLine(equation.Solve(x));
        /// Console.WriteLine("----------------------");
        /// var statement1 = Equality(5, 10);
        /// Console.WriteLine(statement1);
        /// Console.WriteLine((bool)statement1.EvalBoolean());
        /// Console.WriteLine("----------------------");
        /// var statement2 = Equality(5, 5);
        /// Console.WriteLine(statement2);
        /// Console.WriteLine((bool)statement2.EvalBoolean());
        /// Console.WriteLine("----------------------");
        /// var statement3 = Equality(x, y);
        /// Console.WriteLine(statement3);
        /// Console.WriteLine(statement3.Simplify());
        /// // throws here!
        /// Console.WriteLine((bool)statement3.EvalBoolean());
        /// </code>
        /// Prints
        /// <code>
        /// sqrt(x) = 4 * x - 3
        /// { 9/16, 1 }
        /// ----------------------
        /// 5 = 10
        /// False
        /// ----------------------
        /// 5 = 5
        /// True
        /// ----------------------
        /// x = y
        /// x = y
        /// Unhandled exception. AngouriMath.Core.Exceptions.CannotEvalException
        /// </code>
        /// </example>
        public static Entity Equality(Entity a, Entity b) => a.EqualTo(b);

        /// <param name="a">Left argument node of which the greater than node will be taken</param>
        /// <param name="b">Right argument node of which the greater than node function will be taken</param>
        /// <returns>A node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// Console.WriteLine(GreaterThan(x, y));
        /// Console.WriteLine(GreaterThan(6, 5));
        /// Console.WriteLine(GreaterThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessThan(x, y));
        /// Console.WriteLine(LessThan(6, 5));
        /// Console.WriteLine(LessThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(GreaterOrEqualThan(x, y));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessOrEqualThan(x, y));
        /// Console.WriteLine(LessOrEqualThan(6, 5));
        /// Console.WriteLine(LessOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// var statement1 = GreaterThan(Sqr(x), 5);
        /// Console.WriteLine(statement1);
        /// Console.WriteLine(statement1.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement2 = GreaterThan(Sqr(x), 16) &amp; LessThan(x, y);
        /// Console.WriteLine(statement2);
        /// Console.WriteLine(statement2.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement3 = LessThan(Sqr(x), 16) &amp; GreaterThan(x, 2);
        /// Console.WriteLine(statement3);
        /// Console.WriteLine(statement3.Solve("x"));
        /// </code>
        /// Prints
        /// <code>
        /// x &gt; y
        /// 6 &gt; 5
        /// True
        /// False
        /// ----------------------------------
        /// x &lt; y
        /// 6 &lt; 5
        /// False
        /// False
        /// ----------------------------------
        /// x &gt;= y
        /// 6 &gt;= 5
        /// True
        /// True
        /// ----------------------------------
        /// x &lt;= y
        /// 6 &lt;= 5
        /// False
        /// True
        /// ----------------------------------
        /// x ^ 2 &gt; 5
        /// (-oo; -sqrt(20) / 2) \/ (sqrt(20) / 2; +oo)
        /// ----------------------------------
        /// x ^ 2 &gt; 16 and x &lt; y
        /// ((-oo; -4) \/ (4; +oo)) /\ (-oo; -y / (-1))
        /// ----------------------------------
        /// x ^ 2 &lt; 16 and x &gt; 2
        /// (2; 4)
        /// </code>
        /// </example>
        public static Entity GreaterThan(Entity a, Entity b) => a > b;

        /// <param name="a">Left argument node of which the less than node will be taken</param>
        /// <param name="b">Right argument node of which the less than node function will be taken</param>
        /// <returns>A node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// Console.WriteLine(GreaterThan(x, y));
        /// Console.WriteLine(GreaterThan(6, 5));
        /// Console.WriteLine(GreaterThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessThan(x, y));
        /// Console.WriteLine(LessThan(6, 5));
        /// Console.WriteLine(LessThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(GreaterOrEqualThan(x, y));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessOrEqualThan(x, y));
        /// Console.WriteLine(LessOrEqualThan(6, 5));
        /// Console.WriteLine(LessOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// var statement1 = GreaterThan(Sqr(x), 5);
        /// Console.WriteLine(statement1);
        /// Console.WriteLine(statement1.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement2 = GreaterThan(Sqr(x), 16) &amp; LessThan(x, y);
        /// Console.WriteLine(statement2);
        /// Console.WriteLine(statement2.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement3 = LessThan(Sqr(x), 16) &amp; GreaterThan(x, 2);
        /// Console.WriteLine(statement3);
        /// Console.WriteLine(statement3.Solve("x"));
        /// </code>
        /// Prints
        /// <code>
        /// x &gt; y
        /// 6 &gt; 5
        /// True
        /// False
        /// ----------------------------------
        /// x &lt; y
        /// 6 &lt; 5
        /// False
        /// False
        /// ----------------------------------
        /// x &gt;= y
        /// 6 &gt;= 5
        /// True
        /// True
        /// ----------------------------------
        /// x &lt;= y
        /// 6 &lt;= 5
        /// False
        /// True
        /// ----------------------------------
        /// x ^ 2 &gt; 5
        /// (-oo; -sqrt(20) / 2) \/ (sqrt(20) / 2; +oo)
        /// ----------------------------------
        /// x ^ 2 &gt; 16 and x &lt; y
        /// ((-oo; -4) \/ (4; +oo)) /\ (-oo; -y / (-1))
        /// ----------------------------------
        /// x ^ 2 &lt; 16 and x &gt; 2
        /// (2; 4)
        /// </code>
        /// </example>
        public static Entity LessThan(Entity a, Entity b) => a < b;

        /// <param name="a">Left argument node of which the greter than or equal node will be taken</param>
        /// <param name="b">Right argument node of which the greater than or equal node function will be taken</param>
        /// <returns>A node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// Console.WriteLine(GreaterThan(x, y));
        /// Console.WriteLine(GreaterThan(6, 5));
        /// Console.WriteLine(GreaterThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessThan(x, y));
        /// Console.WriteLine(LessThan(6, 5));
        /// Console.WriteLine(LessThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(GreaterOrEqualThan(x, y));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessOrEqualThan(x, y));
        /// Console.WriteLine(LessOrEqualThan(6, 5));
        /// Console.WriteLine(LessOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// var statement1 = GreaterThan(Sqr(x), 5);
        /// Console.WriteLine(statement1);
        /// Console.WriteLine(statement1.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement2 = GreaterThan(Sqr(x), 16) &amp; LessThan(x, y);
        /// Console.WriteLine(statement2);
        /// Console.WriteLine(statement2.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement3 = LessThan(Sqr(x), 16) &amp; GreaterThan(x, 2);
        /// Console.WriteLine(statement3);
        /// Console.WriteLine(statement3.Solve("x"));
        /// </code>
        /// Prints
        /// <code>
        /// x &gt; y
        /// 6 &gt; 5
        /// True
        /// False
        /// ----------------------------------
        /// x &lt; y
        /// 6 &lt; 5
        /// False
        /// False
        /// ----------------------------------
        /// x &gt;= y
        /// 6 &gt;= 5
        /// True
        /// True
        /// ----------------------------------
        /// x &lt;= y
        /// 6 &lt;= 5
        /// False
        /// True
        /// ----------------------------------
        /// x ^ 2 &gt; 5
        /// (-oo; -sqrt(20) / 2) \/ (sqrt(20) / 2; +oo)
        /// ----------------------------------
        /// x ^ 2 &gt; 16 and x &lt; y
        /// ((-oo; -4) \/ (4; +oo)) /\ (-oo; -y / (-1))
        /// ----------------------------------
        /// x ^ 2 &lt; 16 and x &gt; 2
        /// (2; 4)
        /// </code>
        /// </example>
        public static Entity GreaterOrEqualThan(Entity a, Entity b) => a >= b;

        /// <param name="a">Left argument node of which the less than or equal node will be taken</param>
        /// <param name="b">Right argument node of which the less than or equal node function will be taken</param>
        /// <returns>A node</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// 
        /// Console.WriteLine(GreaterThan(x, y));
        /// Console.WriteLine(GreaterThan(6, 5));
        /// Console.WriteLine(GreaterThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessThan(x, y));
        /// Console.WriteLine(LessThan(6, 5));
        /// Console.WriteLine(LessThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(GreaterOrEqualThan(x, y));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5));
        /// Console.WriteLine(GreaterOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(GreaterOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// Console.WriteLine(LessOrEqualThan(x, y));
        /// Console.WriteLine(LessOrEqualThan(6, 5));
        /// Console.WriteLine(LessOrEqualThan(6, 5).EvalBoolean());
        /// Console.WriteLine(LessOrEqualThan(6, 6).EvalBoolean());
        /// Console.WriteLine("----------------------------------");
        /// var statement1 = GreaterThan(Sqr(x), 5);
        /// Console.WriteLine(statement1);
        /// Console.WriteLine(statement1.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement2 = GreaterThan(Sqr(x), 16) &amp; LessThan(x, y);
        /// Console.WriteLine(statement2);
        /// Console.WriteLine(statement2.Solve("x"));
        /// Console.WriteLine("----------------------------------");
        /// var statement3 = LessThan(Sqr(x), 16) &amp; GreaterThan(x, 2);
        /// Console.WriteLine(statement3);
        /// Console.WriteLine(statement3.Solve("x"));
        /// </code>
        /// Prints
        /// <code>
        /// x &gt; y
        /// 6 &gt; 5
        /// True
        /// False
        /// ----------------------------------
        /// x &lt; y
        /// 6 &lt; 5
        /// False
        /// False
        /// ----------------------------------
        /// x &gt;= y
        /// 6 &gt;= 5
        /// True
        /// True
        /// ----------------------------------
        /// x &lt;= y
        /// 6 &lt;= 5
        /// False
        /// True
        /// ----------------------------------
        /// x ^ 2 &gt; 5
        /// (-oo; -sqrt(20) / 2) \/ (sqrt(20) / 2; +oo)
        /// ----------------------------------
        /// x ^ 2 &gt; 16 and x &lt; y
        /// ((-oo; -4) \/ (4; +oo)) /\ (-oo; -y / (-1))
        /// ----------------------------------
        /// x ^ 2 &lt; 16 and x &gt; 2
        /// (2; 4)
        /// </code>
        /// </example>
        public static Entity LessOrEqualThan(Entity a, Entity b) => a <= b;

        /// <param name="a">Left argument node of which the union set node will be taken</param>
        /// <param name="b">Right argument node of which the union set node will be taken</param>
        /// <returns>A node</returns>
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
        public static Set Union(Entity a, Entity b) => a.Unite(b);

        /// <param name="a">Left argument node of which the intersection set node will be taken</param>
        /// <param name="b">Right argument node of which the intersection set node will be taken</param>
        /// <returns>A node</returns>
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
        public static Set Intersection(Entity a, Entity b) => a.Intersect(b);

        /// <param name="a">
        /// Left argument node of which the set subtraction node will be taken
        /// That is, the resulting set of set subtraction is necessarily superset of this set
        /// </param>
        /// <param name="b">
        /// Right argument node of which the set subtraction set node will be taken
        /// That is, there is no element in the resulting set that belong to this one
        /// </param>
        /// <returns>A node</returns>
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
        public static Set SetSubtraction(Entity a, Entity b) => a.SetSubtract(b);

        /// <summary>Creates an instance of <see cref="Variable"/>.</summary>
        /// <param name="name">The name of the <see cref="Variable"/> which equality is based on.</param>
        /// <returns>Variable node</returns>
        /// <example>
        /// Here are multiple ways to create variables:
        /// <code>
        /// Entity.Variable x = "x";
        /// var y = Var("y");
        /// var (a, b) = Var("x", "y");
        /// var (c, d, e) = Var("a", "b", "c");
        /// var f = Var("f_1");
        /// var alpha = Var("alpha");
        /// var alphaPhi = Var("alpha_phi");
        /// var alpha1 = Var("alpha_1");
        /// var inGreek = Var("βγδεζη_кΨеВеТкΩ");
        /// var inCyrillic = Var("Абаваола");
        /// </code>
        /// Underscore "_" allows indexing. Greek letter names (e. g.
        /// "alpha") will be latexized as Greek letter (but other than that,
        /// will appear as "alpha" in other places).
        /// </example>
        public static Variable Var(string name) => name;
        
        /// <summary>Creates two instances of <see cref="Variable"/>.</summary>
        /// <returns>A tuple of 2 corresponding variable nodes</returns>
        /// <example>
        /// Here are multiple ways to create variables:
        /// <code>
        /// Entity.Variable x = "x";
        /// var y = Var("y");
        /// var (a, b) = Var("x", "y");
        /// var (c, d, e) = Var("a", "b", "c");
        /// var f = Var("f_1");
        /// var alpha = Var("alpha");
        /// var alphaPhi = Var("alpha_phi");
        /// var alpha1 = Var("alpha_1");
        /// var inGreek = Var("βγδεζη_кΨеВеТкΩ");
        /// var inCyrillic = Var("Абаваола");
        /// </code>
        /// Underscore "_" allows indexing. Greek letter names (e. g.
        /// "alpha") will be latexized as Greek letter (but other than that,
        /// will appear as "alpha" in other places).
        /// </example>
        public static (Variable, Variable) Var(string name1, string name2) => (Var(name1), Var(name2));
        
        /// <summary>Creates three instances of <see cref="Variable"/>.</summary>
        /// <returns>A tuple of 3 corresponding variable nodes</returns>
        /// <example>
        /// Here are multiple ways to create variables:
        /// <code>
        /// Entity.Variable x = "x";
        /// var y = Var("y");
        /// var (a, b) = Var("x", "y");
        /// var (c, d, e) = Var("a", "b", "c");
        /// var f = Var("f_1");
        /// var alpha = Var("alpha");
        /// var alphaPhi = Var("alpha_phi");
        /// var alpha1 = Var("alpha_1");
        /// var inGreek = Var("βγδεζη_кΨеВеТкΩ");
        /// var inCyrillic = Var("Абаваола");
        /// </code>
        /// Underscore "_" allows indexing. Greek letter names (e. g.
        /// "alpha") will be latexized as Greek letter (but other than that,
        /// will appear as "alpha" in other places).
        /// </example>
        public static (Variable, Variable, Variable) Var(string name1, string name2, string name3)
            => (Var(name1), Var(name2), Var(name3));
        
        /// <summary>Creates three instances of <see cref="Variable"/>.</summary>
        /// <returns>A tuple of 3 corresponding variable nodes</returns>
        /// <example>
        /// Here are multiple ways to create variables:
        /// <code>
        /// Entity.Variable x = "x";
        /// var y = Var("y");
        /// var (a, b) = Var("x", "y");
        /// var (c, d, e) = Var("a", "b", "c");
        /// var (c1, d1, e1, f1) = Var("c_1", "d_1", "e_1", "f_1");
        /// var (c1, d1, e1, f1, g1) = Var("c_1", "d_1", "e_1", "f_1", "g_1");
        /// var f = Var("f_1");
        /// var alpha = Var("alpha");
        /// var alphaPhi = Var("alpha_phi");
        /// var alpha1 = Var("alpha_1");
        /// var inGreek = Var("βγδεζη_кΨеВеТкΩ");
        /// var inCyrillic = Var("Абаваола");
        /// </code>
        /// Underscore "_" allows indexing. Greek letter names (e. g.
        /// "alpha") will be latexized as Greek letter (but other than that,
        /// will appear as "alpha" in other places).
        /// </example>
        public static (Variable, Variable, Variable, Variable) Var(string name1, string name2, string name3, string name4)
            => (Var(name1), Var(name2), Var(name3), Var(name4));
        
        /// <summary>Creates three instances of <see cref="Variable"/>.</summary>
        /// <returns>A tuple of 3 corresponding variable nodes</returns>
        /// <example>
        /// Here are multiple ways to create variables:
        /// <code>
        /// Entity.Variable x = "x";
        /// var y = Var("y");
        /// var (a, b) = Var("x", "y");
        /// var (c, d, e) = Var("a", "b", "c");
        /// var (c1, d1, e1, f1) = Var("c_1", "d_1", "e_1", "f_1");
        /// var (c1, d1, e1, f1, g1) = Var("c_1", "d_1", "e_1", "f_1", "g_1");
        /// var f = Var("f_1");
        /// var alpha = Var("alpha");
        /// var alphaPhi = Var("alpha_phi");
        /// var alpha1 = Var("alpha_1");
        /// var inGreek = Var("βγδεζη_кΨеВеТкΩ");
        /// var inCyrillic = Var("Абаваола");
        /// </code>
        /// Underscore "_" allows indexing. Greek letter names (e. g.
        /// "alpha") will be latexized as Greek letter (but other than that,
        /// will appear as "alpha" in other places).
        /// </example>
        public static (Variable, Variable, Variable, Variable, Variable) Var(string name1, string name2, string name3, string name4, string name5)
            => (Var(name1), Var(name2), Var(name3), Var(name4), Var(name5));

        /// <summary>
        /// Infinity. Recommended to use with a plus or minus trailing.
        /// </summary>
        /// <example>
        /// Let's consider <see cref="MathS.oo"/> and <see cref="MathS.NaN"/>.
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Sin(x) / y;
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Substitute(y, 0));
        /// Console.WriteLine(expr1.Substitute(y, 0).Evaled);
        /// Console.WriteLine(expr1.Substitute(y, 0).Evaled == NaN);
        /// Console.WriteLine("--------------------------------");
        /// var expr2 = 5 + NaN;
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Evaled);
        /// Console.WriteLine("--------------------------------");
        /// var expr3 = Sin(NaN) / Cos(x) + y;
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Evaled);
        /// Console.WriteLine("--------------------------------");
        /// var expr4 = 10 * +oo;
        /// Console.WriteLine(expr4);
        /// Console.WriteLine("--------------------------------");
        /// var expr5 = -oo * +oo;
        /// Console.WriteLine(expr5);
        /// Console.WriteLine("--------------------------------");
        /// var expr6 = -oo / +oo;
        /// Console.WriteLine(expr6);
        /// Console.WriteLine("--------------------------------");
        /// var expr7 = 50 / -oo;
        /// Console.WriteLine(expr7);
        /// </code>
        /// Prints
        /// <code>
        /// sin(x) / y
        /// sin(x) / 0
        /// NaN
        /// True
        /// --------------------------------
        /// 5 + NaN
        /// NaN
        /// --------------------------------
        /// sin(NaN) / cos(x) + y
        /// NaN
        /// --------------------------------
        /// +oo
        /// --------------------------------
        /// -oo
        /// --------------------------------
        /// NaN
        /// --------------------------------
        /// 0
        /// </code>
        /// See <see cref="Entity.IsFinite"/> for determining
        /// if there are NaNs or infinities inside an expression.
        /// </example>
        [ConstantField] public static readonly Real oo = (Real)(Entity)"+oo";

        /// <summary>
        /// The e constant
        /// <a href="https://en.wikipedia.org/wiki/E_(mathematical_constant)"/>
        /// </summary>
        /// <example>
        /// There are multiple constants available. Examples:
        /// <code>
        /// var x = Var("x");
        /// Console.WriteLine(e);
        /// Console.WriteLine(e.Evaled);
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo));
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify());
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify() == e);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(pi);
        /// Console.WriteLine(pi.Evaled);
        /// Console.WriteLine(Sin(pi / 3).Simplify());
        /// Console.WriteLine(Cos(pi / 3).Simplify());
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(i);
        /// Console.WriteLine(4 + 3 * i);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(e.Pow(i * pi));
        /// Console.WriteLine(e.Pow(i * pi).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// e
        /// 2.718281828459045235360287471352662497757247093699959574966967627724076630353547594571382178525166427
        /// limit((1 + 1 / x) ^ x, x, +oo)
        /// e
        /// True
        /// -----------------------
        /// pi
        /// 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068
        /// sqrt(3) / 2
        /// 1/2
        /// -----------------------
        /// i
        /// 4 + 3i
        /// -----------------------
        /// e ^ (i * pi)
        /// -1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Variable e = Variable.e;

        /// <summary>
        /// The imaginary one
        /// <a href="https://en.wikipedia.org/wiki/Imaginary_unit"/>
        /// </summary>
        /// <example>
        /// There are multiple constants available. Examples:
        /// <code>
        /// var x = Var("x");
        /// Console.WriteLine(e);
        /// Console.WriteLine(e.Evaled);
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo));
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify());
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify() == e);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(pi);
        /// Console.WriteLine(pi.Evaled);
        /// Console.WriteLine(Sin(pi / 3).Simplify());
        /// Console.WriteLine(Cos(pi / 3).Simplify());
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(i);
        /// Console.WriteLine(4 + 3 * i);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(e.Pow(i * pi));
        /// Console.WriteLine(e.Pow(i * pi).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// e
        /// 2.718281828459045235360287471352662497757247093699959574966967627724076630353547594571382178525166427
        /// limit((1 + 1 / x) ^ x, x, +oo)
        /// e
        /// True
        /// -----------------------
        /// pi
        /// 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068
        /// sqrt(3) / 2
        /// 1/2
        /// -----------------------
        /// i
        /// 4 + 3i
        /// -----------------------
        /// e ^ (i * pi)
        /// -1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Complex i = Complex.ImaginaryOne;

        /// <summary>
        /// The pi constant
        /// <a href="https://en.wikipedia.org/wiki/Pi"/>
        /// </summary>
        /// <example>
        /// There are multiple constants available. Examples:
        /// <code>
        /// var x = Var("x");
        /// Console.WriteLine(e);
        /// Console.WriteLine(e.Evaled);
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo));
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify());
        /// Console.WriteLine(Limit((1 + 1 / x).Pow(x), x, +oo).Simplify() == e);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(pi);
        /// Console.WriteLine(pi.Evaled);
        /// Console.WriteLine(Sin(pi / 3).Simplify());
        /// Console.WriteLine(Cos(pi / 3).Simplify());
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(i);
        /// Console.WriteLine(4 + 3 * i);
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(e.Pow(i * pi));
        /// Console.WriteLine(e.Pow(i * pi).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// e
        /// 2.718281828459045235360287471352662497757247093699959574966967627724076630353547594571382178525166427
        /// limit((1 + 1 / x) ^ x, x, +oo)
        /// e
        /// True
        /// -----------------------
        /// pi
        /// 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117068
        /// sqrt(3) / 2
        /// 1/2
        /// -----------------------
        /// i
        /// 4 + 3i
        /// -----------------------
        /// e ^ (i * pi)
        /// -1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Variable pi = Variable.pi;

        // Undefined
        /// <summary>
        /// NaN represents both "undefined" and "indeterminate".
        /// Any operation on NaN returns NaN
        /// </summary>
        /// <example>
        /// Let's consider <see cref="MathS.oo"/> and <see cref="MathS.NaN"/>.
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Sin(x) / y;
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Substitute(y, 0));
        /// Console.WriteLine(expr1.Substitute(y, 0).Evaled);
        /// Console.WriteLine(expr1.Substitute(y, 0).Evaled == NaN);
        /// Console.WriteLine("--------------------------------");
        /// var expr2 = 5 + NaN;
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(expr2.Evaled);
        /// Console.WriteLine("--------------------------------");
        /// var expr3 = Sin(NaN) / Cos(x) + y;
        /// Console.WriteLine(expr3);
        /// Console.WriteLine(expr3.Evaled);
        /// Console.WriteLine("--------------------------------");
        /// var expr4 = 10 * +oo;
        /// Console.WriteLine(expr4);
        /// Console.WriteLine("--------------------------------");
        /// var expr5 = -oo * +oo;
        /// Console.WriteLine(expr5);
        /// Console.WriteLine("--------------------------------");
        /// var expr6 = -oo / +oo;
        /// Console.WriteLine(expr6);
        /// Console.WriteLine("--------------------------------");
        /// var expr7 = 50 / -oo;
        /// Console.WriteLine(expr7);
        /// </code>
        /// Prints
        /// <code>
        /// sin(x) / y
        /// sin(x) / 0
        /// NaN
        /// True
        /// --------------------------------
        /// 5 + NaN
        /// NaN
        /// --------------------------------
        /// sin(NaN) / cos(x) + y
        /// NaN
        /// --------------------------------
        /// +oo
        /// --------------------------------
        /// -oo
        /// --------------------------------
        /// NaN
        /// --------------------------------
        /// 0
        /// </code>
        /// See <see cref="Entity.IsFinite"/> for determining
        /// if there are NaNs or infinities inside an expression.
        /// </example>
        [ConstantField] public static readonly Entity NaN = Real.NaN;

        /// <summary>
        /// The square identity matrix of size 1
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(I_1.ToString(multilineFormat: true));
        /// Console.WriteLine(I_2.ToString(multilineFormat: true));
        /// Console.WriteLine(I_3.ToString(multilineFormat: true));
        /// Console.WriteLine(I_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 1   
        /// Matrix[2 x 2]
        /// 1   0   
        /// 0   1   
        /// Matrix[3 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix I_1 = IdentityMatrix(1);

        /// <summary>
        /// The square identity matrix of size 2
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(I_1.ToString(multilineFormat: true));
        /// Console.WriteLine(I_2.ToString(multilineFormat: true));
        /// Console.WriteLine(I_3.ToString(multilineFormat: true));
        /// Console.WriteLine(I_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 1   
        /// Matrix[2 x 2]
        /// 1   0   
        /// 0   1   
        /// Matrix[3 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix I_2 = IdentityMatrix(2);

        /// <summary>
        /// The square identity matrix of size 3
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(I_1.ToString(multilineFormat: true));
        /// Console.WriteLine(I_2.ToString(multilineFormat: true));
        /// Console.WriteLine(I_3.ToString(multilineFormat: true));
        /// Console.WriteLine(I_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 1   
        /// Matrix[2 x 2]
        /// 1   0   
        /// 0   1   
        /// Matrix[3 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix I_3 = IdentityMatrix(3);

        /// <summary>
        /// The square identity matrix of size 4
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(I_1.ToString(multilineFormat: true));
        /// Console.WriteLine(I_2.ToString(multilineFormat: true));
        /// Console.WriteLine(I_3.ToString(multilineFormat: true));
        /// Console.WriteLine(I_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 1   
        /// Matrix[2 x 2]
        /// 1   0   
        /// 0   1   
        /// Matrix[3 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix I_4 = IdentityMatrix(4);

        /// <summary>
        /// The square zero matrix of size 1
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(O_1.ToString(multilineFormat: true));
        /// Console.WriteLine(O_2.ToString(multilineFormat: true));
        /// Console.WriteLine(O_3.ToString(multilineFormat: true));
        /// Console.WriteLine(O_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 0   
        /// Matrix[2 x 2]
        /// 0   0   
        /// 0   0   
        /// Matrix[3 x 3]
        /// 0   0   0   
        /// 0   0   0   
        /// 0   0   0   
        /// Matrix[4 x 4]
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix O_1 = ZeroMatrix(1);

        /// <summary>
        /// The square zero matrix of size 2
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(O_1.ToString(multilineFormat: true));
        /// Console.WriteLine(O_2.ToString(multilineFormat: true));
        /// Console.WriteLine(O_3.ToString(multilineFormat: true));
        /// Console.WriteLine(O_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 0   
        /// Matrix[2 x 2]
        /// 0   0   
        /// 0   0   
        /// Matrix[3 x 3]
        /// 0   0   0   
        /// 0   0   0   
        /// 0   0   0   
        /// Matrix[4 x 4]
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix O_2 = ZeroMatrix(2);

        /// <summary>
        /// The square zero matrix of size 3
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(O_1.ToString(multilineFormat: true));
        /// Console.WriteLine(O_2.ToString(multilineFormat: true));
        /// Console.WriteLine(O_3.ToString(multilineFormat: true));
        /// Console.WriteLine(O_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 0   
        /// Matrix[2 x 2]
        /// 0   0   
        /// 0   0   
        /// Matrix[3 x 3]
        /// 0   0   0   
        /// 0   0   0   
        /// 0   0   0   
        /// Matrix[4 x 4]
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix O_3 = ZeroMatrix(3);

        /// <summary>
        /// The square zero matrix of size 4
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(O_1.ToString(multilineFormat: true));
        /// Console.WriteLine(O_2.ToString(multilineFormat: true));
        /// Console.WriteLine(O_3.ToString(multilineFormat: true));
        /// Console.WriteLine(O_4.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[1 x 1]
        /// 0   
        /// Matrix[2 x 2]
        /// 0   0   
        /// 0   0   
        /// Matrix[3 x 3]
        /// 0   0   0   
        /// 0   0   0   
        /// 0   0   0   
        /// Matrix[4 x 4]
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0   
        /// 0   0   0   0
        /// </code>
        /// </example>
        [ConstantField] public static readonly Matrix O_4 = ZeroMatrix(4);


        /// <summary>Converts a <see cref="string"/> to an expression</summary>
        /// <param name="expr"><see cref="string"/> expression, for example, <code>"2 * x + 3 + sqrt(x)"</code></param>
        /// <param name="useCache">By default is true, it boosts performance if you have multiple uses of the same string,
        /// for example, 
        /// Entity expr = (Entity)"+oo" + "x".Limit("x", "+oo") * "+oo";
        /// First occurance will be parsed, others will be replaced with the cached entity
        /// </param>
        /// <returns>The parsed expression</returns>
        /// <example>
        /// Multiple ways to parse an expression.
        /// <code>
        /// Entity expr1 = "a + b";
        /// var expr2 = FromString("a + b");
        /// var expr3 = FromString("a + b", useCache: false);
        /// var expr4 = (Entity)"a + b";
        /// var expr5 = Parse("a + b").Switch(
        ///     res => res,
        ///     failure => failure.Reason.Switch&lt;object&gt;(
        ///         unknown => throw new("Unknown reason"),
        ///         missingOp => throw new("Missing operator"),
        ///         internalError => throw new("Internal error") 
        ///     )
        /// );
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity FromString(string expr, bool useCache)
            => expr
                .LetLazy(out var parsed, Parser.Parse)
                .ReplaceWith(useCache)
                    switch
                    {
                        false => parsed.Value,
                        true =>
                            expr switch
                            {
                                "0" => Integer.Create(0),
                                "1" => Integer.Create(1),
                                "-1" => Integer.Create(-1),
                                "+oo" => Real.PositiveInfinity,
                                "-oo" => Real.NegativeInfinity,
                                // One cache per pair of the settings the parse reads: with the
                                // downcasting off a whole number parses as a decimal, so a string
                                // parsed under one setting is not the other's parse.
                                // https://github.com/asc-community/AngouriMath/issues/1513
                                _ => (Settings.ExplicitParsingOnly.Value, Settings.DowncastingEnabled.Value) switch
                                    {
                                        (false, true) => stringToEntityCache.GetValue(expr, _ => parsed.Value),
                                        (true, true) => stringToEntityCacheExplicitOnly.GetValue(expr, _ => parsed.Value),
                                        (false, false) => stringToEntityCacheNotDowncasting.GetValue(expr, _ => parsed.Value),
                                        (true, false) => stringToEntityCacheExplicitOnlyNotDowncasting.GetValue(expr, _ => parsed.Value)
                                    }
                            }
                    };
        private static ConditionalWeakTable<string, Entity> stringToEntityCacheExplicitOnly = new();
        private static ConditionalWeakTable<string, Entity> stringToEntityCache = new();
        private static ConditionalWeakTable<string, Entity> stringToEntityCacheNotDowncasting = new();
        private static ConditionalWeakTable<string, Entity> stringToEntityCacheExplicitOnlyNotDowncasting = new();

        /// <summary>Converts a <see cref="string"/> to an expression</summary>
        /// <param name="expr"><see cref="string"/> expression, for example, <code>"2 * x + 3 + sqrt(x)"</code></param>
        /// <returns>The parsed expression</returns>
        /// <example>
        /// Multiple ways to parse an expression.
        /// <code>
        /// Entity expr1 = "a + b";
        /// var expr2 = FromString("a + b");
        /// var expr3 = FromString("a + b", useCache: false);
        /// var expr4 = (Entity)"a + b";
        /// var expr5 = Parse("a + b").Switch(
        ///     res => res,
        ///     failure => failure.Reason.Switch&lt;object&gt;(
        ///         unknown => throw new("Unknown reason"),
        ///         missingOp => throw new("Missing operator"),
        ///         internalError => throw new("Internal error") 
        ///     )
        /// );
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity FromString(string expr) => FromString(expr, useCache: true);
        
        /// <summary>
        /// Parses an expression silently, that is,
        /// without throwing an exception. Instead,
        /// it returns a Failure in case of encountered
        /// errors during parsing.
        /// </summary>
        /// <returns>
        /// Returns a type union of the successful result and
        /// failure, which is a type union of multiple reasons
        /// it may have failed.
        /// </returns>
        /// <example>
        /// Multiple ways to parse an expression.
        /// <code>
        /// Entity expr1 = "a + b";
        /// var expr2 = FromString("a + b");
        /// var expr3 = FromString("a + b", useCache: false);
        /// var expr4 = (Entity)"a + b";
        /// var expr5 = Parse("a + b").Switch(
        ///     res => res,
        ///     failure => failure.Reason.Switch&lt;object&gt;(
        ///         unknown => throw new("Unknown reason"),
        ///         missingOp => throw new("Missing operator"),
        ///         internalError => throw new("Internal error") 
        ///     )
        /// );
        /// </code>
        /// </example>
        public static ParsingResult Parse(string source)
            => Parser.ParseSilent(source);

        /// <summary>Translates a <see cref="Number"/> in base 10 into base <paramref name="N"/></summary>
        /// <param name="num">A <see cref="Real"/> in base 10 to be translated into base <paramref name="N"/></param>
        /// <param name="N">The base to translate the number into</param>
        /// <returns>A <see cref="string"/> with the number in the required base</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// using var _ = Settings.DowncastingEnabled.Set(false);
        /// 
        /// Console.WriteLine(ToBaseN(1.5m, 2));
        /// Console.WriteLine(ToBaseN(3.75m, 2));
        /// Console.WriteLine(ToBaseN(13.125m, 2));
        /// Console.WriteLine(ToBaseN(13.125m, 10));
        /// 
        /// // A repeating expansion is cut off at the configured decimal precision
        /// // instead of hanging; see https://github.com/asc-community/AngouriMath/issues/584.
        /// Console.WriteLine(ToBaseN(13.125m, 5).Substring(0, 12) + "...");
        /// 
        /// Console.WriteLine(ToBaseN(13.125m, 8));
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(FromBaseN("FF", 16));
        /// Console.WriteLine(FromBaseN("77", 8));
        /// Console.WriteLine(FromBaseN("1.1", 2));
        /// Console.WriteLine(FromBaseN("1.01", 2));
        /// Console.WriteLine(FromBaseN("1.05", 6));        
        /// </code>
        /// Prints
        /// <code>
        /// 1.1
        /// 11.11
        /// 1101.001
        /// 13.125
        /// 23.030303030...
        /// 15.1
        /// -----------------------
        /// 255
        /// 63
        /// 1.500000000
        /// 1.250000000
        /// 1.138888888
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToBaseN(Real num, int N) => BaseConversion.ToBaseN(num.EDecimal, N);

        /// <summary>Translates a number in base <paramref name="N"/> into base 10</summary>
        /// <param name="num">A <see cref="Real"/> in base <paramref name="N"/> to be translated into base 10</param>
        /// <param name="N">The base to translate the number from</param>
        /// <returns>The <see cref="Real"/> in base 10</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// using var _ = Settings.DowncastingEnabled.Set(false);
        /// 
        /// Console.WriteLine(ToBaseN(1.5m, 2));
        /// Console.WriteLine(ToBaseN(3.75m, 2));
        /// Console.WriteLine(ToBaseN(13.125m, 2));
        /// Console.WriteLine(ToBaseN(13.125m, 10));
        /// 
        /// // A repeating expansion is cut off at the configured decimal precision
        /// // instead of hanging; see issue 584.
        /// Console.WriteLine(ToBaseN(13.125m, 5).Substring(0, 12) + "...");
        /// 
        /// Console.WriteLine(ToBaseN(13.125m, 8));
        /// Console.WriteLine("-----------------------");
        /// Console.WriteLine(FromBaseN("FF", 16));
        /// Console.WriteLine(FromBaseN("77", 8));
        /// Console.WriteLine(FromBaseN("1.1", 2));
        /// Console.WriteLine(FromBaseN("1.01", 2));
        /// Console.WriteLine(FromBaseN("1.05", 6));        
        /// </code>
        /// Prints
        /// <code>
        /// 1.1
        /// 11.11
        /// 1101.001
        /// 13.125
        /// 23.030303030...
        /// 15.1
        /// -----------------------
        /// 255
        /// 63
        /// 1.500000000
        /// 1.250000000
        /// 1.138888888
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Number.Real FromBaseN(string num, int N) => BaseConversion.FromBaseN(num, N);

        /// <returns>
        /// The <a href="https://en.wikipedia.org/wiki/LaTeX">LaTeX</a> representation of the argument
        /// </returns>
        /// <param name="latexizeable">
        /// Any element (<see cref="Entity"/>, <see cref="Set"/>, etc.) that can be represented in LaTeX
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// Entity expr = "sqrt(a) + integral(sin(x), x)";
        /// Console.WriteLine(expr);
        /// Console.WriteLine(Latex(expr));
        /// Entity expr2 = "a / b ^ limit(sin(x) - cosh(y), x, +oo)";
        /// Console.WriteLine(expr2);
        /// Console.WriteLine(Latex(expr2));
        /// </code>
        /// Prints
        /// <code>
        /// sqrt(a) + integral(sin(x), x)
        /// \sqrt{a}+\int \left[\sin\left(x\right)\right] dx
        /// a / b ^ limit(sin(x) - (e ^ y + e ^ (-y)) / 2, x, +oo)
        /// \frac{a}{{b}^{\lim_{x\to \infty } \left[\sin\left(x\right)-\frac{{e}^{y}+{e}^{-y}}{2}\right]}}
        /// </code>
        /// </example>
        public static string Latex(ILatexizeable latexizeable) => latexizeable.Latexize();

        /// <summary>
        /// <para>All operations for <see cref="Number"/> and its derived classes are available from here.</para>
        ///
        /// These methods represent the only possible way to explicitly create numeric instances.
        /// It will automatically downcast the result for you,
        /// so <code>Number.Create(1.0);</code> is an <see cref="Integer"/>.
        /// To avoid it, you may temporarily disable it
        /// <code>
        /// using var _ = MathS.Settings.DowncastingEnabled.Set(false);
        /// var yourNum = Number.Create(1.0);
        /// </code>
        /// and the result will be a <see cref="Real"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using System.Numerics;
        /// using AngouriMath;
        /// using PeterO.Numbers;
        /// using static AngouriMath.MathS.Numbers;
        /// using static AngouriMath.MathS;
        /// 
        /// Entity a1 = 5;
        /// Entity a2 = "5";
        /// Entity a3 = new Complex(5.5, 6.5);
        /// Entity a4 = 6.5m;
        /// Entity a5 = 6.5;
        /// Entity a6 = 6.5f;
        /// Entity a7 = 5.6 + 3 * i;
        /// Entity a8 = EInteger.One;
        /// Entity a9 = ERational.One;
        /// Entity a10 = pi;
        /// Console.WriteLine("---------------");
        /// var n0 = Create(0);
        /// var n1 = Create(0L);
        /// var n2 = Create(new Complex(5.5, 4.75));
        /// var n3 = Create(EInteger.One);
        /// var n4 = CreateRational(4, 5);
        /// var n5 = Create(ERational.One);
        /// var n6 = Create(5.5m);
        /// var n7 = Create(5.5);
        /// var n8 = Create(5.5m, 6.5m);
        /// Console.WriteLine("---------------");
        /// float i0 = (float)FromString("1 + 5").EvalNumerical();
        /// double i1 = (double)FromString("1 + 5").EvalNumerical();
        /// int i2 = (int)FromString("1 + 5").EvalNumerical();
        /// long i3 = (long)FromString("1 + 5").EvalNumerical();
        /// short i4 = (short)FromString("1 + 5").EvalNumerical();
        /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
        /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
        /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
        /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
        /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
        /// </code>
        /// </example>
        public static class Numbers
        {
            /// <summary>Creates an instance of <see cref="Complex"/> from a <see cref="NumericsComplex"/></summary>
            /// <param name="value">A value of type <see cref="NumericsComplex"/></param>
            /// <returns>The resulting <see cref="Complex"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Complex Create(NumericsComplex value) =>
                Create(EDecimal.FromDouble(value.Real), EDecimal.FromDouble(value.Imaginary));

            /// <summary>Creates an instance of <see cref="Integer"/> from a <see cref="long"/></summary>
            /// <param name="value">A value of type <see cref="long"/> (signed 64-bit integer)</param>
            /// <returns>The resulting <see cref="Integer"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Integer Create(long value) => Integer.Create(value);

            /// <summary>Creates an instance of <see cref="Integer"/> from an <see cref="EInteger"/></summary>
            /// <param name="value">A value of type <see cref="EInteger"/></param>
            /// <returns>The resulting <see cref="Integer"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Integer Create(EInteger value) => Integer.Create(value);

            /// <summary>Creates an instance of <see cref="Integer"/> from an <see cref="int"/></summary>
            /// <param name="value">A value of type <see cref="int"/> (signed 32-bit integer)</param>
            /// <returns>The resulting <see cref="Integer"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Integer Create(int value) => Integer.Create(value);

            /// <summary>Creates an instance of <see cref="Rational"/> from two <see cref="EInteger"/>s</summary>
            /// <param name="numerator">Numerator of type <see cref="EInteger"/></param>
            /// <param name="denominator">Denominator of type <see cref="EInteger"/></param>
            /// <returns>
            /// The resulting <see cref="Rational"/>
            /// </returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Rational CreateRational(EInteger numerator, EInteger denominator)
                => Rational.Create(numerator, denominator);

            /// <summary>Creates an instance of <see cref="Rational"/> from an <see cref="ERational"/></summary>
            /// <param name="rational">A value of type <see cref="ERational"/></param>
            /// <returns>The resulting <see cref="Rational"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Rational Create(ERational rational) => Rational.Create(rational);

            /// <summary>Creates an instance of <see cref="Real"/> from an <see cref="EDecimal"/></summary>
            /// <param name="value">A value of type <see cref="EDecimal"/></param>
            /// <returns>The resulting <see cref="Real"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Real Create(EDecimal value) => Real.Create(value);

            /// <summary>Creates an instance of <see cref="Real"/> from a <see cref="double"/></summary>
            /// <param name="value">A value of type <see cref="double"/> (64-bit floating-point number)</param>
            /// <returns>The resulting <see cref="Real"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Real Create(double value) => Real.Create(EDecimal.FromDouble(value));

            /// <summary>
            /// Creates an instance of <see cref="Complex"/> from two <see cref="EDecimal"/>s
            /// </summary>
            /// <param name="re">
            /// Real part of the desired <see cref="Complex"/> of type <see cref="EDecimal"/>
            /// </param>
            /// <param name="im">
            /// Imaginary part of the desired <see cref="Complex"/> of type <see cref="EDecimal"/>
            /// </param>
            /// <returns>The resulting <see cref="Complex"/></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using System.Numerics;
            /// using AngouriMath;
            /// using PeterO.Numbers;
            /// using static AngouriMath.MathS.Numbers;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity a1 = 5;
            /// Entity a2 = "5";
            /// Entity a3 = new Complex(5.5, 6.5);
            /// Entity a4 = 6.5m;
            /// Entity a5 = 6.5;
            /// Entity a6 = 6.5f;
            /// Entity a7 = 5.6 + 3 * i;
            /// Entity a8 = EInteger.One;
            /// Entity a9 = ERational.One;
            /// Entity a10 = pi;
            /// Console.WriteLine("---------------");
            /// var n0 = Create(0);
            /// var n1 = Create(0L);
            /// var n2 = Create(new Complex(5.5, 4.75));
            /// var n3 = Create(EInteger.One);
            /// var n4 = CreateRational(4, 5);
            /// var n5 = Create(ERational.One);
            /// var n6 = Create(5.5m);
            /// var n7 = Create(5.5);
            /// var n8 = Create(5.5m, 6.5m);
            /// Console.WriteLine("---------------");
            /// float i0 = (float)FromString("1 + 5").EvalNumerical();
            /// double i1 = (double)FromString("1 + 5").EvalNumerical();
            /// int i2 = (int)FromString("1 + 5").EvalNumerical();
            /// long i3 = (long)FromString("1 + 5").EvalNumerical();
            /// short i4 = (short)FromString("1 + 5").EvalNumerical();
            /// BigInteger i5 = (BigInteger)FromString("1 + 5").EvalNumerical();
            /// Complex i6 = (Complex)FromString("1 + 5").EvalNumerical();
            /// EInteger i7 = ((Entity.Number.Integer)FromString("1 + 5").EvalNumerical()).EInteger;
            /// ERational i8 = ((Entity.Number.Rational)FromString("1 + 5").EvalNumerical()).ERational;
            /// EDecimal i9 = ((Entity.Number.Real)FromString("1 + 5").EvalNumerical()).EDecimal;
            /// </code>
            /// </example>
            public static Complex Create(EDecimal re, EDecimal im) => Complex.Create(re, im);
        }

        /// <summary>
        /// Finds the determinant of the given matrix. If
        /// the matrix is non-square, returns null
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.Entity;
        /// using static AngouriMath.MathS;
        /// 
        /// Matrix A = @"
        /// [[1, 2],
        ///  [3, 4]]
        /// ";
        /// Console.WriteLine(Det(A));
        /// 
        /// Matrix B = @"
        /// [[1, 2],
        ///  [3, 6]]
        /// ";
        /// Console.WriteLine(Det(B));
        /// 
        /// Matrix C = @"
        /// [[1, 2],
        ///  [3, 6],
        ///  [7, 8]]
        /// ";
        /// Console.WriteLine(Det(C) is null);
        /// </code>
        /// Prints
        /// <code>
        /// -2
        /// 0
        /// True
        /// </code>
        /// </example>
        public static Entity? Det(Matrix m)
            => m.Determinant;

        /// <summary>Creates an instance of <see cref="Entity.Matrix"/>.</summary>
        /// <param name="values">
        /// A two-dimensional array of values.
        /// The first dimension is the row count, the second one is for columns.
        /// </param>
        /// <returns>A two-dimensional <see cref="Entity.Matrix"/> which is a matrix</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.Entity;
        /// using static AngouriMath.MathS;
        /// 
        /// var A = Matrix(new Entity[,]
        ///     {
        ///         { 1, 2, 3 },
        ///         { 4, 5, 6 }
        ///     }
        /// );
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// var B = Vector(1, 2, 3, 4);
        /// Console.WriteLine(B.ToString(multilineFormat: true));
        /// 
        /// Matrix C = @"
        /// [[1, 2],
        ///  [3, 4],
        ///  [5, 6]]
        ///  ";
        /// Console.WriteLine(C.ToString(multilineFormat: true));
        /// 
        /// Matrix D = "[1, 2, 3, 4]";
        /// Console.WriteLine(D.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 3]
        /// 1   2   3   
        /// 4   5   6   
        /// Matrix[4 x 1]
        /// 1   
        /// 2   
        /// 3   
        /// 4   
        /// Matrix[3 x 2]
        /// 1   2   
        /// 3   4   
        /// 5   6   
        /// Matrix[4 x 1]
        /// 1   
        /// 2   
        /// 3   
        /// 4
        /// </code>
        /// </example>
        public static Matrix Matrix(Entity[,] values) => new(GenTensor.CreateMatrix(values));

        /// <summary>
        /// Creates an instance of matrix, where each cell's
        /// index is mapped to a value with the help of the
        /// mapping function.
        /// </summary>
        /// <param name="rowCount">
        /// The number of rows (corresponds to the first index).
        /// </param>
        /// <param name="colCount">
        /// The number of columns (corresponds to the second index).
        /// </param>
        /// <param name="map">
        /// The first argument of the mapping function
        /// function is the index of row, the second one for the 
        /// column index.
        /// 
        /// Indexing starts from 0.
        /// </param>
        /// <returns>
        /// A newly created matrix of the given size.
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath.Extensions;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = "sin(x) + cos(y)";
        /// var vars = new[] { Var("x"), Var("y") };
        /// var A = Matrix(2, 2, (i1, i2) => expr
        ///     .Differentiate(vars[i1])
        ///     .Differentiate(vars[i2])
        ///     .Simplify());
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// Console.WriteLine(ZeroMatrix(3));
        /// Console.WriteLine(ZeroMatrix(3, 4));
        /// Console.WriteLine(ZeroVector(3));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// -sin(x)   0         
        /// 0         -cos(y)   
        /// [[0, 0, 0], [0, 0, 0], [0, 0, 0]]
        /// [[0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0]]
        /// [0, 0, 0]
        /// </code>
        /// </example>
        public static Matrix Matrix(int rowCount, int colCount, Func<int, int, Entity> map)
            => new(GenTensor.CreateMatrix(rowCount, colCount, map));


        /// <summary>Creates an instance of <see cref="Entity.Matrix"/> that has one column.</summary>
        /// <param name="values">The cells of the <see cref="Entity.Matrix"/></param>
        /// <returns>A one-dimensional <see cref="Entity.Matrix"/> which is a vector</returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.Entity;
        /// using static AngouriMath.MathS;
        /// 
        /// var A = Matrix(new Entity[,]
        ///     {
        ///         { 1, 2, 3 },
        ///         { 4, 5, 6 }
        ///     }
        /// );
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// var B = Vector(1, 2, 3, 4);
        /// Console.WriteLine(B.ToString(multilineFormat: true));
        /// 
        /// Matrix C = @"
        /// [[1, 2],
        ///  [3, 4],
        ///  [5, 6]]
        ///  ";
        /// Console.WriteLine(C.ToString(multilineFormat: true));
        /// 
        /// Matrix D = "[1, 2, 3, 4]";
        /// Console.WriteLine(D.ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 3]
        /// 1   2   3   
        /// 4   5   6   
        /// Matrix[4 x 1]
        /// 1   
        /// 2   
        /// 3   
        /// 4   
        /// Matrix[3 x 2]
        /// 1   2   
        /// 3   4   
        /// 5   6   
        /// Matrix[4 x 1]
        /// 1   
        /// 2   
        /// 3   
        /// 4
        /// </code>
        /// </example>
        /// <remarks>
        /// A vector of no entries is refused rather than built. It used to leak a
        /// <see cref="IndexOutOfRangeException"/>, which is outside the hierarchy
        /// <c>Docs/Usage/Exceptions.md</c> documents — so a caller catching
        /// <c>AngouriMathBaseException</c> did not catch it.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1028">#1028</a>
        /// </remarks>
        public static Matrix Vector(params Entity[] values)
            => values.Length is 0
                ? throw new InvalidMatrixOperationException(
                    "a vector needs at least one entry; there is no 0x1 matrix here")
                : new Matrix(GenTensor.CreateTensor(new(values.Length, 1), arr => values[arr[0]]));

        /// <summary>
        /// Creates a zero square matrix
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath.Extensions;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = "sin(x) + cos(y)";
        /// var vars = new[] { Var("x"), Var("y") };
        /// var A = Matrix(2, 2, (i1, i2) => expr
        ///     .Differentiate(vars[i1])
        ///     .Differentiate(vars[i2])
        ///     .Simplify());
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// Console.WriteLine(ZeroMatrix(3));
        /// Console.WriteLine(ZeroMatrix(3, 4));
        /// Console.WriteLine(ZeroVector(3));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// -sin(x)   0         
        /// 0         -cos(y)   
        /// [[0, 0, 0], [0, 0, 0], [0, 0, 0]]
        /// [[0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0]]
        /// [0, 0, 0]
        /// </code>
        /// </example>
        public static Matrix ZeroMatrix(int size)
            => ZeroMatrix(size, size);

        /// <summary>
        /// Creates a zero square matrix
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath.Extensions;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = "sin(x) + cos(y)";
        /// var vars = new[] { Var("x"), Var("y") };
        /// var A = Matrix(2, 2, (i1, i2) => expr
        ///     .Differentiate(vars[i1])
        ///     .Differentiate(vars[i2])
        ///     .Simplify());
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// Console.WriteLine(ZeroMatrix(3));
        /// Console.WriteLine(ZeroMatrix(3, 4));
        /// Console.WriteLine(ZeroVector(3));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// -sin(x)   0         
        /// 0         -cos(y)   
        /// [[0, 0, 0], [0, 0, 0], [0, 0, 0]]
        /// [[0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0]]
        /// [0, 0, 0]
        /// </code>
        /// </example>
        public static Matrix ZeroMatrix(int rowCount, int columnCount)
            => new Matrix(GenTensor.CreateTensor(new(rowCount, columnCount), arr => 0));

        /// <summary>
        /// Creates a zero vector
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath.Extensions;
        /// using static AngouriMath.MathS;
        /// 
        /// var expr = "sin(x) + cos(y)";
        /// var vars = new[] { Var("x"), Var("y") };
        /// var A = Matrix(2, 2, (i1, i2) => expr
        ///     .Differentiate(vars[i1])
        ///     .Differentiate(vars[i2])
        ///     .Simplify());
        /// Console.WriteLine(A.ToString(multilineFormat: true));
        /// 
        /// Console.WriteLine(ZeroMatrix(3));
        /// Console.WriteLine(ZeroMatrix(3, 4));
        /// Console.WriteLine(ZeroVector(3));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[2 x 2]
        /// -sin(x)   0         
        /// 0         -cos(y)   
        /// [[0, 0, 0], [0, 0, 0], [0, 0, 0]]
        /// [[0, 0, 0, 0], [0, 0, 0, 0], [0, 0, 0, 0]]
        /// [0, 0, 0]
        /// </code>
        /// </example>
        public static Matrix ZeroVector(int size)
            => new Matrix(GenTensor.CreateTensor(new(size, 1), arr => 0));

        /// <summary>
        /// Creates a 1x1 matrix of a given value. It will be simplified
        /// once InnerSimplified or Evaled are addressed
        /// </summary>
        /// <returns>
        /// A 1x1 matrix, which is also a 1-long vector, or just a scalar.
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(Scalar(56));
        /// Console.WriteLine(MatrixFromIEnum2x2(new Entity[][]
        ///     {
        ///         new Entity[]{ 1, 2, 3 },
        ///         new Entity[]{ 4, 5, 6 }
        ///     }
        /// ));
        /// </code>
        /// Prints
        /// <code>
        /// [56]
        /// [[1, 2, 3], [4, 5, 6]]
        /// </code>
        /// </example>
        public static Matrix Scalar(Entity value)
            => Vector(value);

        /// <summary>
        /// Creates a matrix from given rows
        /// </summary>
        /// <param name="vectors">
        /// There should be at least one row.
        /// All rows must have the same number
        /// of columns
        /// </param>
        public static Matrix MatrixFromRows(IEnumerable<Matrix> vectors)
        {
            if (!vectors.Any())
                throw new InvalidMatrixOperationException("No rows were provided");
            var tb = new MatrixBuilder(vectors.First().ColumnCount);
            foreach (var v in vectors)
                tb.Add(v);
            return tb.ToMatrix() ?? throw new AngouriBugException("Nullability should have been checked before");
        }

        /// <summary>
        /// Creates a matrix from given elements
        /// </summary>
        /// <param name="elements">
        /// There should be at least one row.
        /// All rows must have the same number
        /// of columns
        /// </param>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(Scalar(56));
        /// Console.WriteLine(MatrixFromIEnum2x2(new Entity[][]
        ///     {
        ///         new Entity[]{ 1, 2, 3 },
        ///         new Entity[]{ 4, 5, 6 }
        ///     }
        /// ));
        /// </code>
        /// Prints
        /// <code>
        /// [56]
        /// [[1, 2, 3], [4, 5, 6]]
        /// </code>
        /// </example>
        public static Matrix MatrixFromIEnum2x2(IEnumerable<IEnumerable<Entity>> elements)
        {
            if (!elements.Any())
                throw new InvalidMatrixOperationException("No rows were provided");
            var tb = new MatrixBuilder(elements.First().Count());
            foreach (var v in elements)
                tb.Add(v);
            return tb.ToMatrix() ?? throw new AngouriBugException("Nullability should have been checked before");
        }

        /// <summary>
        /// Creates a closed interval (segment)
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
        public static Interval Interval(Entity left, Entity right) => new Interval(left, true, right, true);

        /// <summary>
        /// Creates an interval with custom endings
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
        public static Interval Interval(Entity left, bool leftClosed, Entity right, bool rightClosed) => new Interval(left, leftClosed, right, rightClosed);

        /// <summary>
        /// Creates a square identity matrix
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(IdentityMatrix(4).ToString(multilineFormat: true));
        /// Console.WriteLine(IdentityMatrix(4, 3).ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1   
        /// Matrix[4 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// 0   0   0   
        /// </code>
        /// </example>
        public static Matrix IdentityMatrix(int size)
            => Entity.Matrix.I(size);

        /// <summary>
        /// Creates a rectangular identity matrix
        /// with the given size
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// Console.WriteLine(IdentityMatrix(4).ToString(multilineFormat: true));
        /// Console.WriteLine(IdentityMatrix(4, 3).ToString(multilineFormat: true));
        /// </code>
        /// Prints
        /// <code>
        /// Matrix[4 x 4]
        /// 1   0   0   0   
        /// 0   1   0   0   
        /// 0   0   1   0   
        /// 0   0   0   1   
        /// Matrix[4 x 3]
        /// 1   0   0   
        /// 0   1   0   
        /// 0   0   1   
        /// 0   0   0   
        /// </code>
        /// </example>
        public static Matrix IdentityMatrix(int rowCount, int columnCount)
            => Entity.Matrix.I(rowCount, columnCount);

        /// <summary>Classes and functions related to matrices are defined here</summary>
        public static class Matrices
        {
            /// <summary>
            /// Direction of matrix concatenation
            /// </summary>
            public enum Direction
            {
                /// <summary>Side by side, so the columns of the second follow the first.</summary>
                Horizontal,

                /// <summary>One above the other, so the rows of the second follow the first.</summary>
                Vertical
            }

            /// <summary>
            /// Concatenates two matrices by the given direction.
            /// </summary>
            /// <param name="dir"></param>
            /// <param name="matrices"></param>
            /// <returns></returns>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.Entity;
            /// 
            /// Matrix A = @"
            /// [[1, 2, 3]]
            /// ";
            /// Matrix B = @"
            /// [[1, 2, 3],
            ///  [4, 5, 6]]";
            /// Matrix C = @"
            /// [[2, 4, 1],
            ///  [4, 1, 2],
            ///  [a, x, y]]
            /// ";
            /// Console.WriteLine(
            ///     Matrices.Concat(Matrices.Direction.Vertical, A, B, C)
            ///     .ToString(multilineFormat: true)
            ///  );
            /// Console.WriteLine("----------------------------");
            /// Matrix D = "[1, 2, 3]";
            /// Matrix E = "[4, 5, 6]";
            /// Console.WriteLine(D.ToString(multilineFormat: true));
            /// Console.WriteLine(E.ToString(multilineFormat: true));
            /// Console.WriteLine(
            ///     Matrices.Concat(Matrices.Direction.Horizontal, D, E)
            ///     .ToString(multilineFormat: true)
            /// );
            /// </code>
            /// Prints
            /// <code>
            /// Matrix[6 x 3]
            /// 1   2   3   
            /// 1   2   3   
            /// 4   5   6   
            /// 2   4   1   
            /// 4   1   2   
            /// a   x   y   
            /// ----------------------------
            /// Matrix[3 x 1]
            /// 1   
            /// 2   
            /// 3   
            /// Matrix[3 x 1]
            /// 4   
            /// 5   
            /// 6   
            /// Matrix[3 x 2]
            /// 1   4   
            /// 2   5   
            /// 3   6
            /// </code>
            /// </example>
            public static Matrix Concat(Direction dir, params Matrix[] matrices)
                => MatrixOperations.Concat(dir, matrices);
        
            /// <summary>
            /// Performs a pointwise multiplication operation,
            /// or throws exception if shapes mismatch
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS;
            /// using static AngouriMath.Entity;
            /// 
            /// Matrix A = @"
            /// [[a, b],
            ///  [c, d]]
            /// ";
            /// Matrix B = @"
            /// [[f, g],
            ///  [k, l]]
            /// ";
            /// Console.WriteLine((A * B).ToString(multilineFormat: true));
            /// Console.WriteLine(Matrices.PointwiseMultiplication(A, B).ToString(multilineFormat: true));
            /// Console.WriteLine("-------------------");
            /// var v1 = Vector(1, 2, 3);
            /// var v2 = Vector(10, 20, 30);
            /// Console.WriteLine(v1);
            /// Console.WriteLine(v2);
            /// Console.WriteLine((v1.T * v2).AsScalar());
            /// </code>
            /// Prints
            /// <code>
            /// Matrix[2 x 2]
            /// a * f + b * k   a * g + b * l   
            /// c * f + d * k   c * g + d * l   
            /// Matrix[2 x 2]
            /// a * f   b * g   
            /// c * k   d * l   
            /// -------------------
            /// [1, 2, 3]
            /// [10, 20, 30]
            /// 140
            /// </code>
            /// </example>
            public static Matrix PointwiseMultiplication(Matrix m1, Matrix m2)
            {
                // GenericTensor caches the compiled elementwise loop in an unsynchronised
                // dictionary, so the entry has to be there before several threads read it.
                // See Functions.GenTensorGuard.
                Functions.GenTensorGuard.EnsurePiecewiseCacheWarmed();
                return (Matrix)new Matrix(GenTensor.PiecewiseMultiply(m1.InnerMatrix, m2.InnerMatrix)).InnerSimplified;
            }

            /// <summary>
            /// Creates an instance of <see cref="Entity.Matrix"/> that is a matrix.
            /// Usage example:
            /// <code>
            /// var t = MathS.Matrix(5, 3,<br/>
            /// <list type="bullet"><list type="bullet"><list type="bullet"><list type="bullet">
            ///     10, 11, 12,<br/>
            ///     20, 21, 22,<br/>
            ///     30, 31, 32,<br/>
            ///     40, 41, 42,<br/>
            ///     50, 51, 52);
            /// </list></list></list></list>
            /// </code>
            /// creates 5×3 matrix with the appropriate elements
            /// </summary>
            /// <param name="rows">Number of rows (first axis)</param>
            /// <param name="columns">Number of columns (second axis)</param>
            /// <param name="values">
            /// Array of values of the matrix so that its length is equal to
            /// the product of <paramref name="rows"/> and <paramref name="columns"/>
            /// </param>
            /// <returns>A two-dimensional <see cref="Entity.Matrix"/> which is a matrix</returns>
            public static Matrix Matrix(int rows, int columns, params Entity[] values) =>
                new(GenTensor.CreateMatrix(rows, columns, (x, y) => values[x * columns + y]));

        }

        /// <summary>
        /// A couple of settings allowing you to set some preferences for AM's algorithms
        /// To use these settings the syntax is
        /// <code>
        /// using var _ = MathS.Settings.SomeSetting.Set(5 /* Here you set a value to the setting */);
        /// // here you write your code normally
        /// </code>
        /// </summary>
        public static partial class Settings
        {
            /// <summary>
            /// Determine if it should only parse if it is explicit
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using AngouriMath.Core.Exceptions;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity expr = "a2 + 2x + a b + 2(g + e)3";
            /// Console.WriteLine(expr);
            /// using var _ = Settings.ExplicitParsingOnly.Set(true);
            /// try
            /// {
            ///     Entity expr2 = "a2 + 2x + a b + 2(g + e)3";
            ///     Console.WriteLine(expr2);
            /// }
            /// catch (MissingOperatorParseException)
            /// {
            ///     Entity expr3 = "a^2 + 2*x + a * b + 2*(g + e)^3";
            ///     Console.WriteLine($"Exception, but we still can parse {expr3}");
            /// }
            /// </code>
            /// Prints
            /// <code>
            /// a ^ 2 + 2 * x + a * b + 2 * (g + e) ^ 3
            /// Exception, but we still can parse a ^ 2 + 2 * x + a * b + 2 * (g + e) ^ 3
            /// </code>
            /// </example>
            public static Setting<bool> ExplicitParsingOnly { get; } = false;
            
            /// <summary>
            /// That is how we perform newton solving when no analytical solution was found
            /// in <see cref="Entity.Solve(Variable)"/> and <see cref="Entity.SolveEquation(Variable)"/>
            /// </summary>
            public sealed record NewtonSetting
            {
                /// <summary>
                /// The point where we start going from
                /// </summary>
                public (EDecimal Re, EDecimal Im) From { get; init; } = (-10, -10);

                /// <summary>
                /// The point after which we do not perform seach
                /// </summary>
                public (EDecimal Re, EDecimal Im) To { get; init; } = (10, 10);

                /// <summary>
                /// The number of steps to go through for real and for complex part
                /// </summary>
                public (int Re, int Im) StepCount { get; init; } = (10, 10);

                /// <summary>
                /// How precise the result is required to be. The higher, the longer 
                /// the algorithm takes to return the result
                /// </summary>
                public int Precision { get; init; } = 30;
            }
            /// <summary>
            /// Enables downcasting. Not recommended to turn off, disabling might be only useful for some calculations
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS;
            /// 
            /// Entity expr = (Entity.Number.Real)5.5m;
            /// Console.WriteLine(expr.GetType());
            /// 
            /// using var _ = Settings.DowncastingEnabled.Set(false);
            /// 
            /// Entity expr2 = (Entity.Number.Real)5.5m;
            /// Console.WriteLine(expr2.GetType());
            /// </code>
            /// Prints
            /// <code>
            /// AngouriMath.Entity+Number+Rational
            /// AngouriMath.Entity+Number+Real
            /// </code>
            /// </example>
            public static Setting<bool> DowncastingEnabled { get; } = true;

            /// <summary>
            /// Amount of iterations allowed for attempting to cast to a rational
            /// The more iterations, the larger fraction could be calculated
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS;
            /// 
            /// void Method()
            /// {
            ///     Console.WriteLine("---------------------");
            ///     Entity a = 5.5m;
            ///     Console.WriteLine(a);
            ///     Entity b = 5.462m;
            ///     Console.WriteLine(b);
            ///     Entity c = 5.0m / 7.0m;
            ///     Console.WriteLine(c);
            /// }
            /// 
            /// Settings.FloatToRationalIterCount.As(1, Method);
            /// Settings.FloatToRationalIterCount.As(3, Method);
            /// Settings.FloatToRationalIterCount.As(5, Method);
            /// Settings.FloatToRationalIterCount.As(10, Method);
            /// </code>
            /// Prints
            /// <code>
            /// 5.5
            /// 5.462
            /// 0.7142857142857142857142857143
            /// ---------------------
            /// 11/2
            /// 5.462
            /// 0.7142857142857142857142857143
            /// ---------------------
            /// 11/2
            /// 5.462
            /// 5/7
            /// ---------------------
            /// 11/2
            /// 2731/500
            /// 5/7
            /// </code>
            /// </example>
            public static Setting<int> FloatToRationalIterCount { get; } = 15;

            /// <summary>
            /// If a numerator or denominator is too large, it's suspended to better keep the real number instead of casting
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS;
            /// 
            /// void Method()
            /// {
            ///     Console.WriteLine("---------------------");
            ///     Console.WriteLine((Entity)(100m / 169));
            ///     Console.WriteLine((Entity)(100m / 1691));
            ///     Console.WriteLine((Entity)(100m / 16913));
            ///     Console.WriteLine((Entity)(100m / 169137));
            /// }
            /// 
            /// Settings.MaxAbsNumeratorOrDenominatorValue.As(100, Method);
            /// Settings.MaxAbsNumeratorOrDenominatorValue.As(1000, Method);
            /// Settings.MaxAbsNumeratorOrDenominatorValue.As(10000, Method);
            /// Settings.MaxAbsNumeratorOrDenominatorValue.As(100000, Method);
            /// Settings.MaxAbsNumeratorOrDenominatorValue.As(1000000, Method);
            /// </code>
            /// Prints
            /// <code>
            /// 0.5917159763313609467455621302
            /// 0.0591366055588409225310467179
            /// 0.0059126116005439602672500443
            /// 0.000591236689784021237221897
            /// ---------------------
            /// 100/169
            /// 0.0591366055588409225310467179
            /// 0.0059126116005439602672500443
            /// 0.000591236689784021237221897
            /// ---------------------
            /// 100/169
            /// 100/1691
            /// 0.0059126116005439602672500443
            /// 0.000591236689784021237221897
            /// ---------------------
            /// 100/169
            /// 100/1691
            /// 100/16913
            /// 0.000591236689784021237221897
            /// ---------------------
            /// 100/169
            /// 100/1691
            /// 100/16913
            /// 100/169137
            /// </code>
            /// </example>
            public static Setting<EInteger> MaxAbsNumeratorOrDenominatorValue { get; } = EInteger.FromInt32(100000000);

            /// <summary>
            /// Sets threshold for comparison
            /// For example, if you don't need precision higher than 6 digits after .,
            /// you can set it to 1.0e-6 so 1.0000000 == 0.9999999
            /// </summary>
            public static Setting<EDecimal> PrecisionErrorCommon { get; } = EDecimal.Create(1, -6);


            /// <summary>
            /// Numbers whose absolute value is less than PrecisionErrorZeroRange are considered zeros
            /// </summary>
            public static Setting<EDecimal> PrecisionErrorZeroRange { get; } = EDecimal.Create(1, -16);

            /// <summary>
            /// The tolerance within which downcasting rounds a number onto a nearby integer.
            /// </summary>
            /// <remarks>
            /// This is <see cref="PrecisionErrorZeroRange"/> whenever a caller has set it,
            /// but its default of 1e-16 is far too coarse to be applied blindly: numbers are
            /// evaluated to <see cref="DecimalPrecisionContext"/> digits, 100 by default, so
            /// 1e-16 sits nowhere near the noise floor. Applying it destroyed every legitimate
            /// small value -- <c>1e-20</c> parsed to zero and <c>e^(-40)</c> evaluated to zero.
            /// Residuals genuinely left over by exact cancellation, such as the one from
            /// <c>sin(pi)</c>, land around 1e-99, so half the working digits separates the two
            /// cases with room to spare. Never looser than the setting's own default, so that
            /// lowering the precision cannot silently widen the tolerance.
            /// </remarks>
            /// <summary>
            /// <see cref="DowncastingTolerance"/> as a double, for the cheap check in front of
            /// the exact one: the conversion of the decimal costs more than the check.
            /// </summary>
            internal static double DowncastingToleranceAsDouble
            {
                get
                {
                    if (PrecisionErrorZeroRange.IsOverriden)
                        return PrecisionErrorZeroRange.Value.ToDouble();
                    var precision = DecimalPrecisionContext.Value.Precision;
                    if (precision.IsZero || !precision.CanFitInInt32())
                        return 1e-16;
                    return System.Math.Min(System.Math.Pow(10, -(precision.ToInt32Unchecked() / 2)), 1e-16);
                }
            }

            internal static EDecimal DowncastingTolerance
            {
                get
                {
                    var configured = PrecisionErrorZeroRange.Value;
                    if (PrecisionErrorZeroRange.IsOverriden)
                        return configured;
                    var precision = DecimalPrecisionContext.Value.Precision;
                    if (precision.IsZero)
                        return configured;
                    // Built from EIntegers throughout: the precision is user-supplied and
                    // need not fit in an int.
                    var derived = EDecimal.Create(EInteger.One, precision.Divide(2).Negate());
                    return derived.LessThan(configured) ? derived : configured;
                }
            }

            /// <summary>
            /// If you only need analytical solutions, disable Newton's method. What comes back
            /// where there are none is the equation itself as a set builder: the empty set
            /// would say the equation has no roots, which is a claim nothing established.
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath.Extensions;
            /// using static AngouriMath.MathS;
            /// 
            /// Console.WriteLine("x5 + 3x + 1 = 0".ToEntity().Solve("x"));
            /// 
            /// using var _ = Settings.AllowNewton.Set(false);
            /// 
            /// Console.WriteLine("x5 + 3x + 1 = 0".ToEntity().Solve("x"));
            /// </code>
            /// Prints
            /// <code>
            /// { 1.0050669478588620808778841819730587303638458251953125 + 0.93725915669289194820379407246946357190608978271484375i,
            /// ... omitting here most of the output, because it's huge
            /// }
            /// { x : x ^ 5 + 3 * x = -1 } // no analytical solution was found for this quintic
            /// </code>
            /// </example>
            public static Setting<bool> AllowNewton { get; } = true;

            /// <summary>
            /// Criteria for simplifier so you could control which expressions are considered easier by you.
            /// The higher the value - the more complicated an expression is.
            /// </summary>
            /// <example>
            /// Consider a situation, when we simplify this simple expression:
            /// <code>
            /// using System;
            /// using AngouriMath.Extensions;
            /// 
            /// Console.WriteLine("a / b + b / c".ToEntity().Simplify());
            /// </code>
            /// The output is
            /// <code>
            /// a / b + b / c
            /// </code>
            /// Assume we now hate division operators. For simplicity, our new complexity criteria
            /// equals to how many division operators are there. Basically, the more of them there are in
            /// expression, the more complicated it is. Now we add it:
            /// <code>
            /// using System;
            /// using System.Linq;
            /// using AngouriMath;
            /// using AngouriMath.Extensions;
            /// using static AngouriMath.MathS;
            /// 
            /// Console.WriteLine("a / b + b / c".ToEntity().Simplify());
            /// 
            ///     using var _ = Settings.ComplexityCriteria.Set(
            ///     expr => expr.Nodes.Count(node => node is Entity.Divf)
            /// );
            /// 
            /// Console.WriteLine("a / b + b / c".ToEntity().Simplify());
            /// </code>
            /// The output:
            /// <code>
            /// a / b + b / c
            /// (a * c + b ^ 2) / (b * c)
            /// </code>
            /// Note that in the second case, this expression is simpler, because our complexity
            /// criteria gets lower when there's fewer division operators. Let's check the
            /// property <see cref="Entity.SimplifiedRate"/>:
            /// <code>
            /// using System;
            /// using System.Linq;
            /// using AngouriMath;
            /// using AngouriMath.Extensions;
            /// using static AngouriMath.MathS;
            /// 
            /// Console.WriteLine("a / b + b / c".ToEntity().SimplifiedRate);
            /// Console.WriteLine("a / b + b / c".ToEntity().Simplify().SimplifiedRate);
            /// 
            /// 
            /// using var _ = Settings.ComplexityCriteria.Set(
            ///     expr => expr.Nodes.Count(node => node is Entity.Divf)
            /// );
            /// 
            /// Console.WriteLine("a / b + b / c".ToEntity().SimplifiedRate);
            /// Console.WriteLine("a / b + b / c".ToEntity().Simplify().SimplifiedRate);
            /// </code>
            /// Prints
            /// <code>
            /// 24
            /// 24
            /// 2
            /// 1
            /// </code>
            /// By default criteria it cannot simplify it further, however, the custom one
            /// it simplified from 2 to 1.
            /// </example>
            /// <remarks>
            /// <para>
            /// The function itself lives on <see cref="CostModel.Default"/>, with the named
            /// alternatives beside it — <see cref="CostModel.SmallestTree"/>,
            /// <see cref="CostModel.FewestDivisions"/>, <see cref="CostModel.FewestRadicals"/>.
            /// It is referenced rather than repeated so the setting's default and the model
            /// cannot drift apart.
            /// </para>
            /// <para>
            /// The second half of the example above used to need
            /// <c>FromString(expr, useCache: false)</c>, because a parsed expression is cached by
            /// its text and <see cref="Entity.SimplifiedRate"/> cached one rate per instance —
            /// so the number computed under the default criteria was handed back under this one.
            /// That is fixed: a rate is cached only while nobody has scoped this setting, and a
            /// scoped criteria is computed afresh. Reading a rate under a criteria of your own no
            /// longer needs the parser cache defeated to be right.
            /// </para>
            /// </remarks>
            public static Setting<Func<Entity, double>> ComplexityCriteria { get; } =
                new Func<Entity, double>(CostModel.DefaultCost);

            /// <summary>
            /// Settings for the Newton-Raphson's root-search method
            /// e.g.
            /// <code>
            /// using var _ = MathS.Settings.NewtonSolver.Set(new NewtonSetting {
            ///     From = (-10, -10),
            ///     To = (10, 10),
            ///     StepCount = (10, 10),
            ///     Precision = 30
            /// });
            /// ...
            /// </code>
            /// </summary>
            public static Setting<NewtonSetting> NewtonSolver { get; } = new NewtonSetting();

            /// <summary>
            /// The maximum number of linear children of an expression in polynomial solver
            /// considering that there's no more than 1 children with the required variable, for example:
            /// <list type="table">
            ///     <listheader>
            ///         <term>Expression</term>
            ///         <description>Complexity</description>
            ///     </listheader>
            ///     <item>
            ///         <term>(x + 2) ^ 2</term>
            ///         <description>3 [x2, 4x, 4]</description>
            ///     </item>
            ///     <item>
            ///         <term>x + 3 + a</term>
            ///         <description>2 [x, 3 + a]</description>
            ///     </item>
            ///     <item>
            ///         <term>(x + a)(b + c)</term>
            ///         <description>2 [(b + c)x, a(b + c)]</description>
            ///     </item>
            ///     <item>
            ///         <term>(x + 3 + a) / (x + 3)</term>
            ///         <description>2 [x / (x + 3), (3 + a) / (x + 3)]</description>
            ///     </item>
            ///     <item>
            ///         <term>x2 + x + 1</term>
            ///         <description>3 [x2, x, 1]</description>
            ///     </item>
            /// </list>
            /// </summary>
            public static Setting<long> MaxExpansionTermCount { get; } = 2_000;

            /// <summary>
            /// Settings for <see cref="EDecimal"/> precisions of <a href="https://github.com/peteroupc/Numbers">PeterO.Numbers</a>
            /// </summary>
            public static Setting<EContext> DecimalPrecisionContext { get; } =
                new(new EContext(100, ERounding.HalfUp, -100, 1000, false));

            /// <summary>
            /// Whether functions are being read as real-valued or complex-valued. It is a
            /// statement about the reading and not about any one expression -- an
            /// <see cref="Entity"/>'s own <c>Codomain</c> says what a particular node is
            /// declared over, where this says what the library should take a function to be.
            /// </summary>
            /// <remarks>
            /// <para>
            /// <see cref="Domain.Complex"/> by default, which is what AngouriMath has always
            /// done: <c>lim x-&gt;0- ln(x)</c> answers <c>-oo</c>, reading the logarithm along
            /// its complex continuation, since the logarithm of a negative real is
            /// <c>ln|x| + i*pi</c> and only its magnitude is being reported.
            /// </para>
            /// <para>
            /// Under <see cref="Domain.Real"/> a limit approached through values the function
            /// does not take in the reals has no value rather than the continued one. That is
            /// the missing information behind a family of questions -- whether two agreeing
            /// one-sided limits may be promoted to a two-sided one, and whether
            /// <c>sqrt(x^2)</c> is <c>x</c> -- because each of them is answerable one way over
            /// the reals and another over the complex plane, and the library had no way to be
            /// told which was meant.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/719">#719</a>
            /// </para>
            /// <example>
            /// <code>
            /// using var _ = MathS.Settings.Codomain.Set(Domain.Real);
            /// Console.WriteLine("ln(x)".Limit("x", 0, ApproachFrom.Left));
            /// </code>
            /// prints the unevaluated limit, where the default prints <c>-oo</c>.
            /// </example>
            /// </remarks>
            public static Setting<Domain> Codomain { get; } = Domain.Complex;

            /// <summary>
            /// What an algorithm that can run away is allowed to spend before it declines.
            /// </summary>
            /// <remarks>
            /// <para>
            /// <b>Leaving this alone is not the same as setting it to
            /// <see cref="WorkBudget.Unlimited"/>.</b> Unset, each algorithm keeps the budget
            /// it chose for itself -- the Gr&#246;bner path bounds itself by five seconds and
            /// by five structural ceilings, and did so before this setting existed. Set, this
            /// replaces those defaults wherever a budget is honoured, so
            /// <see cref="WorkBudget.Unlimited"/> here means "do not stop", which for a
            /// doubly-exponential algorithm means what it says.
            /// </para>
            /// <para>
            /// A budget in <see cref="WorkBudget.Steps"/> is reproducible and one in
            /// <see cref="WorkBudget.Time"/> is not, so a caller who needs the same answer on
            /// every machine sets the first and leaves the second
            /// <see langword="null"/>. Which one stopped a computation is reported through
            /// <see cref="BudgetRecording"/>.
            /// (<a href="https://github.com/asc-community/AngouriMath/issues/373">#373</a>)
            /// </para>
            /// <para>
            /// It is honoured only where an algorithm asks -- today that is the Gr&#246;bner
            /// system solver. Everything else in the library bounds itself by ceilings of its
            /// own that this does not reach, so setting it does not make an arbitrary call
            /// bounded.
            /// </para>
            /// </remarks>
            /// <example>
            /// <code>
            /// using var _ = MathS.Settings.Budget.Set(new WorkBudget { Steps = 100_000 });
            /// using var recording = BudgetRecording.Start();
            /// var solutions = MathS.Equations("x2 + y2 - 4", "x y - 1").Solve("x", "y");
            /// </code>
            /// </example>
            public static Setting<WorkBudget> Budget { get; } = WorkBudget.Unlimited;
        }

        /// <summary>Returns an <see cref="Entity"/> in polynomial order if possible</summary>
        /// <param name="expr">The unordered <see cref="Entity"/></param>
        /// <param name="variable">The variable of the polynomial</param>
        /// <param name="dst">The ordered result</param>
        /// <returns>
        /// <see langword="true"/> if success,
        /// <see langword="false"/> otherwise (<paramref name="dst"/> will be <see langword="null"/>)
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        /// 
        /// Entity expr = "a x + b (x + 3) + (x + 2) * (x + 3)";
        /// if (TryPolynomial(expr, "x", out var res))
        ///     Console.WriteLine(res);
        /// else
        ///     Console.WriteLine("Cannot get polynomial :((");
        /// 
        /// Entity expr2 = "sin(x) + cos(x)";
        /// if (TryPolynomial(expr2, "x", out var res2))
        ///     Console.WriteLine(res2);
        /// else
        ///     Console.WriteLine("Cannot get polynomial :((");
        /// </code>
        /// Prints
        /// <code>
        /// x ^ 2 + (a + b + 3 + 2) * x + b * 3 + 6
        /// Cannot get polynomial :((
        /// </code>
        /// </example>
        public static bool TryPolynomial(Entity expr, Variable variable,
            [NotNullWhen(true)]
            out Entity? dst) => Simplificator.TryPolynomial(expr, variable, out dst);

        /// <returns>sympy interpretable format</returns>
        /// <param name="expr">An <see cref="Entity"/> representing an expression</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y, a) = Var("x", "y", "a");
        /// var expr = Limit(Integral(Sin(x) / (Cos(x) + Tan(y)), x) / a, y, +oo);
        /// Console.WriteLine(expr);
        /// Console.WriteLine("----------------------------");
        /// Console.WriteLine(ToSympyCode(expr));
        /// </code>
        /// Prints
        /// <code>
        /// limit(integral(sin(x) / (cos(x) + tan(y)), x) / a, y, +oo)
        /// ----------------------------
        /// import sympy
        /// 
        /// x = sympy.Symbol('x')
        /// y = sympy.Symbol('y')
        /// a = sympy.Symbol('a')
        /// 
        /// expr = sympy.limit(sympy.integrate(sympy.sin(x) / (sympy.cos(x) + sympy.tan(y)), x, 1) / a, y, +oo)
        /// </code>
        /// </example>
        public static string ToSympyCode(Entity expr)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("import sympy\n\n");
            // Name rather than Stringize: a variable narrowed with WithCodomain prints as
            // `domain(x, ZZ)` since https://github.com/asc-community/AngouriMath/issues/1022, and
            // that is not a Python identifier. The codomain itself does not survive the export --
            // SymPy would spell it as an assumption on the symbol, which nothing here emits yet.
            foreach (var f in expr.Vars)
                sb.Append($"{f.Name} = sympy.Symbol('{f.Name}')\n");
            sb.Append('\n');
            sb.Append("expr = ").Append(expr.ToSymPy());
            return sb.ToString();
        }

        /// <summary>
        /// Functions and classes related to sets defined here
        /// 
        /// Class <see cref="Set"/> defines true mathematical sets
        /// It supports intersection, union, subtraction
        ///             
        /// <see cref="Set"/>
        /// </summary>
        public static class Sets
        {
            /// <summary>Creates an instance of an empty <see cref="Set"/></summary>
            /// <returns>A <see cref="Set"/> with no elements</returns>
            public static Set Empty => Set.Empty;

            /// <summary>
            /// The number of elements of a set, written <c>card(S)</c>. Counted for a finite
            /// set of numbers; left as written for an infinite set.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.Sets.Card(MathS.Sets.Finite(1, 2, 3)).Evaled);
            /// Console.WriteLine(MathS.Sets.Card(MathS.Sets.R));
            /// </code>
            /// Prints
            /// <code>
            /// 3
            /// #RR
            /// </code>
            /// </example>
            public static Entity Card(Entity set) => new Cardf(set);

            /// <summary>
            /// The set of all subsets of a set, written <c>powerset(S)</c>. Listed for a finite
            /// set, with the empty set and the set itself among the members; an object with a
            /// membership test for an infinite one.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.Sets.PowerSet(MathS.Sets.Finite(1, 2)).Evaled);
            /// Console.WriteLine(MathS.Sets.Finite(1, 3).In(MathS.Sets.PowerSet(MathS.Sets.Z)).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// { {  }, { 2 }, { 1 }, { 1, 2 } }
            /// True
            /// </code>
            /// </example>
            public static Set PowerSet(Entity set) => new Powersetf(set);

            /// <summary>
            /// The statement that <paramref name="sub"/> is a subset of <paramref name="super"/>,
            /// written <c>A subset B</c>: every member of the first is a member of the second.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine(MathS.Sets.Subset(MathS.Sets.Finite(1, 2), MathS.Sets.Z).Evaled);
            /// Console.WriteLine(MathS.Sets.Subset(MathS.Sets.Q, MathS.Sets.Z).Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// True
            /// False
            /// </code>
            /// </example>
            public static Entity Subset(Entity sub, Entity super) => new Subsetf(sub, super);

            /// <summary>
            /// The union of a family of sets indexed by <paramref name="var"/> ranging over
            /// <paramref name="over"/>: <c>union(A_i, i in I)</c>, the set of what is in some
            /// <c>A_i</c>. Folded over a listed index set; a membership object otherwise.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine("union({k, 2 k}, k in {1, 2, 3})".ToEntity().Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// { 1, 2, 4, 3, 6 }
            /// </code>
            /// </example>
            public static Set IndexedUnion(Entity body, Entity var, Entity over) => new IndexedUnionf(var, over, body);

            /// <summary>
            /// The intersection of a family of sets indexed by <paramref name="var"/> ranging
            /// over <paramref name="over"/>: <c>intersection(A_i, i in I)</c>, the set of what
            /// is in every <c>A_i</c>.
            /// </summary>
            public static Set IndexedIntersection(Entity body, Entity var, Entity over) => new IndexedIntersectionf(var, over, body);

            /// <summary>
            /// The complement of <paramref name="set"/> relative to the universe
            /// <paramref name="universe"/>, which is <c>universe \ set</c>: a complement is
            /// always relative to a universe, there being no set of everything.
            /// </summary>
            public static Set Complement(Entity set, Entity universe) => universe.SetSubtract(set);

            /// <summary>
            /// The image of <paramref name="over"/> under the expression <paramref name="body"/>
            /// in <paramref name="var"/>: <c>image(f(x), x in A)</c>, the set of the values
            /// <c>f(x)</c> takes on <c>A</c> -- <c>{ f(x) : x in A }</c>, which is
            /// <c>union({f(x)}, x in A)</c>. Listed over a listed set, an interval by interval
            /// arithmetic where the name occurs once, and a membership object otherwise.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine("image(x^2, x in {1, 2, 3})".ToEntity().Evaled);
            /// Console.WriteLine("image(9 c / 5 + 32, c in (0; 100))".ToEntity().Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// { 1, 4, 9 }
            /// (32; 212)
            /// </code>
            /// </example>
            public static Set Image(Entity body, Entity var, Entity over) => new IndexedUnionf(var, over, new FiniteSet(body));

            /// <summary>
            /// The pre-image of <paramref name="target"/> under the expression <paramref name="body"/>
            /// in <paramref name="var"/> ranging over <paramref name="over"/>:
            /// <c>preimage(f(x), x in A, Y)</c> is <c>{ x in A : f(x) in Y }</c>, solved on
            /// evaluation where the membership is one the statement solver reads.
            /// </summary>
            /// <example>
            /// <code>
            /// Console.WriteLine("preimage(x^2, x in RR, {1})".ToEntity().Evaled);
            /// </code>
            /// Prints
            /// <code>
            /// { 1, -1 }
            /// </code>
            /// </example>
            public static Set PreImage(Entity body, Entity var, Entity over, Entity target) => new ConditionalSet(var, var.In(over) & body.In(target));

            /// <returns>A set of all Complexes/>s</returns>
            public static Set C => SpecialSet.Create(Domain.Complex);

            /// <returns>A set of all Reals/>s</returns>
            public static Set R => SpecialSet.Create(Domain.Real);

            /// <returns>A set of all Rationals/>s</returns>
            public static Set Q => SpecialSet.Create(Domain.Rational);

            /// <returns>A set of all Integers/></returns>
            public static Set Z => SpecialSet.Create(Domain.Integer);

            /// <summary>The non-negative integers <c>{0, 1, 2, ...}</c>, written <c>ZZ*</c>.
            /// Not called the natural numbers, because that name means this set to some and
            /// <see cref="PositiveIntegers"/> to others.
            /// https://github.com/asc-community/AngouriMath/issues/1409</summary>
            public static Set NonNegativeIntegers => SpecialSet.Create("ZZ*");

            /// <summary>The positive integers <c>{1, 2, 3, ...}</c>, written <c>ZZ+</c>.
            /// https://github.com/asc-community/AngouriMath/issues/1409</summary>
            public static Set PositiveIntegers => SpecialSet.Create("ZZ+");

            /// <summary>The prime numbers <c>{2, 3, 5, 7, ...}</c>, written <c>PP</c>.
            /// https://github.com/asc-community/AngouriMath/issues/1450</summary>
            public static Set Primes => SpecialSet.Create("PP");

            /// <summary>
            /// Creates a <see cref="FiniteSet"/> with given elements
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
            public static FiniteSet Finite(params Entity[] entities) => new FiniteSet(entities);

            /// <summary>
            /// Creates a <see cref="FiniteSet"/> with given elements. See
            /// <see cref="MathS.Sets.Finite(Entity[])"/>.
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
            public static FiniteSet Finite(List<Entity> entities) => new FiniteSet((IEnumerable<Entity>)entities);

            /// <summary>
            /// Creates a closed interval
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
            public static Interval Interval(Entity from, Entity to) => new(from, true, to, true);

            /// <summary>
            /// Creates an interval where <paramref name="leftClosed"/> shows whether <paramref name="from"/> is included,
            /// <paramref name="rightClosed"/> shows whether <paramref name="to"/> included.
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
            public static Interval Interval(Entity from, bool leftClosed, Entity to, bool rightClosed) => new(from, leftClosed, to, rightClosed);

            /// <summary>
            /// Creates a node of whether the given element belongs to the given set
            /// </summary>
            /// <returns>A node</returns>
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
            public static Entity ElementInSet(Entity element, Entity set)
                => element.In(set);
        }

        /// <summary>
        /// Implements necessary functions for symbolic computation of limits, derivatives and integrals
        /// </summary>
        public static class Compute
        {
            /// <summary>
            /// Computes Euler phi function
            /// <a href="https://en.wikipedia.org/wiki/Euler%27s_totient_function"/>
            /// If integer x is non-positive, the result will be 0
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Compute;
            /// 
            /// Console.WriteLine(Phi(12));
            /// Console.WriteLine(Phi(13));
            /// </code>
            /// Prints
            /// <code>
            /// 4
            /// 12
            /// </code>
            /// </example>
            public static Integer Phi(Integer integer) => integer.Phi();
            
            /// <summary>
            /// Finds the symbolic form of sine, if can
            /// For example, sin(9/14) is sin(1/2 + 1/7) which
            /// can be expanded as a sine of sum and hence
            /// an analytical (symbolic) form.
            /// </summary>
            /// <param name="angle">
            /// The angle in radians
            /// </param>
            /// <returns>
            /// The sine's symbolic form
            /// or null if cannot find it
            /// </returns>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Compute;
            /// using static AngouriMath.MathS;
            /// 
            /// Console.WriteLine(SymbolicFormOfSine(pi / 3));
            /// Console.WriteLine(SymbolicFormOfSine(pi / 7));
            /// Console.WriteLine(SymbolicFormOfSine(9 * pi / 14));
            /// Console.WriteLine("------------------------------");
            /// Console.WriteLine(SymbolicFormOfCosine(pi / 3));
            /// Console.WriteLine(SymbolicFormOfCosine(pi / 7));
            /// Console.WriteLine(SymbolicFormOfCosine(9 * pi / 14));
            /// </code>
            /// Prints
            /// <code>
            /// sqrt(3) / 2
            /// sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2))
            /// sqrt(1 - sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2)) ^ 2)
            /// ------------------------------
            /// 1/2
            /// sqrt(1 - sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2)) ^ 2)
            /// -sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2))
            /// </code>
            /// </example>
            public static Entity? SymbolicFormOfSine(Entity angle)
                => TrigonometricAngleExpansion.SymbolicFormOfSine(angle)?.InnerSimplified;
            
            /// <summary>
            /// Finds the symbolic form of cosine, if can
            /// For example, cos(9/14) is cos(1/2 + 1/7) which
            /// can be expanded as a cosine of sum and hence
            /// an analytical (symbolic) form.
            /// </summary>
            /// <param name="angle">
            /// The angle in radians
            /// </param>
            /// <returns>
            /// The cosine's symbolic form
            /// or null if cannot find it
            /// </returns>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.Compute;
            /// using static AngouriMath.MathS;
            /// 
            /// Console.WriteLine(SymbolicFormOfSine(pi / 3));
            /// Console.WriteLine(SymbolicFormOfSine(pi / 7));
            /// Console.WriteLine(SymbolicFormOfSine(9 * pi / 14));
            /// Console.WriteLine("------------------------------");
            /// Console.WriteLine(SymbolicFormOfCosine(pi / 3));
            /// Console.WriteLine(SymbolicFormOfCosine(pi / 7));
            /// Console.WriteLine(SymbolicFormOfCosine(9 * pi / 14));
            /// </code>
            /// Prints
            /// <code>
            /// sqrt(3) / 2
            /// sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2))
            /// sqrt(1 - sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2)) ^ 2)
            /// ------------------------------
            /// 1/2
            /// sqrt(1 - sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2)) ^ 2)
            /// -sqrt(1/2 - 1/2 * sqrt(1 - sqrt(1 - (1/6 * (-1 + ((7 + 21 * sqrt(-3)) / 2) ^ (1/3) + ((7 - 21 * sqrt(-3)) / 2) ^ (1/3))) ^ 2) ^ 2))
            /// </code>
            /// </example>
            public static Entity? SymbolicFormOfCosine(Entity angle)
                => TrigonometricAngleExpansion.SymbolicFormOfCosine(angle)?.InnerSimplified;
        }

        /// <summary>
        /// Hangs your <see cref="Entity"/> to a derivative node
        /// (to evaluate instead use <see cref="Entity.Differentiate(Entity.Variable)"/>)
        /// </summary>
        /// <param name="expr">Expression to be hung</param>
        /// <param name="var">Variable over which derivative is taken</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, a) = Var("x", "a");
        /// 
        /// var e1 = Derivative(Sin(Cos(x)), x);
        /// Console.WriteLine(e1);
        /// Console.WriteLine(e1.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e2 = Derivative(Sin(Cos(x)), x, 2);
        /// Console.WriteLine(e2);
        /// Console.WriteLine(e2.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e3 = Integral(Sin(a * x), x);
        /// Console.WriteLine(e3);
        /// Console.WriteLine(e3.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e4 = Integral(Sin(a * x), x, 2);
        /// Console.WriteLine(e4);
        /// Console.WriteLine(e4.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e5 = Limit(Sin(a * x) / x, x, 0);
        /// Console.WriteLine(e5);
        /// Console.WriteLine(e5.InnerSimplified);
        /// </code>
        /// Prints
        /// <code>
        /// derivative(sin(cos(x)), x)
        /// cos(cos(x)) * -sin(x)
        /// -----------------------
        /// derivative(sin(cos(x)), x, 2)
        /// -sin(cos(x)) * -sin(x) * -sin(x) + cos(x) * (-1) * cos(cos(x))
        /// -----------------------
        /// integral(sin(a * x), x)
        /// -cos(a * x) / a
        /// -----------------------
        /// integral(sin(a * x), x, 2)
        /// -sin(a * x) / a / a
        /// -----------------------
        /// limit(sin(a * x) / x, x, 0)
        /// a
        /// </code>
        /// </example>
        public static Entity Derivative(Entity expr, Entity var) => new Derivativef(expr, var, 1);

        /// <summary>
        /// Hangs your <see cref="Entity"/> to a derivative node
        /// (to evaluate instead use <see cref="Entity.Differentiate(Entity.Variable)"/>)
        /// </summary>
        /// <param name="expr">Expression to be hung</param>
        /// <param name="var">Variable over which derivative is taken</param>
        /// <param name="power">Number of times derivative is taken. Only integers will be simplified or evaluated.</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, a) = Var("x", "a");
        /// 
        /// var e1 = Derivative(Sin(Cos(x)), x);
        /// Console.WriteLine(e1);
        /// Console.WriteLine(e1.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e2 = Derivative(Sin(Cos(x)), x, 2);
        /// Console.WriteLine(e2);
        /// Console.WriteLine(e2.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e3 = Integral(Sin(a * x), x);
        /// Console.WriteLine(e3);
        /// Console.WriteLine(e3.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e4 = Integral(Sin(a * x), x, 2);
        /// Console.WriteLine(e4);
        /// Console.WriteLine(e4.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e5 = Limit(Sin(a * x) / x, x, 0);
        /// Console.WriteLine(e5);
        /// Console.WriteLine(e5.InnerSimplified);
        /// </code>
        /// Prints
        /// <code>
        /// derivative(sin(cos(x)), x)
        /// cos(cos(x)) * -sin(x)
        /// -----------------------
        /// derivative(sin(cos(x)), x, 2)
        /// -sin(cos(x)) * -sin(x) * -sin(x) + cos(x) * (-1) * cos(cos(x))
        /// -----------------------
        /// integral(sin(a * x), x)
        /// -cos(a * x) / a
        /// -----------------------
        /// integral(sin(a * x), x, 2)
        /// -sin(a * x) / a / a
        /// -----------------------
        /// limit(sin(a * x) / x, x, 0)
        /// a
        /// </code>
        /// </example>
        public static Entity Derivative(Entity expr, Entity var, int power) => new Derivativef(expr, var, power);

        /// <summary>
        /// Hangs your entity to an integral node
        /// (to evaluate instead use <see cref="Entity.Integrate(Entity.Variable)"/>)
        /// </summary>
        /// <param name="expr">Expression to be hung</param>
        /// <param name="var">Variable over which integral is taken</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, a) = Var("x", "a");
        /// 
        /// var e1 = Derivative(Sin(Cos(x)), x);
        /// Console.WriteLine(e1);
        /// Console.WriteLine(e1.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e2 = Derivative(Sin(Cos(x)), x, 2);
        /// Console.WriteLine(e2);
        /// Console.WriteLine(e2.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e3 = Integral(Sin(a * x), x);
        /// Console.WriteLine(e3);
        /// Console.WriteLine(e3.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e4 = Integral(Sin(a * x), x, 2);
        /// Console.WriteLine(e4);
        /// Console.WriteLine(e4.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e5 = Limit(Sin(a * x) / x, x, 0);
        /// Console.WriteLine(e5);
        /// Console.WriteLine(e5.InnerSimplified);
        /// </code>
        /// Prints
        /// <code>
        /// derivative(sin(cos(x)), x)
        /// cos(cos(x)) * -sin(x)
        /// -----------------------
        /// derivative(sin(cos(x)), x, 2)
        /// -sin(cos(x)) * -sin(x) * -sin(x) + cos(x) * (-1) * cos(cos(x))
        /// -----------------------
        /// integral(sin(a * x), x)
        /// -cos(a * x) / a
        /// -----------------------
        /// integral(sin(a * x), x, 2)
        /// -sin(a * x) / a / a
        /// -----------------------
        /// limit(sin(a * x) / x, x, 0)
        /// a
        /// </code>
        /// </example>
        public static Entity Integral(Entity expr, Entity var) => new Integralf(expr, var, null);

        /// <summary>
        /// Hangs your entity to an integral node
        /// (to evaluate instead use <see cref="Entity.Integrate(Entity.Variable)"/>)
        /// </summary>
        /// <param name="expr">Expression to be hung</param>
        /// <param name="var">Variable over which integral is taken</param>
        /// <param name="from">The lower bound for integrating</param>
        /// <param name="to">The upper bound for integrating</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, a) = Var("x", "a");
        /// 
        /// var e1 = Derivative(Sin(Cos(x)), x);
        /// Console.WriteLine(e1);
        /// Console.WriteLine(e1.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e2 = Derivative(Sin(Cos(x)), x, 2);
        /// Console.WriteLine(e2);
        /// Console.WriteLine(e2.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e3 = Integral(Sin(a * x), x);
        /// Console.WriteLine(e3);
        /// Console.WriteLine(e3.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e4 = Integral(Sin(a * x), x, 2);
        /// Console.WriteLine(e4);
        /// Console.WriteLine(e4.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e5 = Limit(Sin(a * x) / x, x, 0);
        /// Console.WriteLine(e5);
        /// Console.WriteLine(e5.InnerSimplified);
        /// </code>
        /// Prints
        /// <code>
        /// derivative(sin(cos(x)), x)
        /// cos(cos(x)) * -sin(x)
        /// -----------------------
        /// derivative(sin(cos(x)), x, 2)
        /// -sin(cos(x)) * -sin(x) * -sin(x) + cos(x) * (-1) * cos(cos(x))
        /// -----------------------
        /// integral(sin(a * x), x)
        /// -cos(a * x) / a
        /// -----------------------
        /// integral(sin(a * x), x, 2)
        /// -sin(a * x) / a / a
        /// -----------------------
        /// limit(sin(a * x) / x, x, 0)
        /// a
        /// </code>
        /// </example>
        public static Entity Integral(Entity expr, Entity var, Entity from, Entity to) => new Integralf(expr, var, (from, to));

        /// <summary>
        /// Hangs your entity to a limit node
        /// (to evaluate instead use <see cref="Entity.Limit(Entity.Variable, Entity)"/>)
        /// </summary>
        /// <param name="expr">Expression to be hung</param>
        /// <param name="var">Variable over which limit is taken</param>
        /// <param name="dest">Where <paramref name="var"/> approaches (could be finite or infinite)</param>
        /// <param name="approach">From where it approaches</param>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, a) = Var("x", "a");
        /// 
        /// var e1 = Derivative(Sin(Cos(x)), x);
        /// Console.WriteLine(e1);
        /// Console.WriteLine(e1.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e2 = Derivative(Sin(Cos(x)), x, 2);
        /// Console.WriteLine(e2);
        /// Console.WriteLine(e2.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e3 = Integral(Sin(a * x), x);
        /// Console.WriteLine(e3);
        /// Console.WriteLine(e3.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e4 = Integral(Sin(a * x), x, 2);
        /// Console.WriteLine(e4);
        /// Console.WriteLine(e4.InnerSimplified);
        /// Console.WriteLine("-----------------------");
        /// var e5 = Limit(Sin(a * x) / x, x, 0);
        /// Console.WriteLine(e5);
        /// Console.WriteLine(e5.InnerSimplified);
        /// </code>
        /// Prints
        /// <code>
        /// derivative(sin(cos(x)), x)
        /// cos(cos(x)) * -sin(x)
        /// -----------------------
        /// derivative(sin(cos(x)), x, 2)
        /// -sin(cos(x)) * -sin(x) * -sin(x) + cos(x) * (-1) * cos(cos(x))
        /// -----------------------
        /// integral(sin(a * x), x)
        /// -cos(a * x) / a
        /// -----------------------
        /// integral(sin(a * x), x, 2)
        /// -sin(a * x) / a / a
        /// -----------------------
        /// limit(sin(a * x) / x, x, 0)
        /// a
        /// </code>
        /// </example>
        public static Entity Limit(Entity expr, Entity var, Entity dest, ApproachFrom approach = ApproachFrom.BothSides)
            => new Limitf(expr, var, dest, approach);

        /// <summary>
        /// A summation of <paramref name="expr"/> as <paramref name="var"/> runs from
        /// <paramref name="from"/> to <paramref name="to"/> inclusive.
        /// </summary>
        /// <param name="expr">The summand. It may mention <paramref name="var"/>.</param>
        /// <param name="var">
        /// The index, which this <b>binds</b> — it is not a free variable of the result. Naming
        /// <c>i</c> works: it is the imaginary unit everywhere else, but declaring it as the index
        /// is taken as meaning it, throughout this operator and nowhere outside it — and the
        /// same holds of every other binder in the language. See <see cref="Core.Binding"/>.
        /// </param>
        /// <param name="from">The first value of the index.</param>
        /// <param name="to">The last value of the index, inclusive.</param>
        /// <returns>
        /// The sum written out where the bounds are concrete integers and there are not too many
        /// terms; a closed form where the body is a polynomial in the index, whatever the bounds;
        /// and an unevaluated <see cref="Entity.Summationf"/> otherwise — so a body this cannot
        /// read is carried rather than refused.
        /// </returns>
        /// <remarks>
        /// <para>
        /// An empty range sums to <c>0</c>, which is stated rather than left to fall out of the
        /// loop. <a href="https://github.com/asc-community/AngouriMath/issues/248">#248</a>
        /// </para>
        /// <para>
        /// That convention is why a closed form carries a condition: <c>sum(k, k, 1, n)</c> is
        /// <c>(n + n^2)/2</c> only where <c>n >= 0</c>, the range being empty and the sum
        /// <c>0</c> below that while the polynomial is not.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Sum("k", "k", 1, 10).Simplify());
        /// Console.WriteLine(Sum("k", "k", 1, "n"));
        /// </code>
        /// Prints
        /// <code>
        /// 55
        /// sum(k, k, 1, n)
        /// </code>
        /// </example>
        public static Entity Sum(Entity expr, Entity var, Entity from, Entity to)
            => new Summationf(expr, var, from, to);

        /// <summary>
        /// The sum of <paramref name="expr"/> over the members of <paramref name="over"/> as
        /// <paramref name="var"/> ranges over it, each member counted once: written
        /// <c>sum(expr, var in over)</c>. A sum over the roots of a polynomial is a sum over its
        /// set of roots, <c>sum(f(w), w in { w : p(w) = 0 })</c>, with no index and no order.
        /// </summary>
        /// <param name="expr">The term, as a function of <paramref name="var"/>.</param>
        /// <param name="var">The name that ranges over the set, bound by the sum.</param>
        /// <param name="over">The set.</param>
        /// <returns>
        /// The terms added up where the set is finite and its members are known: a listed set of
        /// numbers, or roots of a polynomial that are rational, roots of a quadratic or roots of a
        /// binomial -- those of an irreducible cubic or quartic are kept as the sum, radicals
        /// reading no more simply than it. Evaluated to a
        /// number, the roots it cannot write are found numerically, all of them or none. Left as
        /// written otherwise: a set with a symbol among its members, which may coincide, or a set
        /// that is not finite.
        /// </returns>
        /// <remarks><a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a></remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Sum("w^2", "w", "{ w : w^3 - 2w + 1 = 0 }").Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 4
        /// </code>
        /// </example>
        public static Entity Sum(Entity expr, Entity var, Entity over)
            => new SumOverSetf(expr, var, over);

        /// <summary>
        /// The largest value <paramref name="expr"/> takes as <paramref name="var"/> ranges over
        /// <paramref name="over"/>, written <c>max(expr, var in over)</c>. Answered over a finite
        /// set of numbers, and over a closed interval with numeric ends for an expression whose
        /// extrema are all stationary; left as written otherwise, and where the value is not
        /// attained. https://github.com/asc-community/AngouriMath/issues/1212
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine(MathS.Maximum("sin(t)^3 * cos(t)", "t", "[0; pi/2]").Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 3/16 * sqrt(3)
        /// </code>
        /// </example>
        public static Entity Maximum(Entity expr, Entity var, Entity over) => new Maximumf(expr, var, over);

        /// <summary>The smallest value; see <see cref="Maximum"/>.</summary>
        public static Entity Minimum(Entity expr, Entity var, Entity over) => new Minimumf(expr, var, over);

        /// <summary>The set of points where <see cref="Maximum"/> is taken, written <c>argmax(expr, var in over)</c>.</summary>
        public static Entity Argmax(Entity expr, Entity var, Entity over) => new Argmaxf(expr, var, over);

        /// <summary>The set of points where <see cref="Minimum"/> is taken, written <c>argmin(expr, var in over)</c>.</summary>
        public static Entity Argmin(Entity expr, Entity var, Entity over) => new Argminf(expr, var, over);

        /// <summary>
        /// A product of <paramref name="expr"/> as <paramref name="var"/> runs from
        /// <paramref name="from"/> to <paramref name="to"/> inclusive. Mirrors
        /// <see cref="Sum(Entity, Entity, Entity, Entity)"/>, with an empty range multiplying to
        /// <c>1</c>.
        /// </summary>
        /// <param name="expr">The factor. It may mention <paramref name="var"/>.</param>
        /// <param name="var">The index, which this binds.</param>
        /// <param name="from">The first value of the index.</param>
        /// <param name="to">The last value of the index, inclusive.</param>
        /// <returns>
        /// The product written out where the bounds are concrete integers and there are not too
        /// many terms; a closed form where the factor is a <b>monomial</b> in the index — a
        /// narrower class than the sum's, a product having no linearity to take a sum of terms
        /// apart with; and an unevaluated node otherwise.
        /// </returns>
        /// <remarks>
        /// The closed form's condition is <c>to &gt;= from</c> rather than the sum's
        /// <c>to &gt;= from - 1</c>: at the empty range itself it would be <c>c ^ 0</c>, which is
        /// undefined at <c>c = 0</c> where the empty product is <c>1</c>. Where the index is in
        /// the factor the lower bound must be a concrete integer of at least one, since the
        /// answer is a ratio of factorials and <c>factorial</c> has no value at the negative
        /// integers.
        /// </remarks>
        /// <example>
        /// <code>
        /// using System;
        /// using AngouriMath;
        /// using static AngouriMath.MathS;
        ///
        /// Console.WriteLine(Product("k", "k", 1, 5).Simplify());
        /// </code>
        /// Prints
        /// <code>
        /// 120
        /// </code>
        /// </example>
        public static Entity Product(Entity expr, Entity var, Entity from, Entity to)
            => new Productf(expr, var, from, to);

        /// <summary>Some non-symbolic constants</summary>
        [SuppressMessage("Style", "IDE1006:Naming Styles",
            Justification = "Lowercase constants as written in Mathematics")]
        public static class DecimalConst
        {
            /// <summary><a href="https://en.wikipedia.org/wiki/Pi"/></summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.DecimalConst;
            /// 
            /// Console.WriteLine(pi);
            /// Console.WriteLine(e);
            /// </code>
            /// Prints
            /// <code>
            /// 3.1415926535897932384
            /// 2.7182818284590452353
            /// </code>
            /// </example>
            public static EDecimal pi =>
                InternalAMExtensions.ConstantCache.Lookup(Settings.DecimalPrecisionContext).Pi;

            /// <summary><a href="https://en.wikipedia.org/wiki/E_(mathematical_constant)"/></summary>
            /// <example>
            /// <code>
            /// using System;
            /// using static AngouriMath.MathS.DecimalConst;
            /// 
            /// Console.WriteLine(pi);
            /// Console.WriteLine(e);
            /// </code>
            /// Prints
            /// <code>
            /// 3.1415926535897932384
            /// 2.7182818284590452353
            /// </code>
            /// </example>
            public static EDecimal e =>
                InternalAMExtensions.ConstantCache.Lookup(Settings.DecimalPrecisionContext).E;
        }

        /// <summary>
        /// Symbolic quantum states: superpositions of basis kets with <see cref="Entity"/>
        /// amplitudes.
        /// </summary>
        /// <remarks>
        /// A state is an ordinary expression rather than a new kind of node. <c>Ket</c> builds
        /// <c>apply(ket, 0, 1)</c> for |01&gt;, so a superposition is a sum of products and
        /// everything that reads expressions reads it -- which is why adding two states,
        /// scaling one, and collecting <c>a|x&gt; + b|x&gt;</c> into <c>(a+b)|x&gt;</c> need no
        /// method here. <c>Simplify</c> already does them.
        /// <para/>
        /// What is here is what the ordinary machinery has no reason to know: factoring a
        /// definite qubit out of a superposition, and the two questions that are about states
        /// as states rather than as expressions.
        /// <para/>
        /// There are deliberately no gates. A gate acts *on* a state rather than being one,
        /// and operators, circuits and measurement belong to a quantum computing library
        /// rather than to a computer algebra system.
        /// </remarks>
        public static class Quantum
        {
            /// <summary>
            /// The basis ket over the given qubits, each 0 or 1: <c>Ket(0, 1)</c> is |01&gt;.
            /// </summary>
            /// <example>
            /// <code>
            /// var bell = (MathS.Quantum.Ket(0, 0) + MathS.Quantum.Ket(1, 1)) / MathS.Sqrt(2);
            /// Console.WriteLine(bell.Simplify());
            /// </code>
            /// </example>
            public static Entity Ket(params int[] qubits)
                => Entity.Variable.CreateVariableUnchecked(Functions.Quantum.QuantumState.KetHead)
                    .Apply(qubits.Select(q => (Entity)q).ToArray());

            /// <summary>
            /// The state written as a tensor product where its leading or trailing qubits are
            /// in a definite basis state, or the expression unchanged where none is.
            /// </summary>
            /// <remarks>
            /// <c>|001&gt; + |011&gt;</c> becomes <c>|0&gt; (|0&gt; + |1&gt;) |1&gt;</c>. A
            /// state with no definite qubit at either end comes back as it went in -- including
            /// an entangled one such as <c>|00&gt; + |11&gt;</c>, which has no factorisation at
            /// all, and a product state such as <c>(|0&gt;+|1&gt;)(|0&gt;+|1&gt;)</c>, whose
            /// separability this does not yet detect.
            /// </remarks>
            public static Entity TensorFactorize(Entity state)
                => Functions.Quantum.Factorization.TensorFactorize(state) ?? state;

            /// <summary>
            /// A product of states over consecutive qubits multiplied back out into a single
            /// superposition -- the inverse of <c>TensorFactorize</c> -- or the expression unchanged
            /// where it is not such a product.
            /// </summary>
            public static Entity TensorExpand(Entity state)
                => Functions.Quantum.Factorization.TensorExpand(state) ?? state;

            /// <summary>
            /// Whether the amplitudes square-sum to one, or <see langword="null"/> where that
            /// cannot be settled -- a symbolic amplitude need not have a decidable modulus.
            /// </summary>
            public static bool? IsNormalized(Entity state)
                => Functions.Quantum.Factorization.IsNormalized(state);

            /// <summary>
            /// Whether two states differ only by a global phase, which is the difference no
            /// measurement can see -- or <see langword="null"/> where that cannot be settled.
            /// </summary>
            public static bool? EqualUpToGlobalPhase(Entity left, Entity right)
                => Functions.Quantum.Factorization.EqualUpToGlobalPhase(left, right);
        }

        /// <summary>
        /// Some operations on booleans are stored here
        /// </summary>
        public static class Boolean
        {

            /// <summary>
            /// Combines all possible values of <paramref name="variables"/>
            /// and has the last column as the result of the function
            /// </summary>
            /// <example>
            /// <code>
            /// using AngouriMath;
            /// using System;
            /// using static AngouriMath.MathS;
            /// 
            /// var (x, y) = Var("x", "y");
            /// var myXor = Disjunction(Conjunction(x, Negation(y)), Conjunction(y, Negation(x)));
            /// Console.WriteLine(myXor);
            /// Console.WriteLine(MathS.Boolean.BuildTruthTable(myXor, x, y).ToString(multilineFormat: true));
            /// </code>
            /// Prints
            /// <code>
            /// x and not y or y and not x
            /// Matrix[4 x 3]
            /// False   False   False   
            /// False   True    True    
            /// True    False   True    
            /// True    True    False   
            /// </code>
            /// </example>
            public static Matrix? BuildTruthTable(Entity expression, params Variable[] variables)
                => BooleanSolver.BuildTruthTable(expression, variables);


            /// <summary>
            /// Creates a boolean
            /// </summary>
            public static Entity.Boolean Create(bool b)
                => Entity.Boolean.Create(b);

            /// <summary>
            /// One of the Boolean's states, which also behaves as Entity
            /// That is, hangable
            /// </summary>
            [ConstantField] public static readonly Entity.Boolean True = Entity.Boolean.True;

            /// <summary>
            /// One of the Boolean's states, which also behaves as Entity
            /// That is, hangable
            /// </summary>
            [ConstantField] public static readonly Entity.Boolean False = Entity.Boolean.False;
        }

        /// <summary>
        /// A few functions convenient to use in industrial projects
        /// to keep the system more reliable and distribute computations
        /// to other threads
        /// </summary>
        public static class Multithreading
        {
            /// <summary>
            /// Sets the thread-local cancellation token
            /// </summary>
            /// <param name="token"></param>
            /// <example>
            /// <code>
            /// Entity eq = "a e sin(x ^ 14 + 3)3 + a b c d sin(x ^ 14 + 2)4 - k d sin(x ^ 14 + 3)2 + sin(x ^ 14 + 3) + e = 0";
            /// 
            /// using var tokenSource = new CancellationTokenSource();
            /// tokenSource.CancelAfter(millisecondsDelay: 1000);
            /// Multithreading.SetLocalCancellationToken(tokenSource.Token);
            /// 
            /// var task = Task.Run(() =&gt; eq.Solve("x"));
            /// 
            /// try
            /// {
            ///     while (!task.IsCompleted)
            ///     {
            ///         Thread.Sleep(100);
            ///         Console.WriteLine("Not completed yet. Waiting 100 ms...");    
            ///     }
            ///     Console.WriteLine(task.Result);
            /// }
            /// catch (AggregateException e)
            /// {
            ///     if (e.InnerExceptions.AsEnumerable().Any(c => c is OperationCanceledException))
            ///         Console.WriteLine("Operation cancelled");
            ///     else
            ///         throw;
            /// }
            /// </code>
            /// Prints
            /// <code>
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Not completed yet. Waiting 100 ms...
            /// Operation cancelled
            /// </code>
            /// </example>
            public static void SetLocalCancellationToken(CancellationToken token) => MultithreadingFunctional.SetLocalCancellationToken(token);
        }

        /// <summary>
        /// Some additional functions that would be barely
        /// ever used by the user, but kept for "just in case" as public
        /// </summary>
        public static class Utils
        {
            /// <summary>
            /// Performs the expansion operation over the given variable
            /// </summary>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS.Utils;
            /// 
            /// Entity expr = "(x + 2)(a + b + 2x) + x + sin(x)";
            /// Console.WriteLine(SmartExpandOver(expr, "x"));
            /// </code>
            /// Prints
            /// <code>
            /// x * 2 * x + x * (a + b) + 2 * 2 * x + 2 * (a + b) + x + sin(x)
            /// </code>
            /// </example>
            public static Entity SmartExpandOver(Entity expr, Variable x)
            {
                var linChildren = Sumf.LinearChildren(expr);
                
                List<Entity> nodes = new();

                foreach (var linChild in linChildren)
                {
                    var children = TreeAnalyzer.SmartExpandOver(linChild, n => n.ContainsNode(x));
                    if (children is null)
                        return expr;
                    nodes.AddRange(children);
                }

                return TreeAnalyzer.MultiHangBinary(nodes, (a, b) => a + b);
            }

            /// <summary>
            /// Extracts a polynomial with integer powers
            /// </summary>
            /// <param name="expr">From which to extract the polynomial</param>
            /// <param name="variable">Over which variable to extract the polynomial</param>
            /// <param name="dst">
            /// Where to put the dictionary, whose keys
            /// are powers, and values - coefficients
            /// </param>
            /// <returns>Whether the input expression is a valid polynomial</returns>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS.Utils;
            /// 
            /// Entity expr = "(x^2 + 2)(a + b + 2x) + x + sin(h)";
            /// if (TryGetPolynomial(expr, "x", out var dict))
            ///     foreach (var (pow, coef) in dict)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// Console.WriteLine("------------------------");
            /// Entity expr1 = "sin(x) + a";
            /// if (TryGetPolynomial(expr1, "x", out var dict1))
            ///     foreach (var (pow, coef) in dict1)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// else
            ///     Console.WriteLine("Failed to interpret as polynomial");
            /// Console.WriteLine("------------------------");
            /// Entity expr2 = "(x + a)(b + x) + a + 2 + x";
            /// if (TryGetPolyQuadratic(expr2, "x", out var a, out var b, out var c))
            ///     Console.WriteLine($"The expr is ({a}) * x^2 + ({b}) * x + ({c})");
            /// Console.WriteLine("------------------------");
            /// Entity expr3 = "(b + x) + a + 2 + x";
            /// if (TryGetPolyLinear(expr3, "x", out var a1, out var b1))
            ///     Console.WriteLine($"The expr is ({a1}) * x + ({b1})");
            /// </code>
            /// Prints
            /// <code>
            /// Pow: 2. Coef: 1 * 1 ^ 2 * a + 1 * 1 ^ 2 * b
            /// Pow: 3. Coef: 1 * 1 ^ 2 * 2
            /// Pow: 0. Coef: 2 * a + 2 * b + sin(h)
            /// Pow: 1. Coef: 1 * 2 * 2 + 1
            /// ------------------------
            /// Failed to interpret as polynomial
            /// ------------------------
            /// The expr is (1 * 1 ^ 2) * x^2 + (1 * b + 1 * a + 1) * x + (a * b + a + 2)
            /// ------------------------
            /// The expr is (1 + 1) * x + (b + a + 2)
            /// </code>
            /// </example>
            public static bool TryGetPolynomial(Entity expr, Variable variable,
            [NotNullWhen(true)] out Dictionary<EInteger, Entity>? dst)
                => TreeAnalyzer.TryGetPolynomial(expr, variable, out dst);

            /// <summary>
            /// Extracts the linear coefficient and the bias over a variable
            /// a x + b
            /// </summary>
            /// <param name="expr">From which to extract the linear function</param>
            /// <param name="variable">Over which to extract</param>
            /// <param name="a">The linear coefficient</param>
            /// <param name="b">The bias</param>
            /// <returns>Whether the extract was successful</returns>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS.Utils;
            /// 
            /// Entity expr = "(x^2 + 2)(a + b + 2x) + x + sin(h)";
            /// if (TryGetPolynomial(expr, "x", out var dict))
            ///     foreach (var (pow, coef) in dict)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// Console.WriteLine("------------------------");
            /// Entity expr1 = "sin(x) + a";
            /// if (TryGetPolynomial(expr1, "x", out var dict1))
            ///     foreach (var (pow, coef) in dict1)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// else
            ///     Console.WriteLine("Failed to interpret as polynomial");
            /// Console.WriteLine("------------------------");
            /// Entity expr2 = "(x + a)(b + x) + a + 2 + x";
            /// if (TryGetPolyQuadratic(expr2, "x", out var a, out var b, out var c))
            ///     Console.WriteLine($"The expr is ({a}) * x^2 + ({b}) * x + ({c})");
            /// Console.WriteLine("------------------------");
            /// Entity expr3 = "(b + x) + a + 2 + x";
            /// if (TryGetPolyLinear(expr3, "x", out var a1, out var b1))
            ///     Console.WriteLine($"The expr is ({a1}) * x + ({b1})");
            /// </code>
            /// Prints
            /// <code>
            /// Pow: 2. Coef: 1 * 1 ^ 2 * a + 1 * 1 ^ 2 * b
            /// Pow: 3. Coef: 1 * 1 ^ 2 * 2
            /// Pow: 0. Coef: 2 * a + 2 * b + sin(h)
            /// Pow: 1. Coef: 1 * 2 * 2 + 1
            /// ------------------------
            /// Failed to interpret as polynomial
            /// ------------------------
            /// The expr is (1 * 1 ^ 2) * x^2 + (1 * b + 1 * a + 1) * x + (a * b + a + 2)
            /// ------------------------
            /// The expr is (1 + 1) * x + (b + a + 2)
            /// </code>
            /// </example>
            public static bool TryGetPolyLinear(Entity expr, Variable variable,
            [NotNullWhen(true)] out Entity? a,
            [NotNullWhen(true)] out Entity? b)
                => TreeAnalyzer.TryGetPolyLinear(expr, variable, out a, out b);

            /// <summary>
            /// Extracts the quadratic coefficient, linear coefficient and the bias over a variable
            /// a x ^ 2 + b x + c
            /// </summary>
            /// <param name="expr">From which to extract the quadratic function</param>
            /// <param name="variable">Over which to extract</param>
            /// <param name="a">The quadratic coefficient</param>
            /// <param name="b">The linear coefficient</param>
            /// <param name="c">The bias</param>
            /// <returns>Whether the extract was successful</returns>
            /// <example>
            /// <code>
            /// using System;
            /// using AngouriMath;
            /// using static AngouriMath.MathS.Utils;
            /// 
            /// Entity expr = "(x^2 + 2)(a + b + 2x) + x + sin(h)";
            /// if (TryGetPolynomial(expr, "x", out var dict))
            ///     foreach (var (pow, coef) in dict)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// Console.WriteLine("------------------------");
            /// Entity expr1 = "sin(x) + a";
            /// if (TryGetPolynomial(expr1, "x", out var dict1))
            ///     foreach (var (pow, coef) in dict1)
            ///         Console.WriteLine($"Pow: {pow}. Coef: {coef}");
            /// else
            ///     Console.WriteLine("Failed to interpret as polynomial");
            /// Console.WriteLine("------------------------");
            /// Entity expr2 = "(x + a)(b + x) + a + 2 + x";
            /// if (TryGetPolyQuadratic(expr2, "x", out var a, out var b, out var c))
            ///     Console.WriteLine($"The expr is ({a}) * x^2 + ({b}) * x + ({c})");
            /// Console.WriteLine("------------------------");
            /// Entity expr3 = "(b + x) + a + 2 + x";
            /// if (TryGetPolyLinear(expr3, "x", out var a1, out var b1))
            ///     Console.WriteLine($"The expr is ({a1}) * x + ({b1})");
            /// </code>
            /// Prints
            /// <code>
            /// Pow: 2. Coef: 1 * 1 ^ 2 * a + 1 * 1 ^ 2 * b
            /// Pow: 3. Coef: 1 * 1 ^ 2 * 2
            /// Pow: 0. Coef: 2 * a + 2 * b + sin(h)
            /// Pow: 1. Coef: 1 * 2 * 2 + 1
            /// ------------------------
            /// Failed to interpret as polynomial
            /// ------------------------
            /// The expr is (1 * 1 ^ 2) * x^2 + (1 * b + 1 * a + 1) * x + (a * b + a + 2)
            /// ------------------------
            /// The expr is (1 + 1) * x + (b + a + 2)
            /// </code>
            /// </example>
            public static bool TryGetPolyQuadratic(Entity expr, Variable variable,
            [NotNullWhen(true)] out Entity? a,
            [NotNullWhen(true)] out Entity? b,
            [NotNullWhen(true)] out Entity? c)
                => TreeAnalyzer.TryGetPolyQuadratic(expr, variable, out a, out b, out c);
        }
    }
}
