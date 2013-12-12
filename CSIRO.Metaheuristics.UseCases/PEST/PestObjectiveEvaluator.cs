using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.DataTypes;
using TIME.Tools;
using CSIRO.Metaheuristics.Objectives;
using TIME.Tools.Metaheuristics;

namespace CSIRO.Metaheuristics.UseCases.PEST
{
    public class PestObjectiveEvaluator<T> : IObjectiveEvaluator<T>
            where T :  IHyperCube<double>
    {
        private TimeSeries observedData;
        private ModelRunner modelRunner;
        private String modelOutputTimeSeriesName;

        public PestObjectiveEvaluator(TimeSeries observationData, ModelRunner mr, string outputTimeSeriesName)
        {
            this.observedData = observationData;
            this.modelRunner = mr;
            this.modelOutputTimeSeriesName = outputTimeSeriesName;
        }

        public TimeSeries ObservedData { get { return observedData; } }
        public ModelRunner ModelRunner { get { return modelRunner; } }
        public String ModelOutputTimeSeries { get { return modelOutputTimeSeriesName; } }


        public IObjectiveScores<T> EvaluateScore(T systemConfiguration)
        {
            double score = calculateScore(systemConfiguration);
            DoubleObjectiveScore objectiveScore = new DoubleObjectiveScore("Sum of Squared Residuals", score, false);
            MultipleScores<T> objectiveScores = new MultipleScores<T>(new DoubleObjectiveScore[] { objectiveScore }, systemConfiguration);

            return objectiveScores;
        }

        /// <summary>
        /// Calculates the sum of square residuals for the two time series
        /// (i.e. one calculated from the hypercube and the observed data)
        /// </summary>
        /// <param name="systemConfiguration">hyper cube parameter set</param>
        /// <returns></returns>
        private double calculateScore(T systemConfiguration)
        {
            TimeSeries output = new TimeSeries();

            systemConfiguration.ApplyConfiguration(modelRunner.Model);

            modelRunner.record(this.modelOutputTimeSeriesName, output);
            modelRunner.execute();

            double[] residuals = output.ToArray().Zip(observedData.ToArray(), (one, two) => Math.Pow((one - two), 2.0)).ToArray();
            double score = residuals.Sum();

            return score;
        }
    }
}
