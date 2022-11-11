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
                operators.Add(AssignmentOperator());
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

    private AssignmentOperatorNode AssignmentOperator()
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
        return new AssignmentOperatorNode(identifier, expression);
    }

    private Node Expression()
    {
        var leftOperand = Operand();
        if (IsRelationGroupOperation())
        {
            var token = Token;
            Scan();
            var rightOperand = Operand();
            return new BinaryOperationNode(token, leftOperand, rightOperand);
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
            return new BinaryOperationNode(token, leftOperand, rightOperand);
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
            return new BinaryOperationNode(token, leftOperand, rightOperand);
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
        return new IntegerNumberNode(token);
    }

    private FloatNumberNode FloatNumber()
    {
        var token = (FloatNumberToken)Token;
        Scan();
        return new FloatNumberNode(token);
    }

    private BoolConstantNode BoolConstant()
    {
        var token = Token;
        Scan();
        return new BoolConstantNode(token);
    }

    private UnaryOperationNode UnaryOperation()
    {
        var token = Token;
        Scan();
        var operand = Factor();
        return new UnaryOperationNode(token, operand);
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
        return new FixedLoopOperatorNode();
    }

    private ConditionalLoopOperatorNode ConditionalLoopOperator()
    {
        return new ConditionalLoopOperatorNode();
    }

    private InputOperatorNode InputOperator()
    {
        return new InputOperatorNode();
    }

    private OutputOperatorNode OutputOperator()
    {
        return new OutputOperatorNode();
    }

    private IdentifierNode Identifier()
    {
        var token = Token; 
        Scan();
        return new IdentifierNode(token);
    }

    private TypeNode Type()
    {
        var token = Token;
        Scan();
        return new TypeNode(token);
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
    public Token Token { get; private set; }
    public IdentifierNode(Token token)
    {
        Token = token;
    }
}

public class IntegerNumberNode : Node
{
    public IntegerNumberToken Token { get; private set; }
    public IntegerNumberNode(IntegerNumberToken token)
    {
        Token = token;
    }
}

public class FloatNumberNode : Node
{
    public FloatNumberToken Token { get; private set; }
    public FloatNumberNode(FloatNumberToken token)
    {
        Token = token;
    }
}

public class BoolConstantNode : Node
{
    public Token Token { get; private set; }
    public BoolConstantNode(Token token)
    {
        Token = token;
    }
}

public class TypeNode : Node
{
    public Token Token { get; private set; }
    public TypeNode(Token token)
    {
        Token = token;
    }
}

public class AssignmentOperatorNode : Node
{
    public IdentifierNode Identifier { get; }
    public Node Expression { get; }
    public AssignmentOperatorNode(IdentifierNode identifier, Node expression)
    {
        Identifier = identifier;
        Expression = expression;
    }
}

public class UnaryOperationNode : Node
{
    public Token Token { get; private set; }
    public Node Operand { get; private set; }
    public UnaryOperationNode(Token token, Node operand)
    {
        Token = token;
        Operand = operand;
    }
}

public class BinaryOperationNode : Node
{
    public Token Token { get; private set; }
    public Node LeftOperand { get; private set; }
    public Node RightOperand { get; private set; }
    public BinaryOperationNode(Token token, Node leftOperand, Node rightOperand)
    {
        Token = token;
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
}

public class ConditionalLoopOperatorNode : Node { }

public class InputOperatorNode : Node { }

public class OutputOperatorNode : Node { }

public class ParserException : Exception { }
