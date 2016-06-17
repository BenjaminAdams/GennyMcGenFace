using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GennyMcGenFace.GennyMcGenFace
{
    public static class Words
    {
        private static string[] _words = { };
        private static bool _hasLoaded = false;

        public static string Gen()
        {
            if (_hasLoaded == false) ReadFromFile();

            return _words[StaticRandom.Instance.Next(1, _words.Length)];
        }

        public static string Gen(int count)
        {
            var str = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                str.Append(Gen());
            }
            return str.ToString();
        }

        private static void ReadFromFile()
        {
            // Get the file's text.
            // string whole_file = System.IO.File.ReadAllText(filename);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "GennyMcGenFace.GennyMcGenFace.Words.csv";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader r = new StreamReader(stream))
            {
                var fileContents = r.ReadToEnd();
                fileContents = fileContents.Replace("\r\n", "");
                _words = fileContents.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                _hasLoaded = true;
            }
        }

        //public static string NewList()
        //{
        //    ReadFromFile();
        //    var str = "";
        //    foreach (var wrd in _words)
        //    {
        //        var nwrd = wrd.Replace("\"", "").Trim();
        //        if (nwrd.Length < 5) continue;
        //        str += string.Format("\"{0}\",", FirstCharToUpper(nwrd));
        //    }

        //    return str;
        //}

        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}