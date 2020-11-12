using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class Frame
    {
        int dataCount;
        byte[] data;
    };



    public class describe
    {
        ushort index;
        byte value;
        public describe(ushort i, byte v)
        {
            index = i;
            value = v;
        }
    }

    class Program
    {
        private static string Processing = "Processing";
        private static string OutputFolder = "Output";
        private static readonly StringBuilder baseAssembly = new StringBuilder(@".gba
.arm
.open ""test.gba"", 0x8000000

.definelabel DMA0SAD, 0x040000D4
.definelabel DMA0DAD, 0x040000D8
.definelabel DMA0CNT_L, 0x040000Dc
.definelabel REG_DISPLAYCONTROL, 0x04000000
.definelabel VIDEOMODE_3, 0x0003
.definelabel BGMODE_2, 0x0400
.definelabel FrameCounter, 0x02026000

.org 0x8000000
//Hop to main
b armMain
.align 4
.org 0x80000C0
armMain:
ldr r0, = MainFunc + 1
//hop to thumb main
bx r0
interruptHandler:
LDR     R3, = 0x4000202
MOVS    R2, #1
STRH    R2, [R3]
LDR     R2, = 0x3007FF8
LDRH    R3, [R2]
MOVS    R1, #1
ORRS    R3, R1
STRH    R3, [R2]
BX      LR
.thumb

register_vblank_isr:
                LDR     R3, = 0x3007FFC
                LDR     R2, = interruptHandler
                STR     R2, [R3]
                LDR     R2, = 0x4000004
                LDRH    R3, [R2]
                MOV    R1, #8
                ORR    R3, R1
                STRH    R3, [R2]
                LDR     R2, = 0x4000200
                LDRH    R3, [R2]
                MOV    R1, #1
                ORR    R3, R1
                STRH    R3, [R2]
                LDR     R3, = 0x4000208
                MOV    R2, #1
                STRH    R2, [R3]
                BX      LR


VBlankIntrWait:
SWI             5
BX              LR

// void __fastcall DMA3(int srcAdd, int dstAdd, int size)
DMA3:
                LDR     R3, = 0x40000D4
                STR     R0, [R3]
                LDR     R3, = 0x40000D8
                STR     R1, [R3]
                MOV    R3, #0x80000000
                ORR    R3, R2
                LDR     R2, = 0x40000DC
                STR     R3, [R2]

                nop

                nop

                nop
                BX      LR

// void __fastcall LZ77UnCompVram(int src, int dst)

LZ77UnCompVram:
                // src = R0                               
                // dst = R1                               
                SWI     0x12
                BX      LR


                //decode, framedata
decode:
                add     R3, R0,#4                           
                LDR     R2, [R3]
                LDR     R1, = 0x88FFFF00
                CMP     R2, R1
                BEQ     rawCopy
                LDR     R3, = 0x88FFFF01
                CMP     R2, R3
                BEQ     lzUncomp

returnDecode:

                BX      LR

rawCopy:

                LDR     R2, [R3,#4]                              
                ADD    R3, #8
                LDR     R1, = 0x40000D4
                STR     R3, [R1]


                LDR     R3, = 0x40000D8
                LDR   R1, =#0x6000000
                STR     R1, [R3]
                 LDR   R3,= #0x80000000
                ORR    R3, R2
                LDR     R2, = 0x40000DC
                STR     R3, [R2]
                B       returnDecode

lzUncomp:
LDR   R1, =#0x6000000  
mov r2, 0xC
add     R0, R0, r2
                SWI     0x12
                B       returnDecode

.pool
.align 4
MainFunc:
    //Set up bg
    ldr r0, =#0x4000000
	//videomode3|bgmode_2
	bl register_vblank_isr
    ldr r1,= #0x403
    str r1, [r0]
    ldr r2, =#FrameCounter//FrameCounter offset is in r2
	mov r0, 0
    str r0, [r2]

ourLoop:
bl VBlankIntrWait
//Clear registers
mov r0, 0
mov r1, 0
mov r2, 0
mov r3, 0

    CheckMaxFrames:
    //loop while FrameCounter < MaxFrames
    LoadFrameCounter:

        ldr r3,= #FrameCounter
		ldr r2, [r3]
        LoadMaxFrames:	

        ldr r1, =#MaxFrames
		ldr r1, [r1]
        CompareFrameValues:

        cmp r2, r1//if r0 is greater, then we're done
        bge end
        //otherwise loop.
        //increment frame count
        IncrementFrameCounter:

        add r2, 1
        str r2, [r3]
        mov r5, r2
        //get gfx table
        IndexFrameTable:	

        ldr r1, =#FrameTable
		lsl r2, 2

        add r1, r2//r1 is a struct of {offset, size} both words/long		

        ldr r1, [r1]

        mov r2, r1	//mov r1 into r2

    CalcSize:		

        add r2, 4
        ldr r2, [r2]//r2 contains size offset

    DecodeStuff:
        ldr r0, [r1]
        bl decode
    b ourLoop
.pool
end:
    ldr r2, =#FrameCounter//FrameCounter offset is in r2
	mov r0, 0

    str r0, [r2]
b ourLoop
//pop {r14}
//bx r0
.pool
.align 4
");
        private const uint RAWHEADER = 0x88FFFF00;
        private const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
        private const uint HUFFMANCOMPRESSEDHEADER = 0x88FFFF02;
        private const uint DESCRIBEHEADER = 0x88FFFF03;
        public static Semaphore sem = new Semaphore(1, 1);
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


        List<Frame> frames;
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

        private static void Compress(byte[] buffer, string fn)
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

        public static string AssembleVideoRom()
        {
            StringBuilder assembly = new StringBuilder();
            assembly.Append(baseAssembly);
            assembly.AppendLine("");
            var files = Directory.GetFiles(OutputFolder);
            //Convert audio
            var n = files.OrderBy(x => x).ToList().Where(x => x.Contains(".bin")).ToList().OrderBy(x => int.Parse(x.Replace("Output\\tmp", "").Replace(".img.bin", ""))).ToList();//Make sure frame are in right order
            
            assembly.AppendLine("MaxFrames: .word " + n.Count);
            StringBuilder FramesInclude = new StringBuilder();
            StringBuilder FramesIncludeTable = new StringBuilder();
            StringBuilder FramesIncludeTablePointers = new StringBuilder();
            FramesIncludeTablePointers.AppendLine("FrameTable:");
            foreach (string file in n)
            {

                FileInfo nf = new FileInfo(file);
                string thisFrame = nf.Name.Replace(".img.bin", "");

                FramesInclude.AppendLine($"{thisFrame}gfx:");
                FramesInclude.AppendLine($"\t.incbin \"{nf.FullName}\"");
                FramesInclude.AppendLine($".align 0x4");
                //Insert pointer to Gfx and size
                FramesIncludeTable.AppendLine($"\n{thisFrame}:");
                FramesIncludeTable.AppendLine($".word {thisFrame}gfx");
                FramesIncludeTable.AppendLine($".word 0x{nf.Length.ToString("X")}");
                FramesIncludeTable.AppendLine($".align 0x4");
                //Make table 
                FramesIncludeTablePointers.AppendLine($".word {thisFrame}");
                FramesIncludeTablePointers.AppendLine($".align 0x4");
            }



            assembly.Append(FramesIncludeTablePointers);
            assembly.Append(FramesInclude);
            assembly.Append(FramesIncludeTable);
            assembly.AppendLine("//Audio track");
            assembly.AppendLine("audio: ");
         
            var z = files.OrderBy(x => x).ToList().Where(x => x.Contains(".raw")).ToList();//Make sure frame are in right order
         
            assembly.AppendLine($".incbin {z.First()}");
            assembly.Append(".close");
            return assembly.ToString();
        }

        public string SetupIncludes()
        {
            return "";
        }

        public static void RenderAudio(string srcFile)
        {
            int mffreq = 10512;
            int zmfreq = 13379;
            int freq = zmfreq;
            //if (title.ToLower() == "mf")
            //{
                freq = mffreq;
            //}
            //else if (title.ToLower() == "zm")
            //{
            //    freq = zmfreq;
            //}
            //else
            //{
            //    Console.WriteLine("Not Fusion or ZM, using freq " + title);
            //    freq = int.Parse(title);
            //}

            FileInfo srcAudio = new FileInfo(srcFile);

            //we need to be 8bit and mono channel, apply desired frequency.
            var outFormat = new WaveFormat(freq, 8, 1);

            //Find out level of decode 
            WaveStream srcStream = null;

            if (srcAudio.Extension.ToLower() == ".mp3")
            {
                Console.WriteLine("Decoding mp3.");
                srcStream = new Mp3FileReader(srcFile);
            }

            if (srcAudio.Extension.ToLower() == ".wav")
            {
                Console.WriteLine("Decoding wav.");
                srcStream = new WaveFileReader(srcFile);
            }

            if (srcStream == null)
            {
                Console.WriteLine($"{srcAudio.Extension} is an unsupported format");
                return;
            }

            //Convert either source to wave.
            using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(outFormat, srcStream))
            {
                using (RawSourceWaveStream raw = new RawSourceWaveStream(conversionStream, outFormat))
                {
                    //Convert to signed 8bit.
                    raw.Seek(0, SeekOrigin.Begin);
                    int len = 0;
                    List<sbyte> data = new List<sbyte>();
                    for (; len < raw.Length; len++)
                    {
                        sbyte n = Convert.ToSByte(raw.ReadByte() - 128);
                        data.Add(n);

                    }                    //Generate 
                    //Write it

                    using(FileStream fs = new FileStream($"{OutputFolder}\\{srcAudio.Name}.raw", FileMode.OpenOrCreate))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        for (len = 0; len < raw.Length; len++)
                        {
                            bw.Write(data[len]);
                        }
                        bw.Close();                        
                    }
                }
            }
        }


        static void Main(string[] args)
        {

            Console.WriteLine("Decoding video");
            // get wav
            //./ffmpeg -i lztown.mp4 .\lztown.wav
            if (!Directory.Exists(Processing))
            {
                Directory.CreateDirectory(Processing);
            }
            else
            {
                List<string> killme = Directory.GetFiles(Processing).ToList();
                foreach (string s in killme)
                {
                    File.Delete(s);
                }
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }
            else
            {
                List<string> killme = Directory.GetFiles(OutputFolder).ToList();
                foreach (string s in killme)
                {
                    File.Delete(s);
                }
            }


            //Re-encode.
            var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i lztown.mp4 -filter:v fps=fps=10 {Processing}\\lztown.mp4" };
            var P = Process.Start(PSI);
            P.WaitForExit();


            PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\lztown.mp4 {Processing}\\lztown.wav" };
            P = Process.Start(PSI);
            P.WaitForExit();

            PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\lztown.mp4 -s 240x160 {Processing}/tmp%03d.png" };
            P = Process.Start(PSI);
            P.WaitForExit();


            List<string> images = Directory.GetFiles(Processing).ToList().Where(x => x.Contains("png")).ToList();

            List<Thread> conversions = new List<Thread>();
            List<Thread> bgconversions = new List<Thread>();
            int dumblock = 0;
        
            for (int a = 0; a < images.Count; a++)
            {
                string hey = images[a];
                var t = new Thread(
                    () =>
               {

                   PSI = new ProcessStartInfo { RedirectStandardError = true, RedirectStandardOutput = true, FileName = "grit.exe", UseShellExecute = false, Arguments = $"{hey} -gb -gB 16 -ftbin" };
                   //creates tmp001.img.bin
                   P = Process.Start(PSI);
                   P.WaitForExit();
                   FileInfo deets = new FileInfo(hey);
                   string rawName = deets.Name.Replace(".png", "");
                   var t2 = new Thread(
                 () =>
                 {
                     try
                     {

                         byte[] data = File.ReadAllBytes(rawName + ".img.bin");

                         //insert gfx                    
                         Compress(data, OutputFolder + "//" + rawName + ".img.bin");

                         File.Delete(rawName + ".img.bin");
                         Thread.Sleep(100);
                         File.Delete(hey);
                         Thread.Sleep(100);
                     }
                     catch (Exception e)
                     {

                     }
                     Console.WriteLine($"Processed {hey}");
                 });

                   Program.sem.WaitOne();

                   while (bgconversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) >10) ;
                   t2.IsBackground = true;
                   t2.Start();
                   bgconversions.Add(t2);
                   Program.sem.Release();
                
               }
               );

                //Wait until free

                while (conversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 100) ;
                t.IsBackground = true;
                t.Start();
                conversions.Add(t);
                conversions.RemoveAll(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive);
            }

            while (conversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 0) ;
            Console.WriteLine("Encoding video");
            if (File.Exists("Output//testplz.asm")) { File.Delete("Output//testplz.asm"); }
            //Wait for the others to be done.

            while(true)
            {
                Program.sem.WaitOne();
                if (bgconversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) == 0)
                {
                    Program.sem.Release(); break;
                }
                else
                {
                    Program.sem.Release();
                }
                Thread.Sleep(1000);
            }
            RenderAudio($"{Processing}\\lztown.wav");
            string asm = AssembleVideoRom();
            File.WriteAllText("Output//testplz.asm", asm);
            Console.WriteLine("Finished video");
            //./ffmpeg 
            //get all files
            //256*192*
        }
    }
}
