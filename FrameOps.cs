using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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


        public static byte[] DiffFrame2(byte[] oldframe, byte[] newframe)
        {


            IOStream stream = new IOStream(8);


            BitConverter.GetBytes(0x88FFFF22).CopyTo(stream.Data, 0);
            stream.Position = 4;
            for (int i = 0; i < oldframe.Length; i++)
            {
                //if it's more than a pixel we copy. 

                byte src = oldframe[i];
                byte cmp = newframe[i];


                if (src != cmp)
                {
                    if (i + 1 == oldframe.Length - 1) break; // WE finish up.
                    int copyCounter = i;
                    List<byte> diffs = new List<byte>();
                    for (; copyCounter < oldframe.Length; copyCounter++)
                    {
                        src = oldframe[copyCounter];
                        cmp = newframe[copyCounter];
                        if (src == cmp)
                        {
                            //we done.
                            break;
                        }
                        diffs.Add(cmp);
                    }

                    stream.Write32(i);
                    int size = copyCounter - i;
                    stream.Write32(size);
                    stream.WriteData(diffs.ToArray(), 1, diffs.Count);
                    i += copyCounter-i;//Difference
                }
            }


            return stream.Data;
        }

        public static byte[] DiffFrame(byte[] frame, byte[] newf)
        {
            short[] oldframe = new short[frame.Length / 2];
            short[] newframe = new short[frame.Length / 2];

            Array.Copy(frame, oldframe, frame.Length / 2);

            Array.Copy(newf, newframe, frame.Length / 2);

            IOStream stream = new IOStream(8);


            BitConverter.GetBytes(0x88FFFF23).CopyTo(stream.Data, 0);

            for (int i = 0; i < oldframe.Length; i++)
            {
                //if it's more than a pixel we copy. 

                int src = oldframe[i];
                int cmp = newframe[i];


                if (src != cmp)
                {
                    if (i + 1 == oldframe.Length - 1) break; // WE finish up.
                    int copyCounter = i;
                    for (; copyCounter < oldframe.Length; copyCounter++)
                    {
                        src = oldframe[copyCounter];
                        cmp = newframe[copyCounter];
                        if (src == cmp)
                        {
                            //we done.
                            break;
                        }
                    }

                    stream.Write32(i);
                    int size = copyCounter - i;
                    stream.Write32(size);
                    //Array time
                    foreach (ushort a in newframe.ToList().GetRange(i, size).ToArray())
                    {
                        stream.Write16(a);
                    }
                    i += copyCounter;
                }


            }


            return stream.Data;
        }

        public static byte[] LzCompress2(byte[] frame)
        {
            IOStream src = new IOStream(frame);
            IOStream output = VideoCompression.CompLZ77(src, frame.Length);

            byte[] returnvalue = new byte[8 + output.Length];
            BitConverter.GetBytes(CompressionHeaders.LZCOMPRESSEDHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(frame.Length).CopyTo(returnvalue, 4);
            output.CopyToArray(0, returnvalue, 8, (int)output.Length);

            return returnvalue;
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
            for (int i = 0; i < newArr.Length; i++)
            {
                byte s = src[i];
                byte n = newArr[i];
                if (s != n)
                {
                    //WE HAVE A CHANGE.
                    // io.Write32(i);
                    offsetTable.Add(i);
                    IOStream writeStr = new IOStream();
                    while (s != n && i + 1 < newArr.Length)
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

                io.CopyFromArray(offstream.Data, (int)offstream.Length);
                io.CopyFromArray(datastream.Data, (int)datastream.Length);
            }

            return io.Data;
        }

        //public static byte[] FrameCompare(byte[] buffer)
        //{
        //    byte[] returnValue = new byte[1];
        //    if (buffer.Length < 200)
        //    {
        //        return buffer;
        //    }
        //    if (VideoCompression.oldFrame == null)
        //    {
        //        return buffer;
        //    }
        //    //Split the arrays up.

        //    IOStream src = new IOStream(4);
        //    src.Seek(0);
        //    src.WriteU32(CompressionHeaders.NINTYRLHEADERINTR);

        //    for (int i =0;i< newerBuffers.Count;i++)
        //    {   
        //        newBuffers[i] = GetDifferences(oldBuffers[i], newerBuffers[i]);
        //        var lz = VideoCompression.CompLZ77(newBuffers[i]).Data;
        //        var rl = VideoCompression.RLCompWrite(newBuffers[i]);
        //        int compressionType = 0;
        //        int bestSize = newBuffers[i].Length;
        //        if (bestSize > lz.Length)
        //        {
        //            bestSize = lz.Length;
        //            newBuffers[i] = lz;
        //            compressionType = 1;
        //        }

        //        if (bestSize > rl.Length)
        //        {
        //            newBuffers[i] = rl;
        //            compressionType = 2;
        //        }

        //        src.Write8((byte)compressionType);         
        //    }



        //    foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
        //    foreach (var s in newBuffers) src.CopyFromArray(s, s.Length);

        //    returnValue = src.Data;

        //    return returnValue;
        //}


        private static List<byte> bigBuffer = new List<byte>();

        public static void CompressFile3(ref byte[] buffer, string fn, string output, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            ROM.MakeSource(fn, Compress2(buffer), output);

            VideoCompression.oldFrame = buffer;



        }

        public static void CompressFile2(ref byte[] buffer, string fn, string output, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            ROM.MakeSource(fn, Compress(buffer), output);

            VideoCompression.oldFrame = buffer;



        }


        public static void CompressFile(byte[] buffer, string fn, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            File.WriteAllBytes(fn, Compress(buffer));
            VideoCompression.oldFrame = buffer;
        }
        public static int GetComparePercent(int[] newBuf, int[] oldBuf)
        {
            int similar = 0;


            for (int i = 0; i < newBuf.Length; i++)
            {
                if (newBuf[i] != oldBuf[i])
                {
                    similar++;
                }
            }

            float calc = similar / oldBuf.Length;
            float calc2 = calc * 100;
            int ret = (int)(calc2);
            if (ret != 0)
            {
                return ret;
            }
            return ret;
        }

        //num bytes, 
        public static byte[] DiffCompress(byte[] newFrame, byte[] oldFrame)
        {
            List<int> frameData = new List<int>();
            frameData.Add(0xFFFFFFF);//we use this later.
            if (newFrame.Length != oldFrame.Length) throw new Exception("I can't do this Starfox");

            //Get frames as 4 bytes a piece.
            int[] betterNewFrame = new int[newFrame.Length / 4];
            int[] betterOldFrame = new int[newFrame.Length / 4];

            //Copy byte buffer to abvove buffer
            Array.Copy(newFrame, betterNewFrame, newFrame.Length / 4);
            Array.Copy(oldFrame, betterOldFrame, newFrame.Length / 4);


            int count = 0;
            for (; count < betterOldFrame.Length; count++)
            {
                int old = betterOldFrame[count];
                int newb = betterNewFrame[count];

                if (old == newb) continue;

                //A color will be 4 bytes, 
                int start = 50;

                if (count + start > betterOldFrame.Length)
                {
                    start = betterOldFrame.Length - count;
                }
                //we want a 80 percent difference. 
                int[] rng1 = betterNewFrame.ToList().GetRange(count, start).ToArray(); //Copy X many bytes
                int[] rng2 = betterOldFrame.ToList().GetRange(count, start).ToArray();

                //if (GetComparePercent(rng1, rng2) > 25)
                //{
                int buffersize = start;

                //We have a start
                int off = count;
                int size = buffersize;


                frameData.Add(off);
                frameData.Add(size * 4);
                frameData.AddRange(betterNewFrame.ToList().GetRange(count, buffersize));
                count += buffersize;
                continue;
                // }
            }

            if (frameData.Count == 0)
            {//If we got here, it's a special packet. Because somehow the whole frame was the same. 

                frameData.Add(0);
                int valu = -1;
                frameData.Add(valu);
                // frameData.Add(frameData.Count*4);
            }

            frameData[0] = count;//Decoder needs to +1 this value.
            int len2 = sizeof(int);
            byte[] validData = new byte[frameData.Count * 4];

            IntPtr toByte = Marshal.AllocHGlobal(frameData.Count * 4);
            int[] arr = frameData.ToArray();
            IntPtr srcz = Marshal.AllocHGlobal(frameData.Count * 4);

            //Copy frame data to src.
            Marshal.Copy(frameData.ToArray(), 0, srcz, frameData.Count);


            for (int i = 0; i < frameData.Count * 4; i++)
            {
                byte val = Marshal.ReadByte(srcz + i);
                Marshal.WriteByte(toByte + i, val);
            }

            Marshal.Copy(toByte, validData, 0, frameData.Count * 4);


            Marshal.FreeHGlobal(toByte);
            Marshal.FreeHGlobal(srcz);



            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(CompressionHeaders.PATCHHEADER);

            src.Seek(8);


            src.Write(validData, 8, validData.Length);
            //write size to file
            src.Seek(4);
            src.WriteU32((UInt32)(src.Length - 8));


            return src.Data;
        }

        public static byte[] Compress(byte[] buffer, int enableCircleComp = 3)
        {

            byte[] bestData = null; ///RawFrame(buffer);

            //See who has best data. 
            int bestSize = int.MaxValue;// bestData.Length;//raw 

            //   byte[] lz = LzCompress(buffer);
            //   // byte[] diff = FrameCompare(buffer);
            //   ////  byte[] difflz = FrameCompareCompQuad(buffer);


            //   //byte[] nint2 = FrameCompareCompQuadNinty2(buffer);
            //   //byte[] nint3 = FrameCompareCompQuadNinty3(buffer);
            //   //byte[] nint4 = FrameCompareCompQuadNinty4(buffer);

            //   //byte[] RlEd = VideoCompression.GBatroidRLE(buffer);


            //   //byte[] nint5 = LzCompress(nint4);

            //   //byte[] RlEd2 = LzCompress(RlEd);
            byte[] diff = null;
            Thread diffThread = null;
            byte[] cmp = null;
       
                //dif should work nearly all the time.
                Thread t = new Thread(()=>{
                    if (VideoCompression.oldFrame != null)
                    {
                        diff = DiffFrame2(VideoCompression.oldFrame, buffer);
                    }
                });
                    Thread t2 = new Thread(()=>{
                        cmp = LzCompress(buffer);
                    });
                t.Start();
                t2.Start();
                Console.WriteLine("Waiting for threads to complete");
                while (t2.IsAlive || t2.ThreadState == ThreadState.Running  || t.IsAlive && t.ThreadState == ThreadState.Running)
                {
                    
                    Thread.Sleep(20);
                }
                Console.WriteLine("Threads completed.");
            
            //diffThread2?.Start();
            ////   diffThread?.Start();
            //lzThread.Start();

            ////   diffThread?.Join();

            //Thread.Sleep(100);
            //while ((lzThread != null && lzThread.IsAlive || lzThread.ThreadState == ThreadState.Running) || (diffThread != null && diffThread.IsAlive && diffThread.ThreadState == ThreadState.Running))
            //{
            //    Thread.Sleep(20);
            //}

            //   if (diff != null && diff.Length != 0 && diff.Length < bestSize)
            //   {
            //       bestSize = diff.Length;
            //       bestData = diff;
            //   }


            if (diff != null && diff.Length != 0 && diff.Length < bestSize)
            {
                bestSize = diff.Length;
                bestData = diff;
            }


            if (cmp != null && cmp.Length < bestSize)
            {
                bestSize = cmp.Length;
                bestData = cmp;
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
        public static byte[] Compress2(byte[] buffer, int enableCircleComp = 3)
        {

            byte[] bestData = RawFrame(buffer);

            //See who has best data. 
            int bestSize = bestData.Length;//raw 

            //   byte[] lz = LzCompress(buffer);
            //   // byte[] diff = FrameCompare(buffer);
            //   ////  byte[] difflz = FrameCompareCompQuad(buffer);


            //   //byte[] nint2 = FrameCompareCompQuadNinty2(buffer);
            //   //byte[] nint3 = FrameCompareCompQuadNinty3(buffer);
            //   //byte[] nint4 = FrameCompareCompQuadNinty4(buffer);

            //   //byte[] RlEd = VideoCompression.GBatroidRLE(buffer);


            //   //byte[] nint5 = LzCompress(nint4);

            //   //byte[] RlEd2 = LzCompress(RlEd);
            //   byte[] diff = null;
            //   Thread diffThread = null;
            //   if (VideoCompression.oldFrame != null)
            //   {

            //       diffThread = new Thread(
            //     () =>
            //     {
            //         diff = DiffCompress(buffer, VideoCompression.oldFrame);

            //     });
            //   }

            byte[] diff2 = null;
            Thread diffThread2 = null;
            if (VideoCompression.oldFrame != null)
            {

                diffThread2 = new Thread(
              () =>
              {
                  diff2 = DiffFrame(VideoCompression.oldFrame, buffer);

              });
            }




            byte[] cmp = null;
            Thread lzThread = new Thread(
                () =>
                {
                    cmp = LzCompress(buffer);

                });
            diffThread2?.Start();
            //   diffThread?.Start();
            lzThread.Start();

            lzThread.Join();
            //   diffThread?.Join();
            diffThread2?.Join();
            Thread.Sleep(100);
            while ((lzThread != null && lzThread.IsAlive || lzThread.ThreadState == ThreadState.Running)) //|| (diffThread != null && diffThread.IsAlive && diffThread.ThreadState == ThreadState.Running))
            {
                Thread.Sleep(20);
            }
            //   if (diff != null && diff.Length != 0 && diff.Length < bestSize)
            //   {
            //       bestSize = diff.Length;
            //       bestData = diff;
            //   }
            if (cmp != null && cmp.Length < bestSize)
            {
                bestSize = cmp.Length;
                bestData = cmp;
            }
            if (diff2 != null && diff2.Length != 0 && diff2.Length < bestSize)
            {
                bestSize = diff2.Length;
                bestData = diff2;
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
