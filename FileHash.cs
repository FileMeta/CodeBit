using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit {
    internal static class FileHash {

        const string c_prefixSHA256 = "SHA256:";

        /// <summary>
        /// Calculate the SHA256 hash of a stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string GetHashNormEol(Stream stream) {
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] hash = sha256.ComputeHash(stream);
                return c_prefixSHA256 + Convert.ToHexString(hash);
            }
        }
    }

    class LineEndFilterStream : Stream {

        const int c_bufSize = 2;
        Stream m_internalStream;
        byte[] m_buffer = new byte[c_bufSize];
        int m_bufPos = 0;
        int m_bufEnd = 0;
        bool m_disposeStream;
        bool m_isDisposed = false;

        public LineEndFilterStream(Stream stream, bool dispose = true) {
            m_internalStream = stream;
            m_disposeStream = dispose;
        }

        public override int Read(byte[] buffer, int offset, int count) {

            int outPos = offset;
            int outEnd = offset + count;
            while (outPos < outEnd) {
                if (m_bufPos >= m_bufEnd) {
                    RefillBuffer();
                    if (m_bufPos >= m_bufEnd) break;
                }

                if (m_buffer[m_bufPos] == '\r') {
                    if (m_bufPos + 1 >= m_bufEnd) {
                        RefillBuffer();
                    }
                    // Suppress the \r of a \r\n pair
                    if (m_bufPos + 1 < m_bufEnd && m_buffer[m_bufPos + 1] == '\n') {
                        ++m_bufPos;
                    }
                }
                buffer[outPos] = m_buffer[m_bufPos];
                ++outPos;
                ++m_bufPos;
            }
            return outPos - offset;
        }

        private void RefillBuffer() {
            if (m_bufPos < m_bufEnd) {
                Buffer.BlockCopy(m_buffer, m_bufPos, m_buffer, 0, m_bufEnd - m_bufPos);
                m_bufEnd -= m_bufPos;
                m_bufPos = 0;
            }
            else {
                m_bufPos = m_bufEnd = 0;
            }
            Console.WriteLine($"{c_bufSize - m_bufEnd}");
            int bytesRead = m_internalStream.Read(m_buffer, m_bufEnd, c_bufSize - m_bufEnd);
            m_bufEnd += bytesRead;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException("Length not supported on LineEndFilterStream");

        public override long Position { get => throw new NotSupportedException("Position not supported on LineEndFilterStream"); set => throw new NotSupportedException("Position not supported on LineEndFilterStream"); }

        public override void Flush() {
            // Do nothing
        }
        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("Seek() not supported on LineEndFilterStream");
        }

        public override void SetLength(long value) {
            throw new NotSupportedException("SetLength() not supported on LineEndFilterStream");
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("Write() not supported on LineEndFilterStream");
        }

        protected override void Dispose(bool disposing) {
            if (!m_isDisposed && m_disposeStream) {
                m_isDisposed = true;
                m_internalStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
