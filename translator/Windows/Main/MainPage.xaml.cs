using translator.Services;

namespace translator;

public partial class MainPage : ContentPage
{
    public MainPage(TranslatorService translatorService)
	{
        InitializeComponent();
        BindingContext = translatorService;
    }
}
