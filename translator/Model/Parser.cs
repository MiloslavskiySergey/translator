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

    public ProgramNode Program()
    {
        Scan();
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
                children.Add(Operator());
            if (IsSeparator("\n"))
                Scan();
            else
                throw new ParserException();
        }
        if (IsKeyWord("end"))
            Scan();
        return new ProgramNode(children);
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
        return new DescriptionNode(identifiers, Type());
            
    }

    private OperatorNode Operator()
    {
        return new OperatorNode();
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
}

public class Node { }

public class ProgramNode : Node
{
    public List<Node> Children { get; } = new();
    public ProgramNode(List<Node> children)
    {
        Children = children;
    }
}

public class DescriptionNode : Node
{
    public List<IdentifierNode> Identifiers { get; }
    public TypeNode Type { get; }
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

public class TypeNode : Node
{
    public Token Token { get; private set; }
    public TypeNode(Token token)
    {
        Token = token;
    }
}

public class OperatorNode : Node
{

}

public class ParserException : Exception { }
