using Microsoft.WindowsAPICodePack.Shell;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Video2Gba
{

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
        //Should eventually be a config
        private static string Processing = "F:\\Processing";
        private static string OutputFolder = "F:\\Output";
        public static int numFrames = 0;
        private static List<string> fileInfos = new List<string>();
        public static Semaphore sem = new Semaphore(1, 1);

        public static Semaphore sem2 = new Semaphore(1, 1);
        public static List<string> FramesInclude = new List<string>();
        public static StringBuilder FramesTable = new StringBuilder("VidFrame theframes[]={");
        
        /// <summary>
        /// Spits out the base video code
        /// </summary>
        /// <param name="FPS">FPS we're using</param>
        /// <returns>Some c code</returns>
        public static string AssembleVideoCCode(string FPS)
        {
            StringBuilder assembly = new StringBuilder();

            var files = Directory.GetFiles(OutputFolder);
            //Convert audio



            Program.FramesTable.AppendLine("};");
            foreach (string s in FramesInclude) assembly.AppendLine(s);
            assembly.AppendLine("//Frames don't have a type.");
            assembly.AppendLine("typedef struct");
            assembly.AppendLine("{");
            assembly.AppendLine("    unsigned long* fra;");
            assembly.AppendLine("    unsigned long size");
            assembly.AppendLine("} VidFrame;");
            assembly.AppendLine($"const int FPS ={FPS}; ");
            assembly.AppendLine($"const int FrameCount = {FramesInclude.Count};");
            assembly.AppendLine(FramesTable.ToString());

            return assembly.ToString();
        }

        /// <summary>
        /// Renders an wav or mp3 to gba file format c source
        /// </summary>
        /// <param name="srcFile"></param>
        public static void RenderAudio(string srcFile)
        {
            int mffreq = 10512;
            int zmfreq = 13379;
            int freq = zmfreq;

            freq = mffreq;

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
                    string fn = srcAudio.Name.Replace(".wav", "");
                    using (FileStream fs = new FileStream($"{fn}.raw", FileMode.OpenOrCreate))
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        for (len = 0; len < raw.Length; len++)
                        {
                            bw.Write(data[len]);
                        }
                        bw.Close();
                    }



                    ROM.MakeSource(fn, data.ToArray(), $"{OutputFolder}");
                }
            }
        }
        static object lockobj = new object();
        // Function to swap a value from
        // big Endian to little Endian and
        // vice versa.
        static int swap_Endians(int value)
        {

            // This var holds the leftmost 8
            // bits of the output.
            int leftmost_byte;

            // This holds the left middle
            // 8 bits of the output
            int left_middle_byle;

            // This holds the right middle
            // 8 bits of the output
            int right_middle_byte;

            // This holds the rightmost
            // 8 bits of the output
            int rightmost_byte;

            // To store the result
            // after conversion
            int result;

            // Get the rightmost 8 bits of the number
            // by anding it 0x000000FF. since the last
            // 8 bits are all ones, the result will be the
            // rightmost 8 bits of the number. this will
            // be converted into the leftmost 8 bits for the
            // output (swapping)
            leftmost_byte = (value & 0x000000FF) >> 0;

            // Similarly, get the right middle and left
            // middle 8 bits which will become
            // the left_middle bits in the output
            left_middle_byle = (value & 0x0000FF00) >> 8;

            right_middle_byte = (value & 0x00FF0000) >> 16;

            // Get the leftmost 8 bits which will be the
            // rightmost 8 bits of the output
            rightmost_byte = (int)(value & 0xFF000000) >> 24;

            // Left shift the 8 bits by 24
            // so that it is shifted to the
            // leftmost end
            leftmost_byte <<= 24;

            // Similarly, left shift by 16
            // so that it is in the left_middle
            // position. i.e, it starts at the
            // 9th bit from the left and ends at the
            // 16th bit from the left
            left_middle_byle <<= 16;

            right_middle_byte <<= 8;

            // The rightmost bit stays as it is
            // as it is in the correct position
            rightmost_byte <<= 0;

            // Result is the concatenation of all these values.
            result = (leftmost_byte | left_middle_byle |
                      right_middle_byte | rightmost_byte);

            return result;
        }

        static void Main(string[] args)
        {
            int ticks_per_sample = 16777216 / 10512;

            IOStream tmp = new IOStream(0);

            /////Test Code
            //for (int i = 0; i < 240 * 160 * 4; i++)
            //{
            //    tmp.Write16(0);
            //    Random sel = new Random(i * 0x3E3C);

            //    int selec = sel.Next(0, 0xFFFF);
            //    if (selec % 2 == 0)
            //    {
            //        int randomValue = sel.Next(0, 0xFFFF);
            //        for (int z = 0; z < 32; z++)
            //        {

            //            tmp.Write16((byte)randomValue);
            //            i++;
            //        }
            //    }
            //    else
            //    {

            //        for (int z = 0; z < 32; z++)
            //        {
            //            int randomValue = sel.Next(0, 0xFFFF);
            //            tmp.Write16((byte)randomValue);
            //            i++;
            //        }
            //    }
            //}


            //int rand = 0xFE;
            //////0xFE 00 0x2 00
            //for (int i = 0; i < 32; i++)
            //{
            //    for (int j = 0; j < 8; j++)
            //    {
            //        rand = new Random(rand * (1 + i) * j).Next(0x1000, 0xFFFF);

            //        tmp.Write16((ushort)rand);
            //    }

            //    //  RLE16
            //    rand = new Random(rand * (1 + i)).Next(0x1000, 0xFFFF);
            //    int tryagain = rand;
            //    for (int j = 0; j < 4; j++)
            //    {
            //        tmp.Write16((ushort)rand);
            //    }
            //    // RAW
            //    for (int j = 0; j < 4; j++)
            //    {
            //        rand = new Random(rand * (1 + i) * j).Next(0x1000, 0xFFFF);

            //        tmp.Write16((ushort)rand);
            //    }
            //    //POINTER
            //    for (int j = 0; j < 4; j++)
            //    {
            //        tmp.Write16((ushort)tryagain);
            //    }
            //}

            //byte[] compresssed;
            //byte[] uncompressed;

            ////0x8a | whatever
            //using (var comp = new GbaNativeCompression(tmp.Data))
            //{
            //    compresssed = comp.To1D();
            //}

            //using (var comp = new GbaNativeCompression(compresssed))
            //{
            //    uncompressed = comp.From1D();
            //}

            //for (int iz = 0; iz < tmp.Data.Length; iz++)
            //{
            //    if (tmp.Data[iz] != uncompressed[iz])
            //    {
            //        throw new Exception("lol");
            //    }
            //}


            //Entry code 
            string videoFile = args[0];

            var b = ((1.0) / 280806);
            var b2 = Convert.ToUInt32(b);
            Console.WriteLine("Decoding video");
            // get wav
            //./ffmpeg -i {videoFile} .\alie.wav

            //if not testing an doing a real run, comment from here to the line that says START HERE
            //if (!Directory.Exists(Processing))
            //{
            //    Directory.CreateDirectory(Processing);
            //}
            //else
            //{
            //    List<string> killme = Directory.GetFiles(Processing).ToList();
            //    foreach (string sz in killme)
            //    {
            //        File.Delete(sz);
            //    }
            //}

            //if (!Directory.Exists(OutputFolder))
            //{
            //    Directory.CreateDirectory(OutputFolder);
            //}
            //else
            //{
            //    List<string> killme = Directory.GetFiles(OutputFolder).ToList();
            //    foreach (string sz in killme)
            //    {
            //        File.Delete(sz);
            //    }
            //}

            uint fr = (ShellFile.FromFilePath(videoFile).Properties.System.Video.FrameRate.Value == null ? 0 : ShellFile.FromFilePath(videoFile).Properties.System.Video.FrameRate.Value.Value) / 1000;
            int targetFps = 15;

            //var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {videoFile} -filter:v fps=fps={targetFps} {Processing}\\{videoFile}" };

            //var P = Process.Start(PSI);

            //P.WaitForExit();

            float fps = (float)((float)fr / (float)targetFps);
            if (fps > 2.0 || fps < 0.5)
            {
                Console.WriteLine("Range is bad, clamping value");
                if (fps > 2.0f) fps = 2.0f;
                if (fps < 0.5f) fps = 0.5f;

            }
            //PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\{videoFile} -af atempo={fps} {Processing}\\alie.wav" };
            //P = Process.Start(PSI);
            //P.WaitForExit();

            //PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\{videoFile} -s 240x160 {Processing}/tmp%03d.png" };
            //P = Process.Start(PSI);
            //P.WaitForExit();

            //////FInal file
            ////// ffmpeg - i input.mp4 output.mp4

            //////PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\{videoFile} {Processing}\\alie.mpg" };
            //////P = Process.Start(PSI);
            //////P.WaitForExit();

            //////PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\alie.mpg -s 240x160 -c:v mpeg1video -c:a mp2 -format mpeg -r 20 -ac 1 -b:a 32000 {OutputFolder}\\alie.mpg" };
            //////P = Process.Start(PSI);
            //////P.WaitForExit();

          
            //List<string> images = Directory.GetFiles(Processing).ToList().Where(x => x.Contains("png")).ToList();
            //images = images.OrderBy(x => Convert.ToInt32(new FileInfo(x).Name.Replace(".png", "").Replace("tmp", ""))).ToList();

            //GritSharp.GritSharp s = new GritSharp.GritSharp();

            //for (int a = 0; a < images.Count; a++)
            //{
            //    string hey = images[a];
            //    // var t = new Thread(//
            //    //     () =>
            //    //{
            //    //no we try now


            //    IntPtr gritRect = s.Export(hey);
            //    byte[] tmp2 = s.GetData();


            //    string rawName = $"Frame_{a}";

            //    try
            //    {
            //        byte[] data = tmp2;
            //        File.WriteAllBytes($"{Processing}\\{rawName}.dhbin", tmp2);
            //        //insert gfx                    

            //        // FrameOps.CompressFile2(ref data, rawName, OutputFolder);
            //        //GC.Collect();
            //        //if (a % 1000 == 0)
            //        //{
            //        //    lock (lockobj)
            //        //    {
            //        //        Program.FramesInclude.Add($"#include \"FrameSet{a / 2}.h\"");
            //        //        ROM.Write(OutputFolder, $"FrameSet{a / 2}");
            //        //    }
            //        //}

            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e);
            //    }
            //    Console.WriteLine($"Processed {hey}");


            //}
            //START HERE

            List<string> imgbins = Directory.GetFiles(Processing).ToList().Where(x => x.Contains("dhbin")).ToList();
            imgbins = imgbins.OrderBy(x => Convert.ToInt32(new FileInfo(x).Name.Replace(".dhbin", "").Replace("Frame_", ""))).ToList();

            //We can now do stuff here.

            //files are short 240x160. 

            var vi = new VideoInfo();


            //for (int a = 0; a < imgbins.Count; a++)
            //{
                string hey = imgbins[7];

                try
                {
                    byte[] bytes = File.ReadAllBytes(hey);
                    vi.AddFile(hey, new Container(7, bytes));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

         //   }

            //How many similar files.

            //vi.CountSimilarFiles();
            vi.Compress1();
            //   vi.Compress2();

            vi.GenerateBinary();
            RenderAudio($"{Processing}\\{(new FileInfo(videoFile)).Name}.wav");
            GC.Collect();
            return;
           

            //Below needs to be recreated
            string asm = AssembleVideoCCode(targetFps.ToString());///; ; AssembleVideoRom();

            File.WriteAllText($"{OutputFolder}//videoframes.c", asm);
            Console.WriteLine("Finished video");


        }
    }
}
