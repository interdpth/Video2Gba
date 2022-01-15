using System;

namespace Video2Gba
{
    public class FrameBlock : Container
    {
        public CompressionHeaders header;

        public FrameBlock(byte hdr, long id, byte[] rawDataz = null, int theIndex = -1) : base(id, rawDataz, theIndex, true)
        {
            header = (CompressionHeaders)hdr;

            Decompress();
        }

        public FrameBlock(long id, byte[] rawDataz = null, int theIndex = -1) : base(id, rawDataz, theIndex)
        {
            byte[] rawData = null;
            int oldSize = rawDataz.Length;

            using (var comp = new GbaNativeCompression(rawDataz))
            {
                rawData = comp.Set1D();
            }
            int DSize = rawData.Length;
            byte[] lzComp = null;
            byte[] rlComp = null;
            byte[] rlComp16 = null;
            byte[] diff8 = null;
            byte[] diff16 = null;
            byte[] huff = null;

            try
            {
                using (var comp = new GbaNativeCompression(rawData))
                {
                    lzComp = comp.Lz77Compress();
                }
            }
            catch (Exception e)
            {
                //Compression was bad. 
                lzComp = null;
                Console.WriteLine("Bad data");
            }
            try
            {
                using (var comp = new GbaNativeCompression(rawData))
                {
                    rlComp16 = comp.Rle16Compress();
                }
            }
            catch (Exception e)
            {
                //Compression was bad. 
                rlComp16 = null;
                Console.WriteLine("Bad data");
            }

            //try
            //{
            //    //using (var comp = new GbaNativeCompression(rawData))
            //    //{
            //    //    rlComp = comp.RleCompress();

            //    //}
            //    IOStream inIo = new IOStream(rawData);
            //    IOStream outIo = new IOStream();

            //    VideoCompression.RLECompress(inIo, rawData.Length, outIo);
            //    rlComp = outIo.Data;


            //}
            //catch (Exception e)
            //{
            //    //Compression was bad. 
            //    rlComp = null;
            //    Console.WriteLine("Bad data");
            //}
            //IOStream outbuffer = new IOStream();
            //VideoCompression.RleCompress(new IOStream(frame.Value.Data), ref outbuffer);
            //byte[] rlComp = outbuffer.Data;
            header = CompressionHeaders.RAWHEADER;
            int bestSize = Data.Length;
            byte[] bestBuffer = Data;
            bool changed = false;
            if (lzComp != null && lzComp.Length < bestSize)
            {
                bestBuffer = lzComp;
                bestSize = lzComp.Length;
                changed = true;


                header = CompressionHeaders.LZCOMPRESSEDHEADER;
            }

            if (rlComp != null && rlComp.Length < bestSize)
            {
                //bestBuffer = rlComp;
                //bestSize = rlComp.Length;
                //changed = true;
                //header = CompressionHeaders.RLEHEADER;
            }
            if (rlComp16 != null && rlComp16.Length < bestSize)
            {
                bestBuffer = rlComp16;
                bestSize = rlComp16.Length;
                changed = true;
                header = CompressionHeaders.RLEHEADER16;
            }

            SetData(bestBuffer);
            //Best compression for us?
        }


        private void Decompress()
        {
            switch (header)
            {
                case CompressionHeaders.LZCOMPRESSEDHEADER:
                    using (var comp = new GbaNativeCompression(Data))
                    {
                        OGData = comp.Lz77Deompress();
                    }
                    break;
                case CompressionHeaders.RLEHEADER:
                    using (var comp = new GbaNativeCompression(Data))
                    {
                        OGData = comp.RleDecompress();
                    }
                    break;
                case CompressionHeaders.RLEHEADER16:
                    using (var comp = new GbaNativeCompression(Data))
                    {
                        OGData = comp.Rle16Decompress();
                    }
                    break;
                default:
                    throw new Exception("add support");
                    break;
            }


            //other wise we have the 1d data

            using (var comp = new GbaNativeCompression(Data))
            {
                OGData = comp.From1D();
            }
            GetCheckSum();
            //Used for data from disk.
        }
    }
}
