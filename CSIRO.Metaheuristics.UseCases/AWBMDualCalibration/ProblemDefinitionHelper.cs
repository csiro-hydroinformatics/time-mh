using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TIME.DataTypes;
using TIME.Tools;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.Objectives;
using TIME.Tools.ModelExecution;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence.DataMapping;
using TIME.Tools.Persistence;
using System.Reflection;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.UseCases.AWBMDualCalibration
{
    public class ProblemDefinitionHelper
    {
        //internal static TIMESystemRunner BuildSystem( )
        //{
        //    return BuildSystem( ModelRunDefFile );
        //}

        internal static ModelRunner BuildSystem( string modelRunDefnFile, string modelOutputName )
        {
            var modelRun = new SimulationXmlFilesRepository( ).Load( modelRunDefnFile );
            modelRun.Record( modelOutputName );
            return modelRun;
        }

        private static XmlSerializableModelRunDefinition LoadRunDefinition( string modelRunDefnFile )
        {
            return InputOutputHelper.DeserializeFromXML<XmlSerializableModelRunDefinition>( modelRunDefnFile );
        }

        //private static TIMESystemRunner buildModelRunner( ModelRunDefinition runDefn )
        //{
        //    ModelRunner result = new ModelRunner( runDefn.ModelType );
        //    foreach( var memberName in runDefn.InputConfiguration.Keys )
        //        result.play( AccessorMemberInfo.getMemberInfo( runDefn.ModelType, memberName ).member, runDefn.InputConfiguration[memberName] );
        //    result.SetPeriod( runDefn.StartDate, runDefn.EndDate );
        //    return new TIMESystemRunner( result );
        //}


        public static IClonableObjectiveEvaluator<ICloneableSystemConfiguration> BuildEvaluator( string modelRunDefnFile, string modelOutputName, string runoffToMatchFile )
        {
            var modelRun = BuildSystem( modelRunDefnFile, modelOutputName );
            var toMatch = loadObservedRunoff( LoadRunDefinition( modelRunDefnFile ), runoffToMatchFile );
            var result = new MultiObjectiveEvaluator<ICloneableSimulation, ICloneableSystemConfiguration>( CloneableSimulationWrapperFactory.CreateWrapper(modelRun) );
            result.Add( ObjectiveFunctionsFactory.CreateNashSutcliffeEvaluator( modelOutputName, toMatch ) );
            result.Add( ObjectiveFunctionsFactory.CreateBiasEvaluator( modelOutputName, toMatch ) );
            return result;
        }

        private static TimeSeries loadObservedRunoff( XmlSerializableModelRunDefinition defn, string runoffToMatchFile )
        {
            TimeSeries toMatch = InputOutputHelper.LoadTimeSeries( runoffToMatchFile );
            toMatch = toMatch.extract( defn.StartDate, defn.EndDate );
            return toMatch;
        }

    }
}
