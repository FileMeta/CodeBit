/*
CodeBit Metadata

&name=Bredd.tech/CommandLineLexer.cs
&description="CodeBit class for lexing (parsing) command lines."
&author="Brandt Redd"
&url=https://raw.githubusercontent.com/bredd/CommandLineLexer/main/CommandLineLexer.cs
&version=2.0.0
&successorOf=https://raw.githubusercontent.com/bredd/CommandLineLexer/54ca47c0eca9572072bc1492da2d0eb447afb504/CommandLineLexer.cs
&keywords=CodeBit
&datePublished=2022-09-21
&license=https://opensource.org/licenses/BSD-3-Clause

About Codebits: http://www.filemeta.org/CodeBit
/ph red?
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bredd
{
    /// <summary>
    /// Yet another command-line parsing helper class.
    /// </summary>
    /// <remarks>
    /// <para>This one command-line tool uses a lexer structure and expects the application
    /// to read the command-line options in sequence. Distinction from a simple
    /// foreach loop on 'args' is that this class makes error reporting easier
    /// by tracking the context.
    /// </para>
    /// <para>The lexer is sensitive to whether an argument is quoted or not. For example,
    /// <c>-i</c> is an option whereas <c>"-i"</c> is not. Among other advantages, this
    /// allows filenames that start with dash to be distinguished from options. It also
    /// provides for better error detection and reporting.
    /// </para>
    /// <para>In order to be quote-sensitive, the lexer takes the full original command
    /// line instead of the traditional argument list. The default constructor uses
    /// this or you can pass <see cref="Environment.CommandLine"/> to the constructor.
    /// </para>
    /// </remarks>
    class CommandLineLexer : IEnumerator<string>
    {
        string m_args;
        int m_start;
        int m_pos;
        string m_currentArg;
        string m_latestOption;
        bool m_isQuoted;
        bool m_isOption;

        /// <summary>
        /// Initialize with the command line from the environment
        /// </summary>
        /// <param name="acceptEmpty">If true, accept an empty command line. Else throws an exception.</param>
        public CommandLineLexer()
            : this(Environment.CommandLine)
        {
            ReadNextArg();  // Read the application filename
            m_start = m_pos;
            m_currentArg = null;
        }

        /// <summary>
        /// Initialize with the specified arguments.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        /// <param name="acceptEmpty">If true, accept an empty command line. Else throws an exception.</param>
        public CommandLineLexer(string args)
        {
            m_args = args;
            m_start = 0;
            Reset();
        }

        /// <summary>
        /// Reset to the beginning of the command line
        /// </summary>
        public void Reset()
        {
            m_pos = m_start;
            m_currentArg = null;
            m_isOption = false;
            m_latestOption = String.Empty;
            m_isQuoted = false;
        }

        /// <summary>
        /// The current argument as a string
        /// </summary>
        public string Current
        {
            get
            {
                if (m_currentArg == null)
                {
                    throw new InvalidOperationException(
                        (m_pos <= m_start) ? "CommandLineLexer: Must call MoveNext() before Current"
                        : "CommandLineLexer: All arguments have been read.");
                }
                return m_currentArg;
            }
        }

        /// <summary>
        /// Indicates whether the current argument is an option.
        /// </summary>
        /// <remarks>
        /// A command-line argument starts with a dash.
        /// </remarks>
        public bool IsOption => m_isOption;

        /// <summary>
        /// Indicates whether the current argument was quoted on the command line.
        /// </summary>
        /// <remarks>Quotes are removed during lexing. This indicates whether they
        /// were originally present.
        /// </remarks>
        public bool IsQuoted => m_isQuoted;

        /// <summary>
        /// The current argument in the iteration.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Returns the current argument as an integer.
        /// </summary>
        /// <exception cref="CommandLineException">Thrown if the value is not an integer.</exception>
        public int CurrentAsInt
        {
            get
            {
                int value;
                if (!int.TryParse(Current, out value))
                    throw new CommandLineException($"{GetOptionErrPrefix(m_latestOption)}Expected integer; found \"{m_currentArg}\".");
                return value;
            }
        }

        /// <summary>
        /// The latest option is the most recent argument that was read as an option.
        /// </summary>
        /// <remarks>
        /// <para> It will have a dash (-) prefix. Filenames may also start with a dash so
        /// the defining feature is that it was read as an option.
        /// </para>
        /// <para>This is useful for error reporting with an invalid value. It may also be
        /// cleared (by setting to null or empty string) or set to a special value.
        /// </para>
        /// </remarks>
        public string LatestOption
        {
            get
            {
                return m_latestOption;
            }

            set
            {
                m_latestOption = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Disposal is not required - does nothing
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <summary>
        /// Moves to the next argument. Returns true if another argument is present. Otherwise, false.
        /// </summary>
        /// <returns>True if another argument was read. Else, false.</returns>
        public bool MoveNext()
        {
            m_isOption = false;
            while (m_pos < m_args.Length && char.IsWhiteSpace(m_args[m_pos])) ++m_pos;
            if (m_pos >= m_args.Length)
            {
                m_currentArg = null;
                return false;
            }

            // Quoted argument
            if (m_args[m_pos] == '"')
            {
                m_isQuoted = true;
                m_pos++;
                var sb = new StringBuilder();
                while (m_pos < m_args.Length)
                {
                    if (m_args[m_pos] == '"')
                    {
                        ++m_pos;

                        // If not two consecutive quotes exit the loop
                        if (m_pos >= m_args.Length || m_args[m_pos] != '"')
                            break;
                    }
                    sb.Append(m_args[m_pos]);
                    ++m_pos;
                }
                m_currentArg = sb.ToString();
            }

            else
            {
                int anchor = m_pos;
                while (m_pos < m_args.Length && !char.IsWhiteSpace(m_args[m_pos]))
                    ++m_pos;
                m_currentArg = m_args.Substring(anchor, m_pos - anchor);

                System.Diagnostics.Debug.Assert(m_currentArg.Length > 0);
                if (m_currentArg[0] == '-')
                {
                    m_isOption = true;
                    m_latestOption = m_currentArg;
                }
            }

            return true;
        }

        /// <summary>
        /// Moves to the next argument and returns it.
        /// </summary>
        /// <returns>The next argument or null if at the end of the argument list.</returns>
        public string ReadNextArg()
        {
            if (!MoveNext()) return null;
            return m_currentArg;
        }

        /// <summary>
        /// Moves to the next option and returns it.
        /// </summary>
        /// <returns>The next option or null if at the end of the argument list.</returns>
        /// <remarks>
        /// <para>The next argument must be an option, not a value. That is, it must be
        /// prefixed with a hyphen. If the next argument does not have a hyphen prefix
        /// then a <see cref="CommandLineException"/> is thrown with a meaningful error
        /// message. If there are no more arguments then null is returned.</para>
        /// <para>The null return behavior is in contrast with <see cref="ReadNextValue"/>
        /// which throws an exception if there are no more arguments. But it makes sense
        /// in the context in which each method is used.</para>
        /// </remarks>
        /// <returns>The next option in the argument list or null if no arguments remain.</returns>
        /// <exception cref="CommandLineException">Thrown if the next argument is not an option.</exception>
        public string ReadNextOption()
        {
            if (!MoveNext()) return null;
            if (!IsOption)
                throw new CommandLineException($"Expected option but found argument \"{m_currentArg}\".");
            return m_currentArg;
        }

        /// <summary>
        /// Advances to the next value and returns it.
        /// </summary>
        /// <returns>The string value of the next argument.</returns>
        /// <remarks>
        /// The next argument must be a value, not an option. That is, it must not be
        /// prefixed with a hyphen. If at the end of the argument string or the value
        /// is an option then an <see cref="CommandLineException"/> is thrown.
        /// </remarks>
        /// <exception cref="CommandLineException">Thrown with a user-friendly message
        /// if at the end of the argument string or the next argument is an option.</exception>
        public string ReadNextValue()
        {
            string latestOption = m_latestOption; // Save this first because if the next value is an option it will be overwritten
            if (!MoveNext())
                throw new CommandLineException(GetOptionErrPrefix(latestOption) + "Expected value but reached end of argument list.");
            if (IsOption)
                throw new CommandLineException($"{GetOptionErrPrefix(latestOption)}Expected value but found option \"{m_currentArg}\".");
            return m_currentArg;
        }

        /// <summary>
        /// Advances to the next value and returns it as an integer.
        /// </summary>
        /// <returns>The string value of the next argument.</returns>
        /// <remarks>
        /// The next argument must be a value, not an option. That is, it must not be
        /// prefixed with a hyphen. If at the end of the argument string or the value
        /// is an option then an <see cref="CommandLineException"/> is thrown.
        /// </remarks>
        /// <exception cref="CommandLineException">Thrown if at the end of the argument string
        /// or the next argument is an option.</exception>
        public int ReadNextValueAsInt()
        {
            ReadNextValue();
            return CurrentAsInt;
        }

        /// <summary>
        /// If the current argument is not an option, throws a <see cref="CommandLineException"/>
        /// with a meaningful error message.
        /// </summary>
        /// <exception cref="CommandLineException">Thrown if the current argument is not an option.</exception>
        public void ThrowIfNotOption()
        {
            if (!m_isOption)
                throw new CommandLineException($"Option expected. Found \"{m_currentArg}\"");
        }

        /// <summary>
        /// If the current argument is not a value, throws a <see cref="CommandLineException"/>
        /// with a meaningful error message.
        /// </summary>
        /// <exception cref="CommandLineException">Thrown if the current argument is not an option.</exception>
        public void ThrowIfNotValue()
        {
            if (m_isOption)
                throw new CommandLineException($"Value expected. Found \"{m_currentArg}\"");
        }

        /// <summary>
        /// Throws a <see cref="CommandLineException"/> regarding the <see cref="LatestOption"/>
        /// </summary>
        /// <param name="message">An error message.</param>
        /// <remarks>The error message is prefixed with "Command Line Error on Option" with the
        /// option name if CurrentOption has a value. Otherwise prefixed with "Command Line Error"
        /// </remarks>
        /*[DoesNotReturn] Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowValueError(string message)
        {
            throw new CommandLineException(GetOptionErrPrefix(m_latestOption) + message);
        }

        /// <summary>
        /// Throws an error indicating that the current argument was unexpected.
        /// </summary>
        /*[DoesNotReturn]  Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowUnexpectedArgError()
        {
            throw new CommandLineException("Command Line Error: Unexpected argument: " + m_currentArg);
        }

        /// <summary>
        /// Throws an error indicating that the current argument was unexpected.
        /// </summary>
        /*[DoesNotReturn]  Requires .Net 5.0_ or .Net Core 3.0+ */
        public void ThrowUnknownOptionError()
        {
            throw new CommandLineException("Command Line Error: Unknown option: " + m_currentArg);
        }

        private string GetOptionErrPrefix(string option)
        {
            return (string.IsNullOrEmpty(option))
                ? String.Empty
                : $"Following Option \"{option}\" ";
        }

    }

    /// <summary>
    /// An error found when parsing the command line.
    /// </summary>
    /// <remarks>
    /// The <see cref="Message"/> value is always suitable for reporting to the user.
    /// </remarks>
    class CommandLineException : Exception
    {
        public CommandLineException(string errorMessage)
            : base("Command Line Error: " + errorMessage)
        {
        }

    }
}
