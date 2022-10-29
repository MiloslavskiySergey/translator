using translator.Windows.Main;

namespace translator;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel mainViewModel)
	{
        InitializeComponent();
        BindingContext = mainViewModel;
    }
}
