﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using ucar.nc2;
using ucar.nc2.dt;
using ucar.nc2.units;
using ArrayFloat = ucar.ma2.ArrayFloat;
using ArrayInt = ucar.ma2.ArrayInt;
using GeoGrid = ucar.nc2.dt.grid.GeoGrid;
using ucar.ma2;

namespace CSIRO.Data.netCDF
{
    /// <summary>
    /// Helper functions build around the netCDF Java API.
    /// </summary>
    /// <remarks>
    /// This is meant to be as 'thin' as possible a layer between the netCDF API and 
    /// some idioms and patterns customary to the CLR.
    /// </remarks>
    public static class NetCdfHelper
    {
        // Some of the following strings are conforming to CF1 conventions: do not change lightly!
        internal const string latVarName = "lat";
        internal const string lonVarName = "lon";
        public const string TimeVarName = "time";
        public const string UnitsAttName = "units";
        public const string AxisAttName = "axis";
        internal const string standardNameAttName = "standard_name";
        public const string LongNameAttName = "long_name";
        // The following does not work anymore as DEFAULT_NULL_VALUE is not a constant anymore...
        //const float fillValue = (float)TIME.Core.Data.DEFAULT_NULL_VALUE;
        internal const float fillValue = -9999.0f;
        public const string MissingValueAttName = "missing_value";
        public const string FillValueAttName = "_FillValue";

        // Strings need to be stored along a 'fictive' dimensions, somehow:
        internal const string seriesIdName = "identifierStrings";
        internal const int DEFAULT_STRING_LEN = 20;

        internal static void addMissingValueDefinition(string variableName, NetcdfFileWriteable writeableFile)
        {
            writeableFile.addVariableAttribute(variableName, MissingValueAttName, new java.lang.Float(fillValue));
            writeableFile.addVariableAttribute(variableName, FillValueAttName, new java.lang.Float(fillValue));
        }

        internal static void createLatitudeVariable(NetcdfFileWriteable writeableFile, Dimension latDim)
        {
            writeableFile.addVariable(latVarName, DataType.FLOAT, new Dimension[] { latDim });
            writeableFile.addVariableAttribute(latVarName, UnitsAttName, "degrees_north");
            writeableFile.addVariableAttribute(latVarName, AxisAttName, "Y");
            writeableFile.addVariableAttribute(latVarName, standardNameAttName, "latitude");
        }

        internal static void createLongitudeVariable(NetcdfFileWriteable writeableFile, Dimension lonDim)
        {
            writeableFile.addVariable(lonVarName, DataType.FLOAT, new Dimension[] { lonDim });
            writeableFile.addVariableAttribute(lonVarName, UnitsAttName, "degrees_east");
            writeableFile.addVariableAttribute(lonVarName, AxisAttName, "X");
            writeableFile.addVariableAttribute(lonVarName, standardNameAttName, "longitude");
        }

        internal static void createTimeVariableDaily(NetcdfFileWriteable writeableFile, Dimension timeDim, DateTime startDate)
        {
            writeableFile.addVariable(TimeVarName, DataType.INT, new Dimension[] { timeDim });
            writeableFile.addVariableAttribute(TimeVarName, UnitsAttName, "days since " + startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writeableFile.addVariableAttribute(TimeVarName, AxisAttName, "T");
            writeableFile.addVariableAttribute(TimeVarName, standardNameAttName, TimeVarName);
            writeableFile.addVariableAttribute(TimeVarName, LongNameAttName, TimeVarName);
            writeableFile.setFill(true);
        }


        public static string[] GetVariableNames(string ncFile)
        {
            string[] result = null;
            using (var netcdfFile = new DisposableNetcdfDataset(ncFile))
            {
                result = GetVariableNames(netcdfFile);
            }
            return result;
        }

        public static string[] GetVariableNames(DisposableNetcdfDataset ncFile)
        {
            return System.Array.ConvertAll(ncFile.getVariables().toArray(), (x => ((Variable)x).getName()));
        }

        public static string[] GetDimensionsNames(string ncFile)
        {
            string[] result = null;
            using (var netcdfFile = new DisposableNetcdfDataset(ncFile))
            {
                result = GetDimensionsNames(netcdfFile);
            }
            return result;
        }

        public static string[] GetDimensionsNames(DisposableNetcdfDataset netcdfFile)
        {
            return System.Array.ConvertAll(netcdfFile.getDimensions().toArray(), (x => ((Dimension)x).getName()));
        }

        /// <summary>
        /// Finds the index of an item in an array, within a specified tolerance. 
        /// This is used to overcome rounding issues to locate the index of e.g. geographic coordinates in netcdf files.
        /// </summary>
        public static int IndexOf( float[] array, float value, int decimalPlaces )
        {
            return System.Array.IndexOf<double>( toDoubleArray( array, decimalPlaces ), Math.Round( value, decimalPlaces ) );
        }

        public static float[] GetFloatVariableValues( string ncFile, string variableName )
        {
            float[] result = null;
            using (var netcdfFile = new DisposableNetcdfDataset(ncFile))
            {
                result = GetFloatVariableValues(netcdfFile, variableName);
            }
            return result;
        }

        public static float[] GetFloatVariableValues(DisposableNetcdfDataset netcdfFile, string variableName)
        {
            var theArray = (ArrayFloat.D1)netcdfFile.findVariable(variableName).read();
            return (float[])theArray.get1DJavaArray(typeof(float));
        }

        public static Tuple<GeoGrid, DateTime> GetDailyGeoGrid( GridDataset dataCube, int gridIndex )
        {
            var geoGrid = (GeoGrid)dataCube.getGrids( ).get( gridIndex );
            var dateRange = geoGrid.getCoordinateSystem( ).getDateRange( );
            var length = geoGrid.getCoordinateSystem( ).getTimeAxis( ).getShape( ).Length;
            var startDate = DateTime.Parse( dateRange.getStart( ).getText( ).Replace( "Z", "" ) );
            var endDate = DateTime.Parse( dateRange.getStart( ).getText( ).Replace( "Z", "" ) );
            // Pretty weak check, but more is more work than doable now:
            if( !( startDate.AddDays( length - 1 ) == endDate ) )
                throw new NotSupportedException( "The data cube may not contain daily data, given the start and end date. Only daily time step is supported" );
            return Tuple.Create( geoGrid, startDate );
        }

        // TODO: consider introducing a DisposableGridDataset
        public static GridDataset ReadThreeDimFloatDataCube( string filename )
        {
            if( !File.Exists( filename ) )
                throw new FileNotFoundException( "File not found", filename );
            return ucar.nc2.dt.grid.GridDataset.open( filename );
        }

        public static DateTime[] BuildTimeDimension(ucar.ma2.ArrayInt.D1 timeArray, Variable timeVar)
        {
            int[] time1Darray = (int[])timeArray.copyTo1DJavaArray();
            DateTime[] dateArray = new DateTime[time1Darray.Length];
            String startUnit = timeVar.getUnitsString();
            var s = startUnit.ToLower().Trim();
            const string daysSince = "days since ";
            if (!s.StartsWith(daysSince))
                throw new NotSupportedException("Can only read time dimension of a daily period (variable dimension units must start with 'days since ')");
            var startDate = DateTime.Parse(s.Replace(daysSince, ""), CultureInfo.InvariantCulture);
            // Constructing a new DateUnit takes quite some time, in the order of seconds. 
            // This creates some performance difficulties dealing with gridded catchment data. Restrict support for the time being.
//            DateUnit du = new DateUnit(startUnit);
            for (int i = 0; i < dateArray.Length; i++)
            {
                // START should be:
                // var date = du.makeDate(time1Darray[i]);
                // WARNING
                // TODO: beware this! most data sets would be in local time. Goes back to the netcdf header content.
                // need revisit... time zones are a huge issue lurking!
                // dateArray[i] = DateTime.Parse(date.toGMTString().Replace(" GMT", string.Empty), CultureInfo.InvariantCulture);
                // END should be

                dateArray[i] = startDate.AddDays(i);

            }
            return dateArray;
        }

        public static DateTime[] GetTimeCoordinates(string ncFileLocation, string timeVarName = "time")
        {
            using (var ncFile = new DisposableNetcdfDataset(ncFileLocation))
            {
                return GetTimeCoordinates(ncFile, timeVarName);
            }
        }

        public static DateTime[] GetTimeCoordinates(NetcdfFile ncFile, string timeVarName = "time")
        {
            var timeVar = ncFile.findVariable(timeVarName);
            var theArray = (ArrayInt.D1)timeVar.read();
            return BuildTimeDimension(theArray, timeVar);
        }

        /// <summary>
        /// Find a variable CF-1 convention compliant missing value attribute. If not found return defaultValue 
        /// </summary>
        /// <param name="v">v - the variable or null for global attribute</param>
        public static double GetMissingValueAttributeDouble(NetcdfFile ncFile, Variable v, double defaultValue)
        {
            return ncFile.readAttributeDouble(v, MissingValueAttName, defaultValue);
        }

        public static T[] GetOneDimArray<T>(ucar.ma2.Array theArray)
        {
            if (typeof(string) != typeof(T))
            {
                return (T[])theArray.get1DJavaArray(typeof(T));
            }
            else
            {
                var shape = theArray.getShape();
                var size = shape[1];


                char[] temp = theArray.get1DJavaArray(typeof(char)) as char[];
                List<char> charList = new List<char>();
                List<string> strList = new List<string>();
                for (int i = 0; i < temp.Length; i++)
                {
                    if (temp[i] == '\0')
                    {
                        if (charList.Count > 0)
                        {
                            strList.Add(new string(charList.ToArray()));
                            charList.Clear();
                        }
                    }
                    else
                    {
                        charList.Add(temp[i]);
                        if (charList.Count == size)
                        {
                            strList.Add(new string(charList.ToArray()));
                            charList.Clear();
                        }
                    }
                }
                return strList.ToArray() as T[];
            }
        }

        private static double[] toDoubleArray(float[] array, int decimalPlaces)
        {
            return System.Array.ConvertAll(array, (x => Math.Round(x, decimalPlaces)));
        }

    }
}