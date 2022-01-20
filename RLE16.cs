using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class RLE16
    {
        enum Commands
        {
            Raw = 0,
            RLE16 = 1,
            Pointer = 2,
        }

        Dictionary<string, long> offsets = new Dictionary<string, long>();
        const int MAXBYTES = 32;
        /// <summary>
        /// Constant buffer
        /// </summary>
        private List<ushort> dat = new List<ushort>();

        /// <summary>
        /// Output stream
        /// </summary>
        public IOStream _outPut = new IOStream(0);

        public byte[] GetData() { return _outPut.Data; }

        /// <summary>
        /// Encodes RLE
        /// </summary>
        /// <param name="isrle"></param>
        private void WriteRle(bool isrle)
        {
            while (dat.Count != 0)
            {
                byte max = (byte)(dat.Count > 255 ? 255 : dat.Count);
                if (max > 127)
                {
                    throw new Exception("hell");
                }

                byte hdr = (byte)((isrle ? 0x80 : 0));

                _outPut.Write8((byte)((byte)hdr | ((byte)max & 0xFE)));


                var writeRange = dat.GetRange(0, max);

                if (!isrle)
                {
                    foreach (ushort u in writeRange)
                    {
                        _outPut.Write16(u);
                    }
                }
                else
                {
                    _outPut.Write16(writeRange.First());
                }
                if (max != _outPut.Length)
                {
                    throw new Exception("probably end of buffer");
                }
                dat.RemoveRange(0, max);
            }
            if (dat.Count > 0) throw new Exception("Bad math");
        }

        public RLE16(IntPtr src, int len, bool compressed)
        {
            if (compressed)
            {
                Compress(src, len);
            }
            if (!compressed)
            {
                Decompress(src, len);
            }
        }

        Commands FromCommand(byte s)
        {
            return (Commands)s;
        }
        byte ToCommand(Commands c)
        {
            return (byte)c;
        }
        public void ProcessData(bool isRle)
        {
            //we do two things
            if (dat.Count == 0) return;
            //first md5
            string CheckSum = "";
            byte[] btes = new byte[dat.Count * 2];
            Buffer.BlockCopy(dat.ToArray(), 0, btes, 0, dat.Count * 2);
            using (var md5Instance = MD5.Create())
            {
                using (var stream = new MemoryStream(btes))
                {
                    var hashResult = md5Instance.ComputeHash(stream);

                    CheckSum = BitConverter.ToString(hashResult).Replace("-", "").ToLowerInvariant();
                }
            }


            //see if check sum exists

            //if (offsets.ContainsKey(CheckSum))
            //{
            //    //we can leave early! 
            //    _outPut.Write8((byte)ToCommand(Commands.Pointer));
            //    _outPut.Write8(0xFF);
            //    _outPut.Write32((int)offsets[CheckSum]);
            //    dat = new List<ushort>();
            //    return;
            //}

            //offsets[CheckSum] = _outPut.Position;  //points to command

            byte hdr = (byte)(isRle ? ToCommand(Commands.RLE16) : ToCommand(Commands.Raw));

            while (dat.Count != 0)
            {
                byte currentDump = 0;
                if (dat.Count > 254)
                {
                    currentDump = 254;
                }
                else
                {
                    currentDump = (byte)dat.Count;
                }
                //Get our valid range.
                var currentRange = dat.GetRange(0, currentDump);
                dat.RemoveRange(0, currentDump);
                _outPut.Write8((byte)(hdr));
                _outPut.Write8(currentDump);
                if (isRle)
                {
                    _outPut.Write16(currentRange[0]);
                }
                else
                {
                    currentRange.ForEach(x => _outPut.Write16(x));
                }
            }
            dat = new List<ushort>();
        }
        public void Compress(IntPtr src, int len)
        {
            int processsed = 0;
            //start it all
            IntPtr srcp = src;
            bool wasRle = false;

            while (processsed < len)
            {
                ushort cur = ((ushort)Marshal.ReadInt16(srcp)); srcp += 2; processsed += 2;

                bool isRle = false;
                if (dat.Count == 0)
                {
                    //we can't decide lol
                    dat.Add(cur);
                    continue;//Process next
                }

                if (dat.Last() == cur)
                {
                    if (!wasRle)
                    {
                        //We were not RLE and now we are. 
                        dat.RemoveAt(dat.Count - 1);
                        ProcessData(wasRle);//Dump the non RLE data
                        dat.Add(cur);
                    }
                    isRle = true;
                }


                if (dat.Count > 254)
                {
                    ProcessData(isRle);
                }
                dat.Add(cur);
                wasRle = isRle;
            }
            if (dat.Count != 0) ProcessData(wasRle);
        }

        private int SingleDecompress(IntPtr src)
        {
            int processed = 0;
            IntPtr srcp = src;
            byte hdr = Marshal.ReadByte(srcp); srcp += 1;
            int maxSize = Marshal.ReadByte(srcp); srcp += 1;
            if (maxSize != 0)
            {
                if (hdr == (byte)Commands.Raw)
                {
                    for (int i = 0; i < maxSize; i++)
                    {
                        ushort cur = (ushort)Marshal.ReadInt16(srcp); srcp += 2; processed += 2;
                        //read, write
                        _outPut.Write16(cur);
                    }
                }
                else if (hdr == (byte)Commands.RLE16)
                {
                    //read
                    ushort cmd = (ushort)Marshal.ReadInt16(srcp); srcp += 2; processed += 2;

                    //write
                    for (int i = 0; i < maxSize; i++)
                    {
                        _outPut.Write16(cmd);
                    }
                }
                else if (hdr == (byte)Commands.Pointer)
                {

                    int pointer = Marshal.ReadInt32(srcp); srcp += 4; processed += 4;
                    IntPtr lookup = src + pointer;
                    SingleDecompress(lookup);
                }
                else
                {
                    throw new Exception("unsupported command");
                }
            }
            return processed;
        }

        private void Decompress(IntPtr src, int len)
        {
            IntPtr srcp = src;
            //Hello!
            int processed = 0;

            long length = srcp.ToInt64() - src.ToInt64();
            while (length < len)
            {
                byte hdr = Marshal.ReadByte(srcp); srcp += 1; processed += 1;
                int maxSize = Marshal.ReadByte(srcp); srcp += 1; processed += 1;

                if (hdr == (byte)Commands.Raw)
                {
                    for (int i = 0; i < maxSize; i++)
                    {
                        _outPut.Write16((ushort)Marshal.ReadInt16(srcp)); srcp += 2; processed += 2;
                    }
                }

                if (hdr == (byte)Commands.RLE16)
                {
                    ushort cmd = (ushort)Marshal.ReadInt16(srcp); srcp += 2; processed += 2;
                    for (int i = 0; i < maxSize; i++)
                    {
                        _outPut.Write16(cmd);
                    }
                }

                if (hdr == (byte)Commands.Pointer)
                {
                    //oh fuck yeah
                    int pointer = Marshal.ReadInt32(srcp); srcp += 4; processed += 4;
                    IntPtr lookup = src + pointer;
                    SingleDecompress(lookup);
                }
                length = srcp.ToInt64() - src.ToInt64();
            }
        }

        private void OldDecompress(IntPtr src, int len)
        {
            IntPtr srcp = src;
            //Hello!
            int processed = 0;
            while (processed + 2 < len)
            {
                byte hdr = Marshal.ReadByte(srcp); srcp += 1; processed += 1;

                bool rleFlag = (hdr == 1 << 7); //Comp flag

                byte count = Marshal.ReadByte(srcp); srcp += 1; processed += 1;


                if (rleFlag)
                {
                    ushort val = (ushort)Marshal.ReadInt16(srcp); srcp += 2; processed += 2;
                    for (int i = 0; i < count; i++)
                    {
                        _outPut.Write16(val); ;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        ushort val = (ushort)Marshal.ReadInt16(srcp); srcp += 2; processed += 2;
                        _outPut.Write16(val);
                    }
                }
            }
        }

        public void OldCompress(IntPtr src, int len)
        {
            int processsed = 0;
            //start it all
            IntPtr srcp = src;
            bool isrle = false;
            dat.Add((ushort)Marshal.ReadInt16(srcp)); srcp += 2; processsed += 2; //init 

            while (processsed < len)
            {
                ushort cur = ((ushort)Marshal.ReadInt16(srcp)); srcp += 2; processsed += 2;
                bool newRle = cur == dat.Last();
                if (newRle != isrle)
                {
                    if (!isrle)
                    {
                        //It's different man, we're going to RLE, so we can the last result off the stack
                        dat.RemoveAt(dat.Count - 1);
                        if (dat.Count != 0) WriteRle(false);//Flush the not RLE to buffer.
                        dat.Add(cur);//Done!/
                    }
                    else
                    {
                        if (dat.Count != 0) WriteRle(true);//Drop current buffer, we don't need to get off the stack.                       
                    }
                    isrle = newRle;
                }
                else
                {
                    //if no change, just add to current buffer. or flush it.
                    //Do we need to flush the buffer?
                    if (dat.Count > 126)
                    {
                        WriteRle(isrle);
                    }
                }
                dat.Add(cur);

            }
            if (dat.Count != 0) WriteRle(isrle);//Write remainig
        }
    }
}
