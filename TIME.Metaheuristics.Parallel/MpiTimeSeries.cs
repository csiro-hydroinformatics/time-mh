using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.DataTypes;

namespace TIME.Metaheuristics.Parallel
{
    /// <summary>
    /// Lightweight serializable time series class for use in MPI messages.
    /// To reconstruct an <see cref="TIME.DataTypes.TimeSeries"/> instance,
    /// use the TimeSeries(DateTime, TimeStep, double[]) constructor.
    /// </summary>
    [Serializable]
    public class MpiTimeSeries
    {
        private double[] timeSeries;
        public DateTime Start { get; private set; }
        public TimeStep TimeStep { get; private set; }
        public double[] TimeSeries
        {
            get { return timeSeries; }
            private set { timeSeries = value; }
        }

        public double this[int index]
        {
            get { return TimeSeries[index]; }
            set { TimeSeries[index] = value; }
        }

        public MpiTimeSeries(TimeSeries timeSeries)
        {
            Start = new DateTime(timeSeries.Start.Ticks);
            TimeStep = timeSeries.timeStep;
            TimeSeries = timeSeries.ToArray();
        }

        public MpiTimeSeries(DateTime start, TimeStep timeStep, double[] timeSeries)
        {
            Start = new DateTime(start.Ticks);
            TimeStep = timeStep;
            TimeSeries = (double[])timeSeries.ToArray().Clone();
        }

        public void ApplyWeighting(float weight)
        {
            for (int i = 0; i < TimeSeries.Length; i++)
                TimeSeries[i] *= weight;
        }

        //public unsafe void ApplyWeighting_Unsafe(float weight)
        //{
        //    int length = timeSeries.Length;
        //    fixed (double* pFixedTs = timeSeries)
        //    {
        //        double* pTs = pFixedTs;
        //        for (int i = 0; i < length; i++)
        //        {
        //            *pTs *= weight;
        //            pTs++;
        //        }
        //    }
        //}

        /// <summary>
        /// In-place element-wise addition of a time series to the current instance.
        /// Note, I have not defined this as an operator to avoid the overhead of duplicating the objects
        /// </summary>
        /// <param name="source">The source.</param>
        public void InplaceAdd(MpiTimeSeries source)
        {
            if (source.TimeSeries.Length != this.TimeSeries.Length)
                throw new ArgumentOutOfRangeException("MpiTimeSeries length mismatch");
            for (int i = 0; i < TimeSeries.Length; i++)
                TimeSeries[i] += source[i];
        }

        //public unsafe void InplaceAdd_Unsafe(MpiTimeSeries source)
        //{
        //    if (source.TimeSeries.Length != this.TimeSeries.Length)
        //        throw new ArgumentOutOfRangeException("MpiTimeSeries length mismatch");

        //    int length = source.TimeSeries.Length;
        //    fixed (double* pDest = this.timeSeries, pSource = source.timeSeries)
        //    {
        //        double* pDst = pDest;
        //        double* pSrc = pSource;
        //        for (int i = 0; i < length; i++)
        //        {
        //            *pDst += *pSrc;
        //            pDst++;
        //            pSrc++;
        //        }
        //    }
        //}

        #region Implementation of ICloneable

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public object Clone()
        {
            return new MpiTimeSeries(Start, TimeStep, TimeSeries);
        }

        #endregion
    }
}
