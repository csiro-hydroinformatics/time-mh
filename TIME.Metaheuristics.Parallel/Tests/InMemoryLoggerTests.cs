using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CSIRO.Metaheuristics;
using CSIRO.Metaheuristics.Logging;
using CSIRO.Metaheuristics.Objectives;
using CSIRO.Metaheuristics.Parallel.Objectives;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using NUnit.Framework;
using TIME.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Models.RainfallRunoff.GR4J;
using TIME.Tools.Optimisation;

namespace TIME.Metaheuristics.Parallel.Tests
{
    [TestFixture]
    public class InMemoryLoggerTests
    {
        private MpiSysConfig config;
        private Dictionary<string, string> tags;
        readonly string binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace(@"file:///", string.Empty).Replace(@"file://", @"//"));
        private const string pythonSysPaths = @"[ 'C:\\Program Files (x86)\\IronPython 2.7\\Lib' ,'C:\\Program Files (x86)\\IronPython 2.7\\DLLs' ,'C:\\Program Files (x86)\\IronPython 2.7' ,'C:\\Program Files (x86)\\IronPython 2.7\\Lib\\site-packages' ]";

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            
            config = new MpiSysConfigTIME(new ParameterSet(new GR4J()));
            tags = new Dictionary<string, string> {{"CalibName", "Test"}, {"Category", "Initial Pop"}, {"Message", "testing one two"}};
        }

        [Test]
        //[Ignore("the iron python logger doesn't like my test data. It throws a divide by zero exception")]
        public void TestWriteToCsv()
        {
            //*/
            string filepy = Path.GetTempFileName();
            string filenew = Path.GetTempFileName();
            /*/
            const string filepy = @"E:\Code\AWRA-Calibration\output\filepy.csv";
            const string filenew = @"E:\Code\AWRA-Calibration\output\filenew.csv";
            //*/
            CreateTestLogger(5).CsvSerialise(filenew, "test");
           // LoggingUtils.WriteLoggerContent(CreateTestLogger(5), filepy, binFolder, pythonSysPaths,"test");
        }

        private InMemoryLogger CreateTestLogger(int numEntries)
        {
            InMemoryLogger log = new InMemoryLogger();
            for (int i = 0; i < numEntries; i++)
                log.Write(CreateScores(5, 3), tags);

            return log;
        }

        private IObjectiveScores[] CreateScores(int numScores, int objectivesPerScore)
        {

            MpiSysConfig[] configs = new MpiSysConfig[numScores];
            for (int i = 0; i < numScores; i++)
                configs[i] = config;
            return CreateTestScores(objectivesPerScore, configs);
        }

        public static IObjectiveScores[] CreateTestScores(int objectivesPerScore, MpiSysConfig[] configs)
        {
            int numScores = configs.Length;
            IObjectiveScores[] scores = new IObjectiveScores[numScores];
            for (int i = 0; i < numScores; i++)
            {
                IObjectiveScore[] objectives = new IObjectiveScore[objectivesPerScore];
                for (int j = 0; j < objectivesPerScore; j++)
                    objectives[j] = new DoubleObjectiveScore(String.Format("Score:{0}-{1}", i, j), FakeScore(i + j), true);

                scores[i] = new MpiObjectiveScores(objectives, configs[i]);
            }

            return scores;
        }

        private static double FakeScore(double factor)
        {
            return System.Math.PI * factor;
        }
    }
}
