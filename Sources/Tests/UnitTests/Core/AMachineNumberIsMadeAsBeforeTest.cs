//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeterO.Numbers;
using Xunit;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// A number made from a <see cref="double"/>, a <see cref="float"/> or a <see cref="System.Numerics.Complex"/> is
    /// the number 2.7.0 made from it -- the same type, the same decimal, the same text and the same hash -- under the
    /// default settings and under every setting the cheaper path reads. <see cref="NumberCreationAsOf270"/> is the
    /// old code, copied.
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AMachineNumberIsMadeAsBeforeTest
    {
        private const int Million = 1_000_000;

        /// <summary>The failures, a few of them, with their count; an empty string when there are none.</summary>
        private sealed class Mismatches
        {
            private int count;
            private readonly List<string> first = new();
            internal void Add(string what)
            {
                if (Interlocked.Increment(ref count) <= 10)
                    lock (first)
                        first.Add(what);
            }
            internal void AssertNone()
                => Assert.True(count == 0, $"{count} values made differently, among them:\n" + string.Join("\n", first));
        }

        private static string Describe(Entity e) => $"{e.GetType().Name} {e}";

        private static bool Same(Entity actual, Entity expected)
            => actual.GetType() == expected.GetType()
            && actual.Equals(expected)
            && actual.GetHashCode() == expected.GetHashCode()
            && actual.ToString() == expected.ToString()
            && (actual is not Real r || expected is not Real s || r.EDecimal.Equals(s.EDecimal));

        private static void CheckDouble(double x, Mismatches mismatches)
        {
            var expected = NumberCreationAsOf270.FromMachineNumber(EDecimal.FromDouble(x));
            Entity viaEntity = x;
            Real viaReal = x;
            Entity.Number viaNumber = x;
            foreach (var actual in new Entity[] { viaEntity, viaReal, viaNumber, MathS.Numbers.Create(x) })
                if (!Same(actual, expected))
                {
                    mismatches.Add($"{x:R}: {Describe(actual)}, was {Describe(expected)}");
                    return;
                }
            Complex viaComplex = x;
            CheckComplexResult(viaComplex, EDecimal.FromDouble(x), EDecimal.Zero, $"{x:R} as a complex", mismatches);
        }

        private static void CheckFloat(float x, Mismatches mismatches)
        {
            var expected = NumberCreationAsOf270.FromMachineNumber(EDecimal.FromSingle(x));
            Entity viaEntity = x;
            Real viaReal = x;
            foreach (var actual in new Entity[] { viaEntity, viaReal })
                if (!Same(actual, expected))
                {
                    mismatches.Add($"{x:R}f: {Describe(actual)}, was {Describe(expected)}");
                    return;
                }
            Complex viaComplex = x;
            CheckComplexResult(viaComplex, EDecimal.FromSingle(x), EDecimal.Zero, $"{x:R}f as a complex", mismatches);
        }

        private static void CheckComplex(double re, double im, Mismatches mismatches)
        {
            var value = new System.Numerics.Complex(re, im);
            Entity viaEntity = value;
            Complex viaComplex = value;
            CheckComplexResult(viaEntity, EDecimal.FromDouble(re), EDecimal.FromDouble(im), $"{re:R} + {im:R}i", mismatches);
            CheckComplexResult(viaComplex, EDecimal.FromDouble(re), EDecimal.FromDouble(im), $"{re:R} + {im:R}i as a complex", mismatches);
        }

        private static void CheckComplexResult(Entity actual, EDecimal re, EDecimal im, string input, Mismatches mismatches)
        {
            var (number, realPart, imaginaryPart) = NumberCreationAsOf270.FromMachineNumbers(re, im);
            bool same = number is not null
                ? Same(actual, number)
                : actual.GetType() == typeof(Complex)
                    && Same(((Complex)actual).RealPart, realPart!) && Same(((Complex)actual).ImaginaryPart, imaginaryPart!);
            if (!same)
                mismatches.Add($"{input}: {Describe(actual)}, was {(number is not null ? Describe(number) : $"{Describe(realPart!)} + {Describe(imaginaryPart!)} i")}");
        }

        /// <summary>Under the settings <paramref name="scope"/> opens, in every worker.</summary>
        private static void Run(int count, Func<int, double> value, Func<IDisposable?>? scope = null)
        {
            var mismatches = new Mismatches();
            Parallel.For(0, Environment.ProcessorCount, worker =>
            {
                using var _ = scope?.Invoke();
                for (var i = worker; i < count; i += Environment.ProcessorCount)
                    CheckDouble(value(i), mismatches);
            });
            mismatches.AssertNone();
        }

        private static double FromBits(int seed, int i)
        {
            // A value from the bits of a hash of the index: random, reproducible, and the same in every worker.
            var bits = unchecked((long)Mix((ulong)seed * 0x9E3779B97F4A7C15UL + (ulong)i));
            var x = BitConverter.Int64BitsToDouble(bits);
            return double.IsNaN(x) || double.IsInfinity(x) ? bits * 1e-300 : x;
        }

        private static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>(r - 1/2) 10^k, r uniform in [0, 1), k in -30..30 in turn.</summary>
        private static double Scaled(int seed, int i)
            => ((Mix((ulong)seed * 0x9E3779B97F4A7C15UL + (ulong)i) >> 11) * (1.0 / (1UL << 53)) - 0.5) * Math.Pow(10, i % 61 - 30);

        private static IEnumerable<double> Neighbours(double x, int ulps)
        {
            var d = x;
            var u = x;
            yield return x;
            for (var i = 0; i < ulps; i++)
            {
                d = Math.BitDecrement(d);
                u = Math.BitIncrement(u);
                yield return d;
                yield return u;
            }
        }

        private static readonly double[] ShortDecimals =
        {
            0.1, 0.2, 0.3, 0.05, 0.37, 1.0 / 3, 2.0 / 3, 1.0 / 7, 22.0 / 7, 2.5, 1234.5, 0.125, 0.001, 0.999, 100.01,
            3.14159, 0.7, 1.1, 99999.99, 0.0625, 1e-8, 12345.678, 1.0 / 99999989, 99999989.5, 1e8 + 0.5,
        };

        private static readonly double[] Specials =
        {
            0.0, -0.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon, -double.Epsilon,
            double.MaxValue, double.MinValue, 2.2250738585072014E-308, 2.2250738585072009E-308, 1e-310, -1e-320, 1e-16,
            1e-50, 1e-100, 1e-200, 1e-300, 1e300, 1, -1, 0.5, -0.5,
        };

        private static IEnumerable<double> Edges()
        {
            foreach (var x in ShortDecimals)
                foreach (var n in Neighbours(x, 8))
                {
                    yield return n;
                    yield return -n;
                }
            foreach (var x in new[] { Math.Pow(2, 53), Math.Pow(2, 62), Math.Pow(2, 63), long.MinValue, long.MaxValue, 1e8, 1e9, 1e15, 1e16 })
                foreach (var n in Neighbours(x, 16))
                {
                    yield return n;
                    yield return -n;
                }
            foreach (var x in new[] { 1e-16, 1e-50, 1e-15, 1e-30, 1e-150, 1e-17, 2e-16, 2e-50, 1e-8, 1e-9, 2.5e-9, 1e-10 })
                foreach (var n in Neighbours(x, 8))
                    foreach (var shifted in new[] { n, -n, 1 + n, 1 - n, 7 + n, 7 - n, 0.5 + n, 0.5 - n, 1e6 + n, 1e6 - n })
                        yield return shifted;
            foreach (var x in Specials)
                yield return x;
        }

        private static readonly List<double> edges = new(Edges());

        private static readonly Func<IDisposable?>[] Scopes =
        {
            () => null,
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(30, ERounding.HalfUp, -30, 1000, false)),
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(300, ERounding.HalfUp, -300, 1000, false)),
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(150, ERounding.HalfEven, -100000, 100000, false)),
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(29, ERounding.HalfUp, -29, 1000, false)),
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(20, ERounding.HalfUp, -100, 1000, false)),
            () => MathS.Settings.DecimalPrecisionContext.Set(EContext.Unlimited.WithPrecision(100)),
            () => MathS.Settings.DecimalPrecisionContext.Set(new EContext(100, ERounding.Down, -100, 1000, false)),
            () => MathS.Settings.PrecisionErrorZeroRange.Set(EDecimal.Create(1, -16)),
            () => MathS.Settings.PrecisionErrorZeroRange.Set(EDecimal.Create(1, -12)),
            () => MathS.Settings.PrecisionErrorZeroRange.Set(EDecimal.Create(1, -30)),
            () => MathS.Settings.PrecisionErrorZeroRange.Set(EDecimal.Create(1, -6)),
            () => MathS.Settings.MaxAbsNumeratorOrDenominatorValue.Set(EInteger.FromInt32(1000)),
            () => MathS.Settings.MaxAbsNumeratorOrDenominatorValue.Set(EInteger.FromInt32(1_000_000_000)),
            () => MathS.Settings.MaxAbsNumeratorOrDenominatorValue.Set(EInteger.FromInt64(1_000_000_000_000)),
            () => MathS.Settings.FloatToRationalIterCount.Set(1),
            () => MathS.Settings.FloatToRationalIterCount.Set(3),
            () => MathS.Settings.FloatToRationalIterCount.Set(100),
            () => MathS.Settings.FloatToRationalIterCount.Set(200),
            () => MathS.Settings.DowncastingEnabled.Set(false),
        };

        [Fact]
        public void RandomBitPatterns() => Run(Million, i => FromBits(1, i));

        [Fact]
        public void RandomValuesAtEveryScale() => Run(Million, i => Scaled(2, i));

        [Fact]
        public void EveryRatioOfIntegersUnderAThousand()
        {
            var ratios = new List<double>();
            for (var q = 1; q < 1000; q++)
                for (var p = -q; p <= q; p++)
                    ratios.Add((double)p / q);
            Run(ratios.Count, i => ratios[i]);
        }

        [Fact]
        public void EdgesUnderTheDefaults() => Run(edges.Count, i => edges[i]);

        [Fact]
        public void UnderEverySettingTheCheckReads()
        {
            foreach (var scope in Scopes)
            {
                Run(edges.Count, i => edges[i], scope);
                Run(20_000, i => FromBits(3, i), scope);
                Run(20_000, i => Scaled(4, i), scope);
                Run(20_000, i => (double)(i % 1999 - 999) / (i / 1999 % 997 + 1), scope);
            }
        }

        /// <summary>
        /// A ratio with a denominator up to past the bound, and the doubles a few ulps from it: where the search finds
        /// a ratio the cheaper path must not have refused to look.
        /// </summary>
        private static double NearARatio(int seed, int i)
        {
            var bits = Mix((ulong)seed * 0x9E3779B97F4A7C15UL + (ulong)i);
            var q = 2 + (double)(bits % 200_000_000UL) / Math.Pow(10, (int)(bits >> 40) % 8);
            q = Math.Floor(q);
            var p = Math.Floor(q * ((bits >> 20) % 1000 / 7.0)) + (bits >> 35) % 3;
            var x = p / q;
            for (var k = (int)((bits >> 50) % 5); k > 0; k--)
                x = (bits >> 55) % 2 == 0 ? Math.BitIncrement(x) : Math.BitDecrement(x);
            return (bits >> 60) % 2 == 0 ? x : -x;
        }

        [Fact]
        public void NearRatiosWithLargeDenominators()
        {
            Run(200_000, i => NearARatio(12, i));
            foreach (var scope in Scopes)
                Run(10_000, i => NearARatio(13, i), scope);
        }

        [Fact]
        public void Floats()
        {
            var mismatches = new Mismatches();
            Parallel.For(0, Environment.ProcessorCount, worker =>
            {
                for (var i = worker; i < 300_000; i += Environment.ProcessorCount)
                {
                    var f = BitConverter.Int32BitsToSingle(unchecked((int)Mix(5UL * 0x9E3779B97F4A7C15UL + (ulong)i)));
                    CheckFloat(float.IsNaN(f) ? 0.1f : f, mismatches);
                    CheckFloat((float)Scaled(6, i), mismatches);
                }
            });
            for (var q = 1; q < 300; q++)
                for (var p = -q; p <= q; p++)
                    CheckFloat((float)p / q, mismatches);
            foreach (var x in edges)
                CheckFloat((float)x, mismatches);
            foreach (var f in new[] { float.Epsilon, float.MaxValue, float.MinValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -0f, 1e-40f })
                CheckFloat(f, mismatches);
            mismatches.AssertNone();
        }

        [Fact]
        public void ComplexNumbers()
        {
            var mismatches = new Mismatches();
            var imaginaries = new[] { 0.0, -0.0, 1e-60, -1e-60, 1e-40, 1e-16, 0.5, -2.5, 0.1, double.NaN, double.PositiveInfinity, 1.0 / 3 };
            Parallel.For(0, Environment.ProcessorCount, worker =>
            {
                for (var i = worker; i < 100_000; i += Environment.ProcessorCount)
                {
                    CheckComplex(Scaled(7, i), Scaled(8, i), mismatches);
                    CheckComplex(FromBits(9, i), imaginaries[i % imaginaries.Length], mismatches);
                    CheckComplex(imaginaries[i % imaginaries.Length], FromBits(10, i), mismatches);
                }
            });
            foreach (var re in edges)
                foreach (var im in imaginaries)
                    CheckComplex(re, im, mismatches);
            mismatches.AssertNone();
        }

        /// <summary>
        /// The equivalence above holds trivially if nothing takes the cheaper path; at the defaults almost every
        /// double does.
        /// </summary>
        [Fact]
        public void AlmostEveryDoubleIsDecidedWithoutTheSearch()
        {
            var decided = 0;
            const int count = 100_000;
            for (var i = 0; i < count; i++)
                if (Real.IsProvablyNoSmallRational(Scaled(11, i) * 1e4 / Math.Pow(10, i % 61 - 30)))
                    decided++;
            Assert.True(decided > count * 0.99, $"{decided} of {count}");
        }
    }
}
