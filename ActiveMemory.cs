using System.Collections.Generic;
using System.IO;

namespace Video2Gba
{
    public class ActiveMemory : IOStream
    {
        private const long mbsize = 1000000;
        private long maxFileSize = 4 * mbsize;
        private string file;
        private long maxMemory = 1000000;




        public ActiveMemory(string dstFile, int maxFileSize = 4 * 1000000, int maxMemory = 1000000, bool useExisting = false) : base()
        {
            if (useExisting)
            {
                FileInfo fi = new FileInfo(dstFile);

                if (fi.Length > maxMemory)
                {

                    //read max bytes
                    using (var fil = new FileStream(dstFile, FileMode.Open))
                    {
                        List<byte> l = new List<byte>();
                        for (int i = 0; i < maxMemory; i++) l.Add((byte)fil.ReadByte());
                        //_stream = new IOStream(l.ToArray());
                        Capacity = 0;

                    }
                }
                else
                {
                    //  _stream = new IOStream(File.ReadAllBytes(dstFile));
                }
            }
        }
    }
}
