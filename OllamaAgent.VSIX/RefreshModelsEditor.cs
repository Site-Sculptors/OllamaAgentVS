using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

using Microsoft.VisualStudio.Shell;

namespace OllamaAgent.VSIX
{
	public class RefreshModelsEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService;

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal;
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

			var package = context?.Instance as OllamaAgentOptionsPage;
			if (package == null)
				return value;

			var confirm = MessageBox.Show(
				"Refresh models from Ollama now?",
				"Ollama Agent",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			if (confirm != DialogResult.Yes)
				return value;

			// call shared service (same logic as command)
			var service = new OllamaModelService();

			try
			{
				var task = service.GetModelsAsync(package.OllamaEndpoint);
				task.Wait();

				package.AvailableModels = task.Result;

				package.SaveSettingsToStorage();

				MessageBox.Show(
					$"Loaded {task.Result.Count} models.",
					"Ollama Agent",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					ex.Message,
					"Ollama Agent Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			return value;
		}
	}
}