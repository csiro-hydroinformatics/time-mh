using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CSIRO.Metaheuristics.Source.SourceCalibrationSimpleAWBM.SourceCalibrationSimpleAWBM;
using RiverSystem;
using RiverSystem.Catchments;
using RiverSystem.Management.ExpressionBuilder;
using TIME.Core.Metadata;
using TIME.Models;
using TIME.Models.RainfallRunoff.AWBM;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM
{
    public class LumpedAWBMFactory : ICandidateFactory<MetaParameterSet>
    {
        private RiverSystemScenario scenario;
        public LumpedAWBMFactory( RiverSystemScenario scenario, IRandomNumberGeneratorFactory rng )
        {
            this.scenario = scenario;
            this.random = rng.CreateRandom();
           GetMetaParameterSet();
        }

        private Random random;

        #region ICandidateFactory<MetaParameterSet> Members

        public class MinMax
        {
            public MinMax(double min, double max)
            {
                this.Min = min;
                this.Max = max;
            }

            public double Min { get; private set; }
            public double Max { get; private set; }
        }
        private MetaParameterSet parameterSet;
        public MetaParameterSet CreateRandomCandidate( )
        {
            MetaParameterSet metaParameterSet = new MetaParameterSet(this.scenario);
            //RainfallRunoffModel[] instances = findAllAwbmInstances( scenario );
            //List<AccessorMemberInfo> accessorInfoList = MetaParameterSet.KnownParameters( instances[0] );            
            //Dictionary<string, MinMax> bounds = getBounds(accessorInfoList);
            //foreach( var model in instances )
            //{
            //    var dic = ReflectedParameterFactory.NewItems( accessorInfoList, model );
            //    foreach (var paramName in dic.Keys)
            //    {
            //        var minMax = bounds[paramName];
            //        metaParameterSet.AddMappingRelation(paramName, dic[paramName], minMax.Min, minMax.Max);                    
            //    }
            //}
            ////SourceUtility.PrintMetaParameterSet(metaParameterSet);
            //randomize(metaParameterSet);


            int i = 0;
            foreach (Catchment c in scenario.Network.catchmentList)
            {
                foreach (StandardFunctionalUnit fu in c.FunctionalUnits)
                {
                    var list = AccessorMemberInfo.GetFieldsAndPropertiesList(fu.rainfallRunoffModel.GetType(), typeof(double), typeof(ParameterAttribute));
                    foreach (MemberInfo m in list)
                    {
                        string metaParameterName = "$tag" + i++;
                        MinMax mm = getBounds(m);
                        double equation = getRand(mm.Min, mm.Max);
                        metaParameterSet.MasterKnobs.Add(metaParameterName, equation);
                    }
                }
            }
            return metaParameterSet;
        }

        private Dictionary<string, MinMax> getBounds( IEnumerable<AccessorMemberInfo> accessorInfoList )
        {
            var result = new Dictionary<string, MinMax>();
            foreach (var accessorMemberInfo in accessorInfoList)
            {
                result.Add(accessorMemberInfo.Name,
                           new MinMax(MinimumAttribute.MinimumFor(accessorMemberInfo),
                                      MaximumAttribute.MaximumFor(accessorMemberInfo)));
            }
            return result;
        }

        private MinMax getBounds(MemberInfo memberInfo)
        {
            return new MinMax(MinimumAttribute.MinimumFor(memberInfo), MaximumAttribute.MaximumFor(memberInfo));
        }

        private void randomize( MetaParameterSet metaParameterSet )
        {
            foreach (var variableName in metaParameterSet.GetVariableNames())
            {
                metaParameterSet.SetValue(variableName, getRand(metaParameterSet.GetMinValue(variableName), metaParameterSet.GetMaxValue(variableName)));
            }
        }

        private double getRand( double min, double max )
        {
            return min + random.NextDouble()*(max - min);
        }

        private static RainfallRunoffModel[] findAllAwbmInstances( RiverSystemScenario scenario )
        {
            return RiverSystem.TestingSupport.TestHelperRiverSystem.RainfallRunoffModels(scenario).FindAll( (x => (x is  AWBM)) ).ToArray();
        }

        private MetaParameterSet GetMetaParameterSet()
        {
            var metaParameterSet = new MetaParameterSet( scenario );
            return metaParameterSet;
        }

        public static void printPrams(RiverSystemScenario scenario, ISystemConfiguration pSet)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Catchment c in scenario.Network.catchmentList)
            {
                foreach( StandardFunctionalUnit fu in c.FunctionalUnits )
                {
                    List<AccessorMemberInfo> accessorInfoList = MetaParameterSet.KnownParameters(fu.rainfallRunoffModel);
                    foreach (var accessorMemberInfo in accessorInfoList)
                    {
                        sb.AppendLine(c.Name + ":" + fu.definition.Name + ":" + accessorMemberInfo.Name + ":" + accessorMemberInfo.GetValue(fu.rainfallRunoffModel));
                    }
                    sb.AppendLine("-----");                    
                }
            }
            (pSet as MetaParameterSet).RealConfiguration = sb.ToString();
            Console.WriteLine(sb.ToString());
        }
        #endregion
    }
}
