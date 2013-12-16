using System;
using System.Collections.Generic;
using CSIRO.Sys;
using TIME.DataTypes;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.SystemConfigurations;
using TIME.Tools.ModelExecution;

namespace TIME.Metaheuristics.Parallel
{
    internal sealed class PointTimeSeriesSimulationDictionaryAdapter : ICloneableSimulation
    {
        private IDictionary<string, TimeSeries> TimeSeriesDictionary { get; set; }

        public PointTimeSeriesSimulationDictionaryAdapter(IDictionary<string, TimeSeries> timeSeriesDictionary)
        {
            if (timeSeriesDictionary == null) throw new ArgumentNullException("timeSeriesDictionary");

            TimeSeriesDictionary = timeSeriesDictionary;
        }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public void SetPeriod(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        /// <summary>
        /// Executes the time stepping simulation over the time span currently specified
        /// </summary>
        public void Execute()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the time series containing model outputs for a model state variable.
        /// </summary>
        public TimeSeries GetRecorded(string variableName)
        {
            return TimeSeriesDictionary[variableName];
        }

        /// <summary>
        /// Gets the time series containing model inputs for a model state variable (state or input)
        /// </summary>
        public TimeSeries GetPlayed(string variableName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a flat list of all the model inputs for which there is a played time series
        /// </summary>
        public string[] GetPlayedVariableNames()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a flat list of all the model outputs recorded to a time series
        /// </summary>
        public string[] GetRecordedVariableNames()
        {
            string[] names = new string[TimeSeriesDictionary.Keys.Count];
            TimeSeriesDictionary.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// Sets a time series as an input to a specified model variable
        /// </summary>
        public void Play(string inputIdentifier, TimeSeries timeSeries)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Record a specified model variable into a time series.
        /// </summary>
        /// <param name="variableName"></param>
        public void Record(string variableName)
        {
            throw new NotImplementedException();
        }

        #region Implementation of ICloningSupport

        public ICloneableSimulation Clone()
        {
            throw new NotImplementedException();
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
}