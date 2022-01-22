using System;
using System.Runtime.InteropServices;

namespace Video2Gba
{
    public class GbaNativeCompression : IDisposable
    {

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

        private IntPtr decompBuffer = IntPtr.Zero;
        private IntPtr srcp = IntPtr.Zero;
        private IntPtr newFramep = IntPtr.Zero;
        private int srcLength = 0;
        private UInt32 compressedSize = 0;
        public GbaNativeCompression(byte[] srca, byte[] newFramea = null)
        {
            //Ready the buffers!
            srcLength = srca.Length;
            decompBuffer = Marshal.AllocCoTaskMem(240 * 160 * 16);//srcLength * 16);
            FillMemory(decompBuffer, (uint)(240 * 160 * 16), 0);
            srcp = Marshal.AllocCoTaskMem(240 * 160 * 16);
            FillMemory(srcp, (uint)(240 * 160 * 16), 0);
            Marshal.Copy(srca, 0, srcp, srcLength);
            newFramep = IntPtr.Zero;

            if (newFramea != null) newFramep = Marshal.AllocCoTaskMem(srcLength);
            if (newFramea != null) Marshal.Copy(newFramea, 0, newFramep, srcLength);
        }

        public byte[] Rle16Compress()
        {
            var dat = new RLE16(srcp, srcLength, true);
            compressedSize = (uint)dat.GetData().Length; ;// RLCustomCompress16(srcp, (uint)srcLength, decompBuffer);
            Marshal.Copy(dat.GetData(), 0, decompBuffer, (int)compressedSize);
            return SetData();
        }

        public byte[] Rle16Decompress()
        {
            var dat = new RLE16(srcp, srcLength, false);
            compressedSize = (uint)dat.GetData().Length; ;// RLCustomCompress16(srcp, (uint)srcLength, decompBuffer);
            Marshal.Copy(dat.GetData(), 0, decompBuffer, (int)compressedSize);
            return SetData();
        }


        public byte[] To1D()
        {
            int dstCounter = 0;
            for(int i = 0;i<srcLength/2;)
            {
                byte firstByte = Marshal.ReadByte(srcp + i);
                byte secondByte = Marshal.ReadByte(srcp + i + 1);

                Marshal.WriteByte(decompBuffer + i , firstByte);
                Marshal.WriteByte(decompBuffer + i + srcLength / 2, secondByte);

                i += 2;

            }
            compressedSize = (uint)srcLength;
            return SetData();
        }

        public byte[] From1D()
        {
            int dstCounter = 0;
            for (int i = 0; i < srcLength / 2;)
            {
                byte firstByte = Marshal.ReadByte(srcp + i);
                byte secondByte = Marshal.ReadByte(srcp + i + srcLength / 2);

                Marshal.WriteByte(decompBuffer + i , firstByte);
                Marshal.WriteByte(decompBuffer + i + 1 , secondByte);

                i += 2;
            }
            compressedSize = (uint)srcLength;
            return SetData();
        }


        public void Dispose()
        {
            if (srcp != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(srcp); srcp = IntPtr.Zero;
            }
            if (newFramep != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(newFramep); newFramep = IntPtr.Zero;
            }
            if (decompBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(decompBuffer); decompBuffer = IntPtr.Zero;
            }
        }

        public void ThrowAss()
        {
            throw new Exception("Buffer is too big.");
        }

        public byte[] SetData()
        {
            if (compressedSize == 0) ThrowAss();

            //Make a new return buffer
            byte[] newBuffer = new byte[compressedSize];
            Marshal.Copy(decompBuffer, newBuffer, 0, (int)compressedSize);
            return newBuffer;
        }
    }
}
