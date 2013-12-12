using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.Tools.Persistence;
using TIME.Tools.Persistence.DataMapping;
using TIME.Tools;
using TIME.Tools.Optimisation;
using TIME.DataTypes;
using System.IO;
using NVelocity;
using NVelocity.App;
using System.Xml.Serialization;

namespace CSIRO.Metaheuristics.UseCases.PEST.FileCreation
{
    public class ControlFileCreator : PestFileCreator
    {
        private const String VELOCITY_CONTROL_DATA = "ControlData";

        // required fields to populate the control data object
        private String commandLineExecutable;
        private String templateFile;
        private String parameterFile;
        private String instructionFile;
        private String resultFile;
        private String controlFile;
        private TimeSeries observedTimeSeries;
        private ModelRunner modelRunner;
        private IHyperCube<double> startingPoint;
        private int NumberOfIterations;
        private string xmlSettingsFile;

        public ControlFileCreator(
            IHyperCube<double> StartingPoint,
            String CommandLineExecutable,
            String TemplateFile,
            String ParameterFile,
            String InstructionFile,
            String ResultFile,
            ModelRunner mr,
            String ControlFile,
            TimeSeries ObservationData,
            int Iterations,
            string controlSettings
            )
        {
            this.startingPoint = StartingPoint;
            this.commandLineExecutable = CommandLineExecutable;
            this.templateFile = TemplateFile;
            this.parameterFile = ParameterFile;
            this.instructionFile = InstructionFile;
            this.resultFile = ResultFile;
            this.controlFile = ControlFile;
            this.NumberOfIterations = Iterations;
            this.xmlSettingsFile = controlSettings;
            modelRunner = mr;

            this.observedTimeSeries = ObservationData;
        }

        public override void CreateFile()
        {
            PestControlData controlData;
            if (String.IsNullOrEmpty(this.xmlSettingsFile))
            {
                controlData = new PestControlData();
            }
            else
            {
                FileStream readXmlSettings = new FileStream(this.xmlSettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {

                    XmlSerializer serializer = new XmlSerializer(typeof(PestControlData));
                    controlData = (PestControlData)serializer.Deserialize(readXmlSettings);

                }
                finally
                {
                    readXmlSettings.Close();
                }
            }
                    

            controlData.AddObservationGroupNames(modelRunner.GetRecordedVariableNames());

            //controlData.AddObservationGroupNames((ModelPropertiesOutputRecordingDefinition)modelRunDefinition.Outputs);
            controlData.AddModelIOInformation(templateFile, parameterFile);
            controlData.AddModelIOInformation(instructionFile, resultFile);
            controlData.CreateCommandLine(this.commandLineExecutable, parameterFile);
            controlData.SetDefaultControlValues();
            controlData.AddParameterSet(this.startingPoint);
            controlData.SetMaxNumberOfIterations(this.NumberOfIterations);
            string observationGroupName = controlData.ObservationGroupNames[0];
            controlData.AddObservationalData(observedTimeSeries, observationGroupName);

            VelocityEngine velocity = createNewVelocityEngine();

            // Set up the required velocity data types and parameters

            //TODO: can this be done in the same way as the instruction file?
            Template controlTemplate = velocity.GetTemplate("CSIRO.Metaheuristics.UseCases.PEST.FileCreation.Resources.ControlFile.vm");
            StringWriter writer = new StringWriter();
            VelocityContext controlContext = new VelocityContext();

            controlContext.Put(VELOCITY_CONTROL_DATA, controlData);
            controlTemplate.Merge(controlContext, writer);

            StreamWriter controlWriter = new StreamWriter(controlFile);

            controlWriter.Write(writer);
            writer.Close();
            controlWriter.Close();
        }
    }
}
