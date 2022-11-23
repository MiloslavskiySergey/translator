namespace translator.Model;

public class Parser
{
    public BlockNode Parse()
    {
        return Program();
    }

    private readonly Lexer _lexer;
    private TokenPosition? _current = null;

    private TokenPosition TokenPosition
    {
        get
        {
            if (_current is null)
                throw Error("Неожиданное окончание программы");
            return _current!;
        }
    }

    public Parser(Lexer lexer)
    {
        _lexer = lexer;
    }

    private BlockNode Program()
    {
        Scan();
        var block = Block();
        KeyWordSafe("end");
        return block;
    }

    private void Scan()
    {
        _current = _lexer.Scan();
    }

    private BlockNode Block()
    {
        var position = TokenPosition.Position;
        var children = new List<Node>();
        while (
            IsKeyWord("dim") ||
            IsIdentifier() ||
            IsKeyWord("if") ||
            IsKeyWord("for") ||
            IsKeyWord("while") ||
            IsKeyWord("read") ||
            IsKeyWord("write")
        )
        {
            if (IsKeyWord("dim"))
                children.Add(Description());
            else
                children.AddRange(Operator());
        }
        return new BlockNode(children, position);
    }

    private DescriptionNode Description()
    {
        var position = TokenPosition.Position;
        Scan();
        var identifiers = ReadIdentifiers();
        var type = TypeSafe();
        SeparatorSafe(";");
        return new DescriptionNode(identifiers, type, position);
    }

    private List<Node> Operator()
    {
        var operators = new List<Node>();
        while (
            IsIdentifier() ||
            IsKeyWord("if") ||
            IsKeyWord("for") ||
            IsKeyWord("while") ||
            IsKeyWord("read") ||
            IsKeyWord("write")
        )
        {
            if (IsIdentifier())
                operators.Add(AssignmentSemicolonOperator());
            if (IsKeyWord("if"))
                operators.Add(ConditionalOperotor());
            if (IsKeyWord("for"))
                operators.Add(FixedLoopOperator());
            if (IsKeyWord("while"))
                operators.Add(ConditionalLoopOperator());
            if (IsKeyWord("read"))
                operators.Add(InputOperator());
            if (IsKeyWord("write"))
                operators.Add(OutputOperator());
        }
        return operators;
    }

    private AssignmentNode AssignmentOperator()
    {
        var position = TokenPosition.Position;
        var identifier = IdentifierSafe();
        KeyWordSafe("as");
        var expression = Expression();
        return new AssignmentNode(identifier, expression, position);
    }

    private AssignmentNode AssignmentSemicolonOperator()
    {
        var position = TokenPosition.Position;
        var identifier = Identifier();
        KeyWordSafe("as");
        var expression = Expression();
        SeparatorSafe(";");
        return new AssignmentNode(identifier, expression, position);
    }

    private Node Expression()
    {
        var position = TokenPosition.Position;
        var leftOperand = Operand();
        while (IsRelationGroupOperation())
        {
            var operatorTokenPosition = TokenPosition;
            Scan();
            var rightOperand = Operand();
            leftOperand = new BinaryOperationNode(
                new OperatorNode(operatorTokenPosition.Token.Lexema, operatorTokenPosition.Position),
                leftOperand,
                rightOperand,
                position
            );
        }
        return leftOperand;
    }

    private Node Operand()
    {
        var position = TokenPosition.Position;
        var leftOperand = Term();
        while (IsAdditionGroupOperation())
        {
            var operatorTokenPosition = TokenPosition;
            Scan();
            var rightOperand = Term();
            leftOperand = new BinaryOperationNode(
                new OperatorNode(operatorTokenPosition.Token.Lexema, operatorTokenPosition.Position),
                leftOperand,
                rightOperand,
                position
            );
        }
        return leftOperand;
    }

    private Node Term()
    {
        var position = TokenPosition.Position;
        var leftOperand = Factor();
        while (IsMultiplicationGroupOperation())
        {
            var operatorTokenPosition = TokenPosition;
            Scan();
            var rightOperand = Factor();
            leftOperand = new BinaryOperationNode(
                new OperatorNode(operatorTokenPosition.Token.Lexema, operatorTokenPosition.Position),
                leftOperand,
                rightOperand,
                position
            );
        }
        return leftOperand;
    }

    private Node Factor()
    {
        var position = TokenPosition.Position;
        var leftOperand = PowerSafe();
        if (IsPowerOperation())
        {
            var operatorTokenPosition = TokenPosition;
            Scan();
            var rightOperand = Factor();
            return new BinaryOperationNode(
                new OperatorNode(operatorTokenPosition.Token.Lexema, operatorTokenPosition.Position),
                leftOperand,
                rightOperand,
                position
            );
        }
        return leftOperand;
    }

    private Node PowerSafe()
    {
        if (IsIdentifier())
            return Identifier();
        if (IsIntegerConstant())
            return IntegerConstant();
        if (IsFloatConstant())
            return FloatConstant();
        if (IsStringConstant())
            return StringConstant();
        if (IsBoolConstant())
            return BoolConstant();
        if (IsUnaryOperation())
            return UnaryOperation();
        if (IsSeparator("("))
            return ParenthesizedExpression();
        throw Error("Ожидалось выражение");
    }

    private IntegerConstantNode IntegerConstant()
    {
        var position = TokenPosition.Position;
        var token = (IntegerConstantToken)TokenPosition.Token;
        Scan();
        return new IntegerConstantNode(token.Value, position);
    }

    private FloatConstantNode FloatConstant()
    {
        var position = TokenPosition.Position;
        var token = (FloatConstantToken)TokenPosition.Token;
        Scan();
        return new FloatConstantNode(token.Value, position);
    }

    private StringConstantNode StringConstant()
    {
        var position = TokenPosition.Position;
        var token = (StringConstantToken)TokenPosition.Token;
        Scan();
        return new StringConstantNode(token.Value, position);
    }

    private BoolConstantNode BoolConstant()
    {
        var position = TokenPosition.Position;
        var token = (BoolConstantToken)TokenPosition.Token;
        Scan();
        return new BoolConstantNode(token.Value, position);
    }

    private UnaryOperationNode UnaryOperation()
    {
        var operatorTokenPosition = TokenPosition;
        Scan();
        var operand = PowerSafe();
        return new UnaryOperationNode(
            new OperatorNode(operatorTokenPosition.Token.Lexema, operatorTokenPosition.Position),
            operand,
            operatorTokenPosition.Position
        );
    }

    private Node ParenthesizedExpression()
    {
        Scan();
        var expession = Expression();
        SeparatorSafe(")");
        return expession;
    }

    private ConditionalBlockNode ConditionalBlock()
    {
        var position = TokenPosition.Position;
        Scan();
        var condition = Expression();
        KeyWordSafe("then");
        var body = Block();
        return new ConditionalBlockNode(condition, body, position);
    }

    private ConditionalOperotorNode ConditionalOperotor()
    {
        var position = TokenPosition.Position;
        var conditions = new List<ConditionalBlockNode>
        {
            ConditionalBlock()
        };
        BlockNode? elseBody = null;
        while (IsKeyWord("else") && elseBody is null)
        {
            Scan();
            if (IsKeyWord("if"))
                conditions.Add(ConditionalBlock());
            else
                elseBody = Block();
        }
        KeyWordSafe("endif");
        return new ConditionalOperotorNode(conditions, elseBody, position);
    }

    private FixedLoopOperatorNode FixedLoopOperator()
    {
        var position = TokenPosition.Position;
        Scan();
        var assigment = AssignmentOperator();
        KeyWordSafe("to");
        var expression = Expression();
        KeyWordSafe("do");
        var body = Block();
        KeyWordSafe("endfor");
        return new FixedLoopOperatorNode(assigment, expression, body, position);
    }

    private ConditionalLoopOperatorNode ConditionalLoopOperator()
    {
        var position = TokenPosition.Position;
        Scan();
        var expression = Expression();
        KeyWordSafe("do");
        var body = Block();
        KeyWordSafe("endwhile");
        return new ConditionalLoopOperatorNode(expression, body, position);
    }

    private InputOperatorNode InputOperator()
    {
        var position = TokenPosition.Position;
        Scan();
        SeparatorSafe("(");
        var identifiers = ReadIdentifiers();
        SeparatorSafe(")");
        SeparatorSafe(";");
        return new InputOperatorNode(identifiers, position);
    }

    private OutputOperatorNode OutputOperator()
    {
        var position = TokenPosition.Position;
        Scan();
        SeparatorSafe("(");
        var expressions = ReadExpressions();
        SeparatorSafe(")");
        SeparatorSafe(";");
        return new OutputOperatorNode(expressions, position);
    }

    private IdentifierNode IdentifierSafe()
    {
        if (IsIdentifier())
            return Identifier();
        throw Error("Ожидался идентификатор");
    }

    private IdentifierNode Identifier()
    {
        var tokenPosition = TokenPosition;
        Scan();
        return new IdentifierNode(tokenPosition.Token.Lexema, tokenPosition.Position);
    }

    private TypeNode TypeSafe()
    {
        if (IsType())
            return Type();
        throw Error("Ожидался тип");
    }

    private TypeNode Type()
    {
        var tokenPosition = TokenPosition;
        Scan();
        return new TypeNode(tokenPosition.Token.Lexema, tokenPosition.Position);
    }

    private List<IdentifierNode> ReadIdentifiers()
    {
        var identifiers = new List<IdentifierNode>();
        identifiers.Add(IdentifierSafe());
        while (IsSeparator(","))
        {
            Scan();
            identifiers.Add(IdentifierSafe());
        }
        return identifiers;
    }

    private List<Node> ReadExpressions()
    {
        var expressions = new List<Node>();
        expressions.Add(Expression());
        while (IsSeparator(","))
        {
            Scan();
            expressions.Add(Expression());
        }
        return expressions;
    }

    private void KeyWordSafe(string keyWord)
    {
        if (IsKeyWord(keyWord))
            Scan();
        else
            throw Error($"Ожидалось ключевое слово `{keyWord}`");
    }

    private void SeparatorSafe(string separator)
    {
        if (IsSeparator(separator))
            Scan();
        else
            throw Error($"Ожидался разделитель `{separator}`");
    }

    private bool IsIdentifier() => TokenPosition.Token.Type == TokenType.Identifier;
    private bool IsType() => TokenPosition.Token.Type == TokenType.Type;
    private bool IsKeyWord(string keyWord) => TokenPosition.Token.Type == TokenType.KeyWord && TokenPosition.Token.Lexema == keyWord;
    private bool IsSeparator(string separator) => TokenPosition.Token.Type == TokenType.Separator && TokenPosition.Token.Lexema == separator;
    private bool IsRelationGroupOperation() => TokenPosition.Token.Type == TokenType.RelationGroupOperation;
    private bool IsAdditionGroupOperation() => TokenPosition.Token.Type == TokenType.AdditionGroupOperation;
    private bool IsMultiplicationGroupOperation() => TokenPosition.Token.Type == TokenType.MultiplicationGroupOperation;
    private bool IsPowerOperation() => TokenPosition.Token.Type == TokenType.PowerOperation;
    private bool IsIntegerConstant() => TokenPosition.Token.Type == TokenType.IntegerConstant;
    private bool IsFloatConstant() => TokenPosition.Token.Type == TokenType.FloatConstant;
    private bool IsStringConstant() => TokenPosition.Token.Type == TokenType.StringConstant;
    private bool IsBoolConstant() => TokenPosition.Token.Type == TokenType.BoolConstant;
    private bool IsUnaryOperation() => TokenPosition.Token.Type == TokenType.UnaryOperation;

    private ParserException Error(string message)
    {
        if (_current is null)
            return new ParserException(message);
        return new ParserException(message, _current.Position);
    }
}

public class Node
{
    public ProgramPosition Position { get; set; }
    public Node(ProgramPosition position)
    {
        Position = position;
    }
}

public class BlockNode : Node
{
    public List<Node> Children { get; private set; }
    public BlockNode(List<Node> children, ProgramPosition position) : base(position)
    {
        Children = children;
    }
}

public class DescriptionNode : Node
{
    public List<IdentifierNode> Identifiers { get; private set; }
    public TypeNode Type { get; private set; }
    public DescriptionNode(List<IdentifierNode> identifiers, TypeNode type, ProgramPosition position) : base(position)
    {
        Identifiers = identifiers;
        Type = type;
    }
}

public class IdentifierNode : Node
{
    public string Name { get; private set; }
    public IdentifierNode(string name, ProgramPosition position) : base(position)
    {
        Name = name;
    }
}

public class IntegerConstantNode : Node
{
    public int Value { get; private set; }
    public IntegerConstantNode(int value, ProgramPosition position) : base(position)
    {
        Value = value;
    }
}

public class FloatConstantNode : Node
{
    public double Value { get; private set; }
    public FloatConstantNode(double value, ProgramPosition position) : base(position)
    {
        Value = value;
    }
}

public class StringConstantNode : Node
{
    public string Value { get; private set; }
    public StringConstantNode(string value, ProgramPosition position) : base(position)
    {
        Value = value;
    }
}

public class BoolConstantNode : Node
{
    public bool Value { get; private set; }
    public BoolConstantNode(bool value, ProgramPosition position) : base(position)
    {
        Value = value;
    }
}

public class TypeNode : Node
{
    public string Name { get; private set; }
    public TypeNode(string name, ProgramPosition position) : base(position)
    {
        Name = name;
    }
}

public class AssignmentNode : Node
{
    public IdentifierNode Identifier { get; }
    public Node Expression { get; }
    public AssignmentNode(IdentifierNode identifier, Node expression, ProgramPosition position) : base(position)
    {
        Identifier = identifier;
        Expression = expression;
    }
}

public class OperatorNode : Node
{
    public string Name { get; private set; }
    public OperatorNode(string name, ProgramPosition position) : base(position)
    {
        Name = name;
    }
}

public class UnaryOperationNode : Node
{
    public OperatorNode Operator { get; private set; }
    public Node Operand { get; private set; }
    public UnaryOperationNode(OperatorNode operatorNode, Node operand, ProgramPosition position) : base(position)
    {
        Operator = operatorNode;
        Operand = operand;
    }
}

public class BinaryOperationNode : Node
{
    public OperatorNode Operator { get; private set; }
    public Node LeftOperand { get; private set; }
    public Node RightOperand { get; private set; }
    public BinaryOperationNode(OperatorNode operatorNode, Node leftOperand, Node rightOperand, ProgramPosition position) : base(position)
    {
        Operator = operatorNode;
        LeftOperand = leftOperand;
        RightOperand = rightOperand;
    }
}

public class ConditionalBlockNode : Node
{
    public Node Condition { get; private set; }
    public BlockNode Body { get; private set; }
    public ConditionalBlockNode(Node condition, BlockNode body, ProgramPosition position) : base(position)
    {
        Condition = condition;
        Body = body;
    }
}

public class ConditionalOperotorNode : Node
{
    public List<ConditionalBlockNode> Conditions { get; private set; }
    public BlockNode? ElseBody { get; private set; }
    public ConditionalOperotorNode(List<ConditionalBlockNode> conditions, BlockNode? elseBody, ProgramPosition position) : base(position)
    {
        Conditions = conditions;
        ElseBody = elseBody;
    }
}

public class FixedLoopOperatorNode : Node
{
    public AssignmentNode Assignment { get; private set; }
    public Node Expression { get; private set; }
    public BlockNode Body { get; private set; }
    public FixedLoopOperatorNode(AssignmentNode assignment, Node expression, BlockNode body, ProgramPosition position) : base(position)
    {
        Assignment = assignment;
        Expression = expression;
        Body = body;
    }
}

public class ConditionalLoopOperatorNode : Node
{
    public Node Expression { get; private set; }
    public BlockNode Body { get; private set; }
    public ConditionalLoopOperatorNode(Node expression, BlockNode body, ProgramPosition position) : base(position)
    {
        Expression = expression;
        Body = body;
    }
}

public class InputOperatorNode : Node
{
    public List<IdentifierNode> Identifiers { get; private set; }
    public InputOperatorNode(List<IdentifierNode> identifiers, ProgramPosition position) : base(position)
    {
        Identifiers = identifiers;
    }
}

public class OutputOperatorNode : Node
{
    public List<Node> Expressions { get; private set; }
    public OutputOperatorNode(List<Node> expressions, ProgramPosition position) : base(position)
    {
        Expressions = expressions;
    }
}

public class ParserException : Exception {
    public ParserException(string message) : base(message) { }
    public ParserException(string message, ProgramPosition position): base($"{message} - {position}") { }
}
