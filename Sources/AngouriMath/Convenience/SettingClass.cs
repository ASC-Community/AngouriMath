//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Threading;

namespace AngouriMath.Convenience
{
    /// <summary>A setting, without its value's type — enough to ask what it currently reads as.</summary>
    internal interface ISettingState
    {
        /// <summary>
        /// The object identifying this setting's value in the calling flow, or
        /// <see langword="null"/> where nothing is set and the default applies.
        /// </summary>
        object? CurrentState { get; }

        /// <summary>Whether a scope of this setting has been opened, in any flow, since the process started.</summary>
        bool EverScoped { get; }
    }

    /// <summary>
    /// What every setting reads as, taken together, so that something holding a result computed
    /// under the settings of a moment can tell whether those settings still hold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A state and not a change count.</b> Counting changes looks equivalent and is not: a
    /// scope opened and closed leaves every setting exactly as it found it while moving a
    /// counter twice, and the library opens such scopes constantly — a single
    /// <c>Simplify</c> of one unevaluated integral opened <b>3520</b> of them, all four from
    /// numeric downcasting in <c>Number/Operators.cs</c>. A counter therefore reports
    /// "everything has changed" continuously and is useless to a cache. Measured, not assumed:
    /// the counter was built first and threw away the whole benefit.
    /// </para>
    /// <para>
    /// The state of one setting is the <i>reference</i> to the frame on top of its stack, which
    /// is exact rather than a hash — releasing a scope restores the very frame object that was
    /// there before it, since the chain below a pushed frame is never rebuilt. So comparing
    /// snapshots is a handful of reference comparisons with no chance of collision, and a
    /// balanced open-and-close compares equal, which is the whole point.
    /// </para>
    /// </remarks>
    internal static class SettingsState
    {
        /// <summary>
        /// Every setting there is. Replaced rather than appended to, so that a reader can walk it
        /// without a lock: registering happens as the settings are constructed and a reader is on
        /// the integrator's hot path, where taking a lock per call would be contention bought for
        /// a list that stops changing before any real work starts.
        /// </summary>
        [ConcurrentField] private static ISettingState[] registered = System.Array.Empty<ISettingState>();

        [ConcurrentField] private static readonly object registering = new();

        internal static void Register(ISettingState setting)
        {
            lock (registering)
            {
                var grown = new ISettingState[registered.Length + 1];
                System.Array.Copy(registered, grown, registered.Length);
                grown[^1] = setting;
                System.Threading.Volatile.Write(ref registered, grown);
            }
        }

        [ConcurrentField] private static bool anyEverScoped;

        /// <summary>Called by every <see cref="Setting{T}.Set"/>, before its frame is pushed.</summary>
        internal static void NoteScoped()
        {
            if (!anyEverScoped)
                System.Threading.Volatile.Write(ref anyEverScoped, true);
        }

        /// <summary>
        /// Whether any setting reads, in this flow, from a scope rather than from its default.
        /// Until a scope of any setting has been opened somewhere this is one read of a flag; after
        /// that, one async-local read for each setting that has ever been scoped.
        /// </summary>
        internal static bool AnyScopeOpen()
        {
            if (!System.Threading.Volatile.Read(ref anyEverScoped))
                return false;
            var settings = System.Threading.Volatile.Read(ref registered);
            for (var i = 0; i < settings.Length; i++)
                if (settings[i].EverScoped && settings[i].CurrentState is not null)
                    return true;
            return false;
        }

        /// <summary>What every setting reads as right now, in this flow.</summary>
        internal static object?[] Snapshot()
        {
            var settings = System.Threading.Volatile.Read(ref registered);
            var snapshot = new object?[settings.Length];
            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i] = settings[i].CurrentState;
            return snapshot;
        }

        /// <summary>
        /// Whether <paramref name="snapshot"/> is still what every setting reads as. One taken
        /// before a setting existed has a different length and is reported stale, which is what
        /// should happen — that setting's value was not part of it.
        /// </summary>
        internal static bool StillHolds(object?[] snapshot)
        {
            var settings = System.Threading.Volatile.Read(ref registered);
            if (snapshot.Length != settings.Length)
                return false;
            for (var i = 0; i < snapshot.Length; i++)
                if (!ReferenceEquals(snapshot[i], settings[i].CurrentState))
                    return false;
            return true;
        }
    }

    /// <summary>
    /// This class for configuring some internal mechanisms from outside
    /// </summary>
    /// <typeparam name="T">
    /// Those configurations can be of different types
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// A setting is one object for the whole process, and what it holds is per *flow*, not
    /// per thread: the stack of values lives in an <see cref="AsyncLocal{T}"/>. A scope
    /// opened before an <see langword="await"/> is therefore still in force after it, and a
    /// scope opened inside a task is invisible to that task's siblings and to whatever
    /// started it. Backing the field with <c>[ThreadStatic]</c> gave neither: a continuation
    /// resumed on a pool thread saw the defaults, and a thread returned to the pool carried
    /// whatever scope was left on it into the next caller.
    /// </para>
    /// <para>
    /// The frames are immutable, which is what makes the reference safe to share. An
    /// <see cref="AsyncLocal{T}"/> copies on assignment to <see cref="AsyncLocal{T}.Value"/>
    /// and on nothing else, so if a frame could be mutated in place, two flows holding the
    /// same chain would write over each other. Pushing and popping therefore build a new
    /// chain and assign it, rather than editing one.
    /// </para>
    /// </remarks>
    public sealed class Setting<T> : ISettingState where T : notnull
    {
        /// <summary>
        /// One pushed value, and the chain below it. Never mutated once built.
        /// </summary>
        /// <remarks>
        /// <see cref="Id"/> is what a scope is released by, rather than the frame's own
        /// reference. Releasing a scope that is not on top rebuilds everything above it, and
        /// a rebuilt frame is a different object — so identifying a scope by its frame would
        /// leave the ones above it impossible to release afterwards. The id is a counter and
        /// not a <see cref="Guid"/>: generating a guid per scope cost more than everything
        /// else <see cref="Set"/> does put together.
        /// </remarks>
        private sealed class Frame
        {
            internal readonly long Id;
            internal readonly T Value;
            internal readonly Frame? Next;
            internal Frame(long id, T value, Frame? next) => (Id, Value, Next) = (id, value, next);
        }

        private readonly AsyncLocal<Frame?> frames = new();
        private long lastId;
        private bool everScoped;

        internal Setting(T defaultValue)
        {
            Default = defaultValue;
            SettingsState.Register(this);
        }

        /// <summary>
        /// The frame this setting currently reads from, as the identity of its state. Released
        /// in order — which <c>using</c> guarantees — a scope restores the very frame that was
        /// on top before it, so this compares equal across a balanced open and close. Out of
        /// order the chain above is rebuilt and this reads as a change, which is conservative
        /// and not wrong.
        /// </summary>
        object? ISettingState.CurrentState => frames.Value;

        bool ISettingState.EverScoped => EverScoped;

        /// <summary>
        /// Whether a scope of this setting has been opened, in any flow, since the process
        /// started, so that a reader can skip asking for the flow's frame while none can exist.
        /// It never goes back: a child flow that captured a scope keeps it after its parent
        /// disposes it, so a count of open scopes returning to zero would not mean there are none.
        /// </summary>
        internal bool EverScoped => Volatile.Read(ref everScoped);

        /// <summary>
        /// Sets the new value for the setting
        /// </summary>
        /// <param name="value">
        /// New value of a setting
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> struct. Make sure to use operator `using` before, so that it auto-disposes
        /// once the method is over.
        /// Example:
        /// <code>
        /// using var _ = MathS.Settings.SomeSetting.Set(4);
        /// // do something, once the method is ended, the setting is automatically returned to the initial statement
        /// </code>
        /// </returns>
        public AutoBackRollableTemporarySettingUnit Set(T value)
        {
            var id = Interlocked.Increment(ref lastId);
            // Before the frame, so that no flow can see the frame without the flag.
            if (!everScoped)
                Volatile.Write(ref everScoped, true);
            SettingsState.NoteScoped();
            frames.Value = new Frame(id, value, frames.Value);
            return new AutoBackRollableTemporarySettingUnit(this, id);
        }

        /// <summary>
        /// Takes the scope numbered <paramref name="id"/> back out of this flow's chain,
        /// wherever in it it sits. Disposal is normally in the reverse order of
        /// <see cref="Set"/>, which is the cheap case of popping the head; out-of-order
        /// disposal rebuilds what was above it. An id that is not in this flow's chain —
        /// because the scope was opened in another one, or is already released — is not an
        /// error and undoes nothing.
        /// </summary>
        private void Remove(long id)
        {
            var top = frames.Value;
            if (top is null)
                return;
            if (top.Id == id)
            {
                frames.Value = top.Next;
                    return;
            }
            var above = new System.Collections.Generic.List<Frame>();
            var current = top;
            while (current is not null && current.Id != id)
            {
                above.Add(current);
                current = current.Next;
            }
            if (current is null)
                return;
            var rebuilt = current.Next;
            for (var i = above.Count - 1; i >= 0; i--)
                rebuilt = new Frame(above[i].Id, above[i].Value, rebuilt);
            frames.Value = rebuilt;
        }

        /// <summary>
        /// For example,
        /// <code>
        /// using var _ = MathS.Settings.Precision.Set(100);
        /// // your code here
        /// </code>
        /// </summary>
        /// <param name="value">New value that will be automatically reverted after action is done</param>
        /// <param name="action">What should be done under this setting</param>
        public void As(T value, Action action)
        {
            using var _ = Set(value);
            action();
        }

        /// <summary>
        /// For example,
        /// <code>
        /// var res = MathS.Settings.Precision.As(100, () => { /* some code considering precision = 100 */ return 4; });
        /// </code>
        /// </summary>
        /// <param name="value">New value that will be automatically reverted after action is done</param>
        /// <param name="action">What should be done under this setting</param>
        public TReturnType As<TReturnType>(T value, Func<TReturnType> action)
        {
            using var _ = Set(value);
            return action();
        }

        /// <summary>
        /// An implicit operator so that one does not have to call <see cref="Value"/>
        /// </summary>
        /// <param name="s">The setting</param>
        public static implicit operator T(Setting<T> s) => s.Value;

        /// <summary>
        /// An implicit operator so that one does not have to call the ctor
        /// </summary>
        /// <param name="a">The value</param>
        public static implicit operator Setting<T>(T a) => new(a);

        /// <summary>
        /// Overriden ToString so that one could see the value of the setting
        /// (if overriden)
        /// </summary>
        public override string? ToString() => Value.ToString();

        /// <summary>
        /// The current value of the setting
        /// </summary>
        public T Value => frames.Value is { } frame ? frame.Value : Default;

        /// <summary>
        /// The default value of the setting
        /// </summary>
        public T Default { get; }

        /// <summary>
        /// Whether anyone has called <see cref="Set"/> on this setting, i.e. whether
        /// <see cref="Value"/> is anything other than the value the constructor pushed.
        /// Lets a default be treated as "nobody expressed an opinion" rather than as a
        /// deliberate choice.
        /// </summary>
        internal bool IsOverriden => frames.Value is not null;

        /// <summary>
        /// This tiny struct is needed to be under `using` operator, so that your settings
        /// are automatically rolled back on the end of your method
        /// <code>
        /// using var _ = MathS.Settings.SomeSetting.Set(4);
        /// // do something, once the method is ended, the setting is automatically returned to the initial statement
        /// </code>
        /// </summary>
        public struct AutoBackRollableTemporarySettingUnit : IDisposable
        {
            private readonly Setting<T> setting;
            private bool disposed;
            private readonly long id;
            internal AutoBackRollableTemporarySettingUnit(Setting<T> settingToRollBack, long id)
                => (setting, disposed, this.id) = (settingToRollBack, false, id);

            /// <inheritdoc/>
            public void Dispose()
            {
                if (disposed)
                    return;
                setting.Remove(id);
                disposed = true;
            }
        }
    }
}
