using System.Collections.Generic;
using NUnit.Framework;
using TIME.DataTypes;
using TIME.Management;
using TIME.Tools;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.SystemConfigurations;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence.DataMapping;
using System.Xml.Serialization;
using System.IO;
using System;

namespace CSIRO.Metaheuristics.UseCases.PEST
{
    [TestFixture]
    class PestTest
    {
        /* 
         //commented out to allow for teamcity. This is because
         // I'm unsure on how to get Teamcity to run generated files and
         // to interface with the PestToMetaheuristics.exe
        [Test]
        public void TestPestEngineNoConfig()
        {
            SimulationXmlFilesRepository repo = new SimulationXmlFilesRepository();
            
            ModelRunner mr = repo.Load("C:/HWB/AWRA/gr4jrepro/dynamic_model_simulation_gr4j_201001.xml");
            
            TimeSeries runoff = NonInteractiveIO.LoadSingleTimeSeries("C:\\pestTests\\runoff.csv");

            SimpleHyperCube startingPoint = CreateStartingPoint(mr);

            int Iterations = 3;
            PestObjectiveEvaluator<TIMEModelParameterSet> evaluator = new PestObjectiveEvaluator<TIMEModelParameterSet>(runoff, mr, "runoff");
            PestEngine<TIMEModelParameterSet> engine = 
                new PestEngine<TIMEModelParameterSet>
                    (evaluator,
                    startingPoint,
                    evaluator.ModelRunner,
                    "c:\\pesttests",
                    evaluator.ObservedData,
                    evaluator.ModelOutputTimeSeries,
                    "c:\\pestTests\\PestToMetaheuristics.exe",
                    Iterations
                    );

            IOptimizationResults<TIMEModelParameterSet> results = engine.Evolve();

        }
        [Test]
        public void TestPestEngineWithIncorrectConfig()
        {

            SimulationXmlFilesRepository repo = new SimulationXmlFilesRepository();

            ModelRunner mr = repo.Load("C:/HWB/AWRA/gr4jrepro/dynamic_model_simulation_gr4j_201001.xml");

            TimeSeries runoff = NonInteractiveIO.LoadSingleTimeSeries("C:\\pestTests\\runoff.csv");

            SimpleHyperCube startingPoint = CreateStartingPoint(mr);

            int Iterations = 3;
            PestObjectiveEvaluator<TIMEModelParameterSet> evaluator = new PestObjectiveEvaluator<TIMEModelParameterSet>(runoff, mr, "runoff");
            PestEngine<TIMEModelParameterSet> engine =
                new PestEngine<TIMEModelParameterSet>
                    (evaluator,
                    startingPoint,
                    evaluator.ModelRunner,
                    "c:\\pesttests",
                    evaluator.ObservedData,
                    evaluator.ModelOutputTimeSeries,
                    "c:\\pestTests\\PestToMetaheuristics.exe",
                    Iterations,
                    "c:\\pestTests\\config.xml"
                    );
            try
            {
                IOptimizationResults<TIMEModelParameterSet> results = engine.Evolve();
                Assert.Fail();
            }
            
            catch (PestExecutionFailedException)
            {
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Need a hyper cube -> simple method to set up with defaults and mean value in parameter range
        /// may need to move elsewhere in a helper class
        /// </summary>
        /// <param name="mr"></param>
        /// <returns></returns>
        public SimpleHyperCube CreateStartingPoint(ModelRunner mr)
        {
            ParameterSet pSet = new ParameterSet(mr.Model);
            List<string> pNames = new List<string>();

            foreach (var pSpec in pSet.sortedEntries)
            {
                pNames.Add(pSpec.Name);
            }

            SimpleHyperCube startingPoint = new SimpleHyperCube(pNames.ToArray());

            foreach (var pSpec in pSet.sortedEntries)
            {
                startingPoint.SetMinMaxValue(pSpec.Name, pSpec.min, pSpec.max, pSpec.Value);
            }

            return startingPoint;
        }
        */
    }
}
