//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Multithreading
{
    /// <summary>
    /// An expansion stops when the computation it is part of is cancelled, as the other steps that
    /// can run long do.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/718">#718</a>
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class ExpansionCancelTest
    {
        [Fact]
        public void ACancelledExpansionStops()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            // On a thread of its own, since the token is local to the thread that sets it.
            var task = Task.Factory.StartNew(() =>
            {
                MathS.Multithreading.SetLocalCancellationToken(source.Token);
                return "(a + b + c)^5 * (d + e)^3".ToEntity().Expand();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            var thrown = Assert.Throws<AggregateException>(() => task.Wait());
            Assert.IsAssignableFrom<OperationCanceledException>(thrown.InnerException);
        }

        [Fact]
        public void AnExpansionThatIsNotCancelledCompletes()
        {
            using var source = new CancellationTokenSource();
            var task = Task.Factory.StartNew(() =>
            {
                MathS.Multithreading.SetLocalCancellationToken(source.Token);
                return "(a + b)^2".ToEntity().Expand();
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            var expanded = task.Result;
            Assert.IsType<Entity.Sumf>(expanded);
            Assert.Equal((Entity)64, expanded.Substitute("a", 3).Substitute("b", 5).Evaled);
        }
    }
}
