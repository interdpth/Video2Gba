using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Video2Gba.LibIpsNet;

namespace Video2Gba
{
   public static class FrameOps
    {

        public static byte[] RawFrame(byte[] frame)
        {
            byte[] buffer = new byte[frame.Length + 8];

            BitConverter.GetBytes(0x88FFFF00).CopyTo(buffer, 0);
            BitConverter.GetBytes(frame.Length).CopyTo(buffer, 4);
            frame.CopyTo(buffer, 8);

            return buffer;
        }

        public static byte[] LzCompress(byte[] frame)
        {
            IOStream src = new IOStream(frame);
            IOStream output = VideoCompression.CompLZ77(src, frame.Length);

            byte[] returnvalue = new byte[8 + output.Length];
            BitConverter.GetBytes(CompressionHeaders.LZCOMPRESSEDHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(frame.Length).CopyTo(returnvalue, 4);
            output.CopyToArray(0, returnvalue, 8, (int)output.Length);

            return returnvalue;
        }
        public static byte[] FrameCompareCompQuad(byte[] oldFrame, byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2Quad(buffer, 240, 160);



            List<byte[]> old = BufferHelper.Buffer2Quad(oldFrame, 240, 160);


            List<byte[]> compressedDifferences = new List<byte[]>();
            int quadlen = 0;
            Creator c = new Creator();
            for (int i = 0; i < 4; i++)
            {
                using (var olds = new MemoryStream(old[i]))
                {
                    using (var newf = new MemoryStream(newBuffers[i]))
                    {
                        var str = c.Create(olds, newf);
                        compressedDifferences.Add(str);
                        quadlen += (int)str.Length;
                    }
                }
            }

            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.QUADDIFFHEADER);
            src.WriteU32((uint)quadlen);


            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);

            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }

        public static byte[] FrameCompareCompQuad2(byte[] oldFrame, byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2Quad(buffer, 240, 160);



            List<byte[]> old = BufferHelper.Buffer2Quad(oldFrame, 240, 160);


            List<byte[]> compressedDifferences = new List<byte[]>();
            int quadlen = 0;
            Creator c = new Creator();
            for (int i = 0; i < 4; i++)
            {
                using (var olds = new MemoryStream(old[i]))
                {
                    using (var newf = new MemoryStream(newBuffers[i]))
                    {
                        var str = c.Create16(olds, newf);
                        compressedDifferences.Add(str);
                        quadlen += (int)str.Length;
                    }
                }
            }


            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.QUADDIFFHEADER2);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }


        public static byte[] FrameCompareCompQuadInterleaved(byte[] oldFrame, byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2Interleave(buffer, 2);
            List<byte[]> old = BufferHelper.Buffer2Interleave(oldFrame, 2);

            List<byte[]> compressedDifferences = new List<byte[]>();
            int quadlen = 0;
            Creator c = new Creator();
            for (int i = 0; i < newBuffers.Count; i++)
            {
                using (var olds = new MemoryStream(old[i]))
                {
                    using (var newf = new MemoryStream(newBuffers[i]))
                    {
                        var str = c.Create(olds, newf);
                        compressedDifferences.Add(str);
                        quadlen += (int)str.Length;
                    }
                }
            }


            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.INTERLACERLEHEADER);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }


        public static byte[] FrameCompareCompQuadInterleaved2(byte[] oldFrame, byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2Interleave(buffer, 2);
            List<byte[]> old = BufferHelper.Buffer2Interleave(oldFrame, 2);

            List<byte[]> compressedDifferences = new List<byte[]>();
            int quadlen = 0;
            Creator c = new Creator();
            for (int i = 0; i < newBuffers.Count; i++)
            {
                using (var olds = new MemoryStream(old[i]))
                {
                    using (var newf = new MemoryStream(newBuffers[i]))
                    {
                        var str = c.Create(olds, newf);
                        compressedDifferences.Add(VideoCompression.GBatroidRLE(str));
                        quadlen += (int)str.Length;
                    }
                }
            }


            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.INTERLACERLEHEADER2);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }


        /// <summary>
        /// Takes a buff assuming width * height image with an RGB being two bytes.
        /// </summary>
        /// <param name="buff">Data source</param>
        /// <param name="width">Source image width</param>
        /// <param name="height">Source image height</param>
        /// <returns>image split in 4s</returns>

        public static byte[] FrameCompareCompQuadNinty(byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            //if (VideoCompression.oldFrame == null)
            //{
            //    return buffer;
            //}
            //Split the arrays up.



            var str = VideoCompression.RLCompWrite(buffer, buffer.Length);




            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.QUADDIFFHEADER);
            src.WriteU32((uint)0);

            src.CopyFromArray(str, str.Length);

            returnValue = src.Data;

            return returnValue;
        }



        public static byte[] FrameCompareCompQuadNinty2(byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (VideoCompression.oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2Quad(buffer, 240, 160);



            List<byte[]> compressedDifferences = new List<byte[]>();
            int quadlen = 0;
            Creator c = new Creator();
            for (int i = 0; i < 4; i++)
            {

                var str = VideoCompression.RLCompWrite(newBuffers[i], newBuffers[i].Length);
                compressedDifferences.Add(str);
                quadlen += (int)str.Length;
            }

            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.NINTYRLHEADER);
            src.WriteU32((uint)quadlen);



            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }




        public static byte[] FrameCompareCompQuadNinty3(byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
       
            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2QuadStraight(buffer);


            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.NINTYRLHEADERINTR);

            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in newBuffers) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }




        public static byte[] FrameCompareCompQuadNinty4(byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }

            //Split the arrays up.

            List<byte[]> newBuffers = BufferHelper.Buffer2QuadStraight(buffer);
            for (int i = 0; i < newBuffers.Count; i++) newBuffers[i] = VideoCompression.GBatroidRLE(newBuffers[i]);

            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.NINTYRLHEADERINTR);

            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in newBuffers) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }


        public static byte[] GetDifferences(byte[] src, byte[] newArr)
        {
            IOStream io = new IOStream();
            IOStream offstream = new IOStream();
            IOStream datastream = new IOStream();
            List<int> offsetTable = new List<int>();
            for(int i = 0; i<newArr.Length;i++)
            {
                byte s = src[i];
                byte n = newArr[i];
                if(s!=n)
                {
                    //WE HAVE A CHANGE.
                   // io.Write32(i);
                    offsetTable.Add(i);
                        IOStream writeStr = new IOStream();
                    while(s!=n && i+1 < newArr.Length)
                    {
                        writeStr.Write8(n);
                        i++;
                        s = src[i];
                        n = newArr[i];
                    }
                    datastream.Write32((Int32)writeStr.Length);

                    //if(writeStr.Length>4)
                    //{
                    //    var c = VideoCompression.RLCompWrite(writeStr.Data);
                    //    datastream.CopyFromArray(c, c.Length);
                    //}
                    //else
                    //{
                        datastream.CopyFromArray(writeStr.Data, (Int32)writeStr.Length);
                    //}
                    

                }

                io.CopyFromArray(offstream.Data, (int) offstream.Length);
                io.CopyFromArray(datastream.Data, (int)datastream.Length);
            }

            return io.Data;
        }

        public static byte[] FrameCompare(byte[] buffer)
        {
            byte[] returnValue = new byte[1];
            if (buffer.Length < 200)
            {
                return buffer;
            }
            if (VideoCompression.oldFrame == null)
            {
                return buffer;
            }
            //Split the arrays up.
            List<byte[]> newerBuffers = BufferHelper.Buffer2QuadStraight(buffer);

            List<byte[]> oldBuffers = BufferHelper.Buffer2QuadStraight(VideoCompression.oldFrame);
            List<byte[]> newBuffers = new List<byte[]>() { new byte[1], new byte[1], new byte[1], new byte[1] };

          
            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.NINTYRLHEADERINTR);
            src.Write16((ushort)newerBuffers.Count);
            for (int i =0;i< newerBuffers.Count;i++)
            {   
                newBuffers[i] = GetDifferences(oldBuffers[i], newerBuffers[i]);
                var lz = VideoCompression.CompLZ77(newBuffers[i]).Data;
                var rl = VideoCompression.RLCompWrite(newBuffers[i]);
                int compressionType = 0;
                int bestSize = newBuffers[i].Length;
                if (bestSize > lz.Length)
                {
                    bestSize = lz.Length;
                    newBuffers[i] = lz;
                    compressionType = 1;
                }

                if (bestSize > rl.Length)
                {
                    newBuffers[i] = rl;
                    compressionType = 2;
                }

                src.Write8((byte)compressionType);         
            }

 

            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in newBuffers) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }


        public static void CompressFile2(ref byte[] buffer, string fn, string output, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            ROM.MakeSource(fn, Compress(ref buffer), output);

            VideoCompression.oldFrame = buffer;



        }


        public static void CompressFile(byte[] buffer, string fn, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            File.WriteAllBytes(fn, Compress(ref buffer));
            VideoCompression.oldFrame = buffer;
        }
        public static byte[] Compress(ref byte[] buffer, int enableCircleComp = 3)
        {

            byte[] bestData = RawFrame(buffer);

            //See who has best data. 
            int bestSize = buffer.Length;//raw 

            byte[] lz = LzCompress(buffer);
            // byte[] diff = FrameCompare(buffer);
            ////  byte[] difflz = FrameCompareCompQuad(buffer);


            //byte[] nint2 = FrameCompareCompQuadNinty2(buffer);
            //byte[] nint3 = FrameCompareCompQuadNinty3(buffer);
            //byte[] nint4 = FrameCompareCompQuadNinty4(buffer);

            //byte[] RlEd = VideoCompression.GBatroidRLE(buffer);


            //byte[] nint5 = LzCompress(nint4);

            //byte[] RlEd2 = LzCompress(RlEd);


            if (lz.Length < bestSize)
            {
                bestSize = lz.Length;
                bestData = lz;
            }
            //if (nint2.Length < bestSize)
            //{
            //    bestSize = nint2.Length;
            //    bestData = nint2;
            //}
            //if (nint3.Length < bestSize)
            //{
            //    bestSize = nint3.Length;
            //    bestData = nint3;
            //}
            //if (diff.Length < bestSize)
            //{
            //    bestSize = diff.Length;
            //    bestData = diff;
            //}
            //if (RlEd.Length < bestSize)
            //{
            //    bestSize = RlEd.Length;
            //    bestData = RlEd;
            //}
            //if (nint4.Length < bestSize)
            //{
            //    bestSize = nint4.Length;
            //    bestData = nint4;
            //}


            //if (RlEd2.Length < bestSize)
            //{
            //    bestSize = RlEd2.Length;
            //    bestData = RlEd2;
            //}
            //if (nint5.Length < bestSize)
            //{
            //    bestSize = nint5.Length;
            //    bestData = nint5;
            //}

            //if (diff.Length < bestSize)
            //{
            //    bestSize = diff.Length;
            //    bestData = diff;
            //}


            //if (difflz.Length < bestSize)
            //{
            //    bestSize = difflz.Length;
            //    bestData = difflz;
            //}

            //if (difflz2.Length < bestSize)
            //{
            //    bestSize = difflz2.Length;
            //    bestData = difflz2;
            //}

            //if (interleave.Length < bestSize)
            //{
            //    bestSize = interleave.Length;
            //    bestData = interleave;
            //}
            //if (interleave2.Length < bestSize)
            //{
            //    bestSize = interleave2.Length;
            //    bestData = interleave2;
            //}
            //if (RlEd.Length < bestSize)
            //{
            //    bestSize = RlEd.Length;
            //    bestData = RlEd;
            //}


            byte[] file = new byte[bestSize + 4];


            BitConverter.GetBytes(bestSize).CopyTo(file, 0);


            bestData.CopyTo(file, 4);

            return file;


        }

    }
}
