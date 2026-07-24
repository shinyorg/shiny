using System.Linq.Expressions;
using System.Reflection;

namespace Shiny.Calendar.Internals;

/// <summary>
/// Visits a LINQ expression tree and extracts native fetch hints (calendar id + start/end window)
/// from an <see cref="IQueryable{CalendarEvent}"/> query. The hints only narrow what is fetched from
/// the platform; the full predicate is always re-applied in-memory so results match exactly regardless
/// of native window/overlap semantics.
/// </summary>
public class CalendarExpressionVisitor : ExpressionVisitor
{
    readonly CalendarEventQueryDescriptor descriptor = new();
    readonly List<Expression> predicates = [];

    public static CalendarEventQueryDescriptor Parse(Expression expression)
    {
        var visitor = new CalendarExpressionVisitor();
        visitor.Visit(expression);
        visitor.BuildInMemoryPredicate();
        return visitor.descriptor;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
        {
            this.Visit(node.Arguments[0]); // visit source

            if (node.Arguments[1] is UnaryExpression { Operand: LambdaExpression lambda })
            {
                // Extract native hints, and retain the whole predicate for exact in-memory filtering.
                this.ExtractHints(lambda.Body, lambda.Parameters[0]);
                this.predicates.Add(lambda.Body);
            }
            return node;
        }

        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Skip")
        {
            this.Visit(node.Arguments[0]);
            if (node.Arguments[1] is ConstantExpression constant)
                this.descriptor.Skip = (int)constant.Value!;
            return node;
        }

        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Take")
        {
            this.Visit(node.Arguments[0]);
            if (node.Arguments[1] is ConstantExpression constant)
                this.descriptor.Take = (int)constant.Value!;
            return node;
        }

        return base.VisitMethodCall(node);
    }

    void ExtractHints(Expression body, ParameterExpression parameter)
    {
        // Walk AND (&&) chains, inspecting each leaf for a native-translatable hint.
        if (body is BinaryExpression { NodeType: ExpressionType.AndAlso } andAlso)
        {
            this.ExtractHints(andAlso.Left, parameter);
            this.ExtractHints(andAlso.Right, parameter);
            return;
        }

        if (body is BinaryExpression binary)
            this.TryExtractComparison(binary, parameter);
    }

    void TryExtractComparison(BinaryExpression binary, ParameterExpression parameter)
    {
        // Normalise so the member (e.PropertyName) is on the left.
        var (member, valueExpr, op) = Normalise(binary, parameter);
        if (member == null || valueExpr == null)
            return;

        var propName = member.Member.Name;

        if (propName == nameof(CalendarEvent.CalendarId) && op == ExpressionType.Equal)
        {
            if (TryGetValue(valueExpr, out var v) && v is string s)
                this.descriptor.CalendarId = s;
            return;
        }

        if (propName is nameof(CalendarEvent.Start) or nameof(CalendarEvent.End))
        {
            if (!TryGetValue(valueExpr, out var raw) || !TryToDateTimeOffset(raw, out var dto))
                return;

            switch (op)
            {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    // e.Start >= d  → window lower bound
                    this.descriptor.StartAfter = Max(this.descriptor.StartAfter, dto);
                    break;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    // e.End <= d  → window upper bound
                    this.descriptor.EndBefore = Min(this.descriptor.EndBefore, dto);
                    break;
            }
        }
    }

    /// <summary>
    /// Returns (member, value, op) with the member on the left. If the constant was on the left the
    /// comparison operator is flipped so the extracted bound keeps the right meaning.
    /// </summary>
    static (MemberExpression? Member, Expression? Value, ExpressionType Op) Normalise(BinaryExpression binary, ParameterExpression parameter)
    {
        if (binary.Left is MemberExpression lm && lm.Expression == parameter)
            return (lm, binary.Right, binary.NodeType);

        if (binary.Right is MemberExpression rm && rm.Expression == parameter)
            return (rm, binary.Left, Flip(binary.NodeType));

        return (null, null, binary.NodeType);
    }

    static ExpressionType Flip(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op
    };

    static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset b) => a == null || b > a.Value ? b : a;
    static DateTimeOffset? Min(DateTimeOffset? a, DateTimeOffset b) => a == null || b < a.Value ? b : a;

    static bool TryToDateTimeOffset(object? raw, out DateTimeOffset value)
    {
        switch (raw)
        {
            case DateTimeOffset dto:
                value = dto;
                return true;
            case DateTime dt:
                value = new DateTimeOffset(dt);
                return true;
            default:
                value = default;
                return false;
        }
    }

    static bool TryGetValue(Expression expr, out object? value)
    {
        value = null;

        if (expr is ConstantExpression constant)
        {
            value = constant.Value;
            return true;
        }

        // Captured variables (closures) / static members - resolve by walking the member chain.
        return TryResolveCapturedValue(expr, out value);
    }

    /// <summary>
    /// Resolves a captured variable value by walking the member/constant expression chain.
    /// AOT-safe: uses direct member access on known constant instances.
    /// </summary>
    static bool TryResolveCapturedValue(Expression expr, out object? result)
    {
        result = null;

        if (expr is not MemberExpression member)
            return false;

        if (member.Expression is ConstantExpression closureConst)
        {
            result = GetMemberValue(member.Member, closureConst.Value);
            return true;
        }

        // Static member (e.g. DateTimeOffset.Now captured via a local is handled above; this covers statics)
        if (member.Expression == null)
        {
            result = GetMemberValue(member.Member, null);
            return true;
        }

        // Nested member access: closure.outer.Field
        if (member.Expression is MemberExpression && TryResolveCapturedValue(member.Expression, out var parent))
        {
            result = GetMemberValue(member.Member, parent);
            return true;
        }

        return false;
    }

    static object? GetMemberValue(MemberInfo memberInfo, object? instance) => memberInfo switch
    {
        FieldInfo fi => fi.GetValue(instance),
        PropertyInfo pi => pi.GetValue(instance),
        _ => throw new NotSupportedException($"Member type '{memberInfo.MemberType}' is not supported for value resolution.")
    };

    void BuildInMemoryPredicate()
    {
        if (this.predicates.Count == 0) return;

        var param = Expression.Parameter(typeof(CalendarEvent), "e");
        Expression? combined = null;

        foreach (var pred in this.predicates)
        {
            var rewritten = new ParameterReplacer(param).Visit(pred);
            combined = combined == null ? rewritten : Expression.AndAlso(combined, rewritten);
        }

        if (combined != null)
            this.descriptor.InMemoryPredicate = Expression.Lambda<Func<CalendarEvent, bool>>(combined, param);
    }

    /// <summary>
    /// Replaces lambda parameter references so multiple Where predicates can be combined into one.
    /// </summary>
    class ParameterReplacer(ParameterExpression newParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node.Type == typeof(CalendarEvent) ? newParameter : base.VisitParameter(node);
    }
}
