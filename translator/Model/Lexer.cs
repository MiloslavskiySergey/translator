using System.Collections;

namespace translator.Model;

/// <summary>
/// Класс лексера
/// </summary>
public class Lexer : IEnumerable<TokenPosition>
{
    /// <summary>
    /// Конструктор лексера
    /// </summary>
    /// <param name="program"></param>
    public Lexer(string program)
    {
        _program = program;
        _numberParsers = new NumberParser[]
        {
            new NumberParser(2, 'b'),
            new NumberParser(8, 'o'),
            new NumberParser(10, 'd', false),
            new NumberParser(16, 'h'),
        };
        _postfixes = (from p in _numberParsers select p.Postfix).ToArray();
    }

    /// <summary>
    /// Получение перечислителя токенов
    /// </summary>
    /// <returns></returns>
    public IEnumerator<TokenPosition> GetEnumerator()
    {
        var token = Scan();
        while (token != null)
        {
            yield return token;
            token = Scan();
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Метод для получения очередного токена
    /// </summary>
    /// <returns></returns>
    public TokenPosition? Scan()
    {
        if (_pointer == _program.Length)
            return null;
        while (_program[_pointer] == ' ' || _program[_pointer] == '\t' || _program[_pointer] == '\n')
        {
            if (!Skip())
                return null;
        }
        var commentDeleted = true;
        while (commentDeleted && _pointer + 1 < _program.Length)
        {
            if (_program[_pointer..(_pointer + 2)] == "//")
            {
                do
                {
                    if (!Skip())
                        return null;
                } while (_program[_pointer] != '\n');
                if (!Skip())
                    return null;
                continue;
            }
            if (_program[_pointer..(_pointer + 2)] == "/*")
            {
                do
                {
                    Skip();
                    if (_pointer + 1 >= _program.Length)
                        throw new LexerException();
                } while (_program[_pointer..(_pointer + 2)] != "*/");
                Skip();
                Skip();
                if (_program[_pointer] == '\n' && !Skip())
                    return null;
                continue;
            }
            commentDeleted = false;
        }
        var pointerStart = _pointer;
        if (char.IsDigit(_program[_pointer]))
        {
            var lexema = CollectLexema(c =>
                c == '.' ||
                NumberParser.StandardAlphabet.ContainsKey(char.ToLower(c)) ||
                _postfixes.Contains(char.ToLower(c))
            );
            var oldPosition = _position;
            _position += _pointer - pointerStart;
            if (LiteralsTokens.ContainsKey(lexema))
                return new TokenPosition(LiteralsTokens[lexema], _line, oldPosition);
            foreach (var parser in _numberParsers)
            {
                if (parser.TryParse(lexema.ToLower(), out var token))
                {
                    LiteralsTokens.Add(lexema, token!);
                    return new TokenPosition(LiteralsTokens[lexema], _line, oldPosition);
                }
            }
            throw new LexerException();
        }
        else if (char.IsLetter(_program[_pointer]))
        {
            var lexema = CollectLexema(c => char.IsLetter(c) || char.IsDigit(c));
            var oldPosition = _position;
            _position += _pointer - pointerStart;
            if (LetterTokens.ContainsKey(lexema))
                return new TokenPosition(LetterTokens[lexema], _line, oldPosition);
            if (IdentifiersTokens.ContainsKey(lexema))
                return new TokenPosition(IdentifiersTokens[lexema], _line, oldPosition);
            IdentifiersTokens.Add(lexema, new Token(TokenType.Identifier, lexema));
            return new TokenPosition(IdentifiersTokens[lexema], _line, oldPosition);
        }
        else if (_program[_pointer] == '"')
        {
            var lexema = CollectLexema(1) + CollectLexema(c => c != '"') + CollectLexema(1);
            var oldPosition = _position;
            _position += _pointer - pointerStart;
            if (LiteralsTokens.ContainsKey(lexema))
                return new TokenPosition(LiteralsTokens[lexema], _line, oldPosition);
            LiteralsTokens.Add(lexema, new StringToken(lexema, lexema[1..^1]));
            return new TokenPosition(LiteralsTokens[lexema], _line, oldPosition);
        }
        else
        {
            for (var count = 2; count >= 1; count--)
            {
                var lexema = CollectLexema(count);
                if (SpecialSymbolsTokens.ContainsKey(lexema))
                {
                    var oldPosition = _position;
                    _position += _pointer - pointerStart;
                    return new TokenPosition(SpecialSymbolsTokens[lexema], _line, oldPosition);
                }
                _pointer -= count;
            }
        }
        throw new LexerException();
    }

    public static Dictionary<string, Token> LetterTokens { get; } = new Dictionary<string, Token>()
    {
        { "or", new Token(TokenType.AdditionGroupOperation, "or") },
        { "and", new Token(TokenType.MultiplicationGroupOperation, "and") },
        { "not", new Token(TokenType.UnaryOperation, "not") },
        { "true", new BoolToken("true", true) },
        { "false", new BoolToken("false", false) },
        { "dim", new Token(TokenType.KeyWord, "dim") },
        { "ass", new Token(TokenType.KeyWord, "ass") },
        { "if", new Token(TokenType.KeyWord, "if") },
        { "then", new Token(TokenType.KeyWord, "then") },
        { "else", new Token(TokenType.KeyWord, "else") },
        { "for", new Token(TokenType.KeyWord, "for") },
        { "to", new Token(TokenType.KeyWord, "to") },
        { "do", new Token(TokenType.KeyWord, "do") },
        { "while", new Token(TokenType.KeyWord, "while") },
        { "read", new Token(TokenType.KeyWord, "read") },
        { "write", new Token(TokenType.KeyWord, "write") },
        { "endif", new Token(TokenType.KeyWord, "endif") },
        { "endfor", new Token(TokenType.KeyWord, "endfor") },
        { "endwhile", new Token(TokenType.KeyWord, "endwhile") },
        { "end", new Token(TokenType.KeyWord, "end") },
    };
    public static Dictionary<string, Token> SpecialSymbolsTokens { get; } = new Dictionary<string, Token>()
    {
        { "<>", new Token(TokenType.RelationGroupOperation, "<>") },
        { "=", new Token(TokenType.RelationGroupOperation, "=") },
        { "<", new Token(TokenType.RelationGroupOperation, "<") },
        { "<=", new Token(TokenType.RelationGroupOperation, "<=") },
        { ">", new Token(TokenType.RelationGroupOperation, ">") },
        { ">=", new Token(TokenType.RelationGroupOperation, ">=") },
        { "+", new Token(TokenType.AdditionGroupOperation, "+") },
        { "-", new Token(TokenType.AdditionGroupOperation, "-") },
        { "*", new Token(TokenType.MultiplicationGroupOperation, "*") },
        { "/", new Token(TokenType.MultiplicationGroupOperation, "/") },
        { "(", new Token(TokenType.Separator, "(") },
        { ")", new Token(TokenType.Separator, ")") },
        { ",", new Token(TokenType.Separator, ",") },
        { ";", new Token(TokenType.Separator, ";") },
        { "%", new Token(TokenType.Type, "%") },
        { "!", new Token(TokenType.Type, "!") },
        { "$", new Token(TokenType.Type, "$") },
    };
    public Dictionary<string, Token> LiteralsTokens { get; } = new Dictionary<string, Token>();
    public Dictionary<string, Token> IdentifiersTokens { get; } = new Dictionary<string, Token>();

    private bool Skip()
    {
        if (_program[_pointer] == '\n')
        {
            MoveToNextLine();
        }
        else
        {
            _pointer++;
            _position++;
        }
        return _pointer < _program.Length;
    }
    private void MoveToNextLine()
    {
        _pointer++;
        _line++;
        _position = 0;
    }
    private string CollectLexema(Func<char, int, bool> predicate)
    {
        var lexema = "";
        var i = 0;
        while (_pointer < _program.Length && predicate(_program[_pointer], i))
        {
            lexema += _program[_pointer];
            _pointer++;
            i++;
        };
        return lexema;
    }
    private string CollectLexema(Predicate<char> predicate)
    {
        return CollectLexema((c, _) => predicate(c));
    }
    private string CollectLexema(int count)
    {
        return CollectLexema((_, i) => i < count);
    }

    private readonly string _program;
    private int _pointer;
    private int _line = 0;
    private int _position = 0;

    private readonly NumberParser[] _numberParsers;
    private readonly char[] _postfixes;

    /// <summary>
    /// Абстрактный парсер лексера
    /// </summary>
    private abstract class Parser
    {
        /// <summary>
        /// Попытка распарсить лексему в токен
        /// </summary>
        /// <param name="lexema">Лексема</param>
        /// <param name="result">Результат парсинга (null, если не удалось распарсить)</param>
        /// <returns>true, если получилось распарсить, иначе - false</returns>
        public abstract bool TryParse(string lexema, out Token? result);
    }

    /// <summary>
    /// Парсер из лексемы в число в определенной системе счисления
    /// </summary>
    private class NumberParser : Parser
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="notation">Система счисления</param>
        /// <param name="postfix">Постфикс, например, 'b'</param>
        /// <param name="postfixRequired">Обязателен ли постфикс</param>
        public NumberParser(int notation, char postfix, bool postfixRequired = true)
        {
            Notation = notation;
            Postfix = postfix;
            PostfixRequired = postfixRequired;
            Alphabet = (from p in StandardAlphabet where p.Value < notation select p)
                .ToDictionary(p => p.Key, p => p.Value);
        }

        public override bool TryParse(string lexema, out Token? result)
        {
            if (TryParseToInteger(lexema.ToLower(), out var i))
            {
                result = new IntegerNumberToken(lexema, i);
                return true;
            }
            if (TryParseToFloat(lexema.ToLower(), out var f))
            {
                result = new FloatNumberToken(lexema, f);
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Стандартный алфавит для чисел
        /// </summary>
        public static Dictionary<char, ushort> StandardAlphabet { get; } = new Dictionary<char, ushort>
        {
            { '0', 0 },
            { '1', 1 },
            { '2', 2 },
            { '3', 3 },
            { '4', 4 },
            { '5', 5 },
            { '6', 6 },
            { '7', 7 },
            { '8', 8 },
            { '9', 9 },
            { 'a', 10 },
            { 'b', 11 },
            { 'c', 12 },
            { 'd', 13 },
            { 'e', 14 },
            { 'f', 15 },
        };

        public int Notation { get; }
        public char Postfix { get; }
        public bool PostfixRequired { get; }
        /// <summary>
        /// Сокращенный алфавит в соответствии с заданной системой счисления
        /// </summary>
        public Dictionary<char, ushort> Alphabet { get; }

        /// <summary>
        /// Попытка распарсить лексему в целое число
        /// </summary>
        /// <param name="lexema">Лексема</param>
        /// <param name="result">Результат парсинга (0, если не удалось распарсить)</param>
        /// <returns>true, если получилось распарсить, иначе - false</returns>
        public bool TryParseToInteger(string lexema, out int result)
        {
            result = 0;
            if (lexema[lexema.Length - 1] == Postfix)
                return TryParseIntegerPart(lexema[..^1], out result);
            else if (PostfixRequired)
                return false;
            return TryParseIntegerPart(lexema, out result);
        }
        /// <summary>
        /// Попытка распарсить лексему в число с плавающей точкой
        /// </summary>
        /// <param name="lexema">Лексема</param>
        /// <param name="result">Результат парсинга (0.0, если не удалось распарсить)</param>
        /// <returns>true, если получилось распарсить, иначе - false</returns>
        public bool TryParseToFloat(string lexema, out double result)
        {
            result = 0.0;
            var parts = lexema.Split('.');
            if (parts.Length != 2)
                return false;
            if (parts[1][parts[1].Length - 1] == Postfix)
                parts[1] = parts[1][..^1];
            else if (PostfixRequired)
                return false;
            if (!TryParseIntegerPart(parts[0], out var i))
                return false;
            if (!TryParseFloatPart(parts[1], out var f))
                return false;
            result = i + f;
            return true;
        }
        /// <summary>
        /// Попытка распарсить целую часть
        /// </summary>
        /// <param name="integerPart">Целая часть</param>
        /// <param name="result">Результат парсинга (0, если не удалось распарсить)</param>
        /// <returns>true, если получилось распарсить, иначе - false</returns>
        private bool TryParseIntegerPart(string integerPart, out int result)
        {
            result = 0;
            foreach (var c in integerPart)
            {
                result *= Notation;
                if (!Alphabet.TryGetValue(c, out var v))
                {
                    result = 0;
                    return false;
                }
                result += v;
            }
            return true;
        }
        /// <summary>
        /// Попытка распарсить часть после плавающей точки
        /// </summary>
        /// <param name="floatPart">Часть после плавающей точки</param>
        /// <param name="result">Результат парсинга (0.0, если не удалось распарсить)</param>
        /// <returns>true, если получилось распарсить, иначе - false</returns>
        private bool TryParseFloatPart(string floatPart, out double result)
        {
            result = 0.0;
            var factor = 1.0;
            foreach (var c in floatPart)
            {
                factor /= Notation;
                if (!Alphabet.TryGetValue(c, out var v))
                {
                    result = 0;
                    return false;
                }
                result += factor * v;
            }
            return true;
        }
    }
}

public class LexerException : Exception { }

/// <summary>
/// Позиция токена
/// </summary>
/// <param name="Token">Токен</param>
/// <param name="Line">Строка</param>
/// <param name="Position">Позиция в строке</param>
public record TokenPosition(Token Token, int Line, int Position);

/// <summary>
/// Запись токена
/// </summary>
public record Token(TokenType Type, string Lexema);

/// <summary>
/// Запись целого числа с дополнительным полем для значения числа
/// </summary>
public record IntegerNumberToken(string Lexema, int Value) : Token(TokenType.IntegerNumber, Lexema);

/// <summary>
/// Запись числа с плавающей точкой с дополнительным полем для значения числа
/// </summary>
public record FloatNumberToken(string Lexema, double Value) : Token(TokenType.FloatNumber, Lexema);

/// <summary>
/// Запись строки с дополнительным полем для строки
/// </summary>
public record StringToken(string Lexema, string Value) : Token(TokenType.String, Lexema);

/// <summary>
/// Запись логического типа с дополнительным полем значения логического типа
/// </summary>
public record BoolToken(string Lexema, bool Value) : Token(TokenType.BoolConstant, Lexema);

/// <summary>
/// Тип токена
/// </summary>
public enum TokenType
{
    RelationGroupOperation,
    AdditionGroupOperation,
    MultiplicationGroupOperation,
    UnaryOperation,
    Separator,
    Type,
    BoolConstant,
    KeyWord,
    IntegerNumber,
    FloatNumber,
    String,
    Identifier,
}
