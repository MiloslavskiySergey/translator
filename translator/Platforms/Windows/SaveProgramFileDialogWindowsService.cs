using Windows.Storage.Pickers;
using WinRT.Interop;
using translator.Services;

namespace translator.Platforms.Windows;

public class SaveProgramFileDialogWindowsService : ISaveProgramFileDialogService
{
    public async Task<Utils.FileInfo?> PickSaveFileAsync()
    {
        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        savePicker.FileTypeChoices.Add("Program", new List<string> { ".as" });
        savePicker.SuggestedFileName = "New program";
        if (MauiWinUIApplication.Current.Application.Windows[0].Handler!.PlatformView is MauiWinUIWindow window)
        {
            InitializeWithWindow.Initialize(savePicker, window.WindowHandle);
        }
        var file = await savePicker.PickSaveFileAsync();
        if (file is null)
            return null;
        return new Utils.FileInfo(file.Path, file.Name);
    }
}
