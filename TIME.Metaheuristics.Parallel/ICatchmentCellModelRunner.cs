using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.DataTypes;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using TIME.Tools.Metaheuristics.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel
{
    public interface ICatchmentCellModelRunner 
    {
        /// <summary>
        /// Gets the Id of the parent catchment to the cell being evaluated.
        /// </summary>
        string CatchmentId { get; }

        /// <summary>
        /// Gets the cell id.
        /// </summary>
        string CellId { get; }

        /// <summary>
        /// Gets the cell.
        /// </summary>
        CellDefinition Cell { get; }

        /// <summary>
        /// Executes the model runner using the specified system configuration.
        /// </summary>
        /// <param name="systemConfiguration">The system configuration.</param>
        /// <returns>A dictionary of time series, indexed by the state variable name.</returns>
        SerializableDictionary<string, MpiTimeSeries> Execute(MpiSysConfig systemConfiguration);
    }
}