﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class IOStream : Stream
    {
        private byte[] data;

        private int pos;

        private int length;

        public byte[] Data => data;

        public override long Position { get { return pos; } set { pos = (int)value; } }

        public override long Length => length;

        public int Capacity
        {
            get
            {
                return data.Length;
            }
            set
            {
                Array.Resize(ref data, value);
            }
        }

        public override bool CanRead
        {
            get
            {
               return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        //public override long Length
        //{
        //    get
        //    {
        //        return (long)length;
        //    }
        //}

        //public override long Position
        //{
        //    get
        //    {
        //        return (int)Position;
        //    }

        //    set
        //    {
        //        Position = (int)value;
        //    }
        //}

        public IOStream(int capacity = 4)
        {
            data = new byte[capacity];
            pos = 0;
            length = 0;
        }

        public IOStream(byte[] data)
        {
            this.data = data;
            pos = 0;
            length = data.Length;
        }

        public void Seek(int offset)
        {
            pos = offset;
        }

        public void Align(int remainder = 4)
        {
            while (pos % remainder != 0)
            {
                pos++;
            }
        }

        public byte Read8()
        {
            return data[pos++];
        }

        public ushort Read16()
        {
            if (pos % 2 != 0)
            {
                pos++;
            }
            ushort result;
            result = (ushort)(data[pos] | (data[pos + 1] << 8));
            pos += 2;
            return result;
        }

        public int Read32()
        {
            int num;
            num = pos % 4;
            if (num != 0)
            {
                pos += 4 - num;
            }
            int result;
            result = (data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return result;
        }

        public int ReadPtr()
        {
            int num;
            num = pos % 4;
            if (num != 0)
            {
                pos += 4 - num;
            }
            int result;
            result = (data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] - 8 << 24));
            pos += 4;
            return result;
        }

        public string ReadASCII(int len)
        {
            byte[] array;
            array = new byte[len];
            Array.Copy(data, pos, array, 0, len);
            string @string;
            @string = Encoding.ASCII.GetString(array);
            pos += len;
            return @string;
        }
        public void Write24(int value)
        {

            Write32(value);
            //Write8((byte)(value >> 16));
            //Write8((byte)(value >> 8));
            //Write8((byte)(value));
        }
        public void Write8(byte val)
        {
            if (pos >= length)
            {
                length = pos + 1;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            data[pos++] = val;
        }

        public void Write16(ushort val)
        {
            if (pos % 2 != 0)
            {
                Write8(0);
            }
            if (pos + 2 > length)
            {
                length = pos + 2;
                if (length > Capacity)
                {
                    Capacity=pos + 2;
                }
            }
            data[pos] = (byte)val;
            data[pos + 1] = (byte)(val >> 8);
            pos += 2;
        }

        public void Write32(int val)
        {
            while (pos % 4 != 0)
            {
                Write8(0);
            }
            if (pos + 4 > length)
            {
                length = pos + 4;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            data[pos] = (byte)val;
            data[pos + 1] = (byte)(val >> 8);
            data[pos + 2] = (byte)(val >> 16);
            data[pos + 3] = (byte)(val >> 24);
            pos += 4;
        }

        public void WriteU32(UInt32 val)
        {
            while (pos % 4 != 0)
            {
                Write8(0);
            }
            if (pos + 4 > length)
            {
                length = pos + 4;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            data[pos] = (byte)val;
            data[pos + 1] = (byte)(val >> 8);
            data[pos + 2] = (byte)(val >> 16);
            data[pos + 3] = (byte)(val >> 24);
            pos += 4;
        }
        public void WritePtr(int val)
        {
            while (pos % 4 != 0)
            {
                Write8(0);
            }
            if (pos + 4 > length)
            {
                length = pos + 4;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            data[pos] = (byte)val;
            data[pos + 1] = (byte)(val >> 8);
            data[pos + 2] = (byte)(val >> 16);
            data[pos + 3] = (byte)((val >> 24) + 8);
            pos += 4;
        }

        public void WriteASCII(string str)
        {
            byte[] bytes;
            bytes = Encoding.ASCII.GetBytes(str);
            int num;
            num = bytes.Length;
            if (pos + num > length)
            {
                length = pos + num;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            Array.Copy(bytes, 0, data, pos, num);
            pos += num;
        }

        public byte Read8(int offset)
        {
            return data[offset];
        }

        public ushort Read16(int offset)
        {
            return (ushort)(BitConverter.ToInt16(data,offset));
        }

        public int Read32(int offset)
        {
            return (ushort)(BitConverter.ToInt32(data, offset));
        }

        public int ReadPtr(int offset)
        {
            return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] - 8 << 24);
        }

        public string ReadASCII(int offset, int len)
        {
            byte[] array;
            array = new byte[len];
            Array.Copy(data, offset, array, 0, len);
            return Encoding.ASCII.GetString(array);
        }

        public void Write8(int offset, byte val)
        {
            data[offset] = val;
        }

        public void Write16(int offset, ushort val)
        {
          //  data[offset] = (byte)val;
          //  data[offset + 1] = (byte)(val >> 8);

            Array.Copy(BitConverter.GetBytes(val), 0, data, offset, 2);
        }

        public void Write32(int offset, int val)
        {
            //data[offset] = (byte)val;
            //data[offset + 1] = (byte)(val >> 8);
            //data[offset + 2] = (byte)(val >> 16);
            //data[offset + 3] = (byte)(val >> 24);

            Array.Copy(BitConverter.GetBytes(val), 0, data, offset, 4);
        }

        public void WritePtr(int offset, int val)
        {
            data[offset] = (byte)val;
            data[offset + 1] = (byte)(val >> 8);
            data[offset + 2] = (byte)(val >> 16);
            data[offset + 3] = (byte)((val >> 24) + 8);
        }

        public void WriteASCII(int offset, string str)
        {
            byte[] bytes;
            bytes = Encoding.ASCII.GetBytes(str);
            Array.Copy(bytes, 0, data, offset, bytes.Length);
        }

        public void CopyToArray(int srcOffset, Array dstData, int dstOffset, int len)
        {
            Buffer.BlockCopy(data, srcOffset, dstData, dstOffset, len);
        }
        
        public void CopyFromArray(Array srcData, int srcOffset, int dstOffset, int len)
        {
          
            Buffer.BlockCopy(srcData, srcOffset, data, dstOffset, len);
        }
        
        public void CopyFromArray(Array srcData, int len)
        {
            if (pos + len > data.Length)
            {
                Capacity = pos + len;
            }
            Buffer.BlockCopy(srcData, 0, data, pos, len);
        
        }

        public void OverlappingCopy(int amount, int window)
        {
            if (pos + amount > length)
            {
                length = pos + amount;
                if (length > Capacity)
                {
                    Capacity = length;
                }
            }
            for (int i = 0; i < amount; i++)
            {
                data[pos] = data[pos - window];
                pos++;
            }
        }

        public List<int> GetPointers(int offset)
        {
            byte b;
            b = (byte)offset;
            byte b2;
            b2 = (byte)(offset >> 8);
            byte b3;
            b3 = (byte)(offset >> 16);
            byte b4;
            b4 = (byte)((offset >> 24) + 8);
            List<int> list;
            list = new List<int>();
            for (int i = 0; i < length; i += 4)
            {
                if (data[i] == b && data[i + 1] == b2 && data[i + 2] == b3 && data[i + 3] == b4)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        private int FindEndOfData()
        {
            int endOfData;
           
                endOfData = length;
                FindEndOfRun(ref endOfData);
            
           
            return endOfData;
        }

        private void FindEndOfRun(ref int endOfData)
        {
            while (endOfData > 0)
            {
                endOfData--;
                if (data[endOfData] != byte.MaxValue)
                {
                    break;
                }
            }
            endOfData++;
        }

        private int CheckFreeSpaceAfter(int offset)
        {
            int num;
            num = offset;
            while (offset % 4 != 0)
            {
                if (data[offset++] != 0)
                {
                    return num;
                }
            }
            string a;
            a = ReadASCII(offset, 4);
            if (a == "mage")
            {
                ushort num2;
                num2 = Read16(offset + 4);
                for (int i = num; i < offset + 6; i++)
                {
                    data[i] = byte.MaxValue;
                }
                return offset + num2;
            }
            return num;
        }

        private void MarkFreeSpace(int addr, int prevLen, int newLen)
        {
            int num;
            num = FindEndOfData();
            int num2;
            num2 = addr + prevLen;
            bool flag;
            flag = true;
            if (num2 + 4 >= num)
            {
                while (num2 < num)
                {
                    if (data[num2++] != 0)
                    {
                        flag = false;
                        break;
                    }
                }
            }
            else
            {
                flag = false;
            }
            int num3;
            num3 = addr + newLen;
            if (flag)
            {
                if (data[num3 - 1] == byte.MaxValue)
                {
                    data[num3++] = 0;
                }
                while (num3 % 4 != 0)
                {
                    data[num3++] = 0;
                }
                while (num3 < num)
                {
                    data[num3++] = byte.MaxValue;
                }
                return;
            }
            int num4;
            num4 = CheckFreeSpaceAfter(addr + prevLen);
            if (num3 < num4)
            {
                if (data[num3 - 1] == byte.MaxValue)
                {
                    data[num3++] = 0;
                }
                while (num3 % 4 != 0 && num3 < num4)
                {
                    data[num3++] = 0;
                }
            }
            int num5;
            num5 = num3;
            while (num3 < num4)
            {
                data[num3++] = byte.MaxValue;
            }
            int num6;
            num6 = num4 - num5;
            if (num6 >= 6)
            {
                Seek(num5);
                WriteASCII("mage");
                Write16((ushort)num6);
            }
        }

        private bool AllocateSpace(ref int offset, int newLen)
        {
            int endOfData;
            endOfData = FindEndOfData();
            for (int i = 655360; i < endOfData; i += 4)
            {
                if (data[i] != 109 || data[i + 1] != 97 || data[i + 2] != 103 || data[i + 3] != 101)
                {
                    continue;
                }
                int num;
                num = Read16(i + 4);
                if (num < newLen)
                {
                    continue;
                }
                bool flag;
                flag = true;
                for (int j = 6; j < num; j++)
                {
                    if (data[i + j] != byte.MaxValue)
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                {
                    continue;
                }
                int k;
                for (k = i + newLen; k % 4 != 0; k++)
                {
                }
                int num2;
                num2 = i + num - k;
                if (num2 >= 6)
                {
                    Seek(i + newLen);
                    while (pos % 4 != 0)
                    {
                        data[pos++] = 0;
                    }
                    WriteASCII("mage");
                    Write16((ushort)num2);
                }
                offset = i;
                return false;
            }
           
                endOfData = Capacity;
                FindEndOfRun(ref endOfData);
            
            offset = endOfData;
            return true;
        }

        //public void Write2(ByteStream src, int origLen, ref int offset, bool fixPtrs)
        //{
        //    int num;
        //    num = src.length;
        //    if (num <= origLen)
        //    {
        //        Array.Copy(src.data, 0, data, offset, num);
        //        MarkFreeSpace(offset, origLen, num);
        //        return;
        //    }
        //    MarkFreeSpace(offset, origLen, 0);
        //    int offset2;
        //    offset2 = offset;
        //    bool flag;
        //    flag = AllocateSpace(ref offset, num);
        //    Array.Copy(src.data, 0, data, offset, num);
        //    if (flag)
        //    {
        //        int num2;
        //        num2 = offset + num;
        //        if (data[num2 - 1] == byte.MaxValue)
        //        {
        //            data[num2++] = 0;
        //        }
        //        while (num2 % 4 != 0)
        //        {
        //            data[num2++] = 0;
        //        }
        //    }
        //    if (fixPtrs)
        //    {
        //        List<int> pointers;
        //        pointers = GetPointers(offset2);
        //        foreach (int item in pointers)
        //        {
        //            WritePtr(item, offset);
        //        }
        //    }
        //}

        //public void Write(ByteStream src, int origLen, int ptr, bool shared)
        //{
        //    int offset;
        //    offset = ReadPtr(ptr);
        //    List<int> pointers;
        //    pointers = GetPointers(offset);
        //    int num;
        //    num = src.length;
        //    bool flag;
        //    flag = (num > origLen);
        //    bool flag2;
        //    flag2 = (pointers.Count == 1);
        //    if (!flag && (flag2 || shared))
        //    {
        //        Array.Copy(src.data, 0, data, offset, num);
        //        MarkFreeSpace(offset, origLen, num);
        //        return;
        //    }
        //    if (flag && (flag2 || shared))
        //    {
        //        MarkFreeSpace(offset, origLen, 0);
        //    }
        //    bool flag3;
        //    flag3 = AllocateSpace(ref offset, num);
        //    Array.Copy(src.data, 0, data, offset, num);
        //    if (flag3)
        //    {
        //        int num2;
        //        num2 = offset + num;
        //        if (data[num2 - 1] == byte.MaxValue)
        //        {
        //            data[num2++] = 0;
        //        }
        //        while (num2 % 4 != 0)
        //        {
        //            data[num2++] = 0;
        //        }
        //    }
        //    if (shared)
        //    {
        //        foreach (int item in pointers)
        //        {
        //            WritePtr(item, offset);
        //        }
        //    }
        //    else
        //    {
        //        WritePtr(ptr, offset);
        //    }
        //}

        //public void Write(ByteStream src)
        //{
        //    int num;
        //    num = src.length;
        //    if (pos + num > length)
        //    {
        //        length = pos + num;
        //        if (length > Capacity)
        //        {
        //            Resize();
        //        }
        //    }
        //    Array.Copy(src.data, 0, data, pos, num);
        //    pos += num;
        //}

        //public int AddData(ByteStream src)
        //{
        //    int num;
        //    num = src.length;
        //    int offset;
        //    offset = 0;
        //    bool flag;
        //    flag = AllocateSpace(ref offset, num);
        //    Array.Copy(src.data, 0, data, offset, num);
        //    if (flag)
        //    {
        //        int num2;
        //        num2 = offset + num;
        //        if (data[num2 - 1] == byte.MaxValue)
        //        {
        //            data[num2++] = 0;
        //        }
        //        while (num2 % 4 != 0)
        //        {
        //            data[num2++] = 0;
        //        }
        //    }
        //    return offset;
        //}

        public void Export(string outputFile)
        {
            BinaryWriter binaryWriter;
            binaryWriter = new BinaryWriter(File.Open(outputFile, FileMode.Create));
            binaryWriter.Write(data, 0, length);
            binaryWriter.Close();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            pos = (int)offset;
            return pos;
        }

        public override void SetLength(long value)
        {
            Capacity = (int)value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Buffer.BlockCopy(buffer, 0, data, offset, count);
            return 0;
        }



        public void WriteData(Array buffer,  int elSize, int count)
        {

            byte[] newBuffer = new byte[buffer.Length * elSize];
            Array.Copy(buffer, newBuffer, buffer.Length* elSize);
         
            for (int i = 0; i < buffer.Length * elSize; i++)
            {
                Write8(newBuffer[i]);
            }

    

        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int oldPos = (int)Position;
            if (count + offset > Capacity)
            {
                Capacity = count + offset;
            }
            pos = offset;
            
            for(int i = 0; i<count; i++)
            {
                Write8(buffer[i]);
            }

            pos = oldPos;
       
        }
    }

}
