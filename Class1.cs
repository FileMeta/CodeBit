using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit
{
    internal static class CodebitJsonWriter
    {
        public static void Write(CodeBitMetadata metadata, Stream stream, bool closeStream = true)
        {
            using (var writer = JsonXmlWriter.Create(stream, closeStream))
            {
                writer.WriteDocumentObjectBegin();
                writer.WriteObjectProperty("@type", metadata.AtType);
                writer.WriteObjectProperty("name", metadata.Name);
                writer.WriteObjectProperty("version", metadata.Version.ToString());
                writer.WriteObjectProperty("url", metadata.Url);
                writer.WriteObjectProperty("keywords", String.Join(',', metadata.Keywords));
                writer.WriteObjectOptionalProperty("datePublished", metadata.DatePublishedStr);
                writer.WriteObjectOptionalProperty("author", metadata.Author);
                writer.WriteObjectOptionalProperty("description", metadata.Description);
                writer.WriteObjectOptionalProperty("license", metadata.License);
                foreach(var pair in metadata)
                {
                    if (!CodeBitMetadata.IsStandardAttributeKey(pair.Key))
                    {
                        foreach(var value in pair.Value)
                        {
                            writer.WriteObjectOptionalProperty(pair.Key, value);
                        }
                    }
                }
                writer.WriteObjectEnd();
            }
        }

    }
}
