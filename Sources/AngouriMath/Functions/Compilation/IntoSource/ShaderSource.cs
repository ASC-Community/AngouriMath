//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Globalization;
using System.Text;
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
    /// rather than printed as something close.
    /// </para>
    /// <para>
    /// Integer powers are multiplied out up to the fourth, and above that taken as
    /// <c>pow(abs(x), n)</c> with the sign restored for an odd <c>n</c>: <c>pow</c> of a
    /// negative base is undefined in both HLSL and GLSL, while <c>x^5</c> is defined for every
    /// real <c>x</c>.
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

        private sealed class Declined : Exception
        {
            internal Entity At { get; }
            internal Declined(Entity at, string reason) : base(reason) => At = at;
        }

        // Checked by hand: a regular expression would add System.Text.RegularExpressions to what
        // the kernel references, which KernelDependenciesTest holds fixed.
        private static bool IsIdentifier(string s)
            => s.Length > 0 && !char.IsDigit(s[0]) && s.All(c => c == '_' || c < 128 && char.IsLetterOrDigit(c));

        internal static GeneratedSource Emit(SourceDialect dialect, IReadOnlyList<KeyValuePair<string, Entity>> outputs, IReadOnlyDictionary<Variable, string>? names)
        {
            var emitter = new ShaderSource(dialect, names ?? new Dictionary<Variable, string>());
            try
            {
                foreach (var output in outputs)
                    emitter.Count(output.Value);
                var lines = new StringBuilder();
                foreach (var output in outputs)
                    lines.Append(output.Key).Append(" = ").Append(emitter.Value(output.Value)).Append(";\n");
                return new GeneratedSource(emitter.body.ToString() + lines, null, "");
            }
            catch (Declined d)
            {
                return new GeneratedSource(null, d.At, d.Message);
            }
        }

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
            Constant c => throw new Declined(c, $"the constant {c} has no literal here"),
            Variable v => Name(v),
            Integer i => Literal(EDecimal.FromEInteger(i.EInteger)),
            Rational r => Literal(r.ERational.ToEDecimal(EContext.Binary64)),
            Real { EDecimal: var d } when d.IsFinite => Literal(d),
            Entity.Boolean b => (bool)b ? "true" : "false",
            _ => throw new Declined(e, $"{e} is not a finite real number"),
        };

        private string Name(Variable v)
        {
            var name = names.TryGetValue(v, out var n) ? n : v.Name;
            // A swizzle or a member such as p.x is a legitimate target name.
            if (!name.Split('.').All(IsIdentifier))
                throw new Declined(v, $"{name} is not an identifier; map it to one through the names");
            if (name.Length > 2 && name.StartsWith("am") && name.Skip(2).All(char.IsDigit))
                throw new Declined(v, $"{name} is the name of a local this code declares");
            return name;
        }

        private static string Literal(EDecimal d) => Literal(d.ToDouble());

        // The shortest text that reads back as the same double, always with a point or an
        // exponent, so that neither language takes it for an integer.
        private static string Literal(double d)
        {
            var s = d.ToString("R", CultureInfo.InvariantCulture);
            return s.Contains(".") || s.Contains("E") ? s.Replace("E", "e") : s + ".0";
        }

        private string Print(Entity e) => e switch
        {
            Sumf(var a, var b) => $"({Value(a)} + {Value(b)})",
            Minusf(var a, var b) => $"({Value(a)} - {Value(b)})",
            Mulf(Integer { EInteger: var k }, var b) when k.Equals(EInteger.FromInt32(-1)) => $"(-{Value(b)})",
            Mulf(var a, var b) => $"({Value(a)} * {Value(b)})",
            Divf(var a, var b) => $"({Value(a)} / {Value(b)})",
            Powf(var b, var p) => Power(e, b, p),
            // Floored, as Modf is: the remainder takes the divisor's sign, which neither
            // HLSL's fmod nor C's % does.
            Modf(var a, var b) => Modulo(a, b),

            Sinf(var a) => $"sin({Value(a)})",
            Cosf(var a) => $"cos({Value(a)})",
            Tanf(var a) => $"tan({Value(a)})",
            Arcsinf(var a) => $"asin({Value(a)})",
            Arccosf(var a) => $"acos({Value(a)})",
            Arctanf(var a) => $"atan({Value(a)})",
            Logf(Constant c, var a) when c == MathS.e => $"log({Value(a)})",
            Logf(var b, var a) => $"(log({Value(a)}) / log({Value(b)}))",
            Absf(var a) => $"abs({Value(a)})",
            // HLSL's sign returns an int, which an arithmetic context converts back exactly.
            Signumf(var a) => ToReal($"sign({Value(a)})"),
            Floorf(var a) => $"floor({Value(a)})",
            Ceilf(var a) => $"ceil({Value(a)})",
            Maxf(var a, var b) => $"max({Value(a)}, {Value(b)})",
            Minf(var a, var b) => $"min({Value(a)}, {Value(b)})",

            Piecewise pw => Cases(pw),
            Greaterf(var a, var b) => $"({Value(a)} > {Value(b)})",
            GreaterOrEqualf(var a, var b) => $"({Value(a)} >= {Value(b)})",
            Lessf(var a, var b) => $"({Value(a)} < {Value(b)})",
            LessOrEqualf(var a, var b) => $"({Value(a)} <= {Value(b)})",
            Equalsf(var a, var b) => $"({Value(a)} == {Value(b)})",
            Andf(var a, var b) => $"({Value(a)} && {Value(b)})",
            Orf(var a, var b) => $"({Value(a)} || {Value(b)})",
            Notf(var a) => $"(!{Value(a)})",

            _ => throw new Declined(e, $"{e.GetType().Name} has no faithful form in the real-valued reading"),
        };

        private string Power(Entity at, Entity b, Entity p)
        {
            if (b is Constant c && c == MathS.e)
                return $"exp({Value(p)})";
            if (p is not Rational q)
                return $"pow({Value(b)}, {Value(p)})";
            if (q is Integer { EInteger: var k } && k.CanFitInInt32())
            {
                var n = k.ToInt32Checked();
                switch (n)
                {
                    case 0: return Literal(1.0);
                    case 1: return Value(b);
                    case -1: return $"({Literal(1.0)} / {Value(b)})";
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
                return n > 0 ? magnitude : $"({Literal(1.0)} / {magnitude})";
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

        // A conditional per case, the last one taken as the fallback only where its condition is
        // literally true: a piecewise with no case for some input has no value there, and a
        // shader cannot say so.
        private string Cases(Piecewise pw)
        {
            var cases = pw.Cases.ToList();
            if (cases.Count == 0 || cases[cases.Count - 1].Predicate != Entity.Boolean.True)
                throw new Declined(pw, "a piecewise without a final case that is always true has inputs with no value");
            var text = Value(cases[cases.Count - 1].Expression);
            for (var k = cases.Count - 2; k >= 0; k--)
                text = $"({Value(cases[k].Predicate)} ? {Value(cases[k].Expression)} : {text})";
            return text;
        }
    }
}
