using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TIME.Tools.Optimisation;

namespace CSIRO.Metaheuristics.UseCases.PEST.Playground
{
    class PESTTest
    {

        public void ParameterSerializationTest()
        {
            string parameterName = "imperviousThreshold";
            var model = new TIME.Models.RainfallRunoff.SimHyd.SimHydCs();
            var parameterSet = new ParameterSet(model);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(parameterSet.GetType());
            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();

            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            XmlWriter writer = XmlWriter.Create(builder, settings);

            x.Serialize(writer, parameterSet);
            Console.WriteLine(builder);


            System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.Load(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(builder.ToString())));
            XDocument doc = XDocument.Load(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(builder.ToString())));

            var values = from l in doc.Descendants("ParameterSpecification")
                         select l;

            foreach (XElement v in values)
            {
                var DisplayName = v.Descendants("MemberName").First().Value;
                var Value = v.Descendants("Value").First().Value;
                string spaces = 23 - DisplayName.Length > 0 ? new string(' ', 23 - DisplayName.Length) : "";
                v.Descendants("Value").First().Value = "#" + DisplayName + spaces + "#";
                Console.WriteLine("Display name {0} value {1}", DisplayName, Value);
            }
            XmlWriter xw = XmlWriter.Create(Console.Out, settings);
            
            doc.WriteTo(xw);
            xw.Flush();

            XmlNodeList modelParameterSet = xmlDoc.GetElementsByTagName("ParameterSpecification");

            foreach (XmlNode v in modelParameterSet)
            {

                Console.WriteLine("display name " + v["DisplayName"] + " value = " + v["value"]);
            }

        }
    }
}
