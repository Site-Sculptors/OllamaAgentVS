using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Task = System.Threading.Tasks.Task;

namespace OllamaAgent.VSIX.Services
{
    public class SolutionTreeService
    {
        public async Task<string> GetSolutionTreeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var sb = new StringBuilder();
            var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            if (solution == null)
                return "(No solution loaded)";

            solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string optsFile);
            sb.AppendLine($"Solution: {System.IO.Path.GetFileName(solutionFile)}");

            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            if (dte?.Solution == null)
                return sb.ToString();

            foreach (Project proj in dte.Solution.Projects)
            {
                AppendProject(sb, proj, 1);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a prioritized list of (filename, content) tuples for small/relevant files in the solution.
        /// Prioritizes files matching keywords in userInput. Caps total content size.
        /// </summary>
        public async Task<List<(string FileName, string Content)>> GetRelevantSolutionFilesWithContentsAsync(string userInput, int maxTotalChars = 16000)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var results = new List<(string, string)>();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            if (dte?.Solution == null)
                return results;

            // Collect all candidate files
            var candidates = new List<ProjectItem>();
            foreach (Project proj in dte.Solution.Projects)
            {
                CollectRelevantFiles(proj.ProjectItems, candidates);
            }

            // Prioritize: 1. .csproj, 2. Program.cs, 3. interface files, 4. files matching keywords, 5. other small .cs/.xaml files
            var keywords = userInput?.Split(new[] { ' ', '\

        private void AppendProject(StringBuilder sb, Project proj, int indent)
        {
            if (proj == null || string.IsNullOrEmpty(proj.Name)) return;
            sb.AppendLine($"{new string(' ', indent * 2)}- {proj.Name}");
            try
            {
                AppendProjectItems(sb, proj.ProjectItems, indent + 1);
            }
            catch { }
        }

        private void AppendProjectItems(StringBuilder sb, ProjectItems items, int indent)
        {
            if (items == null) return;
            foreach (ProjectItem item in items)
            {
                try
                {
                    if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                    {
                        sb.AppendLine($"{new string(' ', indent * 2)}- {item.Name}");
                    }
                    else if (item.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                    {
                        sb.AppendLine($"{new string(' ', indent * 2)}+ {item.Name}/");
                        AppendProjectItems(sb, item.ProjectItems, indent + 1);
                    }
                }
                catch { }
            }
        }
    }
}
