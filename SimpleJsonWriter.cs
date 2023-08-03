using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CodeBit
{
    internal class SimpleJsonWriter : IDisposable
    {
        static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);

        TextWriter m_textWriter;
        int m_level = 0;
        bool m_leaveWriterOpen = false;
        bool m_disposed = false;
        bool m_lineHasContent = false;

        public SimpleJsonWriter(Stream stream, bool leaveOpen = false)
        {
            m_textWriter = new StreamWriter(stream, UTF8NoBom, -1, leaveOpen);
            m_leaveWriterOpen = false;
        }

        public SimpleJsonWriter(TextWriter writer, bool leaveOpen = false)
        {
            m_textWriter = writer;
            m_leaveWriterOpen = leaveOpen;
        }

        public void WriteDocumentObjectBegin()
        {
            m_textWriter.Write('{');
            m_level = 1;
        }

        public void WriteDocumentArrayBegin()
        {
            m_textWriter.Write('[');
            m_level = 1;
        }

        public void WriteObjectBegin(string propertyName)
        {
            WriteIndent();
            WriteQuotedEncoded(propertyName);
            m_textWriter.Write(": {");
            ++m_level;
        }

        public void WriteObjectEnd()
        {
            FinishElement('}');
        }

        public void WriteObjectProperty(string propertyName, string value)
        {
            WriteIndent();
            WriteQuotedEncoded(propertyName);
            m_textWriter.Write(": ");
            WriteQuotedEncoded(value);
            m_lineHasContent = true;
        }

        public void WriteObjectOptionalProperty(string propertyName, string? value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                WriteObjectProperty(propertyName, value);
            }
        }

        public void WriteObjectArrayBegin(string propertyName)
        {
            WriteIndent();
            WriteQuotedEncoded(propertyName);
            m_textWriter.Write(": [");
            ++m_level;
        }

        public void WriteArrayStringValue(string value)
        {
            if (m_lineHasContent) m_textWriter.Write(", ");
            WriteQuotedEncoded(value);
            m_lineHasContent = true;
        }

        public void WriteArrayObjectStart()
        {
            WriteIndent();
            m_textWriter.Write('{');
        }

        public void WriteArrayArrayStart()
        {
            WriteIndent();
            m_textWriter.Write('{');
        }

        public void WriteArrayEnd()
        {
            if (m_level == 0) throw new InvalidOperationException("Unbalanced begin and end.");
            --m_level;
            m_textWriter.Write(']');
            m_lineHasContent = true;
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_lineHasContent)
                {
                    m_textWriter.WriteLine();
                }
                if (!m_leaveWriterOpen)
                {
                    m_textWriter.Dispose();
                }
                else
                {
                    m_textWriter.Flush();
                }
                m_disposed = true;
            }
        }

        void WriteIndent()
        {
            if (m_lineHasContent)
            {
                m_textWriter.WriteLine(',');
                m_lineHasContent = false;
            }
            else
            {
                m_textWriter.WriteLine();
            }
            for (int i = 0; i < m_level * 2; ++i)
                m_textWriter.Write(' ');
        }

        void FinishElement(char closer)
        {
            if (m_lineHasContent)
            {
                m_lineHasContent = false;
            }
            if (m_level == 0) throw new InvalidOperationException("Unbalanced begin and end.");
            --m_level;
            WriteIndent();
            m_textWriter.Write(closer);
            m_lineHasContent = true;
        }

        void WriteQuotedEncoded(string value)
        {
            m_textWriter.Write('"');
            int len = value.Length;
            for (int i=0; i<len; ++i)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        m_textWriter.Write("\\\"");
                        break;
                    case '\\':
                        m_textWriter.Write("\\\\");
                        break;
                    case '\b':
                        m_textWriter.Write("\b");
                        break;
                    case '\n':
                        m_textWriter.Write("\n");
                        break;
                    case '\r':
                        m_textWriter.Write("\r");
                        break;
                    case '\t':
                        m_textWriter.Write("\t");
                        break;
                    case '\f':
                        m_textWriter.Write("\f");
                        break;
                    default:
                        m_textWriter.Write(c);
                        break;
                }
            }
            m_textWriter.Write('"');
        }

    }
}
