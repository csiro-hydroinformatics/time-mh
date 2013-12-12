using System;
using System.Collections.Generic;
using System.Linq;
using RiverSystem;
using RiverSystem.Catchments;
using RiverSystem.Collections;
using RiverSystem.Management.ExpressionBuilder;
using TIME.Core.Metadata;
using TIME.Tools.Reflection;
using ReflectedParameter = CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM.ReflectedParameter;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public class MetaParameterSet : IHyperCube<double>
    {
        private class ParameterValRange
        {

            public double Value{ get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public ParameterValRange(double min, double max)
            {
                Min = min;
                Max = max;
            }
        }

        private Dictionary<string, IList<ReflectedParameter>> ParameterMapping = new Dictionary<string, IList<ReflectedParameter>>( );
        private Dictionary<string, ParameterValRange> masterParameterSetValues = new Dictionary<string, ParameterValRange>();
        private List<AccessorMemberInfo> availableParameters = new List<AccessorMemberInfo>();
        //public string MetaParameterName { get; set; }
        //public double Equation { get; set; }
        private Dictionary<string, double> masterKnobs = new Dictionary<string, double>( );

        public Dictionary<string, double> MasterKnobs
        {
            get { return masterKnobs; }
            set { masterKnobs = value; }
        }

        public List<AccessorMemberInfo> AvailableParameters
        {
            get { return availableParameters; }
        }
        public MetaParameterSet(object system)
        {
            var r = new List<AccessorMemberInfo>( );
            RiverSystemScenario riverSystemScenario = system as RiverSystemScenario;            
            foreach( var item in riverSystemScenario.Network.catchmentList )
            {
                Catchment catchment = item as Catchment;
                foreach( StandardFunctionalUnit functionalUnit in catchment.FunctionalUnits )
                {
                    List<AccessorMemberInfo> list = KnownParameters( functionalUnit.rainfallRunoffModel );
                    r.AddRange( list );
                }
            }           
            foreach( AccessorMemberInfo key in r )
                if( !availableParameters.Contains( key ) )
                    availableParameters.Add( key );
        }

        public static List<AccessorMemberInfo> KnownParameters( object obj )
        {            
            //Type[] types = new Type[3]
            //{typeof( ParameterAttribute ),
            //typeof( MaximumAttribute ),
            //typeof( MinimumAttribute )
            //};
        
            var result = new List<AccessorMemberInfo>( );
            if( obj != null )
                result.AddRange( AccessorMemberInfo.GetFieldsAndProperties( obj.GetType( ), typeof( double ), typeof( ParameterAttribute ) ) );

            return result;
        }

        public void AddMappingRelation(string masterParameterName, IList<ReflectedParameter> many, double minValue = 0.0, double maxValue = 1.0)
        {
            if( !ParameterMapping.ContainsKey( masterParameterName ) )
            {
                ParameterMapping.Add( masterParameterName, many );
                masterParameterSetValues.Add(masterParameterName,new ParameterValRange(minValue, maxValue));//TODO Fix this range
            }
            else
            {
                var value = ParameterMapping[masterParameterName];
                foreach( var reflectedParameter in many )
                {
                    if (!value.Contains( reflectedParameter ))
                        value.Add( reflectedParameter );
                }
                ParameterMapping[masterParameterName] = value;
            }
        }

        public IList<ReflectedParameter> GroupSimilarParameters(CatchmentList clist, ReflectedParameter reflectedParameter)
        {
            throw new NotImplementedException( );
        }

        public void RemoveMappingRelation(ReflectedParameter reflectedParameter)
        {
            throw new NotImplementedException( );
        }

        public void RemoveMappingRelation(ReflectedParameter master, ReflectedParameter item)
        {
            throw new NotImplementedException( );
        }

        #region Implementation of ISystemConfiguration

        /// <summary>
        /// Gets an alphanumeric description for this system configuration
        /// </summary>
        public string GetConfigurationDescription( )
        {
            return RealConfiguration;
            //string description = Environment.NewLine;
            //foreach( var keyValPair in masterParameterSetValues )
            //{
            //    description = string.Concat( description, keyValPair.Key, " ", keyValPair.Value.Value, Environment.NewLine );
            //}
            //return description;
        }

        /// <summary>
        /// Apply this system configuration to a compatible system, usually a 'model' in the broad sense of the term.
        /// </summary>
        /// <param name="system">A compatible system, usually a 'model' in the broad sense of the term</param>
        /// <exception cref="ArgumentException">thrown if this system configuration cannot be meaningfully applied to the specified system</exception>
        public void ApplyConfiguration( object system )
        {
            foreach (KeyValuePair<string, double> keyValuePair in masterKnobs)
            {
                UpdateFunctionBuilder.SetMetaParameterValue(system as RiverSystemScenario, keyValuePair.Key, keyValuePair.Value);
            }

            //foreach (var keyValPair in masterParameterSetValues)
            //{
            //    double masterValue = keyValPair.Value.Value;
            //    foreach (var tiedParameter in this.ParameterMapping[keyValPair.Key])
            //    {
            //        tiedParameter.UseControllingValue(masterValue);
            //    }
            //}
        }

        #endregion

        #region IHyperCube<double> Members

        public string[] GetVariableNames( )
        {
            return masterParameterSetValues.Keys.ToArray();
        }

        public double GetValue( string variableName )
        {
            return masterParameterSetValues[variableName].Value;
        }

        public double GetMaxValue( string variableName )
        {
            return masterParameterSetValues[variableName].Max;
        }

        public double GetMinValue( string variableName )
        {
            return masterParameterSetValues[variableName].Min;
        }

        public void SetValue( string variableName, double value )
        {
            masterParameterSetValues[variableName].Value = value;
        }

        public IHyperCube<double> HomotheticTransform( IHyperCube<double> point, double factor )
        {
            throw new NotImplementedException( );
        }

        #endregion

        #region ICloningSupport<ICloneableSystemConfiguration> Members

        public ICloneableSystemConfiguration Clone( )
        {
            throw new NotImplementedException( );
        }

        public bool SupportsDeepCloning
        {
            get { throw new NotImplementedException( ); }
        }

        public bool SupportsThreadSafeCloning
        {
            get { throw new NotImplementedException( ); }
        }

        public string RealConfiguration { get; set; }

        #endregion

    }
}
