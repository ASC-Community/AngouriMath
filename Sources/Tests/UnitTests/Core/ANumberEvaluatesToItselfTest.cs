//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AngouriMath.Core;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <see cref="Entity.Evaled"/> of a number is the number itself -- the same instance, at every precision, scoped
    /// or not, read first or again -- as it was when the value was cached; and a number given a codomain that does not
    /// contain it still evaluates to NaN.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ANumberEvaluatesToItselfTest
    {
        private static IEnumerable<Entity.Number> Numbers()
        {
            yield return Integer.Zero;
            yield return Integer.One;
            yield return Integer.Create(-7);
            yield return Integer.Create(EInteger.FromString("123456789012345678901234567890"));
            yield return Rational.Create(1, 3);
            yield return Rational.Create(-22, 7);
            yield return 0.1;
            yield return 1e-200;
            yield return Real.CreateWithoutDowncasting(EDecimal.FromString("0.5"));
            yield return Real.NaN;
            yield return Real.PositiveInfinity;
            yield return Real.NegativeInfinity;
            yield return Complex.Create(1, 2);
            yield return Complex.Create(0.1, -1e-30);
            yield return Complex.ImaginaryOne;
            yield return new System.Numerics.Complex(double.NaN, 1);
            yield return MathS.pi.EvalNumerical();
            yield return MathS.e.EvalNumerical();
        }

        private static void EachIsItself(string where)
        {
            foreach (var number in Numbers())
            {
                Assert.True(ReferenceEquals(number, number.Evaled), $"{number} at {where}");
                Assert.True(ReferenceEquals(number, number.Evaled), $"{number} read again at {where}");
                Assert.True(number.EvaluableNumerical, $"{number} at {where}");
                Assert.True(ReferenceEquals(number, number.EvalNumerical()), $"{number} at {where}");
            }
        }

        [Fact] public void AtTheDefaults() => EachIsItself("the defaults");

        [Fact] public void UnderPrecisionScopes()
        {
            foreach (var digits in new[] { 5, 30, 100, 300 })
            {
                using var _ = MathS.Settings.DecimalPrecisionContext.Set(new EContext(digits, ERounding.HalfUp, -digits, 1000, false));
                EachIsItself($"{digits} digits");
                using (MathS.Settings.DecimalPrecisionContext.Set(EContext.Unlimited.WithPrecision(digits + 7)))
                    EachIsItself($"{digits + 7} digits, nested");
                EachIsItself($"{digits} digits, after a nested scope");
            }
            EachIsItself("the defaults, after the scopes");
        }

        [Fact] public void WithTheDowncastingOff()
        {
            using var _ = MathS.Settings.DowncastingEnabled.Set(false);
            EachIsItself("downcasting off");
        }

        [Fact] public void OnEveryThread()
            => Parallel.For(0, 64, i =>
            {
                using var _ = MathS.Settings.DecimalPrecisionContext.Set(EContext.ForPrecision(20 + i));
                EachIsItself($"{20 + i} digits on a thread");
            });

        [Fact] public void ANumberOutsideItsCodomainIsNaN()
        {
            Assert.Equal(MathS.NaN, Rational.Create(1, 3).WithCodomain(Domain.Integer).Evaled);
            Assert.Equal(MathS.NaN, Complex.Create(1, 2).WithCodomain(Domain.Real).Evaled);
            Assert.Equal(MathS.NaN, ((Entity)0.1).WithCodomain(Domain.Rational).Evaled);
            var widened = Integer.Create(5).WithCodomain(Domain.Real);
            Assert.True(ReferenceEquals(widened, widened.Evaled));
        }
    }
}
