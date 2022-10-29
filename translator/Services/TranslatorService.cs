using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using translator.Model;
using translator.Utils;

namespace translator.Services;

public class TranslatorService : ObservableObject
{
    private Utils.FileInfo? _fileInfo;
    public Utils.FileInfo? FileInfo
    {
        get => _fileInfo;
        set => SetProperty(ref _fileInfo, value);
    }

    private string _program = "";
    public string Program
    {
        get => _program;
        set => SetProperty(ref _program, value);
    }
    public void SetProgram(string program)
    {
        _program = program;
    }

    public ICommand ScanCommand { get; private set; }
    public ICommand OpenCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }

    public TranslatorService(ISaveProgramFileDialogService saveProgramDialog)
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
                FileInfo = new Utils.FileInfo(result.FullPath, result.FileName);
                var program = await File.ReadAllTextAsync(result.FullPath);
                Program = program.Replace("\r\n", "\n");
            }
        });
        SaveCommand = new Command(async () =>
        {
            Utils.FileInfo? fileInfo = null;
            if (FileInfo is not null)
            {
                fileInfo = FileInfo;
            }
            fileInfo = await saveProgramDialog.PickSaveFileAsync();
            if (fileInfo is not null)
            {
                await File.WriteAllTextAsync(fileInfo.Path, Program);
                FileInfo = fileInfo;
            }
        });
    }

    public List<TokensTableItem> Scan()
    {
        var tokenItems = new List<TokensTableItem>();
        var lexer = new Lexer(Program);
        var i = 1;
        foreach (var tokenPosition in lexer)
        {
            object value = "-";
            var token = tokenPosition.Token;
            if (token is IntegerNumberToken intagerToken)
                value = intagerToken.Value;
            else if (token is FloatNumberToken floatToken)
                value = floatToken.Value;
            else if (token is BoolToken boolToken)
                value = boolToken.Value;
            else if (token is StringToken stringToken)
                value = stringToken.Value;
            var lexema = token.Lexema;
            if (lexema == "\n")
                lexema = "new line";
            tokenItems.Add(new TokensTableItem(i, token.Type, lexema, value, tokenPosition.Line + 1, tokenPosition.Position + 1));
            i++;
        }
        return tokenItems;
    }
}
