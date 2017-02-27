using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using IniParser;
using IniParser.Model;

namespace SDS.Utilities.IniFiles
{
    class IniFile   // revision 11
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;
        FileIniDataParser parser = new FileIniDataParser();
        IniData data;

        //[DllImport("kernel32", CharSet = CharSet.Unicode)]
        //static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        //[DllImport("kernel32", CharSet = CharSet.Unicode)]
        //static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName.ToString();

            // Conversion to ini-parser package
            data = parser.ReadFile(Path);
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            //GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);

            // Conversion to ini-parser package
            if (Section == null)
            {
                RetVal.Append(data[Section ?? EXE][Key]);
            }
            else
            {
                if (data.Sections.ContainsSection(Section))
                    RetVal.Append(data[Section ?? EXE][Key]);
                else
                    return null;
            }

            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            //WritePrivateProfileString(Section ?? EXE, Key, Value, Path);

            // Conversion to ini-parser package
            if (Section != null && !data.Sections.ContainsSection(Section))
                data.Sections.AddSection(Section);

            data[Section ?? EXE][Key] = Value;

            parser.WriteFile(Path, data);
        }

        /* DeleteKey and DeleteSection don't work with .NET ini-parser
        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? EXE);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? EXE);
        }
        */

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
}
