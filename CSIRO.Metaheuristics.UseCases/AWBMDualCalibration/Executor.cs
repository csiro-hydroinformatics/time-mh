using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.Models.RainfallRunoff.AWBM;
using TIME.Tools.Metaheuristics;
using TIME.Tools.ModelExecution;
using TIME.Tools;
using TIME.Core;
using TIME.Tools.Optimisation;
using TIME.DataTypes;
using System.Reflection;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.RandomNumberGenerators;

namespace CSIRO.Metaheuristics.UseCases.AWBMDualCalibration
{
    public class Executor
    {
        static string OutputFileName = @"c:\tmp\testAwbmPareto.txt";

        public void Execute( string modelRunDefnFile, string modelOutputName, string runoffToMatchFile )
        {
            setRandSeed( );
            var evaluator = ProblemDefinitionHelper.BuildEvaluator( modelRunDefnFile , modelOutputName, runoffToMatchFile );
            var candidateFactory = buildCandidateFactory( ProblemDefinitionHelper.BuildSystem( modelRunDefnFile, "runoff" ).Model );

            var scores = new IObjectiveScores[1000];
            var pSets = new ICloneableSystemConfiguration[1000];
            for( int i = 0; i < scores.Length; i++ )
            {
                pSets[i] = candidateFactory.CreateRandomCandidate( );
                scores[i] = evaluator.EvaluateScore( pSets[i] );
            }
            IObjectiveScores[] paretoScores = ParetoRanking<IObjectiveScores>.GetParetoFront( scores );
            printSummary( scores );
            Console.ReadLine( );
        }

        private static void setRandSeed( )
        {
            TIME.Science.Probability.RandomNumbers.RandomNumberGenerator.seedCoreNumberGenerator( 123 );
        }

        private void printSummary( IObjectiveScores[] paretoScores )
        {
            string[][] values = new string[paretoScores.Length][];
            for( int i = 0; i < paretoScores.Length; i++ )
            {
                values[i] = new string[2];
                for( int j = 0; j < 2; j++ )
                    values[i][j] = paretoScores[i].GetObjective( j ).ValueComparable.ToString( );
            }
            // string str = CatchmentYield.Utilities.DelimiterSeparatedValues.SerialiseAsCommaSeparated( values );
            // FileIOHelper.SaveTextFile( str, OutputFileName );
        }

        private ICandidateFactory<ICloneableSystemConfiguration> buildCandidateFactory( IModel model )
        {
            return new UniformRandomSamplingFactory<ICloneableSystemConfiguration>( model, new BasicRngFactory( 123 ) );
        }


    }
}
