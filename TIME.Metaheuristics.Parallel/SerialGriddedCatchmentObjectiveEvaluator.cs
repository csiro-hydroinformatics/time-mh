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
using TIME.DataTypes;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    ///   This class assumes primary responsibility for implementing MPI based calculation of objective scores for a set of gridded catchments.
    ///   Code for both the master (MPI.Communicator.world.Rank == 0) and worker/slave processes (rank > 0) is contained here.
    ///   Loosely corresponds to CalibrateParallelModel.MultiCatchmentCompositeObjectiveCalculation and MpiObjectiveEvaluator.
    /// </summary>
    internal class SerialGriddedCatchmentObjectiveEvaluator : BaseGriddedCatchmentObjectiveEvaluator
    {
        private static List<SerialGriddedCatchmentObjectiveEvaluator> instances;
        private static List<MpiObjectiveScores> CatchmentResults;
        
        static SerialGriddedCatchmentObjectiveEvaluator()
        {
            instances = new List<SerialGriddedCatchmentObjectiveEvaluator>();
        }
        public SerialGriddedCatchmentObjectiveEvaluator(FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo, int rank, int size)
            : base(globalDefinitionFileInfo, objectivesDefinitionFileInfo, rank, size)
        {
            if (instances.Count != rank) throw new ArgumentException("Must create SerialGriddedCatchmentObjectiveEvaluator in rank order");
            instances.Add(this);
        }

        internal override IIntracommunicatorProxy CreateIntracommunicatorProxy(IGroupProxy catchmentGroup)
        {
            return new SerialIntracommunicatorProxy(catchmentGroup);
        }

        internal override IGroupProxy CreateGroup(int[] ranks)
        {
            return new SerialGroupProxy(ranks);
        }

        internal override MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int[] counts, int root)
        {
            //return Communicator.world.GatherFlattened(mpiObjectiveScores, counts, root);
            if (this.rank == 0)
            {
                var res = CatchmentResults.ToArray();
                return res;
            }
            else
            {
                throw new NotSupportedException("SerialGriddedCatchmentObjectiveEvaluator only supports this method called from the master");
            }

        }

        internal override MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int root)
        {
            // return Communicator.world.GatherFlattened(mpiObjectiveScores, root);
            if (this.rank == 0)
            {
                throw new NotSupportedException("SerialGriddedCatchmentObjectiveEvaluator only supports this method called from a slave worker");
            }
            else
            {
                CatchmentResults.AddRange(mpiObjectiveScores);
                return null;
            }
        }

        internal override void WorldBroadcast(ref MpiWorkPacket workPacket, int root)
        {
            // Communicator.world.Broadcast(ref workPacket, root);
            if (this.rank == 0)
            {
                CatchmentResults = new List<MpiObjectiveScores>();
                if (workPacket.Command == SlaveActions.DoWork)
                {
                    // I don't think we can just call DoWork, as happens in the MPI layer (TODO: confirm the intent in the MPI implementation with Daniel).
                    var partialCatchmentResultsByCatchmentIds = new Dictionary<string, SerializableDictionary<string, MpiTimeSeries>>[instances.Count-1];
                    var parameters = workPacket.Parameters;
                    for (int i = 1; i < instances.Count; i++)
                    {
                        // execute our list of models, accumulating the results into the appropriate partial result buffer.
#if CELL_WEIGHTED_SUMS
                        partialCatchmentResultsByCatchmentIds[i-1] = EvaluateModels(parameters);
#else
            Dictionary<string, List<SerializableDictionary<string, MpiTimeSeries>>> partialCatchmentResultsByCatchmentId = EvaluateModels(parameters);
#endif
                    }

                    for (int i = 1; i < instances.Count; i++)
                    {
                        // For each catchment, accumulate the partial results for each catchment back to the catchment-coordinator.
                        MpiObjectiveScores[] finalCatchmentResults = AccumulateCatchmentResultsInCatchmentCoordinator(
                            partialCatchmentResultsByCatchmentIds[i - 1], parameters);
                        WorldGatherFlattened(finalCatchmentResults, 0);
                    }

                    // OK, all catchment results have been accumulated. Time to send the catchment results back to world root.
                    //Log.DebugFormat("Rank {0}: submitting {1} final catchment results to master", WorldRank, finalCatchmentResults.Length);

                }
            }
            else
            {
                throw new NotSupportedException("SerialGriddedCatchmentObjectiveEvaluator only support world broadcast from the master");
            }
        }

        public override void RunSlave()
        {
            throw new NotSupportedException("This class does not support running (listening) in a 'slave' mode");
        }
   }
}