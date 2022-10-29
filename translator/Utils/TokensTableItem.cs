using translator.Model;

namespace translator.Utils;

public record TokensTableItem(
    int Index,
    TokenType TokenType,
    string Lexema,
    object Value,
    int Line,
    int Position
);
