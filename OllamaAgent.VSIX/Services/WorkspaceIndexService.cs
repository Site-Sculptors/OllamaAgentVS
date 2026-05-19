using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Services;

namespace OllamaAgent.VSIX.Services
{
	public class WorkspaceIndexService
	{
		private readonly SolutionTreeService _solutionTreeService;
		private readonly ISymbolExtractorService _symbolExtractorService;
		private readonly ConcurrentDictionary<string, string> _fileSymbolIndex = new();

		public WorkspaceIndexService(SolutionTreeService solutionTreeService, ISymbolExtractorService symbolExtractorService)
		{
			_solutionTreeService = solutionTreeService;
			_symbolExtractorService = symbolExtractorService;
		}

		public async Task BuildIndexAsync()
		{
			var files = await GetAllSolutionFilesAsync();
			foreach (var file in files)
			{
				try
				{
					var code = File.ReadAllText(file);
					var symbolSummary = await _symbolExtractorService.ExtractSymbolSummaryAsync(code, "CSharp");
					_fileSymbolIndex[file] = symbolSummary;
				}
				catch { /* Ignore unreadable files */ }
			}
		}

		/// <summary>
		/// Searches for symbols by name or type in the indexed files.
		/// </summary>
		/// <param name="query">The symbol name or type to search for.</param>
		/// <returns>List of (file, symbol line) tuples matching the query.</returns>
		public List<(string File, string SymbolLine)> SearchSymbols(string query)
		{
			var results = new List<(string, string)>();
			if (string.IsNullOrWhiteSpace(query)) return results;
			foreach (var kvp in _fileSymbolIndex)
			{
				var lines = kvp.Value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					if (line.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						results.Add((kvp.Key, line.Trim()));
					}
				}
			}
			return results;
		}

		/// <summary>
		/// Finds usages of a symbol (by name) across all indexed files.
		/// </summary>
		/// <param name="symbolName">The symbol name to search for.</param>
		/// <returns>List of (file, line) tuples where the symbol is used.</returns>
		public List<(string File, string Line)> FindSymbolUsages(string symbolName)
		{
			var results = new List<(string, string)>();
			if (string.IsNullOrWhiteSpace(symbolName)) return results;
			foreach (var file in _fileSymbolIndex.Keys)
			{
				try
				{
					var lines = File.ReadAllLines(file);
					for (int i = 0; i < lines.Length; i++)
					{
						if (lines[i].IndexOf(symbolName, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							results.Add((file, lines[i].Trim()));
						}
					}
				}
				catch { }
			}
			return results;
		}

		/// <summary>
		/// Performs a full-text search for the given text across all indexed files.
		/// </summary>
		/// <param name="text">The text to search for.</param>
		/// <returns>List of (file, line) tuples where the text is found.</returns>
		public List<(string File, string Line)> FullTextSearch(string text)
		{
			var results = new List<(string, string)>();
			if (string.IsNullOrWhiteSpace(text)) return results;
			foreach (var file in _fileSymbolIndex.Keys)
			{
				try
				{
					var lines = File.ReadAllLines(file);
					foreach (var line in lines)
					{
						if (line.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							results.Add((file, line.Trim()));
						}
					}
				}
				catch { }
			}
			return results;
		}

		/// <summary>
		/// Gets a context window (surrounding lines) for a file and line number.
		/// </summary>
		public string GetContextWindow(string file, int lineNumber, int windowSize = 5)
		{
			if (!File.Exists(file)) return string.Empty;
			var lines = File.ReadAllLines(file);
			int start = Math.Max(1, lineNumber - windowSize);
			int end = Math.Min(lines.Length, lineNumber + windowSize);
			return string.Join(Environment.NewLine, lines, start - 1, end - start + 1);
		}

		/// <summary>
		/// Gets a code snippet from a file, given a line range.
		/// </summary>
		public string GetCodeSnippet(string file, int startLine, int endLine)
		{
			if (!File.Exists(file)) return string.Empty;
			var lines = File.ReadAllLines(file);
			startLine = Math.Max(1, startLine);
			endLine = Math.Min(lines.Length, endLine);
			if (startLine > endLine) return string.Empty;
			return string.Join(Environment.NewLine, lines, startLine - 1, endLine - startLine + 1);
		}

		/// <summary>
		/// Summarizes a file for LLM input by returning the symbol summary and first N lines.
		/// </summary>
		public string SummarizeFile(string file, int maxLines = 50)
		{
			if (!File.Exists(file)) return string.Empty;
			var lines = File.ReadAllLines(file);
			var lineCount = Math.Min(maxLines, lines.Length);
			var snippet = string.Join(Environment.NewLine, lines, 0, lineCount);
			var summary = GetSymbolSummary(file);
			return $"Summary:{Environment.NewLine}{summary}{Environment.NewLine}---{Environment.NewLine}Snippet:{Environment.NewLine}{snippet}";
		}

		/// <summary>
		/// Summarizes large search result sets for LLM input.
		/// </summary>
		public string SummarizeSearchResults(List<(string File, string Line)> results, int maxResults = 20, int maxLineLength = 200)
		{
			if (results == null || results.Count == 0) return string.Empty;
			var output = new List<string>();
			var count = Math.Min(maxResults, results.Count);
			for (int i = 0; i < count; i++)
			{
				var line = results[i].Line ?? string.Empty;
				if (line.Length > maxLineLength)
				{
					line = line.Substring(0, maxLineLength) + "...";
				}
				output.Add($"{results[i].File}: {line}");
			}
			return $"Results: {count}/{results.Count}{Environment.NewLine}{string.Join(Environment.NewLine, output)}";
		}

		/// <summary>
		/// Gets the symbol summary for a file.
		/// </summary>
		public string GetSymbolSummary(string file)
		{
			if (_fileSymbolIndex.TryGetValue(file, out var summary))
				return summary;
			return string.Empty;
		}

		public IReadOnlyDictionary<string, string> GetIndex() => _fileSymbolIndex;

		private async Task<List<string>> GetAllSolutionFilesAsync()
		{
			// Use SolutionTreeService to get all files
			var files = new List<string>();
			var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
			if (dte?.Solution == null)
				return files;
			foreach (EnvDTE.Project proj in dte.Solution.Projects)
			{
				CollectFiles(proj.ProjectItems, files);
			}
			return files;
		}

		private void CollectFiles(EnvDTE.ProjectItems items, List<string> files)
		{
			if (items == null) return;
			foreach (EnvDTE.ProjectItem item in items)
			{
				try
				{
					if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile && item.FileCount > 0)
					{
						string filePath = item.FileNames[1];
						files.Add(filePath);
					}
					else if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
					{
						CollectFiles(item.ProjectItems, files);
					}
				}
				catch { }
			}
		}
	}
}
