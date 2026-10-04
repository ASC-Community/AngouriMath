//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;
using AngouriMath;
using static AngouriMath.Entity;

namespace AngouriMath.Mcp;

/// <summary>
/// Matrix work, and symbolic eigenvalues built on top of it.
///
/// AngouriMath's matrices run on GenericTensor (as GenTensor&lt;Entity, ...&gt;), which has no
/// eigen, SVD or decomposition of any kind. That does not matter here: for a symbolic system
/// the right route to eigenvalues is the characteristic polynomial — det(A - lambda*I) — fed
/// to the existing equation solver. Beyond 4x4 there is no closed form anyway (Abel-Ruffini),
/// so a numerical eigensolver would not have been the answer.
/// </summary>
internal static class Matrices
{
    public sealed record Parsed(Matrix? Value, List<string> Warnings, string? Error);

    /// <summary>Read a JSON array of arrays of expression strings into a matrix.</summary>
    public static Parsed Parse(JsonNode? node, string label)
    {
        if (node is not JsonArray rows || rows.Count == 0)
            return new Parsed(null, [], $"'{label}' must be a non-empty array of rows");

        var warnings = new List<string>();
        var parsed = new List<List<Entity>>();
        var width = -1;

        foreach (var row in rows)
        {
            if (row is not JsonArray cells)
                return new Parsed(null, [], $"'{label}' must be an array of ARRAYS (rows)");

            if (width < 0) width = cells.Count;
            else if (cells.Count != width)
                return new Parsed(null, [],
                    $"'{label}' is ragged: a row has {cells.Count} entries, expected {width}");

            var built = new List<Entity>();
            foreach (var cell in cells)
            {
                var text = cell?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(text))
                    return new Parsed(null, [], $"'{label}' contains an empty entry");

                var outcome = Parsing.Parse(text, strict: false);
                if (outcome.Entity is null)
                    return new Parsed(null, [], $"'{label}': could not parse '{text}' — {outcome.Error}");

                built.Add(outcome.Entity);
                warnings.AddRange(outcome.Warnings);
            }
            parsed.Add(built);
        }

        var grid = new Entity[parsed.Count, width];
        for (var i = 0; i < parsed.Count; i++)
            for (var j = 0; j < width; j++)
                grid[i, j] = parsed[i][j];

        return new Parsed(MathS.Matrix(grid), warnings.Distinct().ToList(), null);
    }

    /// <summary>Render a matrix as rows of strings, so the caller can consume it directly.</summary>
    public static JsonArray Render(Matrix m)
    {
        var rows = new JsonArray();
        for (var i = 0; i < m.RowCount; i++)
        {
            var row = new JsonArray();
            for (var j = 0; j < m.ColumnCount; j++) row.Add(m[i, j].Stringize());
            rows.Add(row);
        }
        return rows;
    }

    public sealed record Eigen(
        Entity CharacteristicPolynomial,
        Entity? Roots,
        List<string> DroppedGuards);

    /// <summary>
    /// Standard wording for what stripping a pivot guard means, so every tool that does it
    /// says the same thing.
    /// </summary>
    public const string GuardNote =
        "A routine that divides by pivots leaves a `provided` guard behind for each one. On " +
        "a symbolic matrix these are artefacts of the algorithm rather than mathematics: " +
        "det([[a,b],[c,d]]) is a*d - b*c for ALL a, and [[0,J],[J,0]] has eigenvalues +/-J " +
        "including at J = 0. So they are removed here, and listed rather than silently dropped.";

    /// <summary>
    /// Remove pivot guards from a value, reporting what was removed.
    ///
    /// Order is load-bearing: simplify FIRST, strip second. The guard is what licenses the
    /// cancellation — strip `provided not a = 0` from the raw Gaussian determinant and
    /// Simplify can no longer reduce `a*(d*a - c*b)/a`, because a might be zero. Simplifying
    /// while the guard is still attached yields `a*d - b*c`, and only then is the guard
    /// itself redundant.
    /// </summary>
    public static (Entity Value, List<string> Guards) Clean(Entity e)
    {
        var simplified = e;
        try { simplified = e.Simplify(); }
        catch { /* fall back to the original form */ }

        var guards = Analysis.Conditions(simplified);
        if (guards.Count == 0) return (simplified, guards);

        return (Numeric.Strip(simplified), guards);
    }

    /// <summary>The same, entry by entry, for a matrix.</summary>
    public static (Matrix Value, List<string> Guards) Clean(Matrix m)
    {
        var guards = new List<string>();
        var grid = new Entity[m.RowCount, m.ColumnCount];

        for (var i = 0; i < m.RowCount; i++)
            for (var j = 0; j < m.ColumnCount; j++)
            {
                var (value, found) = Clean(m[i, j]);
                grid[i, j] = value;
                guards.AddRange(found);
            }

        return (MathS.Matrix(grid), guards.Distinct().ToList());
    }

    /// <summary>
    /// Eigenvalues via the characteristic polynomial, which belongs in the library:
    /// https://github.com/asc-community/AngouriMath/issues/1676
    ///
    /// The guard-dropping is not cosmetic. A determinant taken by dividing by pivots leaves a
    /// `provided` guard for each one, and on a symbolic matrix those are artefacts of the
    /// algorithm: they would make the characteristic polynomial of the Pauli X matrix
    /// `lambda^2 - 1 provided not lambda = 0`, and the eigenvalues of [[0,J],[J,0]]
    /// `{J provided not J = 0, -J provided not J = 0}`, where both values are valid at 0 too.
    /// The library's determinant leaves none (`lambda^2 - 1`, `lambda^2 - J^2`); the stripping
    /// stays, and reports what it dropped, for any routine that pivots.
    /// </summary>
    public static Eigen Compute(Matrix a, Variable lambda)
    {
        var n = a.RowCount;
        var shifted = new Entity[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                shifted[i, j] = i == j ? a[i, j] - lambda : a[i, j];

        var raw = MathS.Matrix(shifted).Determinant
                  ?? throw new InvalidOperationException("determinant unavailable");

        var (poly, guards) = Clean(raw);

        Entity? roots;
        try
        {
            // The solver reintroduces pivot guards of its own, so the result is cleaned
            // again — stripping only the characteristic polynomial leaves eigenvalues
            // reading 'J provided not J = 0'.
            var (cleaned, solverGuards) = Clean(poly.SolveEquation(lambda).Simplify());
            roots = cleaned;
            guards.AddRange(solverGuards);
        }
        catch { roots = null; }

        return new Eigen(poly, roots, guards.Distinct().ToList());
    }
}
