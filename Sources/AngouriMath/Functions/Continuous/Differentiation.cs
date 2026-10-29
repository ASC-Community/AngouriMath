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
            => DerivativeOver(variable).InnerSimplified;

        /// <summary>
        /// The derivative over <paramref name="variable"/>, unsimplified: zero for a node that is
        /// a constant in it, and the node's own rule, <see cref="InnerDifferentiate(Variable)"/>,
        /// for any other. Every rule asks this of its parts rather than asking their rules.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A rule writes the chain rule, and its outer factor can have no value where the node
        /// has one: <c>sqrt(c)</c> gave <c>1/2 c^(-1/2) * 0</c>, and <c>arcsin(c)</c> a factor
        /// with no value at <c>c = 1</c>. Zero times a factor keeps the condition the factor is
        /// defined under, so <c>sqrt(c) x</c> differentiated to <c>sqrt(c) provided not c = 0</c>,
        /// with no value at <c>c = 0</c>, where the derivative is <c>sqrt(0) = 0</c>; an
        /// antiderivative carrying <c>sqrt(D)</c> had no value anywhere at a discriminant
        /// <c>D = 0</c>. Nothing varies with the variable here, so the answer is zero with no
        /// condition, as it already was for a node with no rule of its own, the modulus and the
        /// sign. That holds where the constant has no value too, <c>ln(c - c)</c>: a derivative is
        /// claimed only where the expression has a value.
        /// </para>
        /// <para>
        /// Not where a zero loses what the node says besides a value. A set or a matrix is
        /// differentiated elementwise, <c>{ 1, 2 }</c> to <c>{ 0 }</c>. A <c>provided</c> and a
        /// piecewise keep their own conditions, which their rules write onto the derivative. A
        /// written NaN stays NaN, as <see cref="Number"/>'s rule keeps it. Their rules reach the
        /// constants inside them through this method.
        /// </para>
        /// </remarks>
        internal Entity DerivativeOver(Variable variable)
            => IsConstantIn(variable) && !Nodes.Any(SaysMoreThanAValue) ? Integer.Zero : InnerDifferentiate(variable);

        /// <summary>Whether a zero in place of a derivative in which this node occurs would lose
        /// something: see <see cref="DerivativeOver(Variable)"/>.</summary>
        private static bool SaysMoreThanAValue(Entity node)
            => node is Set or Matrix or Providedf or Piecewise || node.IsNaN;

        /// <summary>
        /// This node's own rule of differentiation. Parts are differentiated by
        /// <see cref="DerivativeOver(Variable)"/>, which answers zero for a constant before a rule
        /// is asked.
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
            protected override Entity InnerDifferentiate(Variable variable) => Elementwise(e => e.DerivativeOver(variable));
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
                    ent = ent.DerivativeOver(x);
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
                    // DifferentiateOnce, not DerivativeOver: the raw chain rule leaves
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
                Augend.DerivativeOver(variable) + Addend.DerivativeOver(variable);
        }

        partial record Minusf
        {
            // (a - b)' = a' - b'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Minuend.DerivativeOver(variable) - Subtrahend.DerivativeOver(variable);
        }

        partial record Mulf
        {
            // (a * b)' = a' * b + b' * a
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Multiplier.DerivativeOver(variable) * Multiplicand + Multiplicand.DerivativeOver(variable) * Multiplier;
        }

        partial record Divf
        {
            // (a / b)' = (a' * b - b' * a) / b^2
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                (Dividend.DerivativeOver(variable) * Divisor - Divisor.DerivativeOver(variable) * Dividend) / Divisor.Pow(2);
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
                    : Dividend.DerivativeOver(variable);
        }

        partial record Powf
        {
            // (a ^ b)' = e ^ (ln(a) * b) * (a' * b / a + ln(a) * b')
            // (a ^ const)' = const * a ^ (const - 1) * a'
            // (const ^ b)' = e^b * b'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Exponent == Integer.One
                ? Base.DerivativeOver(variable).Provided(Base.DomainCondition) // don't create x^0 which is undefined for x=0
                : Exponent.Evaled is Number exp
                ? exp * Base.Pow(exp - 1) * Base.DerivativeOver(variable)
                : Base is Number
                ? Base.Pow(Exponent) * MathS.Ln(Base) * Exponent.DerivativeOver(variable)
                : Base.Pow(Exponent) * (Base.DerivativeOver(variable) * Exponent / Base + MathS.Ln(Base) * Exponent.DerivativeOver(variable));
        }

        partial record Sinf
        {
            // sin(a)' = cos(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Argument.Cos() * Argument.DerivativeOver(variable);
        }

        partial record Erff
        {
            // erf(a)' = 2/sqrt(pi) e^(-a^2) a', from its definition as an integral.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, -MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Erfcf
        {
            // erfc(a)' = -2/sqrt(pi) e^(-a^2) a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, -MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Erfif
        {
            // erfi(a)' = 2/sqrt(pi) e^(a^2) a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                2 / MathS.Sqrt(MathS.pi) * MathS.Pow(MathS.e, MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Eif
        {
            // Ei(a)' = e^a/a a', the integrand of its definition.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Pow(MathS.e, Argument) / Argument * Argument.DerivativeOver(variable);
        }

        partial record Lif
        {
            // li(a)' = a'/ln(a)
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Argument.DerivativeOver(variable) / MathS.Ln(Argument);
        }

        partial record LambertWf
        {
            // W'(u) = e^(-W(u)) / (1 + W(u)) u', the same as W(u) / (u (1 + W(u))) u' and 1 at u = 0 on the
            // principal branch, where that one is 0/0. A branch that varies has no derivative in it.
            // https://github.com/asc-community/AngouriMath/issues/2182
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Branch.ContainsNode(variable)
                ? base.InnerDifferentiate(variable)
                : MathS.Pow(MathS.e, -this) / (1 + this) * Argument.DerivativeOver(variable);
        }

        partial record Polygammaf
        {
            // psi^(n)(a)' = psi^(n + 1)(a) a'. The order is whole, so an order that varies has no derivative
            // in it, and this declines. https://github.com/asc-community/AngouriMath/issues/171
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Order.ContainsNode(variable)
                ? base.InnerDifferentiate(variable)
                : MathS.Polygamma(Order + 1, Argument) * Argument.DerivativeOver(variable);
        }

        partial record IncompleteGammaf
        {
            // Gamma(a, u)' = -u^(a - 1) e^(-u) u', the integrand at its lower limit. A parameter that varies wants
            // a Meijer G function, and this declines. https://github.com/asc-community/AngouriMath/issues/2075
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                Parameter.ContainsNode(variable)
                ? base.InnerDifferentiate(variable)
                : -MathS.Pow(LowerLimit, Parameter - 1) * MathS.Pow(MathS.e, -LowerLimit) * LowerLimit.DerivativeOver(variable);
        }

        partial record Sif
        {
            // Si(a)' = sin(a)/a a', the integrand of its definition, and so for the other three.
            // https://github.com/asc-community/AngouriMath/issues/1501
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Sin(Argument) / Argument * Argument.DerivativeOver(variable);
        }

        partial record Cif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Cos(Argument) / Argument * Argument.DerivativeOver(variable);
        }

        partial record Shif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Hyperbolic.Sinh(Argument) / Argument * Argument.DerivativeOver(variable);
        }

        partial record Chif
        {
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                MathS.Hyperbolic.Cosh(Argument) / Argument * Argument.DerivativeOver(variable);
        }

        partial record Cosf
        {
            // cos(a)' = -sin(a) * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -Argument.Sin() * Argument.DerivativeOver(variable);
        }

        partial record Tanf
        {
            // tan(a)' = 1 / cos(a) ^ 2 * a'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / Argument.Cos().Pow(2) * Argument.DerivativeOver(variable);
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
                -1 / Argument.Sin().Pow(2) * Argument.DerivativeOver(variable);
        }

        partial record Logf
        {
            // log_b(a)' = (ln(a) / ln(b))' = (ln(a)' * ln(b) - ln(a) * ln(b)') / ln(b)^2 = (a' / a * ln(b) - ln(a) * b' / b) / ln(b)^2
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                (Antilogarithm.DerivativeOver(variable) / Antilogarithm * MathS.Ln(Base)
                - MathS.Ln(Antilogarithm) * Base.DerivativeOver(variable) / Base)
                / MathS.Ln(Base).Pow(2);
        }

        partial record Arcsinf
        {
            // arcsin(x)' = 1 / sqrt(1 - x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / MathS.Sqrt(1 - MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Arccosf
        {
            // arccos(x)' = -1 / sqrt(1 - x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -1 / MathS.Sqrt(1 - MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Arctanf
        {
            // arctan(x)' = 1 / (1 + x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                1 / (1 + MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Arccotanf
        {
            // arccotan(x)' = -1 / (1 + x^2) * x'
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable) =>
                -1 / (1 + MathS.Sqr(Argument)) * Argument.DerivativeOver(variable);
        }

        partial record Factorialf
        {
            /// <summary>
            /// <c>(a!)' = a! psi(a + 1) a'</c>: <c>x!</c> is <c>Gamma(x + 1)</c>, and the digamma function is
            /// <c>Gamma'/Gamma</c>.
            /// </summary>
            /// <remarks>
            /// It declined while the library had no digamma function, and before that answered
            /// <see cref="Number.Real.NaN"/>, which says the derivative does not exist where it does
            /// (https://github.com/asc-community/AngouriMath/issues/958): <c>d/dx x!</c> at 3 is
            /// <c>Gamma'(4) = 6 psi(4)</c>, about <c>7.5367</c>.
            /// https://github.com/asc-community/AngouriMath/issues/171
            /// </remarks>
            protected override Entity InnerDifferentiate(Variable variable)
                => this * MathS.Digamma(Argument + 1) * Argument.DerivativeOver(variable);
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
                  ? to.DerivativeOver(variable) * Expression.Substitute(variable, to) -
                    from.DerivativeOver(variable) * Expression.Substitute(variable, from)
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
                : New(Expression.DerivativeOver(variable), Var, Over);
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

        partial record Ref
        {
            // Along the real line of the variable, as the conjugate's: re is real-linear, so the
            // derivative of re(f) is re(f').
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : new Ref(Argument.DerivativeOver(variable));
        }

        partial record Imf
        {
            // Along the real line of the variable: the derivative of im(f) is im(f').
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : new Imf(Argument.DerivativeOver(variable));
        }

        partial record Conjugatef
        {
            // Along the real line of the variable, as the modulus's derivative is read below: the
            // difference quotient's step is real, so it commutes with the conjugate, and the
            // derivative of conjugate(f) is conjugate(f'). There is no complex derivative: the
            // conjugate is nowhere holomorphic.
            // https://github.com/asc-community/AngouriMath/issues/2145
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero : new Conjugatef(Argument.DerivativeOver(variable));
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
            //
            // An argument real only where the bases of its roots are positive is flat there and
            // left unevaluated elsewhere, as a piecewise: sgn(sqrt(1 + x) + 1/2) is 0 where
            // 1 + x > 0. An antiderivative carrying such a sign could not be checked anywhere.
            // https://github.com/asc-community/AngouriMath/issues/1186
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.IsRealValued(Argument, variable)
                    ? Integer.Zero.Provided(!Argument.EqualTo(Integer.Zero))
                    : TreeAnalyzer.WhereRealValued(Argument, variable) is { } real
                    ? MathS.Piecewise(
                        (Integer.Zero.Provided(!Argument.EqualTo(Integer.Zero)), real),
                        (MathS.Derivative(this, variable), Boolean.True))
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
            // argument is then not shown to be real the derivative is left unevaluated, and
            // where it is real only while the bases of its roots are positive, it is the
            // real-line rule there and unevaluated elsewhere, as the sign's is.
            // https://github.com/asc-community/AngouriMath/issues/1186
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => IsConstantIn(variable) ? Integer.Zero
                    : TreeAnalyzer.TryTakeNumericModulusOut(Argument, out var withoutThePhase)
                    ? withoutThePhase.DerivativeOver(variable)
                    : TreeAnalyzer.IsRealValued(Argument, variable)
                    ? MathS.Signum(Argument).Provided(!Argument.EqualTo(Integer.Zero)) * Argument.DerivativeOver(variable)
                    : TreeAnalyzer.WhereRealValued(Argument, variable) is { } real
                    ? MathS.Piecewise(
                        (MathS.Signum(Argument).Provided(!Argument.EqualTo(Integer.Zero))
                            * TreeAnalyzer.Assuming(real, () => Argument.DerivativeOver(variable)), real),
                        (MathS.Derivative(this, variable), Boolean.True))
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
                    ? ((Left.DerivativeOver(variable) + Right.DerivativeOver(variable)
                        + MathS.Signum(Left - Right) * (Left.DerivativeOver(variable) - Right.DerivativeOver(variable))) / 2)
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
                    ? ((Left.DerivativeOver(variable) + Right.DerivativeOver(variable)
                        - MathS.Signum(Left - Right) * (Left.DerivativeOver(variable) - Right.DerivativeOver(variable))) / 2)
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
                => TreeAnalyzer.Assuming(Predicate, () => Expression.DerivativeOver(variable)).Provided(Predicate);
        }

        partial record Piecewise
        {
            // Each case with its own condition known to hold, as a `provided` is.
            /// <inheritdoc/>
            protected override Entity InnerDifferentiate(Variable variable)
                => New(Cases.Select(c => c.New(TreeAnalyzer.Assuming(c.Predicate, () => c.Expression.DerivativeOver(variable)), c.Predicate)));
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