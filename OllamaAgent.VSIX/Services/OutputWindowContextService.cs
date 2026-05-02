using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
    public interface IOutputWindowContextService
    {
        /// <summary>
        /// Gets the content of the Build or Debug output pane (prefers Build if present and non-empty).
        /// </summary>
        /// <returns>Tuple of (pane name, content) or (null, null) if not found.</returns>
        Task<(string PaneName, string Content)> GetBuildOrDebugOutputAsync();
    }

    public class OutputWindowContextService : IOutputWindowContextService
    {
        public async Task<(string PaneName, string Content)> GetBuildOrDebugOutputAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            if (dte == null)
                return (null, null);

            OutputWindow outputWindow = null;
            try
            {
                var window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                outputWindow = window?.Object as OutputWindow;
            }
            catch { }
            if (outputWindow == null)
                return (null, null);

            OutputWindowPane buildPane = null;
            OutputWindowPane debugPane = null;
            foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes)
            {
                if (pane.Name.IndexOf("Build", StringComparison.OrdinalIgnoreCase) >= 0)
                    buildPane = pane;
                else if (pane.Name.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0)
                    debugPane = pane;
            }

            // Prefer Build pane if present and non-empty
            if (buildPane != null)
            {
                string buildContent = GetPaneText(buildPane);
                if (!string.IsNullOrWhiteSpace(buildContent))
                    return (buildPane.Name, buildContent);
            }
            if (debugPane != null)
            {
                string debugContent = GetPaneText(debugPane);
                if (!string.IsNullOrWhiteSpace(debugContent))
                    return (debugPane.Name, debugContent);
            }
            return (null, null);
        }

        private string GetPaneText(OutputWindowPane pane)
        {
            try
            {
                var textDoc = pane.TextDocument;
                var editPoint = textDoc.StartPoint.CreateEditPoint();
                return editPoint.GetText(textDoc.EndPoint);
            }
            catch
            {
                return null;
            }
        }
    }
}
