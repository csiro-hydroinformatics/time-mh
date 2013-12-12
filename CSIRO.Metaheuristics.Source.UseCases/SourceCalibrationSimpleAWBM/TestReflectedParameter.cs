using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Source.UseCases.SourceCalibrationSimpleAWBM;
using NUnit.Framework;
using RiverSystem;
using RiverSystem.Catchments;
using RiverSystem.TestingSupport;
using TIME.Models;
using TIME.Models.RainfallRunoff.AWBM;
using TIME.Tools.Reflection;

namespace CSIRO.Metaheuristics.Tests
{
    [TestFixture]
    class TestReflectedParameter
    {        
        [SetUp]
        public void SetUp( )
        {
                   
        }

        [Test]
        public void TestReflectedParameterFactoryChangeValue( )
        {
            var testModel = new AWBM( );    
            var accessorInfoList = MetaParameterSet.KnownParameters( testModel );

            var dic = ReflectedParameterFactory.NewItems( accessorInfoList, testModel );

            testModel.BFI = 0;
            foreach( var item in dic )
            {
                if (item.Key.Equals( "BFI" ))
                {
                    item.Value[0].UseControllingValue( 1.0 );                    
                }                
            }        
            Assert.AreEqual( 1.0 ,testModel.BFI);    
        }

        [Test]
        public void TestMetaParameterSetConstructor()
        {
            RiverSystemScenario testScenario;
            RiverSystemProject testProject;
            TestHelperRiverSystem.getAPreconfiguredTestScenarioWithAWBM( 3, out testProject,
                                                                         out testScenario );
            var testModel = new AWBM( );
            var accessorInfoList = MetaParameterSet.KnownParameters( testModel );
            var metaParameterSet = new MetaParameterSet( testScenario );
            Assert.AreEqual( accessorInfoList.Count, metaParameterSet.AvailableParameters.Count );
        }    

        [TearDown]
        public void Clean()
        {
            
        }
    }
}
