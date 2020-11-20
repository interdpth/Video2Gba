using Microsoft.WindowsAPICodePack.Shell;
using NAudio.Mixer;
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

        private static List<string> fileInfos = new List<string>();
        public static Semaphore sem = new Semaphore(1, 1);

        public static Semaphore sem2 = new Semaphore(1, 1);
        public static List<string> FramesInclude = new List<string>();
        public static StringBuilder FramesTable = new StringBuilder("VidFrame theframes[]={");
        //I miss this.
        public static string AssembleVideoCCode()
        {
            StringBuilder assembly = new StringBuilder();

            var files = Directory.GetFiles(OutputFolder);
            //Convert audio
       

          
            Program.FramesTable.AppendLine("};");

            assembly.AppendLine(FramesInclude.ToString());
            assembly.AppendLine("//Frames don't have a type.");
            assembly.AppendLine("typedef struct");
            assembly.AppendLine("{");
            assembly.AppendLine("    unsigned long* fra;");
            assembly.AppendLine("    unsigned long size");
            assembly.AppendLine("} VidFrame;");
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



                    ROM.MakeSource(fn, data.ToArray(), "Output");               
                }
            }
        }


        static void Main(string[] args)
        {
            int ticks_per_sample = 16777216 / 10512;


            var b = ((1.0) / 280806);
            var b2 = Convert.ToUInt32(b);
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
          
           uint fr=  (ShellFile.FromFilePath("lztown.mp4").Properties.System.Video.FrameRate.Value==null?0: ShellFile.FromFilePath("lztown.mp4").Properties.System.Video.FrameRate.Value.Value) / 1000;
            int targetFps = 11;
            //Re-encode.
            var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i lztown.mp4 -filter:v fps=fps={targetFps} {Processing}\\lztown.mp4" };
            var P = Process.Start(PSI);
            P.WaitForExit();

            float fps = (float)((float)fr / (float)targetFps);

            PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i {Processing}\\lztown.mp4 -af atempo={fps} {Processing}\\lztown.wav" };
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
                         Program.FramesInclude.Add($"#include \"{rawName}.h\"");
                         Program.FramesTable.AppendLine("{" + rawName + "," + data.Length + "},");

                         File.Delete(rawName + ".img.bin");
                         //insert gfx                    
                         VideoCompression.CompressFile2(data, rawName, OutputFolder);                         
                                             
                     }
                     catch (Exception e)
                     {

                     }
                     Console.WriteLine($"Processed {hey}");
                 });

                   Program.sem.WaitOne();

                   while (bgconversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 10) ;
                   while (bgconversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) != 0) ;
                   t2.IsBackground = true;
                   t2.Start();
                   bgconversions.Add(t2);
                   Program.sem.Release();

               }
               );

                //Wait until free

                while (conversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 80) ;
                t.IsBackground = true;
                t.Start();
                conversions.Add(t);
                conversions.RemoveAll(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive);
            }

            while (conversions.Count(x => x.ThreadState == System.Threading.ThreadState.Running || x.IsAlive) > 0) ;
            Console.WriteLine("Encoding video");
            if (File.Exists("Output//videoframes.c")) { File.Delete("Output//videoframes.c"); }
            //Wait for the others to be done.

            while (true)
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
            string asm = AssembleVideoCCode();///; ; AssembleVideoRom();
            File.WriteAllText("Output//videoframes.c", asm);
            Console.WriteLine("Finished video");
            //./ffmpeg 
            //get all files
            //256*192*
        }
    }
}
