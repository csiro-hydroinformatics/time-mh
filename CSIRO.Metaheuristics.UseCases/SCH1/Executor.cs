using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.SystemConfigurations;

namespace CSIRO.Metaheuristics.UseCases.SCH1
{
    public class Executor
    {
        public void Execute()
        {
            SCH1ObjectiveEvaluator evaluator = new SCH1ObjectiveEvaluator();
            UnivariateRealUniformRandomSampler rand = new UnivariateRealUniformRandomSampler(-5, 5, 0);
            IObjectiveScores[] scores = new IObjectiveScores[1000];
            for (int i = 0; i < scores.Length; i++)
                scores[i] = evaluator.EvaluateScore(new UnivariateReal(rand.GetNext()));
            IObjectiveScores[] paretoScores = getParetoFront(scores);
            foreach (var score in paretoScores)
            {
                Console.Write(score.GetSystemConfiguration().GetConfigurationDescription());
                Console.Write(", ");
            }
            Console.WriteLine();
            Console.WriteLine("Press enter to quit");
            Console.ReadLine();
        }

        private IObjectiveScores[] getParetoFront(IObjectiveScores[] scores)
        {
            var paretoRanking = new ParetoRanking<IObjectiveScores>(scores, new ParetoComparer<IObjectiveScores>());
            return paretoRanking.GetParetoRank(1);
        }

        private class UnivariateRealUniformRandomSampler
        {
            public UnivariateRealUniformRandomSampler(double min, double max, int seed)
            {
                this.rand = new Random(seed);
                this.min = min;
                this.max = max;
            }
            private double min, max;
            private Random rand;
            public double GetNext()
            {
                return min + rand.NextDouble() * (max - min);
            }
        }

        //private class UnivariateReal : ISystemConfiguration
        //{
        //    public UnivariateReal(double value)
        //    {
        //        this.value = value;
        //    }
        //    private double value;

        //    public double Value
        //    {
        //        get { return this.value; }
        //    }

        //    public string GetConfigurationDescription()
        //    {
        //        return "Value: " + this.value;
        //    }

        //    #region ISystemConfiguration Members


        //    public void ApplyConfiguration( object system )
        //    {
        //        throw new NotImplementedException( );
        //    }

        //    #endregion
        //}
    }
}
