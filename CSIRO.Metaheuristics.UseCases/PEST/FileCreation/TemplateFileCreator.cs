using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TIME.Tools;
using TIME.Tools.Optimisation;

namespace CSIRO.Metaheuristics.UseCases.PEST.FileCreation
{

    public class TemplateFileCreator : PestFileCreator
    {
        private String fileName;
        private ModelRunner modelRunner;
        private int valuePrecision;
        private String pestDelimiter;

        public TemplateFileCreator
            (
                String templateFileName,
                ModelRunner mr,
                int precision,
                String delimiter
            )
        {
            this.fileName = templateFileName;
            this.modelRunner = mr;
            this.valuePrecision = precision;
            this.pestDelimiter = delimiter;
        }


        public override void CreateFile()
        {
            // create default parameter set
            ParameterSet parameterSet = new ParameterSet(this.modelRunner.Model);
            const String TEMPLATEHEADERPART = "ptf";
            StringBuilder modelInput = createModelInputFile(parameterSet);

            XDocument templateDoc = createTemplateXDoc(this.valuePrecision, this.pestDelimiter, modelInput);

            saveXDocToDisk(templateDoc, this.fileName, String.Concat(TEMPLATEHEADERPART, " ", this.pestDelimiter));
        }

        private void saveXDocToDisk(
            XDocument doc,
            String fileName,
            String header
            )
        {
            // write XDoc to disk
            MemoryStream stream = new MemoryStream();

            doc.Save(stream);

            // reset stream to the beginning position so that this file can be written out as well
            stream.Position = 0;

            StreamWriter writer = new StreamWriter(fileName);

            // create a StreamReader such that a string representation
            // of the XDocument can be made to write out to disk

            StreamReader reader = new StreamReader(stream);
            String templateString = reader.ReadToEnd();

            // Write the header to stream only if it is give
            if (String.IsNullOrEmpty(header))
            {
                writer.WriteLine(header);
            }

            writer.WriteLine(header);
            writer.Write(templateString);

            // close all open streams related to the template file
            writer.Close();
            reader.Close();
            stream.Close();
        }

        private XDocument createTemplateXDoc(
            int precision,
            String delimiter,
            StringBuilder modelInput
            )
        {
            // create a Linq XDocument to parse serialized parameter set
            XDocument templateDoc = XDocument.Load(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(modelInput.ToString())));

            var values = from l in templateDoc.Descendants("ParameterSpecification")
                         select l;

            // Loop through the Parameters, modifying the
            // value string to a PEST delimiter
            foreach (XElement v in values)
            {
                String DisplayName = v.Descendants("MemberName").First().Value;

                string spaces = precision - DisplayName.Length > 0 ? new string(' ', precision - DisplayName.Length) : "";
                v.Descendants("Value").Descendants("double").First().Value = delimiter + DisplayName + spaces + delimiter;
            }
            return templateDoc;
        }


        private StringBuilder createModelInputFile(
            ParameterSet parameterSet
            )
        {
            XmlSerializer parameterSetSerializer = new XmlSerializer(parameterSet.GetType());
            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            XmlWriter writer = XmlWriter.Create(builder, settings);

            // create settings to make output model file alittle more
            // human readable (maybe not nessisary but makes for easier debugging)
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            parameterSetSerializer.Serialize(writer, parameterSet);
            return builder;
        }
    }
}
