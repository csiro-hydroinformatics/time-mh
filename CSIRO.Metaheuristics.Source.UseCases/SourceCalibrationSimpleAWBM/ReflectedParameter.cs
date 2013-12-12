using System.Reflection;
using RiverSystem;
using RiverSystem.Catchments;
using TIME.Core.Metadata;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public class ReflectedParameter
    {
        public const string FACTOR_PROPERTY_DISPLAY_NAME = "Factor";
        public const string FACTOR_PROPERTY_NAME = "factor";
        public const string NAME_PROPERTY_DISPLAY_NAME = "Name";
        public const string NAME_PROPERTY_NAME = "name";
        public const string VALUE_PROPERTY_DISPLAY_NAME = VALUE_PROPERTY_NAME;
        public const string VALUE_PROPERTY_NAME = "Value";
        public const string MODELNAME_PROPERTY_DISPLAY_NAME = "Model Name";
        public const string MODELNAME_PROPERTY_NAME = "modelName";

        #region constructors
        public ReflectedParameter( ReflectedAccessor key )
        {
            Init( key );
        }

        public ReflectedParameter( ReflectedAccessor key, double factor )
            : this( key )
        {
            Factor = factor;
        }

        public ReflectedParameter( ReflectedAccessor key, string name )
            : this( key )
        {
            Name = name;
        }

        public ReflectedParameter( ReflectedAccessor key, string name, double factor )
            : this( key, name )
        {
            Factor = factor;
        }

        public ReflectedParameter( AccessorMemberInfo key, object obj, string name, double factor )
        {
            var r = (ReflectedAccessor)ReflectedItem.NewItem( key.member, obj );
            Init( r );
            Factor = factor;
            Name = name;
        }

        public ReflectedParameter( AccessorMemberInfo key, object obj, string name, double factor, FunctionalUnit fu)
        {
            var r = (ReflectedAccessor)ReflectedItem.NewItem( key.member, obj );
            Init( r );
            Factor = factor;
            Name = name;
            FunctionalUnit = fu;
        }

        public ReflectedParameter( AccessorMemberInfo key, object obj, string name )
            : this( key, obj, name, 1.0 )
        {
            // Nothing
        }

        public ReflectedParameter( AccessorMemberInfo key, object obj, string name, FunctionalUnit fu )
            : this(key, obj,name, 1.0, fu)
        {
        }

        #endregion

        #region Properties

        public string Name { get; set; }

        public ReflectedAccessor Key { get; private set; }

        public double Factor { get; set; }

        public FunctionalUnit FunctionalUnit { get; private set; } 

        public double Value
        {
            get { return (double)Key.itemValue; }
        }

        public ICatchment SubCatchment
        {
            get
            {
                if( FunctionalUnit == null )
                    return null;
                return FunctionalUnit.catchment;
            }
        }

        public string ModelName
        {
            get { return AkaAttribute.FindAka( Key.reflectedTarget.targetObject.GetType( ) ); }
        }
        #endregion

        private void Init( )
        {
            Factor = 1.0;
            Name = "";
        }

        private void Init( ReflectedAccessor key )
        {
            Init( );
            Key = key;
            Name = AkaAttribute.FindAka( key.provider as MemberInfo );
        }

        public void UseControllingValue( double controllingValue )
        {
            Key.itemValue = controllingValue * Factor;
        }

        public override bool Equals( object obj )
        {
            ReflectedParameter other = obj as ReflectedParameter;
            if( other == null )
                return false;
            return Key.Equals( other.Key );
        }

        public override int GetHashCode( )
        {
            return Key.GetHashCode( );
        }
    }
}
