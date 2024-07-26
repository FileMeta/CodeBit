/*
CodeBit Metadata
&name=bredd.tech/SemVer.cs
&description="CodeBit class for parsing, comparing, and formatting Semantic Versioning values."
&author="Brandt Redd"
&url=https://raw.githubusercontent.com/bredd/SemVer/main/SemVer.cs
&version=1.0.0
&keywords=CodeBit
&dateModified=2023-02-23
&license=https://opensource.org/licenses/BSD-3-Clause
&comment="Implements the Semantic Versioning 2.0.0 specification. See https://semver.org"

About Codebits http://www.filemeta.org/CodeBit
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause
Copyright 2023 Brandt Redd
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
using System.Runtime.CompilerServices;
using System.Text;

namespace FileMeta
{
    internal class SemVer : IComparable<SemVer>, IComparable 
    {
        const string cMajor = "Major";
        const string cMinor = "Minor";
        const string cPatch = "Patch";
        const string cPrerelease = "Prerelease";
        const string cBuild = "Build";

        public SemVer()
        {
            // Natural initialization to zeros works well.
        }

        public SemVer(int major, int minor = 0, int patch = 0, string? prerelease = null, string? build = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
            Build = build;
        }

        /// <summary>
        /// The major version number
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// The minor version number
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// The patch number
        /// </summary>
        public int Patch { get; set; }

        string? m_prerelease;

        /// <summary>
        /// Optional prerelease string
        /// </summary>
        /// <remarks>
        /// WARNING: When setting the value, if there are errors, such as leading zeros or empty
        /// elements, they are silently corrected. If the input is invalid, the value is silently
        /// set to null. To check for errors, use <see cref="TrySetPrerelease(string)"/>.
        /// </remarks>
        public string? Prerelease
        {
            get => m_prerelease;
            set
            {
                (int successLevel, string messages) = TrySetPrerelease(value);
                if (successLevel == Invalid) m_prerelease = null;
            }
        }

        /// <summary>
        /// Set the <see cref="Prerelease"/> value if it is <see cref="Valid"/> or <see cref="Tolerable"/>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>
        /// successLevel will be <see cref="Valid"/>, <see cref="Tolerable"/>, or <see cref="Invalid"/>. If
        /// Tolerable or Invalid, messages will contain warnings and/or errors.
        /// </returns>
        /// <remarks>If successLevel is Valid, the value of <see cref="Prerelease"/> is exactly as passed in.
        /// If Tolerable then the value of <see cref="Prerelease"/> is the corrected value. Empty IDs and
        /// leading zeros are removed. If successLevel is Invalid, the value of <see cref="Prerelease"/>
        /// is unchanged.</remarks>
        public (int successlevel, string messages) TrySetPrerelease(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                m_build = null;
                return (Valid, string.Empty);
            }

            int p = 0;
            var messages = new StringBuilder();
            (string parsed, char term) = ParseTailComponent(value, ref p, cPrerelease, messages);
            if (term != '\0') return (Invalid, "Error: Prerelease includes invalid characters.");
            m_prerelease = parsed;
            if (messages.Length > 0)
                return (Tolerable, messages.ToString());
            return (Valid, string.Empty);
        }

        string? m_build;

        /// <summary>
        /// Optional build string
        /// </summary>
        /// <remarks>
        /// WARNING: When setting the value, if there are errors, such as leading zeros or empty
        /// elements, they are silently corrected. If the input is invalid, the value is silently
        /// set to null. To check for errors, use <see cref="TrySetBuild(string)"/>.
        /// </remarks>
        public string? Build
        {
            get => m_build;
            set
            {
                (int successLevel, string messages) = TrySetBuild(value);
                if (successLevel == Invalid) m_build = null;
            }
        }

        /// <summary>
        /// Set the <see cref="Build"/> value if it is <see cref="Valid"/> or <see cref="Tolerable"/>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>
        /// successLevel will be <see cref="Valid"/>, <see cref="Tolerable"/>, or <see cref="Invalid"/>. If
        /// Tolerable or Invalid, messages will contain warnings and/or errors.
        /// </returns>
        /// <remarks>If successLevel is Valid, the value of <see cref="Build"/> is exactly as passed in.
        /// If Tolerable then the value of <see cref="Build"/> is the corrected value. Empty IDs and
        /// leading zeros are removed. If successLevel is Invalid, the value of <see cref="Build"/>
        /// is unchanged.</remarks>
        public (int successlevel, string messages) TrySetBuild(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                m_build = null;
                return (Valid, string.Empty);
            }

            int p = 0;
            var messages = new StringBuilder();
            (string parsed, char term) = ParseTailComponent(value, ref p, cBuild, messages);
            if (term != '\0') return (Invalid, "Error: Build includes invalid characters.");
            m_build = parsed;
            if (messages.Length > 0)
                return (Tolerable, messages.ToString());
            return (Valid, string.Empty);
        }

        /// <summary>
        /// Compare two semantic versions according to the rules
        /// in the Semantic Versioning 2.0.0 specification.
        /// https://semver.org
        /// </summary>
        /// <param name="other">The other semantic version with which to compare.</param>
        /// <returns>Value less than zero if the other version is higher. Zero if the
        /// values are equal, and value greater than zero if this version is higher.</returns>
        public int CompareTo(SemVer? other)
        {
            if (other == null) return 1;
            int cmp = Major - other.Major;
            if (cmp != 0) return cmp;
            cmp = Minor - other.Minor;
            if (cmp != 0) return cmp;
            cmp = Patch - other.Patch;
            if (cmp != 0) return cmp;
            return ComparePrerelease(Prerelease, other.Prerelease);
            // Per the semver 2.0.0 spec, if Major, Minor, Patch, and Prerelease
            // are the same then the versions are equal regardless of build values.
        }

        /// <summary>
        /// Compare with another object.
        /// </summary>
        /// <param name="obj">The object with which to compare.</param>
        /// <returns>If the other object is a <see cref="SemVer"/> returns
        /// the value of <see cref="CompareTo(SemVer?)"/>. Otherwise
        /// returns 1.</returns>
        public int CompareTo(object? obj)
        {
            return CompareTo(obj as SemVer);
        }

        /// <summary>
        /// Indicate whether this is equal with another object.
        /// </summary>
        /// <param name="obj">The object with which to compare.</param>
        /// <returns>If the other object is a <see cref="SemVer"/> compares
        /// according to Semantic Versioning 2.0.0 rules. Otherwise
        /// returns false.</returns>
        public override bool Equals(object? obj)
        {
            return CompareTo(obj as SemVer) == 0;
        }

        /// <summary>
        /// Calculates a hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Major.GetHashCode() ^ Minor.GetHashCode() ^ Patch.GetHashCode()
                ^ (string.IsNullOrEmpty(Prerelease) ? 0 : Prerelease.GetHashCode());
        }

        /// <summary>
        /// Formats the value as a string according to the
        /// Semantic Versioning 2.0.0 specification.
        /// </summary>
        /// <returns>The formatted semantic versioning value.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Major);
            if (Minor < int.MaxValue)
            {
                sb.Append('.');
                sb.Append(Minor);
            }
            else
            {
                return sb.ToString();
            }
            if (Patch < int.MaxValue)
            {
                sb.Append('.');
                sb.Append(Patch);
            }
            else
            {
                return sb.ToString();
            }
            if (!string.IsNullOrEmpty(Prerelease))
            {
                sb.Append('-');
                sb.Append(Prerelease);
            }
            if (!string.IsNullOrEmpty(Build))
            {
                sb.Append('+');
                sb.Append(Build);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parse a semantic version
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed <see cref="SemVer"/> value.</returns>
        /// <exception cref="ArgumentException">Thrown if the string is not a valid
        /// Semantic Versioning 2.0.0 value.</exception>
        /// <remarks>Performs a best effort parse. Thus, some values that don't
        /// comply with the specification will still succeed. For example,
        /// the value "4" would be parsed as the semantic version "4.0.0". For
        /// strict interpretation of the spec, use <see cref="ParseStrict(string)"/>
        /// or <see cref="TryParse(string)"/>. On the latter function, check the
        /// returned <c>successLevel</c>.
        /// </remarks>
        public static SemVer Parse(string s)
        {
            (int successLevel, SemVer value, string parseMessages) = TryParse(s);
            if (successLevel < Tolerable) throw new ArgumentException(parseMessages);
            return value;
        }

        /// <summary>
        /// Parse a semantic version with strict compliance
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed <see cref="SemVer"/> value.</returns>
        /// <exception cref="ArgumentException">Thrown if the string is not a valid
        /// Semantic Versioning 2.0.0 value.</exception>
        /// <remarks>Performs a strict parse. For best effort parsing use
        /// <see cref="Parse(string)"/>.
        /// </remarks>
        public static SemVer ParseStrict(string s)
        {
            (int successLevel, SemVer value, string parseMessages) = TryParse(s);
            if (successLevel < Valid) throw new ArgumentException(parseMessages);
            return value;
        }

        /// <summary>
        /// Try to parse a semantic version
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="value">The parsed value</param>
        /// <returns>True if successful, else false.</returns>
        /// <remarks>Performs a best effort parse. Thus, some values that don't
        /// comply with the specification will still succeed. For example,
        /// the value "4" would be parsed as the semantic version "4.0.0". For
        /// strict interpretation of the spec, use <see cref="TryParse(string)"/> and
        /// check the returned <c>successLevel</c> or use <see cref="ParseStrict(string)"/>.
        /// </remarks>
        public static bool TryParse(string s, out SemVer value)
        {
            (int successLevel, value, string parseMessages) = TryParse(s);
            return (successLevel >= Tolerable);
        }

        public static readonly SemVer Zero = new SemVer(0, 0, 0);
        public static readonly SemVer Max = new SemVer(int.MaxValue, int.MaxValue, int.MaxValue);

        /// <summary>
        /// Return value from <see cref="TryParse(string)"/>. Indicates invalid format.
        /// </summary>
        public const int Invalid = 0;

        /// <summary>
        /// Return value from <see cref="TryParse(string)"/>. Indicates tolerable success.
        /// </summary>
        /// <remarks>
        /// All parts of the inbound semantic string were successfully parsed but
        /// it is not compliant with Semantic Versioning 2.0.0 spec. For example,
        /// the patch number might be missing or one of the version numbers might
        /// have leading zeros.
        /// </remarks>
        public const int Tolerable = 1;

        /// <summary>
        /// Return value from <see cref="TryParse(string)"/>. Indicates full success.
        /// </summary>
        public const int Valid = 2;

        /// <summary>
        /// Parse a semantic version, gracefully degrading if the format isn't perfect.
        /// </summary>
        /// <param name="s">A string in semantic versioning format to be parsed.</param>
        /// <returns>successLevel, parsed value, and error or warning messages.</returns>
        /// <remarks>
        /// <para>The <c>successLevel</c> return will be <see cref="Invalid"/> (0), <see cref="Tolerable"/> (1),
        /// or <see cref="Valid"/> (2) indicating the level of success in parsing the inbound string.
        /// </para>
        /// <para>The <c>value</c> return will contain the parsed semantic version. If <c>successLevel</c>
        /// is <c>Invalid</c> then it will be simply "0.0.0". If <c>Partial</c> then the value contain
        /// the portions that were recognized.
        /// </para>
        /// <para>The <c>parseMessages</c> return will return error and warning messages if the <c>successLevel</c>
        /// is <c>Invald</c>, or <c>Partial</c>. It will be an empty string when <c>successLevel</c> is
        /// <c>Success</c>.
        /// </para>
        /// </remarks>
        public static (int successLevel, SemVer value, string parseMessages) TryParse(string s)
        {
            var messages = new StringBuilder();
            char term;
            int p = 0;

            if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V'))
            {
                messages.AppendLine("Warning: 'v' prefix to version is not expected.");
                p = 1;
            }

            // Major version
            int major;
            if (p < s.Length && char.IsDigit(s[p]))
            {
                (major, term) = ParseIntComponent(s, ref p, cMajor, messages);
            }
            else
            {
                return (Invalid, Zero, "Error: Invalid Semantic Versioning Format.\n");
            }

            // Minor version
            int minor = 0;
            if (term == '.')
            {
                ++p;
                (minor, term) = ParseIntComponent(s, ref p, cMinor, messages);
            }
            else
            {
                messages.AppendLine("Warning: Minor version not found.");
            }

            // Patch
            int patch = 0;
            if (term == '.')
            {
                ++p;
                (patch, term) = ParseIntComponent(s, ref p, cPatch, messages);
            }
            else
            {
                messages.AppendLine("Warning: Patch version not found.");
            }

            // Prerelease
            string? prerelease = null;
            if (term == '-')
            {
                 ++p;
                (prerelease, term) = ParseTailComponent(s, ref p, cPrerelease, messages);
            }

            // Build
            string? build = null;
            if (term == '+')
            {
                ++p;
                (build, term) = ParseTailComponent(s, ref p, cBuild, messages);
            }

            if (p < s.Length)
            {
                return (Invalid, Zero, $"Error: Unexpected text in semantic version at position {p}.\n");
            }

            var result = new SemVer(major, minor, patch, prerelease, build);
            if (messages.Length == 0)
                return (Valid, result, string.Empty);
            return (Tolerable, result, messages.ToString());
        }

        /// <summary>
        /// Parse a semantic version for search, gracefully degrading if the format isn't perfect.
        /// </summary>
        /// <param name="s">A string in semantic versioning format to be parsed.</param>
        /// <returns>The parsed value.</returns>
        /// <remarks>
        /// <para>The first part that is missing or fails to parse will be set to the maximum value.
        /// All subsequent parts will also be set to max.
        /// </para>
        /// <para>When searching a directory, the best match will be the highest version
        /// (per <see cref="CompareTo"/>) that doesn't exceed thie search value. Note that
        /// a version without <see cref="Prerelease"/> is considered later so the max value for
        /// Prerelease is empty string. <see cref="Build"/> is not used in comparisons so its
        /// max is also set to empty string if it is not included.</para>
        /// <para>Always succeeds regardless of the value to be parsed.</para>
        /// </remarks>
        public static SemVer ParseForSearch(string s)
        {
            var messages = new StringBuilder();
            char term;
            int p = 0;

            // Ignore a leading v.
            if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V'))
            {
                p = 1;
            }

            // Major version
            int major = 0;
            if (p < s.Length && char.IsDigit(s[p]))
            {
                (major, term) = ParseIntComponent(s, ref p, cMajor, messages);
            }
            else
            {
                return new SemVer(int.MaxValue, int.MaxValue, int.MaxValue);
            }

            // Minor version
            int minor = 0;
            if (term == '.')
            {
                ++p;
                (minor, term) = ParseIntComponent(s, ref p, cMinor, messages);
            }
            else
            {
                return new SemVer(major, int.MaxValue, int.MaxValue);
            }

            // Patch
            int patch = 0;
            if (term == '.')
            {
                ++p;
                (patch, term) = ParseIntComponent(s, ref p, cPatch, messages);
            }
            else
            {
                return new SemVer(major, minor, int.MaxValue);
            }

            // Prerelease
            string? prerelease = null;
            if (term == '-')
            {
                ++p;
                (prerelease, term) = ParseTailComponent(s, ref p, cPrerelease, messages);
            }

            // Build
            string? build = null;
            if (term == '+')
            {
                ++p;
                (build, term) = ParseTailComponent(s, ref p, cBuild, messages);
            }

            return  new SemVer(major, minor, patch, prerelease, build);
        }


        private static (int value, char term) ParseIntComponent(string s, ref int p, string partName, StringBuilder messages)
        {
            if (p >= s.Length || s[p] < '0' || s[p] > '9')
            {
                messages.AppendLine($"Warning: Empty {partName} version; using zero.");
                return (0, (p < s.Length) ? s[p] : '\0');
            }

            if (p < s.Length-1 && s[p] == '0' && s[p+1] >= '0' && s[p+1] <= '9')
                messages.AppendLine($"Warning: Leading zero(s) removed from {partName} version.");

            int n = 0;
            while (p < s.Length && s[p] >= '0' && s[p] <= '9')
            {
                n = n * 10 + (s[p] - '0');
                ++p;
            }

            return (n, (p < s.Length) ? s[p] : '\0');
        }

        private static (string value, char term) ParseTailComponent(string s, ref int p, string partName, StringBuilder messages)
        {
            bool emptyId = false;
            bool lz = false;
            int start = p;
            var sb = new StringBuilder();
            for (; ; )
            {
                (int i, int len, int num) = NextIdentifier(s, ref p);
                if (len == 0)
                {
                    // Mark an empty ID and skip this ID altogether.
                    emptyId = true;
                }
                else if (num >= 0)
                {
                    if (sb.Length > 0) sb.Append('.');
                    sb.Append(num);
                    if (len > 1 && s[i] == '0') lz = true;
                }
                else
                {
                    if (sb.Length > 0) sb.Append('.');
                    sb.Append(s, i, len);
                }

                if (p >= s.Length || s[p] != '.') break;
                ++p;
            }

            if (emptyId) messages.AppendLine($"Warning: {partName} version includes at least one empty identifier.");
            if (lz) messages.AppendLine($"Warning: Leading zero(s) removed from {partName} version.");
            if (p - start == 0) messages.AppendLine($"Warning: {partName} is empty.");

            return (sb.ToString(), (p < s.Length) ? s[p] : '\0');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdChar(char c)
        {
            return (c >= '0' && c <= '9')
                || (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || c == '-';

        }

        // Prerelease values are compared according to the Semantic Versioning spec at https://semver.org
        private static int ComparePrerelease(string? a, string? b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return string.IsNullOrEmpty(b) ? 0 : 1; // Full release comes before prerelease
            }
            if (string.IsNullOrEmpty(b)) return -1;
            int ac = 0;
            int bc = 0;
            for (; ; )
            {
                // If a has reached the end and b has not then a is lower (and visa versa)
                if (ac >= a.Length)
                {
                    return (bc >= b.Length) ? 0 : -1;
                }
                if (bc >= b.Length) return 1;

                (int ai, int alen, int anum) = NextIdentifier(a, ref ac);
                if (ac < a.Length) ++ac; // Skip the delimiter
                (int bi, int blen, int bnum) = NextIdentifier(b, ref bc);
                if (bc < b.Length) ++bc; // Skip the delimiter

                // Numeric comparison
                if (anum >= 0 && bnum >= 0)
                {
                    int cmp = anum - bnum;
                    if (cmp != 0) return cmp;
                }

                // String comparison
                else
                {
                    int cmp = string.Compare(a, ai, b, bi, Math.Min(alen, blen), StringComparison.Ordinal);
                    if (cmp != 0) return cmp;
                    cmp = alen - blen;
                    if (cmp != 0) return cmp;
                }

                // Values are equal so get the next set of identifiers
            }
        }

        private static (int i, int len, int num) NextIdentifier(string str, ref int cursor)
        {
            int i = cursor;
            int end = str.Length;
            int num = 0;
            while (cursor < end && IsIdChar(str[cursor]))
            {
                if (num >= 0) // Numeric so far
                {
                    if (char.IsDigit(str[cursor]))
                    {
                        num = num * 10 + (str[cursor] - '0');
                    }
                    else
                    {
                        num = -1;   // Not numeric
                    }
                }
                ++cursor;
            }
            int len = cursor - i;
            return (i, len, num);
        }

    }
}
