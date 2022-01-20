using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Video2Gba
{
    public unsafe class VideoInfo
    {
        public long TotalSize;

        Dictionary<string, Container> Files = new Dictionary<string, Container>();
        public Dictionary<string, string> fileCheckSums = new Dictionary<string, string>();

        private IntPtr filePtr;
        public VideoInfo()
        {




            //Gen checksums
        }

        public void AddFile(string file, Container c)
        {
            Files[file] = c;
        }
        public Dictionary<string, string> similarfiles = new Dictionary<string, string>();

        public int CountSimilarFiles()
        {
            return 0;
        }

        public long GetSize()
        {
            long thesize = 0;
            foreach (var file in Files)
            {
                if (file.Value.IsData())
                {
                    thesize += file.Value.Length;
                }
            }

            return thesize;
        }
        public void Compress1()
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();
            foreach (var frame in Files)
            {
                //Do we have clones?
                if (fixedList.Contains(frame.Value.ID)) continue;
                var dupFrames = Files.Where(x => x.Value.ID != frame.Value.ID && x.Value.CheckSum == frame.Value.CheckSum);
                foreach (var dup in dupFrames)
                {
                    dup.Value.SetIndex(frame.Value.ID);
                    fixedList.Add(dup.Value.ID);
                }
                fixedList.Add(frame.Value.ID);
            }
            long newSize = GetSize();

            Console.WriteLine($"New size {newSize}");
            Console.WriteLine($"Bytes saved {oldSize - newSize}");
            Console.WriteLine($"Comp1 done");

        }
        public void Encoder()
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();
            Container lastFrame = null;

            Container GetContainerAsData(long ID)
            {
                var blah = Files.Where(x => x.Value.ID == ID && x.Value.IsData()).FirstOrDefault();

                return blah.Value;

            }
            foreach (var frame in Files.Where(x => x.Value.IsData()))
            {
                bool keyFrame = true;
                byte[] rawData = frame.Value.Data;
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
                        huff = comp.HuffCompress();
                    }
                }
                catch (Exception e)
                {
                    //Compression was bad. 
                    huff = null;
                    Console.WriteLine("Bad data");
                }
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


                if (lastFrame != null && (frame.Value.ID - lastFrame.ID) == 1) //We can only differ if id - id =1 
                {
                    var lastFrameReal = GetContainerAsData(lastFrame.ID);

                    try
                    {
                        using (var comp = new GbaNativeCompression(lastFrameReal.OGData, rawData))
                        {
                            diff8 = comp.Differ8Compress();
                        }
                    }
                    catch (Exception e)
                    {
                        //Compression was bad. 
                        diff8 = null;
                        Console.WriteLine("Bad data");
                    }

                    try
                    {

                        using (var comp = new GbaNativeCompression(lastFrameReal.OGData, rawData))
                        {
                            diff16 = comp.Differ16Compress();
                        }
                    }
                    catch (Exception e)
                    {
                        //Compression was bad. 
                        diff16 = null;
                        Console.WriteLine("Bad data");
                    }
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

                try
                {
                    using (var comp = new GbaNativeCompression(rawData))
                    {
                        rlComp = comp.RleCompress();

                    }
                }
                catch (Exception e)
                {
                    //Compression was bad. 
                    rlComp = null;
                    Console.WriteLine("Bad data");
                }
                //IOStream outbuffer = new IOStream();
                //VideoCompression.RleCompress(new IOStream(frame.Value.Data), ref outbuffer);
                //byte[] rlComp = outbuffer.Data;

                int bestSize = frame.Value.Data.Length;
                byte[] bestBuffer = frame.Value.Data;
                bool changed = false;
                if (lzComp != null && lzComp.Length < bestSize)
                {
                    bestBuffer = lzComp;
                    bestSize = lzComp.Length;
                    changed = true;
                }

                if (rlComp != null && rlComp.Length < bestSize)
                {
                    bestBuffer = rlComp;
                    bestSize = rlComp.Length;
                    changed = true;
                }
                if (rlComp16 != null && rlComp16.Length < bestSize)
                {
                    bestBuffer = rlComp16;
                    bestSize = rlComp16.Length;
                    changed = true;
                }
                if (diff8 != null && diff8.Length < bestSize)
                {
                    bestBuffer = diff8;
                    bestSize = diff8.Length;
                    changed = true;
                    keyFrame = false;
                }

                if (diff16 != null && diff16.Length < bestSize)
                {
                    bestBuffer = diff16;
                    bestSize = diff16.Length;
                    changed = true;
                    bestSize = bestBuffer.Length;
                    keyFrame = false;
                }

                if (huff != null && huff.Length < bestSize)
                {
                    bestBuffer = huff;
                    bestSize = huff.Length;
                    changed = true;
                }
                //if (rlComp.Length < compressed.Length)
                //{
                //    compressed = rlComp;
                //}
                lastFrame = new Container(frame.Value);

                using (var comp = new GbaNativeCompression(bestBuffer))
                {
                    bestBuffer = comp.Rle16Compress();
                }

                frame.Value.SetData(bestBuffer);

            }
            long newSize = GetSize();

            Console.WriteLine($"New size {newSize}");
            Console.WriteLine($"Bytes saved {oldSize - newSize}");

            //Dump it in a huge file for analyzing

            List<byte> fullFIle = new List<byte>();
            Dictionary<long, int> frameOffsets = new Dictionary<long, int>();
            List<int> rawOffset = new List<int>();

            foreach (var files in Files)
            {
                int offset;
                if (files.Value.IsData())
                {
                    offset = fullFIle.Count;
                    frameOffsets[files.Value.ID] = fullFIle.Count;

                    fullFIle.AddRange(files.Value.Data);

                }
                else
                {
                    offset = frameOffsets[files.Value.Index];
                }
                rawOffset.Add(offset);
            }
            List<byte> realFile = new List<byte>();

            //write frame start
            realFile.AddRange(BitConverter.GetBytes(rawOffset.Count));
            foreach (var o in rawOffset)
            {
                realFile.AddRange(BitConverter.GetBytes(o));
            }
            realFile.AddRange(fullFIle);
            File.WriteAllBytes("compressed.dat", fullFIle.ToArray());
            Console.WriteLine($"Comp2 done");
        }

        private List<FrameBlock> CreateFrameBlocks(int blockWidth, int blockHeight, ref int idBase, ref Dictionary<long, CustomFrame> frames)
        {
            List<FrameBlock> frameBlocks = new List<FrameBlock>();
            foreach (var frame in Files)
            {
                if (frame.Value.IsData())
                {
                    byte[] rawData = frame.Value.Data;
                    short[] newData = new short[rawData.Length / 2];
                    Buffer.BlockCopy(rawData, 0, newData, 0, rawData.Length);



                    //What are we doing.
                    //We are getting 16x16 blocks and inserting them. 
                    //A screen is 150 blocks. 
                    //Screen digits = 15x10
                    //1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
                    //2
                    //3
                    //etc
                    //init tmp data arrays

                    //160x128
                    //240x160
                    int maxX = 240 / blockWidth;
                    int maxY = 160 / blockHeight;
                    CustomFrame cf = new CustomFrame();

                    for (int x = 0; x < maxX; x++)
                    {
                        for (int y = 0; y < maxY; y++)
                        {
                            //hello, get our pixel 
                            short[] buff = new short[blockWidth * blockHeight];

                            for (int tmpX = 0; tmpX < blockWidth; tmpX++)
                            {
                                for (int tmpY = 0; tmpY < blockWidth; tmpY++)
                                {
                                    int trueX = tmpX + x;
                                    int trueY = tmpY + y;
                                    int pixel = trueX + trueY * 240;

                                    buff[tmpX + tmpY * blockWidth] = newData[pixel];
                                }
                            }

                            byte[] tmp = new byte[buff.Length * 2];
                            Buffer.BlockCopy(buff, 0, tmp, 0, buff.Length * 2);
                            FrameBlock newFrame = new FrameBlock(idBase, tmp);

                            //See if we have an id.

                            int index = -1;
                            for (int i = 0; i < frameBlocks.Count; i++)
                            {
                                FrameBlock comp = frameBlocks[i];
                                if (comp.CheckSum == newFrame.CheckSum)
                                {
                                    //yay, we have a match.
                                    index = i;
                                    break;
                                }
                            }

                            if (index == -1)
                            {
                                frameBlocks.Add(newFrame);

                                cf.ContainerIDs.Add(idBase);
                                idBase++;
                            }
                            else
                            {
                                cf.ContainerIDs.Add(index);
                            }
                        }
                    }
                    frames[frame.Value.ID] = (cf);
                }
            }

            return frameBlocks;
        }


        public int GetByteSizeForFrames(ref List<FrameBlock> frameBlocks, ref Dictionary<long, CustomFrame> frames)
        {
            int nsize = 8 + 4 + 4 + 4;
            for (int frameBlock = 0; frameBlock < frameBlocks.Count; frameBlock++)
            {
                var fb = frameBlocks[frameBlock];
                //header, id, index, length
                nsize += 1 + 4 + 4 + 4 + (int)fb.Length;

            }
            foreach (var cf in frames)
            {
                nsize += cf.Value.ContainerIDs.Count * 4;
            }
            nsize += 4;
            nsize += 4;
            return nsize;
        }


        void DumpFrameBlocks(IOStream newFile, List<FrameBlock> frameBlocks)
        {
            //Write frame blocks.
            newFile.WriteU32((uint)frameBlocks.Count);
     
            for (int frameBlock = 0; frameBlock < frameBlocks.Count; frameBlock++)
            {
                var fb = frameBlocks[frameBlock];
                IOStream compressBlocks = new IOStream();

                newFile.Write8((byte)fb.header);
                newFile.Write32((int)fb.ID);
                newFile.Write32((int)fb.Index);
                long lenPointer = newFile.Position;
                long realSize = 0;
                newFile.Write32((int)fb.Length);
                using (var compress = new GbaNativeCompression(fb.Data))
                {
                    byte[] newDAta = compress.Lz77Compress();

                    if (newDAta.Length < compressBlocks.Data.Length)
                    {
                      
                        newFile.Write8(0xFF);
                        long start = newFile.Position;
                        foreach (byte b in newDAta)
                        {
                            newFile.Write8(b);
                        }
                        long end = newFile.Position;
                        realSize = end - start;
                    }
                    else
                    {
                        //Lazy writing
                        newFile.Write8(0x00);
                        long start = newFile.Position;
                        newFile.Write(fb.Data, fb.Data.Length);
                        long end = newFile.Position;

                        realSize = end - start;
                    }
                
                }
                long curPos = newFile.Position;
                newFile.Position = lenPointer;
                newFile.Write32((int)realSize);
                newFile.Position = curPos;
            }
        }

        public void Compress3()
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();






            //HELLO THIS CAN GET COMPLICATRED MATT
            //WE WILL BREAK THE SCREEN UP INTO 16x16 sections. 
            //Once broken up, we will search for duplicates via containers. 
            //Once everything is sorted. a frame will just represent those blocks. 


            Container lastFrame = null;

            //MD5 of data, and FrameBlock

            Dictionary<long, CustomFrame> sceens = new Dictionary<long, CustomFrame>();
            //240x160
            //160x128 
            int idBase = 0;
            int blockWidth =240/16;
            int blockHeight = 160;


            //update frame screen to use frame hash
            //All done? 
            var frameBlocks = CreateFrameBlocks(blockWidth, blockHeight, ref idBase, ref sceens);
            //Write everything lol

            //Get sizes 
            //Offset, offset, count, filler, count
            int nsize = GetByteSizeForFrames(ref frameBlocks, ref sceens);


            //Real output.


            //1/10/2022 
            //remaining work
            //bottom of func is bad and copy paste code. just need to drop frame table then update data at beginning of stream


            //definition
            //Ascii header
            //byte FPS
            //byte frameblocks
            //byte blockWidth;
            //byte blockHeight;
            //4 bytes, pointer to FrameBlocks
            //4 bytes, pointer to Screens
            //4 bytes, NumberOfFrames
            //FrameBlockCount 4 bytes
            //FrameBlocks
            //Screens 
            //Frames -> Pointer to screen

            string header = "WHID.";

            int screenSize = 4 * sceens[0].ContainerIDs.Count * sceens.Count;
            IOStream newFile = new IOStream(nsize + 12 + header.Length + 1 + screenSize + Files.Count*4);
            newFile.WriteASCII(header);
            newFile.Write8(20);
            newFile.Write8((byte)blockWidth);
            newFile.Write8((byte)blockHeight);
            int pointerOffset = (int)newFile.Position;
            newFile.Position = pointerOffset + 12;
            int frameBlocksPointer = (int)newFile.Position;
            DumpFrameBlocks(newFile, frameBlocks);
            newFile.Write32(0xFFFFFFF);//Filler between frame code blocks, and screens
            //Write sscreens
            int screenPositions = (int)newFile.Position;
            newFile.Write32(sceens.Count);
            newFile.Write16((ushort)sceens[0].ContainerIDs.Count);
            Dictionary<long, int> idByOffset = new Dictionary<long, int>();
            foreach (var cf in sceens)
            {
                idByOffset[cf.Key] = (int)newFile.Position;
                foreach (var e in cf.Value.ContainerIDs)
                {
                    newFile.Write32(e);
                }
            }

            newFile.Write32(0xFFFFFFF);//screens and frame pointers
            int framePositions = (int)newFile.Position;
            newFile.Write32(Files.Count);
            //Frame count will not match 
            foreach (var files in Files)
            {
                int offset = idByOffset[files.Value.IsData() ? files.Value.ID : files.Value.Index];

                newFile.Write32(offset);
            }
            long endOfFile = newFile.Position;
            newFile.Position = pointerOffset;
            ///write headers
            //4 bytes, pointer to FrameBlocks
            //4 bytes, pointer to Screens
            //4 bytes, NumberOfFrames

            newFile.Write32(frameBlocksPointer);
            newFile.Write32(screenPositions);
            newFile.Write32(framePositions);

            byte[] newArray = new byte[endOfFile];
            Array.Copy(newFile.Data, newArray, endOfFile);
           File.WriteAllBytes($"whidglecodec{blockWidth}x{blockHeight}_full", newArray);
            long newSize = GetSize();

            Console.WriteLine($"New size {newSize}");
            Console.WriteLine($"Bytes saved {oldSize - newSize}");
            Console.WriteLine($"Comp2 done");


            DecompressAndTest(frameBlocks, sceens, $"whidglecodec{blockWidth}x{blockHeight}_full");
        }

        void DecompressAndTest(List<FrameBlock> oldFrameBlocks, Dictionary<long, CustomFrame> oldScreens, string tstfile)
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();
            Container lastFrame = null;
            Dictionary<long, CustomFrame> sceens = new Dictionary<long, CustomFrame>();
            List<FrameBlock> frameBlocks = new List<FrameBlock>();

            byte[] loadFile = File.ReadAllBytes(tstfile);
            IOStream movieFIle = new IOStream(loadFile);


            //Ascii header
            //byte FPS
            //byte frameblocks
            //byte blockWidth;
            //byte blockHeight;
            //4 bytes, pointer to FrameBlocks
            //4 bytes, pointer to Screens
            //4 bytes, NumberOfFrames
            //FrameBlockCount 4 bytes
            //FrameBlocks
            //Screens 
            //Frames -> Pointer to screen


            //How we will check.
            Dictionary<string, Container> newFiles = new Dictionary<string, Container>();
            string headerTest = "WHID.";
            string header = movieFIle.ReadASCII(headerTest.Length);

            if (headerTest != header)
            {
                throw new Exception("Invalid WhidgleCodecFIle");
            }
            int fps = movieFIle.Read8();
            int blockWidth = movieFIle.Read8();
            int blockHeight = movieFIle.Read8();


            int frameBlockPointer = movieFIle.Read32();
            int screenPointer = movieFIle.Read32();
            int framesPointer = movieFIle.Read32();

            //Real output.
            //Screen BLocks.

            movieFIle.Position = frameBlockPointer;
            int frameBlockSize = movieFIle.Read32();
            List<FrameBlock> blocks = new List<FrameBlock>();
            for (int frameBlock = 0; frameBlock < frameBlockSize; frameBlock++)
            {
                byte compressheader = movieFIle.Read8();
                int index = movieFIle.Read32();
                int id = movieFIle.Read32();
                int datalen = movieFIle.Read32();
                byte datacompressed = movieFIle.Read8();
                byte[] data = new byte[datalen];
                Array.Copy(movieFIle.Data, movieFIle.Position, data, 0, datalen);
                movieFIle.Position += datalen;
                FrameBlock newBlock = new FrameBlock(compressheader, id, datacompressed==0xFF, data, index);
                blocks.Add(newBlock);
            }

            //CHecking 

            //everything should be the same.
            if (blocks.Count != oldFrameBlocks.Count)
            {
                throw new Exception("Block count was wrong");
            }


            for (int i = 0; i < blocks.Count; i++)
            {
                var old = oldFrameBlocks[i];
                var newb = blocks[i];

                if (old.ID != newb.ID) throw new Exception("Ids don't match");

                if (old.Index != newb.Index) throw new Exception("indexes do not match");
                if (old.Length != newb.Length) throw new Exception("Length does not match");
                old.GetCheckSum();
                newb.GetCheckSum();
                if (old.CheckSum != newb.CheckSum) throw new Exception("Checksums don't match");
            }


            movieFIle.Position = screenPointer;


            /*
                newFile.Write32(sceens.Count);
            newFile.Write16((ushort)sceens[0].ContainerIDs.Count);
            Dictionary<long, int> idByOffset = new Dictionary<long, int>();
            foreach (var cf in sceens)
            {
                idByOffset[cf.Key] = (int)newFile.Position;
                foreach (var e in cf.Value.ContainerIDs)
                {
                    newFile.Write32(e);
                }
            } 
             
             * 
             */
            //CheckBlocks();
            int screenCount = movieFIle.Read32();
            int numOfBlocksInScreen = movieFIle.Read16();


            long newSize = GetSize();

            Console.WriteLine($"New size {newSize}");
            Console.WriteLine($"Bytes saved {oldSize - newSize}");
            Console.WriteLine($"Comp2 done");
        }
    }

}

