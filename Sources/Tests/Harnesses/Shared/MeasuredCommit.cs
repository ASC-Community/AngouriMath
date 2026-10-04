//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

// Shared by every harness that writes a report, so that each report names the commit it
// measured. A report describes a *build*, and without its commit a stale report and a fresh
// one cannot be told apart.

using System;
using System.IO;
using System.Linq;

namespace Harness
{
    static class Measured
    {
        /// <summary>
        /// The commit of the library this was measured against.
        /// </summary>
        /// <remarks>
        /// Read from the <c>ProjectReference</c> in this harness's own project file, not from the
        /// loaded assembly: the build system copies AngouriMath.dll into the harness's output
        /// directory, which is not in a repository, so the assembly's location says nothing about
        /// which checkout it came from. A report generated from a branch and read later as the
        /// library's behaviour has cost half a day here before, so this is derived rather than
        /// written down.
        /// </remarks>
        internal static string Commit()
        {
            try
            {
                // The harness's own project directory, resolved from the assembly rather than
                // from the working directory, which need not hold a .csproj at all.
                var projectDirectory = Directory
                    .GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.FullName;
                var project = Directory.GetFiles(projectDirectory, "*.csproj").FirstOrDefault();
                if (project is null)
                    return "unknown build";
                var reference = File.ReadAllLines(project)
                    .Select(line => line.Trim())
                    .FirstOrDefault(line => line.StartsWith("<ProjectReference", StringComparison.Ordinal));
                if (reference is null)
                    return "unknown build";
                var quoted = reference.Split('"');
                if (quoted.Length < 2)
                    return "unknown build";
                // A ProjectReference is relative to the project file that declares it, never to
                // the working directory.
                return At(Path.GetFullPath(Path.Combine(
                    projectDirectory, quoted[1].Replace('\\', Path.DirectorySeparatorChar))));
            }
            catch { return "unknown build"; }
        }

        /// <summary>
        /// The commit of the repository that contains <paramref name="path"/>, for a harness
        /// that builds the library itself rather than referencing it.
        /// </summary>
        internal static string Commit(string path) => At(path);

        /// <summary>
        /// The commit of the repository that contains <paramref name="path"/>.
        /// </summary>
        /// <remarks>
        /// A worktree's <c>.git</c> is a *file* pointing at the real one, so the walk upwards accepts
        /// either. The decoration names HEAD, remote-tracking branches and tags only: worktrees share
        /// their local branches, and another checkout's branch at the same commit is not this build.
        /// </remarks>
        private static string At(string path)
        {
            var at = Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path));
            while (at is not null && !Directory.Exists(Path.Combine(at, ".git"))
                                  && !File.Exists(Path.Combine(at, ".git")))
                at = Path.GetDirectoryName(at);
            if (at is null)
                return "unknown build";
            var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "git", $"-C \"{at}\" log -1 --format=%h%d --decorate-refs=refs/remotes --decorate-refs=refs/tags --decorate-refs=HEAD") { RedirectStandardOutput = true });
            var line = git.StandardOutput.ReadToEnd().Trim();
            git.WaitForExit();
            return line.Length == 0 ? "unknown build" : line;
        }
    }
}
