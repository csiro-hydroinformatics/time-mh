using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIME.Tools.Optimisation;
using TIME.Management;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using TIME.Tools.Persistence;
using TIME.Tools;
using TIME.Tools.Persistence.DataMapping;
using TIME.DataTypes;

namespace PestToMetaheuristics
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                usage();
                return;
            }

            //MessageQueueInterface msmqInterface = new MessageQueueInterface();

            // deserialize parameter set:

            ParameterSet pSet = new ParameterSet();
            XmlReaderSettings settings = new XmlReaderSettings();
            XmlSerializer serializer = new XmlSerializer(typeof(ParameterSet));

            StreamReader reader = new StreamReader(args[0]);
            MemoryStream newStream = new MemoryStream();
            StreamWriter sw = new StreamWriter(newStream);
            
            string text;
            while ((text = reader.ReadLine()) != null)
            {
                // clean text of fortran nasties
                // in this case doubles are represented as doubles such as
                // 8.290000D+02 for 829.0
                // C# can handle 8.290000E+02 so we replace all
                // instances of the D with E
                text = text.Replace("D+", "E+");
                text = text.Replace("D-", "E-");
                sw.WriteLine(text);
            }
            sw.Flush();
            newStream.Seek(0, SeekOrigin.Begin); // move to beginning

            StreamReader newReader = new StreamReader(newStream);
            
            pSet = (ParameterSet)serializer.Deserialize(newReader);
            reader.Close();

            MessageQueueInterface mq = new MessageQueueInterface();
            mq.RunModelExecution(pSet);
        }



        private static void usage()
        {
            Console.Out.WriteLine("usage: PestToMetaheuristics.exe <ParameterFile> ");
        }
    }
}
