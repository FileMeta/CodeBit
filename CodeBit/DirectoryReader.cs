using FileMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;

namespace CodeBit
{
    internal class DirectoryReader : IDisposable
    {
        const string err_unexpectedEnd = "Unexpected end of Directory file.";
        const string key_itemList = "itemListElement";

        enum State
        {
            PreRead,
            InMetadata,
            AfterMetadata,
            InItemList,
            InItem,
            End,
            Error
        }

        JsonXmlReader m_jsonReader;
        State m_state;

        public DirectoryReader(Stream stream)
        {
            m_jsonReader = JsonXmlReader.Create(stream);
            m_state = State.PreRead;
        }

        public DirectoryMetadata ReadDirectory()
        {
            if (m_state != State.PreRead) throw new InvalidOperationException("ReadMetadata must precede other read operations.");
            m_state = State.Error;
            JsonRead();
            if (m_jsonReader.NodeType != JsonNodeType.StartObject) throw new ApplicationException("Invalid Directory File Format.");
            m_state = State.InMetadata;

            // Read metadata
            var metadata = new DirectoryMetadata();
            while (m_state == State.InMetadata)
            {
                JsonRead();
                switch (m_jsonReader.NodeType)
                {
                    case JsonNodeType.Value:
                        metadata.AddValue(m_jsonReader.Name, m_jsonReader.Value);
                        break;

                    case JsonNodeType.StartArray:
                        if (m_jsonReader.Name == key_itemList)
                        {
                            m_state = State.AfterMetadata;
                        }
                        else
                        {
                            m_jsonReader.Skip();
                        }
                        break;

                    case JsonNodeType.StartObject:
                        m_jsonReader.Skip();
                        break;

                    case JsonNodeType.EndElement:
                        m_state = State.End;
                        break;

                    default:
                        ThrowUnexpected();
                        break;

                }
            }
            return metadata;
        }

        public CodeBitMetadata? ReadCodeBit()
        {
            if (m_state == State.PreRead)
            {
                ReadDirectory();    // Read and throw away the metadata. Not efficient memory-wise but not likely to happen either
            }

            if (m_state == State.AfterMetadata)
            {
                Debug.Assert(m_jsonReader.NodeType == JsonNodeType.StartArray && m_jsonReader.Name == key_itemList);
                JsonRead();
                m_state = State.InItemList;
            }

            while (m_state == State.InItemList)
            {
                switch (m_jsonReader.NodeType)
                {
                    case JsonNodeType.StartObject: // This is what's expected
                        JsonRead();
                        m_state = State.InItem;
                        break;

                    case JsonNodeType.Value:
                        JsonRead();
                        // Ignore the value and loop again
                        break;

                    case JsonNodeType.StartArray:
                        m_jsonReader.Skip(); // Skip the array and loop again
                        break;

                    case JsonNodeType.EndElement:
                        JsonRead();
                        m_state = State.End;
                        return null;

                    default:
                        ThrowUnexpected();
                        break;
                }
            }

            var codeBit = new CodeBitMetadata();
            while (m_state == State.InItem)
            {
                switch (m_jsonReader.NodeType)
                {
                    case JsonNodeType.Value: // This is what's expected
                        codeBit.AddValue(m_jsonReader.Name, m_jsonReader.Value);
                        JsonRead();
                        break;

                    case JsonNodeType.StartArray: // One way to get multiple values
                        {
                            var name = m_jsonReader.Name;
                            bool exit = false;
                            JsonRead();
                            while (!exit)
                            {
                                switch (m_jsonReader.NodeType)
                                {
                                    case JsonNodeType.Value: // This is what's expected
                                        codeBit.AddValue(name, m_jsonReader.Value);
                                        JsonRead();
                                        break;

                                    case JsonNodeType.EndElement: // Also expected
                                        exit = true;
                                        JsonRead();
                                        break;

                                    case JsonNodeType.StartObject:
                                    case JsonNodeType.StartArray:
                                        m_jsonReader.Skip();
                                        break;

                                    default:
                                        ThrowUnexpected();
                                        break;
                                }
                            }
                        }
                        break;

                    case JsonNodeType.EndElement:
                        JsonRead();
                        m_state = State.InItemList;
                        break;

                    case JsonNodeType.StartObject:
                        m_jsonReader.Skip();
                        break;

                    default:
                        ThrowUnexpected();
                        break;
                }
            }
            return codeBit;
        }

        /// <summary>
        /// Finds CodeBit metadata in the directory with the same name and the closest version match.
        /// </summary>
        /// <param name="codebitName">Name of the CodeBit to find.</param>
        /// <param name="version">Version</param>
        /// <returns>Best match or null</returns>
        /// <remarks>
        /// <para>The best match is a codebit with the greatest version value (according to
        /// Semantic Version comparisons) that is less than or equal to the specified version.
        /// </para>
        /// <para>Makes one pass through the metadata using repeated calls to <see cref="ReadCodeBit"/>
        /// Therefore, this cannot be called repeatedly nor can it be mixed with calls to
        /// ReadCodeBit().</para>
        /// </remarks>
        public CodeBitMetadata? Find(string codebitName, SemVer version)
        {
            CodeBitMetadata? dirMetadata = null;
            for (; ; )
            {
                var candidate = ReadCodeBit();
                if (candidate is null) break;
                if (string.Equals(candidate.Name, codebitName, StringComparison.Ordinal)
                    && (dirMetadata is null || candidate.Version.CompareTo(dirMetadata.Version) > 0)
                    && candidate.Version.CompareTo(version) <= 0)
                {
                    dirMetadata = candidate;
                }
            }
            return dirMetadata;
        }

        void JsonRead()
        {
            if (!m_jsonReader.Read())
            {
                m_state = State.Error;
                throw new ApplicationException(err_unexpectedEnd);
            }
        }

        [DoesNotReturn]
        void ThrowUnexpected()
        {
             throw new ApplicationException($"Unexpected JSON in directory: {m_jsonReader.NodeType}");
        }

        public void Dispose()
        {
            m_jsonReader.Dispose();
        }
    }
}
