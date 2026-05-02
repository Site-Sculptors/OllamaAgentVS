using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
    public interface IEditorContextService
    {
        Task<(string fileName, string language, string content, string selection)> GetActiveDocumentContextAsync();
    }

    public class EditorContextService : IEditorContextService
    {
        public async Task<(string fileName, string language, string content, string selection)> GetActiveDocumentContextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            if (dte?.ActiveDocument == null)
                return (null, null, null, null);

            var doc = dte.ActiveDocument;
            var fileName = doc.FullName;
            var language = doc.Language;
            string content = null;
            string selection = null;

            var textDoc = doc.Object("TextDocument") as TextDocument;
            if (textDoc != null)
            {
                var editPoint = textDoc.StartPoint.CreateEditPoint();
                var endPoint = textDoc.EndPoint;
                int totalLines = endPoint.Line;
                if (totalLines > 500)
                {
                    // Only include visible lines (approximate: 100 lines around selection)
                    var sel = textDoc.Selection;
                    int startLine = Math.Max(1, sel.TopLine - 50);
                    int endLine = Math.Min(totalLines, sel.BottomLine + 50);
                    var startPt = textDoc.CreateEditPoint();
                    startPt.MoveToLineAndOffset(startLine, 1);
                    var endPt = textDoc.CreateEditPoint();
                    endPt.MoveToLineAndOffset(endLine, 1);
                    content = startPt.GetText(endPt);
                }
                else
                {
                    content = editPoint.GetText(endPoint);
                }
                if (!textDoc.Selection.IsEmpty)
                {
                    selection = textDoc.Selection.Text;
                }
            }
            return (fileName, language, content, selection);
        }
    }
}
