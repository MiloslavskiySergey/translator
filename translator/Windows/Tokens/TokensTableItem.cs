using translator.Model;

namespace translator.Windows.Tokens
{
    internal record TokensTableItem(
        int Index,
        TokenType TokenType,
        string Lexema,
        object Value,
        int Line,
        int Position
    );
}
