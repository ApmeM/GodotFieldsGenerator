#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public static class Ext
    {
        public static T CopyAnnotationsFrom<T>(this T node, SyntaxNode other) where T : SyntaxNode
        {
            return other.CopyAnnotationsTo(node);
        }

        public static string ToFieldName(this string name)
        {
            return name[0].ToString().ToLower() + name.Substring(1);
        }

        public static string GetFullName(this ITypeSymbol type)
        {
            var result = new List<string>();
            result.Add(type.Name);
            var ns = type.ContainingNamespace;
            while (ns != null && !string.IsNullOrWhiteSpace(ns.Name))
            {
                result.Add(ns.Name);
                ns = ns.ContainingNamespace;
            }
            result.Reverse();
            return string.Join(".", result);
        }
    }
}