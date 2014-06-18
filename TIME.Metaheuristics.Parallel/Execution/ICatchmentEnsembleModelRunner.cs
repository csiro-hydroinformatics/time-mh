using System.Collections.Generic;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Metaheuristics.Parallel.WorkAllocation;
using TIME.Tools.Collections;

namespace TIME.Metaheuristics.Parallel.Execution
{
    public interface ICatchmentEnsembleModelRunner
    {
        /// <summary>
        /// Gets the work package assigned to the ensemble.
        /// </summary>
        /// <value>
        /// The work package.
        /// </value>
        WorkPackage Work { get; }

        /// <summary>
        /// Initialises the ensemble.
        /// </summary>
        /// <param name="work">The work package containing metadata of the catchments and cells to be evaluated.</param>
        void Initialise(WorkPackage work);

        /// <summary>
        /// Executes the ensemble model runner using the specified system configuration.
        /// </summary>
        /// <param name="systemConfiguration">The system configuration.</param>
        /// <returns>
        /// A dictionary of results, indexed by catchment Id, for the cells being calculated on the current process.
        /// Each dictionary value contains the partial weighted sum of cell results for that catchment, for all cells
        /// belonging to the catchment that are being processed by this ensemble.
        /// Note that catchments may be distributed across ensembles, and that clients of this interface have the
        /// responsibility for reassembling the complete catchment results.
        /// Each result in the outer dictionary is a dictionary of time series, indexed by the state variable name.
        /// </returns>
        Dictionary<string, SerializableDictionary<string, MpiTimeSeries>> Execute(MpiSysConfig systemConfiguration);
    }
}
