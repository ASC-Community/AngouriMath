//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core;
using AngouriMath.Extensions;
using AngouriMath.Functions.Compilation.IntoSource;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Convenience
{
    /// <summary>
    /// <see cref="MathS.Shaders"/>. The printed text itself is what is checked: the same
    /// printer writes C# against helpers named as HLSL's intrinsics, that text is compiled,
    /// and its values are compared with <c>Compile&lt;double, double, double&gt;</c> — so a
    /// wrong operand order, a lost sign or a bad shared local shows up as a number.
    /// </summary>
    [Trait("Area", "Convenience")]
    public sealed class ShadersTest
    {
        private const string Helpers = """
            static double sqrt(double x) => Math.Sqrt(x);
            static double rsqrt(double x) => 1.0 / Math.Sqrt(x);
            static double sin(double x) => Math.Sin(x);
            static double cos(double x) => Math.Cos(x);
            static double tan(double x) => Math.Tan(x);
            static double asin(double x) => Math.Asin(x);
            static double acos(double x) => Math.Acos(x);
            static double atan(double x) => Math.Atan(x);
            static double exp(double x) => Math.Exp(x);
            static double log(double x) => Math.Log(x);
            static double abs(double x) => Math.Abs(x);
            static int sign(double x) => Math.Sign(x);
            static double floor(double x) => Math.Floor(x);
            static double ceil(double x) => Math.Ceiling(x);
            static double max(double a, double b) => Math.Max(a, b);
            static double min(double a, double b) => Math.Min(a, b);
            static double pow(double a, double b) => Math.Pow(a, b);
            static void sincos(double a, out double s, out double c) { s = Math.Sin(a); c = Math.Cos(a); }
            """;

        private static Func<double, double, double> Printed(Entity expr)
        {
            var source = ShaderSource.Emit(SourceDialect.CSharpCheck,
                new[] { new KeyValuePair<string, Entity>("r", expr) }, null);
            Assert.True(source.Succeeded, source.ToString());
            var code = $"{Helpers}\nFunc<double, double, double> F = (x, y) => {{ double r;\n{source.Code} return r; }};\nF";
            return CSharpScript.EvaluateAsync<Func<double, double, double>>(code, ScriptOptions.Default.WithImports("System"))
                .GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData("x + y")]
        [InlineData("x - y")]
        [InlineData("y - x")]
        [InlineData("x / y")]
        [InlineData("-x * y")]
        [InlineData("(x + y)^2 + sin(x + y)")]
        [InlineData("sin(x) cos(x) + sin(x)")]
        [InlineData("x^3")]
        [InlineData("x^4")]
        [InlineData("x^5")]
        [InlineData("x^6")]
        [InlineData("x^(-3)")]
        [InlineData("x^(1/2)")]
        [InlineData("x^(-1/2)")]
        [InlineData("x^(3/2)")]
        [InlineData("x^y")]
        [InlineData("e^(x y)")]
        [InlineData("ln(x)")]
        [InlineData("log(2, x)")]
        [InlineData("x mod y")]
        [InlineData("abs(x) + sign(y)")]
        [InlineData("floor(x) + ceil(y)")]
        [InlineData("max(x, y) - min(x, y)")]
        [InlineData("arcsin(x / 4) + arccos(y / 4) + arctan(x)")]
        [InlineData("tan(x)")]
        [InlineData("pi x + e")]
        [InlineData("x / 3 + 0.25 y")]
        [InlineData("derivative(sqrt(x^2 + y^2), x)")]
        [InlineData("x^(1/3) + y^(2/3)")]
        [InlineData("x^(-1/3) - y^(5/3)")]
        [InlineData("x^(1/(1 + 2))")]
        [InlineData("cotan(x) + sec(y) + cosec(x y)")]
        [InlineData("arccotan(x) + arcsec(y + 4) + arccosec(x - 4)")]
        [InlineData("conjugate(x) + re(y) + im(x y) + arg(x)")]
        [InlineData("iverson(x > y) + iverson((x > 0) xor (y > 0)) + iverson((x > 0) implies (y > 1))")]
        [InlineData("iverson(x in [-1; 1] or x in {2, 3})")]
        [InlineData("tanh(x y)")]
        public void ThePrintedCodeComputesWhatCompileComputes(string input)
        {
            var expr = input.ToEntity();
            // Derivatives and the like are evaluated before printing, as a caller would.
            if (expr is CalculusOperator)
                expr = expr.Simplify();
            var printed = Printed(expr);
            var compiled = expr.Compile<double, double, double>("x", "y");
            var rng = new Random(7);
            var compared = 0;
            for (var k = 0; k < 200; k++)
            {
                var (x, y) = (rng.NextDouble() * 6 - 3, rng.NextDouble() * 6 - 3);
                var (got, want) = (printed(x, y), compiled(x, y));
                if (double.IsNaN(want))
                    continue;
                Assert.True(Math.Abs(got - want) <= 1e-9 * Math.Max(1, Math.Abs(want)),
                    $"{input} at ({x}, {y}): printed {got}, compiled {want}");
                compared++;
            }
            // A comparison that skipped every sample would pass having checked nothing.
            Assert.True(compared >= 50, $"{input}: only {compared} samples had a real value");
        }

        [Fact]
        public void TanhDoesNotDivideAnOverflowByAnOverflow()
        {
            // e^(2x) overflows past x = 355 in double, as it does past x = 45 in a shader's float.
            Entity tanh = "tanh(x)";
            var printed = Printed(tanh);
            Assert.Equal(1.0, printed(400, 0));
            Assert.Equal(-1.0, printed(-400, 0));
            Assert.Equal(tanh.Compile<double, double, double>("x", "y")(400, 0), printed(400, 0));
        }

        // The parser reduces an operation on matrices written out, so these are built as nodes, the
        // way Differentiate or a substitution leaves them.
        public static TheoryData<Entity> MatrixOperations => new()
        {
            new DotProductf(MathS.Vector("x", "y"), MathS.Vector("y", "x")),
            new Determinantf(MathS.Matrix(new Entity[,] { { "x", "y" }, { "1", "x" } })),
            new Tracef(MathS.Matrix(new Entity[,] { { "x", "y" }, { "1", "x" } })),
            (Entity)"sin(x)" + new DotProductf(new CrossProductf(MathS.Vector("x", "y", "1"), MathS.Vector("1", "y", "x")), MathS.Vector("1", "1", "1")),
        };

        [Theory]
        [MemberData(nameof(MatrixOperations))]
        public void AMatrixOperationOnMatricesWrittenOutIsTheNumberItGives(Entity expr)
        {
            var printed = Printed(expr);
            var compiled = expr.Compile<double, double, double>("x", "y");
            var rng = new Random(7);
            for (var k = 0; k < 200; k++)
            {
                var (x, y) = (rng.NextDouble() * 6 - 3, rng.NextDouble() * 6 - 3);
                var (got, want) = (printed(x, y), compiled(x, y));
                Assert.True(Math.Abs(got - want) <= 1e-9 * Math.Max(1, Math.Abs(want)), $"{expr} at ({x}, {y}): printed {got}, compiled {want}");
            }
        }

        // MathS.Settings.UnicodeOutput (#2469) respells printed expressions with glyphs; shader
        // source is code, and must be the same text whatever the display setting says.
        [Theory]
        [InlineData("sqrt(x^2 + y^2) - 1")]
        [InlineData("x^2 * y^(-3) - pi * e^(-x)")]
        [InlineData("iverson(x <= y) * (x - y)^2")]
        [InlineData("x mod 3 - y^(1/3)")]
        public void UnicodeOutputDoesNotReachShaderSource(string input)
        {
            var outputs = new[] { new KeyValuePair<string, Entity>("r", input) };
            var hlsl = MathS.Shaders.Hlsl(outputs);
            var glsl = MathS.Shaders.Glsl(outputs);
            Assert.True(hlsl.Succeeded && glsl.Succeeded, hlsl.ToString() + glsl.ToString());
            using var _ = MathS.Settings.UnicodeOutput.Set(true);
            Assert.Equal(hlsl.Code, MathS.Shaders.Hlsl(outputs).Code);
            Assert.Equal(glsl.Code, MathS.Shaders.Glsl(outputs).Code);
        }

        [Fact]
        public void ARepeatedSubexpressionIsComputedOnce()
        {
            var source = MathS.Shaders.Hlsl(new[] { new KeyValuePair<string, Entity>("r", "sqrt(x^2 + y^2) + 1 / sqrt(x^2 + y^2)") });
            Assert.True(source.Succeeded, source.ToString());
            Assert.Equal(1, source.Code!.Split(new[] { "sqrt(" }, StringSplitOptions.None).Length - 1);
        }

        [Fact]
        public void HlslUsesOneSinCos()
        {
            var source = MathS.Shaders.Hlsl(new[] { new KeyValuePair<string, Entity>("r", "sin(x) * cos(x) + sin(x)") });
            Assert.Contains("sincos(x, am0, am1);", source.Code);
            Assert.DoesNotContain("sin(x)", source.Code);
        }

        [Fact]
        public void GlslHasItsOwnNames()
        {
            var source = MathS.Shaders.Glsl(new[] { new KeyValuePair<string, Entity>("r", "x^(-1/2) + sin(x) cos(x)") });
            Assert.Contains("inversesqrt(x)", source.Code);
            Assert.DoesNotContain("sincos", source.Code);
        }

        [Fact]
        public void VariablesAreRenamed()
        {
            var source = MathS.Shaders.Hlsl(new[] { new KeyValuePair<string, Entity>("n.x", "x y") },
                new Dictionary<Variable, string> { ["x"] = "p.x", ["y"] = "SS_DEPTH" });
            Assert.Equal("n.x = (p.x * SS_DEPTH);\n", source.Code);
        }

        [Theory]
        [InlineData("x + i", "i")]
        [InlineData("x + theta", "theta")]
        [InlineData("x!", "x!")]
        [InlineData("gcd(x, y) + 1", "gcd(x, y)")]
        [InlineData("lcm(x, y) + 1", "lcm(x, y)")]
        [InlineData("round(x)", "round(x)")]
        [InlineData("x provided x > 0", "x provided x > 0")]
        [InlineData("piecewise(x provided x > 0, -x provided x < 0)", "piecewise(x provided (x > 0), (-x) provided (x < 0))")]
        [InlineData("x + dot(x, y)", "dot(x, y)")]
        [InlineData("x + cross(x, y)", "cross(x, y)")]
        [InlineData("det(x)", "det(x)")]
        [InlineData("transpose(x)", "transpose(x)")]
        [InlineData("[x, y]", "[x, y]")]
        [InlineData("iverson(x in RR)", "x in RR")]
        [InlineData("x + {1, 2}", "{ 1, 2 }")]
        public void WhatHasNoFaithfulFormIsDeclined(string input, string at)
        {
            var names = new Dictionary<Variable, string> { ["theta"] = "θ" };
            var source = MathS.Shaders.Hlsl(new[] { new KeyValuePair<string, Entity>("r", input) }, names);
            Assert.False(source.Succeeded);
            Assert.Equal(at, source.At!.ToString());
        }

        // What Compile refuses is declined with the message Compile gives.
        [Theory]
        [InlineData("product(x, x in {1, 2}) + y", "product(x, x in { 1, 2 })")]
        [InlineData("sum(x, x in {1, 2}) + y", "sum(x, x in { 1, 2 })")]
        [InlineData("x + integral(y, y)", "integral(y, y)")]
        [InlineData("x + limit(y, y, 0)", "limit(y, y, 0)")]
        [InlineData("sin(x > 0)", "x > 0")]
        public void WhatCompileRefusesIsDeclinedWithItsMessage(string input, string at)
        {
            Entity expr = input;
            var refusal = Assert.ThrowsAny<AngouriMath.Core.Exceptions.UncompilableNodeException>(() => expr.Compile<double, double, double>("x", "y"));
            foreach (var source in new[] { MathS.Shaders.Hlsl([new("r", expr)]), MathS.Shaders.Glsl([new("r", expr)]) })
            {
                Assert.False(source.Succeeded);
                Assert.Equal(at, source.At!.ToString());
                Assert.Equal(refusal.Message, source.Reason);
            }
        }
    }
}
