using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSIRO.Data.netCDF
{
    public class PointTimeSeriesNcDataset : DisposableNetcdfDataset
    {
        protected string ncVarnameIdentifier;
        protected DateTime[] timeCoords;
        private Dictionary<string, int> identifiersIndices;
        private Dictionary<string, double> missingValueCodes;
        private Dictionary<string, ucar.nc2.Variable> variables;
        private string[] identifiers;

        public PointTimeSeriesNcDataset(string location, string ncSeriesIdentifier)
            : base(location)
        {
            this.ncVarnameIdentifier = ncSeriesIdentifier;
            this.identifiers = NetCdfHelper.GetOneDimArray<string>(this.findVariable(ncVarnameIdentifier).read());
            this.identifiersIndices = identifiers.ToDictionary(x => x, y => Array.IndexOf(identifiers, y));
            //if (timeSeriesIdentifierVar == null)
            //    throw new NullReferenceException(string.Format("Could not find variable '{0}' in the netcdf file", ncVarnameIdentifier));
            this.timeCoords = NetCdfHelper.GetTimeCoordinates(this);
            variables = new Dictionary<string, ucar.nc2.Variable>();
            missingValueCodes = new Dictionary<string, double>();
        }

        private void GetTimeSeriesSpecForIdentifier(string entityIdentifier, out int[] origin, out int[] shape)
        {
            if (!identifiersIndices.ContainsKey(entityIdentifier))
                throw new ArgumentException(ncVarnameIdentifier + ": Identifier not found in the netCDF file: " + entityIdentifier);
            int entityIndex = identifiersIndices[entityIdentifier];
            origin = new int[] { entityIndex, 0 };
            shape = new int[] { 1/*entity, e.g. catchment*/, timeCoords.Length/*tslength*/ };
        }


        public double GetMissingValueCode(string ncVarName)
        {
            if (missingValueCodes.ContainsKey(ncVarName))
                return missingValueCodes[ncVarName];
            else
            {
                ucar.nc2.Variable v = getVariable(ncVarName);
                return NetCdfHelper.GetMissingValueAttributeDouble(this, v, double.NaN);
            }
        }

        public double[] GetSeries(string ncVarName, string identifier)
        {
            // TODO: check is daily
            ucar.nc2.Variable v = getVariable(ncVarName);
                 int[] origin;
                int[] shape;
            GetTimeSeriesSpecForIdentifier(identifier, out origin, out shape);
            var temp = v.read(origin, shape);
            return NetCdfHelper.GetOneDimArray<double>(v.read(origin, shape));
        }

        private ucar.nc2.Variable getVariable(string ncVarName)
        {
            ucar.nc2.Variable v;
            v = variables.ContainsKey(ncVarName) ? variables[ncVarName] : this.findVariable(ncVarName);
            if (v == null)
            {
                string msg = String.Format("Variable '{0}' not found in NetCDF file {1}", ncVarName, this.toString());
                throw new ArgumentException(msg);
            }
            return v;
        }
    }
}
