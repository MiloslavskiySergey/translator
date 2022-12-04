using System.Globalization;

namespace translator.Model;

public static class IntermediateCodeParser
{
    public static IntermediateCodeToken ParseLine(string line)
    {
        if (line.StartsWith('@'))
            return new IntermediateCodeLabel(line[0..^1]);
        if (line.StartsWith("goto @"))
            return new IntermediateCodeGoto(new IntermediateCodeLabel(line[5..]));
        if (line.StartsWith("if"))
        {
            var parts = line.Split(" goto ");
            return new IntermediateCodeConditionalOperator(ParseExpression(parts[0][3..]), new IntermediateCodeLabel(parts[1]));
        }
        if (line.StartsWith("Input("))
            return new IntermediateCodeInputOperator(new IntermediateCodeVariable(line[6..^1]));
        if (line.StartsWith("Output("))
            return new IntermediateCodeOutputOperator(ParseExpression(line[7..^1]));
        if (line.Contains('='))
        {
            var parts = line.Split(" = ");
            return new IntermediateCodeAssign(new IntermediateCodeVariable(parts[0]), ParseExpression(parts[1]));
        }
        throw new InvalidOperationException($"Invalid intermediate code line: `{line}`.");
    }

    private static IntermediateCodeToken ParseExpression(string expression)
    {
        if (expression == "True" || expression == "False")
            return new IntermediateCodeBoolConstant(bool.Parse(expression));
        if (expression.StartsWith("\""))
            return new IntermediateCodeStringConstant(expression[1..^1].Replace("\\n", "\n"));
        foreach (var op in _unaryOperators)
        {
            if (expression.StartsWith(op))
                return new IntermediateCodeUnaryOperator(op, ParseValue(expression[(op.Length + 1)..]));
        }
        foreach (var op in _binaryOperators)
        {
            if (expression.Contains(op))
            {
                var parts = expression.Split($" {op} ");
                return new IntermediateCodeBinaryOperator(op, ParseValue(parts[0]), ParseValue(parts[1]));
            }
        }
        foreach (var type in (DataType[])Enum.GetValues(typeof(DataType)))
        {
            var typeName = type.ToString();
            if (expression.StartsWith($"{typeName}("))
                return new IntermediateCodeCast(new IntermediateCodeVariable(expression[(typeName.Length + 1)..^1]), type);
        }
        if (expression.Contains('.'))
            return new IntermediateCodeFloatConstant(double.Parse(expression));
        if (char.IsLetter(expression[0]) || expression[0] == '#')
            return new IntermediateCodeVariable(expression);
        if (expression.All(char.IsDigit))
            return new IntermediateCodeIntegerConstant(int.Parse(expression));
        throw new InvalidOperationException($"Invalid expression: `{expression}`.");
    }

    private static IntermediateCodeValue ParseValue(string value)
    {
        if (value == "True" || value == "False")
            return new IntermediateCodeBoolConstant(bool.Parse(value));
        if (value.StartsWith("\""))
            return new IntermediateCodeStringConstant(value[1..^1].Replace("\\n", "\n"));
        if (value.Contains('.'))
            return new IntermediateCodeFloatConstant(double.Parse(value, CultureInfo.InvariantCulture));
        if (char.IsLetter(value[0]) || value[0] == '#')
            return new IntermediateCodeVariable(value);
        if (value.All(char.IsDigit))
            return new IntermediateCodeIntegerConstant(int.Parse(value));
        throw new InvalidOperationException($"Invalid vales: `{value}`.");
    }

    private static readonly string[] _unaryOperators = new[] { "not" };
    private static readonly string[] _binaryOperators = new[] { "<>", "=", "<", "<=", ">", ">=", "+", "-", "*", "/", "^" };
}

public record IntermediateCodeToken();
public record IntermediateCodeLabel(string Name) : IntermediateCodeToken;
public record IntermediateCodeGoto(IntermediateCodeLabel Label) : IntermediateCodeToken;
public record IntermediateCodeValue() : IntermediateCodeToken;
public record IntermediateCodeVariable(string Name) : IntermediateCodeValue();
public record IntermediateCodeConstant<T>(T Value, DataType Type) : IntermediateCodeValue() where T : notnull;
public record IntermediateCodeIntegerConstant(int Value) : IntermediateCodeConstant<int>(Value, DataType.Integer);
public record IntermediateCodeFloatConstant(double Value) : IntermediateCodeConstant<double>(Value, DataType.Float);
public record IntermediateCodeStringConstant(string Value) : IntermediateCodeConstant<string>(Value, DataType.String);
public record IntermediateCodeBoolConstant(bool Value) : IntermediateCodeConstant<bool>(Value, DataType.Bool);
public record IntermediateCodeUnaryOperator(string Operator, IntermediateCodeValue Operand) : IntermediateCodeToken;
public record IntermediateCodeBinaryOperator(
    string Operator,
    IntermediateCodeValue LeftOperand,
    IntermediateCodeValue RightOperand
) : IntermediateCodeToken;
public record IntermediateCodeAssign(IntermediateCodeVariable Variable, IntermediateCodeToken Value) : IntermediateCodeToken;
public record IntermediateCodeCast(IntermediateCodeVariable Variable, DataType Type) : IntermediateCodeToken;
public record IntermediateCodeConditionalOperator(IntermediateCodeToken Condition, IntermediateCodeLabel Label) : IntermediateCodeToken;
public record IntermediateCodeInputOperator(IntermediateCodeVariable Variable) : IntermediateCodeToken;
public record IntermediateCodeOutputOperator(IntermediateCodeToken Expression) : IntermediateCodeToken;
