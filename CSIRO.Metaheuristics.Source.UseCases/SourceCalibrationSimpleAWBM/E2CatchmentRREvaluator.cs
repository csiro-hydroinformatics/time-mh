using System;
using System.Collections.Generic;
//using CSIRO.Metaheuristics.UseCases.AWBMDualCalibration;
using RiverSystem;
using TIME.DataTypes;
using CSIRO.Metaheuristics.Objectives;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public class E2CatchmentRREvaluator : IObjectiveEvaluator<MetaParameterSet>
    {
        private RiverSystemScenario scenario;
        public E2CatchmentRREvaluator( RiverSystemScenario scenario )
        {
            this.scenario = scenario;
            RiverSystem.Node node = scenario.Network.outletNodes()[0] as RiverSystem.Node;
            theRunoff = scenario.NetworkRunner.record( "Flow", node.Outflow );
            scenario.NetworkRunner.execute();
            observedRunoff = (TimeSeries)theRunoff.copy( );
        }

        private TimeSeries theRunoff; 
        private TimeSeries observedRunoff;

        public IObjectiveScores EvaluateScore( MetaParameterSet systemConfiguration )
        {
            systemConfiguration.ApplyConfiguration( scenario );
            scenario.run( scenario.NetworkRunner.start, scenario.NetworkRunner.end );
            //calculator objective values from streamflow
            MockMultiObjectiveEvaluator multiObjectiveEvaluator = new MockMultiObjectiveEvaluator( systemConfiguration);
            multiObjectiveEvaluator.Add( new MockNashSutcliffeEvaluator( theRunoff, observedRunoff ) );
            multiObjectiveEvaluator.Add( new MockBiasEvaluator( theRunoff, observedRunoff ) );
            return new TestSourceObjectiveScores(multiObjectiveEvaluator.calculateScores( ), systemConfiguration);
        }
 
        /// <summary>
        /// A container of objective evaluators
        /// </summary>
        private class MockMultiObjectiveEvaluator 
        {
            public MockMultiObjectiveEvaluator( ISystemConfiguration systemConfiguration )
            {
                this.systemConfiguration = systemConfiguration;
            }

            List<IMockSingleObjective> evaluators = new List<IMockSingleObjective>( );
            private ISystemConfiguration systemConfiguration;

            public void Add( IMockSingleObjective evaluator )
            {
                this.evaluators.Add( evaluator );
            }

            public IObjectiveScores EvaluateScore( ISystemConfiguration systemConfiguration )
            {
                return new MultipleScores( this.calculateScores( ), systemConfiguration );
            }

            public IObjectiveScore[] calculateScores( )
            {
                List<IObjectiveScore> scores = new List<IObjectiveScore>( );
                for( int i = 0; i < evaluators.Count; i++ )
                    scores.Add( evaluators[i].GetScore( ) );
                return scores.ToArray( );
            }
        }

        private interface IMockSingleObjective
        {
            IObjectiveScore GetScore();
        }

        /// <summary>
        /// Actual objective evaluator.
        /// </summary>
        internal class MockNashSutcliffeEvaluator : IMockSingleObjective
        {
            public MockNashSutcliffeEvaluator( TimeSeries toMatch, TimeSeries observed )
            {
                this.toMatch = toMatch;
                this.observed = observed;
            }

            private TimeSeries toMatch;
            private TimeSeries observed;
            public IObjectiveScore GetScore( )
            {
                double ns = TIME.Science.Statistics.DataStatistics.efficiency( toMatch, observed.extract( toMatch.Start, toMatch.End ) );
                return new DoubleObjectiveScore( "N-S", ns, true );
            }
        }
        /// <summary>
        /// Actual objective evaluator.
        /// </summary>
        internal class MockBiasEvaluator : IMockSingleObjective
        {
            public MockBiasEvaluator( TimeSeries toMatch, TimeSeries observed )
            {
                this.toMatch = toMatch;
                this.observed = observed;
            }

            private TimeSeries toMatch;
            private TimeSeries observed;
            public IObjectiveScore GetScore( )
            {
                double diff = Math.Abs( TIME.Science.Statistics.DataStatistics.relativeDifference( toMatch, observed.extract( toMatch.Start, toMatch.End ) ) );
                return new DoubleObjectiveScore( "RelDiff", diff, false );
            }

        }

        /// <summary>
        /// A container of scores from processing the objectives
        /// </summary>
        public class TestSourceObjectiveScores : IObjectiveScores
        {
            public TestSourceObjectiveScores(IObjectiveScore[] scores, ISystemConfiguration systemConfiguration )
            {
                this.scores = scores;
                this.systemConfiguration = systemConfiguration;
            }
            private IObjectiveScore[] scores;
            private ISystemConfiguration systemConfiguration;
            #region IObjectiveScores Members

            public int ObjectiveCount
            {
                get { return scores.Length; }
            }

            public IObjectiveScore GetObjective( int i )
            {
                return scores[i];
            }

            public ISystemConfiguration SystemConfiguration
            {
                get { return systemConfiguration; }
            }

            #endregion
        }
    }
}