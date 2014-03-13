using System;
using ucar.nc2;


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
