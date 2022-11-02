using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using translator.Model;
using translator.Utils;
using CommunityToolkit.Maui.Views;
using translator.Popups;

namespace translator.Services;

public class TranslatorService : ObservableObject
{
    private Utils.FileInfo? _fileInfo;
    public Utils.FileInfo? FileInfo
    {
        get => _fileInfo;
        set => SetProperty(ref _fileInfo, value);
    }

    private string _fileProgram = "";
    public string FileProgram
    {
        get => _fileProgram;
        set
        {
            SetProperty(ref _fileProgram, value);
            OnPropertyChanged(nameof(IsProgramModified));
        }
    }

    private string _program = "";
    public string Program
    {
        get => _program;
        set
        {
            SetProperty(ref _program, value);
            OnPropertyChanged(nameof(IsProgramModified));
        }
    }
    public void SetProgram(string program)
    {
        _program = program;
        OnPropertyChanged(nameof(IsProgramModified));
    }

    public bool IsProgramModified { get => Program != FileProgram; }

    public ICommand NewCommand { get; private set; }
    public ICommand OpenCommand { get; private set; }
    public ICommand SaveCommand { get; private set; }
    public ICommand ScanCommand { get; private set; }
    public ICommand ParseCommand { get; private set; }

    public TranslatorService(ISaveProgramFileDialogService saveProgramDialog)
    {
        NewCommand = new Command(async () => {
            await SaveChanges();
            FileInfo = null;
            FileProgram = "";
            Program = "";
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
                var program = (await File.ReadAllTextAsync(result.FullPath)).Replace("\r\n", "\n");
                Program = program;
                FileProgram = program;
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
                FileProgram = Program;
            }
        });
        ScanCommand = new Command(() =>
        {
            var tokensWindow = new Window(new TokensPage());
            Application.Current!.OpenWindow(tokensWindow);
        });
        ParseCommand = new Command(() =>
        {
            var astWindow = new Window(new ASTPage());
            Application.Current!.OpenWindow(astWindow);
        });
    }

    public async Task SaveChanges() 
    {
        if (IsProgramModified)
        {
            if (FileInfo is null) {
                var popup = new SaveFilePopup(null);
                var result = await Application.Current!.MainPage!.ShowPopupAsync(popup) as bool?;
            }
        }
    }

    public IEnumerable<TokensTableItem> Scan()
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

    public IEnumerable<TreeViewItem> Parse()
    {
        var items = new List<TreeViewItem>();
        var lexer = new Lexer(Program);
        var parser = new Parser(lexer);
        foreach (var child in parser.Program().Children)
            items.Add(ParseNode(child));
        return items;
    }

    private TreeViewItem ParseNode(Node node)
    {
        if (node is DescriptionNode descriptionNode)
        {
            var children = (from i in descriptionNode.Identifiers select ParseNode(i)).ToList();
            children.Add(ParseNode(descriptionNode.Type));
            var item = new TreeViewItem(
                nameof(DescriptionNode),
                children
            );
            return item;
        } 
        if (node is IdentifierNode identifierNode)
        {
            return new TreeViewItem($"{nameof(IdentifierNode)}: {identifierNode.Token.Lexema}");
        }
        if (node is TypeNode typeNode)
        {
            return new TreeViewItem($"{nameof(TypeNode)}: {typeNode.Token.Lexema}");
        }
        throw new InvalidOperationException();
    }
}
