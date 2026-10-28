//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Exceptions;
using PeterO.Numbers;
using System;

namespace AngouriMath.Functions
{
    internal static class Primes
    {
        [ConstantField] private static readonly object addingPrimes = new();
        /// <summary>
        /// The primes found so far. Grown only under <see cref="addingPrimes"/>, and read only
        /// from inside that lock — <see cref="GetPrime"/> reads <see cref="published"/> instead.
        /// </summary>
        [ConcurrentField] private static readonly List<(Integer actual, int? cache)> primes = new(capacity: 20) { (2, 2), (3, 3), (5, 5), (7, 7), (11, 11) };

        /// <summary>
        /// The same primes as a snapshot nobody mutates, for readers outside the lock.
        /// </summary>
        /// <remarks>
        /// <c>GetPrime</c> used to call <see cref="EnsurePrimesExist"/> and then index
        /// <see cref="primes"/> after the lock had been released, which is not safe for a
        /// <see cref="List{T}"/> that another thread may be growing: <c>Add</c> reallocates the
        /// backing array, so the reader can see the new <c>Count</c> against the old array and
        /// come back with a different element. A wrong prime is a well-formed integer and
        /// propagates into factorisation rather than throwing. Publishing an array by a single
        /// reference write keeps the read lock-free and correct: a reader takes the reference
        /// once, which is atomic, and then indexes something that is never written again.
        /// </remarks>
        [ConcurrentField] private static volatile Integer[] published = ToSnapshot();

        /// <summary>The current <see cref="primes"/> as an array. Call under the lock.</summary>
        private static Integer[] ToSnapshot()
        {
            var snapshot = new Integer[primes.Count];
            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i] = primes[i].actual;
            return snapshot;
        }


        private static void EnsurePrimesExist(int index)
        {
            lock (addingPrimes)
            {
                if (index < primes.Count)
                    return;

                if (index < 100)
                // The fast way (operating on ints)
                {
                    while (primes.Count <= index)
                        AddPrimeC();
                }
                else
                // The slow way (operating on Integers)
                {
                    while (primes.Count <= index)
                        AddPrimeI();
                }

                // The publishing write, still under the lock. Everything above happened to a
                // list no reader outside it has.
                published = ToSnapshot();
            }

            static bool IsPrimeC(int num)
            {
                var max = (int)Math.Sqrt(num) + 1;
                for (int i = 0; primes[i].cache < max; i++)
                    if (num % primes[i].cache == 0)
                        return false;
                return true;
            }

            static bool IsPrimeI(Integer num)
            {
                var max = (EInteger)EFloat.FromEInteger(num.EInteger).Sqrt(MathS.Settings.DecimalPrecisionContext).RoundToIntegerExact(MathS.Settings.DecimalPrecisionContext) + 1;
                for (var i = 0; primes[i].actual < max; i++)
                    if (num % primes[i].actual == Integer.Zero)
                        return false;
                return true;
            }

            static void AddPrimeC()
            {
                var n = (primes[^1].cache + 2) ?? throw new AngouriBugException("It was supposed to be not null");
                while (!IsPrimeC(n))
                    n += 2;
                primes.Add((n, n));
            }

            static void AddPrimeI()
            {
                var n = (primes[^1].actual + 2) ?? throw new AngouriBugException("It was supposed to be not null");
                while (!IsPrimeI(n))
                    n += 2;
                primes.Add((n, null));
            }
        }

        /// <summary>
        /// Whether the whole number is prime, or <see langword="null"/> where it is not decided:
        /// trial division, exact for a number that fits a machine word, and nothing claimed past
        /// that, so that a membership in <c>PP</c> is left open rather than answered slowly or
        /// wrongly. https://github.com/asc-community/AngouriMath/issues/1450
        /// </summary>
        internal static bool? IsPrime(EInteger n)
        {
            if (n.CompareTo(EInteger.FromInt32(2)) < 0)
                return false;
            if (!n.CanFitInInt64())
                return null;
            var value = n.ToInt64Checked();
            if (value < 4)
                return true;
            if (value % 2 == 0)
                return false;
            for (long divisor = 3; divisor * divisor <= value; divisor += 2)
                if (value % divisor == 0)
                    return false;
            return true;
        }

        /// <summary>
        /// The first prime at or after <paramref name="atLeast"/>, or <see langword="null"/>
        /// where the search would leave the machine word or run past the steps allowed.
        /// </summary>
        internal static EInteger? NextPrime(EInteger atLeast)
        {
            var candidate = atLeast.CompareTo(EInteger.FromInt32(2)) < 0 ? EInteger.FromInt32(2) : atLeast;
            for (var step = 0; step < LongestSearch; step++, candidate += 1)
                switch (IsPrime(candidate))
                {
                    case true: return candidate;
                    case null: return null;
                }
            return null;
        }

        /// <summary>The primes in a numeric interval, where there are few enough to list; <see langword="null"/> otherwise.</summary>
        internal static Entity.Set? Within(Entity.Set.Interval interval)
        {
            if (interval.Left.Evaled is not Real from || interval.Right.Evaled is not Real to || !to.IsFinite)
                return null;
            var lower = from.IsFinite ? from.EDecimal.RoundToExponent(EInteger.Zero, ERounding.Ceiling).ToEInteger() : EInteger.FromInt32(2);
            if (from.IsFinite && !interval.LeftClosed && from.EDecimal.CompareTo(EDecimal.FromEInteger(lower)) == 0)
                lower += 1;
            var upper = to.EDecimal.RoundToExponent(EInteger.Zero, ERounding.Floor).ToEInteger();
            if (!interval.RightClosed && to.EDecimal.CompareTo(EDecimal.FromEInteger(upper)) == 0)
                upper -= 1;
            if (lower.CompareTo(EInteger.FromInt32(2)) < 0)
                lower = EInteger.FromInt32(2);
            if (upper.CompareTo(lower) < 0)
                return Entity.Set.Empty;
            if ((upper - lower).CompareTo(EInteger.FromInt32(LongestSearch)) > 0)
                return null;
            var members = new List<Entity>();
            for (var candidate = lower; candidate.CompareTo(upper) <= 0; candidate += 1)
                switch (IsPrime(candidate))
                {
                    case true: members.Add(Integer.Create(candidate)); break;
                    case null: return null;
                }
            return new Entity.Set.FiniteSet(members);
        }

        /// <summary>How far a search for a prime, or a listing of them, is taken before the set is left as written.</summary>
        private const int LongestSearch = 100000;

        /// <summary>
        /// The prime factors of a positive whole number with their multiplicities, smallest first,
        /// or <see langword="null"/> where trial division up to <see cref="LargestTrialDivisor"/>
        /// leaves a part it cannot call prime: every number up to its square is factorised, and
        /// a larger one is whenever what is left after the small primes is below that square.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </summary>
        internal static List<(EInteger Prime, int Power)>? Factorization(EInteger n)
        {
            var factors = new List<(EInteger, int)>();
            var rest = n;
            for (long divisor = 2; divisor <= LargestTrialDivisor; divisor += divisor == 2 ? 1 : 2)
            {
                if (rest.CanFitInInt64())
                    return Finish(rest.ToInt64Checked(), divisor);
                var asInteger = EInteger.FromInt64(divisor);
                var power = 0;
                while (rest.Remainder(asInteger).IsZero)
                {
                    rest = rest.Divide(asInteger);
                    power++;
                }
                if (power > 0)
                    factors.Add((asInteger, power));
            }
            return null;

            // The same division on a machine word, once what is left fits one.
            List<(EInteger, int)>? Finish(long left, long from)
            {
                for (var divisor = from; divisor <= LargestTrialDivisor && divisor * divisor <= left; divisor += divisor == 2 ? 1 : 2)
                {
                    var power = 0;
                    while (left % divisor == 0)
                    {
                        left /= divisor;
                        power++;
                    }
                    if (power > 0)
                        factors.Add((EInteger.FromInt64(divisor), power));
                }
                if (left > 1)
                {
                    // No divisor up to the bound, so what is left is prime if it is below the square.
                    if (left / LargestTrialDivisor >= LargestTrialDivisor)
                        return null;
                    factors.Add((EInteger.FromInt64(left), 1));
                }
                return factors;
            }
        }

        /// <summary>How far trial division goes: far enough to factorise every number up to 10^12.</summary>
        private const long LargestTrialDivisor = 1000000;

        /// <summary>
        /// How many primes are at most <paramref name="n"/>, by a sieve over the odd numbers, or
        /// <see langword="null"/> above <see cref="LargestCounted"/>.
        /// https://github.com/asc-community/AngouriMath/issues/2182
        /// </summary>
        internal static int? CountUpTo(EInteger n)
        {
            if (n.CompareTo(EInteger.FromInt32(2)) < 0)
                return 0;
            if (n.CompareTo(EInteger.FromInt32(LargestCounted)) > 0)
                return null;
            var last = n.ToInt32Checked();
            // composite[i] is whether 2i + 1 is composite.
            var composite = new System.Collections.BitArray(last / 2 + 1);
            var count = 1;
            for (var i = 1; 2 * i + 1 <= last; i++)
            {
                if (composite[i])
                    continue;
                count++;
                long odd = 2 * i + 1;
                for (var multiple = odd * odd; multiple <= last; multiple += 2 * odd)
                    composite[(int)(multiple / 2)] = true;
            }
            return count;
        }

        /// <summary>How far <see cref="CountUpTo"/> sieves before a count is left as written.</summary>
        private const int LargestCounted = 10000000;

        internal static Integer GetPrime(int index)
        {
            // One volatile read, then an array that is never written again.
            var snapshot = published;
            if (index < snapshot.Length)
                return snapshot[index];
            EnsurePrimesExist(index);
            return published[index];
        }
    }
}
