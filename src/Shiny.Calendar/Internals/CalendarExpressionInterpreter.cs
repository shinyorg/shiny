using System.Linq.Expressions;
using System.Reflection;

namespace Shiny.Calendar.Internals;

/// <summary>
/// AOT-safe expression tree interpreter that evaluates expressions
/// without requiring runtime compilation via Expression.Compile().
/// </summary>
static class CalendarExpressionInterpreter
{
    public static bool Evaluate(Expression<Func<CalendarEvent, bool>> expression, CalendarEvent evt)
        => (bool)Interpret(expression.Body, evt, expression.Parameters[0])!;

    static object? Interpret(Expression expr, CalendarEvent evt, ParameterExpression param)
    {
        switch (expr)
        {
            case ConstantExpression c:
                return c.Value;

            case ParameterExpression p when p == param:
                return evt;

            case MemberExpression m:
                return InterpretMember(m, evt, param);

            case UnaryExpression u:
                return InterpretUnary(u, evt, param);

            case BinaryExpression b:
                return InterpretBinary(b, evt, param);

            case MethodCallExpression mc:
                return InterpretMethodCall(mc, evt, param);

            case ConditionalExpression cond:
                var test = (bool)Interpret(cond.Test, evt, param)!;
                return test
                    ? Interpret(cond.IfTrue, evt, param)
                    : Interpret(cond.IfFalse, evt, param);

            case LambdaExpression lambda:
                return lambda;

            case TypeBinaryExpression tb when tb.NodeType == ExpressionType.TypeIs:
                var tbValue = Interpret(tb.Expression, evt, param);
                return tbValue != null && tb.TypeOperand.IsInstanceOfType(tbValue);

            default:
                throw new NotSupportedException(
                    $"Expression node type '{expr.NodeType}' ({expr.GetType().Name}) is not supported for in-memory evaluation.");
        }
    }

    static object? InterpretMember(MemberExpression expr, CalendarEvent evt, ParameterExpression param)
    {
        var instance = expr.Expression != null ? Interpret(expr.Expression, evt, param) : null;

        return expr.Member switch
        {
            PropertyInfo prop => prop.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => throw new NotSupportedException($"Member type '{expr.Member.MemberType}' is not supported.")
        };
    }

    static object? InterpretUnary(UnaryExpression expr, CalendarEvent evt, ParameterExpression param)
    {
        var operand = Interpret(expr.Operand, evt, param);

        return expr.NodeType switch
        {
            ExpressionType.Not => operand is bool b ? !b : throw new InvalidOperationException("Not operator requires boolean operand."),
            ExpressionType.Convert or ExpressionType.ConvertChecked => Convert(operand, expr.Type),
            ExpressionType.TypeAs => operand != null && expr.Type.IsInstanceOfType(operand) ? operand : null,
            ExpressionType.Negate or ExpressionType.NegateChecked => Negate(operand),
            _ => throw new NotSupportedException($"Unary operator '{expr.NodeType}' is not supported.")
        };
    }

    static object? InterpretBinary(BinaryExpression expr, CalendarEvent evt, ParameterExpression param)
    {
        // Short-circuit logical operators
        if (expr.NodeType == ExpressionType.AndAlso)
        {
            var left = (bool)Interpret(expr.Left, evt, param)!;
            return left && (bool)Interpret(expr.Right, evt, param)!;
        }

        if (expr.NodeType == ExpressionType.OrElse)
        {
            var left = (bool)Interpret(expr.Left, evt, param)!;
            return left || (bool)Interpret(expr.Right, evt, param)!;
        }

        if (expr.NodeType == ExpressionType.Coalesce)
        {
            var left = Interpret(expr.Left, evt, param);
            return left ?? Interpret(expr.Right, evt, param);
        }

        var lhs = Interpret(expr.Left, evt, param);
        var rhs = Interpret(expr.Right, evt, param);

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

    static object? InterpretMethodCall(MethodCallExpression expr, CalendarEvent evt, ParameterExpression param)
    {
        var instance = expr.Object != null ? Interpret(expr.Object, evt, param) : null;
        var args = new object?[expr.Arguments.Count];
        for (var i = 0; i < expr.Arguments.Count; i++)
        {
            // Handle lambda arguments without evaluating them (for LINQ methods like Any/All).
            // Queryable methods quote the lambda in a UnaryExpression; Enumerable methods (e.g.
            // List<T>.Any) pass it as a bare LambdaExpression - support both.
            var lambda = expr.Arguments[i] switch
            {
                UnaryExpression { Operand: LambdaExpression l } => l,
                LambdaExpression l => l,
                _ => null
            };

            if (lambda is { ReturnType: var rt, Parameters.Count: 1 } && rt == typeof(bool))
                args[i] = CreateInterpreterDelegate(lambda, evt, param);
            else
                args[i] = Interpret(expr.Arguments[i], evt, param);
        }

        return expr.Method.Invoke(instance, args);
    }

    static object CreateInterpreterDelegate(LambdaExpression lambda, CalendarEvent evt, ParameterExpression outerParam)
    {
        var innerParam = lambda.Parameters[0];
        var elementType = innerParam.Type;

        Func<object?, bool> evaluator = element =>
            (bool)InterpretWithSubstitution(lambda.Body, outerParam, evt, innerParam, element)!;

        return CreateTypedPredicate(elementType, evaluator);
    }

    static object CreateTypedPredicate(Type elementType, Func<object?, bool> evaluator)
    {
        // Common collection element types used in this library
        if (elementType == typeof(EventAttendee))
            return new Func<EventAttendee, bool>(x => evaluator(x));
        if (elementType == typeof(EventReminder))
            return new Func<EventReminder, bool>(x => evaluator(x));
        if (elementType == typeof(string))
            return new Func<string, bool>(x => evaluator(x));

        throw new NotSupportedException($"Collection element type '{elementType.Name}' is not supported for lambda interpretation.");
    }

    static object? InterpretWithSubstitution(
        Expression expr,
        ParameterExpression outerParam, CalendarEvent evt,
        ParameterExpression innerParam, object? innerValue)
    {
        switch (expr)
        {
            case ConstantExpression c:
                return c.Value;

            case ParameterExpression p when p == outerParam:
                return evt;

            case ParameterExpression p when p == innerParam:
                return innerValue;

            case MemberExpression m:
                var instance = m.Expression != null
                    ? InterpretWithSubstitution(m.Expression, outerParam, evt, innerParam, innerValue)
                    : null;
                return m.Member switch
                {
                    PropertyInfo prop => prop.GetValue(instance),
                    FieldInfo field => field.GetValue(instance),
                    _ => throw new NotSupportedException($"Member type '{m.Member.MemberType}' is not supported.")
                };

            case MethodCallExpression mc:
                var obj = mc.Object != null
                    ? InterpretWithSubstitution(mc.Object, outerParam, evt, innerParam, innerValue)
                    : null;
                var args = new object?[mc.Arguments.Count];
                for (var i = 0; i < mc.Arguments.Count; i++)
                    args[i] = InterpretWithSubstitution(mc.Arguments[i], outerParam, evt, innerParam, innerValue);
                return mc.Method.Invoke(obj, args);

            case UnaryExpression u:
                var operand = InterpretWithSubstitution(u.Operand, outerParam, evt, innerParam, innerValue);
                return u.NodeType switch
                {
                    ExpressionType.Not => operand is bool b ? !b : throw new InvalidOperationException(),
                    ExpressionType.Convert or ExpressionType.ConvertChecked => Convert(operand, u.Type),
                    _ => throw new NotSupportedException($"Unary '{u.NodeType}' is not supported.")
                };

            case BinaryExpression b:
                if (b.NodeType == ExpressionType.AndAlso)
                {
                    var left = (bool)InterpretWithSubstitution(b.Left, outerParam, evt, innerParam, innerValue)!;
                    return left && (bool)InterpretWithSubstitution(b.Right, outerParam, evt, innerParam, innerValue)!;
                }
                if (b.NodeType == ExpressionType.OrElse)
                {
                    var left = (bool)InterpretWithSubstitution(b.Left, outerParam, evt, innerParam, innerValue)!;
                    return left || (bool)InterpretWithSubstitution(b.Right, outerParam, evt, innerParam, innerValue)!;
                }
                var lhs = InterpretWithSubstitution(b.Left, outerParam, evt, innerParam, innerValue);
                var rhs = InterpretWithSubstitution(b.Right, outerParam, evt, innerParam, innerValue);
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
        (string x, string y, '+') => x + y,
        _ => throw new InvalidOperationException($"Cannot perform '{op}' on '{a?.GetType().Name}' and '{b?.GetType().Name}'.")
    };

    static object? Negate(object? value) => value switch
    {
        int i => -i,
        long l => -l,
        double d => -d,
        _ => throw new InvalidOperationException($"Cannot negate value of type '{value?.GetType().Name}'.")
    };
}
