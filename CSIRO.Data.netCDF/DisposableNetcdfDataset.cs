using System;
using ucar.nc2.dataset;


namespace CSIRO.Data.netCDF
{
    /// <summary>
    /// A NetcdfDataset that implements the IDisposable pattern for cleaner use in a CLR
    /// </summary>
    public class DisposableNetcdfDataset : NetcdfDataset, IDisposable
    {
        public DisposableNetcdfDataset(string location)
            : base(NetcdfDataset.openDataset(location))
        {
        }

        public void Dispose()
        {
            // TOCHECK: is there some way to check this is already closed??
            this.close();
        }
    }

}
