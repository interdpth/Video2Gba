using System;
using System.Collections.Generic;
using System.Linq;

namespace Video2Gba
{
    public static class BufferHelper
    {


        public static List<byte[]> Buffer2QuadStraight(byte[] buff)
        {
            if (buff.Length % 4 != 0) throw new Exception("Buffer is odd");
            ushort[] convertBuf = new ushort[buff.Length];
            Buffer.BlockCopy(buff, 0, convertBuf, 0, buff.Length);

            //basically expandable memorystreams.
            List<IOStream> quad = new List<IOStream>() { new IOStream(), new IOStream(), new IOStream(), new IOStream() };
            //we are a byte so width and height is times 2

            //240x160
            //Handle first screen.
            int y = 0;
            //Top left   &&    //Top right
            int l = buff.Length / 4;
            for (int i = 0; i < 4; i++)
            {
                var b = buff.ToList().GetRange(i * l, l);
                quad[i].CopyFromArray(b.ToArray(), b.Count);
            }



            return new List<byte[]> { quad[0].Data, quad[1].Data, quad[2].Data, quad[3].Data, };
        }

        public static List<byte[]> Buffer2Quad(byte[] buff, int width, int height)
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
            for (; y < height / 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Check if capturing right
                    if (x < width / 2)
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
            for (; y < height; y++)
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

        public static List<byte[]> Buffer2Interleave(byte[] buff, int bytewidth)
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
            for (int i = 0; i < buff.Length;)
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


    }
}
