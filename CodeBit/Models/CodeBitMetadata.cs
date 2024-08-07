﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileMeta;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;

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
        const string key_name = "name";
        const string key_version = "version";
        const string key_url = "url";
        const string key_keywords = "keywords";
        const string key_datePublished = "datePublished";
        const string key_author = "author";
        const string key_description = "description";
        const string key_license = "license";
        const string key_hash = "hash";
        const string key_atType = "@type";
        const string key_underType = "_type";
        const string val_keyword_codebit = "CodeBit";
        const string val_atType_software = "SoftwareSourceCode";

        static IReadOnlyCollection<string> s_standardKeys = new HashSet<string>
        {
            key_underType,
            key_atType,
            key_name,
            key_version,
            key_url,
            key_keywords,
            key_datePublished,
            key_author,
            key_description,
            key_license,
            key_hash
        };

        /// <summary>
        /// LinkedData type. For a CodeBit it should be "SoftwareSourceCode"
        /// </summary>
        /// <remarks>
        /// The name, "AtType", comes from JSON linked data where the corresponding property is "@type".
        /// In MetaTag format the property is ("_type").
        /// </remarks>
        public string AtType
        {
            get { return GetValue(key_atType) ?? GetValue(key_underType) ?? string.Empty; }
            set { SetValue(key_atType, value); Remove(key_underType); }
        }

        /// <summary>
        /// Name of the CodeBit (optional, may be Null)
        /// </summary>
        public string Name { get => GetValue(key_name) ?? string.Empty; set => SetValue(key_name, value); }

        /// <summary>
        /// Version of the CodeBit (required)
        /// </summary>
        /// <remarks>
        /// Returns a copy of the version. Changing it will not change the internal value. To make a change,
        /// update the version and set the property to the updated version.
        /// </remarks>
        public SemVer Version
        {
            get
            {
                var strValue = GetValue(key_version);
                if (strValue != null && SemVer.TryParse(strValue, out SemVer value)) return value;
                return SemVer.Zero;
            }
            set
            {
                SetValue(key_version, value.ToString());
            }
        }

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
        /// An SHA256 hash of the source file
        /// </summary>
        /// <remarks>
        /// <para>When calculating the hash. Windows line endings of "\r\n" are normalized
        /// to UNIX-style "\n". This transformation is done regardless of whether the file
        /// is actually a text file. So, if a binary file happens to have a "\r\n" sequence
        /// it will be changed to "\n" while calculating the hash.
        /// </para>
        /// <para>The hash value should have a prefix of "SHA256:" followed by 32 hexadecimal
        /// digits representing the hash. Future implementations may support other hash
        /// algorithms.
        /// </para>
        /// </remarks>
        public string Hash { get => GetValue(key_hash) ?? string.Empty; set => SetValue(key_hash, value); }

        /// <summary>
        /// Filename from which the metadata was read. Used only for validation.
        /// </summary>
        public string? FilenameForValidation { get; set; }

        private readonly char[] c_anySlash = new char[] { '/', '\\' };

        /// <summary>
        /// Filename part of name
        /// </summary>
        public string FilenameFromName
        {
            get
            {
                var name = Name;
                var n = name.LastIndexOfAny(c_anySlash);
                if (n >= 0)
                    name = name.Substring(n + 1);
                return name;
            }
        }

        public bool IsCodeBit
        {
            get
            {
                return IsSoftwareSourceCode
                    && Keywords.Contains(val_keyword_codebit);
            }
        }

        public bool IsSoftwareSourceCode
        {
            get
            {
                return (AtType == val_atType_software);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(AtType)) sb.AppendLine("_type: " + AtType);
            if (!string.IsNullOrEmpty(Name)) sb.AppendLine("name: " + Name);
            sb.AppendLine("version: " + Version.ToString());
            if (!string.IsNullOrEmpty(Url)) sb.AppendLine("url: " + Url);
            if (!string.IsNullOrEmpty(DatePublishedStr)) sb.AppendLine("datePublished: " + DatePublishedStr);
            if (!string.IsNullOrEmpty(Author)) sb.AppendLine("author: " + Author);
            if (!string.IsNullOrEmpty(Description)) sb.AppendLine("description: " + Description);
            if (!string.IsNullOrEmpty(License)) sb.AppendLine("license: " + License);
            if (Keywords.Count > 0) sb.AppendLine("keywords: " + String.Join("; ", Keywords));
            if (!string.IsNullOrEmpty(Hash)) sb.AppendLine("hash: " + Hash);
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

        public void ToJson(TextWriter textWriter)
        {
            using (var writer = new SimpleJsonWriter(textWriter, true))
            {
                writer.WriteDocumentObjectBegin();
                writer.WriteObjectProperty("@type", AtType);
                writer.WriteObjectProperty("name", Name);
                writer.WriteObjectProperty("description", Description);
                writer.WriteObjectProperty("url", Url);
                writer.WriteObjectProperty("version", Version.ToString());

                if (Keywords.Count == 1)
                {
                    writer.WriteObjectOptionalProperty("keywords", Keywords[0]);
                }
                else if (Keywords.Count > 1)
                {
                    writer.WriteObjectArrayBegin("keywords");
                    foreach(var keyword in Keywords)
                    {
                        writer.WriteArrayStringValue(keyword);
                    }
                    writer.WriteArrayEnd();
                }

                writer.WriteObjectOptionalProperty("datePublished", DatePublishedStr);
                writer.WriteObjectOptionalProperty("author", Author);
                writer.WriteObjectOptionalProperty("license", License);
                writer.WriteObjectOptionalProperty("hash", Hash);
                foreach (var pair in this)
                {
                    if (s_standardKeys.Contains(pair.Key)) continue;
                    foreach (var value in pair.Value)
                    {
                        writer.WriteObjectOptionalProperty(pair.Key, value);
                    }
                }
                writer.WriteObjectEnd();
            }
        }

        // Regular expression snippet for a domain name
        const string c_rxDomainName = @"(?:[A-Za-z0-9-_]+)(?:\.[A-Za-z0-9-_]+)+";

        // Regular expression snippet for a filename (not a path - no slashes)
        const string c_rxFilename = @"[^/\\><\|:&""*? \r\n]{1,128}";

        // Regular expression to match a codebit name which must be a domain name followed by
        // a filename path (zero or more directory names concluding with a filename).
        static Regex s_rxName = new Regex("^(?:" + c_rxDomainName + ")(?:/" + c_rxFilename + ")*/(" + c_rxFilename + ")$");

        static readonly char[] s_filenameDelimiters = new char[] { '/', '\\', ':' };

        public (ValidationLevel validationLevel, string validationDetail) Validate()
        {
            var validationLevel = ValidationLevel.Pass;
            var validationDetail = new StringBuilder();

            // === Required Properties ===
            if (AtType != val_atType_software)
            {
                validationLevel |= ValidationLevel.FailRecommended;
                validationDetail.AppendLine($"Property '_type' (or '@type' in a directory) should be '{val_atType_software}' but is '{AtType}'.");
            }
            if ((GetValues(key_atType)?.Count ?? 0) > 1)
            {
                validationLevel |= ValidationLevel.FailRecommended;
                validationDetail.AppendLine($"Property '_type' (or '@type' in a directory) has multiple values. Should have one value of '{val_atType_software}'.");
            }

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
                (int successLevel, SemVer value, string parseMessages) = SemVer.TryParse(GetValue(key_version) ?? string.Empty);
                if (successLevel <= SemVer.Tolerable)
                {
                    validationLevel |= (successLevel == SemVer.Tolerable) ? ValidationLevel.FailRecommended : ValidationLevel.FailMandatory;
                    foreach (var msg in parseMessages.Split('\n', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.RemoveEmptyEntries))
                    {
                        validationDetail.AppendLine("'version' property: " + msg);
                    }
                }
            }

            if (ValidateRequiredSingle(key_url, ref validationLevel, validationDetail))
            {
                if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri))
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'url' property is not a valid URL.");
                }
                if (uri == null || uri.Scheme != "http" && uri.Scheme != "https")
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine("'url' scheme is not http or https.");
                }
            }

            if (!Keywords.Contains(val_keyword_codebit))
            {
                validationLevel |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine($"Property '{key_keywords}' must include '{val_keyword_codebit}'.");
            }

            if (FilenameForValidation is not null)
            {
                int slash = Name.LastIndexOf('/');
                string barename = (slash >= 0) ? Name.Substring(slash + 1) : Name;
                slash = FilenameForValidation.LastIndexOfAny(s_filenameDelimiters);
                string bareFilename = (slash >= 0) ? FilenameForValidation.Substring(slash + 1) : FilenameForValidation;
                if (!string.Equals(barename, bareFilename, StringComparison.Ordinal))
                {
                    validationLevel |= ValidationLevel.FailMandatory;
                    validationDetail.AppendLine($"Local filename '{bareFilename}' does not match CodeBit name '{barename}'.");
                }
            }

            // === Optional Properties ===
            if (ValidateOptionalSingle(key_datePublished, ref validationLevel, validationDetail))
            {
                if (DatePublishedStr != null && DatePublished == DateTimeOffset.MinValue)
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
        /// Compare CodeBit with the another CodeBit and return detailed differences
        /// </summary>
        /// <param name="other">The CodeBit with which to compare.</param>
        /// <param name="thisLabel">Label that refers to this CodeBit in the comparison messages.</param>
        /// <param name="otherLabel">Label that refers to the other CodeBit in the comparison messages.</param>
        /// <param name="expectUrlMatch">True if URLs should match.</param>
        /// <returns><see cref="ValidationLevel"/> and a string containing validation detail.</returns>
        public (ValidationLevel validationLevel, string validationDetail) CompareTo(CodeBitMetadata other, string thisLabel, string otherLabel, bool expectUrlMatch = false)
        {
            var validationLevel = ValidationLevel.Pass;
            var validationDetail = new StringBuilder();

            CompareRequiredStrings(AtType, other.AtType, ref validationLevel, validationDetail, key_atType, thisLabel, otherLabel);
            CompareRequiredStrings(Name, other.Name, ref validationLevel, validationDetail, key_name, thisLabel, otherLabel);

            // URL doesn't necessarily have to match depending on whether the codebit is the latest one.
            if (expectUrlMatch)
                CompareRequiredStrings(Url, other.Url, ref validationLevel, validationDetail, key_url, thisLabel, otherLabel);

            int cmp = Version.CompareTo(other.Version);
            if (cmp < 0)
            {
                validationDetail.AppendLine($"Error: {thisLabel} 'version' ({Version}) is older than {otherLabel} ({other.Version}).");
                validationLevel |= ValidationLevel.FailMandatory;
            }
            if (cmp > 0)
            {
                validationDetail.AppendLine($"Error: {thisLabel} 'version' ({Version}) is newer than {otherLabel} ({other.Version}).");
                validationLevel |= ValidationLevel.FailMandatory;
            }

            foreach(var keyword in Keywords)
            {
                if (!other.Keywords.Contains(keyword))
                {
                    validationDetail.AppendLine($"Warning: {thisLabel} 'keywords' includes '{keyword}' which {otherLabel} does not include.");
                    validationLevel |= ValidationLevel.FailRecommended;
                }
            }
            foreach (var keyword in other.Keywords)
            {
                if (!Keywords.Contains(keyword))
                {
                    validationDetail.AppendLine($"Warning: {otherLabel} 'keywords' includes '{keyword}' which {thisLabel} does not include.");
                    validationLevel |= ValidationLevel.FailRecommended;
                }
            }

            if (Math.Abs(DatePublished.UtcTicks - other.DatePublished.UtcTicks) > 10000) // More than one second difference
            {
                validationDetail.AppendLine($"Warning: {thisLabel} 'datePublished' ({DatePublishedStr}) doesn't match {otherLabel} ({other.DatePublishedStr}).");
                validationLevel |= ValidationLevel.FailRecommended;
            }

            CompareRequiredStrings(Hash, other.Hash, ref validationLevel, validationDetail, key_hash, thisLabel, otherLabel);

            CompareOptionalStrings(Author, other.Author, ref validationLevel, validationDetail, key_author, thisLabel, otherLabel);
            CompareOptionalStrings(Description, other.Description, ref validationLevel, validationDetail, key_description, thisLabel, otherLabel);
            CompareOptionalStrings(License, other.License, ref validationLevel, validationDetail, key_license, thisLabel, otherLabel);

            // Compare the rest
            foreach (var pair in this)
            {
                if (s_standardKeys.Contains(pair.Key)) continue;
                if (pair.Value is null) continue;
                var thisStr = string.Join(';', pair.Value);
                var otherValue = other.GetValues(pair.Key);
                if (otherValue is null)
                {
                    validationDetail.AppendLine($"Warning: {thisLabel} '{pair.Key}' contains value ({thisStr}) but {otherLabel} '{pair.Key}' has no value.");
                    validationLevel |= ValidationLevel.FailRecommended;
                    continue;
                }

                var otherStr = string.Join(';', otherValue);
                CompareOptionalStrings(thisStr, otherStr, ref validationLevel, validationDetail, pair.Key, thisLabel, otherLabel);
            }
            foreach(var pair in other)
            {
                if (s_standardKeys.Contains(pair.Key)) continue;
                if (pair.Value is null) continue;
                if (ContainsKey(pair.Key)) continue;
                validationDetail.AppendLine($"Warning: {thisLabel} '{pair.Key}' has no value but {otherLabel} includes '{pair.Key}' ({string.Join(';', pair.Value)}).");
                validationLevel |= ValidationLevel.FailRecommended;
            }

            return (validationLevel, validationDetail.ToString());
        }

        private static void CompareRequiredStrings(string thisStr, string otherStr, ref ValidationLevel validationLevel, StringBuilder validationDetail, string property, string thisLabel, string otherLabel, StringComparison strCmp = StringComparison.Ordinal)
        {
            if (!string.Equals(thisStr, otherStr, strCmp))
            {
                validationDetail.AppendLine($"Error: {thisLabel} '{property}' ({thisStr}) does not match {otherLabel} '{property}' ({otherStr}).");
                validationLevel |= ValidationLevel.FailMandatory;
            }
        }

        private static void CompareOptionalStrings(string thisStr, string otherStr, ref ValidationLevel validationLevel, StringBuilder validationDetail, string property, string thisLabel, string otherLabel, StringComparison strCmp = StringComparison.Ordinal)
        {
            if (!string.Equals(thisStr, otherStr, strCmp))
            {
                validationDetail.AppendLine($"Warning: {thisLabel} '{property}' ({thisStr}) does not match {otherLabel} '{property}' ({otherStr}).");
                validationLevel |= ValidationLevel.FailRecommended;
            }
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
