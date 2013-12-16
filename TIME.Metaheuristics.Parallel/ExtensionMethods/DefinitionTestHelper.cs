using System;
using TIME.Models.RainfallRunoff.GR4J;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence;

namespace TIME.Metaheuristics.Parallel.ExtensionMethods
{
    /// <summary>
    /// Extension methods to assist with testing the globalDef classes.
    /// </summary>
    static class DefinitionTestHelper
    {
        private static readonly Random Rand = new Random();

        static public void RandomCatchments(this GlobalDefinition globalDef, int numCatchments, int minCellCount = 1, int maxCellCount = 50)
        {
            for (int i = 0; i < numCatchments; i++)
            {
                CatchmentDefinition catchment = new CatchmentDefinition { Id = "catchment-" + Rand.Next() };

                int numCells = Rand.Next(minCellCount, maxCellCount + 1);
                for (int cells = 0; cells < numCells; cells++)
                {
                    CellDefinition cell = new CellDefinition { Id = "cell-" + Rand.Next(), CatchmentId = catchment.Id };
                    cell.ModelRunDefinition.PopulateWithTestData();
                    catchment.Cells.Add(cell);
                }

                globalDef.AddCatchment(catchment);
            }
        }

        static string NextString(this System.Random r)
        {
            return r.Next().ToString();
        }

        static public void PopulateWithTestData(this XmlSerializableModelRunDefinition mrd)
        {
            mrd.StartDate = new DateTime(Rand.Next(1900, 1980), Rand.Next(1, 12), Rand.Next(1, 29));
            mrd.EndDate = new DateTime(Rand.Next(1980, 2012), Rand.Next(1, 12), Rand.Next(1, 29));
            mrd.FullyQualifiedModelName = Rand.NextString();

            // Inputs
            ModelInputsDefinition inputs = (ModelInputsDefinition)mrd.Inputs;

            inputs.CatchmentIdentifier = Rand.NextString();
            inputs.StartDate = mrd.StartDate;
            inputs.EndDate = mrd.EndDate;
            inputs.NcIndexVarname = Rand.NextString();
            inputs.NetCdfDataFilename = Rand.NextString();
            for (int i = 0; i < Rand.Next(1, 5); i++)
                inputs.ModelVarToNcVar.Add(Rand.NextString(), Rand.NextString());

            int numCells = Rand.Next(1, 4);
            inputs.CellIdentifiers = new string[numCells];
            for (int i = 0; i < numCells; i++)
                inputs.CellIdentifiers[i] = Rand.NextString();

            // state init
            SimpleStateForcingInitialization state = (SimpleStateForcingInitialization)mrd.StateInitialization;
            for (int i = 0; i < Rand.Next(1, 5); i++)
                state.InitialStates.Add(Rand.NextString(), Rand.NextDouble());

            // outputs
            ModelPropertiesOutputRecordingDefinition outputs = (ModelPropertiesOutputRecordingDefinition)mrd.Outputs;
            for (int i = 0; i < Rand.Next(1, 5); i++)
                outputs.RecordedModelOutputs.Add(Rand.NextString());

            // parameterisation
            ((SimpleParameterization)mrd.Parameterization).ParameterSet = new ParameterSet(typeof(GR4J));
        }
    }
}
