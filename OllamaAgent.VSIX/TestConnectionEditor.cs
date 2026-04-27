using System;
using System.Drawing.Design;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

using Microsoft.VisualStudio.Shell;

namespace OllamaAgent.VSIX
{
	public class TestConnectionEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService;

		public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.Modal;
		}

		public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

			var options = context.Instance as OllamaAgentOptionsPage;
			if (options == null)
				return value;

			var result = MessageBox.Show(
				$"Test connection to:\n{options.OllamaEndpoint}",
				"Ollama Agent",
				MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question);

			if (result != DialogResult.OK)
				return value;

			try
			{
				using var client = new HttpClient();

				// quick lightweight ping endpoint
				var task = client.GetAsync(options.OllamaEndpoint.TrimEnd('/') + "/api/tags");
				task.Wait();

				if (task.Result.IsSuccessStatusCode)
				{
					MessageBox.Show(
						"Connection successful.",
						"Ollama Agent",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show(
						$"Server responded: {task.Result.StatusCode}",
						"Ollama Agent",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"Connection failed:\n{ex.Message}",
					"Ollama Agent",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			return value;
		}
	}
}