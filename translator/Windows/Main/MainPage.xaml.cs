namespace translator;

public partial class MainPage : ContentPage
{
    public MainPage()
	{
        InitializeComponent();
    }

	private void Scan_Clicked(object sender, EventArgs e)
	{
        var tokensWindow = new Window(new TokensPage());
        Application.Current!.OpenWindow(tokensWindow);
    }
}
