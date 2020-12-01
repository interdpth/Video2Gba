using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Video2Gba.LibIpsNet;

namespace Video2Gba
{
    public class VideoCompression
    {

        private const uint RAWHEADER = 0x88FFFF00;
        private const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
        private const uint INTERLACERLEHEADER = 0x88FFFF22;
        private const uint INTERLACERLEHEADER2 = 0x88FFFF23;
        private const uint DESCRIBEHEADER = 0x88FFFF03;
        private const uint LUTHEADER = 0x88FFFF02;
        private const uint DIFFHEADER = 0x88FFFF12;
        private const uint QUADDIFFHEADER = 0x88FFFF13;
        private const uint QUADDIFFHEADER2 = 0x88FFFF14;
        private const uint RLEHEADER = 0x88FFFF15;

        private static byte[] RawFrame(byte[] frame)
        {
            byte[] buffer = new byte[frame.Length + 8];

            BitConverter.GetBytes(0x88FFFF00).CopyTo(buffer, 0);
            BitConverter.GetBytes(frame.Length).CopyTo(buffer, 4);
            frame.CopyTo(buffer, 8);

            return buffer;
        }

        private static byte[] LzCompress(byte[] frame)
        {
            IOStream src = new IOStream(frame);
            IOStream output = CompLZ77(src, frame.Length);

            byte[] returnvalue = new byte[8 + output.Length];
            BitConverter.GetBytes(LZCOMPRESSEDHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(frame.Length).CopyTo(returnvalue, 4);
            output.CopyToArray(0, returnvalue, 8, (int)output.Length);

            return returnvalue;
        }

        private static byte[] HuffmanCompress(byte[] frame)
        {
            return new byte[1];
        }

        //byte count is how much u8, u16, 3, u32
 
        private static byte[] DescribeCompress(byte[] old, byte[] newFrame)
        {
            //
            List<describe> compressedBuf = new List<describe>();
            IOStream newStream = new IOStream(newFrame.Length / 4);
            newStream.Seek(4);


            ////Split into 4.
            ////If not size is not aligned 
            //int len = buffer.Length / 4;

            //for (int i = 0; i < buffer.Length; i++)
            //{
            //    int realSize = i + len < buffer.Length ? len : buffer.Length - i;
            //    byte[] newBuffer = new byte[realSize];
            //    Array.Copy(buffer, i, newBuffer, 0, realSize);
            //    buffers.Add(Compress(newBuffer, enableCircleComp - 1));
            //    i += realSize;
            //}



            //for (ushort i = 0; i < newFrame.Length / 4; i++)
            //{
            //    if (old[i] != newFrame[i])
            //    {
            //        compressedBuf.Add(new describe(i, newFrame[i]));
            //        newStream.Write16(i);
            //        newStream.Write8(newFrame[i]);
            //    }
            //}



            newStream.Seek(0);
            newStream.Write32(compressedBuf.Count - 4);
            byte[] returnvalue = new byte[4 + newStream.Length];
            BitConverter.GetBytes(DESCRIBEHEADER).CopyTo(returnvalue, 0);
            newStream.CopyToArray(0, returnvalue, 8, returnvalue.Length);

            return returnvalue;

        }
        public static byte[] CompRLE(byte[] input)
        {
            IOStream output = new IOStream();
            int position;
            position = (int)output.Position;
            byte[] data;
            int length = input.Length;
            data = input;
            for (int i = 0; i < 2; i++)
            {
                List<byte> list;
                list = new List<byte>();
                List<int> list2;
                list2 = new List<int>();
                byte b;
                b = data[i];
                list.Add(b);
                int num;
                num = 1;
                for (int j = i + 2; j < length; j += 2)
                {
                    byte b2;
                    b2 = data[j];
                    if (b2 == b)
                    {
                        num++;
                        continue;
                    }
                    list.Add(b2);
                    list2.Add(num);
                    b = b2;
                    num = 1;
                }
                list2.Add(num);
                byte[,] array;
                array = new byte[2, length];
                int[] array2;
                array2 = new int[2];
                int[] array3;
                array3 = array2;
                for (int k = 0; k < 2; k++)
                {
                    int num2;
                    num2 = 3 + k;
                    int num3;
                    num3 = 128 << 8 * k;
                    int num4;
                    num4 = num3 - 1;
                    List<byte> list3;
                    list3 = new List<byte>();
                    array[k, array3[k]++] = (byte)(k + 1);
                    for (int l = 0; l < list.Count; l++)
                    {
                        num = list2[l];
                        if (num >= num2)
                        {
                            if (list3.Count > 0)
                            {
                                if (k == 0)
                                {
                                    array[k, array3[k]++] = (byte)list3.Count;
                                }
                                else
                                {
                                    array[k, array3[k]++] = (byte)(list3.Count >> 8);
                                    array[k, array3[k]++] = (byte)list3.Count;
                                }
                                foreach (byte item in list3)
                                {
                                    array[k, array3[k]++] = item;
                                }
                                list3.Clear();
                            }
                            while (num > 0)
                            {
                                int num5;
                                num5 = num3 + Math.Min(num, num4);
                                if (k == 0)
                                {
                                    array[k, array3[k]++] = (byte)num5;
                                }
                                else
                                {
                                    array[k, array3[k]++] = (byte)(num5 >> 8);
                                    array[k, array3[k]++] = (byte)num5;
                                }
                                array[k, array3[k]++] = list[l];
                                num -= num4;
                            }
                            continue;
                        }
                        if (list3.Count + num > num4)
                        {
                            if (k == 0)
                            {
                                array[k, array3[k]++] = (byte)list3.Count;
                            }
                            else
                            {
                                array[k, array3[k]++] = (byte)(list3.Count >> 8);
                                array[k, array3[k]++] = (byte)list3.Count;
                            }
                            foreach (byte item2 in list3)
                            {
                                array[k, array3[k]++] = item2;
                            }
                            list3.Clear();
                        }
                        for (int m = 0; m < num; m++)
                        {
                            list3.Add(list[l]);
                        }
                    }
                    if (list3.Count > 0)
                    {
                        if (k == 0)
                        {
                            array[k, array3[k]++] = (byte)list3.Count;
                        }
                        else
                        {
                            array[k, array3[k]++] = (byte)(list3.Count >> 8);
                            array[k, array3[k]++] = (byte)list3.Count;
                        }
                        foreach (byte item3 in list3)
                        {
                            array[k, array3[k]++] = item3;
                        }
                        list3.Clear();
                    }
                    array[k, array3[k]++] = 0;
                    if (k == 1)
                    {
                        array[k, array3[k]++] = 0;
                    }
                }
                int num6;
                num6 = ((array3[0] > array3[1]) ? 1 : 0);
                int num7;
                num7 = array3[num6];
                for (int n = 0; n < num7; n++)
                {
                    output.Write8(array[num6, n]);
                }
            }



            byte[] returnvalue = new byte[8 + output.Length];
            BitConverter.GetBytes(RLEHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(output.Length).CopyTo(returnvalue, 4);
            output.CopyToArray(0, returnvalue, 8, (int)output.Length);

            return output.Data;
        }

        private static byte[] oldFrame = null;
        public static byte[] Compress(ref byte[] buffer, int enableCircleComp = 3)
        {

            byte[] bestData = RawFrame(buffer);

            //See who has best data. 
            int bestSize = buffer.Length;//raw 

            byte[] lz = LzCompress(buffer);
            byte[] diff = FrameCompareComp(buffer);
            byte[] difflz = FrameCompareCompQuad(buffer);
            //byte[] difflz2 = FrameCompareCompQuad2(buffer);
            //byte[] interleave = FrameCompareCompQuadInterleaved(buffer);
            //byte[] interleave2 = FrameCompareCompQuadInterleaved2(buffer);
            //byte[] RlEd = CompRLE(buffer);
        

            if (lz.Length < bestSize)
            {
                bestSize = lz.Length;
                bestData = lz;
            }

            if (diff.Length < bestSize)
            {
                bestSize = diff.Length;
                bestData = diff;
            }


            if (difflz.Length < bestSize)
            {
                bestSize = difflz.Length;
                bestData = difflz;
            }

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

        private static byte[] LutComp(byte[] buffer, int enableCircleComp = 3)
        {

            //We index all possible values 
            List<UInt32> valueTable = new List<uint>();
            List<UInt32> tmpBuff = new List<UInt32>();

            Semaphore listLock = new Semaphore(1, 1);


            int padding = 0;


            while ((buffer.Length + padding) % 4 != 0) { padding++; }

            int len = (buffer.Length + padding) / 4;//len is for size in u32
            int blockLen = len / 4;//realsize
            tmpBuff = new List<UInt32>(blockLen);


            List<List<UInt32>> buffers = new List<List<uint>> { new List<uint>(len), new List<uint>(len), new List<uint>(len), new List<uint>(len) };

            for (int i = 0; i < 4; i++)
            {
                Buffer.BlockCopy(buffer, i * blockLen, buffers[i].ToArray(), 0, blockLen);
            }


            List<List<ushort>> replaceBuffers = new List<List<ushort>>(4);

            List<Thread> compressionThreads = new List<Thread>();
            for (int i = 0; i < 4; i++)
            {
                for (int x = 0; x < buffers[i].Count; i++)
                {
                    uint tvalue = buffers[i][x];
                    //Is value in the dict? 
                    if (!valueTable.Contains(tvalue))
                    {
                        valueTable.Add(tvalue);
                    }

                    ushort newIndex = (ushort)valueTable.FindIndex(z => z == tvalue);
                    replaceBuffers[i].Add(newIndex);
                }
            }



            List<List<byte>> compressedBuffers = new List<List<byte>>(4);

            for (int i = 0; i < 4; i++)
            {
                Thread t = new Thread(() =>
                {
                    int index = i;

                    byte[] tmpBytes = new byte[replaceBuffers[index].Count * 2];
                    Buffer.BlockCopy(replaceBuffers[index].ToArray(), 0, tmpBytes, 0, tmpBytes.Length);

                    byte[] CompBuff = Compress(ref tmpBytes, enableCircleComp--);
                    tmpBytes = null;
                    listLock.WaitOne();
                    compressedBuffers[index] = CompBuff.ToList();
                    listLock.Release();
                });
                compressionThreads.Add(t);
                t.Start();
            }

            while (compressionThreads.Any(x => x.IsAlive || x.ThreadState == ThreadState.Running)) ;
            IOStream newStream = new IOStream(100);

            List<int> sizes = new List<int> { compressedBuffers[0].Count, compressedBuffers[1].Count, compressedBuffers[2].Count, compressedBuffers[3].Count };
            newStream.Seek(0);


            newStream.Write32(padding);//Padding
            for (int i = 0; i < 4; i++) newStream.Write32(sizes[i]);
            newStream.Write32(valueTable.Count * 4);
            newStream.CopyFromArray(valueTable.ToArray(), valueTable.Count * 4);//Write the look up
            for (int i = 0; i < 4; i++) newStream.CopyFromArray(compressedBuffers[i].ToArray(), compressedBuffers[i].Count);




            byte[] returnvalue = new byte[8 + newStream.Length];
            BitConverter.GetBytes(LUTHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(8 + newStream.Length).CopyTo(returnvalue, 4);

            newStream.CopyToArray(0, returnvalue, 8, (int)newStream.Length);
            //We want to trim padding before actual compression. 
            //Convert to Int32 

            return returnvalue;
        }

        public static IOStream CompLZ77(IOStream input, int length)
        {
            IOStream output = new IOStream(4);
            byte[] data;
            data = input.Data;
            Dictionary<int, List<int>> bufferWindow;
            bufferWindow = new Dictionary<int, List<int>>();
            for (int i = 0; i < input.Length - 2; i++)
            {
                int key;
                key = (data[i] | (data[i + 1] << 8) | (data[i + 2] << 16));
                List<int> value;
                if (bufferWindow.TryGetValue(key, out value))
                {
                    value.Add(i);
                }
                else
                {
                    bufferWindow.Add(key, new List<int>
                    {
                        i
                    });
                }
            }
            int num;
            num = 18;
            int num2;
            num2 = 4096;
            int num3;
            num3 = 0;
            int position;
            position = (int)output.Position;
            output.Write8(16);
            output.Write8((byte)length);
            output.Write8((byte)(length >> 8));
            output.Write8((byte)(length >> 16));
            while (num3 < length)
            {
                int position2;
                position2 = (int)output.Position;
                output.Write8(0);
                for (int num4 = 0; num4 < 8; num4++)
                {
                    if (num3 + 3 <= length)
                    {
                        int key2;
                        key2 = (data[num3] | (data[num3 + 1] << 8) | (data[num3 + 2] << 16));
                        List<int> value2;
                        if (bufferWindow.TryGetValue(key2, out value2))
                        {
                            int j;
                            j = 0;
                            while (value2[j] < num3 - num2)
                            {
                                j++;
                                if (j != value2.Count)
                                {
                                    continue;
                                }
                                goto IL_01cf;
                            }
                            int num5;
                            num5 = -1;
                            int num6;
                            num6 = -1;
                            for (; j < value2.Count; j++)
                            {
                                int num7;
                                num7 = value2[j];
                                if (num7 >= num3 - 1)
                                {
                                    break;
                                }
                                int k;
                                for (k = 3; num3 + k < length && data[num7 + k] == data[num3 + k] && k < num; k++)
                                {
                                }
                                if (k > num5)
                                {
                                    num5 = k;
                                    num6 = num7;
                                }
                            }
                            if (num6 != -1)
                            {
                                int num8;
                                num8 = num3 - num6 - 1;
                                output.Write8((byte)((num5 - 3 << 4) | (num8 >> 8)));
                                output.Write8((byte)num8);
                                output.Data[position2] |= (byte)(128 >> num4);
                                num3 += num5;
                                goto IL_01de;
                            }
                        }
                    }
                    goto IL_01cf;
                IL_01cf:
                    output.Write8(data[num3++]);
                    goto IL_01de;
                IL_01de:
                    if (num3 >= length)
                    {
                        break;
                    }
                }
            }
            return output;
        }

        //IPS format used.
        enum types { COMPRESSED, DECOMPRESSED };
        public static byte[] GetDifferences(byte[] oldbuffer, byte[] newbuffer)
        {


            for (int i = 0; i < newbuffer.Length;)
            {
                //what do we have heare?
                if (oldbuffer[i] == newbuffer[i])
                {

                }
                i++;
            }

            return new byte[] { 0 };

        }
        //Break frame into 4 sections
        //Run IPS on the frames//ips needs to use gba rl 
        //Run lz after rejoining.

        public static byte[] FrameCompareComp_old(byte[] buffer)
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

            List<byte[]> newBuffers = new List<byte[]>();

            int len = buffer.Length / 4;

            for (int i = 0; i < buffer.Length;)
            {

                byte[] newBuffer = new byte[len];
                Array.Copy(buffer, i, newBuffer, 0, len);
                newBuffers.Add(newBuffer);
                i += len;
            }


            List<byte[]> old = new List<byte[]>();

            len = oldFrame.Length / 4;

            for (int i = 0; i < oldFrame.Length;)
            {

                byte[] newBuffer = new byte[len];
                Array.Copy(oldFrame, i, newBuffer, 0, len);
                old.Add(newBuffer);
                i += len;
            }

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
            src.WriteU32(DIFFHEADER);
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
        private static List<byte[]> Buffer2Quad(byte[] buff, int width, int height)
        {
            if (buff.Length % 2 != 0) throw new Exception("Buffer is odd");
            ushort[] convertBuf = new ushort[buff.Length];
            Buffer.BlockCopy(buff, 0, convertBuf, 0, buff.Length);

            //basically expandable memorystreams.
            List<IOStream> quad = new List<IOStream>() { new IOStream(), new IOStream(), new IOStream(), new IOStream() };
            //we are a byte so width and height is times 2
            int w = width;
            int h = height;
            //240x160
            //Handle first screen.
            int y = 0;
            //Top left   &&    //Top right
            for (; y < height/2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Check if capturing right
                    if (x < width/2)
                    {
                        quad[0].Write16(convertBuf[y * width + x]);
                             
                    }
                    else
                    {
                        quad[1].Write16(convertBuf[y * width + x]);
                    }
                }
            }


            //Bottom left   &&   //Bottom right
            for (; y <  height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Check if capturing right
                    if (x < width / 2)
                    {
                        quad[2].Write16(convertBuf[y * width + x]);

                    }
                    else
                    {
                        quad[3].Write16(convertBuf[y * width + x]);
                    }
                }
            }


            return new List<byte[]> { quad[0].Data, quad[1].Data, quad[2].Data, quad[3].Data, };
        }

        private static List<byte[]> Buffer2Interleave(byte[] buff, int bytewidth)
        {
            if (buff.Length % 2 != 0) throw new Exception("Buffer is odd");
            ushort[] convertBuf = new ushort[buff.Length];
            Buffer.BlockCopy(buff, 0, convertBuf, 0, buff.Length);

            //basically expandable memorystreams.
            List<IOStream> quad = new List<IOStream>();


            for (int i = 0; i < bytewidth; i++)
            {
                quad.Add(new IOStream());
            }

            //we are a byte so width and height is times 2
            int quadCount = 0;
            for(int i =0;i<buff.Length;)
            {
                for (int c = 0; c < bytewidth; c++)
                {
                    quad[c].Write8(buff[i++]);                   
                }
            }



            List<byte[]> returnVal = new List<byte[]>();
            foreach (var q in quad) returnVal.Add(q.Data);



            return returnVal;
        }



        public static byte[] FrameCompareCompQuad(byte[] buffer)
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

            List<byte[]> newBuffers = Buffer2Quad(buffer, 240, 160);



            List<byte[]> old = Buffer2Quad(VideoCompression.oldFrame, 240, 160);


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
            src.WriteU32(QUADDIFFHEADER);
            src.WriteU32((uint)quadlen);


            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);

            foreach (var s in newBuffers) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }

        public static byte[] FrameCompareCompQuad2(byte[] buffer)
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

            List<byte[]> newBuffers = Buffer2Quad(buffer, 240, 160);



            List<byte[]> old = Buffer2Quad(VideoCompression.oldFrame, 240, 160);


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
            src.WriteU32(QUADDIFFHEADER2);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }
        public static byte[] FrameCompareCompQuadInterleaved(byte[] buffer)
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

            List<byte[]> newBuffers = Buffer2Interleave(buffer, 2);
            List<byte[]> old = Buffer2Interleave(VideoCompression.oldFrame, 2);

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
            src.WriteU32(INTERLACERLEHEADER);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }

        public static byte[] FrameCompareCompQuadInterleaved2(byte[] buffer)
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

            List<byte[]> newBuffers = Buffer2Interleave(buffer, 2);
            List<byte[]> old = Buffer2Interleave(VideoCompression.oldFrame, 2);

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
                        compressedDifferences.Add(CompRLE(str));
                        quadlen += (int)str.Length;
                    }
                }
            }


            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(INTERLACERLEHEADER2);
            src.WriteU32((uint)quadlen);
            foreach (var s in compressedDifferences) src.WriteU32((uint)s.Length);
            foreach (var s in compressedDifferences) src.CopyFromArray(s, s.Length);

            returnValue = src.Data;

            return returnValue;
        }

        public static byte[] FrameCompareComp(byte[] buffer)
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

            IOStream src = new IOStream(4);
            src.Seek(0);
            src.WriteU32(DIFFHEADER);
            Creator c = new Creator();
            using (var olds = new MemoryStream(VideoCompression.oldFrame))
            {
                using (var newf = new MemoryStream(buffer))
                {
                    var str = c.Create(olds, newf);
                    src.WriteU32((uint)str.Length);
                    src.CopyFromArray(str, str.Length);
                }
            }
            returnValue = src.Data;

            return returnValue;
        }

        public static byte[] Create3(byte[] source, byte[] target)
        {
            long sourcelen = source.Length;
            long targetlen = target.Length;

            bool sixteenmegabytes = false;

            if (sourcelen > 16777216)
            {
                sourcelen = 16777216;
                sixteenmegabytes = true;
            }

            if (targetlen > 16777216)
            {
                targetlen = 16777216;
                sixteenmegabytes = true;
            }



            IOStream rawBinData = new IOStream(5);
            Dictionary<UInt32, List<byte>> dics = new Dictionary<UInt32, List<byte>>();//Containing the differences at end.
            for (uint srcCount = 0; srcCount < source.Length; srcCount++)
            {
                UInt32 curOffset = 0;
                List<byte> differences = new List<byte>();
                if (source[srcCount] != target[srcCount])
                {
                    curOffset = srcCount;
                    while (srcCount != source.Length && source[srcCount] != target[srcCount])
                    {
                        differences.Add(target[srcCount++]);
                    }

                    dics.Add(curOffset, differences);
                }
            }


            //We store offsets then data because it'll make the LZ compression better.
            foreach (var entry in dics.Keys)
            {
                rawBinData.Write32((int)entry);
            }

            foreach (var entry in dics.Values)
            {
                foreach (var d in entry)
                {
                    rawBinData.Write16(d);
                }
            }


            return rawBinData.Data;
        }


        public static byte[] CircleComp(byte[] buffer, int enableCircleComp)
        {
            if (buffer.Length < 200 || enableCircleComp == 0)
            {
                return buffer;
            }

            //Split the arrays up.

            List<byte[]> buffers = new List<byte[]>();

            int len = buffer.Length / 4;

            for (int i = 0; i < buffer.Length; i++)
            {
                int realSize = i + len < buffer.Length ? len : buffer.Length - i;
                byte[] newBuffer = new byte[realSize];
                Array.Copy(buffer, i, newBuffer, 0, realSize);
                buffers.Add(Compress(ref newBuffer, enableCircleComp - 1));
                i += realSize;
            }
            List<byte> fulbuf = new List<byte>();
            foreach (var b in buffers)
            {
                fulbuf.AddRange(b.ToList());
            }
            //no
            return fulbuf.ToArray(); ;
            //on arrays after 4, use lz compression other wise we 


            // 01 32 EF 56
            // FF 00 BE
            // 02 AA CD 
            // 00 CD 00

            //Illegal size is > 243







        }
    }
}
