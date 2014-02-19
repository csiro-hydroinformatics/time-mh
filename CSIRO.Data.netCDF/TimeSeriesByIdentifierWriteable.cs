using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.ma2;

namespace CSIRO.Data.netCDF
{
    /// <summary>
    /// A class to create univariate point time series files in netCDF
    /// </summary>
    public class TimeSeriesByIdentifierWriteable : BaseTimeSeriesByIdentifierWriteable
    {
        private string variableName;

        /// <summary>
        /// A class to create univariate point time series files in netCDF
        /// </summary>
        /// <param name="filename">Valid file name where the file will be created</param>
        /// <param name="variableName">name of the variable to use in the netCDF file</param>
        /// <param name="units">Units of the variable stored</param>
        /// <param name="missingValue">Code for the missing value</param>
        /// <param name="longName">Long human readable name of the variable stored</param>
        /// <param name="startDate">Start date of the temporal span</param>
        /// <param name="timeLength">Length of the temporal dimension</param>
        /// <param name="itemIndexDimname">The name of the dimension storing the identifiers, e.g. 'CatchmentNo'</param>
        /// <param name="itemIndexVarname">The name of the netCDF variable storing the identifiers, e.g. 'CatchmentId'</param>
        /// <param name="maxLengthStrings">Maximum number of character to use for the identifiers</param>
        public TimeSeriesByIdentifierWriteable(string filename, string variableName, string units, double missingValue, 
            string longName, DateTime startDate, int timeLength, string itemIndexDimname = ITEM_INDEX_DIMNAME, 
            string itemIndexVarname = ITEM_INDEX_VARNAME, int maxLengthStrings = DEFAULT_STRING_LEN)
            : base(filename, DataType.FLOAT, new[] { variableName }, new[] { units }, new[]{missingValue}, new[]{longName}, 
            startDate, timeLength, itemIndexDimname, itemIndexVarname, maxLengthStrings)
        {
            if (string.IsNullOrEmpty(variableName))
                throw new ArgumentException("variableName cannot be null or empty");
            this.variableName = variableName;
        }

        public void AddTimeSeries(string identifier, double[] timeSeries)
        {
            this.InternalAddTimeSeries(identifier, timeSeries, variableName);
        }
    }
}
