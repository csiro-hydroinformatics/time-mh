using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.ma2;
using ucar.nc2;

namespace CSIRO.Data.netCDF
{
    public abstract class BaseTimeSeriesByIdentifierWriteable : IDisposable
    {
        public const int DEFAULT_STRING_LEN = 20;
        public const string ITEM_INDEX_DIMNAME = "indexIdentifier";
        public const string ITEM_INDEX_VARNAME = "identifier";
        private Dimension timeDim;
        private Dimension seriesIdDim;
        private List<string> identifiers = new List<string>();
        private Dictionary<string, int> identifierIndices = new Dictionary<string, int>();
        private NetcdfFileWriteable writeableFile;
        //private int start_index_counter = 0;
        //private int start_index_counter2 = 0;
        private string[] variableName;
        private string itemIndexDimname;
        private string itemIndexVarname;
        private int timeLength;
        private DataType dataType = DataType.FLOAT;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                writeableFile.flush();
                writeableFile.close();
            }
            // Dispose native resources
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected BaseTimeSeriesByIdentifierWriteable(string filename, DataType dataType, string[] variableName, string[] units, double[] missingVal, string[] longName, DateTime startDate, int timeLength, string itemIndexDimname = ITEM_INDEX_DIMNAME, string itemIndexVarname = ITEM_INDEX_VARNAME, int maxLengthStrings = DEFAULT_STRING_LEN, Dictionary<string, Dictionary<string, string>> additionalAttributes = null, Dictionary<string, string> globalAttributes = null)
        {
            if (dataType != DataType.FLOAT && dataType != DataType.DOUBLE)
                throw new NotSupportedException("Time series storage data type in netCDF must be DOUBLE or FLOAT");

            this.dataType = dataType;
            this.variableName = variableName;
            this.itemIndexDimname = itemIndexDimname;
            this.itemIndexVarname = itemIndexVarname;
            this.timeLength = timeLength;

            writeableFile = NetcdfFileWriteable.createNew(filename);

            // define dimensions
            timeDim = writeableFile.addDimension(NetCdfHelper.TimeVarName, timeLength);
            var svarLen = writeableFile.addDimension(NetCdfHelper.seriesIdName, maxLengthStrings);
            seriesIdDim = writeableFile.addUnlimitedDimension(itemIndexDimname);

            // define Dimension Variables
            writeableFile.addVariable(itemIndexDimname, DataType.INT, new Dimension[] { seriesIdDim });
            writeableFile.addVariable(NetCdfHelper.seriesIdName, DataType.INT, new Dimension[] { svarLen });
            NetCdfHelper.createTimeVariableDaily(writeableFile, timeDim, startDate);
            writeableFile.addVariableAttribute(NetCdfHelper.seriesIdName, NetCdfHelper.LongNameAttName, NetCdfHelper.seriesIdName);
            writeableFile.addVariableAttribute(itemIndexDimname, NetCdfHelper.LongNameAttName, itemIndexDimname);
            writeableFile.addVariableAttribute(itemIndexDimname, NetCdfHelper.UnitsAttName, "count");
            //writeableFile.addVariable("CatNo", DataType.INT, new Dimension[] { seriesIdDim });

            if (globalAttributes != null)
            {
                foreach (var att in globalAttributes)
                    writeableFile.addGlobalAttribute(att.Key, att.Value);
            }

            // Now onto the non-dimension variables.
            for (int i = 0; i < variableName.Length; i++)
            {
                writeableFile.addVariable(variableName[i], dataType, new Dimension[] { seriesIdDim, timeDim });
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.LongNameAttName, longName[i]);
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.UnitsAttName, units[i]);
                //NetCdfHelper.addMissingValueDefinition(variableName[i], writeableFile);
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.MissingValueAttName, new java.lang.Float(missingVal[i]));
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.FillValueAttName, new java.lang.Float(missingVal[i]));
            }
            if (additionalAttributes != null)
            {
                foreach (var variableAtt in additionalAttributes)
                {
                    foreach (var att in variableAtt.Value)
                        writeableFile.addVariableAttribute(variableAtt.Key, att.Key, att.Value);
                }
            }

            if (globalAttributes != null)
            {
                foreach (var att in globalAttributes)
                    writeableFile.addGlobalAttribute(att.Key, att.Value);
            }

            writeableFile.addVariable(itemIndexVarname, DataType.CHAR, new Dimension[] { seriesIdDim, svarLen });
            writeableFile.addVariableAttribute(itemIndexVarname, NetCdfHelper.LongNameAttName, itemIndexVarname);

            var daysSinceStart = new int[timeLength];
            for (int i = 0; i < daysSinceStart.Length; i++)
                daysSinceStart[i] = i;

            writeableFile.create();

            // write out the non-record variables
            writeableFile.write(NetCdfHelper.TimeVarName, ucar.ma2.Array.factory(daysSinceStart));
            writeableFile.write(NetCdfHelper.seriesIdName, ucar.ma2.Array.makeArray(DataType.INT, maxLengthStrings, 1, 1));

        }

        protected void InternalAddTimeSeries(string identifier, double[] timeSeries, string variableName)
        {
            if (timeSeries.Length != timeLength)
                throw new ArgumentException("The length of the time series data provided must match the length of the time dimension");

            if (!identifierIndices.ContainsKey(identifier))
            {
                // This is a new identifier; we need to expand the corresponding unlimited dimension
                identifiers.Add(identifier);
                identifierIndices.Add(identifier, identifiers.Count - 1);
                writeableFile.findDimension(itemIndexDimname).setLength(identifiers.Count);
                var cellIdentifiers = identifier.ToCharArray();
                // The origin to use to write the identifier for each record
                int[] id_origin = new int[] { identifiers.Count - 1, 0 };
                ucar.ma2.Array identifierData = ucar.ma2.Array.factory(DataType.CHAR, new int[] { 1, cellIdentifiers.Length }, cellIdentifiers);
                writeableFile.write(itemIndexVarname, id_origin, identifierData);
                writeableFile.write(itemIndexDimname, ucar.ma2.Array.makeArray(DataType.INT, identifiers.Count, 1, 1));
            }

            var index = identifierIndices[identifier];
            // The origin to use to write the variable record
            int[] origin = new int[] { index, 0 }; // tsIdentifier, time
            object values = null;
            if (dataType == DataType.FLOAT)
                values = System.Array.ConvertAll(timeSeries, (x => (float)x));
            else if(dataType == DataType.DOUBLE)
                values = timeSeries;

            ucar.ma2.Array variableData = ucar.ma2.Array.factory(dataType, new int[] { 1, timeLength }, values);

            writeableFile.write(variableName, origin, variableData);

        }

    }
}
