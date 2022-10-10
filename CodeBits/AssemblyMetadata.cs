/*
---
name: AssemblyMetadata.cs
description: Convenient and efficient retrieval of assembly metadata.
url: https://raw.githubusercontent.com/FileMeta/AssemblyMetadata/main/AssemblyMetadata.cs
version: 1.0
keywords: CodeBit
dateModified: 2021-10-26
license: https://opensource.org/licenses/BSD-3-Clause
# Metadata in Yaml format. See http://filemeta.org/CodeBit.html
...
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause

Copyright 2021 Brandt Redd

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
this list of conditions and the following disclaimer in the documentation
and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
may be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Text;
using System.Reflection;

namespace FileMeta
{
    class AssemblyMetadata
    {
        Assembly m_assembly;

        public AssemblyMetadata(Assembly assembly)
        {
            m_assembly = assembly;
        }

        public AssemblyMetadata(Type type)
        {
            m_assembly = type.Assembly;
        }

        public AssemblyMetadata(Object obj)
        {
            m_assembly = obj.GetType().Assembly;
        }

        public string AllAttributes
        {
            get
            {
                var sb = new StringBuilder();
                foreach(var ca in m_assembly.CustomAttributes)
                {
                    sb.AppendLine(ca.ToString());
                }
                return sb.ToString();
            }
        }

        public string FullName => m_assembly.FullName;
        public string Name => m_assembly.GetName().Name;
        public string Title => m_assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        public string Description => m_assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        public string Configuration => m_assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        public string Company => m_assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        public string ProductName => m_assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        public string Copyright => m_assembly.GetCustomAttribute <AssemblyCopyrightAttribute>()?.Copyright;
        public string Trademark => m_assembly.GetCustomAttribute <AssemblyTrademarkAttribute>()?.Trademark;
        public Version Version => m_assembly.GetName().Version;
        public string FileVersion => m_assembly.GetCustomAttribute <AssemblyFileVersionAttribute>()?.Version;

        /// <summary>
        /// Returns a multiline summary suitable for reporting the version information for an application.
        /// </summary>
        public string Summary
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine(ProductName ?? Title ?? Name);

                var value = Description;
                if (!string.IsNullOrEmpty(value)) sb.AppendLine(value);

                value = Company;
                if (!string.IsNullOrEmpty(value))
                {
                    sb.Append("By ");
                    sb.AppendLine(value);
                }

                sb.Append("Version ");
                sb.Append(Version.ToString());

                value = Configuration;
                if (!string.IsNullOrEmpty(value))
                {
                    sb.Append($" ({value})");
                }
                sb.AppendLine();

                value = Copyright;
                if (!string.IsNullOrEmpty(value)) sb.AppendLine(value);
                return sb.ToString();
            }
        }

    }
}
