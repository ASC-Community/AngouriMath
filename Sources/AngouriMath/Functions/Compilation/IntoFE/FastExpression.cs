//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Runtime.CompilerServices;
using AngouriMath.Core.Exceptions;
using Logic = AngouriMath.Core.Compilation.IntoLinq.ThreeValuedLogic;


namespace AngouriMath.Core
{
    public sealed partial class FastExpression
    {
        internal enum InstructionType
        {
            PUSH_VAR,
            PUSH_CONST,
            LOAD_CACHE,
            SAVE_CACHE,

            // 1-arg functions
            CALL_SIN = 50,
            CALL_COS,
            CALL_SECANT,
            CALL_COSECANT,
            CALL_TAN,
            CALL_COTAN,
            CALL_ARCSIN,
            CALL_ARCCOS,
            CALL_ARCTAN,
            CALL_ARCCOTAN,
            CALL_ARCSECANT,
            CALL_ARCCOSECANT,
            CALL_FACTORIAL,
            CALL_SIGNUM,
            CALL_ABS,
            CALL_PHI,
            CALL_FLOOR,
            CALL_CEIL,
            CALL_ROUND,
            CALL_ERF,
            CALL_ERFC,
            CALL_ERFI,
            CALL_EI,
            CALL_LI,
            CALL_SI,
            CALL_CI,
            CALL_SHI,
            CALL_CHI,
            CALL_CONJ,
            CALL_ARG,
            CALL_RE,
            CALL_IM,
            CALL_NOT,
            CALL_DOUBLE_FACTORIAL,
            CALL_CATALAN_NUMBER,
            CALL_MOBIUS,
            CALL_PRIME_NU,
            CALL_PRIME_OMEGA,
            CALL_PRIME,
            CALL_PRIME_PI,
            // Membership in the set of numbers its reference names, a NumberStatements.NumberSet.
            CALL_MEMBER,

            // 2-arg functions
            CALL_SUM = 100,
            CALL_MINUS,
            CALL_MUL,
            CALL_DIV,
            CALL_MOD,
            CALL_POW,
            CALL_LOG,
            CALL_MAX,
            CALL_MIN,
            CALL_LESS,
            CALL_LESS_OR_EQUAL,
            CALL_GREATER,
            CALL_GREATER_OR_EQUAL,
            CALL_EQUAL,
            CALL_AND,
            CALL_OR,
            CALL_XOR,
            CALL_IMPLIES,
            // A value where its predicate holds, and NaN elsewhere.
            CALL_PROVIDED,
            CALL_BINOMIAL,
            CALL_FALLING_FACTORIAL,
            CALL_RISING_FACTORIAL,
            CALL_LAMBERTW,
            CALL_POLYGAMMA,
            CALL_HARMONIC_NUMBER,
            CALL_DIVISOR_SIGMA,
            CALL_VALUATION,
            CALL_DIVIDES,

            // 3-arg: a value where its predicate holds, and the one below it elsewhere.
            CALL_SELECT = 150,
        }
        internal sealed partial record Instruction(InstructionType Type, int Reference = -1, System.Numerics.Complex Value = default)
        {
            public override string ToString() =>
                Type
                + (Reference == -1 ? "" : Reference.ToString())
                + (Type != InstructionType.PUSH_CONST ? "" : Value.ToString());
        }

        /// <summary>
        /// The working stack and the cache of repeated subexpressions, one set per thread
        /// rather than one per compiled expression.
        /// </summary>
        /// <remarks>
        /// They used to be instance fields, which made a compiled expression unsafe to call
        /// from more than one thread at a time: two calls interleaved their pushes and pops
        /// and the second to finish found the stack in a state it did not put it in. Worse,
        /// the damage was permanent -- the count check throws before anything is popped, so
        /// one racing call left the leftovers behind and every later call failed too, on one
        /// thread or many. See https://github.com/asc-community/AngouriMath/issues/637.
        /// <para/>
        /// Per thread rather than per call so that calling a compiled expression still
        /// allocates nothing, which is the point of compiling it. Neither buffer carries
        /// anything from one call to the next: the stack is emptied on the way in, and the
        /// cache is only ever read at a slot the same call has already written.
        /// </remarks>
        private sealed class Scratch
        {
            public readonly Stack<System.Numerics.Complex> Stack = new();
            public System.Numerics.Complex[] Cache = System.Array.Empty<System.Numerics.Complex>();
        }

        [System.ThreadStatic] private static Scratch? scratch;

        private readonly List<Instruction> instructions;
        private readonly int varCount;
        private readonly int cacheCount;

        /// <summary>
        /// You cannot modify this function once it is sealed. The final user will never access to its
        /// direct instructions
        /// </summary>
        internal FastExpression(int varCount, List<Instruction> instructions, int cacheCount)
        {
            this.varCount = varCount;
            this.instructions = instructions;
            this.cacheCount = cacheCount;
        }

        /// <summary>Calls the compiled function (synonym to <see cref="Substitute(System.Numerics.Complex[])"/>)</summary>
        /// <param name="values">List arguments in the same order in which you compiled the function</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public System.Numerics.Complex Call(params System.Numerics.Complex[] values) => Substitute(values);

        /// <summary>Calls the compiled function (synonym to <see cref="Call(System.Numerics.Complex[])"/>)</summary>
        /// <param name="values">List arguments in the same order in which you compiled the function</param>
        /// <exception cref="WrongNumberOfArgumentsException">
        /// Thrown when the length of <paramref name="values"/> does not match the number of variables compiled.
        /// </exception>
        public System.Numerics.Complex Substitute(params System.Numerics.Complex[] values)
        {
            if (values.Length != varCount)
                throw new WrongNumberOfArgumentsException($"Wrong number of parameters: Expected {varCount} but {values.Length} provided");
            var mine = scratch ??= new Scratch();
            var stack = mine.Stack;
            stack.Clear();
            var cache = mine.Cache;
            if (cache.Length < cacheCount)
                mine.Cache = cache = new System.Numerics.Complex[cacheCount];
            foreach (var instruction in instructions)
                switch (instruction.Type)
                {
                    case InstructionType.PUSH_VAR:
                        stack.Push(values[instruction.Reference]);
                        break;
                    case InstructionType.PUSH_CONST:
                        stack.Push(instruction.Value);
                        break;
                    case InstructionType.LOAD_CACHE:
                        stack.Push(cache[instruction.Reference]);
                        break;
                    case InstructionType.SAVE_CACHE:
                        cache[instruction.Reference] = stack.Peek();
                        break;
                    case InstructionType.CALL_SUM:
                        stack.Push(stack.Pop() + stack.Pop());
                        break;
                    case InstructionType.CALL_MINUS:
                        stack.Push(stack.Pop() - stack.Pop());
                        break;
                    // Real where both sides are, so that an infinity keeps an imaginary part of 0:
                    // https://github.com/asc-community/AngouriMath/issues/2222
                    case InstructionType.CALL_MUL:
                        stack.Push(AngouriMath.Core.Compilation.RealProducts.Product(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_DIV:
                        stack.Push(AngouriMath.Core.Compilation.RealProducts.Quotient(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_MOD:
                        stack.Push(Core.Compilation.RealOnly.Mod(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_MAX:
                        stack.Push(Core.Compilation.RealOnly.Max(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_MIN:
                        stack.Push(Core.Compilation.RealOnly.Min(stack.Pop(), stack.Pop()));
                        break;
                    // Componentwise, as the interpreter takes them, and to even at a half for
                    // the rounding.
                    case InstructionType.CALL_FLOOR:
                    {
                        var z = stack.Pop();
                        stack.Push(new System.Numerics.Complex(System.Math.Floor(z.Real), System.Math.Floor(z.Imaginary)));
                        break;
                    }
                    case InstructionType.CALL_CEIL:
                    {
                        var z = stack.Pop();
                        stack.Push(new System.Numerics.Complex(System.Math.Ceiling(z.Real), System.Math.Ceiling(z.Imaginary)));
                        break;
                    }
                    case InstructionType.CALL_ROUND:
                    {
                        var z = stack.Pop();
                        stack.Push(new System.Numerics.Complex(
                            System.Math.Round(z.Real, System.MidpointRounding.ToEven), System.Math.Round(z.Imaginary, System.MidpointRounding.ToEven)));
                        break;
                    }
                    case InstructionType.CALL_POW:
                        stack.Push(System.Numerics.Complex.Pow(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_SIN:
                        stack.Push(System.Numerics.Complex.Sin(stack.Pop()));
                        break;
                    case InstructionType.CALL_COS:
                        stack.Push(System.Numerics.Complex.Cos(stack.Pop()));
                        break;
                    case InstructionType.CALL_SECANT:
                        stack.Push(1 / System.Numerics.Complex.Cos(stack.Pop()));
                        break;
                    case InstructionType.CALL_COSECANT:
                        stack.Push(1 / System.Numerics.Complex.Sin(stack.Pop()));
                        break;
                    case InstructionType.CALL_TAN:
                        stack.Push(System.Numerics.Complex.Tan(stack.Pop()));
                        break;
                    case InstructionType.CALL_COTAN:
                        stack.Push(1 / System.Numerics.Complex.Tan(stack.Pop()));
                        break;
                    case InstructionType.CALL_LOG:
                        stack.Push(System.Numerics.Complex.Log(stack.Pop(), stack.Pop().Real));
                        break;
                    case InstructionType.CALL_ARCSIN:
                        stack.Push(Core.Compilation.ComplexBranches.Arcsin(stack.Pop()));
                        break;
                    case InstructionType.CALL_ARCCOS:
                        stack.Push(System.Numerics.Complex.Acos(stack.Pop()));
                        break;
                    case InstructionType.CALL_ARCTAN:
                        stack.Push(System.Numerics.Complex.Atan(stack.Pop()));
                        break;
                    case InstructionType.CALL_ARCCOTAN:
                        stack.Push(System.Numerics.Complex.Atan(1 / stack.Pop()));
                        break;
                    case InstructionType.CALL_ARCSECANT:
                        stack.Push(System.Numerics.Complex.Acos(1 / stack.Pop()));
                        break;
                    case InstructionType.CALL_ARCCOSECANT:
                        stack.Push(Core.Compilation.ComplexBranches.Arccosecant(stack.Pop()));
                        break;
                    // In double precision, as the kernels define them.
                    // https://github.com/asc-community/AngouriMath/issues/1607
                    case InstructionType.CALL_ERF:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Erf(stack.Pop()));
                        break;
                    case InstructionType.CALL_ERFC:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Erfc(stack.Pop()));
                        break;
                    case InstructionType.CALL_ERFI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Erfi(stack.Pop()));
                        break;
                    case InstructionType.CALL_EI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Ei(stack.Pop()));
                        break;
                    case InstructionType.CALL_LI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Li(stack.Pop()));
                        break;
                    case InstructionType.CALL_SI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Si(stack.Pop()));
                        break;
                    case InstructionType.CALL_CI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Ci(stack.Pop()));
                        break;
                    case InstructionType.CALL_SHI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Shi(stack.Pop()));
                        break;
                    case InstructionType.CALL_CHI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Chi(stack.Pop()));
                        break;
                    case InstructionType.CALL_FACTORIAL:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Factorial(stack.Pop()));
                        break;
                    case InstructionType.CALL_SIGNUM:
                        stack.Push(stack.Pop().Signum());
                        break;
                    case InstructionType.CALL_ABS:
                        stack.Push(stack.Pop().Abs());
                        break;
                    case InstructionType.CALL_PHI:
                        stack.Push(AngouriMath.Numerics.SpecialFunctions.Phi(stack.Pop()));
                        break;
                    case InstructionType.CALL_CONJ:
                        stack.Push(AngouriMath.Numerics.ComplexParts.Conj(stack.Pop()));
                        break;
                    case InstructionType.CALL_ARG:
                        stack.Push(AngouriMath.Numerics.ComplexParts.Arg(stack.Pop()));
                        break;
                    case InstructionType.CALL_RE:
                        stack.Push(AngouriMath.Numerics.ComplexParts.Re(stack.Pop()));
                        break;
                    case InstructionType.CALL_IM:
                        stack.Push(AngouriMath.Numerics.ComplexParts.Im(stack.Pop()));
                        break;
                    // A truth value is 1, 0, or NaN where there is none, as evaluation leaves one;
                    // the comparisons and the connectives are the LINQ compiler's.
                    // https://github.com/asc-community/AngouriMath/issues/2195
                    case InstructionType.CALL_LESS:
                        stack.Push(Truth(Logic.Less(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_LESS_OR_EQUAL:
                        stack.Push(Truth(Logic.LessOrEqual(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_GREATER:
                        stack.Push(Truth(Logic.Greater(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_GREATER_OR_EQUAL:
                        stack.Push(Truth(Logic.GreaterOrEqual(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_EQUAL:
                        stack.Push(Truth(Logic.Equal(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_NOT:
                        stack.Push(Truth(Logic.Not(Truth(stack.Pop()))));
                        break;
                    case InstructionType.CALL_AND:
                        stack.Push(Truth(Logic.And(Truth(stack.Pop()), Truth(stack.Pop()))));
                        break;
                    case InstructionType.CALL_OR:
                        stack.Push(Truth(Logic.Or(Truth(stack.Pop()), Truth(stack.Pop()))));
                        break;
                    case InstructionType.CALL_XOR:
                        stack.Push(Truth(Logic.Xor(Truth(stack.Pop()), Truth(stack.Pop()))));
                        break;
                    case InstructionType.CALL_IMPLIES:
                        stack.Push(Truth(Logic.Implies(Truth(stack.Pop()), Truth(stack.Pop()))));
                        break;
                    case InstructionType.CALL_PROVIDED:
                    {
                        var value = stack.Pop();
                        stack.Push(Truth(stack.Pop()) == true ? value : double.NaN);
                        break;
                    }
                    // The binomial coefficient, the factorial powers and the double factorial.
                    // https://github.com/asc-community/AngouriMath/issues/2195
                    case InstructionType.CALL_BINOMIAL:
                        stack.Push(AngouriMath.Numerics.CombinatorialFunctions.Binomial(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_FALLING_FACTORIAL:
                        stack.Push(AngouriMath.Numerics.CombinatorialFunctions.FallingFactorial(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_RISING_FACTORIAL:
                        stack.Push(AngouriMath.Numerics.CombinatorialFunctions.RisingFactorial(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_LAMBERTW:
                        stack.Push(AngouriMath.Numerics.LambertWFunction.LambertW(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_POLYGAMMA:
                        stack.Push(AngouriMath.Numerics.PolygammaFunction.Polygamma(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_HARMONIC_NUMBER:
                        stack.Push(AngouriMath.Numerics.PolygammaFunction.HarmonicNumber(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_MOBIUS:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.Mobius(stack.Pop()));
                        break;
                    case InstructionType.CALL_PRIME_NU:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.PrimeNu(stack.Pop()));
                        break;
                    case InstructionType.CALL_PRIME_OMEGA:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.PrimeOmega(stack.Pop()));
                        break;
                    case InstructionType.CALL_PRIME:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.Prime(stack.Pop()));
                        break;
                    case InstructionType.CALL_PRIME_PI:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.PrimePi(stack.Pop()));
                        break;
                    case InstructionType.CALL_DIVISOR_SIGMA:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.DivisorSigma(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_VALUATION:
                        stack.Push(AngouriMath.Numerics.NumberTheoreticFunctions.Valuation(stack.Pop(), stack.Pop()));
                        break;
                    case InstructionType.CALL_MEMBER:
                        stack.Push(Truth(AngouriMath.Numerics.NumberStatements.In(stack.Pop(), instruction.Reference)));
                        break;
                    case InstructionType.CALL_DIVIDES:
                        stack.Push(Truth(AngouriMath.Numerics.NumberStatements.Divides(stack.Pop(), stack.Pop())));
                        break;
                    case InstructionType.CALL_CATALAN_NUMBER:
                        stack.Push(AngouriMath.Numerics.CombinatorialFunctions.CatalanNumber(stack.Pop()));
                        break;
                    case InstructionType.CALL_DOUBLE_FACTORIAL:
                        stack.Push(AngouriMath.Numerics.CombinatorialFunctions.DoubleFactorial(stack.Pop()));
                        break;
                    case InstructionType.CALL_SELECT:
                    {
                        var value = stack.Pop();
                        var holds = Truth(stack.Pop()) == true;
                        var otherwise = stack.Pop();
                        stack.Push(holds ? value : otherwise);
                        break;
                    }
                }
            if (stack.Count != 1)
                throw new AngouriBugException($"Unused values remain in the stack: {stack.Count} instead of 1");
            return stack.Pop();
        }

        /// <summary>A truth value on the stack: 1 where it holds, 0 where it does not, NaN where it has none.</summary>
        private static System.Numerics.Complex Truth(bool? value) => value is { } holds ? (holds ? 1 : 0) : double.NaN;

        /// <summary>The truth value a number on the stack stands for: none but for exactly 1 or 0.</summary>
        private static bool? Truth(System.Numerics.Complex value) => value == 1 ? true : value == 0 ? false : null;

        /// <summary>Might be useful for debug if a function works too slowly</summary>
        public override string ToString() => string.Join(" \n| ", instructions);
    }
}