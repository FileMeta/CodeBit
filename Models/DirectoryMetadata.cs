using FileMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit.Models
{
    internal class DirectoryMetadata : FlatMetadata
    {
        const string key_atContext = "@context";
        const string key_atType = "@type";

        public string AtContext { get => GetValue(key_atContext) ?? string.Empty; set => SetValue(key_atContext, value); }
        public string AtType { get => GetValue(key_atType) ?? string.Empty; set => SetValue(key_atType, value); }
    }
}
