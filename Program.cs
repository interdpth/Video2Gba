using Microsoft.WindowsAPICodePack.Shell;
using NAudio.Mixer;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private static string Processing = "F:\\Processing";
        private static string OutputFolder = "F:\\Output";
        public static int numFrames = 0;
        private static List<string> fileInfos = new List<string>();
        public static Semaphore sem = new Semaphore(1, 1);

        public static Semaphore sem2 = new Semaphore(1, 1);
        public static List<string> FramesInclude = new List<string>();
        public static StringBuilder FramesTable = new StringBuilder("VidFrame theframes[]={");
        //I miss this.
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





        public string SetupIncludes()
        {
            return "";
        }

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

        static void Main(string[] args)
        {
            int ticks_per_sample = 16777216 / 10512;


            var b = ((1.0) / 280806);
            var b2 = Convert.ToUInt32(b);
            Console.WriteLine("Decoding video");
            // get wav
            //./ffmpeg -i alie.mp4 .\alie.wav
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

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }
            else
            {
                List<string> killme = Directory.GetFiles(OutputFolder).ToList();
                foreach (string sz in killme)
                {
                    File.Delete(sz);
                }
            }

            uint fr = (ShellFile.FromFilePath("alie.mp4").Properties.System.Video.FrameRate.Value == null ? 0 : ShellFile.FromFilePath("alie.mp4").Properties.System.Video.FrameRate.Value.Value) / 1000;
            int targetFps = 20;

            //var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i alie.mp4 -filter:v fps=fps={targetFps} {Processing}\\alie.mp4" };

            //var P = Process.Start(PSI);

            //P.WaitForExit();

            //float fps = (float)((float)fr / (float)targetFps);
            //if (fps > 2.0 || fps < 0.5)
            //{
            //    Console.WriteLine("Range is bad, clamping value");
            //    if (fps > 2.0f) fps = 2.0f;
            //    if (fps < 0.5f) fps = 0.5f;

            //}
            //PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\alie.mp4 -af atempo={fps} {Processing}\\alie.wav" };
            //P = Process.Start(PSI);
            //P.WaitForExit();

            //PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\alie.mp4 -s 160x128 {Processing}/tmp%03d.png" };
            //P = Process.Start(PSI);
            //P.WaitForExit();

            ////FInal file
            ///ffmpeg -i input.mp4 output.mp4
            ///
            //var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\alie.mp4 {Processing}\\alie.mpg" };
            //var P = Process.Start(PSI);
            //P.WaitForExit();

            var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\alie.mpg -s 240x160 -c:v mpeg1video -c:a mp2 -format mpeg -r 20 -ac 1 -b:a 32000 {OutputFolder}\\alie.mpg" };
            var P = Process.Start(PSI);
            P.WaitForExit();

            //Read in the file.

            byte[] video = File.ReadAllBytes($"{OutputFolder}\\alie.mpg");

            //swap endianness
            List<byte> newOrder = new List<byte>();
            for(int i = 0; i<video.Length;i+=4)
            {
                byte[] tmp = new byte[4];
                Array.Copy(video, i, tmp, 0, 4);
                newOrder.AddRange(tmp.Reverse());
            }
            ROM.MakeSource("ALIEtest", newOrder.ToArray(), $"{OutputFolder}");
            ROM.Write($"{OutputFolder}", "Alietest");
            return;
            List<string> images = Directory.GetFiles(Processing).ToList().Where(x => x.Contains("png")).ToList();
           images =  images.OrderBy(x =>    Convert.ToInt32(new FileInfo(x).Name.Replace(".png", "").Replace("tmp",""))).ToList();
                  
       //     var k2 = images.OrderBy(x => Convert.ToInt32(x.Replace($"{Processing}\\tmp", "").Replace(".png", ""))).ToList();
            List<Thread> conversions = new List<Thread>();
            List<Thread> bgconversions = new List<Thread>();
            int dumblock = 0;
            GritSharp.GritSharp s = new GritSharp.GritSharp();


            //So what we're going to is pretty simple. 
            //GEt all the data
           
            for (int a = 0; a < images.Count; a++)
            {
                string hey = images[a];
                // var t = new Thread(//
                //     () =>
                //{
                //no we try now


                IntPtr gritRect = s.Export(hey);
                byte[] tmp = s.GetData();



                


                //PSI = new ProcessStartInfo { RedirectStandardError = true, RedirectStandardOutput = true, FileName = "grit.exe", UseShellExecute = false, Arguments = $"{hey} -gb -gB 16 -ftbin" };
                ////creates tmp001.img.bin
                //P = Process.Start(PSI);
                //P.WaitForExit();
                //FileInfo deets = new FileInfo(hey);
                string rawName = $"Frame_{a}";
                ////  var t2 = new Thread(
                ////() =>
                //{
                try
                {
                    byte[] data = tmp;

                    ///used to write size Program.FramesTable.AppendLine("{" + rawName + "," + data.Length + "},");

                    lock (lockobj)
                    {
                        Program.FramesTable.AppendLine("{" + rawName + "},");
                    }
                    //insert gfx                    

                    FrameOps.CompressFile2(ref data, rawName, OutputFolder);
                    GC.Collect();
                    if (a % 1000 == 0)
                    {
                        lock (lockobj)
                        {
                            Program.FramesInclude.Add($"#include \"FrameSet{a / 2}.h\"");
                            ROM.Write(OutputFolder, $"FrameSet{a / 2}");
                        }
                    }
                    lock (lockobj)
                    {
                        Program.numFrames++;
                    }
                }
                catch (Exception e)
                {

                }
                Console.WriteLine($"Processed {hey}");


            }
            Program.FramesInclude.Add($"#include \"FrameSetFinal.h\"");
            ROM.Write(OutputFolder, $"FrameSetFinal");
            //while (conversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 0) ;
            Console.WriteLine("Encoding video");
            if (File.Exists($"{OutputFolder}//videoframes.c")) { File.Delete($"{OutputFolder}//videoframes.c"); }
            //Wait for the others to be done.

            //while (true)
            //{
            //    Program.sem.WaitOne();
            //    if (bgconversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) == 0)
            //    {
            //        Program.sem.Release(); break;
            //    }
            //    else
            //    {
            //        Program.sem.Release();
            //    }
            //    Thread.Sleep(1000);
            //}
            RenderAudio($"{Processing}\\Alie.wav");
            string asm = AssembleVideoCCode(targetFps.ToString());///; ; AssembleVideoRom();

            File.WriteAllText($"{OutputFolder}//videoframes.c", asm);
            Console.WriteLine("Finished video");
            //./ffmpeg 
            //get all files
            //256*192*
            Console.WriteLine("Best headers");
            //foreach (var k in FrameOps.k)
            //{
            //    Console.WriteLine($"{k.Key.ToString("X")} - {k.Value}");
            //}
            Console.WriteLine("Observe");
        }
    }
}
