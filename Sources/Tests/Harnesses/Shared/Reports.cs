//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.IO;

namespace Harness
{
    /// <summary>Where a harness writes its report.</summary>
    static class Reports
    {
        /// <summary>
        /// The directory named by <c>HARNESS_REPORTS</c> when it is set -- CI points it at the
        /// directory it uploads -- and the Harnesses folder otherwise, resolved from the
        /// assembly rather than the working directory, so that two runs started from different
        /// places cannot leave two reports of different ages.
        /// </summary>
        internal static string PathFor(string file)
        {
            var directory = Environment.GetEnvironmentVariable("HARNESS_REPORTS");
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetParent(AppContext.BaseDirectory).Parent.Parent.Parent.Parent.FullName;
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, file);
        }
    }
}
