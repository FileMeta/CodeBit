using FileMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit
{
    internal class DirectoryMetadata : FlatMetadata
    {
        const string key_atContext = "@context";
        const string key_atType = "@type";
        const string val_atContext_schema = "https://schema.org";
        const string val_atType_itemList = "ItemList";

        public string AtContext { get => GetValue(key_atContext) ?? string.Empty; set => SetValue(key_atContext, value); }
        public string AtType { get => GetValue(key_atType) ?? string.Empty; set => SetValue(key_atType, value); }

        /// <summary>
        /// Validate the Directory Metadata
        /// </summary>
        /// <returns>A Validation Level and Validation Detail.</returns>
        /// <remarks>
        /// Only validates the directory metadata - not the collection contained in the directory.
        /// </remarks>
        public (ValidationLevel validationLevel, string validationDetail) Validate()
        {
            ValidationLevel validationLevel = ValidationLevel.Pass;
            var validationDetail = new StringBuilder();
            ValidateMatch(key_atContext, val_atContext_schema, ref validationLevel, validationDetail);
            ValidateMatch(key_atType, val_atType_itemList, ref validationLevel, validationDetail);
            return (validationLevel, validationDetail.ToString());
        }

        private void ValidateMatch(string key, string expectedValue, ref ValidationLevel validationLevel, StringBuilder validationDetail)
        {
            if (GetValue(key) != expectedValue)
            {
                validationLevel |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine($"Property '{key}' should be '{expectedValue}' but is '{GetValue(key)}'.");
            }
            if ((GetValues(key)?.Count ?? 0) > 1)
            {
                validationLevel |= ValidationLevel.FailMandatory;
                validationDetail.AppendLine($"Property '{key}' has multiple values. Should have one value of '{expectedValue}'.");
            }
        }

    }
}
