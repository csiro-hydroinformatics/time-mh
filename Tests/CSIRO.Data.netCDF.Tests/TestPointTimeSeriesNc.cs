using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace CSIRO.Data.netCDF.Tests
{
    public class TestPointTimeSeriesNc
    {
        [Fact]
        public void TestPointTimeSeriesDataReadWrite()
        {
            var tempFileName = Path.GetTempFileName();

            try
            {
                var varNames = new[] { "varOne", "varTwo" };
                var units = new[] { "mm", "m^3/s" };
                double[] missingVal = new[] { -9999.0, double.NaN };
                string[] longName = new[] { "First variable", "Second variable" };
                DateTime startDate = new DateTime(1984, 01, 30);
                int timeLength = 123;
                string itemIndexDimname = "cell_id";
                string itemIndexVarname = "cell_id_name";
                var additionalAttributes = new Dictionary<string, Dictionary<string, string>>();
                int numCellIds = 5;
                var cellNames = new[] { "a", "b", "c", "d", "e" };

                for (int i = 1; i < varNames.Length; i++)
                {
                    additionalAttributes.Add(varNames[i], createDict(i));
                }
                var globalAttributes = createDict(22);

                using (var writer = new TimeSeriesByIdentifierWriteableMultiVar(tempFileName, varNames, units, missingVal, longName,
                    startDate, timeLength, itemIndexDimname, itemIndexVarname, maxLengthStrings: 30,
                    additionalAttributes: additionalAttributes, globalAttributes: globalAttributes))
                {
                    int varId = 0;
                    for (int cellId = 0; cellId < numCellIds; cellId++)
                        writer.AddTimeSeries(varNames[varId], cellNames[cellId], createSeries(varId, cellId, timeLength));

                    // Purposely add series in an sequence that does not match the natural order of cellNames
                    varId = 1;
                    writer.AddTimeSeries(varNames[varId], cellNames[3], createSeries(varId, 3, timeLength));
                    writer.AddTimeSeries(varNames[varId], cellNames[2], createSeries(varId, 2, timeLength));
                    writer.AddTimeSeries(varNames[varId], cellNames[4], createSeries(varId, 4, timeLength));
                    writer.AddTimeSeries(varNames[varId], cellNames[1], createSeries(varId, 1, timeLength));
                    writer.AddTimeSeries(varNames[varId], cellNames[0], createSeries(varId, 0, timeLength));

                }

                //Thread.Sleep(1000);

                using (var reader = new PointTimeSeriesNcFile(tempFileName, itemIndexVarname))
                {
                    for (int i = 0; i < varNames.Length; i++)
                    {
                        for (int j = 0; j < cellNames.Length; j++)
                        {
                            float[] s = toFloat(reader.GetSeries(varNames[i], cellNames[j]));
                            Assert.Equal(toFloat(createSeries(i, j, timeLength)), s);
                        }
                        Assert.Equal(missingVal[i], reader.GetMissingValueCode(varNames[i]));
                    }

                    // TODO; Reading attributes is more involved than anticipated. 
                    /*
                    var actualAttributes = reader.GetVariableAttributes();
                    foreach (var v in additionalAttributes.Keys)
                    {
                        var a = additionalAttributes[v];
                        var e = a.Keys.ToList();
                        e.AddRange(new[] { NetCdfHelper.LongNameAttName, NetCdfHelper.UnitsAttName, NetCdfHelper.MissingValueAttName, NetCdfHelper.FillValueAttName });
                        var expected = e.ToArray();
                        Array.Sort(expected);
                        var actual = actualAttributes[v].Keys.ToArray();
                        Array.Sort(actual);

                        Assert.Equal(expected, actual);
                    }
                    Assert.Equal(globalAttributes, reader.GetGlobalAttributes());
                     * */

                    Assert.Equal(startDate, reader.GetTimeCoordinate(0));
                    Assert.Equal(startDate.AddDays(timeLength - 1), reader.GetTimeCoordinate(timeLength-1));
                }
            }
            finally
            {
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
            }
        }

        private float[] toFloat(double[] p)
        {
            return Array.ConvertAll(p, x => (float)x);
        }

        private double[] createSeries(int i, int cellId, int timeLength)
        {
            var x = new double[timeLength];
            for (int t = 0; t < timeLength; t++)
            {
                x[t] = i + t * 0.1 * (cellId + 1);
            }
            return x;
        }

        private Dictionary<string, string> createDict(int i)
        {
            var x = new Dictionary<string, string>();
            for (int j = 0; j < 4; j++)
            {
                var k = (i + j).ToString();
                x.Add(k, k + "_value");
            }
            return x;
        }
    }
}
