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
    /// What every setting reads as, taken together, as one object, so that something holding a
    /// result computed under the settings of a moment can tell whether those settings still hold
    /// with one async-local read and one reference comparison.
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
    /// A <see cref="State"/> is reached from the defaults by the scopes opened to reach it,
    /// setting by setting and value by value, and is made once for each path: opening a scope of
    /// a setting at a value from a state gives the state it gave before. So the scopes the
    /// library opens on every call — <c>Solve</c> opens two — reach the same state on every
    /// call, and a value cached under them is found again. A scope closed in order puts back
    /// the very state it replaced, so a balanced open and close compares equal, and one closed out
    /// of order reaches the state the scopes left open reach. Values compare by
    /// <see cref="object.Equals(object)"/>, so a value compared by reference only is conservative:
    /// it reaches a state of its own, and a value cached under it is not found again.
    /// https://github.com/asc-community/AngouriMath/issues/2479
    /// </para>
    /// </remarks>
    internal static class SettingsState
    {
        /// <summary>
        /// Every setting there is, at the index it reads its value from in a <see cref="State"/>.
        /// Replaced rather than appended to, so that a reader can walk it without a lock.
        /// </summary>
        [ConcurrentField] private static ISettingState[] registered = System.Array.Empty<ISettingState>();

        [ConcurrentField] private static readonly object registering = new();

        /// <summary>Registers a setting, and answers its index.</summary>
        internal static int Register(ISettingState setting)
        {
            lock (registering)
            {
                var grown = new ISettingState[registered.Length + 1];
                System.Array.Copy(registered, grown, registered.Length);
                grown[^1] = setting;
                System.Threading.Volatile.Write(ref registered, grown);
                return grown.Length - 1;
            }
        }

        /// <summary>One value a scope put on a setting, with the one it covers. Never mutated once built.</summary>
        internal sealed class Entry
        {
            internal readonly object Setting;
            internal readonly object Value;
            internal readonly Entry? Below;
            internal Entry(object setting, object value, Entry? below) => (Setting, Value, Below) = (setting, value, below);
        }

        /// <summary>
        /// What every setting reads as: the top of each setting's stack, by its index. Never mutated
        /// once built, but for the table of the states one scope away, which only grows, and only
        /// by an atomic exchange.
        /// </summary>
        internal sealed class State
        {
            private readonly Entry?[] tops;

            /// <summary>
            /// The state this one was reached from, held so that a state kept alive keeps the path
            /// to it alive: a value cached under it is found again by a flow that opens the same
            /// scopes from the defaults, which it would not be if a state on the way had been
            /// collected and made again as another object.
            /// </summary>
            private readonly State? parent;

            /// <summary>
            /// The states one scope away, with the setting and the value that reach each, held
            /// weakly: a state is kept by the scopes open in it and by the values cached under it,
            /// and once neither is left it is collected and its entry dropped the next time one is
            /// added. So a program that scopes a value that differs on every call -- a precision
            /// computed from its data -- keeps no more states than it has scopes open and values
            /// cached. And at most <see cref="Bound"/> of the entries are of one setting, so that
            /// those values, while they are alive, do not crowd out the scopes of the others, which
            /// the library opens on every call; past it a scope of that setting reaches a state of
            /// its own, which only costs its values a cache miss.
            /// </summary>
            /// <remarks>
            /// Two flows opening the same scope at once both read the table; the one whose exchange
            /// fails reads it again, finds the state the other added and answers that one, so both
            /// get the same object. Where they do not -- past the bound, or a state collected and
            /// made again -- two states with the same values are only a cache miss: a value is
            /// answered under the state object it was computed under, never under another with the
            /// same contents, and a state object is never changed once made.
            /// </remarks>
            private (object Setting, object Value, WeakReference<State> Reached)[] next = System.Array.Empty<(object, object, WeakReference<State>)>();
            internal const int Bound = 32;

            internal State(Entry?[] tops, State? parent) => (this.tops, this.parent) = (tops, parent);

            internal Entry? Top(int index) => index < tops.Length ? tops[index] : null;

            /// <summary>The state with <paramref name="value"/> on <paramref name="setting"/>; compared as <typeparamref name="T"/>, so that a value type is not boxed to look it up.</summary>
            internal State After<T>(Setting<T> setting, int index, T value) where T : notnull
            {
                var known = System.Threading.Volatile.Read(ref next);
                while (true)
                {
                    foreach (var (s, v, reached) in known)
                        if (ReferenceEquals(s, setting) && v is T seen && System.Collections.Generic.EqualityComparer<T>.Default.Equals(seen, value)
                            && reached.TryGetTarget(out var state))
                            return state;
                    if (Added(setting, index, value, ref known) is { } made)
                        return made;
                }
            }

            /// <summary>The same for a value already boxed, as a scope closed out of order replays the ones above it.</summary>
            internal State After(object setting, int index, object value)
            {
                var known = System.Threading.Volatile.Read(ref next);
                while (true)
                {
                    foreach (var (s, v, reached) in known)
                        if (ReferenceEquals(s, setting) && Equals(v, value) && reached.TryGetTarget(out var state))
                            return state;
                    if (Added(setting, index, value, ref known) is { } made)
                        return made;
                }
            }

            // The state reached, added to the table, with the entries of collected states dropped,
            // where there is room for one more of its setting; null where another flow changed the
            // table first, with known set to what it changed it to, to be read again.
            private State? Added(object setting, int index, object value, ref (object, object, WeakReference<State>)[] known)
            {
                var pushed = new Entry?[System.Math.Max(tops.Length, index + 1)];
                System.Array.Copy(tops, pushed, tops.Length);
                pushed[index] = new Entry(setting, value, Top(index));
                var made = new State(pushed, this);
                var kept = new System.Collections.Generic.List<(object, object, WeakReference<State>)>(known.Length + 1);
                var ofThisSetting = 0;
                foreach (var entry in known)
                    if (entry.Item3.TryGetTarget(out _))
                    {
                        kept.Add(entry);
                        if (ReferenceEquals(entry.Item1, setting))
                            ofThisSetting++;
                    }
                if (ofThisSetting >= Bound)
                    return made;
                kept.Add((setting, value, new WeakReference<State>(made)));
                var was = System.Threading.Interlocked.CompareExchange(ref next, kept.ToArray(), known);
                if (ReferenceEquals(was, known))
                    return made;
                known = was;
                return null;
            }
        }

        /// <summary>
        /// One scope open in a flow: its id, which is what releases it, the setting it is on, the
        /// state with it open, and the scope that was innermost when it opened. Made for each scope
        /// opened, so that a scope is released by its own id and by nothing
        /// else: a copy of the disposal struct disposed twice, or after the same scope was opened
        /// again, finds no scope with its id and does nothing.
        /// </summary>
        internal sealed class Scope
        {
            internal readonly long Id;
            internal readonly int Index;
            internal readonly State State;
            internal readonly Scope? Below;
            internal Scope(long id, int index, State state, Scope? below) => (Id, Index, State, Below) = (id, index, state, below);
        }

        /// <summary>The defaults, from which the states are reached; a flow at them reads <see langword="null"/>.</summary>
        [ConstantField] private static readonly State defaults = new(System.Array.Empty<Entry?>(), parent: null);

        [ConcurrentField] private static readonly AsyncLocal<Scope?> scopes = new();

        [ConcurrentField] private static long lastId;

        /// <summary>Whether any setting has been scoped, in any flow, since the process started.</summary>
        [ConcurrentField] private static bool anyEverScoped;

        /// <summary>
        /// What every setting reads as in this flow: <see langword="null"/> where every setting
        /// reads its default, which costs one read of a flag until a scope has been opened.
        /// </summary>
        internal static State? Current => System.Threading.Volatile.Read(ref anyEverScoped) ? scopes.Value?.State : null;

        /// <summary>Whether any setting reads, in this flow, from a scope rather than from its default.</summary>
        internal static bool AnyScopeOpen() => Current is not null;

        /// <summary>Opens a scope of <paramref name="setting"/> at <paramref name="value"/>, and answers the id that releases it.</summary>
        internal static long Open<T>(Setting<T> setting, int index, T value) where T : notnull
        {
            if (!anyEverScoped)
                System.Threading.Volatile.Write(ref anyEverScoped, true);
            var id = Interlocked.Increment(ref lastId);
            var below = scopes.Value;
            scopes.Value = new Scope(id, index, (below?.State ?? defaults).After(setting, index, value), below);
            return id;
        }

        /// <summary>
        /// Releases the scope numbered <paramref name="id"/>, wherever in this flow's scopes it sits.
        /// Released in order, which is the cheap case, the scope below it is back; out of order, the
        /// scopes above it are opened again over the one below it, so the flow reaches the state it
        /// would have reached without it. An id that is not in this flow's scopes -- opened in another
        /// flow, or released already -- is not an error and undoes nothing.
        /// </summary>
        internal static void Close(long id)
        {
            var top = scopes.Value;
            if (top is null)
                return;
            if (top.Id == id)
            {
                scopes.Value = top.Below;
                return;
            }
            var above = new System.Collections.Generic.List<Scope>();
            var current = top;
            while (current is not null && current.Id != id)
            {
                above.Add(current);
                current = current.Below;
            }
            if (current is null)
                return;
            var rebuilt = current.Below;
            for (var i = above.Count - 1; i >= 0; i--)
            {
                var scope = above[i];
                var entry = scope.State.Top(scope.Index)!;
                rebuilt = new Scope(scope.Id, scope.Index, (rebuilt?.State ?? defaults).After(entry.Setting, scope.Index, entry.Value), rebuilt);
            }
            scopes.Value = rebuilt;
        }

        /// <summary>What every setting reads as right now, in this flow.</summary>
        internal static object?[] Snapshot() => new object?[] { Current };

        /// <summary>Whether <paramref name="snapshot"/> is still what every setting reads as.</summary>
        internal static bool StillHolds(object?[] snapshot)
            => snapshot.Length == 1 && ReferenceEquals(snapshot[0], Current);
    }

    /// <summary>
    /// A value of a <see cref="SettingsDependent{T}"/> with the state it was computed under,
    /// <see langword="null"/> for the defaults, and the one value before it computed under another
    /// state, if any. Two are kept because the library's own scopes ask a node under two states in
    /// turn -- <c>Solve</c>'s, and its analytical solver's inside it, with the zero range at 1e-7 --
    /// and one slot recomputed the value at every turn: 46% more allocation on solving a quadratic.
    /// Not generic, so that the type test on every read needs no lookup of the type in the code
    /// shared between the instantiations over reference types.
    /// </summary>
    internal sealed class SettingsStamped
    {
        internal readonly object? Value;
        internal readonly SettingsState.State? State;
        internal readonly SettingsStamped? Previous;
        internal SettingsStamped(object? value, SettingsState.State? state, SettingsStamped? previous)
            => (Value, State, Previous) = (value, state, previous);

        /// <summary>Stands for a value of <see langword="null"/> computed at the defaults, which an empty slot cannot.</summary>
        [ConstantField] internal static readonly object NullAtDefaults = new();
    }

    /// <summary>
    /// A cached value that depends on the settings it was computed under -- an evaluation, whose
    /// numbers are rounded by the precision and the zero range, and anything computed from one.
    /// Answered only under the settings it was computed under: <see cref="SettingsState.Current"/>
    /// is the stamp, and a value computed with every setting at its default is held as it is, so
    /// that the common case costs no more than <see cref="HonkSharp.Laziness.LazyPropertyA{T}"/>.
    /// Equal to every other, as a cache on a record has to be, so that it takes no part in the
    /// record's equality.
    /// https://github.com/asc-community/AngouriMath/issues/2479
    /// </summary>
    internal struct SettingsDependent<T> : IEquatable<SettingsDependent<T>>
    {
        // Null, nothing yet; a SettingsStamped, the values of up to two states; anything else, the
        // value computed at the defaults alone, or NullAtDefaults. One field, written in one store,
        // so that no flow reads one flow's value under another's stamp.
        private object? slot;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public T GetValue<TOwner>(Func<TOwner, T> compute, TOwner owner)
            => TryGetValue(out var value) ? value : Computed(compute, owner);

        /// <summary>
        /// The value computed under the settings in force, where there is one. Apart from
        /// <see cref="GetValue"/> so that a reader on a hot path pays for the delegate only on a miss.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(out T value)
        {
            var seen = Volatile.Read(ref slot);
            if (seen is null)
            {
                value = default!;
                return false;
            }
            var current = SettingsState.Current;
            if (seen is SettingsStamped stamped)
            {
                if (ReferenceEquals(stamped.State, current))
                {
                    value = Read(stamped.Value);
                    return true;
                }
                if (stamped.Previous is { } previous && ReferenceEquals(previous.State, current))
                {
                    value = Read(previous.Value);
                    return true;
                }
            }
            else if (current is null)
            {
                value = Read(seen);
                return true;
            }
            value = default!;
            return false;
        }

        // Only a T, or NullAtDefaults for a null, is ever stored, so a reference is read without
        // the type check a cast to a type that is not sealed costs on every read.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static T Read(object? stored)
        {
            if (ReferenceEquals(stored, SettingsStamped.NullAtDefaults) || stored is null)
                return default!;
#if NET
            return typeof(T).IsValueType ? (T)stored : System.Runtime.CompilerServices.Unsafe.As<object, T>(ref stored);
#else
            return (T)stored;
#endif
        }

        /// <summary>Computes the value under the settings in force and keeps it, with the one kept before under other settings.</summary>
        public T Computed<TOwner>(Func<TOwner, T> compute, TOwner owner)
        {
            // The state is taken before the computation, so a scope opened and closed inside it
            // leaves the value stamped with the state it was asked in.
            var current = SettingsState.Current;
            var computed = compute(owner);
            var seen = Volatile.Read(ref slot);
            object stored = (object?)computed ?? SettingsStamped.NullAtDefaults;
            SettingsStamped? kept = seen switch
            {
                null => null,
                // Another flow got there first under the same settings: keep what it kept.
                SettingsStamped stamped when ReferenceEquals(stamped.State, current) => stamped.Previous,
                SettingsStamped { Previous: null } stamped => stamped,
                SettingsStamped stamped => new SettingsStamped(stamped.Value, stamped.State, previous: null),
                _ when current is null => null,
                _ => new SettingsStamped(seen, state: null, previous: null),
            };
            Volatile.Write(ref slot, current is null && kept is null ? stored : new SettingsStamped(stored, current, kept));
            return computed;
        }

        public bool Equals(SettingsDependent<T> other) => true;
        public override bool Equals(object? obj) => obj is SettingsDependent<T>;
        public override int GetHashCode() => 0;
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
    /// per thread: the stacks of values of every setting live in one
    /// <see cref="AsyncLocal{T}"/>, as a <see cref="SettingsState.State"/>. A scope
    /// opened before an <see langword="await"/> is therefore still in force after it, and a
    /// scope opened inside a task is invisible to that task's siblings and to whatever
    /// started it. Backing the field with <c>[ThreadStatic]</c> gave neither: a continuation
    /// resumed on a pool thread saw the defaults, and a thread returned to the pool carried
    /// whatever scope was left on it into the next caller.
    /// </para>
    /// <para>
    /// The states are immutable, which is what makes the reference safe to share. An
    /// <see cref="AsyncLocal{T}"/> copies on assignment to <see cref="AsyncLocal{T}.Value"/>
    /// and on nothing else, so if a state could be mutated in place, two flows holding it
    /// would write over each other. Opening and closing a scope therefore assign another
    /// state, rather than editing one.
    /// </para>
    /// </remarks>
    public sealed class Setting<T> : ISettingState where T : notnull
    {
        private readonly int index;
        private bool everScoped;

        internal Setting(T defaultValue)
        {
            Default = defaultValue;
            index = SettingsState.Register(this);
        }

        /// <summary>
        /// The value this setting currently reads from, as the identity of its state. A scope
        /// released in order puts back the very state that was there before it, so this
        /// compares equal across a balanced open and close.
        /// </summary>
        object? ISettingState.CurrentState => Top;

        bool ISettingState.EverScoped => EverScoped;

        /// <summary>
        /// Whether a scope of this setting has been opened, in any flow, since the process
        /// started, so that a reader can skip asking for the flow's state while none can hold it.
        /// It never goes back: a child flow that captured a scope keeps it after its parent
        /// disposes it, so a count of open scopes returning to zero would not mean there are none.
        /// </summary>
        internal bool EverScoped => Volatile.Read(ref everScoped);

        private SettingsState.Entry? Top => EverScoped ? SettingsState.Current?.Top(index) : null;

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
            // Before the state, so that no flow can see the state without the flag.
            if (!everScoped)
                Volatile.Write(ref everScoped, true);
            return new AutoBackRollableTemporarySettingUnit(SettingsState.Open(this, index, value));
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
        public T Value => Top is { } top ? (T)top.Value : Default;

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
        internal bool IsOverriden => Top is not null;

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
            private readonly long id;
            private bool disposed;
            internal AutoBackRollableTemporarySettingUnit(long id) => (this.id, disposed) = (id, false);

            /// <inheritdoc/>
            public void Dispose()
            {
                if (disposed)
                    return;
                SettingsState.Close(id);
                disposed = true;
            }
        }
    }
}
