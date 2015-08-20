using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace librsync.net
{
    public static class StreamHelpers
    {
        public static void WriteBigEndian(BinaryWriter s, ulong value, int bytes = 4)
        {
            byte[] buffer = new byte[8];
            buffer[0] = (byte)(value >> 56);
            buffer[1] = (byte)(value >> 48);
            buffer[2] = (byte)(value >> 40);
            buffer[3] = (byte)(value >> 32);
            buffer[4] = (byte)(value >> 24);
            buffer[5] = (byte)(value >> 16);
            buffer[6] = (byte)(value >> 8);
            buffer[7] = (byte)(value);
            s.Write(buffer, 8 - bytes, bytes);
        }

        public static uint ReadBigEndianUint32(BinaryReader s)
        {
            return (uint)ConvertFromBigEndian(s.ReadBytes(4));
        }

        public static long ConvertFromBigEndian(byte[] bytes)
        {
            long result = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                result = result << 8 | bytes[i];
            }

            return result;
        }

        public static long ComputeNewPosition(long offset, SeekOrigin origin, long length, long currentPosition)
        {
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = currentPosition + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = length + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid SeekOrigin");
            }

            return newPosition;
        }
    }
}
