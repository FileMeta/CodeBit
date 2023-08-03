using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CodeBit
{
    internal class JsonXmlWriter : IDisposable
    {
        static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);

        public static JsonXmlWriter Create(Stream stream, bool ownsStream)
        {
            return new JsonXmlWriter(JsonReaderWriterFactory.CreateJsonWriter(stream, UTF8NoBom, ownsStream, true));
        }

        XmlWriter m_xmlWriter;
        bool m_disposed = false;

        public JsonXmlWriter(XmlWriter xmlWriter)
        {
            m_xmlWriter = xmlWriter;
        }

        public void WriteDocumentObjectBegin()
        {
            m_xmlWriter.WriteStartDocument();
            m_xmlWriter.WriteStartElement("root");
            m_xmlWriter.WriteAttributeString("type", "object");
        }

        public void WriteDocumentArrayBegin()
        {
            m_xmlWriter.WriteStartDocument();
            m_xmlWriter.WriteStartElement("root");
            m_xmlWriter.WriteAttributeString("type", "object");
        }

        public void WriteObjectBegin(string propertyName)
        {
            m_xmlWriter.WriteStartElement(propertyName);
            m_xmlWriter.WriteAttributeString("type", "object");
        }

        public void WriteObjectEnd()
        {
            m_xmlWriter.WriteEndElement();
        }

        public void WriteObjectProperty(string propertyName, string value)
        {
            m_xmlWriter.WriteStartElement(propertyName);
            m_xmlWriter.WriteAttributeString("type", "string");
            m_xmlWriter.WriteString(value);
            m_xmlWriter.WriteEndElement();
        }

        public void WriteObjectOptionalProperty(string propertyName, string? value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                WriteObjectProperty(propertyName, value);
            }
        }

        public void WriteObjectArrayStart(string propertyName)
        {
            m_xmlWriter.WriteStartElement(propertyName);
            m_xmlWriter.WriteAttributeString("type", "array");
        }

        public void WriteArrayStringValue(string value)
        {
            m_xmlWriter.WriteStartElement("item");
            m_xmlWriter.WriteAttributeString("type", "string");
            m_xmlWriter.WriteString(value);
            m_xmlWriter.WriteEndElement();
        }

        public void WriteArrayObjectStart()
        {
            m_xmlWriter.WriteStartElement("item");
            m_xmlWriter.WriteAttributeString("type", "object");
        }

        public void WriteArrayArrayStart()
        {
            m_xmlWriter.WriteStartElement("item");
            m_xmlWriter.WriteAttributeString("type", "array");
        }

        public void WriteArrayEnd()
        {
            m_xmlWriter.WriteEndElement();
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_xmlWriter.WriteEndDocument();
                m_xmlWriter.Dispose();
                m_disposed = true;
            }
        }

    }
}
