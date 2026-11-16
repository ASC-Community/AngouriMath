//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HonkSharp.Fluency;
using HonkSharp.Functional;
using Xunit;
using AngouriMath.Core.Exceptions;

namespace AngouriMath.Tests
{
    public static class TestExtensions
    {
        /// <summary>
        /// The roots a solution set lists at the given indices: each member of a finite set, and
        /// each member of an indexed union at those of <paramref name="indices"/> its index set has,
        /// so that <c>union({ 2 pi n }, n in ZZ)</c> at 0 and 1 is 0 and 2 pi. A set of any other
        /// kind fails the test. https://github.com/asc-community/AngouriMath/issues/2319
        /// </summary>
        public static List<Entity> Members(this Entity roots, params int[] indices)
        {
            switch (roots)
            {
                case Entity.Set.FiniteSet finite:
                    return finite.ToList();
                case Entity.Set.Unionf(var left, var right):
                    return left.Members(indices).Concat(right.Members(indices)).ToList();
                case Entity.Set.IndexedUnionf family:
                    var members = new List<Entity>();
                    foreach (var index in indices)
                        if (((Entity)index).In(family.Over).Simplify() == Entity.Boolean.True)
                            members.AddRange(family.Body.Substitute(family.Var, index).Members(indices));
                    return members;
                default:
                    throw new Xunit.Sdk.XunitException($"{roots.Stringize()} lists no roots");
            }
        }

        /// <summary>
        /// The principal roots of a solution set, the members of each family at index 0, as a set:
        /// <c>{ ln(2) }</c> for <c>union({ ln(2) + 2 pi i n }, n in ZZ)</c>.
        /// </summary>
        public static Entity.Set.FiniteSet Principal(this Entity roots)
            => new Entity.Set.FiniteSet(roots.Members(0).Select(root => root.InnerSimplified));

        public static (T, T) Into2<T>(this IEnumerable<T> seq)
        {
            using var enumerator = seq.GetEnumerator();
            (T, T) res;
            if (!enumerator.MoveNext()) throw new Exception("Sequence should contain two elements");
            res.Item1 = enumerator.Current;
            if (!enumerator.MoveNext()) throw new Exception("Sequence should contain two elements");
            res.Item2 = enumerator.Current;
            if (enumerator.MoveNext()) throw new Exception("Sequence should contain two elements");
            return res;
        }

        public static void ShouldBe(this Entity @this, Entity other)
        {
            using var _ = MathS.Diagnostic.OutputExplicit.Set(true);
            Assert.True(@this == other, $"\nExpected: {PrettyOutput(other)}\n\nActual: {PrettyOutput(@this)}\n");

            static string PrettyOutput(Entity entity)
                => entity is Entity.Matrix m ? m.ToString(multilineFormat: true) : entity.ToString();
        }

        public static T AsNotNull<T>(T? value)
        {
            Assert.NotNull(value);
            return value!;
        }

        public static T ShouldBeNotNull<T>(this T? value)
            => AsNotNull(value);

        public static void ShouldBeNull<T>(this T? value)
            => Assert.Null(value);

        // okay; this one makes an unnecessary allocation
        public static IEnumerable<T> ShouldCountTo<T>(this IEnumerable<T> value, int target)
        {
            Assert.Equal(target, value.Count());
            return value;
        }
        
        public static Unit ShouldApproximatelyEqual(this Entity actual, Entity target)
            => (actual.EvalNumerical() - target.EvalNumerical())
                .Abs()
                .Pipe(error => Assert.True(error < 0.01, $"Error is {error}"))
                .Discard();
    }

    /// <summary>
    /// When waits, ignores all the operation canceled exceptions
    /// </summary>
    public sealed class WaiterAndSwallower
    {
        private readonly int precision;
        public WaiterAndSwallower(int precisionMs)
        {
            this.precision = precisionMs;
        }

        public async Task Wait(int timeMs)
        {
            var precLocal = precision;
            while (timeMs > 0)
            {
                try
                {
                    await Task.Delay(precLocal);
                    timeMs -= precLocal;
                }
                catch (OperationCanceledException) { }
            }
        }

        public async Task WaitWhile(Func<bool> predicate)
        {
            while (predicate())
                try
                {
                    await Task.Delay(10);
                }
                catch (OperationCanceledException) { }
        }
    }

    public sealed class TimeOutChecker
    {
        public bool BeingCompletedForLessThan(Action action, long timeoutMs)
        {
            var stopped = false;
            var th = new Thread(() =>
            {
                action();
                stopped = true;
            });
            th.Start();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!stopped && stopwatch.ElapsedMilliseconds < timeoutMs)
                Thread.Sleep(5);
            return stopped;
        }
    }

    public sealed class ThreadingChecker
    {
        private readonly Action<int> action;
        public ThreadingChecker(Action<int> action)
        {
            this.action = action;
        }

        public void Run(int iterCount = 1, int threadCount = 4)
        {
            var tasks = new Task[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                // Each task's own index: the lambda read the loop's variable, which had usually reached
                // threadCount by the time a task ran, so every thread was asked the same thing.
                var index = i;
                tasks[i] = Task.Run(
                    () =>
                    {
                        var iterCountLocal = iterCount;
                        for (var j = 0; j < iterCountLocal; j++)
                            action(index);
                    }
                );
            }
            Task.WaitAll(tasks);
        }
    }
}
