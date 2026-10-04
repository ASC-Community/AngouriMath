//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Utils
{
    /// <summary>
    /// Where a run of the unit tests spends its time, by area: the <c>[Trait("Area", …)]</c> on
    /// each test class, summed over the run's results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Run with <c>dotnet run AreaTimes</c> from <c>Sources/Utils/Utils</c>, after
    /// <c>dotnet test Sources/Tests/UnitTests -c Release --logger "trx;LogFileName=results.trx"</c>.
    /// It reads the newest <c>.trx</c> under the test project's <c>TestResults</c>, or the file
    /// <c>AREA_TIMES_TRX</c> names, and the test assembly that file names; it writes a table of
    /// the areas, how many tests each ran and how long they took. Where
    /// <c>GITHUB_STEP_SUMMARY</c> is set the table goes there too, which is how the C# Test job
    /// shows it.
    /// </para>
    /// <para>
    /// The area is read from the test assembly rather than from the results file, which names a
    /// test's class and not its traits. A class with no area is counted as <c>(none)</c>, so the
    /// table adds up to the whole run; <c>AreaTraitTest</c> is what keeps that row empty.
    /// </para>
    /// https://github.com/asc-community/AngouriMath/issues/1183
    /// </remarks>
    public static class AreaTimes
    {
        private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        public static void Do()
        {
            var path = Environment.GetEnvironmentVariable("AREA_TIMES_TRX") ?? Newest();
            var results = XDocument.Load(path);

            var methods = results.Descendants(Trx + "UnitTest")
                .Select(test => (Id: (string?)test.Attribute("id"), Method: test.Element(Trx + "TestMethod")))
                .Where(test => test.Id is not null && test.Method is not null)
                .Select(test => (Id: test.Id!, Method: test.Method!))
                .ToList();
            var codeBase = methods.Select(test => (string?)test.Method.Attribute("codeBase")).FirstOrDefault(p => p is not null)
                ?? throw new InvalidOperationException($"{path} names no test assembly");
            var assembly = Assembly.LoadFrom(codeBase);
            var areaOfClass = new Dictionary<string, string>(StringComparer.Ordinal);
            var areaOfTest = methods.ToDictionary(
                test => test.Id,
                test => AreaOf((string?)test.Method.Attribute("className"), assembly, areaOfClass));

            var totals = new Dictionary<string, (int Tests, TimeSpan Time)>(StringComparer.Ordinal);
            foreach (var result in results.Descendants(Trx + "UnitTestResult"))
            {
                var area = (string?)result.Attribute("testId") is { } id && areaOfTest.TryGetValue(id, out var known) ? known : "(none)";
                var duration = (string?)result.Attribute("duration") is { } text
                    ? TimeSpan.Parse(text, CultureInfo.InvariantCulture)
                    : TimeSpan.Zero;
                totals[area] = totals.TryGetValue(area, out var sum) ? (sum.Tests + 1, sum.Time + duration) : (1, duration);
            }

            var whole = totals.Values.Aggregate(TimeSpan.Zero, (time, row) => time + row.Time);
            var table = new StringBuilder();
            table.AppendLine("| Area | Tests | Seconds | Share |");
            table.AppendLine("|---|--:|--:|--:|");
            foreach (var (area, (tests, time)) in totals.OrderByDescending(row => row.Value.Time).Select(row => (row.Key, row.Value)))
                table.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {area} | {tests} | {time.TotalSeconds:F1} | {(whole.Ticks == 0 ? 0 : 100.0 * time.Ticks / whole.Ticks):F1}% |"));
            table.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| **all** | {totals.Values.Sum(row => row.Tests)} | {whole.TotalSeconds:F1} | 100% |"));

            // xUnit runs test classes in parallel, so the durations add up to more than the run took.
            table.AppendLine();
            table.AppendLine("Seconds are each test's own duration, added up; the classes run in parallel, so the total is more than the run's wall-clock time.");

            Console.WriteLine(table);
            if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summary)
                File.AppendAllText(summary, "### Unit test time by area\n\n" + table + "\n");
        }

        /// <summary>The newest results file under the test project's <c>TestResults</c>.</summary>
        private static string Newest()
        {
            var directory = Path.Combine(Program.GetPathIntoSources(), "Tests", "UnitTests", "TestResults");
            return new DirectoryInfo(directory).EnumerateFiles("*.trx", SearchOption.AllDirectories)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault()
                ?? throw new FileNotFoundException($"no .trx under {directory}; run the tests with --logger trx first");
        }

        /// <summary>
        /// The area a test class declares, read by name so that nothing here depends on xUnit;
        /// the class's base classes are asked too, and <c>(none)</c> where none of them says.
        /// </summary>
        private static string AreaOf(string? className, Assembly assembly, Dictionary<string, string> known)
        {
            if (className is null)
                return "(none)";
            if (known.TryGetValue(className, out var area))
                return area;
            area = "(none)";
            for (var type = assembly.GetType(className); type is not null && type != typeof(object); type = type.BaseType)
            {
                var declared = type.GetCustomAttributesData()
                    .Where(attribute => attribute.AttributeType.FullName == "Xunit.TraitAttribute"
                        && attribute.ConstructorArguments.Count == 2
                        && attribute.ConstructorArguments[0].Value as string == "Area")
                    .Select(attribute => attribute.ConstructorArguments[1].Value as string)
                    .FirstOrDefault(value => value is not null);
                if (declared is not null)
                {
                    area = declared;
                    break;
                }
            }
            known[className] = area;
            return area;
        }
    }
}
