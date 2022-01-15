using System;
using System.Collections.Generic;
using System.IO;

namespace Video2Gba
{
    public class VideoCompression
    {
        public static int RLECompress(Stream instream, long inLength, Stream outstream)
        {
            instream.Position = 0;
            if (inLength > 0xFFFFFF)
                throw new Exception(" Input too large");

            List<byte> compressedData = new List<byte>();

            // at most 0x7F+3=130 bytes are compressed into a single block.
            // (and at most 0x7F+1=128 in an uncompressed block, however we need to read 2
            // more, since the last byte may be part of a repetition).
            byte[] dataBlock = new byte[130];
            // the length of the valid content in the current data block
            int currentBlockLength = 0;

            int readLength = 0;
            int nextByte;
            int repCount = 1;
            while (readLength < inLength)
            {
                bool foundRepetition = false;

                while (currentBlockLength < dataBlock.Length && readLength < inLength)
                {
                    nextByte = instream.ReadByte();
                    if (nextByte < 0)
                        throw new Exception(" Stream too small");
                    readLength++;

                    dataBlock[currentBlockLength++] = (byte)nextByte;
                    if (currentBlockLength > 1)
                    {
                        if (nextByte == dataBlock[currentBlockLength - 2])
                            repCount++;
                        else
                            repCount = 1;
                    }

                    foundRepetition = repCount > 2;
                    if (foundRepetition)
                        break;
                }


                int numUncompToCopy = 0;
                if (foundRepetition)
                {
                    // if a repetition was found, copy block size - 3 bytes as compressed data
                    numUncompToCopy = currentBlockLength - 3;
                }
                else
                {
                    // if no repetition was found, copy min(block size, max block size - 2) bytes as uncompressed data.
                    numUncompToCopy = Math.Min(currentBlockLength, dataBlock.Length - 2);
                }

                #region insert uncompressed block
                if (numUncompToCopy > 0)
                {
                    byte flag = (byte)(numUncompToCopy - 1);
                    compressedData.Add(flag);
                    for (int i = 0; i < numUncompToCopy; i++)
                        compressedData.Add(dataBlock[i]);
                    // shift some possibly remaining bytes to the start
                    for (int i = numUncompToCopy; i < currentBlockLength; i++)
                        dataBlock[i - numUncompToCopy] = dataBlock[i];
                    currentBlockLength -= numUncompToCopy;
                }
                #endregion

                if (foundRepetition)
                {
                    // if a repetition was found, continue until the first different byte
                    // (or until the buffer is full)
                    while (currentBlockLength < dataBlock.Length && readLength < inLength)
                    {
                        nextByte = instream.ReadByte();
                        if (nextByte < 0)
                            throw new Exception(" Stream too small");
                        readLength++;

                        dataBlock[currentBlockLength++] = (byte)nextByte;

                        if (nextByte != dataBlock[0])
                            break;
                        else
                            repCount++;
                    }

                    // the next repCount bytes are the same.
                    #region insert compressed block
                    byte flag = (byte)(0x80 | (repCount - 3));
                    compressedData.Add(flag);
                    compressedData.Add(dataBlock[0]);
                    // make sure to shift the possible extra byte to the start
                    if (repCount != currentBlockLength)
                        dataBlock[0] = dataBlock[currentBlockLength - 1];
                    currentBlockLength -= repCount;
                    #endregion
                }
            }

            // write any reamaining bytes as uncompressed
            if (currentBlockLength > 0)
            {
                byte flag = (byte)(currentBlockLength - 1);
                compressedData.Add(flag);
                for (int i = 0; i < currentBlockLength; i++)
                    compressedData.Add(dataBlock[i]);
                currentBlockLength = 0;
            }

            // write the RLE marker and the decompressed size
            outstream.WriteByte(0x30);
            int compLen = compressedData.Count;
            outstream.WriteByte((byte)(inLength & 0xFF));
            outstream.WriteByte((byte)((inLength >> 8) & 0xFF));
            outstream.WriteByte((byte)((inLength >> 16) & 0xFF));

            // write the compressed data
            outstream.Write(compressedData.ToArray(), 0, compLen);

            // the total compressed stream length is the compressed data length + the 4-byte header
            return compLen + 4;
        }


        //private static byte[] HuffmanCompress(byte[] frame)
        //{
        //    return new byte[1];
        //}

        ////byte count is how much u8, u16, 3, u32

        //private static byte[] DescribeCompress(byte[] old, byte[] newFrame)
        //{
        //    //
        //    List<describe> compressedBuf = new List<describe>();
        //    IOStream newStream = new IOStream(newFrame.Length / 4);
        //    newStream.Seek(4);


        //    ////Split into 4.
        //    ////If not size is not aligned 
        //    //int len = buffer.Length / 4;

        //    //for (int i = 0; i < buffer.Length; i++)
        //    //{
        //    //    int realSize = i + len < buffer.Length ? len : buffer.Length - i;
        //    //    byte[] newBuffer = new byte[realSize];
        //    //    Array.Copy(buffer, i, newBuffer, 0, realSize);
        //    //    buffers.Add(Compress(newBuffer, enableCircleComp - 1));
        //    //    i += realSize;
        //    //}



        //    //for (ushort i = 0; i < newFrame.Length / 4; i++)
        //    //{
        //    //    if (old[i] != newFrame[i])
        //    //    {
        //    //        compressedBuf.Add(new describe(i, newFrame[i]));
        //    //        newStream.Write16(i);
        //    //        newStream.Write8(newFrame[i]);
        //    //    }
        //    //}



        //    newStream.Seek(0);
        //    newStream.Write32(compressedBuf.Count - 4);
        //    byte[] returnvalue = new byte[4 + newStream.Length];
        //    BitConverter.GetBytes(CompressionHeaders.DESCRIBEHEADER).CopyTo(returnvalue, 0);
        //    newStream.CopyToArray(0, returnvalue, 8, returnvalue.Length);

        //    return returnvalue;

        //}
        public static byte[] GBatroidRLE(byte[] input)
        {
            IOStream output = new IOStream();
            int position;
            position = (int)output.Position;
            byte[] data;
            int length = input.Length;
            data = input;
            for (int i = 0; i < 2; i++)
            {
                List<byte> list;
                list = new List<byte>();
                List<int> list2;
                list2 = new List<int>();
                byte b;
                b = data[i];
                list.Add(b);
                int num;
                num = 1;
                for (int j = i + 2; j < length; j += 2)
                {
                    byte b2;
                    b2 = data[j];
                    if (b2 == b)
                    {
                        num++;
                        continue;
                    }
                    list.Add(b2);
                    list2.Add(num);
                    b = b2;
                    num = 1;
                }
                list2.Add(num);
                byte[,] array;
                array = new byte[2, length];
                int[] array2;
                array2 = new int[2];
                int[] array3;
                array3 = array2;
                for (int k = 0; k < 2; k++)
                {
                    int num2;
                    num2 = 3 + k;
                    int num3;
                    num3 = 128 << 8 * k;
                    int num4;
                    num4 = num3 - 1;
                    List<byte> list3;
                    list3 = new List<byte>();
                    array[k, array3[k]++] = (byte)(k + 1);
                    for (int l = 0; l < list.Count; l++)
                    {
                        num = list2[l];
                        if (num >= num2)
                        {
                            if (list3.Count > 0)
                            {
                                if (k == 0)
                                {
                                    array[k, array3[k]++] = (byte)list3.Count;
                                }
                                else
                                {
                                    array[k, array3[k]++] = (byte)(list3.Count >> 8);
                                    array[k, array3[k]++] = (byte)list3.Count;
                                }
                                foreach (byte item in list3)
                                {
                                    array[k, array3[k]++] = item;
                                }
                                list3.Clear();
                            }
                            while (num > 0)
                            {
                                int num5;
                                num5 = num3 + Math.Min(num, num4);
                                if (k == 0)
                                {
                                    array[k, array3[k]++] = (byte)num5;
                                }
                                else
                                {
                                    array[k, array3[k]++] = (byte)(num5 >> 8);
                                    array[k, array3[k]++] = (byte)num5;
                                }
                                array[k, array3[k]++] = list[l];
                                num -= num4;
                            }
                            continue;
                        }
                        if (list3.Count + num > num4)
                        {
                            if (k == 0)
                            {
                                array[k, array3[k]++] = (byte)list3.Count;
                            }
                            else
                            {
                                array[k, array3[k]++] = (byte)(list3.Count >> 8);
                                array[k, array3[k]++] = (byte)list3.Count;
                            }
                            foreach (byte item2 in list3)
                            {
                                array[k, array3[k]++] = item2;
                            }
                            list3.Clear();
                        }
                        for (int m = 0; m < num; m++)
                        {
                            list3.Add(list[l]);
                        }
                    }
                    if (list3.Count > 0)
                    {
                        if (k == 0)
                        {
                            array[k, array3[k]++] = (byte)list3.Count;
                        }
                        else
                        {
                            array[k, array3[k]++] = (byte)(list3.Count >> 8);
                            array[k, array3[k]++] = (byte)list3.Count;
                        }
                        foreach (byte item3 in list3)
                        {
                            array[k, array3[k]++] = item3;
                        }
                        list3.Clear();
                    }
                    array[k, array3[k]++] = 0;
                    if (k == 1)
                    {
                        array[k, array3[k]++] = 0;
                    }
                }
                int num6;
                num6 = ((array3[0] > array3[1]) ? 1 : 0);
                int num7;
                num7 = array3[num6];
                for (int n = 0; n < num7; n++)
                {
                    output.Write8(array[num6, n]);
                }
            }



            byte[] returnvalue = new byte[8 + output.Length];
            //BitConverter.GetBytes(CompressionHeaders.RLEHEADER).CopyTo(returnvalue, 0);
            BitConverter.GetBytes(output.Length).CopyTo(returnvalue, 4);
            output.CopyToArray(0, returnvalue, 8, (int)output.Length);

            return output.Data;
        }

        public static byte[] oldFrame = null;
        public static IOStream CompLZ77(byte[] input)
        {

            return new IOStream(LZ77.Compress(input));
        }

        public static bool CompLZ772(IOStream input, int length, ref IOStream output)
        {
            try
            {
                byte[] data;
                data = input.Data;
                Dictionary<int, List<int>> bufferWindow;
                bufferWindow = new Dictionary<int, List<int>>();
                for (int i = 0; i < input.Length - 2; i++)
                {
                    int key;
                    key = (data[i] | (data[i + 1] << 8) | (data[i + 2] << 16));
                    List<int> value;
                    if (bufferWindow.TryGetValue(key, out value))
                    {
                        value.Add(i);
                    }
                    else
                    {
                        bufferWindow.Add(key, new List<int>
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
                position = (int)output.Position;
                output.Write8(16);
                output.Write8((byte)length);
                output.Write8((byte)(length >> 8));
                output.Write8((byte)(length >> 16));
                while (num3 < length)
                {
                    int position2;
                    position2 = (int)output.Position;
                    output.Write8(0);
                    for (int num4 = 0; num4 < 8; num4++)
                    {
                        if (num3 + 3 <= length)
                        {
                            int key2;
                            key2 = (data[num3] | (data[num3 + 1] << 8) | (data[num3 + 2] << 16));
                            List<int> value2;
                            if (bufferWindow.TryGetValue(key2, out value2))
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
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static IOStream CompLZ77(IOStream input, int length)
        {
            IOStream output = new IOStream(0, length);
            byte[] data;
            data = input.Data;
            Dictionary<int, List<int>> bufferWindow;
            bufferWindow = new Dictionary<int, List<int>>();
            for (int i = 0; i < input.Length - 2; i++)
            {
                int key;
                key = (data[i] | (data[i + 1] << 8) | (data[i + 2] << 16));
                List<int> value;
                if (bufferWindow.TryGetValue(key, out value))
                {
                    value.Add(i);
                }
                else
                {
                    bufferWindow.Add(key, new List<int>
                    {
                        i
                    });
                }
            }
            int num = 18;

            int num2 = 4096;

            int num3 = 0;
            int position = (int)output.Position;
            output.Write8(16);
            output.Write8((byte)length);
            output.Write8((byte)(length >> 8));
            output.Write8((byte)(length >> 16));
            while (num3 < length)
            {
                int position2;
                position2 = (int)output.Position;
                output.Write8(0);
                for (int num4 = 0; num4 < 8; num4++)
                {
                    if (num3 + 3 <= length)
                    {
                        int key2;
                        key2 = (data[num3] | (data[num3 + 1] << 8) | (data[num3 + 2] << 16));
                        List<int> value2;
                        if (bufferWindow.TryGetValue(key2, out value2))
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

        //IPS format used.
        enum types { COMPRESSED, DECOMPRESSED };
        public static byte[] GetDifferences(byte[] oldbuffer, byte[] newbuffer)
        {


            for (int i = 0; i < newbuffer.Length;)
            {
                //what do we have heare?
                if (oldbuffer[i] == newbuffer[i])
                {

                }
                i++;
            }

            return new byte[] { 0 };

        }
        //Break frame into 4 sections
        //Run IPS on the frames//ips needs to use gba rl 
        //Run lz after rejoining.


        public static int RleCompress(Stream instream, ref IOStream outstream)
        {
            long inLength = instream.Length;
            if (inLength > 0xFFFFFF)
                throw new Exception("InputTooLargeException.");//InputTooLargeException();

            List<byte> compressedData = new List<byte>();

            // at most 0x7F+3=130 bytes are compressed into a single block.
            // (and at most 0x7F+1=128 in an uncompressed block, however we need to read 2
            // more, since the last byte may be part of a repetition).
            byte[] dataBlock = new byte[130];
            // the length of the valid content in the current data block
            int currentBlockLength = 0;

            int readLength = 0;
            int nextByte;
            int repCount = 1;
            while (readLength < inLength)
            {
                bool foundRepetition = false;

                while (currentBlockLength < dataBlock.Length && readLength < inLength)
                {
                    nextByte = instream.ReadByte();
                    if (nextByte < 0)
                        throw new Exception("Stream too short.");//StreamTooShortException();
                    readLength++;

                    dataBlock[currentBlockLength++] = (byte)nextByte;
                    if (currentBlockLength > 1)
                    {
                        if (nextByte == dataBlock[currentBlockLength - 2])
                            repCount++;
                        else
                            repCount = 1;
                    }

                    foundRepetition = repCount > 2;
                    if (foundRepetition)
                        break;
                }


                int numUncompToCopy = 0;
                if (foundRepetition)
                {
                    // if a repetition was found, copy block size - 3 bytes as compressed data
                    numUncompToCopy = currentBlockLength - 3;
                }
                else
                {
                    // if no repetition was found, copy min(block size, max block size - 2) bytes as uncompressed data.
                    numUncompToCopy = Math.Min(currentBlockLength, dataBlock.Length - 2);
                }

                #region insert uncompressed block
                if (numUncompToCopy > 0)
                {
                    byte flag = (byte)(numUncompToCopy - 1);
                    compressedData.Add(flag);
                    for (int i = 0; i < numUncompToCopy; i++)
                        compressedData.Add(dataBlock[i]);
                    // shift some possibly remaining bytes to the start
                    for (int i = numUncompToCopy; i < currentBlockLength; i++)
                        dataBlock[i - numUncompToCopy] = dataBlock[i];
                    currentBlockLength -= numUncompToCopy;
                }
                #endregion

                if (foundRepetition)
                {
                    // if a repetition was found, continue until the first different byte
                    // (or until the buffer is full)
                    while (currentBlockLength < dataBlock.Length && readLength < inLength)
                    {
                        nextByte = instream.ReadByte();
                        if (nextByte < 0)
                            throw new Exception("Stream too short.");//StreamTooShortException();
                        readLength++;

                        dataBlock[currentBlockLength++] = (byte)nextByte;

                        if (nextByte != dataBlock[0])
                            break;
                        else
                            repCount++;
                    }

                    // the next repCount bytes are the same.
                    #region insert compressed block
                    byte flag = (byte)(0x80 | (repCount - 3));
                    compressedData.Add(flag);
                    compressedData.Add(dataBlock[0]);
                    // make sure to shift the possible extra byte to the start
                    if (repCount != currentBlockLength)
                        dataBlock[0] = dataBlock[currentBlockLength - 1];
                    currentBlockLength -= repCount;
                    #endregion
                }
            }

            // write any reamaining bytes as uncompressed
            if (currentBlockLength > 0)
            {
                byte flag = (byte)(currentBlockLength - 1);
                compressedData.Add(flag);
                for (int i = 0; i < currentBlockLength; i++)
                    compressedData.Add(dataBlock[i]);
                currentBlockLength = 0;
            }

            // write the RLE marker and the decompressed size
            outstream.WriteByte(0x30);
            int compLen = compressedData.Count;
            outstream.WriteByte((byte)(inLength & 0xFF));
            outstream.WriteByte((byte)((inLength >> 8) & 0xFF));
            outstream.WriteByte((byte)((inLength >> 16) & 0xFF));

            // write the compressed data
            outstream.Write(compressedData.ToArray(), 0, compLen);

            // the total compressed stream length is the compressed data length + the 4-byte header
            return compLen + 4;
        }


        public static byte[] RLCompWrite(byte[] srcp)
        {
            return RLCompWrite(srcp, srcp.Length);
        }
        //===========================================================================
        //  Run length encoding (bytes)
        //===========================================================================
        public static byte[] RLCompWrite(byte[] srcp, int size)
        {
            int RLDstCount;                // Number of bytes of compressed data
            int RLSrcCount;                // Processed data volume of the compression target data (in bytes)
            byte RLCompFlag;                // 1 if performing run length encoding
            byte runLength;                 // Run length
            byte rawDataLength;             // Length of data not run
            byte i;
            IOStream dstp = new IOStream(size);

            //  dbg_printf_rl(stderr, "RLCompWrite\tsize=%d\n", size);

            //  Data header (The size after decompression)
            //      *(u32*)dstp = size << 8 | 0x80;  // data header
            dstp.Write32(size << 8 | 0x80);
            RLDstCount = 4;

            RLSrcCount = 0;
            rawDataLength = 0;
            RLCompFlag = 0;

            while (RLSrcCount < size)
            {
                //    startp = srcp[RLSrcCount];    // Set compression target data

                for (i = 0; i < 128; i++)      // Data volume that can be expressed in 7 bits is 0 to 127
                {
                    // Reach the end of the compression target data
                    if (RLSrcCount + rawDataLength >= size)
                    {
                        rawDataLength = (byte)(size - RLSrcCount);
                        break;
                    }

                    if (RLSrcCount + rawDataLength + 2 < size)
                    {
                        if (srcp[RLSrcCount + i] == srcp[RLSrcCount + i + 1] && srcp[RLSrcCount + i] == srcp[RLSrcCount + i + 2])
                        {
                            RLCompFlag = 1;
                            break;
                        }
                    }
                    rawDataLength++;
                }

                // Store data that is not encoded
                // If the 8th bit of the data length storage byte is 0, the data series that is not encoded.
                // The data length is x - 1, so 0-127 becomes 1-128.
                if (rawDataLength > 0)
                {
                    dstp.Write8((byte)(rawDataLength - 1));


                    //    dstp[RLDstCount++] = rawDataLength - 1;     // Store "data length - 1" (7 bits)
                    for (i = 0; i < rawDataLength; i++)
                    {
                        dstp.Write8(srcp[RLSrcCount++]);
                    }
                    rawDataLength = 0;
                }

                // Run Length Encoding
                if (RLCompFlag != 0)
                {
                    runLength = 3;
                    for (i = 3; i < 128 + 2; i++)
                    {
                        // Reach the end of the data for compression
                        if (RLSrcCount + runLength >= size)
                        {
                            runLength = (byte)(size - RLSrcCount);
                            break;
                        }

                        // If run is interrupted
                        if (srcp[RLSrcCount] != srcp[RLSrcCount + runLength])
                        {
                            break;
                        }
                        // Run continues
                        runLength++;
                    }

                    // If the 8th bit of the data length storage byte is 1, the data series that is encoded.
                    //  dstp[RLDstCount++] = 0x80 | (runLength - 3);        // Add 3, and store 3 to 130
                    dstp.Write8((byte)(0x80 | (runLength - 3)));
                    dstp.Write8((byte)(srcp[RLSrcCount]));

                    //   dstp[RLDstCount++] = srcp[RLSrcCount];
                    RLSrcCount += runLength;
                    RLCompFlag = 0;
                }
            }

            // Align to 4-byte boundary
            //   Does not include Data0 used for alignment as data size
            i = 0;
            while (
                ((RLDstCount + i) & 0x3) != 0

                )
            {
                // dstp[RLDstCount + i] = 0;
                dstp.Write8(0);
                i++;
            }

            return dstp.Data;
        }

        //public static byte[] FrameCompareComp(byte[] buffer)
        //{
        //    byte[] returnValue = new byte[1];
        //    if (buffer.Length < 200)
        //    {
        //        return buffer;
        //    }
        //    if (VideoCompression.oldFrame == null)
        //    {
        //        return buffer;
        //    }
        //    //Split the arrays up.

        //    IOStream src = new IOStream(4);
        //    src.Seek(0);
        //    src.WriteU32(CompressionHeaders.DIFFHEADER);
        //    Creator c = new Creator();
        //    using (var olds = new MemoryStream(VideoCompression.oldFrame))
        //    {
        //        using (var newf = new MemoryStream(buffer))
        //        {
        //            var str = c.Create(olds, newf);
        //            src.WriteU32((uint)str.Length);
        //            src.CopyFromArray(str, str.Length);
        //        }
        //    }
        //    returnValue = src.Data;

        //    return returnValue;
        //}

        public static byte[] Create3(byte[] source, byte[] target)
        {
            long sourcelen = source.Length;
            long targetlen = target.Length;

            bool sixteenmegabytes = false;

            if (sourcelen > 16777216)
            {
                sourcelen = 16777216;
                sixteenmegabytes = true;
            }

            if (targetlen > 16777216)
            {
                targetlen = 16777216;
                sixteenmegabytes = true;
            }



            IOStream rawBinData = new IOStream(5);
            Dictionary<UInt32, List<byte>> dics = new Dictionary<UInt32, List<byte>>();//Containing the differences at end.
            for (uint srcCount = 0; srcCount < source.Length; srcCount++)
            {
                UInt32 curOffset = 0;
                List<byte> differences = new List<byte>();
                if (source[srcCount] != target[srcCount])
                {
                    curOffset = srcCount;
                    while (srcCount != source.Length && source[srcCount] != target[srcCount])
                    {
                        differences.Add(target[srcCount++]);
                    }

                    dics.Add(curOffset, differences);
                }
            }


            //We store offsets then data because it'll make the LZ compression better.
            foreach (var entry in dics.Keys)
            {
                rawBinData.Write32((int)entry);
            }

            foreach (var entry in dics.Values)
            {
                foreach (var d in entry)
                {
                    rawBinData.Write16(d);
                }
            }


            return rawBinData.Data;
        }


    }
}
