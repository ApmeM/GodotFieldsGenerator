#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class DataList {
        public readonly List<DataItem> Items = new List<DataItem>();
        
        public string GetSourceContent()
        {
            var data = this.Items;
            var dict = data.ToDictionary(a => a.TypeFullName, a => a);
            var sorted = DfsSort.SortDfs(data, data.ToDictionary(a => a, a => a.Parameters.Select(b => dict[b]).ToList())).SelectMany(a => a);
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("namespace GodotAnalysers");
            sb.AppendLine("{");
            sb.AppendLine("    public static class DependencyInjector");
            sb.AppendLine("    {");
            sb.AppendLine("         public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();");
            sb.AppendLine("         public static DependencyInjectorContext GetNewContext()");
            sb.AppendLine("         {");
            sb.AppendLine("             return new DependencyInjectorContext(GlobalContext);");
            sb.AppendLine("         }");
            sb.AppendLine("    }");
            sb.AppendLine("    public class DependencyInjectorContext");
            sb.AppendLine("    {");
            foreach (var def in sorted)
            {
                sb.AppendLine($"        public readonly {def.TypeFullName} {def.TypeName.ToFieldName()};");
            }

            sb.AppendLine("        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)");
            sb.AppendLine("        {");

            foreach (var def in sorted)
            {
                sb.Append($"            {def.TypeName.ToFieldName()} = " + ((def.OnDemand) ? "" : $"copyContext?.{def.TypeName.ToFieldName()} ?? ") + $" new {def.TypeFullName} (");
                sb.Append(string.Join(",", def.Parameters
                    .Select(a => dict[a])
                    .Select(a => a.TypeName.ToFieldName())));
                sb.AppendLine($");");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public class DataItem
    {
        public List<string> Parameters;
        public bool OnDemand;
        public string TypeName;
        internal string TypeFullName;
    }
}