//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Numerics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using AngouriMath.Extensions;

// The kernel algebra in Functions/Algebra/Polynomials is internal and is reached from the
// public surface only through Simplify, Solve and Integrate, which means a test driven from
// outside cannot separate a defect in a greatest common divisor from a defect in whichever
// caller happened to invoke it. The pieces that carry their own mathematics -- Berlekamp over
// a prime field, the Hensel lift, the subresultant sequence -- are checked against exhaustive
// or independently computed answers of their own, and that needs them to be nameable there.
// The key is this assembly's own, so the test project signs with key.snk too; the alternative
// to naming it is dropping the strong name, which is a published property of the package.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTests, PublicKey="
    + "0024000004800000940000000602000000240000525341310004000001000100f54ef0f5905ba32e"
    + "cae04751103c5565283a4c4fa2b07627ff7a7e556b287f198203a09935d998ce9d812a696aa2e71c"
    + "552aeb08b7a67f49a5557ef4faab3d9f033182be1a646b24c932399732107462848dcb97acecc844"
    + "9997b1b77e6bd337e1116878226dd4954e004f11193ccd8e29fc156615c798f733712923ffe8d6c5")]

// And to the benchmark project, for one question that cannot be asked from outside: whether a
// rule expressed as data costs more than the same rule as an arm of a `switch`. #746 v2.0 names a
// slower Simplify as unacceptable, so the migration in #248 is only viable if that cost is
// measured rather than assumed, and the matching engine is internal.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DotnetBenchmark, PublicKey="
    + "0024000004800000940000000602000000240000525341310004000001000100f54ef0f5905ba32e"
    + "cae04751103c5565283a4c4fa2b07627ff7a7e556b287f198203a09935d998ce9d812a696aa2e71c"
    + "552aeb08b7a67f49a5557ef4faab3d9f033182be1a646b24c932399732107462848dcb97acecc844"
    + "9997b1b77e6bd337e1116878226dd4954e004f11193ccd8e29fc156615c798f733712923ffe8d6c5")]

// And to the MCP server behind `amcli mcp`, whose tools answer from the catalogue of operations in
// Core/Operations, which stays internal until v3. The server is no package of its own: it ships
// inside the terminal's tool with the library it was built against, so it never meets another
// version's internals. https://github.com/asc-community/AngouriMath/issues/1684
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AngouriMath.Mcp, PublicKey="
    + "0024000004800000940000000602000000240000525341310004000001000100f54ef0f5905ba32e"
    + "cae04751103c5565283a4c4fa2b07627ff7a7e556b287f198203a09935d998ce9d812a696aa2e71c"
    + "552aeb08b7a67f49a5557ef4faab3d9f033182be1a646b24c932399732107462848dcb97acecc844"
    + "9997b1b77e6bd337e1116878226dd4954e004f11193ccd8e29fc156615c798f733712923ffe8d6c5")]

namespace AngouriMath
{
    // Visibility for the class is internal so we can use public for methods as we like
    /// <summary>
    /// This is a set of extensions for internal use. You might be more interested
    /// in publicly exposed <see cref="AngouriMathExtensions"/> or function class <see cref="MathS"/>
    /// </summary>
    internal static class InternalAMExtensions
    {
        private static Exception AngouriBugException(string v)
        {
            throw new NotImplementedException();
        }

        public static TOut AggregateIndexed<TIn, TOut>(this TIn[] arr, TOut seed, Func<TOut, int, TIn, TOut> func)
            => Enumerable.Range(1, arr.Length - 1).Select(id => (id, arr[id])).Aggregate(func(seed, 0, arr[0]), (acc, pair) => func(acc, pair.Item1, pair.Item2));

        /// <summary>
        /// Concatenates 2-element tuples into an IEnumerable
        /// { (1, 2), (3, 4), (5, 6) } -> { 1, 2, 3, 4, 5, 6 }
        /// </summary>
        public static IEnumerable<T> ConcatTuples<T>(this IEnumerable<(T, T)> arrOfTuplesToConcat)
        {
            var l = new List<T>();
            foreach (var el in arrOfTuplesToConcat)
            {
                l.Add(el.Item1);
                l.Add(el.Item2);
            }
            return l;
        }

        /// <summary>
        /// Checks that if the sequences point to the same object or that all elements of them point to the same objects
        /// </summary>
        public static bool SequencesAreEqualReferences<T>(this (IEnumerable<T>, IEnumerable<T>) seqs)
        {
            if (ReferenceEquals(seqs.Item1, seqs.Item2))
                return true;
            if (seqs.Item1.Count() != seqs.Item2.Count())
                return false;
            foreach (var (left, right) in seqs.Zip())
                if (!ReferenceEquals(left, right))
                    return false;
            return true;
        }

        internal record struct Zipped<T1, T2, TList1, TList2>(TList1 First, TList2 Second)
            where TList1 : IReadOnlyList<T1>
            where TList2 : IReadOnlyList<T2>
        {
            public ZippedEnumerator<T1, T2, TList1, TList2> GetEnumerator()
                => new(First, Second);
        }

        internal struct ZippedEnumerator<T1, T2, TList1, TList2>
            where TList1 : IReadOnlyList<T1>
            where TList2 : IReadOnlyList<T2>
        {
            private readonly TList1 list1;
            private readonly TList2 list2;
            private readonly int list1Length;
            private readonly int list2Length;
            private int curr;

            public ZippedEnumerator(TList1 list1, TList2 list2)
            {
                this.list1 = list1;
                this.list2 = list2;
                list1Length = list1.Count;
                list2Length = list2.Count;
                curr = -1;
            }

            public (T1, T2) Current
                => (list1[curr], list2[curr]);

            public bool MoveNext()
            {
                curr++;
                if (curr == list1Length && curr == list2Length)
                    return false;
                if (curr < list1Length && curr < list2Length)
                    return true;
                throw new AngouriBugException(
                    $"Collections should have the same size, and these are {list1Length} and {list2Length}");
            }
        }

        public static Zipped<T1, T2, TList1, TList2> ZipLists<T1, T2, TList1, TList2>(this (TList1, TList2) seqs)
            where TList1 : IReadOnlyList<T1>
            where TList2 : IReadOnlyList<T2>
            => new(seqs.Item1, seqs.Item2);

        public static IEnumerable<(T1 left, T2 right)> Zip<T1, T2>(this (IEnumerable<T1>, IEnumerable<T2>) seqs)
        {
            var iterLeft = seqs.Item1.GetEnumerator();
            var iterRight = seqs.Item2.GetEnumerator();
            bool leftAdv, rightAdv;
            while ((leftAdv = iterLeft.MoveNext()) & (rightAdv = iterRight.MoveNext()))
                yield return (iterLeft.Current, iterRight.Current);

            if (leftAdv != rightAdv)
                throw new AngouriBugException(
                    $"Collections should have the same size, and the {(leftAdv ? "right" : "left")} one ran out first");
        }

        public static IEnumerable<(T1 left, T2 right)> EachForEach<T1, T2>(this (IEnumerable<T1>, IEnumerable<T2>) seqs)
        {
            foreach (var a in seqs.Item1)
                foreach (var b in seqs.Item2)
                    yield return (a, b);
        }

        public static IEnumerable<TResult> EachForEach<T1, T2, TResult>(this (IEnumerable<T1>, IEnumerable<T2>) seqs, Func<T1, T2, TResult> op)
        {
            foreach (var a in seqs.Item1)
                foreach (var b in seqs.Item2)
                    yield return op(a, b);
        }


        public static IEnumerable<T> IntersectSequences<T>(this (IEnumerable<T>, IEnumerable<T>) seqs)
        {
            var tempSet = new HashSet<T>();
            tempSet.Clear();
            foreach (var s in seqs.Item1)
                tempSet.Add(s);
            foreach (var s in seqs.Item2)
                if (tempSet.Contains(s))
                    yield return s;
        }

        public static System.Numerics.Complex Signum(this System.Numerics.Complex z)
            => z == 0 ? 0 : z / z.Magnitude;

        public static System.Numerics.Complex Abs(this System.Numerics.Complex z)
            => System.Numerics.Complex.Abs(z);

        public sealed class ConstantCache
        {
            public static ConstantCache Lookup(EContext context)
            {
                if (!Constants.TryGetValue(context, out var cache))
                    lock (Constants)
                        if (!Constants.TryGetValue(context, out cache))
                        {
                            cache = new ConstantCache(context);
                            Constants.Add(context, cache);
                        }
                return cache;
            }
            private static Dictionary<EContext, ConstantCache> Constants => constants ??= new();
            [ThreadStatic] private static Dictionary<EContext, ConstantCache>? constants;
            ConstantCache(EContext context)
            {
                Half = EDecimal.One.Divide(2, context);
                Pi = EDecimal.PI(context);
                TwoPi = Pi.Multiply(2, context);
                HalfPi = Pi.Multiply(Half, context);
                QuarterPi = HalfPi.Multiply(Half, context);
                E = EDecimal.One.Exp(context);
                // The fixed point the logarithm's and the exponential's series run in: the
                // context's digits in bits, and forty-eight bits over for the series' own
                // truncations and the exponential's ten squarings.
                FixedBits = (int)(context.Precision.ToInt32Checked() * 3.32192809488736 + 48);
                FixedOne = BigInteger.One << FixedBits;
                FixedSqrt2 = ToFixed(EDecimal.FromString("1.41421356237309504880168872420969807856967187537694"), FixedBits);
                FixedPi = ToFixed(Pi, FixedBits);
                FixedTwoPi = FixedPi << 1;
                FixedHalfPi = FixedPi >> 1;
                // ln 2 = 2 artanh(1/3), and ln 10 = ln 8 + ln 5/4 = 3 ln 2 + 2 artanh(1/9).
                FixedLn2 = TwiceArtanh(FixedOne / 3, FixedBits);
                FixedLn10 = FixedLn2 * 3 + TwiceArtanh(FixedOne / 9, FixedBits);
            }
            /// <summary>Represents <see cref="Math.PI"/></summary>
            public EDecimal Pi { get; }
            /// <summary>The bits after the point of the fixed-point numbers below</summary>
            public int FixedBits { get; }
            /// <summary>One, in fixed point: 2 to the <see cref="FixedBits"/></summary>
            public BigInteger FixedOne { get; }
            /// <summary>
            /// 10 to <paramref name="power"/>, kept once each: a fixed-point number is read
            /// back as a decimal by one of a few powers near the context's digits.
            /// </summary>
            public BigInteger TenTo(int power)
            {
                if (!powersOfTen.TryGetValue(power, out var result))
                    powersOfTen[power] = result = BigInteger.Pow(BigTen, power);
                return result;
            }
            private readonly Dictionary<int, BigInteger> powersOfTen = new();
            /// <summary>
            /// <c>ln(1 + j/64)</c> in fixed point, for <paramref name="sixtyFourths"/> = j
            /// from -19 to 27, which covers <c>[1/sqrt 2, sqrt 2]</c>: the logarithm's
            /// argument is divided by the nearest of these before its series, so that the
            /// series' argument is within 1/256. Built outward from <c>ln 1 = 0</c> when first
            /// asked, one short series a step -- <c>ln((64 + j)/(63 + j)) = 2 artanh(1/(127 + 2j))</c>
            /// above one and <c>ln((64 - k)/(65 - k)) = -2 artanh(1/(129 - 2k))</c> below it.
            /// </summary>
            public BigInteger FixedLnOfOnePlus(int sixtyFourths)
            {
                if (sixtyFourths >= 0)
                {
                    while (lnAbove.Count <= sixtyFourths)
                        lnAbove.Add(lnAbove[lnAbove.Count - 1] + TwiceArtanh(FixedOne / (127 + 2 * lnAbove.Count), FixedBits));
                    return lnAbove[sixtyFourths];
                }
                while (lnBelow.Count <= -sixtyFourths)
                    lnBelow.Add(lnBelow[lnBelow.Count - 1] - TwiceArtanh(FixedOne / (129 - 2 * lnBelow.Count), FixedBits));
                return lnBelow[-sixtyFourths];
            }
            private readonly List<BigInteger> lnAbove = new() { BigInteger.Zero };
            private readonly List<BigInteger> lnBelow = new() { BigInteger.Zero };
            /// <summary>
            /// <c>arctan(j/64)</c> in fixed point for <paramref name="sixtyFourths"/> = j from
            /// 0 to 64: the arctangent's argument is brought within 1/128 of zero by the
            /// nearest of these, <c>arctan x = arctan c + arctan((x - c)/(1 + xc))</c>. Built
            /// up from <c>arctan 0 = 0</c> when first asked, one short series a step:
            /// <c>arctan(j/64) - arctan((j - 1)/64) = arctan(64/(4096 + j(j - 1)))</c>.
            /// </summary>
            public BigInteger FixedArctanOf(int sixtyFourths)
            {
                while (arctans.Count <= sixtyFourths)
                {
                    var j = arctans.Count;
                    arctans.Add(arctans[j - 1] + ArctanSeries((FixedOne << 6) / (4096 + j * (j - 1)), FixedBits));
                }
                return arctans[sixtyFourths];
            }
            private readonly List<BigInteger> arctans = new() { BigInteger.Zero };
            /// <summary>The square root of 2, in fixed point, to fifty digits</summary>
            public BigInteger FixedSqrt2 { get; }
            /// <summary>Pi in fixed point, to the context's digits</summary>
            public BigInteger FixedPi { get; }
            /// <summary>2 pi in fixed point</summary>
            public BigInteger FixedTwoPi { get; }
            /// <summary>Pi / 2 in fixed point</summary>
            public BigInteger FixedHalfPi { get; }
            /// <summary>The natural logarithm of 2, in fixed point</summary>
            public BigInteger FixedLn2 { get; }
            /// <summary>The natural logarithm of 10, in fixed point</summary>
            public BigInteger FixedLn10 { get; }
            /// <summary>Represents 2 * <see cref="Math.PI"/></summary>
            public EDecimal TwoPi { get; }
            /// <summary>Represents <see cref="Math.PI"/> / 2</summary>
            public EDecimal HalfPi { get; }
            /// <summary>Represents <see cref="Math.PI"/> / 4</summary>
            public EDecimal QuarterPi { get; }
            /// <summary>Represents <see cref="Math.E"/></summary>
            public EDecimal E { get; }
            /// <summary>Represents 0.5</summary>
            public EDecimal Half { get; }
        }

        /// <summary>
        /// <a href="https://en.wikipedia.org/wiki/Least_common_multiple#Using_the_greatest_common_divisor"/>
        /// </summary>
        public static EInteger Lcm(this EInteger bigintFirst, EInteger bigintSecond) =>
            bigintFirst.IsZero && bigintSecond.IsZero
            ? EInteger.Zero
            : bigintFirst.Abs().Divide(bigintFirst.GreatestCommonDivisor(bigintSecond)).Multiply(bigintSecond.Abs());

        /// <summary><a href="https://en.wikipedia.org/wiki/Combination"/>, equivalent to nCr
        /// or <a href="https://en.wikipedia.org/wiki/Binomial_coefficient"/></summary>
        public static EInteger Combinations(this EInteger n, EInteger k) =>
            n.Factorial() / ((n - k).Factorial() * k.Factorial());

        /// <summary>
        /// Computes Euler phi function
        /// <a href="https://en.wikipedia.org/wiki/Euler%27s_totient_function"/>
        /// </summary>
        /// If integer x is non-positive, the result will be 0
        public static EInteger Phi(this EInteger n)
        {
            if (n <= 0)
                return 0;

            var result = n.ToInt64Checked();
            var original = result;

            for (long i = 2; i * i <= result; i++) {
                if (original % i == 0)
                {
                    while (original % i == 0) original /= i;
                    result -= result / i;
                }
            }

            if (original > 1)
                result -= result / original;

            return result;
        }
        /// <summary>
        /// Computes Euler phi function (for long)
        /// <a href="https://en.wikipedia.org/wiki/Euler%27s_totient_function"/>
        /// </summary>
        /// If integer x is non-positive, the result will be 0
        public static long Phi(this long n)
        {
            if (n <= 0)
                return 0;

            var result = n;
            var original = result;

            for (long i = 2; i * i <= result; i++)
            {
                if (original % i == 0)
                {
                    while (original % i == 0) original /= i;
                    result -= result / i;
                }
            }

            if (original > 1)
                result -= result / original;

            return result;
        }

        /// <summary>
        /// Factorization of integer
        /// </summary>
        public static IEnumerable<(long prime, long power)> Factorize(this EInteger n)
        {
            var result = n.ToInt64Checked();
            var original = result;

            for (long i = 2; i * i <= result; i++)
            {
                if (original % i == 0)
                {
                    long power = 0;
                    while (original % i == 0)
                    {
                        original /= i;
                        power++;
                    }

                    result -= result / i;
                    yield return (i, power);
                }
            }

            if (original > 1)
                yield return (original, 1L);
        }

        /// <summary>
        /// Count of all divisors of an integer
        /// </summary>
        /// If integer x is non-positive, the result will be 0
        public static EInteger CountDivisors(this EInteger n)
        {
            if (n <= 0)
                return 0;

            EInteger result = 1;
            foreach ((var prime, var power) in Factorize(n))
            {
                result *= power + 1;
            }

            return result;
        }

        public static bool GreaterThan(this EDecimal bigDecimalOne, EDecimal bigDecimalTwo) =>
            bigDecimalOne.CompareTo(bigDecimalTwo) > 0;
        public static bool GreaterThanOrEquals(this EDecimal bigDecimalOne, EDecimal bigDecimalTwo) =>
            bigDecimalOne.CompareTo(bigDecimalTwo) >= 0;
        public static bool LessThan(this EDecimal bigDecimalOne, EDecimal bigDecimalTwo) =>
            bigDecimalOne.CompareTo(bigDecimalTwo) < 0;
        public static bool LessThanOrEquals(this EDecimal bigDecimalOne, EDecimal bigDecimalTwo) =>
            bigDecimalOne.CompareTo(bigDecimalTwo) <= 0;

        /// <summary>Analogy of <see cref="Math.Cos(double)"/></summary>
        public static EDecimal Cos(this EDecimal x, EContext context)
            => x.IsFinite ? SinAndCos(x, context).Cos : EDecimal.NaN;

        /// <summary>
        /// <paramref name="context"/> with <paramref name="digits"/> more of precision, the same
        /// instance every time for the same context: <see cref="ConstantCache"/> is keyed by the
        /// context instance, so a working context built afresh on every call would have pi
        /// recomputed on every call, and would leave an entry behind each time.
        /// </summary>
        internal static EContext WithGuardDigits(EContext context, int digits)
        {
            var table = digits switch
            {
                5 => fiveMoreDigits,
                8 => eightMoreDigits,
                12 => twelveMoreDigits,
                _ => throw new AngouriBugException("A guard-digit count without its table"),
            };
            return table.GetValue(context, c => c.WithPrecision(c.Precision.ToInt32Checked() + digits));
        }
        [ConstantField] private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EContext, EContext> eightMoreDigits = new();
        [ConstantField] private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EContext, EContext> fiveMoreDigits = new();
        [ConstantField] private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<EContext, EContext> twelveMoreDigits = new();

        /// <summary>
        /// The sine and the cosine of <paramref name="x"/> together, each to the precision of
        /// <paramref name="context"/>: the argument reduced to <c>[-pi, pi]</c> and then halved
        /// until it is below a twentieth, two Taylor series of a dozen terms there, and the
        /// double-angle formulas back up.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The series alone, on an argument up to <c>pi</c>, is sixty terms at a hundred digits
        /// and its cost was most of a <c>sin(x)</c>; halved six times the argument is below a
        /// twentieth and fifteen terms are enough, and a doubling is a multiplication. Each
        /// doubling multiplies the absolute error by about four, so the work is done with
        /// eight digits more than asked for and rounded at the end.
        /// </para>
        /// <para>
        /// The sine is its own series and not <c>sqrt(1 - cos^2)</c>, which it was: that loses
        /// half the digits wherever the sine is small, and <c>sin(10^-10)</c> came back to fifty
        /// digits at a hundred. https://github.com/asc-community/AngouriMath/issues/1338
        /// </para>
        /// </remarks>
        internal static (EDecimal Sin, EDecimal Cos) SinAndCos(EDecimal x, EContext context)
        {
            var working = WithGuardDigits(context, 8);
            var consts = ConstantCache.Lookup(working);
            var bits = consts.FixedBits;
            var one = consts.FixedOne;

            // In fixed point from here: the reduction is integer arithmetic on pi's digits,
            // and a halving is a shift.
            var fixedX = ToFixed(x, bits);
            // Into [-pi, pi]: the nearest multiple of 2 pi off.
            var shifted = fixedX + consts.FixedPi;
            var turns = BigInteger.DivRem(shifted, consts.FixedTwoPi, out var rest);
            if (shifted.Sign < 0 && !rest.IsZero)
                turns -= 1;   // the division truncates, and this floors
            if (!turns.IsZero)
                fixedX -= consts.FixedTwoPi * turns;
            // Into [-pi/2, pi/2], with the sign the fold costs the cosine.
            var negateCos = false;
            if (fixedX > consts.FixedHalfPi)
            {
                // sin(x) = sin(pi - x), cos(x) = -cos(pi - x)
                fixedX = consts.FixedPi - fixedX;
                negateCos = true;
            }
            else if (fixedX < -consts.FixedHalfPi)
            {
                // sin(x) = sin(-pi - x), cos(x) = -cos(-pi - x)
                fixedX = -consts.FixedPi - fixedX;
                negateCos = true;
            }

            // Halve until |x| < 1/20.
            var halvings = 0;
            var twentieth = one / 20;
            while (BigInteger.Abs(fixedX) > twentieth)
            {
                fixedX = HalveFixed(fixedX);
                halvings++;
            }

            var square = MultiplyFixed(fixedX, fixedX, bits);
            // sin: x - x^3/3! + ...; and the cosine is sqrt(1 - sin^2), which at |x| < 1/20
            // is within 1/800 of one and cancels nothing -- the other way round, the sine
            // from the cosine, is what lost half the digits (see the remarks).
            var sinTerm = fixedX;
            var sin = fixedX;
            for (var i = 1; i < 100000; i++)
            {
                sinTerm = -MultiplyFixed(sinTerm, square, bits) / ((2 * i) * (2 * i + 1));
                if (sinTerm.IsZero)
                    break;
                sin += sinTerm;
            }
            var cos = IntegerSquareRoot((one - MultiplyFixed(sin, sin, bits)) << bits);
            for (var i = 0; i < halvings; i++)
            {
                // sin(2y) = 2 sin(y) cos(y), cos(2y) = 2 cos(y)^2 - 1
                var doubledSin = MultiplyFixed(sin, cos, bits) << 1;
                cos = (MultiplyFixed(cos, cos, bits) << 1) - one;
                sin = doubledSin;
            }
            if (negateCos)
                cos = -cos;
            return (FromFixed(sin, consts, working).RoundToPrecision(context), FromFixed(cos, consts, working).RoundToPrecision(context));
        }

        /// <summary>Analogy of <see cref="Math.Tan(double)"/></summary>
        public static EDecimal Tan(this EDecimal x, EContext context)
        {
            if (!x.IsFinite) return EDecimal.NaN;
            var (sin, cos) = SinAndCos(x, context);
            if (cos.IsZero) return EDecimal.NaN;
            return sin.Divide(cos, context);
        }
        /// <summary>Analogy of <see cref="Math.Sin(double)"/></summary>
        public static EDecimal Sin(this EDecimal x, EContext context)
            => x.IsFinite ? SinAndCos(x, context).Sin : EDecimal.NaN;

        public static EDecimal Signum(this EDecimal x, EContext _)
            => x.Sign;

        /// <summary>
        /// 1 / Cos(x)
        /// </summary>
        public static EDecimal Secant(this EDecimal x, EContext context)
        {
            if (!x.IsFinite)
                return EDecimal.NaN;
            return EDecimal.One.Divide(x.Cos(context), context);
        }

        /// <summary>
        /// 1 / Sin(x)
        /// </summary>
        public static EDecimal Cosecant(this EDecimal x, EContext context)
        {
            if (!x.IsFinite)
                return EDecimal.NaN;
            return EDecimal.One.Divide(x.Sin(context), context);
        }

        /// <summary>
        /// sec(x) = value
        /// 1 / cos(x) = value
        /// 1 / value = cos(x)
        /// x = arccos(1 / value)
        /// </summary>
        public static EDecimal Arcsecant(this EDecimal x, EContext context)
        {
            if (!x.IsFinite)
                return EDecimal.NaN;
            return EDecimal.One.Divide(x.Acos(context), context);
        }

        /// <summary>
        /// csc(x) = value
        /// 1 / sin(x) = value
        /// 1 / value = sin(x)
        /// x = arcsin(1 / value)
        /// </summary>
        public static EDecimal Arccosecant(this EDecimal x, EContext context)
        {
            if (!x.IsFinite)
                return EDecimal.NaN;
            return EDecimal.One.Divide(x.Arcsin(context), context);
        }

        /// <summary>Truncates <paramref name="x"/> to [-2*<see cref="Math.PI"/>, 2*<see cref="Math.PI"/>] </summary>
        /// <summary>Analogy of <see cref="Math.Asin(double)"/></summary>
        /// <remarks>
        /// <c>arcsin(x) = arctan(x/sqrt(1 - x^2))</c>, with <c>1 - x^2</c> as <c>(1 - x)(1 + x)</c>
        /// so that nothing cancels near the ends: <c>1 - x</c> is exact for an <c>x</c> of the
        /// context's width. The series it was, in <c>x</c> directly, converges like
        /// <c>x^(2n)/n^(3/2)</c> and took a hundred terms at a hundred digits for an
        /// argument of a third.
        /// </remarks>
        public static EDecimal Arcsin(this EDecimal x, EContext context)
        {
            if (x.GreaterThan(EDecimal.One) || x.LessThan(-EDecimal.One))
                return EDecimal.NaN;
            var consts = ConstantCache.Lookup(context);

            //known values
            if (x.IsZero) return x;
            if (x.Equals(EDecimal.One)) return consts.HalfPi;
            if (x.Equals(-EDecimal.One)) return -consts.HalfPi;
            if ((x - EDecimal.One).Abs().LessThan(MathS.Settings.PrecisionErrorZeroRange))
                return consts.HalfPi;
            //asin function is odd function
            if (x.IsNegative) return -Arcsin(-x, context);

            var working = WithGuardDigits(context, 5);
            var oneMinusSquare = EDecimal.One.Subtract(x, working).Multiply(EDecimal.One.Add(x, working), working);
            return Arctan(x.Divide(oneMinusSquare.SqrtByIntegerRoot(working), working), working).RoundToPrecision(context);
        }

        /// <summary>Analogy of <see cref="Math.Atan(double)"/></summary>
        /// <remarks>
        /// Reduced to <c>[0, 1]</c> by <c>arctan(x) = pi/2 - arctan(1/x)</c>, then to within
        /// 1/128 of zero by the nearest <c>c = j/64</c> -- <c>arctan x = arctan c +
        /// arctan((x - c)/(1 + xc))</c>, with <c>arctan c</c> from the constant cache -- where
        /// the series <c>y - y^3/3 + y^5/5 - ...</c> is thirty terms at a hundred digits, each
        /// a multiplication and a division by a small integer. It was halved to a twentieth
        /// by <c>arctan(x) = 2 arctan(x/(1 + sqrt(1 + x^2)))</c>, three square roots and
        /// forty-seven terms; before that the arcsine of <c>x/sqrt(1 + x^2)</c>, two
        /// milliseconds at a hundred digits for an argument of a third.
        /// https://github.com/asc-community/AngouriMath/issues/1338
        /// </remarks>
        public static EDecimal Arctan(this EDecimal x, EContext context)
        {
            if (x.IsNaN()) return EDecimal.NaN;
            var consts = ConstantCache.Lookup(context);
            if (x.IsInfinity()) return x.Sign * consts.HalfPi;
            if (x.IsZero) return x;
            if (x.Equals(EDecimal.One)) return consts.QuarterPi;
            if (x.IsNegative) return -Arctan(-x, context);
            var working = WithGuardDigits(context, 5);
            if (x.GreaterThan(EDecimal.One))
                return ConstantCache.Lookup(working).HalfPi.Subtract(Arctan(EDecimal.One.Divide(x, working), working), working).RoundToPrecision(context);

            // In fixed point from here. The nearest sixty-fourth is exact in fixed point
            // (FixedBits is at least six), so the table's entry is the arctangent of it.
            var workingConsts = ConstantCache.Lookup(working);
            var bits = workingConsts.FixedBits;
            var one = workingConsts.FixedOne;
            var fixedX = ToFixed(x, bits);
            var j = (int)(((fixedX << 6) + (one >> 1)) >> bits);
            var c = (one >> 6) * j;
            var y = ((fixedX - c) << bits) / (one + MultiplyFixed(fixedX, c, bits));
            var sum = ArctanSeries(y, bits) + workingConsts.FixedArctanOf(j);
            return FromFixed(sum, workingConsts, working).RoundToPrecision(context);
        }

        /// <summary>
        /// <c>y - y^3/3 + y^5/5 - ...</c> in fixed point, which is <c>arctan y</c>; for
        /// <c>|y|</c> within 1/128, thirty terms at a hundred digits.
        /// </summary>
        private static BigInteger ArctanSeries(BigInteger y, int bits)
        {
            var square = MultiplyFixed(y, y, bits);
            var power = y;
            var sum = y;
            for (var i = 1; i < 100000; i++)
            {
                power = -MultiplyFixed(power, square, bits);
                var term = power / (2 * i + 1);
                if (term.IsZero)
                    break;
                sum += term;
            }
            return sum;
        }
        /// <summary>Analogy of <see cref="Math.Acos(double)"/></summary>
        public static EDecimal Acos(this EDecimal x, EContext context)
        {
            var consts = ConstantCache.Lookup(context);
            if (x.IsZero) return consts.HalfPi;
            if (x.Equals(EDecimal.One)) return EDecimal.Zero;
            if (x.IsNegative) return consts.Pi.Subtract(Acos(-x, context), context);
            return consts.HalfPi.Subtract(Arcsin(x, context), context);
        }

        /// <summary>
        /// Analogy of <see cref="Math.Atan2(double, double)"/> for more see this
        /// <img src="http://i.imgur.com/TRLjs8R.png"/>
        /// </summary>
        /// <remarks>
        /// <b>A zero is a zero, whatever its sign.</b> <c>Math.Atan2</c> reads a negative zero
        /// as a limit from below and answers <c>-pi</c> on the negative axis for it, and this
        /// did the same. A decimal's negative zero is not a limit: it is what
        /// <c>0E-100 * -0.43</c> multiplies to, an artefact of the exponent arithmetic, and
        /// reading <c>-pi</c> off it put <c>-1.54 - 0i</c> a hair below the negative axis, so
        /// that <c>(-1.54)^(-1/2)</c> came back <c>+0.80 i</c> with the downcasting off and
        /// <c>-0.80 i</c> with it on -- the setting deciding a branch. A number on the negative
        /// axis has argument <c>pi</c>. <c>EDecimal.Sign</c> is 0 for either zero, and is what
        /// is read here. https://github.com/asc-community/AngouriMath/issues/1378
        /// </remarks>
        public static EDecimal Arctan2(this EDecimal y, EDecimal x, EContext context)
        {
            if (y.IsNaN() || x.IsNaN()) return EDecimal.NaN;
            var consts = ConstantCache.Lookup(context);
            const int inf = 100;

            // Values for infinity: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Math/atan2
            return (x.Sign * (x.IsInfinity() ? inf : 1), y.Sign * (y.IsInfinity() ? inf : 1)) switch
            {
                (inf, inf) => consts.QuarterPi,
                (inf, -inf) => -consts.QuarterPi,
                (-inf, inf) => consts.QuarterPi * 3,
                (-inf, -inf) => -consts.QuarterPi * 3,
                (_, inf) => consts.HalfPi,
                (_, -inf) => -consts.HalfPi,
                (-inf, -1) => -consts.Pi,
                (-inf, 0) => consts.Pi,
                (-inf, 1) => consts.Pi,
                (inf, _) => EDecimal.Zero,
                (1, _) => Arctan(y.Divide(x, context), context),
                (-1, -1) => Arctan(y.Divide(x, context), context).Subtract(consts.Pi, context),
                (-1, 0) => consts.Pi,
                (-1, 1) => Arctan(y.Divide(x, context), context).Add(consts.Pi, context),
                (0, 1) => consts.HalfPi,
                (0, -1) => -consts.HalfPi,
                // The origin: pi for a negative zero on the x axis, as Math.Atan2 has it, and
                // zero otherwise -- the sign of y decides nothing, see above.
                (0, 0) => x.IsNegative ? consts.Pi : EDecimal.Zero,
                _ => throw new AngouriBugException("Unexpected scenario"),
            };
        }


        // The logarithm and the exponential are series, and a series in EDecimal pays for
        // each term an alignment of exponents, an exact sum and a rounding -- two to three
        // microseconds a term at a hundred digits, which is why PeterO's Log and Exp are
        // three hundred to seven hundred microseconds. The series here run in fixed point:
        // an integer holding the value times 2^FixedBits, where a product is a big-integer
        // multiply and a shift, a division by a term's index an integer division, and a sum
        // an addition -- a tenth of a microsecond a term -- and the value is a decimal again
        // by scaling to a power of ten a few digits past the precision and rounding.
        // https://github.com/asc-community/AngouriMath/issues/1338

        // The fixed-point integers are System.Numerics.BigInteger, not EInteger: a multiply
        // of two hundred-digit integers is 109 ns there and 1,271 ns in PeterO's, a division
        // 193 against 953, and the series are made of those -- sin(0.37) at a hundred digits
        // was 44 µs on EInteger where mpmath, on CPython's integers, which are as fast as the
        // BCL's, is 5.7. The decimal is converted once on the way in and once on the way out,
        // as two's-complement bytes, which both types read and write.
        // https://github.com/asc-community/AngouriMath/issues/1338

        /// <summary>The integer as the BCL's, by its two's-complement bytes.</summary>
        private static BigInteger ToBig(EInteger n) => new(n.ToBytes(littleEndian: true));

        /// <summary>The integer as PeterO's, by its two's-complement bytes.</summary>
        private static EInteger ToEInteger(BigInteger n) => EInteger.FromBytes(n.ToByteArray(), littleEndian: true);

        [ConstantField] private static readonly BigInteger BigTen = new(10);

        /// <summary><paramref name="x"/> times 2^<paramref name="bits"/>, truncated to an integer.</summary>
        private static BigInteger ToFixed(EDecimal x, int bits)
        {
            var exponent = x.Exponent.ToInt32Checked();
            var mantissa = ToBig(x.Mantissa);
            return exponent >= 0
                ? (mantissa * BigInteger.Pow(BigTen, exponent)) << bits
                : (mantissa << bits) / BigInteger.Pow(BigTen, -exponent);
        }

        /// <summary>
        /// A fixed-point <paramref name="n"/> as a decimal to <paramref name="working"/>'s
        /// digits. The scaling to a power of ten is done in <see cref="BigInteger"/>, with
        /// six digits over the precision, and only the rounding is left to
        /// <see cref="EDecimal"/>: the exact reading, <c>n 5^bits</c> over
        /// <c>10^bits</c>, costs a multiply and a rounding of four times the digits.
        /// https://github.com/asc-community/AngouriMath/issues/1338
        /// </summary>
        private static EDecimal FromFixed(BigInteger n, ConstantCache consts, EContext working)
        {
            var bits = consts.FixedBits;
            // The value is n / 2^bits, so its leading digit is near 10^(log10(2) (bitlen n - bits)).
            // The byte length overestimates the bit length by up to seven, which only asks for
            // up to two digits more than the six.
            var magnitude = (int)Math.Floor((n.ToByteArray().Length * 8 - bits) * 0.30102999566398120);
            var digits = working.Precision.ToInt32Checked() + 6 - magnitude;
            var mantissa = digits >= 0
                ? (n * consts.TenTo(digits)) >> bits
                : (n >> bits) / consts.TenTo(-digits);
            return EDecimal.Create(ToEInteger(mantissa), -digits).RoundToPrecision(working);
        }

        /// <summary>
        /// The product of two fixed-point numbers, truncated towards zero -- a shift alone
        /// floors, and a negative term of a series floored never reaches zero.
        /// </summary>
        private static BigInteger MultiplyFixed(BigInteger a, BigInteger b, int bits)
        {
            var product = a * b;
            return product.Sign < 0 ? -((-product) >> bits) : product >> bits;
        }

        /// <summary>Half of a fixed-point number, truncated towards zero.</summary>
        private static BigInteger HalveFixed(BigInteger a)
            => a.Sign < 0 ? -((-a) >> 1) : a >> 1;

        /// <summary>
        /// The integer square root, floored. The root of the top half of the bits, shifted
        /// back, is right to a quarter of them; one Newton step from a value on either side
        /// lands at or above the root (<c>(r + n/r)/2 &gt;= sqrt(n)</c>) with the error
        /// squared, the next one squares it again, and the iteration then descends to the
        /// floor and stops -- three divisions at the full width and three at a quarter of it,
        /// where a power of two above the root took one division per bit of the exponent.
        /// </summary>
        private static BigInteger IntegerSquareRoot(BigInteger n)
        {
            if (n.Sign <= 0)
                return BigInteger.Zero;
            // The byte count is a bound on the bit length that netstandard2.0's BigInteger
            // can give without a logarithm.
            var bytes = n.ToByteArray().Length;
            BigInteger root;
            if (bytes <= 8)
            {
                // Fits a double's exponent; its square root is within one of the floor.
                root = new BigInteger(Math.Sqrt((double)n));
                while (root * root > n)
                    root -= 1;
                while ((root + 1) * (root + 1) <= n)
                    root += 1;
                return root;
            }
            // A multiple of sixteen, so that its half is a whole number of bits.
            var shift = (bytes / 2) * 8;
            root = IntegerSquareRoot(n >> shift) << (shift / 2);
            root = (root + n / root) >> 1;
            while (true)
            {
                var next = (root + n / root) >> 1;
                if (next >= root)
                    return root;
                root = next;
            }
        }

        /// <summary>
        /// The square root of a nonnegative finite <paramref name="x"/> to
        /// <paramref name="context"/>, correctly rounded in its rounding mode, as PeterO's
        /// <see cref="EDecimal.Sqrt(EContext)"/> is: the mantissa, made a whole number of
        /// twice the digits and more by an even power of ten, has its integer square root
        /// taken -- exactly floored, so the true root lies within one unit above it -- and a
        /// sticky digit appended, 1 where the root was inexact, so that the rounding to the
        /// context decides every tie as the true root would. PeterO's is Newton's iteration
        /// in decimal; this is the integer one, faster by the same factor as the series above
        /// it are. Zero, a negative, an infinity and NaN are PeterO's answers.
        /// https://github.com/asc-community/AngouriMath/issues/1338
        /// </summary>
        public static EDecimal SqrtByIntegerRoot(this EDecimal x, EContext context)
        {
            if (!x.IsFinite || x.IsNegative || x.IsZero)
                return x.Sqrt(context);
            var exponent = x.Exponent.ToInt32Checked();
            var mantissa = ToBig(x.Mantissa);
            var digits = x.Precision().ToInt32Checked();
            if ((exponent & 1) != 0)
            {
                // sqrt(m 10^e) = sqrt(10 m) 10^((e - 1)/2): the power taken out must be even.
                mantissa *= BigTen;
                exponent -= 1;
                digits += 1;
            }
            // Padded so that the root has two digits past the context's precision, for the
            // rounding to read, and then one more for the sticky digit.
            var precision = context.Precision.ToInt32Checked();
            var padding = Math.Max(0, (2 * (precision + 2) - digits + 1) / 2);
            if (padding > 0)
                mantissa *= BigInteger.Pow(BigTen, 2 * padding);
            var root = IntegerSquareRoot(mantissa);
            var sticky = root * root == mantissa ? BigInteger.Zero : BigInteger.One;
            return EDecimal.Create(ToEInteger(root * BigTen + sticky), exponent / 2 - padding - 1).RoundToPrecision(context);
        }

        /// <summary>
        /// <c>2 artanh(y)</c> as its series <c>2 (y + y^3/3 + y^5/5 + ...)</c>, which is
        /// <c>ln((1 + y)/(1 - y))</c>, in fixed point: twenty-five terms at a hundred digits
        /// for <c>|y|</c> within <c>1/256</c>, which the logarithm's reduction reaches, and a
        /// hundred and thirty for the third that gives <c>ln 2</c>.
        /// </summary>
        private static BigInteger TwiceArtanh(BigInteger y, int bits)
        {
            var square = MultiplyFixed(y, y, bits);
            var term = y;
            var sum = y;
            for (var k = 1; k < 100000; k++)
            {
                term = MultiplyFixed(term, square, bits);
                if (term.IsZero)
                    break;
                sum += term / (2 * k + 1);
            }
            return sum << 1;
        }

        [ConstantField] private static readonly EDecimal sqrt2 = EDecimal.FromString("1.4142135623730950488");
        [ConstantField] private static readonly EDecimal halfSqrt2 = EDecimal.FromString("0.70710678118654752440");

        /// <summary>
        /// The natural logarithm of <paramref name="x"/> to the precision of
        /// <paramref name="context"/>: the argument written as a mantissa times a power of ten
        /// and of two, the mantissa brought between <c>1/sqrt(2)</c> and <c>sqrt(2)</c>, and
        /// the mantissa divided by the nearest <c>t = 1 + j/64</c>, whose logarithm the
        /// constant cache holds, and <c>2 artanh((q - 1)/(q + 1))</c> for the quotient, a
        /// series in a square below <c>1/65536</c> -- twenty-five terms at a hundred digits
        /// where the series straight from <c>[1/sqrt(2), sqrt(2)]</c> was eighty -- in fixed
        /// point. PeterO's <see cref="EDecimal.Log(EContext)"/> is seven hundred microseconds
        /// at a hundred digits; this is under ten. An argument within
        /// <c>[1/sqrt(2), sqrt(2)]</c> goes to the reduction as it is, and one within 1/128 of
        /// 1 to the series as it is, so a value near 1 keeps every digit rather than losing
        /// them to <c>ln 10 - 3 ln 2 - ...</c> cancelling. Zero, a negative, an infinity and
        /// NaN are PeterO's answers.
        /// https://github.com/asc-community/AngouriMath/issues/1338
        /// </summary>
        public static EDecimal NaturalLogarithm(this EDecimal x, EContext context)
        {
            if (!x.IsFinite || x.IsNegative || x.IsZero)
                return x.Log(context);
            var working = WithGuardDigits(context, 8);
            var consts = ConstantCache.Lookup(working);
            var bits = consts.FixedBits;
            BigInteger mantissa;
            var tens = 0;
            var twos = 0;
            if (x.CompareTo(sqrt2) <= 0 && x.CompareTo(halfSqrt2) >= 0)
                mantissa = ToFixed(x, bits);
            else
            {
                // m * 10^tens with 1 <= m < 10, by moving the point: the digits are untouched;
                // then halved, exactly, until it is at most sqrt(2).
                tens = x.Exponent.Add(x.Precision()).Subtract(1).ToInt32Checked();
                mantissa = ToFixed(x.MovePointLeft(tens), bits);
                while (mantissa > consts.FixedSqrt2)
                {
                    mantissa >>= 1;
                    twos++;
                }
            }
            // The nearest 1 + j/64, exact in fixed point (FixedBits is at least six), and the
            // mantissa over it, within 1/128 of one.
            var one = consts.FixedOne;
            var j = (int)((((mantissa - one) << 6) + (one >> 1)) >> bits);
            if (j != 0)
                mantissa = (mantissa << bits) / (one + (one >> 6) * j);
            var y = ((mantissa - one) << bits) / (mantissa + one);
            var log = TwiceArtanh(y, bits);
            if (j != 0)
                log += consts.FixedLnOfOnePlus(j);
            if (twos != 0)
                log += consts.FixedLn2 * twos;
            if (tens != 0)
                log += consts.FixedLn10 * tens;
            return FromFixed(log, consts, working).RoundToPrecision(context);
        }

        /// <summary>
        /// <c>e</c> to the <paramref name="x"/> to the precision of <paramref name="context"/>:
        /// <c>x = k ln 2 + r</c> with <c>|r|</c> at most half of <c>ln 2</c>, <c>r</c> halved
        /// ten times, the Taylor series there -- twenty-five terms at a hundred digits -- and
        /// ten squarings and the power of two back, in fixed point. PeterO's
        /// <see cref="EDecimal.Exp(EContext)"/> is three hundred microseconds at a hundred
        /// digits; this is about ten. An argument beyond a thousand in magnitude, and an
        /// infinity or NaN, are PeterO's answers.
        /// https://github.com/asc-community/AngouriMath/issues/1338
        /// </summary>
        public static EDecimal Exponential(this EDecimal x, EContext context)
        {
            if (!x.IsFinite || x.Abs().CompareTo(EDecimal.FromInt32(1000)) > 0)
                return x.Exp(context);
            if (x.IsZero)
                return EDecimal.One;
            // Twelve guard digits: the ten squarings each double the error, three digits,
            // and the reduction by up to fifteen hundred times ln 2 costs four.
            var working = WithGuardDigits(context, 12);
            var consts = ConstantCache.Lookup(working);
            var bits = consts.FixedBits;
            var fixedX = ToFixed(x, bits);
            var k = BigInteger.DivRem(fixedX, consts.FixedLn2, out var r);
            var halfLn2 = consts.FixedLn2 >> 1;
            if (r > halfLn2)
            {
                k += 1;
                r -= consts.FixedLn2;
            }
            else if (r < -halfLn2)
            {
                k -= 1;
                r += consts.FixedLn2;
            }
            const int halvings = 10;
            r = r.Sign < 0 ? -((-r) >> halvings) : r >> halvings;
            var term = consts.FixedOne;
            var sum = consts.FixedOne;
            for (var n = 1; n < 100000; n++)
            {
                term = MultiplyFixed(term, r, bits) / n;
                if (term.IsZero)
                    break;
                sum += term;
            }
            for (var i = 0; i < halvings; i++)
                sum = MultiplyFixed(sum, sum, bits);
            var power = (int)k;
            if (power >= 0)
                return FromFixed(sum << power, consts, working).RoundToPrecision(context);
            return FromFixed(sum, consts, working)
                .Divide(EDecimal.FromEInteger(EInteger.One.ShiftLeft(-power)), working)
                .RoundToPrecision(context);
        }

        /// <summary>Analogy of <see cref="Math.Sinh(double)"/></summary>
        public static EDecimal Sinh(this EDecimal x, EContext context)
        {
            var y = x.Exponential(context);
            var yy = EDecimal.One.Divide(y, context);
            return y.Subtract(yy, context).Divide(2, context);
        }

        /// <summary>Analogy of <see cref="Math.Cosh(double)"/></summary>
        public static EDecimal Cosh(this EDecimal x, EContext context)
        {
            var y = x.Exponential(context);
            var yy = EDecimal.One.Divide(y, context);
            return y.Add(yy, context).Divide(2, context);
        }

        /// <summary>Analogy of <see cref="Math.Tanh(double)"/></summary>
        public static EDecimal Tanh(this EDecimal x, EContext context)
        {
            if (x.IsNaN())
                return EDecimal.NaN;
            if (x.IsInfinity())
                return x.Sign;
            var y = x.Exponential(context);
            var yy = EDecimal.One.Divide(y, context);
            return y.Subtract(yy, context).Divide(y.Add(yy, context), context);
        }

        // End of https://github.com/raminrahimzada/CSharp-Helper-Classes/blob/ffbb33c1ee90ce12357c72fa65f2510a09834693/Math/DecimalMath/DecimalMath.cs

        /// <summary>Rounds half up to nearest integer</summary>
        public static EDecimal Round(this EDecimal x) => x.RoundToExponent(0, ERounding.HalfUp);
        /// <summary>Rounds towards zero to nearest integer</summary>
        public static EDecimal Truncate(this EDecimal x) => x.RoundToExponent(0, ERounding.Down);
        /// <summary>Splits decimal into integral part and fractional part.
        /// The fractional part is guaranteed to be positive.</summary>
        public static (EInteger Integral, EDecimal Fractional) SplitDecimal(this EDecimal x)
        {
            var integral = x.Floor().ToEInteger();
            return (integral, x - integral);
        }
        /// <summary>If there is a fractional part, returns the next smallest integer</summary>
        public static EDecimal Ceiling(this EDecimal x) => x.RoundToExponent(0, ERounding.Ceiling);
        /// <summary>If there is a fractional part, returns the previous largest integer</summary>
        public static EDecimal Floor(this EDecimal x) => x.RoundToExponent(0, ERounding.Floor);
        /// <summary>
        /// Rounds to the nearest integer, a tie going to the even one — what
        /// <see cref="Entity.Roundf"/> means by rounding.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="Round(EDecimal)"/> just above, which is half <i>up</i>
        /// and is what the numeric helpers around it want. The two disagree at every tie:
        /// half up sends 5/2 to 3, half to even sends it to 2. Python, SymPy, Mathematica
        /// and IEEE 754 all mean the latter by "round".
        /// </remarks>
        public static EDecimal RoundHalfEven(this EDecimal x) => x.RoundToExponent(0, ERounding.HalfEven);

        // Based on https://github.com/eobermuhlner/big-math/blob/ba75e9a80f040224cfeef3c2ac06390179712443/ch.obermuhlner.math.big/src/main/java/ch/obermuhlner/math/big/BigDecimalMath.java

        /// <summary>
        /// The factorials computed so far, as a snapshot nobody mutates: <c>[i]</c> is <c>i!</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This was a <see cref="List{T}"/> grown in place under a lock, read without one. That is
        /// not a safe pattern for <see cref="List{T}"/> and the failure is silent rather than an
        /// exception: <c>Add</c> reallocates the backing array, so a reader outside the lock can
        /// see the <em>new</em> <c>Count</c> against the <em>old</em> array and return a value for
        /// a different index — a wrong factorial, well-formed and enormous, from a method that
        /// cannot fail. The last <c>return</c> was outside the lock as well.
        /// </para>
        /// <para>
        /// An array published by a single reference write fixes it without making readers wait.
        /// A reader takes the reference once — that read is atomic — and then indexes an array
        /// that is never written again, so it sees either the old complete snapshot or the new
        /// one and never a half-grown one. The shared <see cref="IEnumerator{T}"/> that used to
        /// carry the running product went with it: one enumerator advanced from several threads
        /// is the same defect in a second place.
        /// </para>
        /// </remarks>
        [ConcurrentField] static volatile EInteger[] factorialCache = new[] { EInteger.One };

        /// <summary>What the writers of <see cref="factorialCache"/> take; readers take nothing.</summary>
        [ConstantField] static readonly object factorialCacheLock = new object();

        /// <summary>
        /// Spouge's constants per precision. A <see cref="ConcurrentDictionary{TKey, TValue}"/>
        /// because the reader here had the same shape as the one above — an unsynchronised
        /// <c>TryGetValue</c> racing an <c>Add</c> made under a lock, which for
        /// <see cref="Dictionary{TKey, TValue}"/> is undefined rather than merely stale.
        /// </summary>
        [ConcurrentField] static readonly ConcurrentDictionary<int, EDecimal[]> spougeFactorialConstantsCache = new ConcurrentDictionary<int, EDecimal[]>();

        /**
            <summary>
            Calculates the factorial of the specified integer argument.
            <para>factorial = 1 * 2 * 3 * ... n</para>
            </summary>
            <param name="n">The <see cref="int"/>.</param>
            <returns>The factorial <see cref="EInteger"/>.</returns>
            <exception cref="ArgumentOutOfRangeException">Thrown if x &lt; 0</exception>
        */
        public static EInteger Factorial(int n)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "Illegal factorial(n) for n < 0: n = " + n);
            // One volatile read, then an array that is never written again.
            var cache = factorialCache;
            if (n < cache.Length)
                return cache[n];
            lock (factorialCacheLock)
            {
                // Re-read inside the lock: another writer may have published a longer one.
                cache = factorialCache;
                if (n < cache.Length)
                    return cache[n];
                var grown = new EInteger[n + 1];
                Array.Copy(cache, grown, cache.Length);
                var running = grown[cache.Length - 1];
                for (var i = cache.Length; i <= n; i++)
                    grown[i] = running *= i;
                // The publishing write. Everything above happened to an array no reader has.
                factorialCache = grown;
                return grown[n];
            }
        }

        public static EInteger Factorial(this EInteger n) =>
            n.CanFitInInt32()
            ? Factorial(n.ToInt32Checked())
            : throw new InvalidNumberException(
                $"A factorial argument has to fit in an int32, and this one is {n.GetUnsignedBitLengthAsEInteger()} bits wide");

        /**
         * <summary>
         * Calculates the factorial of the specified <see cref="EDecimal"/>.
         *
         * <para>This implementation uses
         * <a href="https://en.wikipedia.org/wiki/Spouge%27s_approximation">Spouge's approximation</a>
         * to calculate the factorial for non-integer values.</para>
         *
         * <para>This involves calculating a series of constants that depend on the desired precision.
         * Since this constant calculation is quite expensive (especially for higher precisions),
         * the constants for a specific precision will be cached
         * and subsequent calls to this method with the same precision will be much faster.</para>
         *
         * <para>It is therefore recommended to do one call to this method with the standard precision of your application during the startup phase
         * and to avoid calling it with many different precisions.</para>
         *
         * <para>See: <a href="https://en.wikipedia.org/wiki/Factorial#Extension_of_factorial_to_non-integer_values_of_argument">Wikipedia: Factorial - Extension of factorial to non-integer values of argument</a></para>
         * </summary>
         *
         * <param name="x">The <see cref="EDecimal"/></param>
         * <param name="mathContext">The <see cref="EContext"/> used for the result</param>
         * <returns>The factorial <see cref="EDecimal"/></returns>
         * <exception cref="ArgumentOutOfRangeException">Thrown when the precision of the <paramref name="mathContext"/> is outside the int32 range</exception>
         * <seealso cref="Factorial(int)"/>
         * <seealso cref="Gamma(EDecimal, EContext)"/>
         */
        public static EDecimal Factorial(this EDecimal x, EContext mathContext)
        {
            if (x.IsPositiveInfinity())
                return x;
            if (!x.IsFinite)
                return EDecimal.NaN;
            // An integer that fits in an int32 is answered exactly; ToInt32IfExact would throw
            // for anything else, and the approximation below takes that.
            if (x.IsInteger() && x.CompareTo(EDecimal.FromInt32(int.MinValue)) >= 0 && x.CompareTo(EDecimal.FromInt32(int.MaxValue)) <= 0)
            {
                var @int = x.ToInt32Unchecked();
                if (@int < 0) return EDecimal.NaN; // Will become ±∞ if we don't insert this line
                return EDecimal.FromEInteger(Factorial(@int)).RoundToPrecision(mathContext);
            }

            if (!mathContext.Precision.CanFitInInt32())
                throw new WrongNumberOfArgumentsException($"The precision of the {nameof(mathContext)} is outside the int32 range");

            var precision = mathContext.Precision.ToInt32Checked();
            var mc = FactorialWorkingContext(precision);
            // Spouge's approximation holds right of zero. Left of it, the reflection formula
            // x! = pi x/(sin(pi x) (-x)!) asks it at -x. Used left of zero it lost its digits as x
            // neared -a, and past -a had none: at 30 digits (-38.0785)! was -3.1e-35, where it is
            // 7.03e-43. https://github.com/asc-community/AngouriMath/issues/1614
            if (x.IsNegative)
            {
                var piX = ConstantCache.Lookup(mc).Pi.Multiply(x, mc);
                return piX.Divide(piX.Sin(mc).Multiply(SpougeFactorial(x.Negate(), precision, mc), mc), mc).RoundToPrecision(mathContext);
            }
            return SpougeFactorial(x, precision, mc).RoundToPrecision(mathContext);
        }

        /// <summary>
        /// <c>x!</c> by Spouge's approximation with <c>a</c> a tenth over <paramref name="precision"/>,
        /// for <c>Re x &gt;= 0</c>, before any rounding back.
        /// https://en.wikipedia.org/wiki/Spouge%27s_approximation
        /// </summary>
        private static EDecimal SpougeFactorial(EDecimal x, int precision, EContext mc)
        {
            var a = precision * 13 / 10;
            var constants = GetSpougeFactorialConstants(a);
            var factor = constants[0];
            for (int k = 1; k < a; k++)
                factor = factor.Add(constants[k].Divide(x.Add(k), mc));
            var result = x.Add(a).Pow(x.Add(0.5m), mc);
            result = result.Multiply(x.Negate().Subtract(a).Exp(mc));
            return result.Multiply(factor);
        }

        /// <summary>
        /// The factorial's working context: twice <paramref name="precision"/>'s digits, and no bound on
        /// the exponent. The caller's context bounded it, so an intermediate past its ceiling or below
        /// its floor lost its digits though the value fit: at the default precision <c>(400.5)!</c> was
        /// <c>+oo</c>, where it is <c>1.28e870</c>. One per precision, so that the constants the library
        /// caches per context stay few. https://github.com/asc-community/AngouriMath/issues/1614
        /// </summary>
        internal static EContext FactorialWorkingContext(int precision)
            => factorialWorkingContexts.GetOrAdd(precision, static precision
                => EContext.ForPrecision(precision << 1).WithRounding(ERounding.HalfEven).WithUnlimitedExponents());
        [ConcurrentField] private static readonly ConcurrentDictionary<int, EContext> factorialWorkingContexts = new();

        // GetOrAdd may run the factory more than once for one key under contention, and that is
        // harmless here: the constants are a pure function of `a`, so the losers are wasted work
        // rather than a different answer. What it cannot do is hand back another key's entry.
        internal static EDecimal[] GetSpougeFactorialConstants(int a)
            => spougeFactorialConstantsCache.GetOrAdd(a, static a =>
            {
                var constants = new EDecimal[a];
                var mc = EContext.ForPrecision(a * 15 / 10);

                constants[0] = EDecimal.PI(mc).Multiply(2, mc).Sqrt(mc);

                // c_k = (-1)^(k - 1) (a - k)^(k - 1)/(k - 1)! sqrt(a - k) e^(a - k), the first factor a ratio of exact
                // integers and e^(a - k) a product of e's, with digits to spare for the a multiplications that make it.
                // With a power of a - k to k - 1/2 and an exponential for each k, the constants for 300 digits took 20
                // seconds, and for 600 digits 218.
                var work = EContext.ForPrecision(a * 15 / 10 + 10);
                var e = EDecimal.One.Exp(work);
                var powersOfE = new EDecimal[a];
                powersOfE[0] = EDecimal.One;
                for (int m = 1; m < a; m++)
                    powersOfE[m] = powersOfE[m - 1].Multiply(e, work);
                for (int k = 1; k < a; k++)
                {
                    var m = a - k;
                    var ck = EDecimal.FromEInteger(EInteger.FromInt32(m).Pow(k - 1)).Divide(EDecimal.FromEInteger(Factorial(k - 1)), work)
                        .Multiply(EDecimal.FromInt32(m).Sqrt(work), work)
                        .Multiply(powersOfE[m], work)
                        .RoundToPrecision(mc);
                    constants[k] = k % 2 == 0 ? ck.Negate() : ck;
                }

                return constants;
            });
        /**
	     * <summary>
	     * Calculates the gamma function of the specified <see cref="EDecimal"/>.
	     *
	     * This implementation uses <see cref="Factorial(EDecimal, EContext)"/> internally,
	     * therefore the performance implications described there apply also for this method.
	     *
	     * </summary>
	     * <para>See: <a href="https://en.wikipedia.org/wiki/Gamma_function">Wikipedia: Gamma function</a></para>
	     * <param name="x">The <see cref="EDecimal"/></param>
	     * <param name="mathContext">The <see cref="EContext"/> used for the result</param>
	     * <returns>The gamma <see cref="EDecimal"/></returns>
         * <exception cref="ArgumentOutOfRangeException">Thrown when the precision of the <paramref name="mathContext"/> is outside the <see cref="int"/> range</exception>
	     */
        public static EDecimal Gamma(this EDecimal x, EContext mathContext) => Factorial(x.Subtract(EDecimal.One), mathContext);

        public static BigInteger ToBigInteger(this EInteger x) => new BigInteger(x.ToBytes(true));
    }
}