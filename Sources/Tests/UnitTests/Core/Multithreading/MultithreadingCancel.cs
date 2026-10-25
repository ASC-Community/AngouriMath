//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using Xunit;
using AngouriMath;
using AngouriMath.Extensions;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace AngouriMath.Tests.Core.Multithreading
{
    [Trait("Area", "Core")]
    public sealed class MultithreadingCancel
    {
        // Solves until it is cancelled, rather than a fixed number of times. A fixed workload put two
        // clocks in this test, each wrong on some machine: it had to last three seconds, which thirty
        // solves no longer do on a desktop, and a cancelled task had to stop within three quarters of
        // a second, which on the Ubuntu runner it sometimes did not. A solve that starts with its token
        // already cancelled has to throw. If it returns instead, the task ends as RanToCompletion and
        // the assertion below says so, rather than the test running forever.
        // https://github.com/asc-community/AngouriMath/issues/2118
        private static void SolveUntilCancelled(CancellationToken token)
        {
            while (true)
            {
                var cancelledBefore = token.IsCancellationRequested;
                "b c d e f g a e cos(x ^ 4 + 3)1 + a f c d cos(x ^ 4 + 2)2 - k d cos(x ^ 4 + 3)2 + sin(x ^ 4 + 3) + e = 0"
                    .Solve("x");
                if (cancelledBefore)
                    return;
            }
        }

        const int CancelAt = 750;

        // How long a cancelled task may take to stop. How soon it stops is the runner's business;
        // that it stops is the library's, so this is a bound no runner should reach.
        const int StopBudget = 30_000;

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, true, false)]
        [InlineData(false, false, false, false, false)]
        [InlineData(false, false, false, false, true)]
        [InlineData(false, false, true, false, false)]
        [InlineData(false, false, true, true, true)]
        [InlineData(false, true, true, true, true)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, true, false)]
        [InlineData(true, false, false, false, false)]
        [InlineData(true, false, false, false, true)]
        [InlineData(true, false, true, false, false)]
        [InlineData(true, false, true, true, true)]
        [InlineData(true, true, true, true, true)]
        public async void TestThoseOnlyCancel(bool generateChild, params bool[] threadToCancel)
        {
            // Each task on a thread of its own rather than on the pool: the tasks are
            // processor-bound, and on a runner with two cores four of them hold every pool
            // thread, so the timer behind CancelAfter -- a pool work item queued behind
            // them -- does not run until one of them has finished, and that one has then
            // run to completion uncancelled. Reproduced with these tasks in a console under
            // DOTNET_PROCESSOR_COUNT=2: fifteen of twenty-four ran to completion, none once
            // on their own threads.
            static Task OnItsOwnThread(Action action, CancellationToken token)
                => Task.Factory.StartNew(action, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            (CancellationTokenSource, Task) GenTask(int c)
            {
                var cts = new CancellationTokenSource();
                var token = cts.Token;
                MathS.Multithreading.SetLocalCancellationToken(token);
                var task = OnItsOwnThread(() =>
                    {
                        if (generateChild)
                            OnItsOwnThread(() => SolveUntilCancelled(token), token).GetAwaiter().GetResult();
                        SolveUntilCancelled(token);
                    }, token);
                var res = (cts, task);
                return res;
            }

            var tasksAndCts = Enumerable.Range(0, threadToCancel.Length).Select(GenTask).ToArray();
            var tasks = tasksAndCts.Select(c => c.Item2).ToArray();
            var ctss = tasksAndCts.Select(c => c.Item1).ToArray();
            try
            {
                for (int i = 0; i < threadToCancel.Length; i++)
                    if (threadToCancel[i])
                        ctss[i].CancelAfter(CancelAt);
                var waitAndSwallow = new WaiterAndSwallower(5);
                await waitAndSwallow.Wait(CancelAt);
                await waitAndSwallow.WaitWhile(() => tasks.Any(c => c.Status == TaskStatus.WaitingToRun)); // wait while at least one task is waiting for execution start
                await Task.WhenAny(Task.WhenAll(tasks.Where((_, i) => threadToCancel[i])), Task.Delay(StopBudget));
                await waitAndSwallow.Wait(CancelAt); // and as long again, for a task cancelled that was not meant to be
            }
            catch (OperationCanceledException) { } // we are going to check their states in finally
            finally
            {
                for (int i = 0; i < threadToCancel.Length; i++)
                    Assert.True(threadToCancel[i] ? tasks[i].IsCanceled : tasks[i].Status == TaskStatus.Running,
                        $"Task number {i} was {(threadToCancel[i] ? "" : "not ")}cancelled, and its status is {tasks[i].Status}");
                foreach (var cts in ctss)
                    cts.Cancel();
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) { }
            }
        }
    }
}
