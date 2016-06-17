using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GennyMcGenFace
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
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "GennyMcGenFace.Words.csv";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var r = new StreamReader(stream))
            {
                var fileContents = r.ReadToEnd();
                fileContents = fileContents.Replace("\r\n", "");
                _words = fileContents.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                _hasLoaded = true;
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input)) throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}