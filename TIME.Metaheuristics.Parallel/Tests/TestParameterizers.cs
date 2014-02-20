using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using NUnit.Framework;
using TIME.Metaheuristics.Parallel.SystemConfigurations;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    public class TestParameterizers
    {
        private class TestMpiSysConfigTIME : MpiSysConfigTIME
        {
            public TestMpiSysConfigTIME()
                : base()
            { this.parameters = new MpiParameterConfig[0]; }
            public void Add(string name, double value, double min, double max)
            {
                var tmp = this.parameters.ToList();
                tmp.Add(new MpiParameterConfig { name = name, value = value, min = min, max = max });
                this.parameters = tmp.ToArray();
            }
        }

        [Test]
        public void TestCloneMpiSysConfigTIME()
        {
            var a = new TestMpiSysConfigTIME();
            a.Add("x", 0.1, 0.0, 1.0);
            a.Add("y", 10, 1, 100);
            var b = (MpiSysConfigTIME)a.Clone();

            Assert.AreEqual(0.0, b.GetMinValue("x"));
            Assert.AreEqual(0.1, b.GetValue("x"));
            Assert.AreEqual(1.0, b.GetMaxValue("x"));

            a.SetValue("x", 0.2);

            Assert.AreEqual(0.1, b.GetValue("x"));
        
        }
    }
}
