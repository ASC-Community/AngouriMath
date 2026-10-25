//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using AngouriMath.Core.Transformations;

namespace AngouriMath.Core.Operations
{
    /// <summary>
    /// One thing the library does, under the name a caller asks for it by. The terminal's
    /// commands and the MCP server's tools are each made of these, and <see cref="Catalogue"/>
    /// lists every one.
    /// </summary>
    /// <remarks>
    /// Internal until v3, while the shape of an entry may still move. The stable surface is the
    /// methods on <see cref="Entity"/> and <see cref="MathS"/>, and an entry answers exactly
    /// what the method it names answers.
    /// https://github.com/asc-community/AngouriMath/issues/1684
    /// </remarks>
    internal abstract class Operation
    {
        private protected Operation(string name, string description, Parameter[] parameters)
        {
            Name = name;
            Description = description;
            Parameters = parameters;
        }

        /// <summary>The name a caller asks for it by, such as <c>simplify</c>.</summary>
        public string Name { get; }

        /// <summary>What it does, in one sentence.</summary>
        public string Description { get; }

        /// <summary>
        /// What it takes besides its input, in the order its delegate takes them after the input.
        /// </summary>
        public IReadOnlyList<Parameter> Parameters { get; }

        /// <inheritdoc/>
        public override string ToString() => Name;
    }

    /// <summary>One argument an <see cref="Operation"/> takes besides its input.</summary>
    /// <param name="Name">Its name, such as <c>variable</c>.</param>
    /// <param name="Description">What it is, in one sentence.</param>
    internal sealed record Parameter(string Name, string Description);

    /// <summary>
    /// An operation whose answer is an expression that stands in a stated relation to its
    /// input: one of the <see cref="Transformation"/>s, applied as the method on
    /// <see cref="Entity"/> applies it.
    /// </summary>
    /// <typeparam name="TApply">
    /// Its signature: the input expression, then each of <see cref="Operation.Parameters"/>,
    /// and an expression back.
    /// </typeparam>
    internal sealed class TransformationOperation<TApply> : Operation where TApply : Delegate
    {
        internal TransformationOperation(string name, string description,
            TransformationRelation relation, TApply apply, params Parameter[] parameters)
            : base(name, description, parameters)
        {
            Relation = relation;
            Apply = apply;
        }

        /// <summary>What the answer claims about the input.</summary>
        public TransformationRelation Relation { get; }

        /// <summary>Answers.</summary>
        public TApply Apply { get; }
    }

    /// <summary>
    /// An operation whose answer is not an expression standing for the input: a set of
    /// solutions, a number, text, a table.
    /// </summary>
    /// <typeparam name="TAsk">
    /// Its signature: the input, then each of <see cref="Operation.Parameters"/>, and the
    /// answer back.
    /// </typeparam>
    internal sealed class Query<TAsk> : Operation where TAsk : Delegate
    {
        internal Query(string name, string description, TAsk ask, params Parameter[] parameters)
            : base(name, description, parameters)
            => Ask = ask;

        /// <summary>Answers.</summary>
        public TAsk Ask { get; }
    }
}
