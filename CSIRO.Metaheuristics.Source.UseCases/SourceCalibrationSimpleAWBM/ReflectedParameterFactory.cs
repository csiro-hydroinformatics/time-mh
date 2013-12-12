using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using RiverSystem.Calibration;
using RiverSystem.Catchments;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public static class ReflectedParameterFactory
    {
        //private PropertyDescriptorCollection descriptors;
        //public ReflectedParameterFactory()
        //{
        //    Type cType = typeof( ReflectedParameter );
        //    ArrayList result = new ArrayList( );
        //    result.Add( new ReflectedPropertyDescriptor( cType, ReflectedParameter.NAME_PROPERTY_NAME,
        //                                                 ReflectedParameter.NAME_PROPERTY_DISPLAY_NAME, true ) );
        //    result.Add( new ReflectedPropertyDescriptor( cType, ReflectedParameter.MODELNAME_PROPERTY_NAME,
        //                                                 ReflectedParameter.MODELNAME_PROPERTY_DISPLAY_NAME,
        //                                                 false ) );
        //    result.Add( new ReflectedPropertyDescriptor( cType, ReflectedParameter.VALUE_PROPERTY_NAME, false ) );
        //    result.Add( new ReflectedPropertyDescriptor( cType, ReflectedParameter.FACTOR_PROPERTY_NAME,
        //                                                 ReflectedParameter.FACTOR_PROPERTY_DISPLAY_NAME, true ) );

        //    result.Insert( 0, new FUPropertyDescriptor( "FU" ) );
        //    result.Insert( 0, new CatchmentPropertyDescriptor( "Catchment" ) );
        //    descriptors = new PropertyDescriptorCollection( (PropertyDescriptor[])result.ToArray( typeof( PropertyDescriptor ) ) );
        //}

        //public PropertyDescriptorCollection PropertyDescriptors
        //{
        //    get { return descriptors; } 
        //}

        public static ReflectedParameter NewItem( ReflectedAccessor key )
        {
            return new ReflectedParameter( key );
        }

        public static ReflectedParameter NewItem( AccessorMemberInfo member, object obj )
        {
            return new ReflectedParameter( member, obj, member.displayName );
        }

        public static ReflectedParameter NewItem( AccessorMemberInfo member, object obj, string name )
        {
            return new ReflectedParameter( member, obj, name );
        }

        public static ReflectedParameter NewItem( AccessorMemberInfo member, object obj, string name, FunctionalUnit fu )
        {
            return new ReflectedParameter( member, obj, name, fu );
        }
    
        public static Dictionary<string,IList<ReflectedParameter>> NewItems(List<AccessorMemberInfo> accessorInfoList, object t)
        {
            var result = new Dictionary<string, IList<ReflectedParameter>>();            
            foreach (var accessorMemberInfo in accessorInfoList)
            {
                var tiedParameters = new List<ReflectedParameter>();
                ReflectedParameter tmp = NewItem(accessorMemberInfo, t);
                tiedParameters.Add(tmp);
                result.Add(accessorMemberInfo.Name, tiedParameters );   
            }
            return result;
        }
    }
}
