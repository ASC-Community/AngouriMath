//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath.Core
{
    /// <summary>
    /// Source code printed from expressions by <see cref="MathS.Shaders"/>, or the reason it was not.
    /// </summary>
    /// <param name="Code">The statements; <see langword="null"/> where printing was declined.</param>
    /// <param name="At">The subexpression that could not be printed faithfully; <see langword="null"/> on success.</param>
    /// <param name="Reason">Why it could not, in a sentence; empty on success.</param>
    public sealed record GeneratedSource(string? Code, Entity? At, string Reason)
    {
        /// <summary>Whether <see cref="Code"/> holds the source.</summary>
        public bool Succeeded => Code is not null;

        /// <inheritdoc/>
        public override string ToString() => Code ?? $"Declined at {At}: {Reason}";
    }
}
