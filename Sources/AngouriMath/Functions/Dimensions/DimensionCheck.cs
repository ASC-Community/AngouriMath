//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

namespace AngouriMath.Core
{
    /// <summary>What a dimensional check settled.</summary>
    public enum DimensionStatus
    {
        /// <summary>Every part of the expression has a dimension and they fit together.</summary>
        Consistent,
        /// <summary>
        /// Some part of the expression is physically meaningless: two different dimensions are
        /// added or compared, or a dimensional quantity is passed to a function such as
        /// <c>sin</c> that only takes a pure number.
        /// </summary>
        Inconsistent,
        /// <summary>
        /// The check could not settle it — a variable has no declared dimension, or a node is
        /// not one the check reads. This is not a verdict that the expression is wrong.
        /// </summary>
        Undetermined,
    }

    /// <summary>
    /// The outcome of <see cref="MathS.Dimensions.Of"/>: a status, the dimension where it is
    /// <see cref="DimensionStatus.Consistent"/>, and where it is not, the smallest subexpression
    /// at fault and why.
    /// </summary>
    /// <param name="Status">What was settled.</param>
    /// <param name="Dimension">
    /// The dimension of the whole expression; <see langword="null"/> unless
    /// <paramref name="Status"/> is <see cref="DimensionStatus.Consistent"/>. A statement — an
    /// equation, an inequality, a conjunction — is <see cref="Core.Dimension.Dimensionless"/>,
    /// being a truth value, once both sides of every comparison in it agree.
    /// </param>
    /// <param name="At">
    /// The subexpression the verdict is about: the first inconsistent or undeterminable one in
    /// evaluation order. <see langword="null"/> when <paramref name="Status"/> is
    /// <see cref="DimensionStatus.Consistent"/>.
    /// </param>
    /// <param name="Reason">Why, in a sentence, naming the dimensions involved.</param>
    public sealed record DimensionCheck(DimensionStatus Status, Dimension? Dimension, Entity? At, string Reason)
    {
        internal static DimensionCheck Ok(Dimension d) => new(DimensionStatus.Consistent, d, null, "");

        internal static DimensionCheck Bad(Entity at, string reason) => new(DimensionStatus.Inconsistent, null, at, reason);

        internal static DimensionCheck Unknown(Entity at, string reason) => new(DimensionStatus.Undetermined, null, at, reason);

        /// <summary>Whether this is <see cref="DimensionStatus.Consistent"/>.</summary>
        public bool IsConsistent => Status == DimensionStatus.Consistent;

        /// <inheritdoc/>
        public override string ToString() => Status switch
        {
            DimensionStatus.Consistent => $"Consistent: {Dimension}",
            _ => $"{Status} at {At}: {Reason}",
        };
    }
}
