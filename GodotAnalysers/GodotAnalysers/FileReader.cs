using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Godot
{
    public interface IFileReader
    {
        IEnumerable<string> ReadLines(string scenePath);
    }

    public class RealFileReader : IFileReader
    {
        public IEnumerable<string> ReadLines(string filePath)
        {
            return File.ReadLines(filePath);
        }
    }
}
