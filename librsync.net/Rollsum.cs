using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace librsync.net
{
    internal class Rollsum
    {
        private const byte RS_CHAR_OFFSET = 31;

        /// <summary>
        /// Count of bytes included in sum
        /// </summary>
        public ulong Count { get; private set; }

        /// <summary>
        /// s1 part of sum
        /// </summary>
        ulong s1;

        /// <summary>
        /// s2 part of sum
        /// </summary>
        ulong s2;

        public void Update(byte[] buf)
        {
            int i;
            ulong s1 = this.s1;
            ulong s2 = this.s2;

            this.Count += (ulong)buf.Length;
            for (i = 0; i < (buf.Length - 4); i += 4)
            {
                s2 += 4 * (s1 + buf[i]) + 3u * buf[i + 1] +
                        2u * buf[i + 2] + buf[i + 3] + 10 * RS_CHAR_OFFSET;
                s1 += ((uint)buf[i + 0] + buf[i + 1] + buf[i + 2] + buf[i + 3] +
                       4 * RS_CHAR_OFFSET);
            }
            for (; i < buf.Length; i++)
            {
                s1 += (ulong)(buf[i] + RS_CHAR_OFFSET);
                s2 += s1;
            }

            this.s1=s1;
            this.s2=s2;
        }

        public int Digest
        {
            get
            {
                return (int)((this.s2 << 16) | (this.s1 & 0xffff));
            }
        }

        /// <summary>
        /// This transforms the rolling sum by removing byteOut from the beginning of the block and adding
        /// byteIn to the end.
        /// Thus if the data before was a checksum for buf[0..n], it becomes a checksum for
        /// buf[1..n+1], assuming byteOut=buf[0] and byteIn = buf[n+1]
        /// </summary>
        public void Rotate(byte byteOut, byte byteIn)
        {
            this.s1 += (ulong)(byteIn - byteOut);
            this.s2 += this.s1 - this.Count * (ulong)(byteOut + RS_CHAR_OFFSET);
        }

        public void Rollout(byte byteOut)
        {
            this.s1 -= (ulong)(byteOut - RS_CHAR_OFFSET);
            this.s2 -= this.Count * (ulong)(byteOut * RS_CHAR_OFFSET);
            this.Count--;
        }
    }
}
