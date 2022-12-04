using translator.Services;

namespace translator.Model;

public class Interpreter
{
    public Interpreter(ITerminalService terminal, string program)
    {
        _terminal = terminal;
        _program = program;
    }

    public void Interpret()
    {
        _terminal.Open();
        Parse();
        Execute();
        _terminal.Close();
    }

    private void Parse()
    {
        foreach (var line in _program.Split('\n'))
        {
            var token = IntermediateCodeParser.ParseLine(line);
            if (token is IntermediateCodeLabel label)
                _labels.Add(label.Name, _tokens.Count);
            _tokens.Add(token);
        }
    }

    private void Execute()
    {
        var position = 0;
        while (position < _tokens.Count)
        {
            var token = _tokens[position];
            if (token is IntermediateCodeGoto gt)
            {
                position = _labels[gt.Label.Name];
                continue;
            }
            else if (token is IntermediateCodeConditionalOperator conditionalOperator)
            {
                if (ExecuteExpression(conditionalOperator.Condition))
                {
                    position = _labels[conditionalOperator.Label.Name];
                    continue;
                }
            }
            else if (token is IntermediateCodeInputOperator inputOperator)
            {
                Assign(inputOperator.Variable.Name, _terminal.Read()!);
            }
            else if (token is IntermediateCodeOutputOperator outputOperator)
            {
                _terminal.Write(ExecuteExpression(outputOperator.Expression).ToString());
            }
            else if (token is IntermediateCodeAssign assign)
            {
                Assign(assign.Variable.Name, ExecuteExpression(assign.Value));
            }
            position++;
        }
    }

    private dynamic ExecuteExpression(IntermediateCodeToken expression)
    {
        if (expression is IntermediateCodeUnaryOperator unaryOperator)
            return _unaryOperators[unaryOperator.Operator](GetValue(unaryOperator.Operand));
        if (expression is IntermediateCodeBinaryOperator binaryOperator)
            return _binaryOperators[binaryOperator.Operator](
                GetValue(binaryOperator.LeftOperand),
                GetValue(binaryOperator.RightOperand)
            );
        if (expression is IntermediateCodeCast cast)
            return _cast[cast.Type](GetValue(cast.Variable));
        return GetValue((IntermediateCodeValue)expression);
    }

    private dynamic GetValue(IntermediateCodeValue value)
    {
        if (value is IntermediateCodeVariable variable)
            return _variables[variable.Name];
        if (value is IntermediateCodeIntegerConstant integerConstant)
            return integerConstant.Value;
        if (value is IntermediateCodeFloatConstant floatConstant)
            return floatConstant.Value;
        if (value is IntermediateCodeStringConstant stringConstant)
            return stringConstant.Value;
        if (value is IntermediateCodeBoolConstant boolConstant)
            return boolConstant.Value;
        throw new InvalidOperationException();
    }

    private void Assign(string name, dynamic value)
    {
        if (_variables.ContainsKey(name))
            _variables[name] = value;
        else
            _variables.Add(name, value);
    }

    private readonly string _program;
    private readonly ITerminalService _terminal;

    private List<IntermediateCodeToken> _tokens = new();
    private Dictionary<string, dynamic> _variables = new();
    private Dictionary<string, int> _labels = new();

    private Dictionary<string, Func<dynamic, dynamic>> _unaryOperators = new()
    {
        { "not", (value) => !value },
    };
    private Dictionary<string, Func<dynamic, dynamic, dynamic>> _binaryOperators = new()
    {
        { "<>", (left, right) => left != right },
        { "=", (left, right) => left == right },
        { "<", (left, right) => left < right },
        { "<=", (left, right) => left <= right },
        { ">", (left, right) => left > right },
        { ">=", (left, right) => left >= right },
        { "+", (left, right) => left + right },
        { "-", (left, right) => left - right },
        { "*", (left, right) => left * right },
        { "/", (left, right) => left / right },
        { "^", (left, right) => Math.Pow(left, right) },
    };
    private Dictionary<DataType, Func<dynamic, dynamic>> _cast = new()
    {
        { DataType.Integer, (value) => value is string ? int.Parse(value) : (int)value },
        { DataType.Float, (value) => value is string ? double.Parse(value) : (double)value },
        { DataType.String, (value) => value.ToString() },
        { DataType.Bool, (value) => value is string ? bool.Parse(value) : (bool)value },
    };
}
