using System.Collections.Generic;

namespace TIME.Metaheuristics.Parallel.WorkAllocation
{
    public interface IWorkAllocator
    {
        /// <summary>
        /// Gets the number of catchment results per worker, indexed by world communicator rank index.
        /// </summary>
        /// <remarks>
        /// Only valid for the root process. Calling from any other rank will return null.
        /// </remarks>
        int[] NumCatchmentResultsPerWorker { get; }

        /// <summary>
        /// Gets the number of gridded results per worker, indexed by world communicator rank index.
        /// </summary>
        /// <remarks>
        /// Only valid for the root process. Calling from any other rank will return null.
        /// </remarks>
        int[] NumGriddedResultsPerWorker { get; }

        /// <summary>
        /// Gets the work allocation for the current process.
        /// </summary>
        WorkPackage WorkPackage { get; }

        /// <summary>
        /// For each catchment, RanksByCatchment contains the set of rank indicies to which cells have been assigned.
        /// Indexed by catchment CatchmentId.
        /// </summary>
        Dictionary<string, HashSet<int>> RanksByCatchment { get; }

        /// <summary>
        /// Gets the total gridded result count.
        /// </summary>
        int GriddedResultCount { get; }

        /// <summary>
        /// Performs the work allocation, using MPI to communicate the allocation to all processes.
        /// </summary>
        void Allocate();
    }
}