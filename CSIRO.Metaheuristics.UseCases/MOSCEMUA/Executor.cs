using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.UseCases.AWBMDualCalibration;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.Fitness;
using TIME.Science.Probability.RandomNumbers;
//using CatchmentYield.Tools.IO;
using CSIRO.Metaheuristics.Optimization;
using CSIRO.Metaheuristics.RandomNumberGenerators;
using TIME.Tools.Metaheuristics;

namespace CSIRO.Metaheuristics.UseCases.MOSCEMUA
{
    public class Executor
    {
        int p = 5, m = 27, q = 14, alpha = 3, beta = 27;
        int numShuffle = 18;

        public void Execute( string modelRunDefnFile, string modelOutputName, string runoffToMatchFile )
        {
            setRandSeed( );
            var engine = createNewEngine( modelRunDefnFile, modelOutputName, runoffToMatchFile );
            engine.Evolve( );
        }

        private IEvolutionEngine<ICloneableSystemConfiguration> createNewEngine( string modelRunDefnFile, string modelOutputName, string runoffToMatchFile )
        {
            return new ShuffledComplexEvolution<ICloneableSystemConfiguration>(
                ProblemDefinitionHelper.BuildEvaluator( modelRunDefnFile, modelOutputName, runoffToMatchFile ),
                new UniformRandomSamplingFactory<ICloneableSystemConfiguration>( ProblemDefinitionHelper.BuildSystem( modelRunDefnFile, "runoff" ).Model, new BasicRngFactory( 123 ) ),
                new ShuffledComplexEvolution<ICloneableSystemConfiguration>.MaxShuffleTerminationCondition( ),
                p, m, q, alpha, beta, numShuffle, new BasicRngFactory( 456 ), new ZitlerThieleFitnessAssignment( ) );
        }

        private static void setRandSeed( )
        {
            TIME.Science.Probability.RandomNumbers.RandomNumberGenerator.seedCoreNumberGenerator( 123 );
        }

    }
}
