using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
    public static class CompressionHeaders
    {

        public const uint RAWHEADER = 0x88FFFF00;
        public const uint PATCHHEADER = 0x88FFFF22;
        public const uint LZCOMPRESSEDHEADER = 0x88FFFF01;
        public const uint LZCOMPRESSEDCONTAINER = 0x88FFFF81;
        public const uint LZCOMPRESSEDDOUBLE =7;
        public const uint INTERLACERLEHEADER = 0x88FFFF22;
        public const uint INTERLACERLEHEADER2 = 0x88FFFF23;
        public const uint DESCRIBEHEADER = 0x88FFFF03;
        public const uint LUTHEADER = 0x88FFFF02;
        public const uint DIFFHEADER = 0x88FFFF12;
        public const uint QUADDIFFHEADER = 0x88FFFF13;
        public const uint QUADDIFFHEADER2 = 0x88FFFF14;
        public const uint RLEHEADER = 0x88FFFF15;
        public const uint GBATRoidHEader = 0x88FFFF75;
        public const uint NINTYRLHEADERINTR = 0x88FFFF76;
        public const uint NINTYRLHEADERINTR2 = 0x88FFFF74;
        public const uint NINTYRLHEADER= 0x98FFFF74;
        public const uint COMPNINTYRLHEADERINTR = 0x88FFFF77;
        public const uint DEWBGUGHEADEWR = 0x88FFFFA7;
    }
}
