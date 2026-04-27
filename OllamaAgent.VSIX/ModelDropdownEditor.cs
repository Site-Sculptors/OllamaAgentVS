using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace OllamaAgent.VSIX
{
	public class ModelDropdownEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService;

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

			if (_editorService == null)
				return value;

			var options = context.Instance as OllamaAgentOptionsPage;

			var listBox = new ListBox
			{
				BorderStyle = BorderStyle.None
			};

			if (options?.AvailableModels != null)
			{
				foreach (var model in options.AvailableModels)
				{
					listBox.Items.Add(model);
				}
			}

			listBox.SelectedIndexChanged += (s, e) =>
			{
				_editorService.CloseDropDown();
			};

			listBox.Click += (s, e) =>
			{
				_editorService.CloseDropDown();
			};

			_editorService.DropDownControl(listBox);

			return listBox.SelectedItem?.ToString() ?? value;
		}
	}
}