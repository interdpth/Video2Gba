using System.IO;
using Video2Gba.LibIpsNet.Utils;

namespace Video2Gba.LibIpsNet
{
    public class Creator
    {

        // Known situations where this function does not generate an optimal patch:
        // In:  80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80
        // Out: FF FF FF FF FF FF FF FF 00 01 02 03 04 05 06 07 FF FF FF FF FF FF FF FF
        // IPS: [         RLE         ] [        Copy         ] [         RLE         ]
        // Possible improvement: RLE across the entire file, copy on top of that.
        // Rationale: It would be a huge pain to create such a multi-pass tool if it should support writing a byte
        // more than twice, and I don't like half-assing stuff.

        // Known improvements over LIPS:
        // In:  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
        // Out: FF 01 02 03 04 05 FF FF FF FF FF FF FF FF FF FF
        // LIPS:[      Copy     ] [            RLE            ]
        // Mine:[] [ Unchanged  ] [            RLE            ]
        // Rationale: While LIPS can break early if it finds something RLEable in the middle of a block, it's not
        // smart enough to back off if there's something unchanged between the changed area and the RLEable spot.

        // In:  FF FF FF FF FF FF FF
        // Out: 00 00 00 00 01 02 03
        // LIPS:[   RLE   ] [ Copy ]
        // Mine:[       Copy       ]
        // Rationale: Again, RLE is no good at RLE.

        // It is also known that I win in some other situations. I didn't bother checking which, though.

        // There are no known cases where LIPS wins over libips.


        const int EndOfFile = 0x464F45;
        const int EndOfFile2 = 0x454F46;

        public byte[] Create16(Stream source, Stream target)
        {
            IOStream patch = new IOStream(8);
            long sourcelen = source.Length;
            long targetlen = target.Length;

            bool sixteenmegaushorts = false;

            if (sourcelen > 16777216)
            {
                sourcelen = 16777216;
                sixteenmegaushorts = true;
            }
            if (targetlen > 16777216)
            {
                targetlen = 16777216;
                sixteenmegaushorts = true;
            }

            int offset = 0;
            {
                int lastknownchange = 0;
                while (offset < targetlen)
                {
                    while (offset < sourcelen && (offset < sourcelen ? Reader.Read16(source, offset) : 0) == Reader.Read16(target, offset)) offset++;

                    // Check how much we need to edit until it starts getting similar.
                    int thislen = 0;
                    int consecutiveunchanged = 0;
                    thislen = lastknownchange - offset;
                    if (thislen < 0) thislen = 0;

                    while (true)
                    {
                        int thisushort = offset + thislen + consecutiveunchanged;
                        if (thisushort < sourcelen && (thisushort < sourcelen ? Reader.Read16(source, thisushort) : 0) == Reader.Read16(target, thisushort)) consecutiveunchanged++;
                        else
                        {
                            thislen += consecutiveunchanged + 1;
                            consecutiveunchanged = 0;
                        }
                        if (consecutiveunchanged >= 6 || thislen >= 65536) break;
                    }

                    // Avoid premature EOF.
                    if (offset == EndOfFile)
                    {
                        offset--;
                        thislen++;
                    }

                    lastknownchange = offset + thislen;
                    if (thislen > 65535) thislen = 65535;
                    if (offset + thislen > targetlen) thislen = (int)(targetlen - offset);
                    if (offset == targetlen) continue;

                    // Check if RLE here is worthwhile.
                    int ushortshere = 0;

                    for (ushortshere = 0; ushortshere < thislen && Reader.Read16(target, offset) == Reader.Read16(target, (offset + ushortshere)); ushortshere++) { }

                    if (ushortshere == thislen)
                    {
                        int thisushort = Reader.Read16(target, offset);
                        int i = 0;

                        while (true)
                        {
                            int pos = offset + ushortshere + i - 1;
                            if (pos >= targetlen || Reader.Read16(target, pos) != thisushort || ushortshere + i > 65535) break;
                            if (pos >= sourcelen || (pos < sourcelen ? Reader.Read8(source, pos) : 0) != thisushort)
                            {
                                ushortshere += i;
                                thislen += i;
                                i = 0;
                            }
                            i++;
                        }
                    }
                    if ((ushortshere > 8 - 5 && ushortshere == thislen) || ushortshere > 8)
                    {
                        patch.Write32(offset);
                        patch.Write32(0x6969 | 0x8000000);
                        patch.Write32(ushortshere);
                        patch.Write8(Reader.Read8(target, offset));
                        offset += ushortshere;
                    }
                    else
                    {
                        // Check if we'd gain anything from ending the block early and switching to RLE.
                        ushortshere = 0;
                        int stopat = 0;

                        while (stopat + ushortshere < thislen)
                        {
                            if (Reader.Read16(target, (offset + stopat)) == Reader.Read16(target, (offset + stopat + ushortshere))) ushortshere++;
                            else
                            {
                                stopat += ushortshere;
                                ushortshere = 0;
                            }

                            // RLE-worthy despite two IPS headers.
                            if (ushortshere > 8 + 5 ||

                                    // RLE-worthy at end of data.
                                    (ushortshere > 8 && stopat + ushortshere == thislen) ||
                                    (ushortshere > 8 && Compare(target, (offset + stopat + ushortshere), target, (offset + stopat + ushortshere + 1), 9 - 1)))//rle-worthy before another rle-worthy
                            {
                                if (stopat != 0) thislen = stopat;

                                // We don't scan the entire block if we know we'll want to RLE, that'd gain nothing.
                                break;
                            }
                        }

                        // Don't write unchanged ushorts at the end of a block if we want to RLE the next couple of ushorts.
                        if (offset + thislen != targetlen)
                        {
                            while (offset + thislen - 1 < sourcelen && Reader.Read16(target, (offset + thislen - 1)) == (offset + thislen - 1 < sourcelen ? Reader.Read16(source, (offset + thislen - 1)) : 0)) thislen--;
                        }
                        if (thislen > 3 && Compare(target, offset, target, (offset + 1), (thislen - 1)))
                        {
                            patch.Write32(offset);
                            patch.Write32(0x6969 | 0x8000000);
                            patch.Write32(thislen);
                            patch.Write8(Reader.Read8(target, offset));
                        }
                        else
                        {
                            patch.Write32(offset);
                            patch.Write32(thislen);
                            int i;
                            for (i = 0; i < thislen; i++)
                            {
                                patch.Write8(Reader.Read8(target, (offset + i)));
                            }
                        }
                        offset += thislen;
                    }
                }


                patch.Write32(EndOfFile);


                if (sourcelen > targetlen) patch.Write24((int)targetlen);

                //   if (sixteenmegaushorts) throw new Exceptions.Ips16MegaushortsException(); ;
                if (patch.Length == 8) throw new Exceptions.IpsIdenticalException();
            }
            return patch.Data;
        }

        public byte[] Create(Stream source, Stream target)
        {
            IOStream patch = new IOStream(8);
            patch.WriteASCII("COMP");
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
            const int RLEMinLength = 16;
            int offset = 0;
            {
                int lastknownchange = 0;
                while (offset < targetlen)
                {
                    while (offset < sourcelen && (offset < sourcelen ? Reader.Read8(source, offset) : 0) == Reader.Read8(target, offset)) offset++;

                    // Check how much we need to edit until it starts getting similar.
                    int thislen = 0;
                    int consecutiveunchanged = 0;
                    thislen = lastknownchange - offset;
                    if (thislen < 0) thislen = 0;

                    while (true)
                    {
                        int thisbyte = offset + thislen + consecutiveunchanged;
                        if (thisbyte < sourcelen && (thisbyte < sourcelen ? Reader.Read8(source, thisbyte) : 0) == Reader.Read8(target, thisbyte)) consecutiveunchanged++;
                        else
                        {
                            thislen += consecutiveunchanged + 1;
                            consecutiveunchanged = 0;
                        }
                        if (consecutiveunchanged >= 6 || thislen >= 65536) break;
                    }

                    // Avoid premature EOF.
                    if (offset == EndOfFile)
                    {
                        offset--;
                        thislen++;
                    }

                    lastknownchange = offset + thislen;
                    if (thislen > 65535) thislen = 65535;
                    if (offset + thislen > targetlen) thislen = (int)(targetlen - offset);
                    if (offset == targetlen) continue;

                    // Check if RLE here is worthwhile.
                    int byteshere = 0;

                    for (byteshere = 0; byteshere < thislen && Reader.Read8(target, offset) == Reader.Read8(target, (offset + byteshere)); byteshere++) { }

                    if (byteshere == thislen)
                    {
                        int thisbyte = Reader.Read8(target, offset);
                        int i = 0;

                        while (true)
                        {
                            int pos = offset + byteshere + i - 1;
                            if (pos >= targetlen || Reader.Read8(target, pos) != thisbyte || byteshere + i > 65535) break;
                            if (pos >= sourcelen || (pos < sourcelen ? Reader.Read8(source, pos) : 0) != thisbyte)
                            {
                                byteshere += i;
                                thislen += i;
                                i = 0;
                            }
                            i++;
                        }
                    }
                    if ((byteshere > 9 - 5 && byteshere == thislen) || byteshere > 8)
                    {
                        patch.Write32(offset);
                        patch.Write16(0x6969);
                        patch.Write16((ushort)byteshere);
                        patch.Write8(Reader.Read8(target, offset));
                        offset += byteshere;
                    }
                    else
                    {
                        // Check if we'd gain anything from ending the block early and switching to RLE.
                        byteshere = 0;
                        int stopat = 0;

                        while (stopat + byteshere < thislen)
                        {
                            if (Reader.Read8(target, (offset + stopat)) == Reader.Read8(target, (offset + stopat + byteshere))) byteshere++;
                            else
                            {
                                stopat += byteshere;
                                byteshere = 0;
                            }

                            // RLE-worthy despite two IPS headers.
                            if (byteshere > RLEMinLength + 5 ||

                                    // RLE-worthy at end of data.
                                    (byteshere > RLEMinLength && stopat + byteshere == thislen) ||
                                    (byteshere > RLEMinLength && Compare(target, (offset + stopat + byteshere), target, (offset + stopat + byteshere + 1), RLEMinLength + 1 - 1)))//rle-worthy before another rle-worthy
                            {
                                if (stopat != 0) thislen = stopat;

                                // We don't scan the entire block if we know we'll want to RLE, that'd gain nothing.
                                break;
                            }
                        }

                        // Don't write unchanged bytes at the end of a block if we want to RLE the next couple of bytes.
                        if (offset + thislen != targetlen)
                        {
                            while (offset + thislen - 1 < sourcelen && Reader.Read8(target, (offset + thislen - 1)) == (offset + thislen - 1 < sourcelen ? Reader.Read8(source, (offset + thislen - 1)) : 0)) thislen--;
                        }
                        if (thislen > RLEMinLength && Compare(target, offset, target, (offset + 1), (thislen - 1)))
                        {
                            patch.Write32(offset);
                            patch.Write16(0x6969);
                            patch.Write16((ushort)thislen);
                            patch.Write8(Reader.Read8(target, offset));
                        }
                        else
                        {
                            patch.Write32(offset);
                            patch.Write16((ushort)thislen);
                            int i;
                            for (i = 0; i < thislen; i++)
                            {
                                patch.Write8(Reader.Read8(target, (offset + i)));
                            }
                        }
                        offset += thislen;
                    }
                }

                patch.Write32(EndOfFile);


                if (sourcelen > targetlen) patch.Write24((int)targetlen);

                if (sixteenmegabytes) throw new Exceptions.Ips16MegabytesException(); ;
                if (patch.Length == 8) throw new Exceptions.IpsIdenticalException();
            }
            return patch.Data;
        }

        // Helper to Compare two BinaryReaders with a starting point and a count of elements.
        private bool Compare(Stream source, int sourceStart, Stream target, int targetStart, int count)
        {
            source.Seek(sourceStart, SeekOrigin.Begin);
            byte[] sourceData = new byte[count];
            source.Read(sourceData, 0, count);

            target.Seek(targetStart, SeekOrigin.Begin);
            byte[] targetData = new byte[count];
            target.Read(targetData, 0, count);

            for (int i = 0; i < count; i++)
            {
                if (sourceData[i] != targetData[i]) return false;
            }
            return true;
        }
    }
}