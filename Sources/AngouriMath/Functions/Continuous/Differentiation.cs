//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Transformations;
using PeterO.Numbers;

namespace AngouriMath
{
    // Adding function Derive to Entity
    partial record Entity
    {
        /// <summary>
        /// Finds the symbolical derivative of the given expression
        /// </summary>
        /// <param name="variable">
        /// Over which variable to find the derivative
        /// </param>
        /// <returns>
        /// The derived expression which might contain <see cref="Derivativef"/> nodes,
        /// or the initial one
        /// </returns>
        /// <example>
        /// <code>
        /// using System;
        /// using static AngouriMath.MathS;
        /// 
        /// var (x, y) = Var("x", "y");
        /// var expr1 = Sqr(x) + Sin(y * x);
        /// Console.WriteLine(expr1);
        /// Console.WriteLine(expr1.Differentiate(x));
        /// Console.WriteLine(expr1.Differentiate(y));
        /// Console.WriteLine(expr1.Differentiate(y).Differentiate(x));
        /// </code>
        /// Prints
        /// <code>
        /// x ^ 2 + sin(y * x)
        /// 2 * x + cos(y * x) * y
        /// cos(y * x) * x
        /// -sin(y * x) * y * x + cos(y * x) 
        /// </code>
        /// </example>
        public Entity Differentiate(Variable variable)
            // Nothing varies with respect to a name that cannot vary, so the answer is 0 --
            // and it is an answer, not a refusal, which is why it is given here rather than
            // left as an unresolved Derivativef for the nodes that build one.
            //
            // The test is whether the name evaluates to a number, not whether it is spelled
            // like a constant: a name a *binder* declares can vary even when it is spelled
            // `pi`, and it evaluates to itself. That is what keeps this compatible with
            // https://github.com/asc-community/AngouriMath/issues/984, whose answer to
            // `derivative(pi ^ 2, pi)` is over the variable the binder holds. This call has
            // no binder in it, and the constant arrives directly in the position that says
            // what varies.
            // https://github.com/asc-community/AngouriMath/issues/993
            => variable.Evaled is Number ? 0
                : Transformation.Differentiation(variable).ApplyOrKeep(this);

        /// <summary>
        /// What <see cref="Differentiate(Variable)"/> does, reachable by
        /// <see cref="Transformation.Differentiation(Variable)"/> without going back
        /// through the public method and round again.
        /// </summary>
        internal Entity DifferentiateOnce(Variable variable)
            => InnerDifferentiate(variable).InnerSimplified;

        /// <summary>
        /// Internal differentiation function
        /// </summary>
        /// <param name="variable">To derive over</param>
        /// <returns>
        /// The differentiated expression, or the Derivative node -- and zero for a node with no
        /// rule of its own that does not mention the variable: it is a constant in it, and the
        /// derivative of a constant is not an open question. A set is not a constant, whatever
        /// it mentions: <c>{ 1, 2 } + sin(x)</c> is the set <c>{ 1 + sin(x), 2 + sin(x) }</c>, and
        /// its derivative is <c>{ cos(x) }</c>, which the derivative node reaches elementwise and
        /// a zero would flatten to the number <c>cos(x)</c>.
        /// </returns>
        protected virtual Entity InnerDifferentiate(Variable variable)
            => ContainsNode(variable) || this is Set ? new Derivativef(this, variable, 1) : Integer.Zero;

        /// <summary>
        /// Whether the node is a constant in <paramref name="variable"/>, which the modulus, the
        /// sign, the floor and their kin ask before their own rule: <c>|c|</c> is a constant in
        /// <c>x</c> whether or not <c>c</c> is known to be real, and <c>floor(c)</c> one with no
        /// condition on <c>c</c>. <c>|c| x</c> was differentiated to <c>derivative(|c|, x) x + |c|</c>,
        /// which every rule downstream read as a function of <c>x</c>.
        /// </summary>
        private bool IsConstantIn(Variable variable) => !ContainsNode(variable);

        partial record Variable
        {
            
            /// <inheritdoc/>
            /// <remarks>
            /// A name that cannot vary contributes nothing to any derivative, so
            /// <c>d(anything)/d(pi)</c> is <c>0</c> and not <c>1</c> even where the name
            /// matches, which is what
            /// <a href="https://github.com/asc-community/AngouriMath/issues/993">#993</a> is
            /// about. The test is whether the name evaluates to a number, not whether it is
            /// spelled like a constant: a name a binder declares can vary even when it is
            /// spelled <c>pi</c>, and it evaluates to itself.
            ///
            /// And where it does vary, the comparison is of the node, not its name. A
            /// <see cref="Constant"/> and a <see cref="Variable"/> can share a spelling — that
            /// is what a binder over <c>pi</c> produces — and comparing the spelling made
            /// <c>derivative(arccos(0) * pi, pi)</c> differentiate the <c>pi / 2</c> that
            /// <c>arccos(0)</c> simplifies to as though it were the index.
            /// <a href="https://github.com/asc-community/AngouriMath/issues/984">#984</a>
            /// </remarks>
            protected override Entity InnerDifferentiate(Variable variable) =>
                variable.Evaled is Number ? 0
                : this == variable ? 1 : 0;
        }

        partial record Matrix
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) => Elementwise(e => e.InnerDifferentiate(variable));
        }

        /// <summary>
        /// <paramref name="power"/> raw passes, with nothing simplified in between.
        /// <see cref="Derivativef"/>'s simplification needs exactly this and not the public
        /// overload: it asks for the derivative and keeps the node when what comes back is
        /// still a <see cref="Derivativef"/>, so a simplification anywhere inside would ask
        /// that same node to simplify, which asks for the derivative again, without end.
        /// </summary>
        internal Entity InnerDifferentiate(Variable x, int power)
        {
            var ent = this;
            // A negative power integrates, exactly as the public overload does -- dropping that
            // here turned derivative(apply(f, x), x, -1) into apply(f, x) rather than into the
            // integral, because a loop that never runs returns the input and the input is not a
            // Derivativef, so the caller took it for a resolved answer.
            if (power < 0)
                for (var _ = 0; _ < -power; _++)
                    ent = ent.Integrate(x);
            else
                for (var _ = 0; _ < power; _++)
                    ent = ent.InnerDifferentiate(x);
            return ent;
        }

        /// <summary>Derives over <paramref name="x"/> <paramref name="power"/> times</summary>
        public Entity Differentiate(Variable x, int power)
        {
            var ent = this;
            // As above -- and only for the differentiating direction, since a negative power
            // integrates and an antiderivative with respect to something that cannot vary has
            // no value to give at all.
            // https://github.com/asc-community/AngouriMath/issues/993
            if (power > 0 && x.Evaled is Number)
                return 0;
            if (power < 0)
                for (var _ = 0; _ < -power; _++)
                    ent = ent.Integrate(x);
            else
                for (var _ = 0; _ < power; _++)
                    // DifferentiateOnce, not InnerDifferentiate: the raw chain rule leaves
                    // every `0 *` and `* 1` it produces in place, and the next pass
                    // differentiates those too, so the expression compounds rather than
                    // reduces. Differentiate(Variable) reaches DifferentiateOnce through the
                    // transformation, which is why one pass of it answered and n passes of
                    // this did not.
                    // https://github.com/asc-community/AngouriMath/issues/1002
                    ent = ent.DifferentiateOnce(x);
            return ent;
        }

        partial record Number
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) => EvalNumerical().IsNaN ? this : 0;
        }

        // Each function and operator processing
        partial record Sumf
        {
            // (a + b)' = a' + b'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Augend.InnerDifferentiate(variable) + Addend.InnerDifferentiate(variable);
        }

        partial record Minusf
        {
            // (a - b)' = a' - b'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Minuend.InnerDifferentiate(variable) - Subtrahend.InnerDifferentiate(variable);
        }

        partial record Mulf
        {
            // (a * b)' = a' * b + b' * a
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Multiplier.InnerDifferentiate(variable) * Multiplicand + Multiplicand.InnerDifferentiate(variable) * Multiplier;
        }

        partial record Divf
        {
            // (a / b)' = (a' * b - b' * a) / b^2
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                (Dividend.InnerDifferentiate(variable) * Divisor - Divisor.InnerDifferentiate(variable) * Dividend) / Divisor.Pow(2);
        }

        partial record Modf
        {
            // a % b is a - b * floor(a / b), so away from the jumps its derivative is
            // a' - b' * floor(a / b) -- and where b is constant that is simply a'. At a
            // multiple of the divisor it is not differentiable at all, which is why the
            // general case is left alone rather than answered with the piecewise-constant
            // formula: floor is not a node this library has, so writing the general case
            // would mean writing something that is wrong at the jumps.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Divisor.ContainsNode(variable)
                    ? new Derivativef(this, variable, 1)
                    : Dividend.InnerDifferentiate(variable);
        }

        partial record Powf
        {
            // (a ^ b)' = e ^ (ln(a) * b) * (a' * b / a + ln(a) * b')
            // (a ^ const)' = const * a ^ (const - 1) * a'
            // (const ^ b)' = e^b * b'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Exponent == Integer.One
                ? Base.InnerDifferentiate(variable).Provided(Base.DomainCondition) // don't create x^0 which is undefined for x=0
                : Exponent.Evaled is Number exp
                ? exp * Base.Pow(exp - 1) * Base.InnerDifferentiate(variable)
                : Base is Number
                ? Base.Pow(Exponent) * MathS.Ln(Base) * Exponent.InnerDifferentiate(variable)
                : Base.Pow(Exponent) * (Base.InnerDifferentiate(variable) * Exponent / Base + MathS.Ln(Base) * Exponent.InnerDifferentiate(variable));
        }

        partial record Sinf
        {
            // sin(a)' = cos(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Argument.Cos() * Argument.InnerDifferentiate(variable);
        }

        partial record Erff
        {
            // erf(a)' = 2/sqrt(pi) e^(-a^2) a', from its definition as an integral.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, -MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Erfcf
        {
            // erfc(a)' = -2/sqrt(pi) e^(-a^2) a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, -MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Erfif
        {
            // erfi(a)' = 2/sqrt(pi) e^(a^2) a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Eif
        {
            // Ei(a)' = e^a/a a', the integrand of its definition.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Pow(MathS.e, Argument) / Argument * Argument.InnerDifferentiate(variable);
        }

        partial record Lif
        {
            // li(a)' = a'/ln(a)
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Argument.InnerDifferentiate(variable) / MathS.Ln(Argument);
        }

        partial record Sif
        {
            // Si(a)' = sin(a)/a a', the integrand of its definition, and so for the other three.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Sin(Argument) / Argument * Argument.InnerDifferentiate(variable);
        }

        partial record Cif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Cos(Argument) / Argument * Argument.InnerDifferentiate(variable);
        }

        partial record Shif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Hyperbolic.Sinh(Argument) / Argument * Argument.InnerDifferentiate(variable);
        }

        partial record Chif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Hyperbolic.Cosh(Argument) / Argument * Argument.InnerDifferentiate(variable);
        }

        partial record Cosf
        {
            // cos(a)' = -sin(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -Argument.Sin() * Argument.InnerDifferentiate(variable);
        }

        partial record Tanf
        {
            // tan(a)' = 1 / cos(a) ^ 2 * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / Argument.Cos().Pow(2) * Argument.InnerDifferentiate(variable);
        }

        partial record Secantf
        {
            // sec(a)' = sec(a) * tan(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                this * Argument.Tan() * Argument.Differentiate(variable);
        }

        partial record Cosecantf
        {
            // csc(a)' = -csc(a) * cotan(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -this * Argument.Cotan() * Argument.Differentiate(variable);
        }

        partial record Arcsecantf
        {
            // asec(a) = 1 / (sqrt(1 - 1 / a2)a2) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => 1 / (MathS.Sqrt(1 - 1 / Argument.Pow(2)) * Argument.Pow(2)) * Argument.Differentiate(variable);
        }

        partial record Arccosecantf
        {
            // asec(a) = 1 / (sqrt(1 - 1 / a2)a2) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => -1 / (MathS.Sqrt(1 - 1 / Argument.Pow(2)) * Argument.Pow(2)) * Argument.Differentiate(variable);
        }

        partial record Cotanf
        {
            // cot(a)' = -1 / sin(a) ^ 2 * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -1 / Argument.Sin().Pow(2) * Argument.InnerDifferentiate(variable);
        }

        partial record Logf
        {
            // log_b(a)' = (ln(a) / ln(b))' = (ln(a)' * ln(b) - ln(a) * ln(b)') / ln(b)^2 = (a' / a * ln(b) - ln(a) * b' / b) / ln(b)^2
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                (Antilogarithm.InnerDifferentiate(variable) / Antilogarithm * MathS.Ln(Base)
                - MathS.Ln(Antilogarithm) * Base.InnerDifferentiate(variable) / Base)
                / MathS.Ln(Base).Pow(2);
        }

        partial record Arcsinf
        {
            // arcsin(x)' = 1 / sqrt(1 - x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / MathS.Sqrt(1 - MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Arccosf
        {
            // arccos(x)' = -1 / sqrt(1 - x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -1 / MathS.Sqrt(1 - MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Arctanf
        {
            // arctan(x)' = 1 / (1 + x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / (1 + MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Arccotanf
        {
            // arccotan(x)' = -1 / (1 + x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -1 / (1 + MathS.Sqr(Argument)) * Argument.InnerDifferentiate(variable);
        }

        partial record Factorialf
        {
            /// <summary>
            /// <c>(x!)' = Γ(x + 1) ψ(x + 1)</c>, which needs a symbolic gamma and polygamma the
            /// library does not have — so this declines rather than answering.
            /// </summary>
            /// <remarks>
            /// <para>
            /// It used to answer <see cref="Number.Real.NaN"/>, which says the derivative <i>does
            /// not exist</i>. It does: <c>x!</c> is <c>Γ(x + 1)</c> and the library already
            /// evaluates it away from the integers — <c>(1/2)!</c> is <c>0.886…</c> — so it is
            /// smooth except at the poles, and <c>d/dx x!</c> at 3 is an ordinary
            /// <c>Γ'(4) ≈ 3.966</c>. Saying <c>NaN</c> was a wrong answer rather than a missing
            /// feature, and it propagated: one factorial anywhere turned an otherwise fine
            /// derivative into <c>NaN</c>.
            /// https://github.com/asc-community/AngouriMath/issues/958
            /// </para>
            /// <para>
            /// Returning the unevaluated derivative is what every other node the library cannot
            /// differentiate already does — <c>apply(f, x)</c> among them — and it is what
            /// <c>AGENTS.md</c> asks for: unevaluated means "I could not settle this", <c>NaN</c>
            /// means "this does not exist", and only the first is true here.
            /// </para>
            /// <para>
            /// Computing it properly is
            /// <a href="https://github.com/asc-community/AngouriMath/issues/171">#171</a>, which is
            /// a separate and larger piece of work; this rule stays correct once that lands.
            /// </para>
            /// </remarks>
            protected override Entity InnerDifferentiate(Variable variable)
                => base.InnerDifferentiate(variable);
        }

#pragma warning disable IDE0054 // Use compound assignment
        partial record Derivativef
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Var == variable
                ? this with { Iterations = Iterations + 1 }
                : base.InnerDifferentiate(variable);
        }

        partial record Integralf
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Var == variable
                ? Range is var (from, to)
                  // https://math.stackexchange.com/a/139191/627798
                  ? to.InnerDifferentiate(variable) * Expression.Substitute(variable, to) -
                    from.InnerDifferentiate(variable) * Expression.Substitute(variable, from)
                  : Expression
                : base.InnerDifferentiate(variable);
        }
#pragma warning restore IDE0054 // Use compound assignment

        partial record SumOverSetf
        {
            /// <inheritdoc/>
            /// <remarks>
            /// Term by term, where the set does not depend on the variable: the derivative of a
            /// sum of finitely many terms is the sum of their derivatives, and a sum over a set
            /// that is not finite is not evaluated in the first place. The name the sum binds is
            /// not a free variable of it, so nothing varies with it.
            /// https://github.com/asc-community/AngouriMath/issues/1285
            /// </remarks>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Var == variable ? Integer.Zero
                : Over.ContainsNode(variable) ? base.InnerDifferentiate(variable)
                : New(Expression.InnerDifferentiate(variable), Var, Over);
        }

        partial record Limitf
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                // TODO: What is a derivative of a limit?

                // See https://math.stackexchange.com/a/1048570/627798:
                // The derivative itself is a limit, so we can exchange limits if possible. -- Happypig375
                base.InnerDifferentiate(variable);
        }

        partial record Signumf
        {
            // sgn is flat either side of zero and has no derivative at zero, so the
            // derivative is 0 wherever it exists. Saying so with a condition is the same
            // stance Absf takes just below, whose derivative is likewise undefined at zero.
            //
            // Flat only for a real-valued argument. Off the real line sgn(z) is z / |z|, which
            // turns with z: sgn(2 + i * x) is (2 + i * x) / sqrt(4 + x^2), and nothing about
            // it is flat. Where the argument is not shown to be real the derivative is left
            // unevaluated rather than answered with the real-line formula.
            // https://github.com/asc-community/AngouriMath/issues/1186
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.IsRealValued(Argument, variable)
                    ? Integer.Zero.Provided(!Argument.EqualTo(Integer.Zero))
                    : MathS.Derivative(this, variable);
        }

        partial record Absf
        {
            // |f|' = sgn(f) * f' is a fact about a real-valued f. For a complex one |f| is
            // sqrt(Re(f)^2 + Im(f)^2), which this library has no node to differentiate with:
            // |i / x| is 1 / |x|, whose derivative is -sgn(x) / x^2, where the formula gives
            // sgn(i / x) * (-i / x^2) = sgn(x) / x^2, the wrong sign -- and through l'Hopital's
            // rule the limit of (i / x) / |i / x| at +oo read as 1 where it is i.
            //
            // A numeric factor off the real line is taken out first, exactly, since |c * g| is
            // |c| * |g| on the whole plane: the derivative of |i * x| is that of |x|. Where the
            // argument is then not shown to be real the derivative is left unevaluated.
            // https://github.com/asc-community/AngouriMath/issues/1186
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.TryTakeNumericModulusOut(Argument, out var withoutThePhase)
                    ? withoutThePhase.InnerDifferentiate(variable)
                    : TreeAnalyzer.IsRealValued(Argument, variable)
                    ? MathS.Signum(Argument).Provided(!Argument.EqualTo(Integer.Zero)) * Argument.InnerDifferentiate(variable)
                    : MathS.Derivative(this, variable);
        }

        partial record Maxf
        {
            // Where both are real, max(u, v) is (u + v + |u - v|)/2, so its derivative is
            // (u' + v' + sgn(u - v)(u' - v'))/2 wherever the two differ, and there is none where
            // they cross: the corner |t| has at 0. A comparison says nothing off the real line, so
            // arguments not shown to be real are left unevaluated, as |f|' is. Without this the
            // derivative was always left as written, and Newton's method, which compiles it, could
            // not run on `max(x, 1) = 2 x`. https://github.com/asc-community/AngouriMath/issues/1603
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.IsRealValued(Left - Right, variable)
                    ? ((Left.InnerDifferentiate(variable) + Right.InnerDifferentiate(variable)
                        + MathS.Signum(Left - Right) * (Left.InnerDifferentiate(variable) - Right.InnerDifferentiate(variable))) / 2)
                        .Provided(!(Left - Right).EqualTo(Integer.Zero))
                    : MathS.Derivative(this, variable);
        }

        partial record Minf
        {
            // As max, with the sign of the difference the other way: min(u, v) is
            // (u + v - |u - v|)/2 where both are real.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.IsRealValued(Left - Right, variable)
                    ? ((Left.InnerDifferentiate(variable) + Right.InnerDifferentiate(variable)
                        - MathS.Signum(Left - Right) * (Left.InnerDifferentiate(variable) - Right.InnerDifferentiate(variable))) / 2)
                        .Provided(!(Left - Right).EqualTo(Integer.Zero))
                    : MathS.Derivative(this, variable);
        }

        partial record Floorf
        {
            // Flat between consecutive integers and discontinuous at each of them, so the
            // derivative is 0 wherever it exists and nowhere at an integer argument. Saying
            // that with a condition is the stance Signumf and Absf take above; SymPy
            // instead leaves the derivative unevaluated, which says less, and this library
            // has Providedf to say more with.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : Integer.Zero.Provided(!Argument.In(MathS.Sets.Z));
        }

        partial record Ceilf
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : Integer.Zero.Provided(!Argument.In(MathS.Sets.Z));
        }

        partial record Roundf
        {
            // Flat between the half-integers and discontinuous at each of them -- round
            // jumps where the argument is exactly a half, not where it is an integer.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : Integer.Zero.Provided(!(Argument - Rational.Create(1, 2)).In(MathS.Sets.Z));
        }

        partial record Providedf
        {
            // Differentiated with its condition known to hold, since the derivative is claimed
            // only there: under `x > 0`, sgn(x^(1/3) + 2) is flat, where for x < 0 the cube
            // root is complex and nothing about it is.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => TreeAnalyzer.Assuming(Predicate, () => Expression.InnerDifferentiate(variable)).Provided(Predicate);
        }

        partial record Piecewise
        {
            // Each case with its own condition known to hold, as a `provided` is.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => New(Cases.Select(c => c.New(TreeAnalyzer.Assuming(c.Predicate, () => c.Expression.InnerDifferentiate(variable)), c.Predicate)));
        }

        partial record Application
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => (Expression, Arguments) switch
                {
                    (Variable v, var args) when args.All(arg => arg.Nodes.Contains(variable) is false)
                        => 0,
                    // An argument that *is* the variable has nothing for the chain rule to do:
                    // its own derivative is one, so the term below would be
                    // MathS.Derivative(this, variable) -- this very node, inside its own
                    // expansion. A raw Derivativef is returned instead, which the caller reads
                    // as "no progress" and leaves alone.
                    //
                    // This used to ask whether the *only* argument is the variable
                    // (`Tail: LEmpty<Entity>`), and with two arguments the expansion below ran:
                    // d/dx apply(f, x, y) came back as derivative(apply(f, x, y), x) * 1 + ...,
                    // which is not a Derivativef but contains one, so InnerSimplify's
                    // "not Derivativef" test read it as progress and simplified its way back
                    // here. `apply(f, x, y).Differentiate("x")` overflowed the stack.
                    (Variable v, var args) when args.Any(arg => arg == variable)
                        => MathS.Derivative(this, variable),

                    // d/dx_i f(g_1(x_1, x_2, ..., x_n), g_2(x_1, x_2, ..., x_n), ..., g_n(x_1, x_2, ..., x_n))
                    // becomes
                    // df/d[g_1] * d[g_1] / dx_i + df/d[g_2] * d[g_2] / dx_i + ...
                    // where
                    // f is a short name for f(g_1(...), ...)
                    // g_1 is a short name for g_1(x_1, ...)
                    // etc.
                    //
                    (Variable v, var args) when args.Any(arg => arg.Nodes.Contains(variable))
                        => args.Select(
                            arg =>
                                MathS.Derivative(this, arg) * arg.Differentiate(variable)
                            ).Aggregate((Entity)0, (a, b) => a + b),
                    _ => base.InnerDifferentiate(variable)
                };
        }
    }
}