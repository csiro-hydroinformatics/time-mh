using System;
using System.Collections.Generic;
using System.Diagnostics;
using MPI;
using TIME.Metaheuristics.Parallel.Exceptions;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel.WorkAllocation
{
    /// <summary>
    ///  Base class for work allocation strategies
    /// </summary>
    public abstract class Allocator : IWorkAllocator
    {
        /// <summary>
        ///   Class logger instance.
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, HashSet<int>> ranksByCatchment;
        private WorkPackage workPackage;
        private readonly Intracommunicator communicator;
        private int griddedResultCount;
        private Dictionary<string, CatchmentDefinition> catchmentsById;

        protected Allocator(GlobalDefinition globalDefinition)
        {
            if (globalDefinition == null) throw new ArgumentNullException("globalDefinition");

            communicator = Communicator.world;
            GlobalDef = globalDefinition;
        }

        protected void InitStorage(int numProcesses)
        {
            NumCatchmentResultsPerWorker = new int[numProcesses];
            NumGriddedResultsPerWorker = new int[numProcesses];
            RanksByCatchment = new Dictionary<string, HashSet<int>>();
            catchmentsById = new Dictionary<string, CatchmentDefinition>(GlobalDef.Catchments.Count);
            foreach (CatchmentDefinition catchment in GlobalDef)
            {
                RanksByCatchment.Add(catchment.Id, new HashSet<int>());
                catchmentsById.Add(catchment.Id, catchment);
            }
        }

        /// <summary>
        /// Gets the catchment by id.
        /// This is provided as a utility method, since the allocation routine has to retrieve a catchment by Id
        /// once for every cell. Rather than search the catchment list every time, this method retrieves it from a dictionary.
        /// </summary>
        /// <param name="catchmentId">The catchment id.</param>
        /// <returns>The catchment</returns>
        protected CatchmentDefinition GetCatchmentById(string catchmentId)
        {
            return catchmentsById[catchmentId];
        }

        /// <summary>
        /// Gets the global definition.
        /// </summary>
        protected GlobalDefinition GlobalDef { get; private set; }

        #region IWorkAllocator Members

        /// <summary>
        ///   Gets the number of catchment results that will be returned from each worker, indexed by world communicator rank index.
        /// </summary>
        /// <remarks>
        ///   Only valid for the root process. Calling from any other rank will return null.
        /// </remarks>
        public int[] NumCatchmentResultsPerWorker { get; private set; }

        /// <summary>
        ///   Gets the number of gridded results per worker, indexed by world communicator rank index.
        /// </summary>
        /// <remarks>
        ///   Only valid for the root process. Calling from any other rank will return null.
        /// </remarks>
        public int[] NumGriddedResultsPerWorker { get; private set; }

        /// <summary>
        ///   Gets the work allocation for the current process.
        /// </summary>
        public WorkPackage WorkPackage
        {
            get { return workPackage; }
            private set
            {
                workPackage = value;
                Debug.Assert(workPackage != null);
                Log.InfoFormat(
                    "Rank {0}: work allocation contains {1} cells across {2} catchments",
                    communicator.Rank,
                    workPackage.Cells.Length,
                    workPackage.Catchments.Count);
            }
        }

        /// <summary>
        ///   For each catchment, RanksByCatchment contains the set of rank indicies to which cells have been assigned.
        ///   Indexed by catchment CatchmentId.
        /// </summary>
        public Dictionary<string, HashSet<int>> RanksByCatchment
        {
            get { return ranksByCatchment; }
            private set { ranksByCatchment = value; }
        }

        /// <summary>
        /// Gets the total gridded result count.
        /// </summary>
        public int GriddedResultCount
        {
            get
            {
                return griddedResultCount;
            }
            protected set
            {
                if (value == 0)
                    throw new ConfigurationException("The global definition contains no work");
                griddedResultCount = value;
            }
        }

        /// <summary>
        ///   Performs the work allocation, using MPI to communicate the allocation to all processes.
        ///   Uses the Template Method pattern to perform the actual allocation step.
        /// </summary>
        public void Allocate()
        {
            int rank = communicator.Rank;
            InitStorage(communicator.Size);

            if (rank == 0)
            {
                WorkPackage[] workPackages = PerformAllocation(communicator.Size);

                for (int i = 0; i < NumCatchmentResultsPerWorker.Length; i++)
                    Log.DebugFormat("Root: worker {0}: {1} catchment results expected", i, NumCatchmentResultsPerWorker[i]);

                Log.Debug("Root: scattering work allocations to slaves");
                communicator.Scatter(workPackages);

                // todo: will it be more efficient to bundle this in with the scatter?
                Log.Debug("Root: broadcasting ranksByCatchment");
                communicator.Broadcast(ref ranksByCatchment, 0);
            }
            else
            {
                Log.DebugFormat("Rank {0}: getting work allocation", rank);
                WorkPackage = communicator.Scatter<WorkPackage>(0);

                Log.DebugFormat("Rank {0}: getting ranksByCatchment", rank);
                communicator.Broadcast(ref ranksByCatchment, 0);
            }
        }

        #endregion

        /// <summary>
        /// Performs the allocation.
        /// </summary>
        /// <param name="numProcesses">The number of processes.</param>
        /// <returns>The work allocation array, indexed by process.</returns>
        protected abstract WorkPackage[] PerformAllocation(int numProcesses);
    }
}