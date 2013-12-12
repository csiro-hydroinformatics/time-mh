using RiverSystem;
using RiverSystem.TestingSupport;
using RiverSystem.Calibration;
using System;
using System.Linq;
using TIME.Tools.ModelExecution;
using TIME.DataTypes;
using TIME.ScenarioManagement;
using System.Collections.Generic;
namespace CSIRO.Metaheuristics.Source.UseCases.SCEToSource
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
            //ICandidateFactory<MetaParameterSet> candidateFactory = new LumpedAWBMFactory( testScenario, new BasicRngFactory(123) );

            var evolutionEngine = new CSIRO.Metaheuristics.Optimization.ShuffledComplexEvolution<MetaParameterSet>( );

            
            IObjectiveScores[] paretoScores = ParetoRanking<IObjectiveScores>.GetParetoFront( scores );
           

            SourceUtility.PrintObjScores( scores );
            Console.WriteLine("----------------------------------------");
            SourceUtility.PrintObjScores( paretoScores );
        }

        private IObjectiveEvaluator<MetaParameterSet> buildNewEvaluator( RiverSystemScenario scenario )
        {
            return new E2CatchmentRREvaluator( scenario );
        }


        private class SourceCatchmentMultiObjectiveEvaluator : IClonableObjectiveEvaluator<MetaParameterSet>
        {
            RiverSystemScenario scenario;
            CSIRO.Metaheuristics.TIMEWrappers.Objectives.ISingleObjectiveEvaluator[] singleObjEvaluators;

            public IObjectiveScores EvaluateScore( MetaParameterSet systemConfiguration )
            {
                systemConfiguration.ApplyConfiguration(scenario);
                scenario.run(scenario.Start, scenario.End);
                singleObjEvaluators[0].GetScore(
            }

            #region ICloningSupport<IClonableObjectiveEvaluator<MetaParameterSet>> Members

            public IClonableObjectiveEvaluator<MetaParameterSet> Clone( )
            {
                throw new NotSupportedException( );
            }

            public bool SupportsDeepCloning
            {
                get { return false; }
            }

            public bool SupportsThreadSafeCloning
            {
                get { return false; }
            }

            #endregion
        }

        private class PointRecordedTimeSeriesFromModel : IPointTimeSeriesSimulation, CSIRO.Metaheuristics.ICloningSupport<PointRecordedTimeSeriesFromModel>
        {
            public RiverSystemScenario Scenario { get; set; }
            public TimeSeries GetRecorded( string variableName )
            {
                // The name of the element|PropertyNameOfTheElement
                var specification = variableName.Split('|');
                var elementUniqueName = specification[0];
                var elementPropertyName = specification[1];
                object uniqueElement = findElement( elementUniqueName ); 

                TimeSeries result = null;
                ProjectViewTable projectViewTable = Scenario.Project.ResultManager.AllRuns( ).FirstOrDefault( ).RunParameters;
 
                Dictionary<ProjectViewRow.RecorderFields, object> searchCriteria =
                    new Dictionary<ProjectViewRow.RecorderFields, object>( );
                searchCriteria.Add( ProjectViewRow.RecorderFields.NetworkElementReference, uniqueElement );
                searchCriteria.Add( ProjectViewRow.RecorderFields.ElementName, elementPropertyName );
                IList<ProjectViewRow> rows = projectViewTable.Select( searchCriteria );
                Dictionary<AttributeRecordingState, TimeSeries> resultList = rows[0].ElementRecorder.GetResultList( );
                foreach( AttributeRecordingState key in resultList.Keys )
                {
                    result = resultList[key];
                    break;
                }
                return result;
            }

            public TimeSeries GetPlayed( string variableName )
            {
                throw new NotImplementedException( );
            }

            public string[] GetPlayedVariableNames( )
            {
                throw new NotImplementedException( );
            }

            public string[] GetRecordedVariableNames( )
            {
                throw new NotImplementedException( );
            }

            public void Play( string inputIdentifier, TimeSeries timeSeries )
            {
                throw new NotImplementedException( );
            }

            public void Record( string variableName )
            {
                throw new NotImplementedException( );
            }

            public DateTime StartDate
            {
                get { throw new NotImplementedException( ); }
            }

            public DateTime EndDate
            {
                get { throw new NotImplementedException( ); }
            }

            public void SetPeriod( DateTime startDate, DateTime endDate )
            {
                throw new NotImplementedException( );
            }

            public void Execute( )
            {
            }

            public void UseStateInitializer( ISimulationStateInitialization<IPointTimeSeriesSimulation> stateInit )
            {
                throw new NotImplementedException( );
            }

            public bool SupportsDeepCloning
            {
                get { throw new NotImplementedException( ); }
            }

            public bool SupportsThreadSafeCloning
            {
                get { throw new NotImplementedException( ); }
            }

            public PointRecordedTimeSeriesFromModel Clone( )
            {
                throw new NotImplementedException( );
            }
        }

    }
}

