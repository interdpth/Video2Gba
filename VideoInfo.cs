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
        }

        public void AddFile(string file, Container c)
        {
            Files[file] = c;
        }

        public Dictionary<string, string> similarfiles = new Dictionary<string, string>();

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

        public int GetIndex(List<FrameBlock> frameBlocks, FrameBlock newFrame)
        {
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

            return index;
        }

        public void SetPixelBlock(int screenSize, int baseX, int baseY, int blockWidth, int blockHeight, ref short[] image, FrameBlock src)
        {
            for (int tmpX = 0; tmpX < blockWidth; tmpX++)
            {
                for (int tmpY = 0; tmpY < blockHeight; tmpY++)
                {
                    int trueX = tmpX + baseX;
                    int trueY = tmpY + baseY;
                    int pixel = trueX + trueY * screenSize;
                    image[pixel] = src.OGData[tmpX + tmpY * blockWidth];
                }
            }
        }

        public byte[] GetPixelBlock(int screenSize, int baseX, int baseY, int blockWidth, int blockHeight, short[] newData)
        {
            //hello, get our pixel 
            short[] buff = new short[blockWidth * blockHeight];

            for (int tmpX = 0; tmpX < blockWidth; tmpX++)
            {
                for (int tmpY = 0; tmpY < blockHeight; tmpY++)
                {
                    int trueX = tmpX + baseX;
                    int trueY = tmpY + baseY;
                    int pixel = trueX + trueY * screenSize;

                    buff[tmpX + tmpY * blockWidth] = newData[pixel];
                }
            }

            byte[] tmp = new byte[buff.Length * 2];
            Buffer.BlockCopy(buff, 0, tmp, 0, buff.Length * 2);
            return tmp;
        }

        Dictionary<int, FrameBlock> loadedBlocks = new Dictionary<int, FrameBlock>();

        public void ClearBlocks(CustomFrame thisFrame)
        {
            var loadedKeys = loadedBlocks.Keys.ToList();
            foreach (int key in loadedKeys)
            {
                if (!thisFrame.ContainerIDs.Contains(key))
                {
                    loadedBlocks.Remove(key);
                }
            }
        }

        void LoadBlock(List<FrameBlock> blocks, CustomFrame currentFrame, int blockId)
        {
            //Do we have more blocks loaded then needed 

            if (loadedBlocks.Count > currentFrame.ContainerIDs.Count - 1)
            {
                //Clear our blocks we don't need. 
                ClearBlocks(currentFrame);
            }
            var possibleBlocks = blocks.Where(x => x.ID == blockId);

            loadedBlocks[blockId] = possibleBlocks.First();
        }

        //returns how many blocks are in usev
        private int CreateFromBlocks(List<FrameBlock> blocks, CustomFrame lastFrame, CustomFrame currentFrame, ref short[] image, int blockWidth, int blockHeight)
        {
            int blockId = -1;
            for (int i = 0; i < currentFrame.ContainerIDs.Count; i++)
            {
                blockId = currentFrame.ContainerIDs[i];
                if (lastFrame == null || lastFrame.ContainerIDs[i] != currentFrame.ContainerIDs[i])
                {

                    int blockCount = loadedBlocks.Count(x => x.Value.ID == blockId);
                    if (blockCount == 0)
                    {
                        LoadBlock(blocks, currentFrame, blockId);
                    }
                }

                //240x160
                int maxX = 240 / blockWidth;
                int maxY = 160 / blockHeight;

                //y = i/MaxX
                //x = i%maxY
                //Now draw 

                //y=mx+b
                //x = m+b/y
                int baseX = i;
                int baseY = i % maxY;
                SetPixelBlock(240, baseX* blockWidth, baseY* blockHeight, blockWidth, blockHeight, ref image, loadedBlocks[blockId]);

            }
            return loadedBlocks.Count;
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

                            byte[] tmp = GetPixelBlock(240, x* blockWidth, y* blockHeight, blockWidth, blockHeight, newData);

                            FrameBlock newFrame = new FrameBlock(idBase, tmp);

                            //See if we have an id.

                            int index = GetIndex(frameBlocks, newFrame);

                            if (index == -1)
                            {
                                frameBlocks.Add(newFrame);

                                cf.ContainerIDs.Add(idBase);

                                index = idBase++;
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
                    //byte[] newDAta = compress.Lz77Compress();

                    //if (newDAta.Length < compressBlocks.Data.Length)
                    //{

                    //    newFile.Write8(0xFF);
                    //    long start = newFile.Position;
                    //    foreach (byte b in newDAta)
                    //    {
                    //        newFile.Write8(b);
                    //    }
                    //    long end = newFile.Position;
                    //    realSize = end - start;
                    //}
                    //else
                    //{
                    //Lazy writing
                    newFile.Write8(0x00);
                    long start = newFile.Position;
                    newFile.Write(fb.Data, fb.Data.Length);
                    long end = newFile.Position;

                    realSize = end - start;
                    // }

                }
                long curPos = newFile.Position;
                newFile.Position = lenPointer;
                newFile.Write32((int)realSize);
                newFile.Position = curPos;
            }
        }

        public void GenerateBinary()
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();

            Container lastFrame = null;

            //MD5 of data, and FrameBlock

            Dictionary<long, CustomFrame> sceens = new Dictionary<long, CustomFrame>();
            //240x160
            //160x128 
            int idBase = 0;
            int blockWidth = 240 / 16;
            int blockHeight = 160;


            //update frame screen to use frame hash
            //All done? 
            var frameBlocks = CreateFrameBlocks(blockWidth, blockHeight, ref idBase, ref sceens);
            //Write everything lol

            //Get sizes 
            //Offset, offset, count, filler, count
            int nsize = GetByteSizeForFrames(ref frameBlocks, ref sceens);


            //Real output.


            //1/21/2022 
            //remaining work
            //reconstructing frame needs work


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

            int screenSize = 4 * sceens[sceens.Keys.First()].ContainerIDs.Count() * sceens.Count;
            IOStream newFile = new IOStream(nsize + 12 + header.Length + 1 + screenSize + Files.Count * 4);
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
            newFile.Write16((ushort)sceens[sceens.Keys.First()].ContainerIDs.Count);
            Dictionary<long, int> idByOffset = new Dictionary<long, int>();
            foreach (var cf in sceens)
            {
                idByOffset[cf.Key] = (int)newFile.Position;
                newFile.Write32((int)cf.Key);
                foreach (var e in cf.Value.ContainerIDs)//update to drop size
                {
                    newFile.Write32(e);
                }
            }

            newFile.Write32(0xFFFFFFF);//screens and frame pointers
            int framePositions = (int)newFile.Position;
            newFile.Write32(Files.Count);

            List<int> offsetz = new List<int>();
            //Frame count will not match 
            foreach (var files in Files)
            {
                int offset = idByOffset[files.Value.IsData() ? files.Value.ID : files.Value.Index];
                offsetz.Add(offset);
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


            DecompressAndTest(frameBlocks, sceens, offsetz, $"whidglecodec{blockWidth}x{blockHeight}_full");
        }

        void DecompressAndTest(List<FrameBlock> oldFrameBlocks, Dictionary<long, CustomFrame> oldScreens, List<int> offsetlist, string tstfile)
        {
            long oldSize = GetSize();
            List<long> fixedList = new List<long>();
            CustomFrame lastFrame = null;
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
                int id = movieFIle.Read32();
                int index = movieFIle.Read32();
                int datalen = movieFIle.Read32();
                byte datacompressed = movieFIle.Read8();
                byte[] data = new byte[datalen];
                Array.Copy(movieFIle.Data, movieFIle.Position, data, 0, datalen);
                movieFIle.Position += datalen;
                FrameBlock newBlock = new FrameBlock(compressheader, id, datacompressed == 0xFF, data, index);
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
                if (newb.header == Video2Gba.CompressionHeaders.LZCOMPRESSEDHEADER)
                {
                    Console.WriteLine("lol");
                }
                if (old.ID != newb.ID) throw new Exception("Ids don't match");

                if (old.Index != newb.Index) throw new Exception("indexes do not match");
                if (old.Length != newb.Length) throw new Exception("Length does not match");
                if (old.OGData.Length != newb.OGData.Length) throw new Exception("OGLength does not match");
                old.GetCheckSum();
                newb.GetCheckSum();
                if (old.CheckSum != newb.CheckSum) throw new Exception("Checksums don't match");
            }


            movieFIle.Position = screenPointer;
            int key = 0;
            int count = movieFIle.Read32();
            int numper = movieFIle.Read16();
            for (int screenCounter = 0; screenCounter < count; screenCounter++)
            {
                CustomFrame list = new CustomFrame();
                key = movieFIle.Read32();
                for (int idCount = 0; idCount < numper; idCount++)
                {
                    list.ContainerIDs.Add(movieFIle.Read32());
                }
                sceens[key] = list;
            }


            if (oldScreens.Count != count) throw new Exception("Screen count don't match");
            if (numper != oldScreens[sceens.Keys.First()].ContainerIDs.Count) throw new Exception("Screens per Screen match does not match");

            //make sure all match
            var m1 = oldScreens.Keys.OrderBy(x => x).ToList();
            var m2 = sceens.Keys.OrderBy(x => x).ToList();

            for (int keyCheck = 0; keyCheck < m1.Count(); keyCheck++)
            {
                if (m1[keyCheck] != m2[keyCheck]) throw new Exception("Mismatch");
            }
            //Compare


            for (int keyCheck = 0; keyCheck < m1.Count(); keyCheck++)
            {
                var tmp = m1[keyCheck];
                var c1 = oldScreens[tmp];
                var c2 = sceens[tmp];

                for (int i = 0; i < c1.ContainerIDs.Count; i++)
                {
                    if (c1.ContainerIDs[i] != c2.ContainerIDs[i])
                    {
                        throw new Exception("Screens aren't right.");
                    }
                }
            }

            //Okay, just check the frames.
            movieFIle.Position = framesPointer;
            int len = movieFIle.Read32();
            List<int> offsets = new List<int>();
            for (int i = 0; i < len; i++)
            {
                offsets.Add(movieFIle.Read32());
            }


            for (int i = 0; i < offsetlist.Count; i++)
            {
                if (offsets[i] != offsetlist[i]) throw new Exception("Offsets failed to validate");
            }

            Console.WriteLine("Succesfully validated");
            //Check against
            List<int> currentScreen = new List<int>();
            short[] ScreenGFX = new short[240 * 160];
            int lastoffset = 0;
            Dictionary<int, Container> conainters = new Dictionary<int, Container>();
            key = 0;
            for (int i = 0; i < offsetlist.Count; i++)
            {
                //Let's form

                //on the gba, we just iterate here, but we need to make sure it all works and it's loaded so..

                //Get your screen
                long lookup = offsetlist[i];

                CustomFrame newScreen = null;
                bool runFirst = true;

                if (lookup != lastoffset)
                {
                    movieFIle.Position = lookup;
                    //Read our key
                    key = movieFIle.Read32();
                    newScreen = sceens[key];//If this works we're good to go.

                    CreateFromBlocks(blocks, lastFrame, newScreen, ref ScreenGFX, blockWidth, blockHeight);
                    runFirst = false;
                    Console.WriteLine("lol");
                    lastFrame = newScreen;
                }

                Container c = new Container((long)key, data: ScreenGFX);
                conainters[key] = c;
            }

            if (Files.Count != conainters.Count) throw new Exception("Failure matching container count");


            Dictionary<int, Container> remap = new Dictionary<int, Container>();
            foreach (var ma in Files)
            {
                remap[(int)ma.Value.ID] = ma.Value;
            }

            var b = remap.Keys;
            foreach (int k in b)
            {
                //Update Checksums
                var obj1 = remap[k];
                var obj2 = conainters[k];
                if (obj1.CheckSum != obj2.CheckSum)
                {
                    File.WriteAllBytes("obj1", obj1.OGData);
                    File.WriteAllBytes("obj2", obj2.OGData);
                    for (int i = 0; i < obj1.OGData.Length; i++)
                    {
                        if (obj1.OGData[i] != obj2.OGData[i])
                        {
                            throw new Exception("Checksums failed");
                        }
                    }

                }

            }


            long newSize = GetSize();

            Console.WriteLine($"New size {newSize}");
            Console.WriteLine($"Bytes saved {oldSize - newSize}");
            Console.WriteLine($"Comp2 done");
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

    }

}

