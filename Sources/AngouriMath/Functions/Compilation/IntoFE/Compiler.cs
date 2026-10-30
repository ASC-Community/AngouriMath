//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using static AngouriMath.Entity;
using static AngouriMath.Core.FastExpression;

namespace AngouriMath
{
    partial record Entity
    {
        /// <summary>
        /// Whether this node, its children aside, has a compiled form: false by default, as
        /// <see cref="CompileNode"/>'s default is to throw, and true beside every override that
        /// compiles rather than throws.
        /// </summary>
        private protected virtual bool CompilesItself => false;

        /// <summary>
        /// Whether the compiler has a form for every node of this expression, so that a caller
        /// that would fall back rather than fail can ask first instead of catching
        /// <see cref="UncompilableNodeException"/>. The variables are not checked against a
        /// list: which ones an expression is compiled over is the caller's to say.
        /// https://github.com/asc-community/AngouriMath/issues/1603
        /// </summary>
        internal bool HasCompiledForm => CompilesItself && DirectChildren.All(child => child.HasCompiledForm);

        private protected virtual void CompileNode(Compiler compiler)
            => throw new UncompilableNodeException(
                $"`{Stringize()}`, a node of type {GetType()}, does not support compilation. "
                + "Feel free to report it as an issue on our official repository.");
        /// <summary>
        /// Recursive compilation that pushes intructions to the stack (<see cref="Compiler.Instructions"/>)
        /// </summary>
        internal void InnerCompile(Compiler compiler)
        {
            if (compiler.Cache.TryGetValue(this, out var cacheLine) && cacheLine >= 0)
                compiler.Instructions.Add(new(InstructionType.LOAD_CACHE, cacheLine));
            else
            {
                CompileNode(compiler);
                if (cacheLine < 0) // If cache doesn't store this entity, cacheLine will be uninitialized = 0
                {
                    cacheLine = ~cacheLine;
                    compiler.Cache[this] = cacheLine;
                    compiler.Instructions.Add(new(InstructionType.SAVE_CACHE, cacheLine));
                }
            }
        }

        public partial record Number
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler) =>
                compiler.Instructions.Add(new(InstructionType.PUSH_CONST, Value: ((Complex)this).ToNumerics()));
        }

        public partial record Variable
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler) =>
                compiler.Instructions.Add(new(InstructionType.PUSH_VAR,
                    compiler.VarNamespace.TryGetValue(this, out var slot)
                    ? slot
                    // A compiled expression is a function of the variables it was compiled
                    // over, so one it does not mention is not something it can be given a
                    // value for. Looking the name up and letting the lookup fail said only
                    // that some key was not present in some dictionary.
                    : throw new UncompilableNodeException(
                        $"{this} is not among the variables the expression is being compiled over" +
                        (compiler.VarNamespace.Count is 0
                            ? ", which are none"
                            : $", which are {string.Join(", ", compiler.VarNamespace.Keys)}"))));

            /// <inheritdoc/>
            public override int GetHashCode()
                => Name.GetHashCode();
        }

        public partial record Constant
        {
            private protected override bool CompilesItself => true;

            // A constant is its value. The named ones are substituted by value before
            // compiling; the base of ln and exp is Euler's number standing in the operator's
            // definition, which substitution leaves alone (#994), so it arrives here.
            private protected override void CompileNode(Compiler compiler) =>
                compiler.Instructions.Add(new(InstructionType.PUSH_CONST, Value: Value.ToNumerics()));
        }

        partial record Matrix
        {
            private protected override void CompileNode(Compiler compiler) =>
                throw new UncompilableNodeException($"A {nameof(Matrix)} cannot be compiled: `{Stringize()}`");
        }

        // Each function and operator processing
        // Note: We pop values when executing instructions, so we add instructions in reverse child order
        public partial record Sumf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Addend.InnerCompile(compiler);
                Augend.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SUM));
            }
        }

        public partial record Minusf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Subtrahend.InnerCompile(compiler);
                Minuend.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MINUS));
            }
        }

        public partial record Mulf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Multiplicand.InnerCompile(compiler);
                Multiplier.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MUL));
            }
        }

        public partial record Divf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Divisor.InnerCompile(compiler);
                Dividend.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_DIV));
            }
        }

        public partial record Modf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Divisor.InnerCompile(compiler);
                Dividend.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MOD));
            }
        }

        public partial record Powf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Exponent.InnerCompile(compiler);
                Base.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_POW));
            }
        }

        public partial record Sinf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SIN));
            }
        }

        public partial record Cosf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_COS));
            }
        }

        public partial record Secantf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SECANT));
            }
        }

        public partial record Cosecantf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_COSECANT));
            }
        }

        public partial record Tanf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_TAN));
            }
        }

        public partial record Cotanf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_COTAN));
            }
        }

        public partial record Logf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                // Unlike AngouriMath which accepts Base as the first parameter,
                // Complex.Log accepts it as the second parameter
                // So we reverse reverse the child order -> same as child order
                Base.InnerCompile(compiler);
                Antilogarithm.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_LOG));
            }
        }

        public partial record Arcsinf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCSIN));
            }
        }

        public partial record Arccosf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCCOS));
            }
        }

        public partial record Arctanf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCTAN));
            }
        }

        public partial record Arccotanf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCCOTAN));
            }
        }

        public partial record Arcsecantf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCSECANT));
            }
        }

        public partial record Arccosecantf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARCCOSECANT));
            }
        }

        public partial record Factorialf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_FACTORIAL));
            }
        }

        public partial record Derivativef
        {
            private protected override void CompileNode(Compiler compiler) =>
                throw new UncompilableNodeException($"A derivative cannot be compiled: `{Stringize()}`");
        }

        public partial record Integralf
        {
            private protected override void CompileNode(Compiler compiler) =>
                throw new UncompilableNodeException($"An integral cannot be compiled: `{Stringize()}`");
        }

        public partial record Limitf
        {
            private protected override void CompileNode(Compiler compiler) =>
                throw new UncompilableNodeException($"A limit cannot be compiled: `{Stringize()}`");
        }

        // The floors, the rounding and the extremes, which the solver's numerical search needs
        // compiled to run on `floor(x) = x/2 + 1/3` or `max(x, 1) = 2 x` at all.
        // https://github.com/asc-community/AngouriMath/issues/1603
        public partial record Floorf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_FLOOR));
            }
        }

        public partial record Ceilf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CEIL));
            }
        }

        public partial record Roundf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ROUND));
            }
        }

        // A statement compiles to its truth value: 1 where it holds, 0 where it does not, and NaN
        // where it has none, as evaluation leaves one. A provided and a piecewise compile every
        // case and keep the one whose predicate holds, which needs no jump in the instructions.
        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Boolean
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
                => compiler.Instructions.Add(new(InstructionType.PUSH_CONST, Value: Value ? 1 : 0));
        }

        public partial record Notf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_NOT));
            }
        }

        // The Iverson bracket of a statement is its truth value.
        public partial record Iversonf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler) => Argument.InnerCompile(compiler);
        }

        public partial record Andf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_AND));
            }
        }

        public partial record Orf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_OR));
            }
        }

        public partial record Xorf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_XOR));
            }
        }

        public partial record Impliesf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Conclusion.InnerCompile(compiler);
                Assumption.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_IMPLIES));
            }
        }

        public partial record Equalsf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_EQUAL));
            }
        }

        public partial record Lessf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_LESS));
            }
        }

        public partial record LessOrEqualf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_LESS_OR_EQUAL));
            }
        }

        public partial record Greaterf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_GREATER));
            }
        }

        public partial record GreaterOrEqualf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_GREATER_OR_EQUAL));
            }
        }

        public partial record Providedf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Predicate.InnerCompile(compiler);
                Expression.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PROVIDED));
            }
        }

        // The first case whose predicate holds, and NaN where none does: from the last case up,
        // each is kept where its predicate holds and the cases below it are kept elsewhere.
        public partial record Piecewise
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                compiler.Instructions.Add(new(InstructionType.PUSH_CONST, Value: double.NaN));
                foreach (var @case in Cases.Reverse())
                {
                    @case.Predicate.InnerCompile(compiler);
                    @case.Expression.InnerCompile(compiler);
                    compiler.Instructions.Add(new(InstructionType.CALL_SELECT));
                }
            }
        }

        // The binomial coefficient, the factorial powers and the double factorial, on the
        // interpreter's terms. https://github.com/asc-community/AngouriMath/issues/2195
        public partial record CatalanNumberf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CATALAN_NUMBER));
            }
        }

        public partial record DoubleFactorialf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_DOUBLE_FACTORIAL));
            }
        }

        public partial record Binomialf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Lower.InnerCompile(compiler);
                Upper.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_BINOMIAL));
            }
        }

        public partial record FallingFactorialf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Order.InnerCompile(compiler);
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_FALLING_FACTORIAL));
            }
        }

        public partial record RisingFactorialf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Order.InnerCompile(compiler);
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_RISING_FACTORIAL));
            }
        }

        // The Lambert W function on its branch. https://github.com/asc-community/AngouriMath/issues/2195
        public partial record LambertWf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                Branch.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_LAMBERTW));
            }
        }

        // The polygamma functions and the harmonic numbers. https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Polygammaf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                Order.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_POLYGAMMA));
            }
        }

        public partial record HarmonicNumberf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Order.InnerCompile(compiler);
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_HARMONIC_NUMBER));
            }
        }

        // The number-theoretic functions, exact at a whole argument and NaN at any other.
        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Mobiusf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MOBIUS));
            }
        }

        public partial record PrimeNuf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PRIME_NU));
            }
        }

        public partial record PrimeOmegaf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PRIME_OMEGA));
            }
        }

        public partial record Primef
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PRIME));
            }
        }

        public partial record PrimePif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PRIME_PI));
            }
        }

        public partial record DivisorSigmaf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                Order.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_DIVISOR_SIGMA));
            }
        }

        public partial record Valuationf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Prime.InnerCompile(compiler);
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_VALUATION));
            }
        }

        // A membership whose set the compilers rewrite into a statement about the element is gone by the time
        // a node is compiled, and what remains is one in a set of numbers, decided where evaluation decides it.
        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Set
        {
            public partial record Inf
            {
                private protected override bool CompilesItself => SupSet is SpecialSet special && AngouriMath.Numerics.NumberStatements.Of(special) is not null;

                private protected override void CompileNode(Compiler compiler)
                {
                    if (SupSet is not SpecialSet special || AngouriMath.Numerics.NumberStatements.Of(special) is not { } set)
                    {
                        base.CompileNode(compiler);
                        return;
                    }
                    Element.InnerCompile(compiler);
                    compiler.Instructions.Add(new(InstructionType.CALL_MEMBER, (int)set));
                }
            }
        }

        // A congruence of whole numbers, with no truth value at any other.
        public partial record Congruentf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Modulus.InnerCompile(compiler);
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CONGRUENT));
            }
        }

        // Divisibility of whole numbers, with no truth value at any other.
        public partial record Dividesf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Dividend.InnerCompile(compiler);
                Divisor.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_DIVIDES));
            }
        }

        public partial record Maxf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MAX));
            }
        }

        public partial record Minf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Right.InnerCompile(compiler);
                Left.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_MIN));
            }
        }

        public partial record Signumf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SIGNUM));
            }
        }

        public partial record Absf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ABS));
            }
        }

        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Conjugatef
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CONJ));
            }
        }

        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Argf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ARG));
            }
        }

        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Ref
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_RE));
            }
        }

        // https://github.com/asc-community/AngouriMath/issues/2195
        public partial record Imf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_IM));
            }
        }

        public partial record Phif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_PHI));
            }
        }

        // The special functions, in double precision as the kernels define them.
        // https://github.com/asc-community/AngouriMath/issues/1607
        public partial record Erff
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ERF));
            }
        }

        public partial record Erfcf
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ERFC));
            }
        }

        public partial record Erfif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_ERFI));
            }
        }

        public partial record Eif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_EI));
            }
        }

        public partial record Lif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_LI));
            }
        }

        public partial record Sif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SI));
            }
        }

        public partial record Cif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CI));
            }
        }

        public partial record Shif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_SHI));
            }
        }

        public partial record Chif
        {
            private protected override bool CompilesItself => true;

            private protected override void CompileNode(Compiler compiler)
            {
                Argument.InnerCompile(compiler);
                compiler.Instructions.Add(new(InstructionType.CALL_CHI));
            }
        }
    }
}

namespace AngouriMath.Core
{
    /// <summary>
    /// Compiled function (not to a delegate, but to AM's VM readable format)
    /// </summary>
    public partial class FastExpression
    {
        /// <summary>The <ref name="Cache"/> stores the saved cache number if zero/positive,
        /// or the bitwise complement of the unsaved cache number if negative.</summary>
        internal sealed record Compiler(List<Instruction> Instructions,
            IReadOnlyDictionary<Variable, int> VarNamespace, IDictionary<Entity, int> Cache)
        {
            /// <summary>Returns a compiled expression. Allows to boost substitution a lot</summary>
            /// <param name="func">The function to be compiled</param>
            /// <param name="variables">Must be equal to func's variables (ignoring constants)</param>
            internal static FastExpression Compile(Entity func, IEnumerable<Variable> variables)
            {
                var varNamespace = new Dictionary<Variable, int>();
                int id = 0;
                foreach (var varName in variables)
                    if (!varName.IsConstant)
                        varNamespace[varName] = id++;
                foreach (var constant in Variable.NamedConstants.Values)
                    func = func.Substitute(constant, constant.Value);
                func = Compilation.CompiledMembership.Rewritten(func);
                Compilation.StatementPositions.Check(func);
                var visited = new HashSet<Entity>();
                var cache = new Dictionary<Entity, int>();
                foreach (var node in func.Nodes)
                    if (node is Number or Variable)
                        continue; // Don't store simple nodes in cache
                    else if (visited.Contains(node))
                    {
                        if (!cache.ContainsKey(node))
                            cache.Add(node, ~cache.Count); // Unsaved by default
                    }
                    else visited.Add(node);
                var compiler = new Compiler(new(), varNamespace, cache);
                func.InnerCompile(compiler);
                return new(id, compiler.Instructions, compiler.Cache.Count);
            }
        }
    }
}