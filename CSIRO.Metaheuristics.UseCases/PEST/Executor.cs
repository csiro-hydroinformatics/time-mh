using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.SystemConfigurations;

namespace CSIRO.Metaheuristics.UseCases.PEST
{
    public class Executor
    {
        public void Execute()
        {
            //IEvolutionEngine<IHyperCube<double>> engine = createEngine(,null);
            //var results = engine.Evolve();
        }
        /*
        private IEvolutionEngine<IHyperCube<double>> createEngine(IObjectiveEvaluator<T> evaluator,ICandidateFactory<T> populationInitializer)
        {
            throw new NotImplementedException();
        }*/


        private class PestCommandLineWrapper
        {
            /* General form of command line call to pest is  PEST controlfile
             * Within the control file, pointers to the pest instruction files (i.e. output file reader)
             * and the template files (model input files containing modified parameters) 
             */
            internal void Optimize(IObjectiveEvaluator<IHyperCube<double>> objEvaluator)
            {
                throw new NotImplementedException();
            }

        }

        private class PestObjectiveEvaluator : IObjectiveEvaluator<IHyperCube<double>>
        {

            public IObjectiveScores<IHyperCube<double>> EvaluateScore(IHyperCube<double> systemConfiguration)
            {
                throw new NotImplementedException();
            }
        }
    }
}
