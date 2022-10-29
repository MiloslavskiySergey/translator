using translator.Model;

namespace translator.Windows.Tokens
{
    public record TokensTableItem(
        int Index,
        TokenType TokenType,
        string Lexema,
        object Value,
        int Line,
        int Position
    );
}
