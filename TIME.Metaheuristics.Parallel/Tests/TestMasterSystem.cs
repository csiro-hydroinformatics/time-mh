using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using CSIRO.Metaheuristics.Utils;
using NUnit.Framework;
using TIME.Core.Metadata;
using TIME.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools.Optimisation;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    public class TestMasterSystem
    {

        private class MockTimeModel : TIME.Core.Model
        {
            [Parameter, Minimum(0), Maximum(1)]
            public double x = 0.1;
            [Parameter, Minimum(0), Maximum(2)]
            public double y = 0.2;

            internal static double getX(int i)
            {
                return (i % 10) * 0.1;
            }

            internal static double getY(int i)
            {
                return (i % 10) * 0.2;
            }
            public override void runTimeStep()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void TestPopulationSeeding()
        {
            var fTmp = Path.GetTempFileName();
            var f = Path.Combine(Path.GetDirectoryName(fTmp), Path.GetFileNameWithoutExtension(fTmp) + ".csv");
            int numScores = 5;
            var pSet = new ParameterSet(new MockTimeModel());
            var expectedMaxBound = 3.0;
            pSet.paramSpec("x").max = expectedMaxBound;
            var pSpaceReference = new MpiSysConfigTIME(pSet);
            IObjectiveScores[] scores = createScores(numScores);
            try
            {
                MetaheuristicsHelper.SaveAsCsv<MpiSysConfig>(scores, f);
                var seeds = GridModelHelper.LoadParameterSets(f, pSpaceReference);
                Assert.AreEqual(numScores, seeds.Length);
                Assert.AreEqual(expectedMaxBound, seeds[0].GetMaxValue("x"));
                Assert.AreEqual(MockTimeModel.getX(4), seeds[4].GetValue("x"), 1e-9);
                Assert.AreEqual(MockTimeModel.getY(3), seeds[3].GetValue("y"), 1e-9);
            }
            finally
            {
                if (File.Exists(f))
                    File.Delete(f);
                if (File.Exists(fTmp))
                    File.Delete(fTmp);
            }
        }

        private IObjectiveScores[] createScores(int p)
        {
            var parameterSets = new MpiSysConfig[p];
            for (int i = 0; i < p; i++)
            {
                var m = new MockTimeModel();
                m.x = MockTimeModel.getX(i);
                m.y = MockTimeModel.getY(i);
                parameterSets[i] = new MpiSysConfigTIME(new ParameterSet(m));
            }
            return InMemoryLoggerTests.CreateTestScores(2, parameterSets);
        }
    }
}
