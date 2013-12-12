using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.SystemConfigurations;
using CSIRO.Metaheuristics.Objectives;

namespace CSIRO.Metaheuristics.UseCases.SCH1
{
    public class SCH1ObjectiveEvaluator : CSIRO.Metaheuristics.IClonableObjectiveEvaluator<UnivariateReal>
    {
        public IObjectiveScores<UnivariateReal> EvaluateScore(UnivariateReal systemConfiguration)
        {
            double x = systemConfiguration.Value;
            double y = x - 2;
            var result = new SCH1ObjectiveScores(x * x, y * y);
            result.SystemConfiguration = systemConfiguration;
            return result;
        }

        private class SCH1ObjectiveScores : IObjectiveScores<UnivariateReal> // TODO: there should be intermediary abstract classes for common cases.
        {
            public SCH1ObjectiveScores(double firstScore, double secondScore)
            {
                scores = new double[] { firstScore, secondScore };
            }
            private double[] scores;
            public int ObjectiveCount
            {
                get { return 2; }
            }

            private UnivariateReal systemConfiguration;

            public ISystemConfiguration GetSystemConfiguration()
            {
                return this.systemConfiguration;
            }

            public UnivariateReal SystemConfiguration
            {
                get { return systemConfiguration; }
                internal set { systemConfiguration = value; }
            }

            public IObjectiveScore GetObjective(int i)
            {
                return new DoubleObjectiveScore( "SCH1_" + i, scores[i], false );
            }

        }

        public IClonableObjectiveEvaluator<UnivariateReal> Clone()
        {
            return new SCH1ObjectiveEvaluator();
        }

        public bool SupportsDeepCloning
        {
            get { return true; }
        }

        public bool SupportsThreadSafeCloning
        {
            get { return true; }
        }
    }
}
