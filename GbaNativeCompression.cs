using System;
using System.Runtime.InteropServices;

namespace Video2Gba
{
    public class GbaNativeCompression : IDisposable
    {
        [DllImport(@"ntrcomp.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern UInt32 To1D(IntPtr srcp, UInt32 size);

        [DllImport(@"ntrcomp.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern UInt32 RLCompWrite16(IntPtr srcp, UInt32 size, IntPtr dstp);
        [DllImport(@"ntrcomp.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern UInt32 RLCompWrite(IntPtr srcp, UInt32 size, IntPtr dstp);
        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 LZCompWrite(IntPtr srcp, UInt32 size, IntPtr dstp, byte lzSearchOffset);
        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 HuffCompWrite(IntPtr srcp, UInt32 size, IntPtr dstp, byte huffBitSize);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 Differ8(IntPtr srcp, IntPtr newframe, UInt32 size, IntPtr dstp);
        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 Differ16(IntPtr srcp, IntPtr newframe, UInt32 size, IntPtr dstp);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 RLCompRead(IntPtr srcp, UInt32 size, IntPtr dstp);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 RLCompRead16(IntPtr srcp, UInt32 size, IntPtr dstp);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 LZCompRead(IntPtr srcp, UInt32 size, IntPtr dstp);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 OneDCompRead(IntPtr srcp, UInt32 size);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 RLCustomCompress16(IntPtr srcp, UInt32 size, IntPtr dstp);

        [DllImport(@"ntrcomp.dll")]
        private static extern UInt32 RLCustomDecompress16(IntPtr srcp, UInt32 size, IntPtr dstp);


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
            decompBuffer = Marshal.AllocCoTaskMem(240 * 160 *16);//srcLength * 16);
            FillMemory(decompBuffer, (uint)(240 * 160 * 16), 0);
            srcp = Marshal.AllocCoTaskMem(srcLength * 1025);
            FillMemory(srcp, (uint)(srcLength * 16), 0);
            Marshal.Copy(srca, 0, srcp, srcLength);
            newFramep = IntPtr.Zero;

            if (newFramea != null) newFramep = Marshal.AllocCoTaskMem(srcLength);
            if (newFramea != null) Marshal.Copy(newFramea, 0, newFramep, srcLength);
        }

        public byte[] Lz77Compress()
        {
            compressedSize = LZCompWrite(srcp, (uint)srcLength, decompBuffer, 0);
            return SetData();
        }

        public byte[] Lz77Deompress()
        {
            compressedSize = LZCompRead(srcp, (uint)srcLength, decompBuffer);
            return SetData();
        }

        public byte[] RleCompress()
        {
            compressedSize = RLCompWrite(srcp, (uint)srcLength, decompBuffer);
            return SetData();
        }

        public byte[] Rle16Compress()
        {
            //  compressedSize = RLCompWrite16(srcp, (uint)srcLength, decompBuffer);
            return CustomCompresssRLE16();
            // return SetData();
            // return SetData();
        }

        public byte[] RleDecompress()
        {
            compressedSize = 0;//RLCompRead(srcp + 4, (uint)srcLength - 4, decompBuffer);
            return SetData();
        }

        public byte[] Rle16Decompress()
        {
            return CustomDecompresssRLE16();
        }

        public byte[] Differ8Compress()
        {
            compressedSize = Differ8(srcp, newFramep, (uint)srcLength, decompBuffer);
            return SetData();
        }

        public byte[] Differ16Compress()
        {
            compressedSize = Differ16(srcp, newFramep, (uint)srcLength, decompBuffer);
            return SetData();
        }

        public byte[] HuffCompress()
        {
            compressedSize = HuffCompWrite(srcp, (uint)srcLength, decompBuffer, 8);
            return SetData();
        }
        public byte[] Set1D()
        {
            compressedSize = To1D(srcp, (uint)srcLength);

            return SetDataFromSelf();
        }
        public byte[] From1D()
        {
            compressedSize = OneDCompRead(srcp, (uint)srcLength);
            return SetDataFromSelf();
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

        public byte[] SetDataFromSelf()
        {
            if (compressedSize == 0) ThrowAss();
            //Make a new return buffer

            byte[] newBuffer = new byte[compressedSize];
            Marshal.Copy(srcp, newBuffer, 0, (int)compressedSize);
            return newBuffer;
        }

        public byte[] CustomCompresssRLE16()
        {
            var dat= new RLE16(srcp, srcLength, true);
            compressedSize = (uint)dat.GetData().Length; ;// RLCustomCompress16(srcp, (uint)srcLength, decompBuffer);
            Marshal.Copy(dat.GetData(), 0, decompBuffer, (int) compressedSize);
            return SetData();
        }

        public byte[] CustomDecompresssRLE16()
        {
            var dat = new RLE16(srcp, srcLength, false);
            compressedSize = (uint)dat.GetData().Length; ;// RLCustomCompress16(srcp, (uint)srcLength, decompBuffer);
            Marshal.Copy(dat.GetData(), 0, decompBuffer, (int)compressedSize);
            return SetData();
        }
    }
}
