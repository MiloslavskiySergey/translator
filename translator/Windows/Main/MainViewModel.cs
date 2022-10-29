using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using translator.Services;

namespace translator.Windows.Main;

public class MainViewModel : ObservableObject
{
    public ICommand ScanCommand { get; private set; }
    public ICommand OpenCommand { get; private set; }

    public MainViewModel(TranslatorService translatorService)
    {
        ScanCommand = new Command(() =>
        {
            var tokensWindow = new Window(new TokensPage());
            Application.Current!.OpenWindow(tokensWindow);
        });
        OpenCommand = new Command(async () =>
        {
            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.WinUI, new[] { ".as" } }
                        }
                    )
                }
            );
            if (result != null)
            {
                var program = await File.ReadAllTextAsync(result.FullPath);
                translatorService.UpdateProgram(program.Replace("\r\n", "\n"));
            }
        });
    }
}
