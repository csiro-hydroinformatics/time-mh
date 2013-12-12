using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.DataTypes;
using NVelocity.App;
using NVelocity;
using System.IO;
using System.Reflection;

namespace CSIRO.Metaheuristics.UseCases.PEST.FileCreation
{
    public class InstructionFileCreator : PestFileCreator
    {
        // TODO: can this be done using assembly 
        private const string TEMPLATEFOLDER = "CSIRO.Metaheuristics.UseCases.PEST.FileCreation.Resources";
        // instruction data template identifier - hard coded to link with template file
        private const string VELOCITY_INSTRUCTION_DATA = "Dates";

        private TimeSeries observationData;
        private String instructionFile;

        public InstructionFileCreator(
            TimeSeries ObservationData,
            String InstructionFile
            )
        {
            this.observationData = ObservationData;
            this.instructionFile = InstructionFile;
        }

        public override void CreateFile()
        {
            VelocityEngine velocity = this.createNewVelocityEngine();
            
            string instructionTemplateFile = Path.Combine(TEMPLATEFOLDER, "InstructionFile.vm");
            Template instructionTemplate = velocity.GetTemplate(instructionTemplateFile);
            StringWriter writer = new StringWriter();
            VelocityContext instructionContext = new VelocityContext();

            // Data type to hold the dates to write 
            // values into template file
            List<string> dateList = new List<string>();

            // Create a set of dates that reflect
            // the expected output time series values
            for (DateTime t = this.observationData.Start;
                t <= this.observationData.End;
                t += this.observationData.timeStep.GetTimeSpan())
            {
                dateList.Add(t.ToString(IsoDateTime.DATE_TSYMBOL_TIME_FORMAT_TO_SECOND));
            }

            // Add the dates to velocity template
            instructionContext.Put(VELOCITY_INSTRUCTION_DATA, dateList.ToArray());

            instructionTemplate.Merge(instructionContext, writer);

            StreamWriter instructionWriter = new StreamWriter(instructionFile);
            instructionWriter.Write(writer);
            instructionWriter.Close();
            writer.Close();
        }
    }
}
