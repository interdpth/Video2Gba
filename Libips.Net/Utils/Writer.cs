using System;
using System.IO;
namespace Video2Gba.LibIpsNet.Utils
{
    public class Writer
    {
        // Helper to write 8bit.
        public static void Write8(byte value, Stream stream)
        {
            stream.WriteByte(value);
        }
        // Helper to write 16bit.
        public static void Write16(int value, Stream stream)
        {
            ushort val = (ushort)value;
            byte[] b = BitConverter.GetBytes(val);
            stream.Write(b, 0, 2);
        }
        // Helper to write 24bit.
        public static void Write24(int value, Stream stream)
        {
            uint val = (uint)value;
            byte[] b = BitConverter.GetBytes(val);
            stream.Write(b, 0, 4);
        }
    }
}
