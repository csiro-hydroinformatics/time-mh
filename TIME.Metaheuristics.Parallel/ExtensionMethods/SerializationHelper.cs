using System;
using System.IO;
using System.Xml.Serialization;

namespace TIME.Metaheuristics.Parallel.ExtensionMethods
{
    // TODO: probably duplicates CSIRO.Utilities and/or TIME.Tools.*
    public static class SerializationHelper
    {
        /// <summary>
        /// Serialize any serializable object to an Xml string.
        /// </summary>
        /// <param name="data">The data instance to serialize.</param>
        /// <param name="types">The additional types. Refer to the <see cref="XmlSerializer"/> documentation for details.</param>
        /// <returns>The string of Xml data</returns>
        public static string XmlSerialize(this object data, Type[] types = null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(data.GetType(), types);
            TextWriter writer = new StringWriter();
            xmlSerializer.Serialize(writer, data);
            return writer.ToString();
        }

        /// <summary>
        /// Serialize any serializable object to a file.
        /// </summary>
        /// <param name="data">The data instance to serialize.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="types">The additional types. Refer to the <see cref="XmlSerializer"/> documentation for details.</param>
        public static void XmlSerialize(this object data, String filename, Type[] types = null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(data.GetType(), types);
            TextWriter writer = new StreamWriter(filename);
            xmlSerializer.Serialize(writer, data);
            writer.Close();
        }

        /// <summary>
        /// Deserialize an object from an xml string.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="xmlData">The XML data.</param>
        /// <param name="types">The additional types. Refer to the <see cref="XmlSerializer"/> documentation for details.</param>
        /// <returns>The deserialized object</returns>
        public static T XmlDeserialize<T>(string xmlData, Type[] types = null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), types);
            TextReader reader = new StringReader(xmlData);
            return (T) xmlSerializer.Deserialize(reader);
        }

        /// <summary>
        /// Deserialize an object from a file.
        /// Note that a <see cref="FileInfo"/> parameter is used rather than a filename string to disambiguate this method from the string
        /// deserialization method.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="fileInfo">The file info.</param>
        /// <param name="types">The additional types. Refer to the <see cref="XmlSerializer"/> documentation for details.</param>
        /// <returns>The deserialized object</returns>
        public static T XmlDeserialize<T>(FileInfo fileInfo, Type[] types = null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), types);
            TextReader reader = new StreamReader(fileInfo.FullName);
            return (T)xmlSerializer.Deserialize(reader);
        }
    }
}
