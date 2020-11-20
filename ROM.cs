using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Video2Gba
{
    public class ROM
    {
        private static string comment = "//---------------------------------------------------------------------------------";
        public static void MakeSource(string ArrayName,  byte[] buffer, string outputdir)
        {
            //---------------------------------------------------------------------------------
            List<string> headerLines = new List<string>();
            List<string> sourceLines = new List<string>();
            UInt64 counter = 0UL;
            UInt64 length = (ulong)buffer.Length;

            headerLines.Add($"#ifndef _{ArrayName}_h_\n");
            headerLines.Add($"#define _{ArrayName}_h_\n");
            headerLines.Add(comment);
            headerLines.Add($"extern const unsigned char {ArrayName}[];\n");
            headerLines.Add($"extern const int {ArrayName}_size;\n");
            headerLines.Add(comment);
            headerLines.Add($"#endif //_{ArrayName}_h_\n");
            headerLines.Add(comment);


            sourceLines.Add($"const unsigned char {ArrayName}[] = {{\n\t");
            string thislIne = "";
            while (counter < length)
            {
                thislIne += "0x"+buffer[counter++].ToString("X2")+",";

                if ((int)(counter % 16) == 0)
                {
                    sourceLines.Add(thislIne);
                    thislIne = "";
                }
            }

            if (!string.IsNullOrEmpty(thislIne))
            {
                sourceLines.Add(thislIne);
            }

            sourceLines.Add($"\n}};\n");
            sourceLines.Add($"const int {ArrayName}_size = sizeof({ArrayName});\n");
            File.WriteAllLines($"{outputdir}\\{ArrayName}.h", headerLines);
            File.WriteAllLines($"{outputdir}\\{ArrayName}.c", sourceLines);
            return;
        }

        //lol
        public static void MakeSource(string ArrayName, sbyte[] buffer, string outputdir)
        {
            //---------------------------------------------------------------------------------
            List<string> headerLines = new List<string>();
            List<string> sourceLines = new List<string>();
            UInt64 counter = 0UL;
            UInt64 length = (ulong)buffer.Length;

            headerLines.Add($"#ifndef _{ArrayName}_h_\n");
            headerLines.Add($"#define _{ArrayName}_h_\n");
            headerLines.Add(comment);
            headerLines.Add($"extern const unsigned char {ArrayName}[];\n");
            headerLines.Add($"extern const int {ArrayName}_size;\n");
            headerLines.Add(comment);
            headerLines.Add($"#endif //_{ArrayName}_h_\n");
            headerLines.Add(comment);


            sourceLines.Add($"const unsigned char {ArrayName}[] = {{\n\t");
            string thislIne = "";
            while (counter < length)
            {
                thislIne += "0x" + buffer[counter++].ToString("X2") + ",";

                if ((int)(counter % 16) == 0)
                {
                    sourceLines.Add(thislIne);
                    thislIne = "";
                }
            }

            if (!string.IsNullOrEmpty(thislIne))
            {
                sourceLines.Add(thislIne);
            }

            sourceLines.Add($"\n}};\n");
            sourceLines.Add($"const int {ArrayName}_size = sizeof({ArrayName});\n");
            File.WriteAllLines($"{outputdir}\\{ArrayName}.h", headerLines);
            File.WriteAllLines($"{outputdir}\\{ArrayName}.c", sourceLines);
            return;
        }


    }
}
