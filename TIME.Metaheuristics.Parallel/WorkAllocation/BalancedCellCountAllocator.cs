using System;
using System.Collections.Generic;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel.WorkAllocation
{
    /// <summary>
    /// Allocates work by sorting the entire list of cells by catchment size, then dividing that list across workers.
    /// This balances cell counts very closely. 
    /// If there is a large variance in catchment sizes, then some workers will have a preponderance of small catchments, 
    /// while others will have a smaller number of large catchments.
    /// If the computational cost of summarising scores per catchment is found to be much larger than the cost of calculating cells,
    /// then a different strategy may be required which attempts to balance the number of catchments, catchment coordinator roles, 
    /// and cell counts.
    /// </summary>
    public class BalancedCellCountAllocator : Allocator
    {
        /// <summary>
        ///   Class logger instance.
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BalancedCellCountAllocator(GlobalDefinition globalDefinition, IIntracommunicatorProxy communicator)
            : base(globalDefinition, communicator)
        {
        }

        protected override WorkPackage[] PerformAllocation(int numProcesses)
        {
            if (numProcesses < 2) 
                throw new ArgumentOutOfRangeException("numProcesses", numProcesses, "At least two processes are required to allocate work");

            // Get the list of cells, sorted by increasing catchment size.
            GlobalDef.SortByAscendingCatchmentSize();
            List<CellDefinition> allCells = GlobalDef.GetFlatCellList();
            GriddedResultCount = allCells.Count;

            int numWorkerProcesses = numProcesses - 1; // the world rank 0 process runs the optimiser and does not calculate cell models
            int numCellsPerProcess = GriddedResultCount / numWorkerProcesses;
            int surplusCells = GriddedResultCount % numWorkerProcesses;
            WorkPackage[] workPackages = new WorkPackage[numProcesses];

            Log.InfoFormat(
                "Root: allocating {0} catchments and {1} cells to {2} processes",
                GlobalDef.Count,
                GriddedResultCount,
                numWorkerProcesses);

            // foreach worker process
            //      grab the quota of cells from the sorted cell list
            //      add the catchments to the set of catchments handled by that process
            IEnumerator<CellDefinition> currentCell = allCells.GetEnumerator();
            currentCell.MoveNext(); // the enumerator starts one element before the first element.
            for (int workerIndex = 1; workerIndex <= numWorkerProcesses; workerIndex++)
            {
                // distribute 1 cell from the surplus to the first workers, since we know that surplus < numWorkers
                int cellQuota = numCellsPerProcess + (workerIndex <= surplusCells ? 1 : 0);
                workPackages[workerIndex] = new WorkPackage(cellQuota);
                int allocatedCellCount = 0;
                while (currentCell.Current != null && allocatedCellCount < cellQuota)
                {
                    // allocate the cell
                    workPackages[workerIndex].Cells[allocatedCellCount++] = currentCell.Current;
                    workPackages[workerIndex].Catchments.Add(GetCatchmentById(currentCell.Current.CatchmentId));

                    // add the workerIndex to the set of ranks handling this catchment
                    if (RanksByCatchment[currentCell.Current.CatchmentId].Count == 0)
                    {
                        // This worker index will be the first rank assigned to the current catchment
                        // therefore, it will become the root process for that catchment, and will be returning 
                        // catchment results. For the GatherFlattened operation to succeed when collecting results,
                        // the receiving process needs to know how many results will be returned from each worker.
                        // the number of results will equal the number of catchments for which that process is 
                        // acting as catchment coordinator.
                        NumCatchmentResultsPerWorker[workerIndex]++;
                    }
                    RanksByCatchment[currentCell.Current.CatchmentId].Add(workerIndex);

                    currentCell.MoveNext();
                }

                NumGriddedResultsPerWorker[workerIndex] = workPackages[workerIndex].Cells.Length;
            }

            return workPackages;
        }
    }
}
