using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    /// This is the primary class for running a gridded calibration slave (or worker) process.
    /// Loosely corresponds to CalibrateParallelModel.SingleCatchmentEvaluatorProgram,
    /// although the MPI communications and model running is defined in <see cref="MpiGriddedCatchmentObjectiveEvaluator"/>,
    /// particularly the RunSlave() method.
    /// </summary>
    public class SlaveSystem
    {
        //private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly FileInfo globalDefinitionFileInfo;
        private readonly FileInfo objectivesDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlaveSystem"/> class.
        /// </summary>
        /// <param name="globalDefinitionFileInfo">The global definition file info.</param>
        /// <param name="objectivesDefinitionFileInfo">The objectives definition file info.</param>
        public SlaveSystem(FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo)
        {
            this.globalDefinitionFileInfo = globalDefinitionFileInfo;
            objectivesDefinition = objectivesDefinitionFileInfo;
        }

        /// <summary>
        ///   Runs the slave.
        /// </summary>
        public void RunSlave()
        {
            using (MpiGriddedCatchmentObjectiveEvaluator mpiSlave = new MpiGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinition))
            {
                mpiSlave.RunSlave();
            }
        }
    }
}