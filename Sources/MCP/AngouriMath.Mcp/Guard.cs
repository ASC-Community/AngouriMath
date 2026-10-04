//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath;
using AngouriMath.Core.Exceptions;

namespace AngouriMath.Mcp;

/// <summary>
/// Every call into the library goes through here, for three protections:
///
///  1. Cancellation through MathS.Multithreading.SetLocalCancellationToken, which stops a
///     runaway Simplify or Solve.
///  2. A dedicated thread with a 64 MB stack, abandoned rather than killed on timeout.
///     Cancellation cannot rescue a stack overflow, which would take the server down with every
///     request after it; the deep stack is what keeps a deep recursion from becoming one. .NET
///     cannot abort a thread, so a case that hangs keeps its thread for the life of the process.
///  3. Screening the result for unevaluated nodes and for NaN, below.
/// </summary>
internal static class Guard
{
    public const int DefaultTimeoutMs = 20_000;
    private const int StackBytes = 64 * 1024 * 1024;

    public readonly record struct Outcome<T>(T? Value, string Status, string? Error)
    {
        public bool Ok => Status == "ok";
    }

    public static Outcome<T> Run<T>(Func<T> work, int timeoutMs = DefaultTimeoutMs)
    {
        T? result = default;
        Exception? failure = null;
        using var cts = new CancellationTokenSource();

        var thread = new Thread(() =>
        {
            try
            {
                MathS.Multithreading.SetLocalCancellationToken(cts.Token);
                result = work();
            }
            catch (Exception e)
            {
                failure = e;
            }
        }, StackBytes) { IsBackground = true };

        thread.Start();

        if (!thread.Join(timeoutMs))
        {
            cts.Cancel();
            // Deliberately not joined again: the thread may be stuck in native/deep
            // recursion. Abandon it rather than block the server.
            return new Outcome<T>(default, "timeout",
                $"no answer within {timeoutMs} ms (the call was abandoned)");
        }

        if (failure is not null)
            return new Outcome<T>(default, StatusFor(failure), Describe(failure));

        return new Outcome<T>(result, "ok", null);
    }

    private static Exception Unwrap(Exception e) =>
        e is AggregateException agg && agg.InnerException is not null ? agg.InnerException : e;

    /// <summary>
    /// A refusal is not a failure. Where the library knows it has no rule, it raises
    /// NotSufficientlySupportedException, and that is reported as `declined`, the word this
    /// server uses for an unevaluated integral too. `failed` keeps its meaning: bad input, or
    /// something that genuinely went wrong.
    /// </summary>
    private static string StatusFor(Exception e) =>
        Unwrap(e) is NotSufficientlySupportedException ? "declined" : "failed";

    private static string Describe(Exception e)
    {
        var inner = Unwrap(e);
        // An AngouriBugException is the library's internal assertion. Surfacing the type name
        // tells the caller "library defect", not "bad input".
        return $"{inner.GetType().Name}: {inner.Message}";
    }

    /// <summary>
    /// Operators AngouriMath leaves in the tree when it cannot do the job. Finding one of
    /// these in the result means "declined", not "solved".
    ///
    /// This MUST be tested against the RAW result, before any Simplify: an unevaluated
    /// limit(...) simplifies to NaN, so simplifying first converts an honest decline into
    /// what looks like a wrong answer.
    /// </summary>
    private static readonly string[] UnevaluatedMarkers =
        ["integral(", "derivative(", "limit(", "limitleft(", "limitright("];

    public static bool IsDeclined(string rawStringized)
    {
        foreach (var marker in UnevaluatedMarkers)
            if (rawStringized.Contains(marker, StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary>
    /// A printed NaN in an answer is never legitimate output here: an integral or a
    /// simplification that exists has none in it, so a NaN inside an otherwise plausible
    /// answer, such as `NaN * x`, marks it wrong.
    ///
    /// Caveat worth knowing: `limit x->0 (1/x)` legitimately evaluates to NaN meaning "the
    /// limit does not exist". The limit tool therefore reports that case distinctly rather
    /// than calling it suspect.
    /// </summary>
    public static bool LooksLikeNaN(string rendered) =>
        rendered.Contains("NaN", StringComparison.Ordinal);
}
