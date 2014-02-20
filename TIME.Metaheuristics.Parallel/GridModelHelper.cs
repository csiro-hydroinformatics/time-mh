using System.Collections.Generic;
using System.Linq;
using CSIRO.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Metaheuristics.Parallel.Execution;
using TIME.Metaheuristics.Parallel.ExtensionMethods;
using TIME.Metaheuristics.Parallel.SystemConfigurations;
using TIME.Tools;
using TIME.Tools.Metaheuristics.Persistence.Gridded;
using CSIRO.Metaheuristics;
using System.IO;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence;
using TIME.Tools.Persistence.DataMapping;
using System;

namespace TIME.Metaheuristics.Parallel
{
    public class GridModelHelper
    {
        public static MpiSysConfigTIME LoadParameterSpace(string filename)
        {
            return new MpiSysConfigTIME(SerializationHelper.XmlDeserialize<ParameterSet>(new FileInfo(filename)));
        }

        public static ICatchmentCellModelRunner CreateCellEvaluator(CellDefinition cellDefinition)
        {
            ModelRunner runner = SimulationXmlFilesRepository.BuildModelRunner(cellDefinition.ModelRunDefinition);
            return new GridCellModel(runner, cellDefinition);
        }

        public static void WriteParameters<T>(IEnumerable<IObjectiveScores> scores, string startfilename, string outfilename) where T : IHyperCube<double>
        {
            var pset = InputOutputHelper.DeserializeFromXML<ParameterSet>(startfilename);
            var firstscore = scores.FirstOrDefault();
            var firstpoint = (T)firstscore.GetSystemConfiguration();
            var varnames = firstpoint.GetVariableNames();
            foreach (var variable in varnames)
                pset[variable] = firstpoint.GetValue(variable);

            InputOutputHelper.SerializeAsXML<ParameterSet>(pset, outfilename);

        }

        public static GlobalDefinition LoadGlobalDefinition(string filename)
        {
            return InputOutputHelper.DeserializeFromXML<GlobalDefinition>(filename);
        }


        internal static MpiSysConfig[] LoadParameterSets(FileInfo fileInfo, MpiSysConfigTIME TemplateParameterSet)
        {
            if (fileInfo == null)
                return new MpiSysConfig[0];
            var filename = fileInfo.FullName;
            var ext = Path.GetExtension(filename).ToLower();
            if (ext == ".xml")
            {
                var pSets = SerializationHelper.XmlDeserialize<ParameterSet[]>(filename, new[]{typeof(ParameterSet)});
                var result = new MpiSysConfig[pSets.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    var p = new MpiSysConfigTIME(TemplateParameterSet);
                    p.CopyValuesFrom(pSets[i]);
                    result[i] = p;
                }
                return result;
            }
            else if (ext == ".csv")
            {
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
    }
}