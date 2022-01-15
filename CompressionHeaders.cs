namespace Video2Gba
{
    public enum CompressionHeaders
    {

        RAWHEADER = 0x00,
        LZCOMPRESSEDHEADER = 0x01,
        RLEHEADER = 0x2,
        DIFF8 = 0x3,
        DIFF16 = 0x4,
        RLEHEADER16 = 0x5,
        OneDimension = 0x6,
    }
}
