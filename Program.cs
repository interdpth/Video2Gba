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
      

        public static Semaphore sem = new Semaphore(1, 1);
       

        List<Frame> frames;
        

        public static string AssembleVideoRom()
        {
            StringBuilder assembly = new StringBuilder();
            assembly.Append(ROM.baseAssembly);
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
            int ticks_per_sample = 16777216 / 10512;

            for (int i= 0; i<10;i++)
            {
                //sample = 10512;
                int norm = (int)(i * ticks_per_sample * (1.0 / 280806)); ;
                int nonorm =(int) (i * ticks_per_sample * 1.0) / 280806; 

                if(norm!=nonorm)
                {
                    Console.Write("lol");
                }
            }
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


            //Re-encode.
            var PSI = new ProcessStartInfo { FileName = "ffmpeg.exe", UseShellExecute = true, CreateNoWindow = true, Arguments = $"-i lztown.mp4 -filter:v fps=fps=1 {Processing}\\lztown.mp4" };
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
                         VideoCompression.Compress(data, OutputFolder + "//" + rawName + ".img.bin");

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
