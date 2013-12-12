using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Optimization;

namespace CSIRO.Metaheuristics.UseCases.HelloWorld
{

    public class Executor
    {
        public void Execute()
        {
            IEvolutionEngine<StringConfiguration> engine = new SimpleStringEvolutionEngine( new TargetStringObjectiveEvaluator( "Hello, world!" ), new StringFactory( ) );
            engine.Evolve();
        }


        private class StringConfiguration : ISystemConfiguration
        {
            public void ApplyConfiguration( object system )
            {
                throw new NotImplementedException( );
            }

            public string GetConfigurationDescription( )
            {
                throw new NotImplementedException( );
            }
        }

        private class SimpleStringEvolutionEngine : IEvolutionEngine<StringConfiguration>
        {
            public SimpleStringEvolutionEngine(TargetStringObjectiveEvaluator target, StringFactory initializationFactory)
            {
                this.target = target;
                this.initializationFactory = initializationFactory;
            }
            TargetStringObjectiveEvaluator target;
            StringFactory initializationFactory;
            public IOptimizationResults<StringConfiguration> Evolve()
            {
                IObjectiveEvaluator<StringSystemConfiguration> objectiveEvaluator = new StringObjEvaluator(this.target);
                StringSystemConfiguration point = initializationFactory.CreateRandomCandidate();
                IObjectiveScores score = objectiveEvaluator.EvaluateScore(point);
                // TODO: Expand on the details of the algorithm. For now, the high-level interfaces is what we are teasing out.
                return null;
            }

            public int CurrentGeneration { get; set; }

            public string GetDescription()
            {
                throw new NotImplementedException();
            }

            public void Cancel()
            {
                ;
            }

        }

        private class StringObjEvaluator : IObjectiveEvaluator<StringSystemConfiguration>
        {
            public StringObjEvaluator(TargetStringObjectiveEvaluator target)
            {
                this.target = target;
            }
            TargetStringObjectiveEvaluator target;
            public IObjectiveScores<StringSystemConfiguration> EvaluateScore( StringSystemConfiguration systemConfiguration )
            {
                return new MyStrScore(0);
            }
        }

        private class MyStrScore : IObjectiveScores<StringSystemConfiguration>
        {
            public MyStrScore(double distance)
            {
                this.distance = distance;
            }
            private double distance;
            public int ObjectiveCount
            {
                get { return 1; }
            }

            public ISystemConfiguration GetSystemConfiguration()
            {
                throw new NotImplementedException();
            }


            public IObjectiveScore GetObjective(int i)
            {
                throw new NotImplementedException();
            }

            #region IObjectiveScores<StringSystemConfiguration> Members

            public StringSystemConfiguration SystemConfiguration
            {
                get { throw new NotImplementedException( ); }
            }

            #endregion
        }


        private class StringSystemConfiguration : ISystemConfiguration
        {
            public StringSystemConfiguration(string str)
            {
                this.str = str;
            }
            private string str;
            public string GetConfigurationDescription()
            {
                return str;
            }

            #region ISystemConfiguration Members


            public void ApplyConfiguration( object system )
            {
                throw new NotImplementedException( );
            }

            #endregion
        }

        private class TargetStringObjectiveEvaluator : IObjectiveEvaluator<StringSystemConfiguration>
        {
            public TargetStringObjectiveEvaluator(string targetString)
            {
                this.targetString = targetString;
            }
            private string targetString;
            public IObjectiveScores<StringSystemConfiguration> EvaluateScore( StringSystemConfiguration systemConfiguration )
            {
                throw new NotImplementedException();
            }
        }

        private class StringFactory : ICandidateFactory<StringSystemConfiguration>
        {
            public StringSystemConfiguration CreateRandomCandidate()
            {
                return new StringSystemConfiguration("");
            }

            public IHyperCubeOperations CreateIHyperCubeOperations()
            {
                throw new NotImplementedException();
            }
        }

    }

}
