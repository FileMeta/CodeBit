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
        /// Failes at least one recommend requirement
        /// </summary>
        FailRecommended = 1,

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
    internal class CodeBitMetadata : FlatMetadata
    {
        const string keyword_codebit = "CodeBit";
        const string key_name = "name";
        const string key_version = "version";
        const string key_url = "url";
        const string key_keywords = "keywords";
        const string key_datePublished = "datepublished";
        const string key_author = "author";
        const string key_description = "description";
        const string key_license = "license";

        static IReadOnlyCollection<string> s_standardKeys = new HashSet<string>
        {
            key_name,
            key_version,
            key_url,
            key_keywords,
            key_datePublished,
            key_author,
            key_description,
            key_license
        };

        /// <summary>
        /// Name of the CodeBit (optional, may be Null)
        /// </summary>
        public string Name { get => GetValue(key_name) ?? string.Empty; set => SetValue(key_name, value); }

        /// <summary>
        /// Version of the CodeBit (required)
        /// </summary>
        public string Version { get => GetValue(key_version) ?? string.Empty; set => SetValue(key_version, value); }

        /// <summary>
        /// URL of the CodeBit (required)
        /// </summary>
        public string Url { get => GetValue(key_url) ?? string.Empty; set => SetValue(key_url, value); }

        /// <summary>
        /// Keywords of the CodeBit (must include "CodeBit")
        /// </summary>
        public IList<string> Keywords => GetValuesAlways(key_keywords);

        /// <summary>
        /// Date the CodeBit was published
        /// </summary>
        /// <remarks>Optional. Value will be <see cref="DateTimeOffset.MinValue"/> if
        /// the property is absent or if the string form is invalid.
        /// </remarks>
        public DateTimeOffset DatePublished { get => GetValueAsDate(key_datePublished); set => SetValue(key_datePublished, value); }

        /// <summary>
        /// Date the CodeBit was published
        /// </summary>
        /// <remarks>Optional. Value will be <see cref="DateTimeOffset.MinValue"/> if
        /// the property is absent or if the string form is invalid.
        /// </remarks>
        public string DatePublishedStr { get => GetValue(key_datePublished) ?? string.Empty; set => SetValue(key_datePublished, value); }

        /// <summary>
        /// Author of the CodeBit (optional, may be null)
        /// </summary>
        public string Author { get => GetValue(key_author) ?? string.Empty; set => SetValue(key_author, value); }

        /// <summary>
        /// Description of the CodeBut (optional, may be null)
        /// </summary>
        public string Description { get => GetValue(key_description) ?? string.Empty; set => SetValue(key_description, value); }

        /// <summary>
        /// License URL of the CodeBit (optional, may be null)
        /// </summary>
        public string License { get => GetValue(key_license) ?? string.Empty; set => SetValue(key_license, value); }

        /// <summary>
        /// Filename from which the metadata was read. Used only for validation.
        /// </summary>
        public string? FilenameForValidation { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Name)) sb.AppendLine("name: " + Name);
            if (!string.IsNullOrEmpty(Version)) sb.AppendLine("version: " + Version);
            if (!string.IsNullOrEmpty(Url)) sb.AppendLine("url: " + Url);
            if (!string.IsNullOrEmpty(DatePublishedStr)) sb.AppendLine("datePublished: " + DatePublishedStr);
            if (!string.IsNullOrEmpty(Author)) sb.AppendLine("author: " + Author);
            if (!string.IsNullOrEmpty(Description)) sb.AppendLine("description: " + Description);
            if (!string.IsNullOrEmpty(License)) sb.AppendLine("license: " + License);
            if (Keywords.Count > 0) sb.AppendLine("keywords: " + String.Join("; ", Keywords));
            foreach (var pair in this)
            {
                if (s_standardKeys.Contains(pair.Key)) continue;
                if (pair.Value == null) continue;
                foreach(var value in pair.Value)
                {
                    sb.Append(pair.Key);
                    sb.Append(": ");
                    sb.AppendLine(value);
                }
            }
            return sb.ToString();
        }

        // Regular expression snippet for a domain name
        const string c_rxDomainName = @"(?:[A-Za-z0-9-_]+)(?:\.[A-Za-z0-9-_]+)+";

        // Regular expression snippet for a filename (not a path - no slashes)
        const string c_rxFilename = @"[^/\\><\|:&""*? \r\n]{1,128}";

        // Regular expression to match a codebit name which must be a domain name followed by
        // a filename path (zero or more directory names concluding with a filename).
        static Regex s_rxName = new Regex("^(?:" + c_rxDomainName + ")(?:/" + c_rxFilename + ")*/(" + c_rxFilename + ")$");

        // (?:(?:[A-Za-z0-9-_]+)(?:\.[A-Za-z0-9-_]+)+)/(?:[^/\\><\|:&""*? \r\n]{1,128})*/([^/\\><\|:&""*? \r\n]{1,128})

        // Regular expression to detect and parse semantic versioning
        static Regex s_rxSemVer = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.CultureInvariant);

        public (ValidationLevel validationLevel, string validationDetail) Validate()
        {
            var validationLevel = ValidationLevel.Pass;
            var validationDetail = new StringBuilder();

            // === Required Properties ===

            if (ValidateRequiredSingle(key_name, ref validationLevel, validationDetail))
            {
                var match = s_rxName.Match(Name);
                if (!match.Success)
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("Property 'name' must be a domain name followed by a file path.");
                }
            }

            if (ValidateRequiredSingle(key_version, ref validationLevel, validationDetail))
            {
                var match = s_rxSemVer.Match(Version);
                if (!match.Success)
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'version' property does not match Semantic Versioning pattern.");
                }
            }

            if (ValidateRequiredSingle(key_url, ref validationLevel, validationDetail))
            {
                if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'url' property is not a valid URL.");
                }
            }

            if (!Keywords.Contains(keyword_codebit))
            {
                validationLevel |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine($"Property '{key_keywords}' must include '{keyword_codebit}'.");
            }

            // === Optional Properties ===
            if (ValidateOptionalSingle(key_description, ref validationLevel, validationDetail))
            {
                if (DatePublishedStr != null && DatePublished == DateTime.MinValue)
                {
                    validationLevel |= ValidationLevel.FailRecommended;
                    validationDetail.AppendLine("Property 'datePublished' is an invalid format. Must be RFC 3339");
                }
            }

            ValidateOptionalSingle(key_author, ref validationLevel, validationDetail);
            ValidateOptionalSingle(key_description, ref validationLevel, validationDetail);
            ValidateOptionalSingle(key_license, ref validationLevel, validationDetail);

            return (validationLevel, validationDetail.ToString());
        }

        /// <summary>
        /// Read CodeBit
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> from which to read the CodeBit.</param>
        /// <returns>A <see cref="CodeBit"/> instance that has not been validated.</returns>
        /// <remarks>Use <see cref="Validate"/> to validate the metadata read from the TextReader.</remarks>
        public static CodeBitMetadata Read(TextReader reader)
        {
            var metadata = new CodeBitMetadata();

            // TODO: Rewrite the MetaTag class to parse a TextReader - it will be much faster and won't consume so much memory
            foreach (var tag in MetaTag.Extract(reader.ReadToEnd()))
            {
                metadata.AddValue(tag.Key, tag.Value);
            }

            return metadata;
        }

        bool ValidateRequiredSingle(string propertyName, ref ValidationLevel validation, StringBuilder validationDetail)
        {
            var values = GetValues(propertyName);
            if (values == null || values.Count == 0)
            {
                validation |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine($"Property '{propertyName}' is required but not present.");
                return false;
            }
            if (values.Count > 1)
            {
                validation |= ValidationLevel.FailRecommended;
                validationDetail.AppendLine($"Multiple instances of property '{propertyName}'. Only one expected.");
                return false;
            }
            return true;
        }

        bool ValidateOptionalSingle(string propertyName, ref ValidationLevel validation, StringBuilder validationDetail)
        {
            var values = GetValues(propertyName);
            if (values == null || values.Count == 0)
            {
                return false;
            }
            if (values.Count > 1)
            {
                validation |= ValidationLevel.FailRecommended;
                validationDetail.AppendLine($"Multiple instances of property '{propertyName}'. Only one expected.");
                return false;
            }
            return true;
        }

    }
}
