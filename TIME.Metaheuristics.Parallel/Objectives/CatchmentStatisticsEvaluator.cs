using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Sys;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.Objectives;
using TIME.Tools.Metaheuristics.SystemConfigurations;
using TIME.Tools.ModelAnalysis;
using TIME.Tools.ModelExecution;

namespace TIME.Metaheuristics.Parallel.Objectives
{
    public class CatchmentStatisticsEvaluator<TSysRunner, TSysConfig> : TimeStatisticsEvaluator<TSysRunner, TSysConfig>
        where TSysRunner : IPointTimeSeriesSimulation, ICloningSupport<TSysRunner>
        where TSysConfig : ISystemConfiguration
    {
        public CatchmentStatisticsEvaluator(TSysRunner modelRunner,
           ITemporalAnalysis<IPointTimeSeriesSimulation> statisticalAnalysis,
           bool[] isMaximizable,
           string[] objectiveNames):
            base(modelRunner, statisticalAnalysis, isMaximizable, objectiveNames)
        {
        }

        public IObjectiveScores<TSysConfig> EvaluateScore(TSysRunner modelRunner, TSysConfig systemConfiguration)
        {
            // I deliberately don't call the base class. I want to avoid the calls to ApplyConfiguration and Execute. 
            // they will crash when using the PointTimeSeriesSimulationDictionaryAdapter class that wraps the 
            // summarised catchment time series data.

            // Also, even though TimeStatisticsEvaluator has a ModelRunner property, we bypass it here and use the 
            // method parameter
            return new MultipleScores<TSysConfig>(calculateScores(modelRunner), systemConfiguration);
        }

        /// <summary>
        /// Sets the model runner.
        /// This is not a property as I don't want to allow public access to the protected ModelRunner set method.
        /// This is only required here because we create a new model runner to wrap the catchment time series
        /// each time the model runs. This is because the catchment time series is calculated from the actual model 
        /// runners for each grid cell.
        /// </summary>
        /// <param name="modelRunner">The model runner.</param>
        public void SetModelRunner(TSysRunner modelRunner)
        {
            ModelRunner = modelRunner;
        }
    }
}
