using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
    public interface IErrorListService
    {
        /// <summary>
        /// Gets all error list items for the given file (full path).
        /// </summary>
        Task<IReadOnlyList<ErrorListItem>> GetErrorsForFileAsync(string filePath);
    }

    public class ErrorListItem
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public string Severity { get; set; }
    }

    public class ErrorListService : IErrorListService
    {
        public async Task<IReadOnlyList<ErrorListItem>> GetErrorsForFileAsync(string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            EnvDTE80.ErrorList errorList = null;
            try
            {
                var window = dte?.Windows?.Item(EnvDTE80.WindowKinds.vsWindowKindErrorList);
                errorList = window?.Object as EnvDTE80.ErrorList;
            }
            catch { }
            var errorItems = errorList?.ErrorItems;
            var results = new List<ErrorListItem>();
            if (errorItems == null || string.IsNullOrEmpty(filePath))
                return results;
            for (int i = 1; i <= errorItems.Count; i++)
            {
                var item = errorItems.Item(i);
                if (item == null) continue;
                try
                {
                    // Only include errors for the given file
                    if (!string.Equals(item.FileName, filePath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    results.Add(new ErrorListItem
                    {
                        Message = item.Description,
                        Line = item.Line,
                        Severity = item.ErrorLevel.ToString()
                    });
                }
                catch { }
            }
            return results;
        }
    }
}
