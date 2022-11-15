namespace translator.Model;

public class Parser
{
    private readonly Lexer _lexer;
    private TokenPosition? _current = null;

    private Token Token
    {
        get 
        {
            if (_current is null)
                throw new ParserException();
            return _current.Token;
        }
    }

    public Parser(Lexer lexer)
    {
        _lexer = lexer;
    }

    public void Scan()
    {
        _current = _lexer.Scan();
    }

    public BlockNode Program()
    {
        Scan();
        var block = Block();
        if (IsKeyWord("end"))
            Scan();
        else
            throw new ParserException();
        return block;
    }

    private BlockNode Block()
    {
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
        return new BlockNode(children);
    }

    private DescriptionNode Description()
    {
        Scan();
        var identifiers = ReadIdentifiers();
        if (!IsType())
            throw new ParserException();
        var type = Type();
        if (IsSeparator(";"))
            Scan();
        else
            throw new ParserException();
        return new DescriptionNode(identifiers, type);
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
        var identifier = Identifier();
        if (IsKeyWord("ass"))
            Scan();
        else
            throw new ParserException();
        var expression = Expression();
        return new AssignmentNode(identifier, expression);
    }

    private AssignmentNode AssignmentSemicolonOperator()
    {
        var identifier = Identifier();
        if (IsKeyWord("ass"))
            Scan();
        else
            throw new ParserException();
        var expression = Expression();
        if (IsSeparator(";"))
            Scan();
        else
            throw new ParserException();
        return new AssignmentNode(identifier, expression);
    }

    private Node Expression()
    {
        var leftOperand = Operand();
        if (IsRelationGroupOperation())
        {
            var token = Token;
            Scan();
            var rightOperand = Operand();
            return new BinaryOperationNode(new OperatorNode(token.Lexema), leftOperand, rightOperand);
        }
        return leftOperand;
    }

    private Node Operand()
    {
        var leftOperand = Term();
        if (IsAdditionGroupOperation())
        {
            var token = Token;
            Scan();
            var rightOperand = Term();
            return new BinaryOperationNode(new OperatorNode(token.Lexema), leftOperand, rightOperand);
        }
        return leftOperand;
    }

    private Node Term()
    {
        var leftOperand = Factor();
        if (IsMultiplicationGroupOperation())
        {
            var token = Token;
            Scan();
            var rightOperand = Factor();
            return new BinaryOperationNode(new OperatorNode(token.Lexema), leftOperand, rightOperand);
        }
        return leftOperand;
    }

    private Node Factor()
    {
        if (IsIdentifier())
            return Identifier();
        if (IsIntegerNumber())
            return IntegerNumber();
        if (IsFloatNumber())
            return FloatNumber();
        if (IsBoolConstant())
            return BoolConstant();
        if (IsUnaryOperation())
            return UnaryOperation();
        if (IsSeparator("("))
            return ParenthesizedExpression();
        throw new ParserException();
    }
   
    private IntegerNumberNode IntegerNumber()
    {
        var token = (IntegerNumberToken)Token;
        Scan();
        return new IntegerNumberNode(token.Value);
    }

    private FloatNumberNode FloatNumber()
    {
        var token = (FloatNumberToken)Token;
        Scan();
        return new FloatNumberNode(token.Value);
    }

    private BoolConstantNode BoolConstant()
    {
        var token = (BoolToken)Token;
        Scan();
        return new BoolConstantNode(token.Value);
    }

    private UnaryOperationNode UnaryOperation()
    {
        var token = Token;
        Scan();
        var operand = Factor();
        return new UnaryOperationNode(new OperatorNode(token.Lexema), operand);
    }

    private Node ParenthesizedExpression()
    {
        Scan();
        var expession = Expression();
        if (IsSeparator(")"))
            Scan();
        else
            throw new ParserException();
        return expession;
    }

    private ConditionalOperotorNode ConditionalOperotor()
    {
        Scan();
        var condition = Expression();
        if (IsKeyWord("then"))
            Scan();
        else
            throw new ParserException();
        var trueBody = Block();
        BlockNode? falseBody = null;
        if (IsKeyWord("else"))
        {
            Scan();
            falseBody = Block();
        }
        if (IsKeyWord("endif"))
            Scan();
        else
            throw new ParserException();
        return new ConditionalOperotorNode(condition, trueBody, falseBody);
    }

    private FixedLoopOperatorNode FixedLoopOperator()
    {
        Scan();
        var assigment = AssignmentOperator();
        if (IsKeyWord("to"))
            Scan();
        else
            throw new ParserException();
        var expression = Expression();
        if (IsKeyWord("do"))
            Scan();
        else
            throw new ParserException();
        var body = Block();
        if (IsKeyWord("endfor"))
            Scan();
        else
            throw new ParserException();
        return new FixedLoopOperatorNode(assigment, expression, body);
    }

    private ConditionalLoopOperatorNode ConditionalLoopOperator()
    {
        Scan();
        var expression = Expression();
        if (IsKeyWord("do"))
            Scan();
        else
            throw new ParserException();
        var body = Block();
        if (IsKeyWord("endwhile"))
            Scan();
        else
            throw new ParserException();
        return new ConditionalLoopOperatorNode(expression, body);
    }

    private InputOperatorNode InputOperator()
    {
        Scan();
        if (IsSeparator("("))
            Scan();
        else
            throw new ParserException();
        var identifiers = ReadIdentifiers();
        if (IsSeparator(")"))
            Scan();
        else
            throw new ParserException();
        if (IsSeparator(";"))
            Scan();
        else
            throw new ParserException();
        return new InputOperatorNode(identifiers);
    }

    private OutputOperatorNode OutputOperator()
    {
        return new OutputOperatorNode();
    }

    private IdentifierNode Identifier()
    {
        var token = Token; 
        Scan();
        return new IdentifierNode(token.Lexema);
    }

    private TypeNode Type()
    {
        var token = Token;
        Scan();
        return new TypeNode(token.Lexema);
    }

    private List<IdentifierNode> ReadIdentifiers()
    {
        var identifiers = new List<IdentifierNode>();
        if (IsIdentifier())
            identifiers.Add(Identifier());
        else
            throw new ParserException();
        while (IsSeparator(","))
        {
            Scan();
            if (IsIdentifier())
                identifiers.Add(Identifier());
            else
                throw new ParserException();
        }
        return identifiers;
    }

    private bool IsIdentifier()
    {
        return Token.Type == TokenType.Identifier;
    }

    private bool IsType()
    {
        return Token.Type == TokenType.Type;
    }

    private bool IsKeyWord(string keyWord)
    {
        return Token.Type == TokenType.KeyWord && Token.Lexema == keyWord;
    }

    private bool IsSeparator(string separator)
    {
        return Token.Type == TokenType.Separator && Token.Lexema == separator;
    }

    private bool IsRelationGroupOperation()
    {
        return Token.Type == TokenType.RelationGroupOperation;
    }

    private bool IsAdditionGroupOperation()
    {
        return Token.Type == TokenType.AdditionGroupOperation;
    }

    private bool IsMultiplicationGroupOperation()
    {
        return Token.Type == TokenType.MultiplicationGroupOperation;
    }

    private bool IsIntegerNumber()
    {
        return Token.Type == TokenType.IntegerNumber;
    }

    private bool IsFloatNumber()
    {
        return Token.Type == TokenType.FloatNumber;
    }

    private bool IsBoolConstant()
    {
        return Token.Type == TokenType.BoolConstant;
    }

    private bool IsUnaryOperation()
    {
        return Token.Type == TokenType.UnaryOperation;
    }
}

public class Node { }

public class BlockNode : Node
{
    public List<Node> Children { get; } = new();
    public BlockNode(List<Node> children)
    {
        Children = children;
    }
}

public class DescriptionNode : Node
{
    public List<IdentifierNode> Identifiers { get; private set; }
    public TypeNode Type { get; private set; }
    public DescriptionNode(List<IdentifierNode> identifiers, TypeNode type)
    {
        Identifiers = identifiers;
        Type = type;
    }
}

public class IdentifierNode : Node
{
    public string Name { get; private set; }
    public IdentifierNode(string name)
    {
        Name = name;
    }
}

public class IntegerNumberNode : Node
{
    public int Value { get; private set; }
    public IntegerNumberNode(int value)
    {
        Value = value;
    }
}

public class FloatNumberNode : Node
{
    public double Value { get; private set; }
    public FloatNumberNode(double value)
    {
        Value = value;
    }
}

public class BoolConstantNode : Node
{
    public bool Value { get; private set; }
    public BoolConstantNode(bool value)
    {
        Value = value;
    }
}

public class TypeNode : Node
{
    public string Name { get; private set; }
    public TypeNode(string name)
    {
        Name = name;
    }
}

public class AssignmentNode : Node
{
    public IdentifierNode Identifier { get; }
    public Node Expression { get; }
    public AssignmentNode(IdentifierNode identifier, Node expression)
    {
        Identifier = identifier;
        Expression = expression;
    }
}

public class OperatorNode : Node
{
    public string Name { get; private set; }
    public OperatorNode(string name)
    {
        Name = name;
    }
}

public class UnaryOperationNode : Node
{
    public OperatorNode Operator { get; private set; }
    public Node Operand { get; private set; }
    public UnaryOperationNode(OperatorNode operatorNode, Node operand)
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
    public BinaryOperationNode(OperatorNode operatorNode, Node leftOperand, Node rightOperand)
    {
        Operator = operatorNode;
        LeftOperand = leftOperand;
        RightOperand = rightOperand;
    }
}

public class ConditionalOperotorNode : Node
{
    public Node Condition { get; private set; }
    public BlockNode TrueBody { get; private set; }
    public BlockNode? FalseBody { get; private set; }
    public ConditionalOperotorNode(Node condition, BlockNode trueBody, BlockNode? falseBody)
    {
        Condition = condition;
        TrueBody = trueBody;
        FalseBody = falseBody;
    }
}

public class FixedLoopOperatorNode : Node
{
    public AssignmentNode Assignment { get; private set; }
    public Node Expression { get; private set; }
    public BlockNode Body { get; private set; }
    public FixedLoopOperatorNode(AssignmentNode assignment, Node expression, BlockNode body)
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
    public ConditionalLoopOperatorNode(Node expression, BlockNode body)
    {
        Expression = expression;
        Body = body;
    }
}

public class InputOperatorNode : Node
{
    public List<IdentifierNode> Identifiers { get; private set; }
    public InputOperatorNode(List<IdentifierNode> identifiers)
    {
        Identifiers = identifiers;
    }
}

public class OutputOperatorNode : Node { }

public class ParserException : Exception { }
