using System.Collections;
using System.Globalization;

namespace translator.Model;

public class IntermediateCodeGenerator
{
    public IntermediateCodeGenerator(Parser parser)
    {
        _parser = parser;
    }

    public event Action<string>? Emit;

    public void Generate()
    {
        GenerateBlockNode(_parser.Parse());
    }

    private readonly Parser _parser;
    private readonly IEnumerator<string> _variablesNamesGenerator = new VariablesNamesGenerator().GetEnumerator();
    private readonly IEnumerator<string> _labelsNamesGenerator = new LabelsNamesGenerator().GetEnumerator();
    private readonly VariablesTable _variablesTable = new();

    private string NextVariableName()
    {
        _variablesNamesGenerator.MoveNext();
        return _variablesNamesGenerator.Current;
    }
    private Variable NextVariable(DataType type)
    {
        return _variablesTable.AddVariable(NextVariableName(), type);
    }

    private string NextLabelName()
    {
        _labelsNamesGenerator.MoveNext();
        return _labelsNamesGenerator.Current;
    }

    private void GenerateBlockNode(BlockNode node) {
        foreach (var child in node.Children)
        {
            if (child is DescriptionNode descriptionNode)
                GenerateDescriptionNode(descriptionNode);
            else if (child is AssignmentNode assignmentNode)
                GenerateAssignmentNode(assignmentNode);
            else if (child is ConditionalOperotorNode conditionalOperotorNode)
                GenerateConditionalOperotorNode(conditionalOperotorNode);
            else if (child is FixedLoopOperatorNode fixedLoopOperatorNode)
                GenerateFixedLoopOperatorNode(fixedLoopOperatorNode);
            else if (child is ConditionalLoopOperatorNode conditionalLoopOperatorNode)
                GenerateConditionalLoopOperatorNode(conditionalLoopOperatorNode);
            else if (child is InputOperatorNode inputOperatorNode)
                GenerateInputOperatorNode(inputOperatorNode);
            else if (child is OutputOperatorNode outputOperatorNode)
                GenerateOutputOperatorNode(outputOperatorNode);
            else
                throw new InvalidOperationException($"Invalid child type of BlockNode: {child.GetType().Name}.");
        }
    }

    private void GenerateDescriptionNode(DescriptionNode node)
    {
        var variableType = GetVariableType(node.Type);
        foreach (var identifier in node.Identifiers)
            _variablesTable.AddVariable(identifier, variableType);
    }

    private void GenerateAssignmentNode(AssignmentNode node)
    {
        var toVariable = _variablesTable.GetVariable(node.Identifier);
        var fromValue = GenerateExpression(node.Expression, toVariable);
        if (fromValue != toVariable)
            AssignValue(toVariable, fromValue, node.Expression.Position);
    }

    private Variable GenerateUnaryOperationNode(UnaryOperationNode node, Variable? toVariable = null)
    {
        var operand = GenerateExpression(node.Operand);
        var key = (node.Operator.Name, operand.Type);
        _possibleUnaryOperations.TryGetValue(key, out var possibleUnaryOperation);
        if (possibleUnaryOperation is null)
            throw new IntermediateCodeGeneratorException(
                $"Can't apply `{node.Operator.Name}` operator to argument of type {operand.Type}",
                node.Operator.Position
            );
        if (possibleUnaryOperation.CastOperand.HasValue)
            operand = CastValue(operand, possibleUnaryOperation.CastOperand.Value, node.Operand.Position);
        Variable variable;
        if (toVariable is not null && toVariable.Type == possibleUnaryOperation.ResultType)
            variable = toVariable;
        else
            variable = NextVariable(possibleUnaryOperation.ResultType);
        AssignUnaryOperator(variable, operand, node.Operator);
        return variable;
    }

    private Variable GenerateBinaryOperationNode(BinaryOperationNode node, Variable? toVariable = null)
    {
        var leftOperand = GenerateExpression(node.LeftOperand);
        var rightOperand = GenerateExpression(node.RightOperand);
        var key = (node.Operator.Name, leftOperand.Type, rightOperand.Type);
        _possibleBinaryOperations.TryGetValue(key, out var possibleBinaryOperation);
        if (possibleBinaryOperation is null)
            throw new IntermediateCodeGeneratorException(
                $"Can't apply `{node.Operator.Name}` operator to argumens of types `{leftOperand.Type}` and `{rightOperand.Type}`",
                node.Operator.Position
            );
        if (possibleBinaryOperation.CastLeftOperand.HasValue)
            leftOperand = CastValue(leftOperand, possibleBinaryOperation.CastLeftOperand.Value, node.Operator.Position);
        if (possibleBinaryOperation.CastRightOperand.HasValue)
            rightOperand = CastValue(rightOperand, possibleBinaryOperation.CastRightOperand.Value, node.Operator.Position);
        Variable variable;
        if (toVariable is not null && toVariable.Type == possibleBinaryOperation.ResultType)
            variable = toVariable;
        else
            variable = NextVariable(possibleBinaryOperation.ResultType);
        AssignBinaryOperator(variable, leftOperand, rightOperand, node.Operator);
        return variable;
    }

    private void GenerateConditionalOperotorNode(ConditionalOperotorNode node) { }

    private void GenerateFixedLoopOperatorNode(FixedLoopOperatorNode node) { }

    private void GenerateConditionalLoopOperatorNode(ConditionalLoopOperatorNode node) { }

    private void GenerateInputOperatorNode(InputOperatorNode node) { }

    private void GenerateOutputOperatorNode(OutputOperatorNode node) { }

    private Value GenerateExpression(Node node, Variable? toVariable = null)
    {
        if (node is UnaryOperationNode unaryOperationNode)
            return GenerateUnaryOperationNode(unaryOperationNode, toVariable);
        else if (node is BinaryOperationNode binaryOperationNode)
            return GenerateBinaryOperationNode(binaryOperationNode, toVariable);
        else if (node is IdentifierNode identifierNode)
            return _variablesTable.GetVariable(identifierNode);
        else if (node is IntegerConstantNode integerConstantNode)
            return new IntegerConstant(integerConstantNode.Value);
        else if (node is FloatConstantNode floatConstantNode)
            return new FloatConstant(floatConstantNode.Value);
        else if (node is StringConstantNode stringConstantNode)
            return new StringConstant(stringConstantNode.Value);
        else if (node is BoolConstantNode boolConstantNode)
            return new BoolConstant(boolConstantNode.Value);
        else
            throw new InvalidOperationException($"Invalid expression type: {node}.");
    }

    private Value CastValue(Value value, DataType type, ProgramPosition position)
    {
        if (value is IntegerConstant integerConstant && type == DataType.Float)
            return new FloatConstant(integerConstant.Value);
        var variable = NextVariable(type);
        AssignValue(variable, value, position);
        return variable;
    }
    private void AssignValue(Variable to, Value from, ProgramPosition position)
    {
        if (from.Type == to.Type)
        {
            Emit?.Invoke(Assign(to.ToString(), from.ToString()));
        }
        else if (from.Type == DataType.Integer && to.Type == DataType.Float)
        {
            if (from is IntegerConstant integerConstant)
                Emit?.Invoke(Assign(to.ToString(), new FloatConstant(integerConstant.Value).ToString()));
            else
                Emit?.Invoke(Assign(to.ToString(), Cast(from.ToString(), to.Type)));
        }
        else
        {
            throw new IntermediateCodeGeneratorException($"Can't assing `{from.Type}` to `{to.Type}`", position);
        }
    }
    private void AssignUnaryOperator(Variable to, Value operand, OperatorNode node)
    {
        Emit?.Invoke(Assign(to.Name, ApplyUnaryOperator(operand.ToString(), node.Name)));
    }
    private void AssignBinaryOperator(Variable to, Value leftOperand, Value rightOperand, OperatorNode node)
    {
        Emit?.Invoke(Assign(to.Name, ApplyBinaryOperator(leftOperand.ToString(), rightOperand.ToString(), node.Name)));
    }

    private static string Assign(string to, string from) => $"{to} = {from}";
    private static string Cast(string value, DataType type) => $"{type}({value})";
    private static string ApplyUnaryOperator(string operand, string operatorName) => $"{operatorName} {operand}";
    private static string ApplyBinaryOperator(string leftOperand, string rightOperand, string operatorName) => $"{leftOperand} {operatorName} {rightOperand}";

    private readonly static Dictionary<
        (string operatorName, DataType operandType),
        PossibleUnaryOperation
    > _possibleUnaryOperations = GetPossibleUnaryOperations();
    private readonly static Dictionary<
        (string operatorName, DataType leftOperandType, DataType rightOperandType),
        PossibleBinaryOperation
    > _possibleBinaryOperations = GetPossibleBinaryOperations();

    private static DataType GetVariableType(TypeNode node)
    {
        return node.Name switch
        {
            "%" => DataType.Integer,
            "!" => DataType.Float,
            "@" => DataType.String,
            "$" => DataType.Bool,
            _ => throw new InvalidOperationException($"Invalid type name: `{node.Name}`."),
        };
    }

    private static Dictionary<
        (string operatorName, DataType operandType),
        PossibleUnaryOperation
    > GetPossibleUnaryOperations()
    {
        return new()
        {
            { ("not", DataType.Bool), new PossibleUnaryOperation(DataType.Bool) }
        };
    }
    private static Dictionary<
        (string operatorName, DataType leftOperandType, DataType rightOperandType),
        PossibleBinaryOperation
    > GetPossibleBinaryOperations()
    {
        var result = new Dictionary<
            (string operatorName, DataType leftOperandType, DataType rightOperandType),
            PossibleBinaryOperation
        >();
        foreach (var boolOperator in new[] { "or", "and" })
        {
            result.Add(
                (boolOperator, DataType.Bool, DataType.Bool),
                new PossibleBinaryOperation(DataType.Bool)
            );
        }
        foreach (var equalOperator in new[] { "=", "<>" })
        {
            result.Add(
                (equalOperator, DataType.Integer, DataType.Integer),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (equalOperator, DataType.Float, DataType.Float),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (equalOperator, DataType.String, DataType.String),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (equalOperator, DataType.Bool, DataType.Bool),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (equalOperator, DataType.Integer, DataType.Float),
                new PossibleBinaryOperation(DataType.Bool, DataType.Float, null)
            );
            result.Add(
                (equalOperator, DataType.Float, DataType.Integer),
                new PossibleBinaryOperation(DataType.Bool, null, DataType.Float)
            );
        }
        foreach (var orderOperator in new[] { "<", "<=", ">", ">=" })
        {
            result.Add(
                (orderOperator, DataType.Integer, DataType.Integer),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (orderOperator, DataType.Float, DataType.Float),
                new PossibleBinaryOperation(DataType.Bool)
            );
            result.Add(
                (orderOperator, DataType.Integer, DataType.Float),
                new PossibleBinaryOperation(DataType.Bool, DataType.Float, null)
            );
            result.Add(
                (orderOperator, DataType.Float, DataType.Integer),
                new PossibleBinaryOperation(DataType.Bool, null, DataType.Float)
            );
        }
        foreach (var arithmeticOperator in new[] { "+", "-", "*", "/", "^" })
        {
            result.Add(
                (arithmeticOperator, DataType.Integer, DataType.Integer),
                new PossibleBinaryOperation(DataType.Integer)
            );
            result.Add(
                (arithmeticOperator, DataType.Float, DataType.Float),
                new PossibleBinaryOperation(DataType.Float)
            );
            result.Add(
                (arithmeticOperator, DataType.Integer, DataType.Float),
                new PossibleBinaryOperation(DataType.Float, DataType.Float, null)
            );
            result.Add(
                (arithmeticOperator, DataType.Float, DataType.Integer),
                new PossibleBinaryOperation(DataType.Float, null, DataType.Float)
            );
        }
        result.Add(
            ("+", DataType.String, DataType.String),
            new PossibleBinaryOperation(DataType.String)
        );
        return result;
    }
}

public class IntermediateCodeGeneratorException : Exception
{
    public IntermediateCodeGeneratorException(string message, ProgramPosition position) : base($"{message} - {position}") { }
}

internal class VariablesNamesGenerator : IEnumerable<string>
{
    private int _index = 0;

    public IEnumerator<string> GetEnumerator()
    {
        while (true)
        {
            var name = $"#t{_index}";
            _index++;
            yield return name;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class LabelsNamesGenerator : IEnumerable<string>
{
    private int _index = 0;

    public IEnumerator<string> GetEnumerator()
    {
        while (true)
        {
            var name = $"@l{_index}:";
            _index++;
            yield return name;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal enum DataType
{
    Integer,
    Float,
    String,
    Bool,
}

internal record Value(DataType Type);
internal record Variable(string Name, DataType Type) : Value(Type)
{
    public override string ToString() => Name;
}
internal record Constant<T>(T Value, DataType Type) : Value(Type) where T : notnull;
internal record IntegerConstant(int Value) : Constant<int>(Value, DataType.Integer)
{
    public override string ToString() => Value.ToString();
}
internal record FloatConstant(double Value) : Constant<double>(Value, DataType.Float)
{
    public override string ToString() => Value.ToString("0.0####################", CultureInfo.InvariantCulture);
}
internal record StringConstant(string Value) : Constant<string>(Value, DataType.String)
{
    public override string ToString() => $"\"{Value}\"";
}
internal record BoolConstant(bool Value) : Constant<bool>(Value, DataType.Bool)
{
    public override string ToString() => Value.ToString();
}

internal record PossibleUnaryOperation(DataType ResultType, DataType? CastOperand = null);
internal record PossibleBinaryOperation(DataType ResultType, DataType? CastLeftOperand = null, DataType? CastRightOperand = null);

internal class VariablesTable
{
    public VariablesTable()
    {
        _prevous = null;
    }

    public VariablesTable EnterScope()
    {
        return new VariablesTable(this);
    }
    public VariablesTable ExitScope()
    {
        if (_prevous is null)
            throw new InvalidOperationException("Can't exit global scope.");
        return _prevous;
    }
    public bool HasVariable(string name)
    {
        return _variables.Find((variable) => variable.Name == name) is not null;
    }
    public Variable GetVariable(IdentifierNode node)
    {
        var variable = _variables.Find((variable) => variable.Name == node.Name);
        if (variable is null)
            throw new IntermediateCodeGeneratorException($"Variable `{node.Name}` not in the scope", node.Position);
        return variable;
    }
    public Variable AddVariable(IdentifierNode node, DataType type)
    {
        if (HasVariable(node.Name))
            throw new IntermediateCodeGeneratorException($"`{node.Name}` variable is already defined in the scope", node.Position);
        var variable = new Variable(node.Name, type);
        _variables.Add(variable);
        return variable;
    }
    public Variable AddVariable(string name, DataType type)
    {
        var variable = new Variable(name, type);
        _variables.Add(variable);
        return variable;
    }

    private VariablesTable? _prevous;
    private List<Variable> _variables = new();

    private VariablesTable(VariablesTable previous)
    {
        _prevous = previous;
    }
}
