using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.DataTypes;

namespace CSIRO.Metaheuristics.UseCases.PEST
{
    public class ObservedData
    {
        private TimeSeries observations;

        public TimeSeries Observations
        {
            get { return observations; }
            set { observations = value; }
        }
        private string observationName;

        public string ObservationName
        {
            get { return observationName; }
            set { observationName = value; }
        }

        private double weight;

        public double Weight
        {
            get { return weight; }
            set { weight = value; }
        }

        

    }
}
