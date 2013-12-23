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
    public class SlaveSystem : IDisposable
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
            MpiSlave = new MpiGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinition);
        }

        public MpiGriddedCatchmentObjectiveEvaluator MpiSlave;

        /// <summary>
        ///   Runs the slave.
        /// </summary>
        public void RunSlave()
        {
            MpiSlave.RunSlave();
        }

        private bool disposed = false;

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
                    MpiSlave.Dispose();
                }
                disposed = true;
            }
        }

    }
}