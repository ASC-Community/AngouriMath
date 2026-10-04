//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Extensions;
using FluentAssertions;
using HonkSharp.Fluency;
using System;
using System.Linq;
using Xunit;
using static AngouriMath.Entity;

namespace AngouriMath.Tests.Common
{
    [Trait("Area", "Common")]
    public class LambdaCalculusTest
    {
        private readonly Variable x = "x";
        private readonly Variable y = "y";
        private readonly Variable z = "z";
        private readonly Variable f = "f";
        [Fact] public void ApplicationDoesNothingToVariable()
            => x.Apply(4).Alias(out var expected).InnerSimplified.Should().Be(expected);

        [Fact] public void ApplicationDoesNothingToNumber()
            => 5.ToNumber().Apply(4).Alias(out var expected).InnerSimplified.Should().Be(expected);

        [Fact] public void LambdaIdentity()
            => x.LambdaOver(x).Apply(67).InnerSimplified.ShouldBe(67);

        [Fact] public void LambdaTest1()
            => (2 * x).LambdaOver(x).Apply(4).InnerSimplified.ShouldBe(8);

        [Fact] public void LambdaTest2()
            => (2 * x * y).LambdaOver(x).LambdaOver(y).Apply(4).Apply(3).InnerSimplified.ShouldBe(24);

        [Fact] public void LambdaTest3()
            => (2 * x * y).LambdaOver(x).LambdaOver(y).Apply(4).InnerSimplified.ShouldBe((2 * x * 4).LambdaOver(x));

        [Fact] public void LambdaTest4()
            => x.Pow(y).LambdaOver(x).LambdaOver(y).Apply(3).Apply(2).InnerSimplified.ShouldBe(8);

        [Fact] public void LambdaTest5()
            => f.Apply(MathS.pi / 3).LambdaOver(f).Apply("sin").InnerSimplified.ShouldBe("0.5 * (sqrt(3))");

        [Fact] public void LambdaTest6()
            => f.Apply(x.Pow(2)).Apply(x).LambdaOver(f).Apply("derivative").InnerSimplified.ShouldBe(2 * x);

        [Fact] public void LambdaTest7()
            => f.Apply(x).Apply(y).InnerSimplified.ShouldBe(f.Apply(x, y));

        [Fact] public void LambdaTest8()
            => (z * x).LambdaOver(x)
                .Substitute(z, x)
                .InnerSimplified
                .ShouldBe((x * "y").LambdaOver("y"));

        [Fact] public void LambdaTest9()
            => (z * x).LambdaOver(x)
                .LambdaOver(z)
                .Apply(x)
                .InnerSimplified
                .ShouldBe((x * "y").LambdaOver("y"));

        [Fact]
        public void LambdaTest10()
            => (x * z * f)
                .LambdaOver(x)
                .LambdaOver(z)
                .LambdaOver(f)
                .Apply(z)
                .InnerSimplified
                .ShouldBe((x * "y" * z).LambdaOver(x).LambdaOver("y"));

        [Fact] public void LambdaTest11()
            => (x * z * f)
                .LambdaOver(x)
                .LambdaOver(z)
                .LambdaOver(f)
                .Apply(z)
                .Apply(x)
                .InnerSimplified
                .ShouldBe(("a" * x * z).LambdaOver("a"));


        [Fact] public void LambdaTest12()
            => (x * y * z)
                .LambdaOver(x)
                .LambdaOver(y)
                .Apply(x)
                .InnerSimplified
                .ShouldBe(("a" * x * z).LambdaOver("a"));

        [Fact] public void LambdaTest13()
            => x.LambdaOver(x)
                .Apply(3)
                .LambdaOver(y)
                .InnerSimplified
                .ShouldBe(3.ToNumber().LambdaOver(y));

        [Fact] public void LambdaTest14()
            => x.LambdaOver(x)
                .Apply(y.LambdaOver(y))
                .InnerSimplified
                .ShouldBe(y.LambdaOver(y));

        [Fact] public void LambdaTest15()
            => x.Apply(y.LambdaOver(y).Apply(3))
                .InnerSimplified
                .ShouldBe(x.Apply(3));

        [Fact] public void SuccWorks1()
        {
            var (f, x, n) = (MathS.Var("f"), MathS.Var("x"), MathS.Var("n"));
            var number3 = f.Apply(f.Apply(f.Apply(x))).LambdaOver(x).LambdaOver(f);
            var succ = f.Apply(n.Apply(f).Apply(x)).LambdaOver(x).LambdaOver(f).LambdaOver(n);
            var expected = y.Apply(y.Apply(y.Apply(y.Apply(z)))).LambdaOver(z).LambdaOver(y);
            var actual = succ.Apply(number3).InnerSimplified;
            Assert.Equal(expected, actual);
        }

        [Fact] public void EtaReduction1()
            => Assert.Equal(
                "y".ToEntity(),
                "apply(lambda(x, y, apply(x, y)), y)".Simplify()
            );
        
        [Fact] public void EtaReduction2()
            => Assert.Equal(
                "y".ToEntity(),
                "lambda(x, apply(y, x))".Simplify()
            );
        
        [Fact] public void EtaReduction3()
            => Assert.Equal(
                @"lambda(x,
                    apply(x, x)
                )".ToEntity(),
                @"lambda(x,
                    apply(x, x)
                )".Simplify()
            );
        
        [Fact] public void EtaReduction4()
            => Assert.Equal(
                @"apply(
                    e,
                    apply(
                        b,
                        apply(
                            a,
                            c
                        )
                    )
                )".ToEntity(),
                @"lambda(
                    x,
                    apply(
                        apply(
                            e,
                            apply(
                                b,
                                apply(
                                    a,
                                    c
                                )
                            )
                        ),
                        x
                    )
                )".Simplify()
            );
        
        [Fact] public void EtaReduction6()
            => Assert.Equal(
                @"lambda(x,
                    apply(
                        a * (34 + sin(x)),
                        x
                    )
                )
                ".ToEntity(),
                @"lambda(x,
                    apply(
                        a * (sin(x) + 34),
                        x
                    )
                )
                ".Simplify()
            );
        [Fact] public void EtaReduction7()
            => Assert.Equal(
                @"a * (34 + sin(y))".ToEntity(),
                @"lambda(x,
                    apply(
                        a * (sin(y) + 34),
                        x
                    )
                )
                ".Simplify()
            );
            
        [Fact] public void EtaReduction8()
            => Assert.Equal(
                @"apply(
                    e,
                    b,
                    a,
                    c
                )".ToEntity(),
                @"lambda(
                    x,
                    apply(
                        e,
                        b,
                        a,
                        c,
                        x
                    )
                )".Simplify()
            );
    }
}
