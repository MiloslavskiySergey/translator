using CommunityToolkit.Maui.Views;

namespace translator.Popups;

public partial class SaveFilePopup : Popup
{
	public string Message { get; set; }

	public SaveFilePopup(string? fileName)
	{
		InitializeComponent();
		Message = fileName is null ? "SaveChanges?" : $"Save changes to a file \"{fileName}\"?";
		BindingContext = this;
	}
}