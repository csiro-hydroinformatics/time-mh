using System;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    ///   Using integer static readonlyants rather than the idiomatic enumeration because MPI.Net cannot automatically serialise enumerations.
    ///   Using integers removes the need for casts and enum parsing from ints.
    /// </summary>
    internal static class SlaveActions
    {
        public static readonly int Nothing = 0;
        public static readonly int DoWork = 1;
        public static readonly int Terminate = 2;

        public static readonly string[] ActionNames =
            {
                "Nothing",
                "Doing work",
                "Shutting down",
            };
    }
}