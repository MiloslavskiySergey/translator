using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace translator.Windows.Main;

public class MainViewModel : ObservableObject
{
    public ICommand ScanCommand { get; private set; }

    public MainViewModel()
    {
        ScanCommand = new Command(() =>
        {
            var tokensWindow = new Window(new TokensPage());
            Application.Current!.OpenWindow(tokensWindow);
        });
    }
}
