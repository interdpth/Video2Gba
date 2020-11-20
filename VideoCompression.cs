﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class VideoCompression
    {

        private const uint RAWHEADER = 0x88FFFF00;
        private const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
        private const uint HUFFMANCOMPRESSEDHEADER = 0x88FFFF02;
        private const uint DESCRIBEHEADER = 0x88FFFF03;
        private const uint LUTHEADER = 0x88FFFF03;
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
            output.CopyToArray(0, returnvalue, 8, output.Length);

            return returnvalue;
        }

        private static byte[] HuffmanCompress(byte[] frame)
        {
            return new byte[1];
        }

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

        private static byte[] oldFrame;
        public static byte[] Compress(byte[] buffer, int enableCircleComp = 3)
        {

            byte[] bestData = RawFrame(buffer);

            //See who has best data. 
            int bestSize = buffer.Length;//raw 

            byte[] lz = LzCompress(buffer);
            byte[] huff = new byte[1];
            //byte[] dic = LutComp(buffer, enableCircleComp);
            //using (MemoryStream m = new MemoryStream(buffer))
            //{
            //    byte[] tmp = new byte[20000000];

            //    using (MemoryStream m2 = new MemoryStream(tmp))
            //    {
            //        Huffman4 h = new Huffman4();

            //     int size=   h.Compress(m, buffer.Length, m2);
            //        huff = new byte[size];
            //        Array.Copy(tmp, huff,  size);
            //} }

            //using (MemoryStream m = new MemoryStream(buffer))
            //{
            //    byte[] tmp = new byte[20000000];

            //    using (MemoryStream m2 = new MemoryStream(tmp))
            //    {
            //        RLE h = new RLE();

            //        int size = h.Compress(m, buffer.Length, m2);
            //        huff = new byte[size];
            //        Array.Copy(tmp, huff, size);
            //    }
            //}

            //new Huffman().Compress("tests/huff/dec/00.ffuh.dat", "tests/huff/cmp/00.huff4");
            //   byte[] circle = CircleComp(buffer, enableCircleComp);
            byte[] describe = null;


            if (lz.Length < bestSize)
            {
                bestSize = lz.Length;
                bestData = lz;
            }


            //if (huff.Length < bestSize)
            //{
            //    bestSize = huff.Length;
            //    bestData = huff;
            //}

            //if (circle.Length < bestSize)
            //{
            //    bestSize = circle.Length;
            //    bestData = huff;
            //}

            //if (huff.Length > bestSize)
            //{
            //    bestSize = huff.Length;
            //    bestData = huff;
            //}
            //if (oldFrame != null && oldFrame.Length > 0)
            //{
            //    DescribeCompress(oldFrame, buffer);
            //    if (describe.Length > bestSize)
            //    {
            //        bestSize = describe.Length;
            //        bestData = describe;
            //    }
            //}

            byte[] file = new byte[bestSize + 4];


            BitConverter.GetBytes(bestSize).CopyTo(file, 0);


            bestData.CopyTo(file, 4);

            return file;


        }

        public static void CompressFile2(byte[] buffer, string fn, string output, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            ROM.MakeSource(fn, Compress((byte[])buffer), output);
        }


        public static void CompressFile(byte[] buffer, string fn, int enableCircleComp = 3)
        {
            Console.WriteLine("Compressing to " + fn);

            File.WriteAllBytes(fn, Compress(buffer));
        }

        private static byte[] LutComp(byte[] buffer, int enableCircleComp= 3)
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
                for(int x = 0; x<buffers[i].Count;i++)
                {
                    uint tvalue = buffers[i][x];
                     //Is value in the dict? 
                     if (!valueTable.Contains(tvalue))
                     {
                        valueTable.Add(tvalue);
                     }

                    ushort newIndex = (ushort)valueTable.FindIndex(z=>z==tvalue);
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

                    byte[] CompBuff = Compress(tmpBytes, enableCircleComp--);

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

            newStream.CopyToArray(0, returnvalue, 8, newStream.Length);
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
            position = output.Position;
            output.Write8(16);
            output.Write8((byte)length);
            output.Write8((byte)(length >> 8));
            output.Write8((byte)(length >> 16));
            while (num3 < length)
            {
                int position2;
                position2 = output.Position;
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
                buffers.Add(Compress(newBuffer, enableCircleComp - 1));
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