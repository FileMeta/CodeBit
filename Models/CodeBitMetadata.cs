using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileMeta;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CodeBit
{
    [Flags]
    enum ValidationLevel
    {
        /// <summary>
        /// Meets all mandatory and recommended requirements.
        /// </summary>
        Pass = 0,

        /// <summary>
        /// Meets all mandatory specifications. but fails 
        /// at least one recommended requirement
        /// </summary>
        PassMandatory = 1, // Also means FailRecommended

        /// <summary>
        /// Fails at least one mandatory requirement
        /// </summary>
        FailMandatory = 2,

        /// <summary>
        /// Fails at least one mandatry and at least one recommended requirement
        /// </summary>
        Fail = 3
    }

    /// <summary>
    /// Model class for CodeBit metadata
    /// </summary>
    internal class CodeBitMetadata
    {
        List<string> m_keywords = new List<string>();
        Dictionary<string, string> m_otherProperties = new Dictionary<string, string>();

        /// <summary>
        /// Name of the CodeBit (optional, may be Null)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the CodeBit (required)
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// URL of the CodeBit (required)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Keywords of the CodeBit (must include "CodeBit")
        /// </summary>
        public List<string> Keywords => m_keywords;

        /// <summary>
        /// DatePublished of the CodeBit (optional, may be DateTimeOffset.MinValue)
        /// </summary>
        public DateTimeOffset DatePublished { get; set; }

        /// <summary>
        /// Author of the CodeBit (optional, may be null)
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Description of the CodeBut (optional, may be null)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// License URL of the CodeBit (optional, may be null)
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// All other properties (optional, may be empty)
        /// </summary>
        public IDictionary<string, string> OtherProperties => m_otherProperties;

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Name)) sb.AppendLine("name: " + Name);
            if (!string.IsNullOrEmpty(Version)) sb.AppendLine("version: " + Version);
            if (!string.IsNullOrEmpty(Url)) sb.AppendLine("url: " + Url);
            if (DatePublished > DateTimeOffset.MinValue) sb.AppendLine("datePublished: " + DatePublished.ToStringConcise());
            if (!string.IsNullOrEmpty(Author)) sb.AppendLine("author: " + Author);
            if (!string.IsNullOrEmpty(Description)) sb.AppendLine("description: " + Description);
            if (!string.IsNullOrEmpty(License)) sb.AppendLine("license: " + License);
            if (Keywords.Count > 0) sb.AppendLine("keywords: " + String.Join("; ", Keywords));
            foreach (var pair in OtherProperties)
            {
                sb.Append(pair.Key);
                sb.Append(": ");
                sb.AppendLine(pair.Value);
            }
            return sb.ToString();
        }

        // Regular expression to detect and parse semantic versioning
        static Regex s_rxSemVer = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Read and validate a CodeBit
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> from which to read the CodeBit.</param>
        /// <returns>A <see cref="CodeBit"/> instance, a <see cref="ValidationLevel"/>, and a string containing validation detail.</returns>
        public static (CodeBitMetadata metadata, ValidationLevel validationLevel, string validationDetail) ReadAndValidate(TextReader reader)
        {
            var metadata = new CodeBitMetadata();
            var validationLevel = ValidationLevel.Pass;
            var validationDetail = new StringBuilder();
            string datePublishedStr = null;

            // TODO: Rewrite the MetaTag class to parse a TextReader - it will be much faster and won't consume so much memory
            foreach (var tag in MetaTag.Extract(reader.ReadToEnd()))
            {
                switch (tag.Key)
                {
                    // === Required Properties ===
                    case "version":
                        if (metadata.Version == null) // Only keep the first instance
                            metadata.Version = tag.Value;
                        else
                            ReportMultiple("version", ref validationLevel, validationDetail);
                        break;

                    case "url":
                        if (metadata.Url == null)
                            metadata.Url = tag.Value;
                        else
                            ReportMultiple("url", ref validationLevel, validationDetail);
                        break;

                    case "keywords":
                        metadata.Keywords.Add(tag.Value);
                        break;

                    // Recommended properties
                    case "name":
                        if (metadata.Name == null)
                            metadata.Name = tag.Value;
                        else
                            ReportMultiple("name", ref validationLevel, validationDetail);
                        break;

                    case "datePublished":
                        if (datePublishedStr == null)
                            datePublishedStr = tag.Value;
                        else
                            ReportMultiple("datePublished", ref validationLevel, validationDetail);
                        break;

                    case "author":
                        if (metadata.Author == null)
                            metadata.Author = tag.Value;
                        else
                            ReportMultiple("author", ref validationLevel, validationDetail);
                        break;

                    case "description":
                        if (metadata.Description == null)
                            metadata.Description = tag.Value;
                        else
                            ReportMultiple("description", ref validationLevel, validationDetail);
                        break;

                    case "license":
                        if (metadata.License == null)
                            metadata.License = tag.Value;
                        else
                            ReportMultiple("license", ref validationLevel, validationDetail);
                        break;

                    default:
                        if (!metadata.OtherProperties.ContainsKey(tag.Key))
                            metadata.OtherProperties.Add(tag);
                        // Don't report an error on duplicate. Depending on the metadata it may be valid.
                        break;
                }
            }

            // === Validate Required Properties ===
            if (!ReportIfEmpty(metadata.Version, "version", true, ref validationLevel, validationDetail))
            {
                var match = s_rxSemVer.Match(metadata.Version);
                if (!match.Success)
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'version' property does not match Semantic Versioning pattern.");
                }
            }

            if (!ReportIfEmpty(metadata.Url, "url", true, ref validationLevel, validationDetail))
            {
                if (!Uri.TryCreate(metadata.Url, UriKind.Absolute, out Uri uri))
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'url' property is not a valid URL.");
                }
            }

            if (!metadata.Keywords.Contains("codebit", StringComparer.OrdinalIgnoreCase))
            {
                validationLevel |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine("'keywords' property with 'CodeBit' value not found.");
            }

            // === Validate Recommended Properties
            ReportIfEmpty(metadata.Name, "name", false, ref validationLevel, validationDetail);
            ReportIfEmpty(metadata.Author, "author", false, ref validationLevel, validationDetail);
            ReportIfEmpty(metadata.Description, "description", false, ref validationLevel, validationDetail);
            ReportIfEmpty(metadata.License, "license", false, ref validationLevel, validationDetail);
            if (!string.IsNullOrEmpty(datePublishedStr))
            {
                if (DateTimeOffset.TryParse(datePublishedStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal|DateTimeStyles.RoundtripKind,
                    out DateTimeOffset date))
                {
                    metadata.DatePublished = date;
                }
                else
                {
                    validationLevel |= ValidationLevel.PassMandatory; // Means FailRecommended
                    validationDetail.AppendLine("Invalid datePublished value.");
                }
            }

            return (metadata, validationLevel, validationDetail.ToString());
        }

        private static void ReportMultiple(string propertyName, ref ValidationLevel validationLevel, StringBuilder validationDetail)
        {
            validationLevel |= ValidationLevel.PassMandatory;
            validationDetail.AppendLine($"Multiple instances of '{propertyName}' propoerty.");
        }

        private static bool ReportIfEmpty(string value, string propName, bool isMandatory, ref ValidationLevel validationLevel, StringBuilder validationDetail)
        {
            if (!string.IsNullOrEmpty(value)) return false;

            validationLevel |= isMandatory ? ValidationLevel.FailMandatory : ValidationLevel.PassMandatory; // PassMandatory means FailRecommended
            validationDetail.AppendLine($"{(isMandatory ? "Mandatory" : "Optional")} property '{propName}' is not present.");
            return true;
        }

    }
}
