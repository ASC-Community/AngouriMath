//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PeterO.Numbers;
using Xunit;

namespace AngouriMath.Tests.Core
{
    /// <summary>
    /// <c>InternalAMExtensions.Factorial(int)</c> returns the factorial that was asked for, however
    /// many threads are growing its cache at the same time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache used to be a <c>List</c> grown in place under a lock and read without one, which
    /// is not safe for <c>List</c> and fails silently rather than loudly: <c>Add</c> reallocates
    /// the backing array, so a reader outside the lock could see the new <c>Count</c> against the
    /// old array and come back with the value at a different index. A wrong factorial is a
    /// well-formed enormous integer, so it does not throw — it propagates.
    /// </para>
    /// <para>
    /// <b>This test does not reproduce the race, and it was checked against the old
    /// implementation rather than assumed to.</b> Reverted to the <c>List</c> version it still
    /// passes, because the cache grows monotonically: the first thread to ask for the highest
    /// index fills everything in one pass, so the window in which a reader can see a new
    /// <c>Count</c> against an old array is one reallocation wide and is gone before the other
    /// threads arrive. A test that only fails sometimes would be worse than none, so no attempt is
    /// made to widen it.
    /// </para>
    /// <para>
    /// What it is, then, is a correctness test that happens to exercise the growth path from many
    /// threads: it would catch a gross regression — factorials coming back wrong, an exception out
    /// of the cache — and it is honest about not catching the interleaving it is named for. The
    /// argument for the implementation it guards is that <c>List</c> is documented as unsafe for
    /// concurrent read and write, which is a property of the contract rather than of any run.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class FactorialCacheIsThreadSafeTest
    {
        /// <summary>Above anything another test is likely to have cached, so growth really runs.</summary>
        private const int From = 4000;
        private const int To = 4200;

        [Fact]
        public void EveryThreadGetsTheFactorialItAskedFor()
        {
            // Independently computed, in one thread, without touching the cache under test.
            var expected = new EInteger[To + 1];
            expected[0] = EInteger.One;
            for (var i = 1; i <= To; i++)
                expected[i] = expected[i - 1].Multiply(EInteger.FromInt32(i));

            var wrong = new ConcurrentBag<string>();
            Parallel.For(0, 64, worker =>
            {
                // Each worker walks the range from a different offset, so the growth path is
                // entered by several of them at once rather than by one that then wins a race.
                for (var step = 0; step <= To - From; step++)
                {
                    var n = From + (step + worker * 7) % (To - From + 1);
                    var got = AngouriMath.InternalAMExtensions.Factorial(n);
                    if (!got.Equals(expected[n]))
                        wrong.Add($"{n}! came back as a different number");
                }
            });

            Assert.True(wrong.IsEmpty, string.Join("; ", wrong));
        }

        /// <summary>The values themselves are right, single-threaded, at the edges that matter.</summary>
        [Theory]
        [InlineData(0, "1")]
        [InlineData(1, "1")]
        [InlineData(2, "2")]
        [InlineData(5, "120")]
        [InlineData(20, "2432902008176640000")]
        public void TheFactorialsAreCorrect(int n, string expected)
            => Assert.Equal(EInteger.FromString(expected), AngouriMath.InternalAMExtensions.Factorial(n));
    }
}
