using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.ma2;

namespace CSIRO.Data.netCDF
{
    /// <summary>
    /// A class to create multivariate point time series files in netCDF
    /// </summary>
    public class TimeSeriesByIdentifierWriteableMultiVar : BaseTimeSeriesByIdentifierWriteable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename">Valid file name where the file will be created</param>
        /// <param name="variableNames">names of the variable to use in the netCDF file</param>
        /// <param name="units">Units of the variables stored</param>
        /// <param name="missingValues">Code for the missing value</param>
        /// <param name="longNames">Long human readable name of the variables stored</param>
        /// <param name="startDate">Start date of the temporal span</param>
        /// <param name="timeLength">Length of the temporal dimension</param>
        /// <param name="itemIndexDimname">The name of the dimension storing the identifiers, e.g. 'CatchmentNo'</param>
        /// <param name="itemIndexVarname">The name of the netCDF variable storing the identifiers, e.g. 'CatchmentId'</param>
        /// <param name="maxLengthStrings">Maximum number of character to use for the identifiers</param>
        /// <param name="additionalAttributes">Additional attributes (key value pairs) for each variable name</param>
        /// <param name="globalAttributes">Global attribute to put on the data set</param>
        /// <param name="dataType">The precision used to store data on disk, DataType.FLOAT (default) or DataType.DOUBLE</param>
        public TimeSeriesByIdentifierWriteableMultiVar(string filename, string[] variableNames, string[] units, double[] missingValues, string[] longNames,
            DateTime startDate, int timeLength, string itemIndexDimname = ITEM_INDEX_DIMNAME, string itemIndexVarname = ITEM_INDEX_VARNAME, int maxLengthStrings = DEFAULT_STRING_LEN,
            Dictionary<string, Dictionary<string, string>> additionalAttributes = null, Dictionary<string, string> globalAttributes = null, DataType dataType=null)
            : base(filename, (dataType ?? DataType.FLOAT), variableNames, units, missingValues, longNames, startDate, timeLength, itemIndexDimname, 
            itemIndexVarname, maxLengthStrings, additionalAttributes, globalAttributes)
        {
        }

        public void AddTimeSeries(string variableName, string identifier, double[] timeSeries)
        {
            base.InternalAddTimeSeries(identifier, timeSeries, variableName);
        }
    }
}
