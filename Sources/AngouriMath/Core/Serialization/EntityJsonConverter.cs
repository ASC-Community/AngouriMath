//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

#if NET8_0_OR_GREATER

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AngouriMath.Core.Serialization
{
    /// <summary>
    /// Reads and writes an <see cref="Entity"/> as a JSON string holding the expression in
    /// the library's own syntax — what <see cref="Entity.Stringize()"/> prints and
    /// <see cref="MathS.FromString(string)"/> reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every node type carries it already (<c>Core/Serialization/Entity.Serialization.cs</c>), so
    /// an expression is serializable wherever it appears — a property, an array, a dictionary key
    /// — with no configuration. Adding an instance to
    /// <see cref="JsonSerializerOptions.Converters"/> is only needed to place it ahead of another
    /// converter for the same type.
    /// </para>
    /// <para>
    /// There is deliberately no second, structural, format. The printed form is already an exact
    /// serialization and it is the one the library has to keep exact anyway, since it is also the
    /// input format: parsing what <c>Stringize</c> prints gives back the expression printed, and
    /// <c>EveryNodeSurvivesEveryPipelineTest</c> enumerates the node types by reflection and fails
    /// the day a new one stops satisfying that. A structural schema would be a second contract
    /// over the same tree, with its own per-node code to keep in step and its own way of drifting
    /// from the first. What it would buy is speed — reading a 43-node expression costs about
    /// 430 us and 0.8 MB through the parser against 6 us and 25 kB to build the same tree from
    /// constructors — and that is an argument for a faster parser, which every caller who writes
    /// <c>Entity e = "..."</c> would also get, rather than for a second representation that only
    /// serialization uses.
    /// </para>
    /// <para>
    /// What the printed form does not carry, and neither therefore does this: an
    /// <see cref="Entity.Number.Complex"/> with both parts non-zero, which prints as a sum and
    /// reads back as one — the same number, a different node. Nor is a right nesting of an
    /// <em>associative</em> operator kept, since such an operand is not bracketed at its own
    /// precedence: <c>1 + (2 + 3)</c> comes back as <c>(1 + 2) + 3</c>, the same value written the
    /// other way round. Both are properties of printing rather than of this converter, and fixing
    /// them there fixes them here — which is what happened to the third,
    /// <see cref="Entity.Codomain"/>: a node narrowed with
    /// <see cref="Entity.WithCodomain(Domain)"/> used to come back with the default, and prints
    /// as <c>domain(inner, SET)</c> since
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1022">#1022</a>, with no
    /// change here at all.
    /// </para>
    /// <a href="https://github.com/asc-community/AngouriMath/issues/323">#323</a>
    /// </remarks>
    /// <example>
    /// <code>
    /// using System.Text.Json;
    /// using AngouriMath;
    ///
    /// public sealed record Problem(string Title, Entity Body);
    ///
    /// var json = JsonSerializer.Serialize(new Problem("quadratic", "x ^ 2 - 3 * x + 2"));
    /// // {"Title":"quadratic","Body":"x ^ 2 - 3 * x + 2"}
    /// var back = JsonSerializer.Deserialize&lt;Problem&gt;(json);
    /// // back.Body == (Entity)"x ^ 2 - 3 * x + 2"
    /// </code>
    /// </example>
    public sealed class EntityJsonConverter : JsonConverter<Entity>
    {
        /// <summary>
        /// Every node type, not just <see cref="Entity"/> itself, so that a member declared as
        /// <see cref="Entity.Variable"/> or <see cref="Entity.Matrix"/> is served too.
        /// </summary>
        public override bool CanConvert(Type typeToConvert)
            => typeof(Entity).IsAssignableFrom(typeToConvert);

        /// <inheritdoc/>
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Parse(Text(ref reader), typeToConvert);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.StringizeAscii());

        /// <inheritdoc/>
        public override Entity ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Parse(Text(ref reader), typeToConvert);

        /// <inheritdoc/>
        public override void WriteAsPropertyName(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
            => writer.WritePropertyName(value.StringizeAscii());

        private static string Text(ref Utf8JsonReader reader)
            => reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName
                ? reader.GetString()!
                : throw new JsonException(
                    $"An expression is written as a JSON string, and this is {reader.TokenType}.");

        /// <summary>
        /// A malformed expression is reported the way <see cref="MathS.FromString(string)"/>
        /// reports it, rather than wrapped: the parser's message names the position and what it
        /// expected, and that is worth more than uniformity with the other converters.
        /// </summary>
        private static Entity Parse(string text, Type typeToConvert)
        {
            var parsed = MathS.FromString(text);
            // A member declared as a specific node type is a claim about what may be read into
            // it. Checking here says which expression and which type; letting it through gives
            // the caller an InvalidCastException from inside the serializer with neither.
            if (!typeToConvert.IsInstanceOfType(parsed))
                throw new JsonException(
                    $"\"{text}\" is {parsed.GetType().Name}, which is not {typeToConvert.Name}.");
            return parsed;
        }
    }

    /// <summary>
    /// Puts <see cref="EntityJsonConverter"/> on <see cref="Entity"/> without the reflective
    /// construction the plain attribute would use. Trimming and NativeAOT are a constraint here
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>), and
    /// <see cref="JsonConverterAttribute"/> falls back to <c>Activator.CreateInstance</c> only
    /// when <see cref="CreateConverter"/> returns <see langword="null"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class EntityJsonConverterAttribute : JsonConverterAttribute
    {
        public override JsonConverter CreateConverter(Type typeToConvert) => new EntityJsonConverter();
    }
}

#endif
