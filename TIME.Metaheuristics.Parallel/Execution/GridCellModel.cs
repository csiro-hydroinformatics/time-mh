using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics.Persistence.Gridded;

namespace TIME.Metaheuristics.Parallel.Execution
{
    public class GridCellModel : ICatchmentCellModelRunner
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ModelRunner modelRunner;

        public GridCellModel(ModelRunner modelRunner, CellDefinition cell)
        {
            this.modelRunner = modelRunner;
            Cell = cell;
        }

        /// <summary>
        /// Gets the cell.
        /// </summary>
        public CellDefinition Cell { get; private set; }

        public SerializableDictionary<string, MpiTimeSeries> Execute(MpiSysConfig systemConfiguration)
        {
            // Handling exceptions here allows more information to be logged, but there is a small performance 
            // cost to exception handling code even when exceptions are not thrown.
#if HANDLE_MODEL_EXCEPTIONS
            try
            {
#endif
                systemConfiguration.ApplyConfiguration(modelRunner);
                modelRunner.execute();
                SerializableDictionary<string, MpiTimeSeries> results = new SerializableDictionary<string, MpiTimeSeries>();
                foreach (string recordedVariableName in modelRunner.GetRecordedVariableNames())
#if CELL_WEIGHTED_SUMS
                {
                    MpiTimeSeries result = new MpiTimeSeries(modelRunner.GetRecorded(recordedVariableName));
                    result.ApplyWeighting(Cell.Weight);
                    results.Add(recordedVariableName, result);
                }
#else
                {    
                    results.Add(recordedVariableName, new MpiTimeSeries(modelRunner.GetRecorded(recordedVariableName)));
                }
#endif

                return results; 
#if HANDLE_MODEL_EXCEPTIONS
            }
            catch(System.Exception e)
            {
                Log.ErrorFormat("Exception in Catchment {0}, Cell {1}: {2}", CatchmentId, CellId, e.Message);
                throw;
            }
#endif
        }

        public string CatchmentId
        {
            get { return Cell.CatchmentId; }
        }

        public string CellId
        {
            get { return Cell.Id; }
        }
    }
}