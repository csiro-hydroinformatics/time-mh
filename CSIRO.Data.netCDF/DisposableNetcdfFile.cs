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

}
