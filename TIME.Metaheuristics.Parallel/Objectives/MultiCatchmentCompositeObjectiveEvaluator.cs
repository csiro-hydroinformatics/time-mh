using System;
using System.Diagnostics;
using System.IO;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel.Objectives
{
    /// <summary>
    ///   Composite objective evaluator for catchments.
    ///   Loosely corresponds to CalibrateParallelModel.Objectives.MultiCatchmentCompositeObjectiveCalculation
    /// </summary>
    public class MultiCatchmentCompositeObjectiveEvaluator : MpiObjectiveEvaluator
    {

        /// <summary>
        ///   Initializes a new instance of the <see cref="MultiCatchmentCompositeObjectiveEvaluator" /> class.
        /// </summary>
        /// <param name="globalDefinitionFileInfo"> The global definition file info. </param>
        /// <param name="objectivesDefinitionFileInfo"> The objectives definition file info. </param>
        // <param name="globalCompoundObjectiveDefinitionFileInfo"> The global objective definition file info. This defines the function for compositing scores into the single global score. </param>
        public MultiCatchmentCompositeObjectiveEvaluator(
            FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo, CompositeObjectiveCalculation<MpiSysConfig> compositeCalculation) : 
            base(new MpiGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinitionFileInfo), compositeCalculation)
        {
            mpiGridEval = (MpiGriddedCatchmentObjectiveEvaluator) systemsEvaluator;
        }

        private MpiGriddedCatchmentObjectiveEvaluator mpiGridEval;

        public int TotalCellCount
        {
            get { return (mpiGridEval != null) ? mpiGridEval.TotalCellCount : 0; }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}