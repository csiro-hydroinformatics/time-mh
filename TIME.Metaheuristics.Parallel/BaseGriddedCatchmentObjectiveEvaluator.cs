using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Metaheuristics.Parallel.Exceptions;
using TIME.Metaheuristics.Parallel.Execution;
using TIME.Metaheuristics.Parallel.ExtensionMethods;
using TIME.Metaheuristics.Parallel.Objectives;
using TIME.Metaheuristics.Parallel.WorkAllocation;
//using MPI;
using TIME.DataTypes;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    /// A base class for the evaluation of the goodness of fit of distributed and/or ensemble of models. 
    /// The main purpose of this class is to abstract away the reliance on MPI or serial, single process use, for 
    /// debugging purposes. Some issues are easier to diagnose without MPI involved.
    /// </summary>
    public abstract class BaseGriddedCatchmentObjectiveEvaluator : IEnsembleObjectiveEvaluator<MpiSysConfig>, IDisposable
    {
        /// <summary>
        ///   Class logger instance.
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private MpiObjectiveScores[] catchmentScores;
        private bool disposed = false;
        private WorkPackage myWork;

        /// <summary>
        ///   Contains the catchment communicators used to collate results for a catchment. Indexed by catchment CatchmentId
        /// </summary>
        private readonly Dictionary<string, IIntracommunicatorProxy> communicatorsByCatchmentId =
            new Dictionary<string, IIntracommunicatorProxy>();

        protected int rank;
        protected int size;


        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGriddedCatchmentObjectiveEvaluator"/> class.
        /// </summary>
        /// <param name="globalDefinitionFileInfo">The global definition file info.</param>
        /// <param name="objectivesDefinitionFileInfo">The objectives definition file info.</param>
        /// <param name="rank">The rank of the process in the 'world'</param>
        /// <param name="size"></param>
        /// <param name="worldCommunicator"></param>
        protected BaseGriddedCatchmentObjectiveEvaluator(FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo, int rank, int size, IIntracommunicatorProxy worldCommunicator)
        {
            this.rank = rank;
            this.size = size;
            WorldRank = GetWorldRank();
            IsFirstRun = true;

            if (IsMaster && GetWorldSize() < 2)
                throw new ConfigurationException("At least 2 MPI processes are required to run this application.");

            ObjectiveDefinitionFileName = objectivesDefinitionFileInfo.FullName;

            // todo: should this be performed once only on root, and then broadcast? Wait and see what data the actual model needs to load.
            Log.DebugFormat("Rank {0}: Loading global definition", WorldRank);
            GlobalDefinition = SerializationHelper.XmlDeserialize<GlobalDefinition>(globalDefinitionFileInfo);
            Log.DebugFormat("Rank {0}: global definition complete", WorldRank);
            AllocateWork(new BalancedCellCountAllocator(GlobalDefinition, worldCommunicator));
        }

        /// <summary>
        /// Gets the rank of this evaluator in the ensemble of evaluators
        /// </summary>
        protected int GetWorldRank() { return rank; }

        /// <summary>
        /// Gets the size of the ensemble of evaluators
        /// </summary>
        protected int GetWorldSize() { return size; }

        public IObjectiveScores<MpiSysConfig>[] EvaluateScore(MpiSysConfig systemConfiguration)
        {
            this.Execute(systemConfiguration);
            var c = this.CatchmentScores;
            var result = new IObjectiveScores<MpiSysConfig>[c.Length];
            c.CopyTo(result, 0);
            return result;
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"> <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources. </param>
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }

                // dispose unmanaged resources here

                // disposal is done. Set the flag so we don't get disposed more than once.
                disposed = true;
            }
        }

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this is the first simulation run or not.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is first run; otherwise, <c>false</c>.
        /// </value>
        private bool IsFirstRun { get; set; }

        /// <summary>
        /// Gets or sets the catchment coordinator count. This is the number of catchments being coordinated by the current process.
        /// </summary>
        /// <value>
        /// The catchment coordinator count.
        /// </value>
        private int CatchmentCoordinatorCount { get; set; }

        /// <summary>
        /// Gets or sets the number of catchment results per worker.
        /// Only valid on the Master process.
        /// </summary>
        /// <value>
        /// The num catchment results per worker.
        /// </value>
        private int[] NumCatchmentResultsPerWorker { get; set; }

        /// <summary>
        /// Gets or sets the number of gridded results per worker.
        /// Only valid on the Master process.
        /// </summary>
        /// <value>
        /// The num gridded results per worker.
        /// </value>
        private int[] NumGriddedResultsPerWorker { get; set; }

        private GlobalDefinition GlobalDefinition { get; set; }

        private string ObjectiveDefinitionFileName { get; set; }

        // caching this so I don't have to recalculate it all the time
        public int TotalCellCount { get; set; }

        public bool IsMaster
        {
            get { return WorldRank == 0; }
        }

        public bool IsSlave
        {
            get { return !IsMaster; }
        }

        /// <summary>
        ///   Gets the catchment scores.
        ///   Only valid on the master process. Slave processes will return null.
        /// </summary>
        public MpiObjectiveScores[] CatchmentScores
        {
            get
            {
                Debug.Assert(IsMaster);
                return catchmentScores;
            }
        }

        /// <summary>
        ///   For slave ranks, this contains metadata about the work allocated to the slave
        /// </summary>
        private WorkPackage MyWork
        {
            get { return myWork; }
            set
            {
                myWork = value;

                // Create the models
                if (myWork != null && myWork.Cells.Length > 0)
                {
                    Log.DebugFormat("Rank {0}: creating {1} model evaluators", WorldRank, myWork.Cells.Length);
                    Models = new ICatchmentCellModelRunner[MyWork.Cells.Length];
                    for (int i = 0; i < MyWork.Cells.Length; i++)
                    {
                        CellDefinition cellDefinition = MyWork.Cells[i];
                        Log.DebugFormat("Rank {0}: model instance {1}, catchment {2}, cell {3}", WorldRank, i, cellDefinition.CatchmentId, cellDefinition.Id);
                        var inputs = (TIME.Tools.Metaheuristics.Persistence.Gridded.ModelInputsDefinition)cellDefinition.ModelRunDefinition.Inputs;
                        if (inputs != null && !File.Exists(inputs.NetCdfDataFilename))
                        {
                            string msg = String.Format(
                                "Rank {0}: Input netcdf file '{1}' not found. Catchment: {2}, cell {3}",
				WorldRank,
				inputs.NetCdfDataFilename,
				cellDefinition.CatchmentId,
				cellDefinition.Id);
                            throw new ConfigurationException(msg);
                        }
#if USE_TOY_MODEL
                        Models[i] = new GriddedCatchmentToyModel(MyWork.Cells[i]);
#else
                        Models[i] = GridModelHelper.CreateCellEvaluator(cellDefinition);
#endif
                    }
                    Log.DebugFormat("Rank {0}: models created", WorldRank);
                }
            }
        }

        private ICatchmentCellModelRunner[] Models { get; set; }

        protected int WorldRank { get; set; }

        /// <summary>
        ///   Gets or sets the iteration number.
        /// </summary>
        /// <remarks>
        ///   Only valid on the Master(root) process
        /// </remarks>
        /// <value> The iteration. </value>
        public int Iterations { get; private set; }

#if !CELL_WEIGHTED_SUMS
        /// <summary>
        /// Each entry is an int array containing the number of 
        /// results for that catchment, indexed by catchment communicator rank.
        /// </summary>
        /// <value>
        /// The catchment results per catchment communicator rank.
        /// </value>
        private Dictionary<string, int[]> CatchmentResultCountPerCatchmentCommunicator { get; set; }
#endif

        private Dictionary<string, CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig>> CatchmentStatisticsEvaluatorCache { get; set; }

        #endregion

        #region Common methods

        /// <summary>
        /// Allocates work and distributes the work packages to all workers.
        /// </summary>
        /// <param name="workAllocator">The work allocator. Different allocation strategies can be selected through this argument.</param>
        private void AllocateWork(IWorkAllocator workAllocator)
        {
            if (workAllocator == null) throw new ArgumentNullException("workAllocator");

            Log.DebugFormat("Rank {0}: Allocating work", WorldRank);
            workAllocator.Allocate();

            DebugDumpWorkAllocator(workAllocator);

            // create the MPI communicators used to coordinate processes involved in each catchment
            CreateCommunicators(workAllocator);

            // we only need to preserve some data from the work allocator
            MyWork = workAllocator.WorkPackage;
            NumCatchmentResultsPerWorker = workAllocator.NumCatchmentResultsPerWorker;
            NumGriddedResultsPerWorker = workAllocator.NumGriddedResultsPerWorker;
            TotalCellCount = workAllocator.GriddedResultCount;

            // check that we have enough work for the number of workers
            if (IsMaster && TotalCellCount < GetWorldSize() - 1)
                throw new ConfigurationException(
                    String.Format(
                        "The number of worker processes cannot be greater than the number of cells. Currently: {0} workers, {1} cells",
                        GetWorldSize() - 1,
                        TotalCellCount));

#if !CELL_WEIGHTED_SUMS
            CalculateCoordinatorCount();
#endif
        }

        private void DebugDumpWorkAllocator(IWorkAllocator workAllocator)
        {
#if DEBUG_MODELS
            Log.DebugFormat("*Rank {0}: gridded result count = {1}", WorldRank, workAllocator.GriddedResultCount);
            for (int i = 0; i < GetWorldSize(); i++)
            {
                Log.DebugFormat("*Rank {0} sees {2} catchment results for rank {1}", WorldRank, i, workAllocator.NumCatchmentResultsPerWorker[i]);
                Log.DebugFormat("*Rank {0} sees {2} gridded results for rank {1}", WorldRank, i, workAllocator.NumGriddedResultsPerWorker[i]);
            }

            foreach (CatchmentDefinition catchment in GlobalDefinition)
            {
                HashSet<int> ranks = workAllocator.RanksByCatchment[catchment.Id];
                string msg = String.Format("*Rank {0}: Catchment {1} has {2} ranks: ", WorldRank, catchment.Id, ranks.Count);
                foreach (int rank in ranks)
                    msg += String.Format("{0}, ", rank);
                Log.DebugFormat(msg);

            }
#endif
        }

#if !CELL_WEIGHTED_SUMS
        /// <summary>
        /// Calculates the catchment result counts for each catchment coordinator.
        /// This is required so that the coordinator knows how many results to expect from
        /// each process in the GatherFlattened call that accumulates the gridded results.
        /// </summary>
        private void CalculateCatchmentResultCounts()
        {
            if (IsSlave)
            {
                Log.DebugFormat("Rank {0}: Calculating catchment result counts", WorldRank);

                // Count how many cells I have for each catchment
                Dictionary<string, int> myResultsPerCatchment = new Dictionary<string, int>(MyWork.Catchments.Count);
                foreach (CellDefinition cell in MyWork.Cells)
                {
                    if (myResultsPerCatchment.ContainsKey(cell.CatchmentId))
                        myResultsPerCatchment[cell.CatchmentId]++;
                    else
                        myResultsPerCatchment.Add(cell.CatchmentId, 1);
                }

                // Gather all the individual result counts into the coordinators
                foreach (CatchmentDefinition catchment in MyWork.Catchments)
                {
                    Intracommunicator comm = communicatorsByCatchmentId[catchment.Id];
                    int[] resultsPerRank = comm.Gather(myResultsPerCatchment[catchment.Id], 0);
                    if (comm.Rank == 0)
                    {
                        // Count how many catchments this process is coordinating
                        CatchmentCoordinatorCount++;

                        // Lazy creation of the dictionary. We don't know if the current 
                        // process needs one of these or not until this point
                        if (CatchmentResultCountPerCatchmentCommunicator == null)
                            CatchmentResultCountPerCatchmentCommunicator = new Dictionary<string, int[]>(comm.Size);

                        // Store the result counts
                        CatchmentResultCountPerCatchmentCommunicator.Add(catchment.Id, resultsPerRank);
                    }
                }
            }
        }
#endif
        /// <summary>
        /// Creates the catchment communicators.
        /// </summary>
        /// <param name="workAllocator">The work allocator.</param>
        private void CreateCommunicators(IWorkAllocator workAllocator)
        {
            // create the communicators (creating communicators is a collective operation, all processes that belong to the new communicator
            // must participate in the call).
            // It is also important that all processes create the communicators in the same order to avoid deadlock
            // There is one communicator per catchment
            Log.DebugFormat("Rank {0}: Creating communicators", WorldRank);

            // we need to sort the list of catchments by catchment CatchmentId. 
            // All processes must have the same catchment order or they can deadlock when creating communicators that span processes.
            GlobalDefinition.SortCatchmentsById();

            foreach (CatchmentDefinition catchment in GlobalDefinition)
            {
                Log.DebugFormat("Rank {0}: catchment {1} Creating communicator for {2} processes", WorldRank, catchment.Id, workAllocator.RanksByCatchment[catchment.Id].Count);
                IGroupProxy catchmentGroup = CreateGroup(workAllocator.RanksByCatchment[catchment.Id].ToArray());
                Log.DebugFormat("Rank {0}: Catchment group created, size = {1}", WorldRank, catchmentGroup.Size);
                IIntracommunicatorProxy catchmentCommunicator = CreateIntracommunicatorProxy(catchmentGroup);
                if (catchmentCommunicator != null)
                    Log.DebugFormat("Rank {0}: Communicator created, rank = {1} size = {2}", WorldRank, catchmentCommunicator.GetRank(this.WorldRank), catchmentCommunicator.Size);
                else
                    Log.DebugFormat("Rank {0}: Communicator created, I am not a member", WorldRank);

                // catchmentCommunicator will be null if the current rank is not a member of the catchmentGroup.
                // This is OK, as each rank only requires the communicators for catchments it is involved in.
                if (catchmentCommunicator != null)
                {
                    Debug.Assert(workAllocator.RanksByCatchment[catchment.Id].Contains(WorldRank));
                    communicatorsByCatchmentId.Add(catchment.Id, catchmentCommunicator);

#if CELL_WEIGHTED_SUMS
                    // If I am the catchment coordinator for at least one catchment, then I will need the dictionary of cached
                    // catchment statistics evaluators
                    if (catchmentCommunicator.GetRank(this.WorldRank) == 0)
                    {
                        Log.DebugFormat("Rank {0}: I am catchment coordinator. Creating stats evaluator cache.", WorldRank);

                        // count how often this process acts as catchment coordinator
                        CatchmentCoordinatorCount++;

                        // create the statistics evaluator cache
                        if (CatchmentStatisticsEvaluatorCache == null)
                            CatchmentStatisticsEvaluatorCache = new Dictionary<string, CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig>>();
                    }
#endif
                }
                else
                {
                    Debug.Assert(!workAllocator.RanksByCatchment[catchment.Id].Contains(WorldRank));
                }
            }
        }

        internal abstract IIntracommunicatorProxy CreateIntracommunicatorProxy(IGroupProxy catchmentGroup);

        internal abstract IGroupProxy CreateGroup(int[] p);

        #endregion

        #region Master methods

        /// <summary>
        ///   Entry point for executing a time series model.
        ///   Must only be called on the master process.
        /// </summary>
        /// <param name="parameters"> the parameter set for the model run </param>
        public void Execute(MpiSysConfig parameters)
        {
            if (IsSlave)
                throw new InvalidOperationException("This method can only be called on the root process");

            Log.DebugFormat("Root: ***** commencing run {0} *****", Iterations);

            // Tell the slaves to expect some work
            MpiWorkPacket workPacket = new MpiWorkPacket(SlaveActions.DoWork, parameters);

            // Gather the catchment results (one result set per catchment)
            Log.Debug("Root: asking slaves to run and waiting for catchment results");
            WorldBroadcast(ref workPacket, 0);
            catchmentScores = WorldGatherFlattened(new MpiObjectiveScores[0], NumCatchmentResultsPerWorker, 0);
            Debug.Assert(catchmentScores.Length == GlobalDefinition.Count);
            Log.DebugFormat("Root: {0} catchment results are in", catchmentScores.Length);

            // debug info about the ordering of catchments that can be expected in the final results
            if (Log.IsInfoEnabled && IsFirstRun)
            {
                IsFirstRun = false;
                StringBuilder sb = new StringBuilder();
                sb.Append("Catchment orders: ");
                foreach (var catchmentScore in catchmentScores)
                    sb.AppendFormat("{0}, ", catchmentScore.CatchmentId);
                Log.Info(sb);
            }

            //foreach (var catchmentScore in catchmentScores)
            //    Log.Debug(catchmentScore);

            // gather the gridded results (one per grid cell)
            // todo: gridded results that are not summarised per catchment.
            /*
            Log.Debug("Root: waiting for gridded results");
            MpiObjectiveScores[] griddedResults = Communicator.world.GatherFlattened(new MpiObjectiveScores[0], NumGriddedResultsPerWorker, 0);
            Debug.Assert(griddedResults.Length == TotalCellCount);
            Log.DebugFormat("Root: ***** completed run {0} *****", Iterations);
            */
#if BARRIER_AT_ITERATION_END
            Communicator.world.Barrier();
#endif
            Iterations++;
        }

        internal abstract MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int[] counts, int root);

        internal abstract MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int root);

        internal abstract void WorldBroadcast(ref MpiWorkPacket workPacket, int root);

        #endregion

        #region Slave methods

        /// <summary>
        ///   Entry point for running a slave process.
        /// </summary>
        public abstract void RunSlave();

        /// <summary>
        ///   Slave process worker method.
        /// </summary>
        /// <param name="parameters"> Input parameters for the work unit </param>
        protected void DoWork(MpiSysConfig parameters)
        {
            Debug.Assert(Models != null);

            // execute our list of models, accumulating the results into the appropriate partial result buffer.
#if CELL_WEIGHTED_SUMS
            Dictionary<string, SerializableDictionary<string, MpiTimeSeries>> partialCatchmentResultsByCatchmentId = EvaluateModels(parameters);
#else
            Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>> partialCatchmentResultsByCatchmentId = EvaluateModels(parameters);
#endif
            // For each catchment, accumulate the partial results for each catchment back to the catchment-coordinator.
            MpiObjectiveScores[] finalCatchmentResults = AccumulateCatchmentResultsInCatchmentCoordinator(
                partialCatchmentResultsByCatchmentId, parameters);

            // OK, all catchment results have been accumulated. Time to send the catchment results back to world root.
            Log.DebugFormat("Rank {0}: submitting {1} final catchment results to master", WorldRank, finalCatchmentResults.Length);

            WorldGatherFlattened(finalCatchmentResults, 0);

            // Send the gridded results back from our cell calculations.
            // todo: gridded results that are not summarised per catchment.
            /*
            MpiObjectiveScores[] myGriddedResults = new MpiObjectiveScores[MyWork.Cells.Length];
            Log.DebugFormat("Rank {0}: submitting {1} final gridded results to master", WorldRank, myGriddedResults.Length);
            Communicator.world.GatherFlattened(myGriddedResults, 0);
            */
        }

#if CELL_WEIGHTED_SUMS
        /// <summary>
        /// Evaluates the models.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A dictionary of results, indexed by catchment Id, for the cells being calculated on the current process. 
        /// Each dictionary value contains the partial weighted sum of cell results for that catchment
        /// </returns>
        protected Dictionary<string, SerializableDictionary<string, MpiTimeSeries>> EvaluateModels(MpiSysConfig parameters)
        {

            OnBeforeModelRuns();

            // For each catchment we may have 1 or more cells, hence the List<MpiObjectiveScores> to store the variable list of scores for each cell
            // from a given catchment.
            Dictionary<string, SerializableDictionary<string, MpiTimeSeries>> partialCatchmentResults =
                new Dictionary<string, SerializableDictionary<string, MpiTimeSeries>>(MyWork.Catchments.Count);

            foreach (var model in Models)
            {
                SerializableDictionary<string, MpiTimeSeries> result = model.Execute(parameters);
                SerializableDictionary<string, MpiTimeSeries> existingResults;
                if (partialCatchmentResults.TryGetValue(model.CatchmentId, out existingResults))
                {
                    // add each weighted time series to the partial results
                    foreach (KeyValuePair<string, MpiTimeSeries> resultPair in result)
                        existingResults[resultPair.Key].InplaceAdd(resultPair.Value);
                }
                else
                {
                    // no partial results yet, use the new result as the partial result buffer.
                    partialCatchmentResults.Add(model.CatchmentId, result);
                }
            }

            return partialCatchmentResults;
        }

        public event EventHandler BeforeModelRuns;

        private void OnBeforeModelRuns()
        {
            if (BeforeModelRuns != null) BeforeModelRuns(this, null);
        }

        /// <summary>
        ///   Accumulates the catchment results from all processes involved in calculating that catchment.
        ///   The root process in each catchment communicator will contain the accumulated result for that catchment.
        /// </summary>
        /// <param name="partialCatchmentResults"> The partial catchment results from the current process. </param>
        /// <param name="sysConfig"> The sys config. </param>
        /// <returns> The array of final catchment results for the current process. There will be one element for each catchment
        ///  for which the current process is the catchment coordinator. 
        /// </returns>
        protected MpiObjectiveScores[] AccumulateCatchmentResultsInCatchmentCoordinator(
            Dictionary<string, SerializableDictionary<string, MpiTimeSeries>> partialCatchmentResults, MpiSysConfig sysConfig)
        {
            MpiObjectiveScores[] finalCatchmentResults = new MpiObjectiveScores[CatchmentCoordinatorCount];
            int finalResultIndex = 0;
            foreach (CatchmentDefinition catchment in MyWork.Catchments)
            {
                // Note that Gather is blocking but not synchronous. This means that I can't cause deadlock by having processes
                // call Gather on the communicators in different sequences. My local call to gather will return even if the other 
                // participants in the gather have not joined in yet.
                var catchmentComm = communicatorsByCatchmentId[catchment.Id];

                var groupRank = catchmentComm.GetRank(this.WorldRank);
                if (groupRank == 0) //Catchment coordinator
                {
                    SerializableDictionary<string, MpiTimeSeries>[] completeCatchmentResults = catchmentComm.Gather(partialCatchmentResults[catchment.Id], 0, groupRank);
                    Log.DebugFormat("Rank {0}: Catchment '{1}': Accumulating {2} results from {3} processes", WorldRank, catchment.Id, completeCatchmentResults.Length, catchmentComm.Size);
                    Debug.Assert(completeCatchmentResults.Length == catchmentComm.Size); // we expect one result from each process

                    // completeCatchmentResults contains the weighted summed scores from every process involved in calculating this catchment
                    // We now need to condense the array of scores to a single weighted sum, using the first partial result as the destination buffer
                    SerializableDictionary<string, MpiTimeSeries> finalResult = null;
                    foreach (SerializableDictionary<string, MpiTimeSeries> partialResult in completeCatchmentResults)
                    {
                        if (finalResult == null)
                            finalResult = partialResult;
                        else
                            foreach (var partialResultPairs in partialResult)
                                finalResult[partialResultPairs.Key].InplaceAdd(partialResultPairs.Value);
                    }

                    MpiObjectiveScores finalCatchmentResult = CalculateCatchmentScores(catchment, finalResult, sysConfig);
                    finalCatchmentResults[finalResultIndex] = finalCatchmentResult;
                    finalResultIndex++;
                }
                else
                {
                    Log.DebugFormat("Rank {0}: Catchment '{1}' rank {2}: Sending {3} cell results", WorldRank, catchment.Id, catchmentComm.GetRank(this.WorldRank), partialCatchmentResults[catchment.Id].Count);
                    catchmentComm.Gather(partialCatchmentResults[catchment.Id], 0, groupRank);
                }
            }

            return finalCatchmentResults;
        }

        private MpiObjectiveScores CalculateCatchmentScores(CatchmentDefinition catchment, SerializableDictionary<string, MpiTimeSeries> catchmentTimeSeries, MpiSysConfig sysConfig)
        {
            //DateTime start = DateTime.Now;
            //Log.InfoFormat("CalcCat Elapsed 1: {0}", (DateTime.Now - start).TotalMilliseconds); start = DateTime.Now;

            // convert back to the Time.Data.TimeSeries objects for use by the statistics objects.
            SerializableDictionary<string, TimeSeries> convertedCatchmentTimeSeries = new SerializableDictionary<string, TimeSeries>();
            foreach (KeyValuePair<string, MpiTimeSeries> keyValuePair in catchmentTimeSeries)
            {
                MpiTimeSeries value = keyValuePair.Value;
                convertedCatchmentTimeSeries.Add(keyValuePair.Key, new TimeSeries(value.Start, value.TimeStep, value.TimeSeries));
            }
            //Log.InfoFormat("CalcCat Elapsed 2: {0}", (DateTime.Now - start).TotalMilliseconds); start = DateTime.Now;

            // make the pre-calculated time series look like a point time series model so it can be used by the statistics evaluator
            PointTimeSeriesSimulationDictionaryAdapter catchmentTimeSeriesAdapter = new PointTimeSeriesSimulationDictionaryAdapter(convertedCatchmentTimeSeries);

            catchmentTimeSeriesAdapter.SetPeriod(catchment.Cells[0].ModelRunDefinition.StartDate, catchment.Cells[0].ModelRunDefinition.EndDate);

            //Log.InfoFormat("CalcCat Elapsed 3: {0}", (DateTime.Now - start).TotalMilliseconds); start = DateTime.Now;
            // retrieve the statistics evaluator for this catchment
            Log.DebugFormat("Rank {0}: Catchment '{1}' creating score evaluator", WorldRank, catchment.Id);
            var catchmentScoreEvaluator = GetCatchmentStatisticsEvaluator(catchment, catchmentTimeSeriesAdapter);

            //Log.InfoFormat("CalcCat Elapsed 4: {0}", (DateTime.Now - start).TotalMilliseconds); start = DateTime.Now;

            catchmentScoreEvaluator.SetModelRunner(catchmentTimeSeriesAdapter);
            Log.DebugFormat("Rank {0}: Catchment '{1}' evaluating score", WorldRank, catchment.Id);
            MpiObjectiveScores calculateCatchmentScores = new MpiObjectiveScores(catchmentScoreEvaluator.EvaluateScore(catchmentTimeSeriesAdapter, sysConfig), catchment.Id);
            //foreach (var s in calculateCatchmentScores.scores)
            //{
            //    if (s.value.Equals(double.NaN))
            //    {
            //        Log.InfoFormat("Score {0},{1} bacame NaN for catchment {2}", s.name, s.text, catchment.Id);
            //        Log.InfoFormat("Configuration: {0}", calculateCatchmentScores.SystemConfiguration);
            //    }
            //}

            //Log.InfoFormat("CalcCat Elapsed 5: {0}", (DateTime.Now - start).TotalMilliseconds); start = DateTime.Now;
            return calculateCatchmentScores;
        }

        private CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig> GetCatchmentStatisticsEvaluator(
            CatchmentDefinition catchment,
            PointTimeSeriesSimulationDictionaryAdapter catchmentTimeSeriesAdapter)
        {
            CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig> catchmentScoreEvaluator;
            CatchmentStatisticsEvaluatorCache.TryGetValue(catchment.Id, out catchmentScoreEvaluator);
            if (catchmentScoreEvaluator == null)
            {
                LumpedCatchmentObjectivesDefinition objDef =
                    SerializationHelper.XmlDeserialize<LumpedCatchmentObjectivesDefinition>(new FileInfo(ObjectiveDefinitionFileName));
                objDef.CatchmentIdentifier = catchment.Id;
                catchmentScoreEvaluator = new CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig>(
                    catchmentTimeSeriesAdapter,
                    objDef.BuildAnalysis(),
                    objDef.GetMaximizable(),
                    objDef.GetObjectiveNames());

                CatchmentStatisticsEvaluatorCache.Add(catchment.Id, catchmentScoreEvaluator);
            }

            return catchmentScoreEvaluator;
        }
#else
        /// <summary>
        /// Evaluates the models.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// A dictionary of results, indexed by catchment Id, for the cells being calculated on the current process. 
        /// Each list in the dictionary contains the time series dictionaries for every cell belonging to the catchment.
        /// </returns>
        private Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>> EvaluateModels(MpiSysConfig parameters)
        {
            // For each catchment we may have 1 or more cells, hence the List<MpiObjectiveScores> to store the variable list of scores for each cell
            // from a given catchment.
            Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>> partialCatchmentResults = new Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>>(MyWork.Catchments.Count);
            foreach (CatchmentDefinition catchment in MyWork.Catchments)
                partialCatchmentResults.InplaceAdd(catchment.Id, new List<SerializableDictionary<string, MpiTimeSeries>>());

            foreach (var model in Models)
                partialCatchmentResults[model.CatchmentId].InplaceAdd(model.Execute(parameters));

            return partialCatchmentResults;
        }

        /// <summary>
        ///   Accumulates the catchment results from all processes involved in calculating that catchment.
        ///   The root process in each catchment communicator will contain the accumulated result for that catchment.
        /// </summary>
        /// <param name="partialCatchmentResults"> The partial catchment results from the current process. </param>
        /// <param name="sysConfig"> The sys config. </param>
        /// <returns> The array of final catchment results for the current process. There will be one element for each catchment
        ///  for which the current process is the catchment coordinator. 
        /// </returns>
        private MpiObjectiveScores[] AccumulateCatchmentResultsInCatchmentCoordinator(
            Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>> partialCatchmentResults, MpiSysConfig sysConfig)
        {
            MpiObjectiveScores[] finalCatchmentResults = new MpiObjectiveScores[CatchmentCoordinatorCount];
            int finalResultIndex = 0;
            foreach (CatchmentDefinition catchment in MyWork.Catchments)
            {
                // Note that Gather is blocking but not synchronous. This means that I can't cause deadlock by having processes
                // call Gather on the communicators in different sequences. My local call to gather will return even if the other 
                // participants in the gather have not joined in yet.
                Intracommunicator catchmentComm = communicatorsByCatchmentId[catchment.Id];
                Log.DebugFormat("Rank {0}: Catchment '{1}' rank {2}: Sending {3} cell results", WorldRank, catchment.Id, catchmentComm.Rank, partialCatchmentResults[catchment.Id].Count);

                if (catchmentComm.Rank == 0)
                {
                    SerializableDictionary<string, MpiTimeSeries>[] completeCatchmentResults = catchmentComm.GatherFlattened(
                        partialCatchmentResults[catchment.Id].ToArray(),
                        CatchmentResultCountPerCatchmentCommunicator[catchment.Id],
                        0);

                    // catchmentResults contains all the gridded results for the current catchment
                    Log.DebugFormat("Rank {0}: Catchment '{1}': Accumulating {2} results from {3} cells", WorldRank, catchment.Id, completeCatchmentResults.Length, catchment.Cells.Count);
                    Debug.Assert(catchment.Cells.Count == completeCatchmentResults.Length);
                    MpiObjectiveScores finalCatchmentResult = CalculateCatchmentScores(catchment, completeCatchmentResults, sysConfig, catchment.Cells.Count);
                    finalCatchmentResults[finalResultIndex] = finalCatchmentResult;
                    finalResultIndex++;
                }
                else
                {
                    catchmentComm.GatherFlattened(partialCatchmentResults[catchment.Id].ToArray(), 0);
                }
            }

            return finalCatchmentResults;
        }

        private MpiObjectiveScores CalculateCatchmentScores(CatchmentDefinition catchment, SerializableDictionary<string, MpiTimeSeries>[] cellResults, MpiSysConfig sysConfig, int cellCount)
        {
            LumpedCatchmentObjectivesDefinition objDef = SerializationHelper.XmlDeserialize<LumpedCatchmentObjectivesDefinition>(new FileInfo(ObjectiveDefinitionFileName));
            objDef.CatchmentIdentifier = catchment.Id;

            PointTimeSeriesSimulationDictionaryAdapter catchmentTimeSeriesAdapter = new PointTimeSeriesSimulationDictionaryAdapter(AverageCellTimeSeries(cellResults, cellCount));
            catchmentTimeSeriesAdapter.SetPeriod(catchment.Cells[0].ModelRunDefinition.StartDate, catchment.Cells[0].ModelRunDefinition.EndDate);
            IClonableObjectiveEvaluator<MpiSysConfig> catchmentScoreEvaluator = new CatchmentStatisticsEvaluator<ICloneableSimulation, MpiSysConfig>(
                catchmentTimeSeriesAdapter, 
                objDef.BuildAnalysis(),
                objDef.GetMaximizable(),
                objDef.GetObjectiveNames());

            return new MpiObjectiveScores(catchmentScoreEvaluator.EvaluateScore(sysConfig));
        }

        private SerializableDictionary<string, TimeSeries> AverageCellTimeSeries(SerializableDictionary<string, MpiTimeSeries>[] allCellResults, int cellCount)
        {
            SerializableDictionary<string, MpiTimeSeries> averages = new SerializableDictionary<string, MpiTimeSeries>();

            foreach (SerializableDictionary<string, MpiTimeSeries> cellResult in allCellResults)
            {
                foreach (KeyValuePair<string, MpiTimeSeries> cellResultTimeSeries in cellResult)
                {
                    if (averages.ContainsKey(cellResultTimeSeries.Key))
                    {
                        // add the time series values
                        double[] dst = averages[cellResultTimeSeries.Key].TimeSeries;
                        double[] src = cellResultTimeSeries.Value.TimeSeries;

                        for (int i = 0; i < src.Length; i++)
                            dst[i] += src[i];
                    }
                    else
                    {
                        averages.Add(cellResultTimeSeries.Key, (MpiTimeSeries)cellResultTimeSeries.Value.Clone());
                    }
                }
            }

            // convert the sums to the mean
            foreach (MpiTimeSeries value in averages.Values)
            {
                for (int i = 0; i < value.TimeSeries.Length; i++)
                    value[i] /= cellCount;
            }

            // convert back to the Time.Data.TimeSeries objects for use by the statistics objects.
            SerializableDictionary<string, TimeSeries> result = new SerializableDictionary<string, TimeSeries>();
            foreach (KeyValuePair<string, MpiTimeSeries> keyValuePair in averages)
            {
                MpiTimeSeries value = keyValuePair.Value;
                result.Add(keyValuePair.Key, new TimeSeries(value.Start, value.TimeStep, value.TimeSeries));
            }

            return result;
        }
#endif


        #endregion
    }
}