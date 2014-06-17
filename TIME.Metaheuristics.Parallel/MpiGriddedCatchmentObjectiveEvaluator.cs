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
using MPI;
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
    public class MpiGriddedCatchmentObjectiveEvaluator : BaseGriddedCatchmentObjectiveEvaluator
    {
        public MpiGriddedCatchmentObjectiveEvaluator(FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo)
            : base(globalDefinitionFileInfo, objectivesDefinitionFileInfo, Communicator.world.Rank, Communicator.world.Size, new MpiWorldIntracommunicatorProxy())
        {
        }

        internal override IIntracommunicatorProxy CreateIntracommunicatorProxy(IGroupProxy catchmentGroup)
        {
            var res = new MpiIntracommunicatorProxy(catchmentGroup);
            return (res.IsNull ? null : res); 
        }

        internal override IGroupProxy CreateGroup(int[] ranks)
        {
            return new MpiGroupProxy(ranks);
        }

        internal override MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int[] counts, int root)
        {
            return Communicator.world.GatherFlattened(mpiObjectiveScores, counts, root);
        }

        internal override MpiObjectiveScores[] WorldGatherFlattened(MpiObjectiveScores[] mpiObjectiveScores, int root)
        {
            return Communicator.world.GatherFlattened(mpiObjectiveScores, root);
        }

        internal override void WorldBroadcast(ref MpiWorkPacket workPacket, int root)
        {
            Communicator.world.Broadcast(ref workPacket, root);
        }

        /// <summary>
        ///   Class logger instance.
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        ///   Entry point for running a slave process.
        /// </summary>
        public override void RunSlave()
        {
            Log.DebugFormat("Rank {0}: RunSlave", WorldRank);

            if (IsMaster)
                throw new InvalidOperationException("This method can only be called on the slave processes");

            MpiWorkPacket workPacket = new MpiWorkPacket(SlaveActions.Nothing);
            while (workPacket.Command != SlaveActions.Terminate)
            {
                // Check for instructions
                Log.DebugFormat("Rank {0}: waiting for work", WorldRank);
                WorldBroadcast(ref workPacket, 0);
                Log.DebugFormat("Rank {0}: {1}", WorldRank, SlaveActions.ActionNames[workPacket.Command]);

                if (workPacket.Command == SlaveActions.DoWork)
                    DoWork(workPacket.Parameters);
            }
        }

   }
}