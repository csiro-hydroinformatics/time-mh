using System;
using System.Reflection;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.RandomNumberGenerators;
using CSIRO.Metaheuristics.Source.SourceCalibrationSimpleAWBM.SourceCalibrationSimpleAWBM;
//using CSIRO.Metaheuristics.UseCases.AWBMDualCalibration;
using RiverSystem;
using RiverSystem.Catchments;
using RiverSystem.Management.ExpressionBuilder;
using RiverSystem.TestingSupport;
using TIME.Core.Metadata;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public class Executor
    {
        public void Execute( )
        {
            RiverSystemScenario testScenario;
            RiverSystemProject testProject;
            TestHelperRiverSystem.getAPreconfiguredTestScenarioWithAWBM( 3, out testProject,
                                                                         out testScenario );
            
            IObjectiveEvaluator<MetaParameterSet> evaluator = buildNewEvaluator( testScenario );
            ICandidateFactory<MetaParameterSet> candidateFactory = new LumpedAWBMFactory( testScenario, new BasicRngFactory(123) );

            int pSetNumber = 5;
            IObjectiveScores[] scores = new IObjectiveScores[pSetNumber];
            ISystemConfiguration[] pSets = new ISystemConfiguration[pSetNumber];


            int k = 0;
            foreach (Catchment c in testScenario.Network.catchmentList)
            {
                foreach (StandardFunctionalUnit fu in c.FunctionalUnits)
                {
                    var list = AccessorMemberInfo.GetFieldsAndPropertiesList(fu.rainfallRunoffModel.GetType(), typeof(double), typeof(ParameterAttribute));
                    foreach (MemberInfo m in list)
                    {
                        string metaParameterName = "$tag" + k++;
                        UpdateFunctionBuilder.CreateMetaParameter(testScenario, metaParameterName);
                        UpdateFunctionBuilder.CreateUpdateFunctionWithLinkToMetaParameter(testScenario, metaParameterName, m, fu.rainfallRunoffModel);
                    }
                }
            }

            for( int i = 0; i < scores.Length; i++ )
            {
                pSets[i] = candidateFactory.CreateRandomCandidate();

                //Do we need to reset the model states??
                scores[i] = evaluator.EvaluateScore( (MetaParameterSet)pSets[i] );

                //print Params after each iteration
                LumpedAWBMFactory.printPrams( testScenario , pSets[i]);
            }
            IObjectiveScores[] paretoScores = ParetoRanking<IObjectiveScores>.GetParetoFront( scores );
           

            SourceUtility.PrintObjScores( scores );
            Console.WriteLine("----------------------------------------");
            SourceUtility.PrintObjScores( paretoScores );
        }

        private IObjectiveEvaluator<MetaParameterSet> buildNewEvaluator( RiverSystemScenario scenario )
        {
            return new E2CatchmentRREvaluator( scenario );
        }

    }
}

