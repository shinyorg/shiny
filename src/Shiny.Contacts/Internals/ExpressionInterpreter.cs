using System.Linq.Expressions;
using System.Reflection;

namespace Shiny.Contacts.Internals;

/// <summary>
/// AOT-safe expression tree interpreter that evaluates expressions
/// without requiring runtime compilation via Expression.Compile().
/// </summary>
static class ExpressionInterpreter
{
    public static bool Evaluate(Expression<Func<Contact, bool>> expression, Contact contact)
        => (bool)Interpret(expression.Body, contact, expression.Parameters[0])!;

    static object? Interpret(Expression expr, Contact contact, ParameterExpression param)
    {
        switch (expr)
        {
            case ConstantExpression c:
                return c.Value;

            case ParameterExpression p when p == param:
                return contact;

            case MemberExpression m:
                return InterpretMember(m, contact, param);

            case UnaryExpression u:
                return InterpretUnary(u, contact, param);

            case BinaryExpression b:
                return InterpretBinary(b, contact, param);

            case MethodCallExpression mc:
                return InterpretMethodCall(mc, contact, param);

            case ConditionalExpression cond:
                var test = (bool)Interpret(cond.Test, contact, param)!;
                return test
                    ? Interpret(cond.IfTrue, contact, param)
                    : Interpret(cond.IfFalse, contact, param);

            case LambdaExpression lambda:
                return lambda;

            case TypeBinaryExpression tb when tb.NodeType == ExpressionType.TypeIs:
                var tbValue = Interpret(tb.Expression, contact, param);
                return tbValue != null && tb.TypeOperand.IsInstanceOfType(tbValue);

            default:
                throw new NotSupportedException(
                    $"Expression node type '{expr.NodeType}' ({expr.GetType().Name}) is not supported for in-memory evaluation.");
        }
    }

    static object? InterpretMember(MemberExpression expr, Contact contact, ParameterExpression param)
    {
        var instance = expr.Expression != null ? Interpret(expr.Expression, contact, param) : null;

        return expr.Member switch
        {
            PropertyInfo prop => prop.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => throw new NotSupportedException($"Member type '{expr.Member.MemberType}' is not supported.")
        };
    }

    static object? InterpretUnary(UnaryExpression expr, Contact contact, ParameterExpression param)
    {
        var operand = Interpret(expr.Operand, contact, param);

        return expr.NodeType switch
        {
            ExpressionType.Not => operand is bool b ? !b : throw new InvalidOperationException("Not operator requires boolean operand."),
            ExpressionType.Convert or ExpressionType.ConvertChecked => Convert(operand, expr.Type),
            ExpressionType.TypeAs => operand != null && expr.Type.IsInstanceOfType(operand) ? operand : null,
            ExpressionType.Negate or ExpressionType.NegateChecked => Negate(operand),
            _ => throw new NotSupportedException($"Unary operator '{expr.NodeType}' is not supported.")
        };
    }

    static object? InterpretBinary(BinaryExpression expr, Contact contact, ParameterExpression param)
    {
        // Short-circuit logical operators
        if (expr.NodeType == ExpressionType.AndAlso)
        {
            var left = (bool)Interpret(expr.Left, contact, param)!;
            return left && (bool)Interpret(expr.Right, contact, param)!;
        }

        if (expr.NodeType == ExpressionType.OrElse)
        {
            var left = (bool)Interpret(expr.Left, contact, param)!;
            return left || (bool)Interpret(expr.Right, contact, param)!;
        }

        if (expr.NodeType == ExpressionType.Coalesce)
        {
            var left = Interpret(expr.Left, contact, param);
            return left ?? Interpret(expr.Right, contact, param);
        }

        var lhs = Interpret(expr.Left, contact, param);
        var rhs = Interpret(expr.Right, contact, param);

        return expr.NodeType switch
        {
            ExpressionType.Equal => Equals(lhs, rhs),
            ExpressionType.NotEqual => !Equals(lhs, rhs),
            ExpressionType.GreaterThan => Compare(lhs, rhs) > 0,
            ExpressionType.GreaterThanOrEqual => Compare(lhs, rhs) >= 0,
            ExpressionType.LessThan => Compare(lhs, rhs) < 0,
            ExpressionType.LessThanOrEqual => Compare(lhs, rhs) <= 0,
            ExpressionType.Add or ExpressionType.AddChecked => ArithmeticOp(lhs, rhs, '+'),
            ExpressionType.Subtract or ExpressionType.SubtractChecked => ArithmeticOp(lhs, rhs, '-'),
            _ => throw new NotSupportedException($"Binary operator '{expr.NodeType}' is not supported.")
        };
    }

    static object? InterpretMethodCall(MethodCallExpression expr, Contact contact, ParameterExpression param)
    {
        var instance = expr.Object != null ? Interpret(expr.Object, contact, param) : null;
        var args = new object?[expr.Arguments.Count];
        for (var i = 0; i < expr.Arguments.Count; i++)
        {
            // Handle lambda arguments without evaluating them (for LINQ methods like Any/All)
            if (expr.Arguments[i] is UnaryExpression { Operand: LambdaExpression } unary)
                args[i] = InterpretLambdaArgument(unary, contact, param);
            else
                args[i] = Interpret(expr.Arguments[i], contact, param);
        }

        return expr.Method.Invoke(instance, args);
    }

    static object? InterpretLambdaArgument(UnaryExpression expr, Contact contact, ParameterExpression param)
    {
        if (expr.Operand is not LambdaExpression lambda)
            return Interpret(expr, contact, param);

        // Build a delegate that interprets the lambda body for each element
        var innerParam = lambda.Parameters[0];
        var elementType = innerParam.Type;

        // Create a Func<T, bool> that uses the interpreter
        if (lambda.ReturnType == typeof(bool) && lambda.Parameters.Count == 1)
        {
            return CreateInterpreterDelegate(lambda, contact, param);
        }

        return Interpret(expr, contact, param);
    }

    static object CreateInterpreterDelegate(LambdaExpression lambda, Contact contact, ParameterExpression outerParam)
    {
        var innerParam = lambda.Parameters[0];
        var elementType = innerParam.Type;

        Func<object?, bool> evaluator = element =>
            (bool)InterpretWithSubstitution(lambda.Body, outerParam, contact, innerParam, element)!;

        return CreateTypedPredicate(elementType, evaluator);
    }

    static object CreateTypedPredicate(Type elementType, Func<object?, bool> evaluator)
    {
        // Common collection element types used in this library
        if (elementType == typeof(ContactPhone))
            return new Func<ContactPhone, bool>(x => evaluator(x));
        if (elementType == typeof(ContactEmail))
            return new Func<ContactEmail, bool>(x => evaluator(x));
        if (elementType == typeof(ContactAddress))
            return new Func<ContactAddress, bool>(x => evaluator(x));
        if (elementType == typeof(ContactDate))
            return new Func<ContactDate, bool>(x => evaluator(x));
        if (elementType == typeof(ContactRelationship))
            return new Func<ContactRelationship, bool>(x => evaluator(x));
        if (elementType == typeof(ContactWebsite))
            return new Func<ContactWebsite, bool>(x => evaluator(x));
        if (elementType == typeof(string))
            return new Func<string, bool>(x => evaluator(x));

        throw new NotSupportedException($"Collection element type '{elementType.Name}' is not supported for lambda interpretation.");
    }

    static object? InterpretWithSubstitution(
        Expression expr,
        ParameterExpression outerParam, Contact contact,
        ParameterExpression innerParam, object? innerValue)
    {
        switch (expr)
        {
            case ConstantExpression c:
                return c.Value;

            case ParameterExpression p when p == outerParam:
                return contact;

            case ParameterExpression p when p == innerParam:
                return innerValue;

            case MemberExpression m:
                var instance = m.Expression != null
                    ? InterpretWithSubstitution(m.Expression, outerParam, contact, innerParam, innerValue)
                    : null;
                return m.Member switch
                {
                    PropertyInfo prop => prop.GetValue(instance),
                    FieldInfo field => field.GetValue(instance),
                    _ => throw new NotSupportedException($"Member type '{m.Member.MemberType}' is not supported.")
                };

            case MethodCallExpression mc:
                var obj = mc.Object != null
                    ? InterpretWithSubstitution(mc.Object, outerParam, contact, innerParam, innerValue)
                    : null;
                var args = new object?[mc.Arguments.Count];
                for (var i = 0; i < mc.Arguments.Count; i++)
                    args[i] = InterpretWithSubstitution(mc.Arguments[i], outerParam, contact, innerParam, innerValue);
                return mc.Method.Invoke(obj, args);

            case UnaryExpression u:
                var operand = InterpretWithSubstitution(u.Operand, outerParam, contact, innerParam, innerValue);
                return u.NodeType switch
                {
                    ExpressionType.Not => operand is bool b ? !b : throw new InvalidOperationException(),
                    ExpressionType.Convert or ExpressionType.ConvertChecked => Convert(operand, u.Type),
                    _ => throw new NotSupportedException($"Unary '{u.NodeType}' is not supported.")
                };

            case BinaryExpression b:
                if (b.NodeType == ExpressionType.AndAlso)
                {
                    var left = (bool)InterpretWithSubstitution(b.Left, outerParam, contact, innerParam, innerValue)!;
                    return left && (bool)InterpretWithSubstitution(b.Right, outerParam, contact, innerParam, innerValue)!;
                }
                if (b.NodeType == ExpressionType.OrElse)
                {
                    var left = (bool)InterpretWithSubstitution(b.Left, outerParam, contact, innerParam, innerValue)!;
                    return left || (bool)InterpretWithSubstitution(b.Right, outerParam, contact, innerParam, innerValue)!;
                }
                var lhs = InterpretWithSubstitution(b.Left, outerParam, contact, innerParam, innerValue);
                var rhs = InterpretWithSubstitution(b.Right, outerParam, contact, innerParam, innerValue);
                return b.NodeType switch
                {
                    ExpressionType.Equal => Equals(lhs, rhs),
                    ExpressionType.NotEqual => !Equals(lhs, rhs),
                    ExpressionType.GreaterThan => Compare(lhs, rhs) > 0,
                    ExpressionType.GreaterThanOrEqual => Compare(lhs, rhs) >= 0,
                    ExpressionType.LessThan => Compare(lhs, rhs) < 0,
                    ExpressionType.LessThanOrEqual => Compare(lhs, rhs) <= 0,
                    _ => throw new NotSupportedException($"Binary '{b.NodeType}' is not supported.")
                };

            default:
                throw new NotSupportedException($"Expression '{expr.NodeType}' is not supported in inner lambda.");
        }
    }

    static new bool Equals(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    static int Compare(object? a, object? b)
    {
        if (a is IComparable ca)
            return ca.CompareTo(b);
        throw new InvalidOperationException($"Cannot compare values of type '{a?.GetType().Name}'.");
    }

    static object? Convert(object? value, Type targetType)
    {
        if (value is null) return null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value)) return value;
        return System.Convert.ChangeType(value, underlying);
    }

    static object? ArithmeticOp(object? a, object? b, char op) => (a, b, op) switch
    {
        (int x, int y, '+') => x + y,
        (int x, int y, '-') => x - y,
        (long x, long y, '+') => x + y,
        (long x, long y, '-') => x - y,
        (double x, double y, '+') => x + y,
        (double x, double y, '-') => x - y,
        (float x, float y, '+') => x + y,
        (float x, float y, '-') => x - y,
        (decimal x, decimal y, '+') => x + y,
        (decimal x, decimal y, '-') => x - y,
        (string x, string y, '+') => x + y,
        _ => throw new InvalidOperationException($"Cannot perform '{op}' on '{a?.GetType().Name}' and '{b?.GetType().Name}'.")
    };

    static object? Negate(object? value) => value switch
    {
        int i => -i,
        long l => -l,
        double d => -d,
        float f => -f,
        decimal m => -m,
        _ => throw new InvalidOperationException($"Cannot negate value of type '{value?.GetType().Name}'.")
    };
}
