/*
CodeBit Metadata
&name=bredd.tech/DnsQuery.cs
&description="CodeBit class for performing DNS Queries for TXT records."
&author="Brandt Redd"
&url=https://raw.githubusercontent.com/bredd/DnsQuery/main/DnsQuery.cs
&version=1.0
&keywords=CodeBit
&dateModified=2023-01-21
&license=https://opensource.org/licenses/BSD-3-Clause
&comment="This could easily be extended to support other DNS record types."

About Codebits http://www.filemeta.org/CodeBit
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause
Copyright 2021-2022 Brandt Redd
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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Bredd
{
    /// <summary>
    /// Perform DNS queries for MX and TXT records via p/invoke to Win32 DnsQuery API
    /// </summary>
    /// <remarks>
    /// <para>Only works on Windows platforms due to p/invoke to the operating system.
    /// To make this a .net Core class that works across platforms the DNS protocol
    /// should be implemented upon the sockets layer. For security, consider using
    /// the DNS over HTTPS (DoH) protocol.
    /// </para>
    /// <para>See https://learn.microsoft.com/en-us/windows/win32/api/windns/
    /// </para>
    /// </remarks>
    internal class WinDnsQuery
    {
        /// <summary>
        /// Retrieve TXT records for the specified domain.
        /// </summary>
        /// <param name="domainName">Name of the domain on which to perform the query.</param>
        /// <returns>An array of TXT records</returns>
        public static string[]? GetTxtRecords(string domainName)
        {
            IntPtr pDnsRecord = IntPtr.Zero;
            try
            {
                var result = new List<string>();

                Int32 hresult = DnsQuery(domainName, QueryTypes.DNS_TYPE_TEXT, QueryOptions.DNS_QUERY_RETURN_MESSAGE, IntPtr.Zero, ref pDnsRecord, IntPtr.Zero);
                if (hresult == (Int32)DnsError.DNS_ERROR_RCODE_NAME_ERROR
                    || hresult == (Int32)DnsError.DNS_ERROR_RCODE_SERVER_FAILURE) return null;
                if (hresult != 0) throw new Win32Exception(hresult);

                IntPtr pCurrent = pDnsRecord;
                while(pCurrent != IntPtr.Zero)
                {
                    var rec = Marshal.PtrToStructure<DNS_RECORDW>(pCurrent);
#if DEBUG
                    Debug.WriteLine(Marshal.PtrToStringUni(rec.pName));
#endif
                    if (rec.wType == (UInt16)QueryTypes.DNS_TYPE_TEXT)
                    {
                        int stringCount = Marshal.ReadInt32(pCurrent + DNS_RECORDW_SIZE);
                        for (int i=0; i<stringCount; ++i)
                        {
                            IntPtr pString = Marshal.ReadIntPtr(pCurrent + DNS_RECORDW_SIZE + ((i+1) * IntPtr.Size));
                            result.Add(Marshal.PtrToStringUni(pString) ?? string.Empty);
                        }
                    }
                    pCurrent = rec.pNext;
                }
                return result.ToArray();
            }
            finally
            {
                if (pDnsRecord != IntPtr.Zero) DnsRecordListFree(pDnsRecord, 0);
            }
        }

        // See https://learn.microsoft.com/en-us/windows/win32/api/windns/nf-windns-dnsquery_w
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern Int32 DnsQuery([MarshalAs(UnmanagedType.LPWStr)] string pszName, [In] QueryTypes wType, [In] QueryOptions options, [In] IntPtr pExtra, [In, Out] ref IntPtr ppQueryResults, [In] IntPtr pReserved);

        // See https://learn.microsoft.com/en-us/windows/win32/api/windns/nf-windns-dnsrecordlistfree
        [DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void DnsRecordListFree(IntPtr pRecordList, Int32 FreeType);

        // See: https://learn.microsoft.com/en-us/windows/win32/dns/dns-constants
        private enum QueryTypes : UInt16
        {
            DNS_TYPE_A = 0x0001,
            DNS_TYPE_MX = 0x000f,
            DNS_TYPE_TEXT = 0x0010,
            DNS_TYPE_SRV = 0x0021
        }

        // See: https://learn.microsoft.com/en-us/windows/win32/dns/dns-constants
        private enum QueryOptions : UInt32
        {
            DNS_QUERY_STANDARD         = 0x00000000,
            DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 0x00000001,
            DNS_QUERY_USE_TCP_ONLY     = 0x00000002,
            DNS_QUERY_NO_RECURSION     = 0x00000004,
            DNS_QUERY_BYPASS_CACHE     = 0x00000008,
            DNS_QUERY_NO_WIRE_QUERY    = 0x00000010,
            DNS_QUERY_NO_LOCAL_NAME    = 0x00000020,
            DNS_QUERY_NO_HOSTS_FILE    = 0x00000040,
            DNS_QUERY_NO_NETBT         = 0x00000080,
            DNS_QUERY_WIRE_ONLY        = 0x00000100,
            DNS_QUERY_RETURN_MESSAGE   = 0x00000200,
            DNS_QUERY_MULTICAST_ONLY   = 0x00000400,
            DNS_QUERY_NO_MULTICAST     = 0x00000800,
            DNS_QUERY_TREAT_AS_FQDN    = 0x00001000,
            DNS_QUERY_ADDRCONFIG       = 0x00002000,
            DNS_QUERY_DUAL_ADDR        = 0x00004000,
            DNS_QUERY_MULTICAST_WAIT   = 0x00020000,
            DNS_QUERY_MULTICAST_VERIFY = 0x00040000,
            DNS_QUERY_DONT_RESET_TTL_VALUES = 0x00100000,
            DNS_QUERY_DISABLE_IDN_ENCODING = 0x00200000,
            DNS_QUERY_APPEND_MULTILABEL = 0x00800000
        }

        private enum DnsError : Int32
        {
            DNS_ERROR_RCODE_FORMAT_ERROR = 9001,
            DNS_ERROR_RCODE_SERVER_FAILURE = 9002,
            DNS_ERROR_RCODE_NAME_ERROR = 9003,
            DNS_ERROR_RCODE_NOT_IMPLEMENTED = 9004,
            DNS_ERROR_RCODE_REFUSED = 9005,
            DNS_ERROR_RCODE_YXDOMAIN = 9006,
            DNS_ERROR_RCODE_YXRRSET = 9007,
            DNS_ERROR_RCODE_NXRRSET = 9008,
            DNS_ERROR_RCODE_NOTAUTH = 9009,
            DNS_ERROR_RCODE_NOTZONE = 9010,
            DNS_ERROR_RCODE_BADSIG = 9016,
            DNS_ERROR_RCODE_BADKEY = 9017,
            DNS_ERROR_RCODE_BADTIME = 9018
        }

        // See https://learn.microsoft.com/en-us/windows/win32/api/windns/ns-windns-dns_recordw
        [StructLayout(LayoutKind.Sequential)]
        private struct DNS_RECORDW
        {
            public IntPtr pNext;
            public IntPtr pName;
            public UInt16 wType;
            public UInt16 wDataLength;
            public UInt32 flags;
            public UInt32 dwTtl;
            public UInt32 dwReserved;
        }

        static readonly int DNS_RECORDW_SIZE = IntPtr.Size * 2 + 16;
    }
}
