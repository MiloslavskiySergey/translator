namespace translator.Services;

public interface ISaveProgramFileDialogService
{
    Task<Utils.FileInfo?> PickSaveFileAsync();
}
