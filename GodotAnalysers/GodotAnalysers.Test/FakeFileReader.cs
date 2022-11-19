using System.Collections.Generic;
using System.IO;

namespace GodotAnalysers.Test
{
    public class FakeFileReader : IFileReader
    {
        private readonly string fileName;
        private readonly string text;

        public FakeFileReader(string fileName, string text)
        {
            this.fileName = fileName;
            this.text = text;
        }
        public IEnumerable<string> ReadLines(string filePath)
        {
            if (fileName != Path.GetFileName(filePath))
            {
                throw new System.Exception($"Unknown file path {Path.GetFileName(filePath)}, Expected {fileName}");
            }

            return text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.None);
        }
    }
}
