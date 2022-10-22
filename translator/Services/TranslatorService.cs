using translator.Model;
using translator.Windows.Tokens;

namespace translator.Services
{
    internal class TranslatorService
	{
        public string Program { get; set; } = "";

		public List<TokensTableItem> Scan() {
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
	}
}
