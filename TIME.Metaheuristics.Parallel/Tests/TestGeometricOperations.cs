using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using NUnit.Framework;
using TIME.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools.Metaheuristics.Tests;
using TIME.Tools.Optimisation;
using CSIRO.Metaheuristics.Tests;

namespace TIME.Metaheuristics.Parallel.Tests
{

    [TestFixture]
    public class TestGeometricOperationsMpiSysconfig : AbstractTestGeometricOperations<MpiSysConfig>
    {
        protected override ITestHypercubeFactory<MpiSysConfig> createFactory()
        {
            return new TestHypercubeFactory();
        }

        private class TestHypercubeFactory : ITestHypercubeFactory<MpiSysConfig>
        {
            public MpiSysConfig Create(int dim, int value, int min, int max)
            {
                var result = new MpiSysConfigTIME(new ParameterSet(typeof(TestGeometricOperationsTimePset.SimpleHypercubeModel)));
                for (int i = 0; i < result.parameters.Length; i++)
                {
                    result.parameters[i].min = min;
                    result.parameters[i].max = max;
                }
                return result;
            }
        }

        // This tests the behavior/feature described in https://jira.csiro.au/browse/WRAA-368. 
        // This has been disabled for the time being.
        //[Test]
        //public void TestBoundaryBehavior()
        //{
        //    var centroid = factory.Create(3, 0, 0, 4);
        //    setValues(centroid, 2.0, 2.0, 3.5);
        //    var point = factory.Create(3, 0, 0, 4);
        //    setValues(point, 1.0, 1.0, 2.1);

        //    // test a reflection 'by' the centroid
        //    var newPoint = centroid.HomotheticTransform(point, -1.0);
        //    // by default keep the former behavior
        //    // Assert.IsNull(newPoint);

        //    newPoint = centroid.HomotheticTransform(point, -1.0);
        //    var varNames = point.GetVariableNames();

        //    double delta = 1e-10;
        //    TestAreEqual(new double[] { 3.0, 3.0, 3.1 }, newPoint, varNames, delta);
        //}



    }

}
