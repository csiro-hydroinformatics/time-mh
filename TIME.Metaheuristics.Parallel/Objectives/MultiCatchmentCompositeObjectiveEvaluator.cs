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
    public class MultiCatchmentCompositeObjectiveEvaluator : MpiObjectiveEvaluator, IDisposable
    {
        private readonly MpiGriddedCatchmentObjectiveEvaluator mpiEvaluator;
        private readonly CompositeObjectiveEvaluator<MpiSysConfig> rEvaluator;
        private bool disposed;
        private readonly Stopwatch simulationTimer = new Stopwatch();

        /// <summary>
        ///   Initializes a new instance of the <see cref="MultiCatchmentCompositeObjectiveEvaluator" /> class.
        /// </summary>
        /// <param name="globalDefinitionFileInfo"> The global definition file info. </param>
        /// <param name="objectivesDefinitionFileInfo"> The objectives definition file info. </param>
        // <param name="globalCompoundObjectiveDefinitionFileInfo"> The global objective definition file info. This defines the function for compositing scores into the single global score. </param>
        public MultiCatchmentCompositeObjectiveEvaluator(
            FileInfo globalDefinitionFileInfo, FileInfo objectivesDefinitionFileInfo, CompositeObjectiveEvaluator<MpiSysConfig> evaluator)
        {
            //rEvaluator = RCompositeObjectiveEvaluator<MpiSysConfig>.Create(globalCompoundObjectiveDefinitionFileInfo);
            rEvaluator = evaluator;
            mpiEvaluator = new MpiGriddedCatchmentObjectiveEvaluator(globalDefinitionFileInfo, objectivesDefinitionFileInfo);
        }

        #region IClonableObjectiveEvaluator<MpiSysConfig> Members

        /// <summary>
        ///   Evaluates the score.
        /// </summary>
        /// <param name="systemConfiguration"> The system configuration. </param>
        /// <returns> The composite objective score for the entire set of catchments. </returns>
        public override IObjectiveScores<MpiSysConfig> EvaluateScore(MpiSysConfig systemConfiguration)
        {
            simulationTimer.Start();
            mpiEvaluator.Execute(systemConfiguration);
            simulationTimer.Stop();
            // Copy the MpiObjectiveScores[] into an IObjectiveScores[]. 
            // MpiObjectiveScores[] is not directly assignable to IObjectiveScores[], 
            // even though MpiObjectiveScores implements IObjectiveScores
            IObjectiveScores[] scores = new IObjectiveScores[mpiEvaluator.CatchmentScores.Length];
            for (int i = 0; i < scores.Length; i++)
                scores[i] = mpiEvaluator.CatchmentScores[i];

            return rEvaluator.CalculateCompositeObjective(scores, systemConfiguration);
        }

        public int SimulationCount { get { return mpiEvaluator.Iterations; } }

        public TimeSpan SimulationTime
        {
            get { return simulationTimer.Elapsed; }
        }

        public int TotalCellCount
        {
            get { return (mpiEvaluator != null) ? mpiEvaluator.TotalCellCount : 0; }
        }

        #endregion

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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                    rEvaluator.Dispose();
                }

                // dispose unmanaged resources here

                // disposal is done. Set the flag so we don't get disposed more than once.
                disposed = true;
            }
        }
    }
}