using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
   public class VideoCompression
    {

        private const uint RAWHEADER = 0x88FFFF00;
        private const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
        private const uint HUFFMANCOMPRESSEDHEADER = 0x88FFFF02;
        private const uint DESCRIBEHEADER = 0x88FFFF03;
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



            for (ushort i = 0; i < newFrame.Length / 4; i++)
            {
                if (old[i] != newFrame[i])
                {
                    compressedBuf.Add(new describe(i, newFrame[i]));
                    newStream.Write16(i);
                    newStream.Write8(newFrame[i]);
                }




            }

            newStream.Seek(0);
            newStream.Write32(compressedBuf.Count - 4);
            byte[] returnvalue = new byte[4 + newStream.Length];
            BitConverter.GetBytes(DESCRIBEHEADER).CopyTo(returnvalue, 0);
            newStream.CopyToArray(0, returnvalue, 8, returnvalue.Length);

            return returnvalue;

        }

        private static byte[] oldFrame;

        public static void Compress(byte[] buffer, string fn)
        {
            Console.WriteLine("Compressing to " + fn);
            byte[] bestData = RawFrame(buffer);

            //See who has best data. 
            int bestSize = buffer.Length;//raw 

            byte[] lz = LzCompress(buffer);
            //    byte[] huff = HuffmanCompress(buffer);


            byte[] describe = null;


            if (lz.Length < bestSize)
            {
                bestSize = lz.Length;
                bestData = lz;
            }

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

            File.WriteAllBytes(fn, file);


            //We need to keep the last frame in memory.
            oldFrame = buffer;
        }

        public static IOStream CompLZ77(IOStream input, int length)
        {
            IOStream output = new IOStream(4);
            byte[] data;
            data = input.Data;
            Dictionary<int, List<int>> dictionary;
            dictionary = new Dictionary<int, List<int>>();
            for (int i = 0; i < input.Length - 2; i++)
            {
                int key;
                key = (data[i] | (data[i + 1] << 8) | (data[i + 2] << 16));
                List<int> value;
                if (dictionary.TryGetValue(key, out value))
                {
                    value.Add(i);
                }
                else
                {
                    dictionary.Add(key, new List<int>
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
                        if (dictionary.TryGetValue(key2, out value2))
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

        public static void CircleComp(byte[] buffer)
        {
            if(buffer.Length>200)
            {
                return;
            }

            //Split the arrays up.

            List<byte[]> buffers = new List<byte[]>();


        }
    }
}
