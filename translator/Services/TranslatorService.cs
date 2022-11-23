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
    public ICommand GenerateCommand { get; private set; }

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
        GenerateCommand = new Command(() =>
        {
            var intermediateCodeWindow = new Window(new IntermediateCodePage());
            Application.Current!.OpenWindow(intermediateCodeWindow);
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
            if (token is IntegerConstantToken integerToken)
                value = integerToken.Value;
            else if (token is FloatConstantToken floatToken)
                value = floatToken.Value;
            else if (token is BoolConstantToken boolToken)
                value = boolToken.Value;
            else if (token is StringConstantToken stringToken)
                value = stringToken.Value;
            var lexema = token.Lexema;
            if (lexema == "\n")
                lexema = "new line";
            tokenItems.Add(
                new TokensTableItem(
                    i,
                    token.Type,
                    lexema,
                    value,
                    tokenPosition.Position.Line + 1,tokenPosition.Position.Position + 1
                )
            );
            i++;
        }
        return tokenItems;
    }

    public IEnumerable<TreeViewItem> Parse()
    {
        var lexer = new Lexer(Program);
        var parser = new Parser(lexer);
        return new List<TreeViewItem>
        {
            ParseNode(parser.Parse())
        };
    }

    public string GenerateIntermediateCode()
    {
        var result = "";
        var lexer = new Lexer(Program);
        var parser = new Parser(lexer);
        var generator = new IntermediateCodeGenerator(parser);
        generator.Emit += (line) => {
            result += $"{line}\n";
        };
        generator.Generate();
        return result;
    }

    private TreeViewItem ParseNode(Node node)
    {
        if (node is BlockNode blockNode)
        {
            var children = (from i in blockNode.Children select ParseNode(i)).ToList();
            return new TreeViewItem(
                nameof(BlockNode),
                children
            );
        }
        if (node is DescriptionNode descriptionNode)
        {
            var indentifiers = (from i in descriptionNode.Identifiers select ParseNode(i)).ToList();
            var types = new List<TreeViewItem>
            {
                ParseNode(descriptionNode.Type)
            };
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Identifiers", indentifiers),
                new TreeViewItem("Type", types),
            };
            return new TreeViewItem(
                nameof(DescriptionNode),
                children
            );
        }
        if (node is IdentifierNode identifierNode)
        {
            return new TreeViewItem($"{nameof(IdentifierNode)}: {identifierNode.Name}");
        }
        if (node is IntegerConstantNode integerConstantNode)
        {
            return new TreeViewItem($"{nameof(IntegerConstantNode)}: {integerConstantNode.Value}");
        }
        if (node is FloatConstantNode floatConstantNode)
        {
            return new TreeViewItem($"{nameof(FloatConstantNode)}: {floatConstantNode.Value}");
        }
        if (node is StringConstantNode stringConstantNode)
        {
            return new TreeViewItem($"{nameof(StringConstantNode)}: \"{stringConstantNode.Value}\"");
        }
        if (node is BoolConstantNode boolConstantNode)
        {
            return new TreeViewItem($"{nameof(BoolConstantNode)}: {boolConstantNode.Value}");
        }
        if (node is TypeNode typeNode)
        {
            return new TreeViewItem($"{nameof(TypeNode)}: {typeNode.Name}");
        }
        if (node is AssignmentNode assignmentOperatorNode)
        {
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Identifier", new List<TreeViewItem>
                {
                    ParseNode(assignmentOperatorNode.Identifier)
                }),
                new TreeViewItem("Expression", new List<TreeViewItem>
                {
                    ParseNode(assignmentOperatorNode.Expression)
                }),
            };
            return new TreeViewItem(
                nameof(AssignmentNode),
                children
            );
        }
        if (node is OperatorNode operatorNode)
        {
            return new TreeViewItem($"{nameof(OperatorNode)}: {operatorNode.Name}");
        }
        if (node is UnaryOperationNode unaryOperationNode)
        {
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Operator", new List<TreeViewItem>
                {
                    ParseNode(unaryOperationNode.Operator)
                }),
                new TreeViewItem("Operand", new List<TreeViewItem>
                {
                    ParseNode(unaryOperationNode.Operand)
                }),
            };
            return new TreeViewItem(
                nameof(UnaryOperationNode),
                children
            );
        }
        if (node is BinaryOperationNode binaryOperationNode)
        {
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Operator", new List<TreeViewItem>
                {
                    ParseNode(binaryOperationNode.Operator)
                }),
                new TreeViewItem("LeftOperand", new List<TreeViewItem>
                {
                    ParseNode(binaryOperationNode.LeftOperand)
                }),
                new TreeViewItem("RightOperand", new List<TreeViewItem>
                {
                    ParseNode(binaryOperationNode.RightOperand)
                }),
            };
            return new TreeViewItem(
                nameof(BinaryOperationNode),
                children
            );
        }
        if (node is ConditionalBlockNode conditionalBlockNode)
        {
            var children = new List<TreeViewItem>()
            {
                new TreeViewItem("Condition", new List<TreeViewItem>
                {
                    ParseNode(conditionalBlockNode.Condition)
                }),
                new TreeViewItem("Body", new List<TreeViewItem>
                {
                    ParseNode(conditionalBlockNode.Body)
                }),
            };
            return new TreeViewItem(
                nameof(ConditionalLoopOperatorNode),
                children
            );
        }
        if (node is ConditionalOperotorNode conditionalOperotorNode)
        {
            var conditions = (from c in conditionalOperotorNode.Conditions select ParseNode(c)).ToList();
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Conditions", conditions),
            };
            if (conditionalOperotorNode.ElseBody is not null)
            {
                children.Add(new TreeViewItem("ElseBody", new List<TreeViewItem> {
                     ParseNode(conditionalOperotorNode.ElseBody)
                }));
            }
            return new TreeViewItem(
                nameof(ConditionalOperotorNode),
                children
            );
        }
        if (node is FixedLoopOperatorNode fixedLoopOperatorNode)
        {
            var children = new List<TreeViewItem>()
            {
                new TreeViewItem("Assignment", new List<TreeViewItem>
                {
                    ParseNode(fixedLoopOperatorNode.Assignment)
                }),
                new TreeViewItem("Expression", new List<TreeViewItem>
                {
                    ParseNode(fixedLoopOperatorNode.Expression)
                }),
                new TreeViewItem("Body", new List<TreeViewItem>
                {
                    ParseNode(fixedLoopOperatorNode.Body)
                }),
            };
            return new TreeViewItem(
                nameof(FixedLoopOperatorNode),
                children
            );
        }
        if (node is ConditionalLoopOperatorNode conditionalLoopOperatorNode)
        {
            var children = new List<TreeViewItem>()
            {
                new TreeViewItem("Expression", new List<TreeViewItem>
                {
                    ParseNode(conditionalLoopOperatorNode.Expression)
                }),
                new TreeViewItem("Body", new List<TreeViewItem>
                {
                    ParseNode(conditionalLoopOperatorNode.Body)
                }),
            };
            return new TreeViewItem(
                nameof(ConditionalLoopOperatorNode),
                children
            );
        }
        if (node is InputOperatorNode inputOperatorNode)
        {
            var indentifiers = (from i in inputOperatorNode.Identifiers select ParseNode(i)).ToList();
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Identifiers", indentifiers),
            };
            return new TreeViewItem(
                nameof(InputOperatorNode),
                children
            );
        }
        if (node is OutputOperatorNode outputOperatorNode)
        {
            var expressions = (from e in outputOperatorNode.Expressions select ParseNode(e)).ToList();
            var children = new List<TreeViewItem>
            {
                new TreeViewItem("Expressions", expressions),
            };
            return new TreeViewItem(
                nameof(OutputOperatorNode),
                children
            );
        }
        throw new InvalidOperationException();
    }
}
