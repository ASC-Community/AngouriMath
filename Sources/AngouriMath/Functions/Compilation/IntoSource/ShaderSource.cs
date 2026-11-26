//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Globalization;
using System.Text;
using AngouriMath.Core.Compilation;
using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions.Compilation.IntoSource
{
    /// <summary>The dialects <see cref="ShaderSource"/> prints.</summary>
    internal enum SourceDialect
    {
        Hlsl,
        Glsl,
        // C# over doubles, against helpers named as HLSL's intrinsics are, so that the exact
        // text the shader dialects print can be compiled and checked against the library.
        CSharpCheck,
    }

    /// <summary>
    /// Prints expressions as straight-line shader code: one statement per output, with every
    /// subexpression that occurs more than once computed once into a local.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The code computes what <c>Compile&lt;double, …&gt;</c> computes, the real-valued
    /// reading: where the library's value is not real, the shader's is <c>NaN</c> or
    /// unspecified, as the shading language's own <c>pow</c> and <c>sqrt</c> leave it. A
    /// node with no faithful form in that reading — a complex constant, a set, a rounding
    /// rule whose tie-breaking the languages do not agree on — is declined, naming the node,
    /// rather than printed as something close. What <c>Compile</c> refuses is declined with
    /// the message <c>Compile</c> gives.
    /// </para>
    /// <para>
    /// Every expression is checked before anything is printed, so that printing has nothing
    /// left to decline: the check is <see cref="Refusal"/>, and <see cref="Print"/> prints
    /// exactly the nodes it accepts.
    /// </para>
    /// <para>
    /// Integer powers are multiplied out up to the fourth, and above that taken as
    /// <c>pow(abs(x), n)</c> with the sign restored for an odd <c>n</c>: <c>pow</c> of a
    /// negative base is undefined in both HLSL and GLSL, while <c>x^5</c> is defined for every
    /// real <c>x</c>. A rational power with an odd denominator below ten is the real root
    /// of a negative base, as <c>Compile</c> takes it, and is printed the same way.
    /// </para>
    /// </remarks>
    internal sealed class ShaderSource
    {
        private readonly SourceDialect dialect;
        private readonly IReadOnlyDictionary<Variable, string> names;
        private readonly Dictionary<Entity, int> uses = new();
        private readonly Dictionary<Entity, string> locals = new();
        private readonly HashSet<Entity> sinCosDone = new();
        private readonly StringBuilder body = new();
        private int next;

        private ShaderSource(SourceDialect dialect, IReadOnlyDictionary<Variable, string> names)
            => (this.dialect, this.names) = (dialect, names);

        // Checked by hand: a regular expression would add System.Text.RegularExpressions to what
        // the kernel references, which KernelDependenciesTest holds fixed.
        private static bool IsIdentifier(string s)
            => s.Length > 0 && !char.IsDigit(s[0]) && s.All(c => c == '_' || c < 128 && char.IsLetterOrDigit(c));

        internal static GeneratedSource Emit(SourceDialect dialect, IReadOnlyList<KeyValuePair<string, Entity>> outputs, IReadOnlyDictionary<Variable, string>? names)
        {
            var emitter = new ShaderSource(dialect, names ?? new Dictionary<Variable, string>());
            var prepared = new List<KeyValuePair<string, Entity>>(outputs.Count);
            foreach (var output in outputs)
            {
                var expression = Prepared(output.Value);
                if (emitter.Refusal(expression) is { } refusal)
                    return new GeneratedSource(null, refusal.At, refusal.Reason);
                prepared.Add(new(output.Key, expression));
            }
            foreach (var output in prepared)
                emitter.Count(output.Value);
            var lines = new StringBuilder();
            foreach (var output in prepared)
                lines.Append(output.Key).Append(" = ").Append(emitter.Value(output.Value)).Append(";\n");
            return new GeneratedSource(emitter.body.ToString() + lines, null, "");
        }

        // The rewrites Compile makes before compiling, so that the shader computes what it
        // computes. A membership in an interval, a finite set or a union of them is the comparisons
        // it stands for. A matrix operation is carried out wherever its names are numbers, which every
        // variable of a shader is, as Compile carries it out: dot(x [1, 2], [3, 4]) is 11 x. A product of
        // matrices written out that is left then is reduced to the number it gives, as Compile reduces it
        // (#425), since a shader output is a number. A quotient of one exponential
        // plus a constant by the same exponential plus another, which is how tanh is spelled, is
        // taken as 1 + (c1 - c2) / (b^v + c2), which does not divide an overflow by an overflow:
        // in single precision e^(2x) overflows from x = 45 on.
        // https://github.com/asc-community/AngouriMath/issues/2254
        private static Entity Prepared(Entity expression)
        {
            // https://github.com/asc-community/AngouriMath/issues/2561
            if (expression.Nodes.Any(node => node is Matrix))
            {
                expression = MatrixOperationsOverNumbers.Rewritten(expression, static _ => true);
                // With an operation still left, simplification would read it as a number beside the written
                // matrices (#2557); it is refused as it stands instead.
                if (expression.Nodes.Any(node => node is Matrix) && !expression.Nodes.Any(MatrixOperationsOverNumbers.IsMatrixOperation))
                    expression = expression.InnerSimplified;
            }
            return ExponentialQuotients.Rewritten(CompiledMembership.Rewritten(expression));
        }

        private (Entity At, string Reason)? Refusal(Entity expression)
        {
            if (Binders.Refusal(expression) is { } binder)
                return binder;
            if (StatementPositions.Refusal(expression) is { } statement)
                return statement;
            foreach (var node in expression.Nodes)
                if (Refusal(node, NodeRefusal(node)) is { } refusal)
                    return refusal;
            return null;

            static (Entity, string)? Refusal(Entity node, string? reason) => reason is null ? null : (node, reason);
        }

        // Why this one node cannot be printed, or null where Print prints it. Its children are
        // asked separately.
        private string? NodeRefusal(Entity node) => node switch
        {
            Constant c when c == MathS.pi || c == MathS.e => null,
            Constant c => $"the constant {c} has no literal here",
            Variable v => NameRefusal(v),
            Integer or Rational => null,
            Real { EDecimal: var d } => d.IsFinite ? null : $"{node} is not a finite real number",
            Number => $"{node} is not a real number, and the shader computes the real-valued reading",
            Entity.Boolean => null,

            Sumf or Minusf or Mulf or Divf or Powf or Modf => null,
            Sinf or Cosf or Tanf or Cotanf or Secantf or Cosecantf => null,
            Arcsinf or Arccosf or Arctanf or Arccotanf or Arcsecantf or Arccosecantf => null,
            Logf or Absf or Signumf or Floorf or Ceilf or Maxf or Minf => null,
            Conjugatef or Ref or Imf or Argf => null,
            Greaterf or GreaterOrEqualf or Lessf or LessOrEqualf or Equalsf => null,
            Andf or Orf or Xorf or Impliesf or Notf or Iversonf => null,
            Piecewise pw => pw.Cases.LastOrDefault() is { Predicate: var last } && last == Entity.Boolean.True
                ? null
                : "a piecewise without a final case that is always true has inputs with no value, and a shader cannot say there is none",

            Providedf => "a shader cannot say there is no value: where the condition cannot fail, give the expression without it",
            Roundf => "round takes a tie to the even neighbour, and HLSL's and GLSL's round leave ties to the implementation",
            Matrix => $"`{node.Stringize()}` is a matrix, and each output of a shader here is a number: assign the entries one by one",
            DotProductf or CrossProductf => $"`{node.Stringize()}` is taken of vectors, and every variable of the shader is a number",
            Transposef or Determinantf or Tracef or MatrixEntryf => $"`{node.Stringize()}` is taken of a matrix, and every variable of the shader is a number",
            Set => $"`{node.Stringize()}` is a set, which has no value in a shader",
            Set.Inf => $"`{node.Stringize()}` is a membership in a set of numbers, which has no straight-line test in a shader",
            Gcdf or Lcmf => $"`{node.Stringize()}` needs Euclid's algorithm, a loop, and the shader is straight-line code",
            _ => $"`{node.Stringize()}` has no intrinsic in HLSL or GLSL and no straight-line form",
        };

        private string? NameRefusal(Variable v)
        {
            var name = NameOf(v);
            // A swizzle or a member such as p.x is a legitimate target name.
            if (!name.Split('.').All(IsIdentifier))
                return $"{name} is not an identifier; map it to one through the names";
            if (name.Length > 2 && name.StartsWith("am") && name.Skip(2).All(char.IsDigit))
                return $"{name} is the name of a local this code declares";
            return null;
        }

        private string NameOf(Variable v) => names.TryGetValue(v, out var n) ? n : v.Name;

        private string Real => dialect == SourceDialect.CSharpCheck ? "double" : "float";

        private static bool IsLeaf(Entity e) => e is Variable or Number or Entity.Boolean;

        private void Count(Entity e)
        {
            if (IsLeaf(e))
                return;
            uses[e] = uses.TryGetValue(e, out var n) ? n + 1 : 1;
            // A repeat's children were counted on its first visit, and are computed once inside
            // its local.
            if (uses[e] > 1)
                return;
            foreach (var c in e.DirectChildren)
                Count(c);
        }

        private string Value(Entity e)
        {
            if (IsLeaf(e))
                return Leaf(e);
            if (locals.TryGetValue(e, out var name))
                return name;
            if (dialect != SourceDialect.Glsl && e is Sinf or Cosf && TrySinCos(e, out var sc))
                return sc;
            var text = Print(e);
            return uses.TryGetValue(e, out var n) && n > 1 ? Hoist(e, text, e is Statement ? "bool" : Real) : text;
        }

        private string Hoist(Entity e, string text, string type)
        {
            var name = $"am{next++}";
            body.Append(dialect == SourceDialect.CSharpCheck ? "var" : type).Append(' ').Append(name).Append(" = ").Append(text).Append(";\n");
            locals[e] = name;
            return name;
        }

        // A value needed twice in one printed form, as x in x * x, goes into a local even when
        // it occurs once in the expression, so that it is not computed twice.
        private string Once(Entity e)
        {
            var text = Value(e);
            return IsLeaf(e) || locals.ContainsKey(e) ? text : Hoist(e, text, Real);
        }

        // sin(a) and cos(a) both wanted: HLSL computes the pair in one call.
        private bool TrySinCos(Entity e, out string result)
        {
            var arg = e.DirectChildren[0];
            result = "";
            if (!uses.ContainsKey(new Sinf(arg)) || !uses.ContainsKey(new Cosf(arg)) || !sinCosDone.Add(arg))
                return false;
            var a = Value(arg);
            var (s, c) = ($"am{next++}", $"am{next++}");
            body.Append(dialect == SourceDialect.CSharpCheck
                ? $"sincos({a}, out var {s}, out var {c});\n"
                : $"float {s}, {c};\nsincos({a}, {s}, {c});\n");
            locals[new Sinf(arg)] = s;
            locals[new Cosf(arg)] = c;
            result = locals[e];
            return true;
        }

        private string Leaf(Entity e) => e switch
        {
            Constant c when c == MathS.pi => Literal(Math.PI),
            Constant c when c == MathS.e => Literal(Math.E),
            Variable v => NameOf(v),
            Integer i => Literal(EDecimal.FromEInteger(i.EInteger)),
            Rational r => Literal(r.ERational.ToEDecimal(EContext.Binary64)),
            Real { EDecimal: var d } => Literal(d),
            Entity.Boolean b => (bool)b ? "true" : "false",
            _ => throw new AngouriBugException($"{e} passed the shader's check and has no literal"),
        };

        private static string Literal(EDecimal d) => Literal(d.ToDouble());

        // The shortest text that reads back as the same double, always with a point or an
        // exponent, so that neither language takes it for an integer.
        private static string Literal(double d)
        {
            var s = d.ToString("R", CultureInfo.InvariantCulture);
            return s.Contains(".") || s.Contains("E") ? s.Replace("E", "e") : s + ".0";
        }

        private string One => Literal(1.0);

        private string Print(Entity e) => e switch
        {
            Sumf(var a, var b) => $"({Value(a)} + {Value(b)})",
            Minusf(var a, var b) => $"({Value(a)} - {Value(b)})",
            Mulf(Integer { EInteger: var k }, var b) when k.Equals(EInteger.FromInt32(-1)) => $"(-{Value(b)})",
            Mulf(var a, var b) => $"({Value(a)} * {Value(b)})",
            Divf(var a, var b) => $"({Value(a)} / {Value(b)})",
            Powf(var b, var p) => Power(b, p),
            // Floored, as Modf is: the remainder takes the divisor's sign, which neither
            // HLSL's fmod nor C's % does.
            Modf(var a, var b) => Modulo(a, b),

            Sinf(var a) => $"sin({Value(a)})",
            Cosf(var a) => $"cos({Value(a)})",
            Tanf(var a) => $"tan({Value(a)})",
            // As Compile takes them: the reciprocal, and the inverse of the reciprocal, which
            // gives arccotan its range (-pi/2, pi/2] with pi/2 at 0, where 1 / 0 is +inf.
            Cotanf(var a) => $"({One} / tan({Value(a)}))",
            Secantf(var a) => $"({One} / cos({Value(a)}))",
            Cosecantf(var a) => $"({One} / sin({Value(a)}))",
            Arcsinf(var a) => $"asin({Value(a)})",
            Arccosf(var a) => $"acos({Value(a)})",
            Arctanf(var a) => $"atan({Value(a)})",
            Arccotanf(var a) => $"atan({One} / {Value(a)})",
            Arcsecantf(var a) => $"acos({One} / {Value(a)})",
            Arccosecantf(var a) => $"asin({One} / {Value(a)})",
            Logf(Constant c, var a) when c == MathS.e => $"log({Value(a)})",
            Logf(var b, var a) => $"(log({Value(a)}) / log({Value(b)}))",
            Absf(var a) => $"abs({Value(a)})",
            // HLSL's sign returns an int, which an arithmetic context converts back exactly.
            Signumf(var a) => ToReal($"sign({Value(a)})"),
            Floorf(var a) => $"floor({Value(a)})",
            Ceilf(var a) => $"ceil({Value(a)})",
            Maxf(var a, var b) => $"max({Value(a)}, {Value(b)})",
            Minf(var a, var b) => $"min({Value(a)}, {Value(b)})",

            // Of a real number, as Compile over double has them: the conjugate and the real part
            // are the number, the imaginary part is 0, and the argument is pi to the left of 0
            // and 0 to the right.
            Conjugatef(var a) => Value(a),
            Ref(var a) => Value(a),
            Imf => Literal(0.0),
            Argf(var a) => $"(({Value(a)} < {Literal(0.0)}) ? {Literal(Math.PI)} : {Literal(0.0)})",

            Piecewise pw => Cases(pw),
            Greaterf(var a, var b) => $"({Value(a)} > {Value(b)})",
            GreaterOrEqualf(var a, var b) => $"({Value(a)} >= {Value(b)})",
            Lessf(var a, var b) => $"({Value(a)} < {Value(b)})",
            LessOrEqualf(var a, var b) => $"({Value(a)} <= {Value(b)})",
            Equalsf(var a, var b) => $"({Value(a)} == {Value(b)})",
            Andf(var a, var b) => $"({Value(a)} && {Value(b)})",
            Orf(var a, var b) => $"({Value(a)} || {Value(b)})",
            Xorf(var a, var b) => $"({Value(a)} != {Value(b)})",
            Impliesf(var a, var b) => $"((!{Value(a)}) || {Value(b)})",
            Notf(var a) => $"(!{Value(a)})",
            Iversonf(var a) => $"({Value(a)} ? {One} : {Literal(0.0)})",

            _ => throw new AngouriBugException($"{e.GetType().Name} passed the shader's check and has no printed form"),
        };

        private string Power(Entity b, Entity p)
        {
            if (b is Constant c && c == MathS.e)
                return $"exp({Value(p)})";
            // An odd denominator below ten: the real root of a negative base, with the sign of
            // the base where the numerator is odd, as Compile and evaluation take it.
            if (RealProducts.RealRootExponent(p) is { } ratio)
            {
                var x = Once(b);
                var magnitude = $"pow(abs({x}), {Value(p)})";
                return ratio.Numerator.IsEven ? magnitude : $"({ToReal($"sign({x})")} * {magnitude})";
            }
            if (p is not Rational q)
                return $"pow({Value(b)}, {Value(p)})";
            if (q is Integer { EInteger: var k } && k.CanFitInInt32())
            {
                var n = k.ToInt32Checked();
                switch (n)
                {
                    case 0: return One;
                    case 1: return Value(b);
                    case -1: return $"({One} / {Value(b)})";
                }
                var x = Once(b);
                var magnitude = Math.Abs(n) switch
                {
                    2 => $"({x} * {x})",
                    3 => $"({x} * {x} * {x})",
                    4 => $"(({x} * {x}) * ({x} * {x}))",
                    var m when m % 2 == 0 => $"pow(abs({x}), {Literal((double)m)})",
                    var m => $"({ToReal($"sign({x})")} * pow(abs({x}), {Literal((double)m)}))",
                };
                return n > 0 ? magnitude : $"({One} / {magnitude})";
            }
            if (q.ERational.CompareTo(ERational.Create(1, 2)) == 0)
                return $"sqrt({Value(b)})";
            if (q.ERational.CompareTo(ERational.Create(-1, 2)) == 0)
                return dialect == SourceDialect.Glsl ? $"inversesqrt({Value(b)})" : $"rsqrt({Value(b)})";
            return $"pow({Value(b)}, {Literal(q.ERational.ToEDecimal(EContext.Binary64))})";
        }

        private string ToReal(string integer)
            => dialect == SourceDialect.CSharpCheck ? $"((double){integer})" : $"float({integer})";

        private string Modulo(Entity a, Entity b)
        {
            var (x, y) = (Once(a), Once(b));
            return $"({x} - {y} * floor({x} / {y}))";
        }

        // A conditional per case, the last one taken as the fallback: the check accepted only a
        // piecewise whose last condition is literally true.
        private string Cases(Piecewise pw)
        {
            var cases = pw.Cases.ToList();
            var text = Value(cases[cases.Count - 1].Expression);
            for (var k = cases.Count - 2; k >= 0; k--)
                text = $"({Value(cases[k].Predicate)} ? {Value(cases[k].Expression)} : {text})";
            return text;
        }
    }
}
