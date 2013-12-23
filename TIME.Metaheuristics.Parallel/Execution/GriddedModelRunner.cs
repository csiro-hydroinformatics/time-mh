using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.DataTypes;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using TIME.Tools.ModelExecution;
using TIME.Tools.Persistence.DataMapping;

namespace TIME.Metaheuristics.Parallel.Execution
{
    /// <summary>
    /// A facade to execute gridded, multi-catchment simulations
    /// </summary>
    /// <remarks>
    /// This class is primarily intended as a facade to interactively test model behaviors. 
    /// <see cref="MpiGriddedCatchmentObjectiveEvaluator"/> is doing several things; 
    /// some of the simulation execution in the slaves may be handled here instead.  
    /// </remarks>
    public class GriddedModelRunner : IPointTimeSeriesSimulation
    {

        public GriddedModelRunner (GlobalDefinition definition)
        {
            models = new Dictionary<string, Dictionary<string, Tuple<CellDefinition, IPointTimeSeriesSimulation>>>();
            for (int i = 0; i < definition.Catchments.Count; i++)
            {
                createCatchment(definition.Catchments[i]);
            }
        }

        private void createCatchment(CatchmentDefinition catchmentDefinition)
        {
            var catId = catchmentDefinition.Id;
            for (int i = 0; i < catchmentDefinition.Cells.Count; i++)
            {
                addCell(catId, catchmentDefinition.Cells[i]);
            }
        }

        private Dictionary<string, Dictionary<string, Tuple<CellDefinition, IPointTimeSeriesSimulation>>> models; 
        private void addCell(string catId, CellDefinition cellDefinition)
        {
            if(!models.ContainsKey(catId)) models[catId] = new Dictionary<string, Tuple<CellDefinition, IPointTimeSeriesSimulation>>();
            IPointTimeSeriesSimulation mr =
                SimulationXmlFilesRepository.BuildModelRunner(cellDefinition.ModelRunDefinition);
            models[catId][cellDefinition.Id] = Tuple.Create(cellDefinition, mr);
        }

        public TimeSeries GetPlayed(string variableName)
        {
            Tuple<string, string, string> keys= getCellKeys(variableName);
            return GetModelRunner(keys).GetPlayed(keys.Item3);
        }

        private IPointTimeSeriesSimulation GetModelRunner(Tuple<string, string, string> keys)
        {
            var mr = models[keys.Item1][keys.Item2].Item2;
            return mr;
        }

        private char variableKeySeparator = '|';
        private DateTime startDate;
        private DateTime endDate;
        private Tuple<string, string, string> getCellKeys(string variableName)
        {
            var s = variableName.Split(variableKeySeparator);
            return Tuple.Create(s[0], s[1], s[2]);
        }

        public string[] GetPlayedVariableNames()
        {
            throw new NotImplementedException();
        }

        public TimeSeries GetRecorded(string variableName)
        {
            Tuple<string, string, string> keys = getCellKeys(variableName);
            return GetModelRunner(keys).GetRecorded(keys.Item3);
        }

        public string[] GetRecordedVariableNames()
        {
            throw new NotImplementedException();
        }

        public void Play(string inputIdentifier, TimeSeries timeSeries)
        {
            throw new NotImplementedException();
        }

        public void Record(string variableName)
        {
            Tuple<string, string, string> keys = getCellKeys(variableName);
            GetModelRunner(keys).Record(keys.Item3);
        }

        public void Execute()
        {
            System.Threading.Tasks.Parallel.ForEach(this.models, ExecuteCatchment);
        }

        private void ExecuteCatchment(KeyValuePair<string, Dictionary<string, Tuple<CellDefinition, IPointTimeSeriesSimulation>>> x)
        {
            foreach (var cellModel in x.Value)
            {
                cellModel.Value.Item2.Execute();
            }
        }

        public DateTime EndDate
        {
            get { return endDate; }
        }

        public void SetPeriod(DateTime startDate, DateTime endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            foreach (var catchment in models.Values)
            {
                foreach (var cell in catchment)
                {
                    cell.Value.Item2.SetPeriod(startDate, endDate);
                }
            }
        }

        public DateTime StartDate
        {
            get { return startDate; }
        }
    }
}
