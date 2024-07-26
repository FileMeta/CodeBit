using System;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace CodeBit
{

    enum JsonNodeType
    {
        End,
        StartObject,
        StartArray,
        Value,
        EndElement
    }

    internal class JsonXmlReader : IDisposable
    {
        public static JsonXmlReader Create(Stream stream)
        {
            return new JsonXmlReader(JsonReaderWriterFactory.CreateJsonReader(stream, Encoding.UTF8, new System.Xml.XmlDictionaryReaderQuotas(), null));
        }

        XmlReader m_xmlReader;
        JsonNodeType m_nodeType;
        string m_name;
        string m_value;

        public JsonXmlReader(XmlReader xmlReader)
        {
            m_xmlReader = xmlReader;
            m_name = string.Empty;
            m_value = string.Empty;
        }

        public JsonNodeType NodeType { get { return m_nodeType; } }
        public string Name { get { return m_name; } }
        public string Value { get { return m_value; } }

        public bool Read()
        {
            m_xmlReader.Read();
            return ToJsonNode();
        }

        public bool Skip()
        {
            if (m_nodeType == JsonNodeType.StartObject || m_nodeType == JsonNodeType.StartArray)
                m_xmlReader.Skip();
            else
                m_xmlReader.Read();
            return ToJsonNode();
        }

        private bool ToJsonNode()
        {
            // Advance to the next node
            m_nodeType = JsonNodeType.End;
            m_name = string.Empty;
            m_value = string.Empty;
            XmlNodeType xmlNodeType;
            for(; ; )
            {
                xmlNodeType = m_xmlReader.NodeType;
                if (xmlNodeType == XmlNodeType.Element || xmlNodeType == XmlNodeType.EndElement)
                    break;
                if (!m_xmlReader.Read()) return false;
            }

            // Return if end element
            if (xmlNodeType == XmlNodeType.EndElement)
            {
                m_nodeType = JsonNodeType.EndElement;
                return true;
            }

            // Get the type of element
            switch (m_xmlReader.GetAttribute("type"))
            {
                case "object":
                    m_nodeType = JsonNodeType.StartObject;
                    break;
                case "array":
                    m_nodeType = JsonNodeType.StartArray;
                    break;
                default:
                    m_nodeType = JsonNodeType.Value;
                    break;
            }

            // Get the name.
            m_name = m_xmlReader.Name;
            if (m_name == "a:item")
                m_name = m_xmlReader.GetAttribute("item") ?? string.Empty;

            if (m_nodeType != JsonNodeType.Value)
                return true;

            // Get the value
            for (; ; )
            {
                if (!m_xmlReader.Read()) break;
                xmlNodeType = m_xmlReader.NodeType;
                if (xmlNodeType == XmlNodeType.Text
                    || xmlNodeType == XmlNodeType.EndElement)
                    break;
            }
            m_value = m_xmlReader.ReadContentAsString();

            return true;
        }

        public void Dispose()
        {
            m_xmlReader.Dispose();
        }
    }
}
