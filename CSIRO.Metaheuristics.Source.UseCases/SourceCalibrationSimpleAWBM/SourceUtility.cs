using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM;
using CSIRO.Metaheuristics.Objectives;
//using CSIRO.Metaheuristics.UseCases.AWBMDualCalibration;

namespace CSIRO.Metaheuristics.Source.SourceCalibrationSimpleAWBM.SourceCalibrationSimpleAWBM
{
    public static class SourceUtility
    {

        /// <summary>
        /// Doesn't really print what is needed
        /// </summary>
        /// <param name="items"></param>
        public static void PrintObjScores( IObjectiveScores[] items )
        {
            for( int i = 0; i < items.Length; i++ )
            {
                for( int j = 0; j < items[i].ObjectiveCount; j++ )
                {
                    IObjectiveScore objScore = items[i].GetObjective( j );
                    DoubleObjectiveScore doubleObjectiveScore = objScore as DoubleObjectiveScore;
                    string objectiveType = doubleObjectiveScore.GetText( );
                    string systemConfig = items[i].SystemConfiguration.GetConfigurationDescription();
                    Console.WriteLine(string.Concat(objectiveType, System.Environment.NewLine, systemConfig));

                }
            }
            Console.WriteLine( "***********" );
        }

        public static void PrintParams( ISystemConfiguration[] pSets )
        {
            for( int i = 0; i < pSets.Length; i++ )
            {
                MetaParameterSet metaParameterSet = ( (MetaParameterSet)pSets[i] );
                PrintMetaParameterSet( metaParameterSet );
            }
        }

        public static void PrintMetaParameterSet( MetaParameterSet metaParameterSet )
        {
            string[] pSetNames = metaParameterSet.GetVariableNames( );
            for( int j = 0; j < pSetNames.Length; j++ )
            {
                double value = metaParameterSet.GetValue( pSetNames[j] );
                string pName = pSetNames[j];
                Console.WriteLine( String.Concat( pName, " ", value.ToString( ) ) );
            }
            Console.WriteLine( "------" );
        }
    }
}
