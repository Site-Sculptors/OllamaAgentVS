using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
    public interface ISymbolExtractorService
    {
        /// <summary>
        /// Extracts a compact symbol summary (classes, methods) from the given source code.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the code (e.g., "CSharp").</param>
        /// <returns>A compact symbol summary string.</returns>
        Task<string> ExtractSymbolSummaryAsync(string code, string language);
    }

    public class SymbolExtractorService : ISymbolExtractorService
    {
        public async Task<string> ExtractSymbolSummaryAsync(string code, string language)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            // For C#, use regex as a fallback if Roslyn is not available
            if (language?.ToLowerInvariant().Contains("csharp") == true || language?.ToLowerInvariant().Contains("c#") == true)
            {
                return await Task.Run(() => ExtractCSharpSymbols(code));
            }
            // Add more languages as needed
            return string.Empty;
        }

        private string ExtractCSharpSymbols(string code)
        {
            var lines = code.Split(new[] { '\
', '\
' }, StringSplitOptions.RemoveEmptyEntries);
            var classRegex = new Regex(@"\b(class|struct|interface|record)\s+(\w+)", RegexOptions.Compiled);
            var methodRegex = new Regex(@"\b(public|private|protected|internal|static|async|virtual|override|sealed|partial|extern|unsafe|new|abstract|readonly|volatile|\s)+\s*([\w<>\[\]]+)\s+(\w+)\s*\(([^)]*)\)", RegexOptions.Compiled);
            var sb = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var classMatch = classRegex.Match(line);
                if (classMatch.Success)
                {
                    sb.AppendLine($"class {classMatch.Groups[2].Value}");
                }
                var methodMatch = methodRegex.Match(line);
                if (methodMatch.Success)
                {
                    sb.AppendLine($"  {methodMatch.Groups[3].Value}({methodMatch.Groups[4].Value})");
                }
            }
            return sb.ToString().Trim();
        }
    }
}
