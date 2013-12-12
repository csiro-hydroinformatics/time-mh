using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Commons.Collections;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;
using TIME.DataTypes;
using TIME.Tools.Metaheuristics;
using TIME.Tools.Metaheuristics.Objectives;
using TIME.Tools.Metaheuristics.SystemConfigurations;
using TIME.Tools.Optimisation;
using TIME.Tools.Persistence;
using TIME.Tools.Persistence.DataMapping;
using TIME.Tools;
using TIME.Management;
using System.Diagnostics;
using CSIRO.Metaheuristics.UseCases.PEST.FileCreation;
using System.Collections;
using CSIRO.Metaheuristics.Objectives;

namespace CSIRO.Metaheuristics.UseCases.PEST
{


    public class PestEngine <T> : IEvolutionEngine<T>
                where T : TIMEModelParameterSet,IHyperCube<double>  // ICloneableSystemConfiguration
    {
        private const string templateFolder = "CSIRO.Metaheuristics.UseCases.PEST.Resources";
        private const string PESTDELIMITER = "#";
        private int currentGeneration;
        
        private string modelOutputName;
        private ModelRunner mr;
        private string resultFileName;
        private string pestFileName;
        private IObjectiveEvaluator<T> evaluator;
        private TimeSeries observationData;

        private string pestProcessOutput;

        public string PestProcessOutput
        {
            get { return pestProcessOutput; }
        }


        public PestEngine(
            IObjectiveEvaluator<T> objectiveEvaluator,
            IHyperCube<double> startingPoint,
            ModelRunner modelRunner,
            string outputFolder,
            TimeSeries observationTimeSeries,
            string modelOutputParameterName,
            string PestToMetaheuristicsCommandLine,
            int MaxNumberOfIterations,
            string controlSettings = null
            )
        {
            string resultFile;
            this.evaluator = objectiveEvaluator;
            this.observationData = observationTimeSeries;
            this.currentGeneration = 0;

            string pestFile = createPESTFiles(
                startingPoint,
                outputFolder,
                observationTimeSeries,
                modelOutputParameterName,
                PestToMetaheuristicsCommandLine,
                modelRunner,
                MaxNumberOfIterations,
                controlSettings,
                out resultFile);


            this.resultFileName = resultFile;
            this.mr = modelRunner;
            this.pestFileName = pestFile;
            this.modelOutputName = modelOutputParameterName;
            //Execute(modelOutputParameterName, modelRunner, resultFile, pestFile);
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public int CurrentGeneration
        {
            get { return this.currentGeneration; }
        }

        private string createPESTFiles(
            IHyperCube<double> startingPoint,
            String outputFolder,
            TimeSeries observedTimeSeries,
            string modelOutputName,
            string PestToMetaheuristicsCommandLine,
            ModelRunner modelRunner,
            int Iterations,
            string controlSettings,
            out string resultFile
            )
        {
            String templateFile;
            String instructionFile;
            String controlFile;
            String parameterFile;
            String calculatedResultFile;
            createFileNames(outputFolder, out templateFile, out instructionFile, out controlFile,out parameterFile,out calculatedResultFile);
            const int precision = 23;

            // create the three files using the helper classes
            TemplateFileCreator templateCreator = new TemplateFileCreator(templateFile, modelRunner,precision,PESTDELIMITER);
            templateCreator.CreateFile();

            InstructionFileCreator instructionCreator = new InstructionFileCreator(observedTimeSeries, instructionFile);
            instructionCreator.CreateFile();

            ControlFileCreator controlCreator =
                new ControlFileCreator(
                    startingPoint,
                    PestToMetaheuristicsCommandLine,
                    templateFile,
                    parameterFile,
                    instructionFile,
                    calculatedResultFile,
                    modelRunner,
                    controlFile,
                    observedTimeSeries,
                    Iterations,
                    controlSettings
                    );

            controlCreator.CreateFile();
            resultFile = calculatedResultFile;

            return controlFile;
        }

       
        /// <summary>
        /// Helper function that creates the required pest file names
        /// </summary>
        /// <param name="outputFolder">output location of the resulting pest files</param>
        /// <param name="templateFile">input string storage for template pest filename</param>
        /// <param name="instructionFile">input string storage for pest instruction filename</param>
        /// <param name="controlFile">input string storage for pest control filename</param>
        private void createFileNames(String outputFolder, out String templateFile, out String instructionFile, out String controlFile, out String parameterFile, out String calculatedResultFile)
        {
            String folder = String.Concat(Path.GetFullPath(outputFolder).TrimEnd(new char[2] { '\\', '/' }), "\\");
            templateFile = String.Concat(folder, "template.tpl");
            //String modelInput = String.Concat(folder, "modelParameter.xml");
            instructionFile = String.Concat(folder, "instruction.ins");
            controlFile = String.Concat(folder, "control.pst");
            parameterFile = String.Concat(folder, "parameters.xml");
            calculatedResultFile = String.Concat(folder, "results.csv");
        }


        public string GetDescription()
        {
            return "Wrapper around the well known PEST optimisation tool";
        }

        public IOptimizationResults<T> Evolve()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "pest";
            p.StartInfo.Arguments = this.pestFileName;
            p.StartInfo.RedirectStandardOutput = true;

                
            p.Start();
            StreamReader outputReader = p.StandardOutput;
            MessageQueueModelRunner mqmr = new MessageQueueModelRunner(this.resultFileName,this.mr, this.modelOutputName);
            ParameterSet pSet;
            var scores = new List<IObjectiveScores>();

            while (mqmr.RunIteration(out pSet))
            {
                currentGeneration++;
            }

            p.WaitForExit();

            if (0 == currentGeneration)
            {
                throw new PestExecutionFailedException("Pest executed 0 times.", outputReader.ReadToEnd());
                //throw new Exception(String.Format("PEST ran 0 times. Pest Output: \n {0}",outputReader.ReadToEnd())); 
            }

            T sysConfig = (T) new TIMEModelParameterSet(pSet);
            IObjectiveScores<T> score = this.evaluator.EvaluateScore(sysConfig);
            IOptimizationResults<T> result = new CSIRO.Metaheuristics.Optimization.BasicOptimizationResults<T>(new IObjectiveScores[]{score});

            this.pestProcessOutput = outputReader.ReadToEnd();

            return result;
        }

        private T convertParameterSetToHyperCube(ParameterSet pSet)
        {
            throw new NotImplementedException();
        }


    }

    [Serializable]
    public class PestExecutionFailedException : Exception
    {
        public PestExecutionFailedException(string message, string PestOutput) : base (String.Concat(message,PestOutput))
        {
            PestStdOut = PestOutput;
        }
        public string PestStdOut { get; private set; }
    }

}



   