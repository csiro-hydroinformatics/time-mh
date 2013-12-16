using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TIME.Metaheuristics.Parallel.ExtensionMethods;
using NUnit.Framework;
using TIME.Tools.Collections;
using TIME.Tools.Metaheuristics.Persistence;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence;

namespace TIME.Metaheuristics.Parallel.Tests
{
    /// <summary>
    ///   Some tests for the GlobalDef class.
    /// </summary>
    [TestFixture]
    public class GlobalDefinitionTests
    {
        #region Setup/Teardown

        [SetUp]
        public void Setup()
        {
            globalDef = new GlobalDefinition();
        }

        #endregion

        private GlobalDefinition globalDef;

        public static void AssertThatDefinitionsAreEquivalent(GlobalDefinition a, GlobalDefinition b)
        {
            // compare the catchments
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < b.Count; i++)
            {
                CatchmentDefinition aCatchment = a[i];
                CatchmentDefinition bCatchment = b[i];
                Assert.That(aCatchment.Id, Is.EqualTo(bCatchment.Id));
                Assert.That(aCatchment.Cells.Count, Is.EqualTo(bCatchment.Cells.Count));

                for (int j = 0; j < bCatchment.Cells.Count; j++)
                {
                    Assert.That(aCatchment.Cells[j].Id, Is.EqualTo(bCatchment.Cells[j].Id));
                }
            }

            // Compare the cells
            List<CellDefinition> aCells = a.GetFlatCellList();
            List<CellDefinition> bCells = b.GetFlatCellList();
            Assert.That(aCells.Count, Is.EqualTo(bCells.Count));
            for (int i = 0; i < bCells.Count; i++)
            {
                CellDefinition ac = aCells[i];
                CellDefinition bc = bCells[i];
                Assert.That(ac.Id, Is.EqualTo(bc.Id));
                AssertThatModelRunDefinitionsAreEqual(ac.ModelRunDefinition, bc.ModelRunDefinition);
            }
        }

        private static void AssertThatModelRunDefinitionsAreEqual(XmlSerializableModelRunDefinition a, XmlSerializableModelRunDefinition b)
        {
            Assert.That(a.StartDate, Is.EqualTo(b.StartDate));
            Assert.That(a.EndDate, Is.EqualTo(b.EndDate));

            // inputs
            ModelInputsDefinition ai = (ModelInputsDefinition)a.Inputs;
            ModelInputsDefinition bi = (ModelInputsDefinition)b.Inputs;
            Assert.That(ai.CatchmentIdentifier, Is.EqualTo(bi.CatchmentIdentifier));
            Assert.That(ai.StartDate, Is.EqualTo(bi.StartDate));
            Assert.That(ai.EndDate, Is.EqualTo(bi.EndDate));
            Assert.That(ai.NcIndexVarname, Is.EqualTo(bi.NcIndexVarname));
            Assert.That(ai.NetCdfDataFilename, Is.EqualTo(bi.NetCdfDataFilename));
            Assert.That(ai.ModelVarToNcVar, Is.EquivalentTo(bi.ModelVarToNcVar));
            Assert.That(ai.ModelVarToNcVar.Count, Is.GreaterThan(0));
            Assert.That(ai.CellIdentifiers, Is.Not.Null);
            Assert.That(bi.CellIdentifiers, Is.Not.Null);
            Assert.That(ai.CellIdentifiers, Is.EquivalentTo(bi.CellIdentifiers));

            // state init
            SimpleStateForcingInitialization asi = (SimpleStateForcingInitialization)a.StateInitialization;
            SimpleStateForcingInitialization bsi = (SimpleStateForcingInitialization)b.StateInitialization;
            Assert.That(asi.InitialStates, Is.EquivalentTo(bsi.InitialStates));
            Assert.That(asi.InitialStates.Count, Is.GreaterThan(0));

            // outputs
            ModelPropertiesOutputRecordingDefinition ao = (ModelPropertiesOutputRecordingDefinition)a.Outputs;
            ModelPropertiesOutputRecordingDefinition bo = (ModelPropertiesOutputRecordingDefinition)b.Outputs;
            Assert.That(ao.RecordedModelOutputs, Is.EquivalentTo(bo.RecordedModelOutputs));
            Assert.That(ao.RecordedModelOutputs.Count, Is.GreaterThan(0));

            // parameters
            ParameterSet ap = ((SimpleParameterization)a.Parameterization).ParameterSet;
            ParameterSet bp = ((SimpleParameterization)b.Parameterization).ParameterSet;
            Assert.That(ap.Attributes, Is.EquivalentTo(bp.Attributes));
            Assert.That(ap.modelType, Is.SameAs(bp.modelType));
            Assert.That(ap.Count, Is.EqualTo(bp.Count));
        }

        [Test]
        public void AddCatchment()
        {
            GlobalDefinition gd = new GlobalDefinition();
            CatchmentDefinition catchment = new CatchmentDefinition { Id = "catchment-124" };

            const int numCells = 9;
            for (int cells = 0; cells < numCells; cells++)
            {
                CellDefinition cell = new CellDefinition { Id = "cell-" + cells };
                catchment.Cells.Add(cell);
            }

            gd.AddCatchment(catchment);

            Assert.That(gd.Count, Is.EqualTo(1));

            List<CellDefinition> gdCells = gd.GetFlatCellList();
            Assert.AreEqual(gdCells.Count, numCells);
            foreach (CellDefinition cell in gdCells)
            {
                Assert.That(catchment.Cells, Contains.Item(cell));
            }
        }

        [Test]
        public void DefaultCounts()
        {
            Assert.That(globalDef.Count, Is.EqualTo(0));
            Assert.That(globalDef.GetFlatCellList().Count, Is.EqualTo(0));
        }

        [Test]
        public void TestCellLists()
        {
            GlobalDefinition gd = new GlobalDefinition();
            gd.RandomCatchments(7);
            List<CellDefinition> gdCells = gd.GetFlatCellList();
            foreach (CatchmentDefinition catchment in gd)
                Assert.That(catchment.Cells, Is.SubsetOf(gdCells));
        }

        [Test]
        public void CellsHaveCorrectParentCatchmentId()
        {
            globalDef.RandomCatchments(3, 1, 5);
            foreach (CatchmentDefinition cd in globalDef)
                foreach (CellDefinition cell in cd.Cells)
                    Assert.That(cell.CatchmentId, Is.EqualTo(cd.Id));
        }

        [Test]
        public void XmlSerialisationRoundTrip()
        {
            GlobalDefinition gd = new GlobalDefinition();
            gd.RandomCatchments(5,1,5);
            string sgd = gd.XmlSerialize();
            GlobalDefinition result = SerializationHelper.XmlDeserialize<GlobalDefinition>(sgd);

            List<CellDefinition> gdCells = gd.GetFlatCellList();
            foreach (CatchmentDefinition catchment in gd)
                Assert.That(catchment.Cells, Is.SubsetOf(gdCells));

            List<CellDefinition> rCells = result.GetFlatCellList();
            foreach (CatchmentDefinition catchment in result)
                Assert.That(catchment.Cells, Is.SubsetOf(rCells));

            foreach (CatchmentDefinition cd in result)
                foreach (CellDefinition cell in cd.Cells)
                    Assert.That(cell.CatchmentId, Is.EqualTo(cd.Id));

            AssertThatDefinitionsAreEquivalent(result, gd);
        }

        [Test]
        public void XmlSerialisationToFileRoundTrip()
        {
            GlobalDefinition gd = new GlobalDefinition();
            gd.RandomCatchments(3, 1, 5);
            const string filename = @"E:\Code\AWRA-Calibration\output\gd.xml";
            gd.XmlSerialize(filename);
            GlobalDefinition result = SerializationHelper.XmlDeserialize<GlobalDefinition>(new FileInfo(filename));
            AssertThatDefinitionsAreEquivalent(result, gd);

            // test the serialisation of the catchment id
            foreach (CatchmentDefinition cd in result)
                foreach (CellDefinition cell in cd.Cells)
                    Assert.That(cell.CatchmentId, Is.EqualTo(cd.Id));
        }

        [Test]
        public void BinarySerialisationRoundTrip()
        {
            GlobalDefinition gd = new GlobalDefinition();
            gd.RandomCatchments(5, 1, 5);

            BinaryFormatter formatter = new BinaryFormatter();
            GlobalDefinition result;
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, gd);
                stream.Position = 0;
                result = (GlobalDefinition) formatter.Deserialize(stream);
            }

            List<CellDefinition> rCells = result.GetFlatCellList();
            foreach (CatchmentDefinition catchment in result)
            {
                Assert.That(catchment.Cells, Is.SubsetOf(rCells));
                foreach (CellDefinition cell in catchment.Cells)
                    Assert.That(cell.CatchmentId, Is.EqualTo(catchment.Id));
            }

            AssertThatDefinitionsAreEquivalent(result, gd);
        }

        [Test]
        public void XmlSerialiseDictionary()
        {
            SerializableDictionary<string, int> d = new SerializableDictionary<string, int> {{"a", 1}, {"b", 2}};
            string sd = d.XmlSerialize();
            SerializableDictionary<string, int> result = SerializationHelper.XmlDeserialize<SerializableDictionary<string, int>>(sd);
            Assert.That(result["a"], Is.EqualTo(1));
            Assert.That(result["b"], Is.EqualTo(2));
        }
      
    }
}