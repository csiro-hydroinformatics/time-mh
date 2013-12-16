using System;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using CSIRO.Metaheuristics.Tests;
using TIME.DataTypes;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using TIME.Tools.Metaheuristics.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    ///  Lightweight class for testing MPI comms. Doesn't do any real calculations
    /// </summary>
    public class GriddedCatchmentToyModel : ICatchmentCellModelRunner
    {
        public GriddedCatchmentToyModel(CellDefinition cell)
        {
            Cell = cell;
            timeSeriesCount = (int)(cell.ModelRunDefinition.EndDate - cell.ModelRunDefinition.StartDate).TotalDays;
            //timeSeriesCount = 100;
        }

        private readonly int timeSeriesCount;
        public CellDefinition Cell { get; set; }

        private double FakeScore(double factor)
        {
            return (random.NextDouble() + factor)/2;
        }

        private static readonly Random random = new Random();

        public string CatchmentId { get { return Cell.CatchmentId; } }

        public string CellId { get { return Cell.Id; } }

        public SerializableDictionary<string, MpiTimeSeries> Execute(MpiSysConfig systemConfiguration)
        {
            SerializableDictionary<string, MpiTimeSeries> results = new SerializableDictionary<string, MpiTimeSeries>();
            var factor = TestHyperCube.CalculateParaboloid(systemConfiguration, 0);

            results.Add("runoff", new MpiTimeSeries(Cell.ModelRunDefinition.StartDate, new DailyTimeStep(), FakeScores(factor)));
            results.Add("LAI", new MpiTimeSeries(Cell.ModelRunDefinition.StartDate, new DailyTimeStep(), FakeScores(factor)));

            return results;
        }

        private double[] FakeScores(double factor)
        {
            double[] scores = new double[timeSeriesCount];
            for (int i = 0; i < timeSeriesCount; i++)
            {
                scores[i] = FakeScore(factor);
            }
            return scores;
        }
    }
}