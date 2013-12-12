using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucar.nc2;
using ucar.ma2;
using ucar.nc2.dataset;
using ucar.nc2.iosp.netcdf3;
using ucar.unidata.io;


namespace CSIRO.Data.netCDF
{
    /// <summary>
    /// A NetcdfFile that implements the IDisposable pattern for cleaner use in a CLR
    /// </summary>
    public class DisposableNetcdfFile : NetcdfFile, IDisposable
    {
        public DisposableNetcdfFile(string location): base(location)
        {
        }

        public void Dispose()
        {
            // TOCHECK: is there some way to check this is already closed??
            this.close();
        }
    }

    /// <summary>
    /// A NetcdfDataset that implements the IDisposable pattern for cleaner use in a CLR
    /// </summary>
    public class DisposableNetcdfDataset : NetcdfDataset, IDisposable
    {
        public DisposableNetcdfDataset(string location): base(NetcdfDataset.openDataset(location))
        {
        }

        public void Dispose()
        {
            // TOCHECK: is there some way to check this is already closed??
            this.close();
        }
    }

    public class TimeSeriesByIdentifierWriteable : IDisposable
    {
        const int DEFAULT_STRING_LEN = 20;
        const string ITEM_INDEX_DIMNAME = "indexIdentifier";
        const string ITEM_INDEX_VARNAME = "identifier";

        public void Close()
        {
            writeableFile.finish();
            writeableFile.close();
        }

        public TimeSeriesByIdentifierWriteable(string filename, string variableName, string units, DateTime startDate, int timeLength, string itemIndexDimname = ITEM_INDEX_DIMNAME, string itemIndexVarname = ITEM_INDEX_VARNAME, int maxLengthStrings = DEFAULT_STRING_LEN)
        {
            this.variableName = variableName;
            this.itemIndexDimname = itemIndexDimname;
            this.itemIndexVarname = itemIndexVarname;
            this.timeLength = timeLength;

            writeableFile = NetcdfFileWriteable.createNew(filename);

            // define dimensions
            var timeDim = writeableFile.addDimension(NetCdfHelper.timeVarName, timeLength);
            var svarLen = writeableFile.addDimension(NetCdfHelper.seriesIdName,maxLengthStrings);
            var seriesIdDim = writeableFile.addUnlimitedDimension(itemIndexDimname);

            // define Dimension Variables
            NetCdfHelper.createTimeVariableDaily(writeableFile, timeDim, startDate);
            //writeableFile.addVariable(NetCdfHelper.seriesIdName, DataType.INT, new Dimension[] { svarLen });
            //writeableFile.addVariable("CatNo", DataType.INT, new Dimension[] { seriesIdDim });

            // Now onto the non-dimension variables.
            writeableFile.addVariable(variableName, DataType.FLOAT, new Dimension[] { seriesIdDim, timeDim });
            writeableFile.addVariableAttribute(variableName, NetCdfHelper.longNameAttName, variableName);
            writeableFile.addVariableAttribute(variableName, NetCdfHelper.unitsAttName, units);
            NetCdfHelper.addMissingValueDefinition(variableName, writeableFile);

            writeableFile.addVariable(itemIndexVarname, DataType.CHAR, new Dimension[] { seriesIdDim, svarLen });

            var daysSinceStart = new int[timeLength];
            for (int i = 0; i < daysSinceStart.Length; i++)
                daysSinceStart[i] = i;

            writeableFile.create();

            // write out the non-record variables
            writeableFile.write(NetCdfHelper.timeVarName, ucar.ma2.Array.factory(daysSinceStart));
        }

        public void AddTimeSeries(string identifier, double[] timeSeries)
        {
            if (timeSeries.Length != timeLength)
                throw new ArgumentException("The length of the time series data provided must match the length of the time dimension");

            // The origin to use to write the variable record
            int[] origin = new int[] { start_index_counter, 0 }; // tsIdentifier, time
            // The origin to use to write the identifier for each record
            int[] id_origin = new int[] { start_index_counter, 0 };


            var cellIdentifiers = identifier.ToCharArray();

            ucar.ma2.Array identifierData = ucar.ma2.Array.factory(DataType.CHAR, new int[] { 1, cellIdentifiers.Length }, cellIdentifiers);
            //ucar.ma2.Array identifierData = ucar.ma2.Array.factory(DataType, new int[] { 1, cellIdentifiers.Length }, cellIdentifiers);
            //ucar.ma2.Array identifierData = new ArrayChar(identifier);
            //ucar.ma2.Array identifierData = new ArrayChar(identifier);
            float[] values = System.Array.ConvertAll(timeSeries, (x => (float)x));
            ucar.ma2.Array variableData = ucar.ma2.Array.factory(DataType.FLOAT, new int[] { 1, timeLength}, values);
            writeableFile.write(variableName, origin, variableData);
            writeableFile.write(itemIndexVarname, id_origin, identifierData);

            start_index_counter++;
            //start_index_counter2 += cellIdentifiers.Length;
            writeableFile.findDimension(itemIndexDimname).setLength(start_index_counter);
        }

        private NetcdfFileWriteable writeableFile;
        private int start_index_counter = 0;
        private int start_index_counter2 = 0; 
        private string variableName;
        private string itemIndexDimname;
        private string itemIndexVarname;
        private int timeLength;

        public void Dispose()
        {
            writeableFile.flush();
            writeableFile.close();
        }
        
    }





    public class TimeSeriesByIdentifierWriteableMultiVar : IDisposable
    {
        const int DEFAULT_STRING_LEN = 20;
        const string ITEM_INDEX_DIMNAME = "indexIdentifier";
        const string ITEM_INDEX_VARNAME = "identifier";

        public void Close()
        {
            writeableFile.finish();
            writeableFile.close();
        }

        private Dimension timeDim;
        private Dimension seriesIdDim;

        public TimeSeriesByIdentifierWriteableMultiVar(string filename, string[] variableName, string[] units, double[] missingVal, string[] longName, DateTime startDate, int timeLength, string itemIndexDimname = ITEM_INDEX_DIMNAME, string itemIndexVarname = ITEM_INDEX_VARNAME, int maxLengthStrings = DEFAULT_STRING_LEN, Dictionary<string, Dictionary<string, string>> additionalAttributes = null, Dictionary<string, string> globalAttributes = null)
        {
            const string missingValueAttName = "missing_value";
            const string fillValueAttName = "_FillValue";

            this.variableName = variableName;
            this.itemIndexDimname = itemIndexDimname;
            this.itemIndexVarname = itemIndexVarname;
            this.timeLength = timeLength;

            writeableFile = NetcdfFileWriteable.createNew(filename);

            // define dimensions
            timeDim = writeableFile.addDimension(NetCdfHelper.timeVarName, timeLength);
            var svarLen = writeableFile.addDimension(NetCdfHelper.seriesIdName, maxLengthStrings);
            seriesIdDim = writeableFile.addUnlimitedDimension(itemIndexDimname);

            // define Dimension Variables
            writeableFile.addVariable(itemIndexDimname, DataType.INT, new Dimension[] { seriesIdDim });
            writeableFile.addVariable(NetCdfHelper.seriesIdName, DataType.INT, new Dimension[] { svarLen });
            NetCdfHelper.createTimeVariableDaily(writeableFile, timeDim, startDate);
            writeableFile.addVariableAttribute(NetCdfHelper.seriesIdName, NetCdfHelper.longNameAttName, NetCdfHelper.seriesIdName);
            writeableFile.addVariableAttribute(itemIndexDimname, NetCdfHelper.longNameAttName, itemIndexDimname);
            writeableFile.addVariableAttribute(itemIndexDimname, NetCdfHelper.unitsAttName, "count");
            //writeableFile.addVariable("CatNo", DataType.INT, new Dimension[] { seriesIdDim });

            if (globalAttributes != null)
            {
                foreach (var att in globalAttributes)
                    writeableFile.addGlobalAttribute(att.Key, att.Value);
            }

            // Now onto the non-dimension variables.
            for (int i = 0; i < variableName.Length;i++)
            {
                writeableFile.addVariable(variableName[i], DataType.FLOAT, new Dimension[] { seriesIdDim, timeDim });
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.longNameAttName, longName[i]);
                writeableFile.addVariableAttribute(variableName[i], NetCdfHelper.unitsAttName, units[i]);
                //NetCdfHelper.addMissingValueDefinition(variableName[i], writeableFile);
                writeableFile.addVariableAttribute(variableName[i], missingValueAttName, new java.lang.Float(missingVal[i]));
                writeableFile.addVariableAttribute(variableName[i], fillValueAttName, new java.lang.Float(missingVal[i]));
                counts.Add(variableName[i], 0);

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
            writeableFile.addVariableAttribute(itemIndexVarname, NetCdfHelper.longNameAttName, itemIndexVarname);

            var daysSinceStart = new int[timeLength];
            for (int i = 0; i < daysSinceStart.Length; i++)
                daysSinceStart[i] = i;

            writeableFile.create();

            // write out the non-record variables
            writeableFile.write(NetCdfHelper.timeVarName, ucar.ma2.Array.factory(daysSinceStart));
            writeableFile.write(NetCdfHelper.seriesIdName, ucar.ma2.Array.makeArray(DataType.INT, maxLengthStrings, 1, 1));

        }

        Dictionary<string,int> counts = new Dictionary<string, int>();
        public void AddTimeSeries(string identifier, double[] timeSeries, string variableName)
        {
            if (timeSeries.Length != timeLength)
                throw new ArgumentException("The length of the time series data provided must match the length of the time dimension");

            // The origin to use to write the variable record
            int[] origin = new int[] { counts[variableName], 0 }; // tsIdentifier, time
            // The origin to use to write the identifier for each record
            int[] id_origin = new int[] { counts[variableName], 0 };


            var cellIdentifiers = identifier.ToCharArray();

            ucar.ma2.Array identifierData = ucar.ma2.Array.factory(DataType.CHAR, new int[] { 1, cellIdentifiers.Length }, cellIdentifiers);
            //ucar.ma2.Array identifierData = ucar.ma2.Array.factory(DataType, new int[] { 1, cellIdentifiers.Length }, cellIdentifiers);
            //ucar.ma2.Array identifierData = new ArrayChar(identifier);
            //ucar.ma2.Array identifierData = new ArrayChar(identifier);
            float[] values = System.Array.ConvertAll(timeSeries, (x => (float)x));
            ucar.ma2.Array variableData = ucar.ma2.Array.factory(DataType.FLOAT, new int[] { 1, timeLength }, values);
            

            writeableFile.write(variableName, origin, variableData);
            writeableFile.write(itemIndexVarname, id_origin, identifierData);

            counts[variableName]++;

            //start_index_counter2 += cellIdentifiers.Length;
            writeableFile.findDimension(itemIndexDimname).setLength(counts[variableName]);

            writeableFile.write(itemIndexDimname, ucar.ma2.Array.makeArray(DataType.INT, counts.First().Value, 1, 1));

        }

        private NetcdfFileWriteable writeableFile;
        //private int start_index_counter = 0;
        //private int start_index_counter2 = 0;
        private string[] variableName;
        private string itemIndexDimname;
        private string itemIndexVarname;
        private int timeLength;

        public void Dispose()
        {
            writeableFile.flush();
            writeableFile.close();
        }





    }

}
