using System.Linq.Expressions;
using System.Reflection;

namespace Shiny.Contacts.Internals;

/// <summary>
/// Visits a LINQ expression tree and extracts Contact query filters
/// that can be translated to native platform queries.
/// </summary>
public class ContactExpressionVisitor : ExpressionVisitor
{
    // String properties on Contact that can be natively filtered
    static readonly HashSet<string> FilterableProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Contact.GivenName),
        nameof(Contact.FamilyName),
        nameof(Contact.MiddleName),
        nameof(Contact.NamePrefix),
        nameof(Contact.NameSuffix),
        nameof(Contact.Nickname),
        nameof(Contact.DisplayName),
        nameof(Contact.Note)
    };

    // Collection sub-properties that can be filtered
    static readonly Dictionary<string, string> CollectionSubProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Phones"] = nameof(ContactPhone.Number),
        ["Emails"] = nameof(ContactEmail.Address),
    };

    static readonly HashSet<string> StringMethods = new()
    {
        nameof(string.Contains),
        nameof(string.StartsWith),
        nameof(string.EndsWith),
        nameof(string.Equals)
    };

    readonly ContactQueryDescriptor descriptor = new();
    readonly List<Expression> unsupportedPredicates = [];

    public static ContactQueryDescriptor Parse(Expression expression)
    {
        var visitor = new ContactExpressionVisitor();
        visitor.Visit(expression);
        visitor.BuildInMemoryPredicate();
        return visitor.descriptor;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Handle Queryable.Where
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
        {
            Visit(node.Arguments[0]); // visit source

            if (node.Arguments[1] is UnaryExpression unary &&
                unary.Operand is LambdaExpression lambda)
            {
                ProcessPredicate(lambda.Body, lambda.Parameters[0]);
            }

            return node;
        }

        // Handle Queryable.Skip
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Skip")
        {
            Visit(node.Arguments[0]);
            if (node.Arguments[1] is ConstantExpression constant)
                descriptor.Skip = (int)constant.Value!;
            return node;
        }

        // Handle Queryable.Take
        if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Take")
        {
            Visit(node.Arguments[0]);
            if (node.Arguments[1] is ConstantExpression constant)
                descriptor.Take = (int)constant.Value!;
            return node;
        }

        return base.VisitMethodCall(node);
    }

    void ProcessPredicate(Expression body, ParameterExpression parameter)
    {
        // Handle AND (&&) expressions — process both sides
        if (body is BinaryExpression binary && binary.NodeType == ExpressionType.AndAlso)
        {
            ProcessPredicate(binary.Left, parameter);
            ProcessPredicate(binary.Right, parameter);
            return;
        }

        if (TryExtractFilter(body, parameter, out var filter))
        {
            descriptor.Filters.Add(filter);
        }
        else
        {
            unsupportedPredicates.Add(body);
        }
    }

    bool TryExtractFilter(Expression expr, ParameterExpression parameter, out ContactQueryFilter filter)
    {
        filter = new ContactQueryFilter();

        if (expr is not MethodCallExpression methodCall)
            return false;

        // Direct string method: c.GivenName.Contains("Jo")
        if (methodCall.Object is MemberExpression memberExpr &&
            StringMethods.Contains(methodCall.Method.Name) &&
            TryGetStringValue(methodCall.Arguments[0], out var value))
        {
            if (memberExpr.Expression == parameter &&
                memberExpr.Member is PropertyInfo prop &&
                FilterableProperties.Contains(prop.Name))
            {
                filter.PropertyName = prop.Name;
                filter.Value = value;
                filter.Operation = ParseOperation(methodCall.Method.Name);
                return true;
            }

            // Nested: c.Organization.Company.Contains("X")
            if (memberExpr.Expression is MemberExpression parentMember &&
                parentMember.Expression == parameter &&
                parentMember.Member.Name == nameof(Contact.Organization))
            {
                filter.PropertyName = nameof(Contact.Organization);
                filter.SubPropertyName = memberExpr.Member.Name;
                filter.Value = value;
                filter.Operation = ParseOperation(methodCall.Method.Name);
                return true;
            }
        }

        // Collection.Any(): c.Phones.Any(p => p.Number.Contains("555"))
        if (methodCall.Method.Name == "Any" &&
            methodCall.Arguments.Count == 2 &&
            methodCall.Method.DeclaringType == typeof(Enumerable))
        {
            if (methodCall.Arguments[0] is MemberExpression collectionMember &&
                collectionMember.Expression == parameter &&
                CollectionSubProperties.ContainsKey(collectionMember.Member.Name))
            {
                if (methodCall.Arguments[1] is LambdaExpression lambda &&
                    lambda.Body is MethodCallExpression innerCall &&
                    innerCall.Object is MemberExpression innerMember &&
                    StringMethods.Contains(innerCall.Method.Name) &&
                    TryGetStringValue(innerCall.Arguments[0], out var innerValue))
                {
                    filter.PropertyName = collectionMember.Member.Name;
                    filter.SubPropertyName = innerMember.Member.Name;
                    filter.Value = innerValue;
                    filter.Operation = ParseOperation(innerCall.Method.Name);
                    return true;
                }
            }
        }

        return false;
    }

    static bool TryGetStringValue(Expression expr, out string value)
    {
        value = string.Empty;

        if (expr is ConstantExpression constant && constant.Value is string s)
        {
            value = s;
            return true;
        }

        // Handle captured variables (closures) — resolve value by walking the member chain
        if (TryResolveCapturedValue(expr, out var resolved) && resolved is string sv)
        {
            value = sv;
            return true;
        }

        return false;
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

        // Walk the chain to find the root constant (closure object)
        if (member.Expression is ConstantExpression closureConst)
        {
            result = GetMemberValue(member.Member, closureConst.Value);
            return true;
        }

        // Nested member access: e.g. closure.outer.Field
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

    static ContactFilterOperation ParseOperation(string methodName) => methodName switch
    {
        nameof(string.Contains) => ContactFilterOperation.Contains,
        nameof(string.StartsWith) => ContactFilterOperation.StartsWith,
        nameof(string.EndsWith) => ContactFilterOperation.EndsWith,
        nameof(string.Equals) => ContactFilterOperation.Equals,
        _ => ContactFilterOperation.Contains
    };

    void BuildInMemoryPredicate()
    {
        if (unsupportedPredicates.Count == 0) return;

        var param = Expression.Parameter(typeof(Contact), "c");
        Expression? combined = null;

        foreach (var pred in unsupportedPredicates)
        {
            var rewritten = new ParameterReplacer(param).Visit(pred);
            combined = combined == null ? rewritten : Expression.AndAlso(combined, rewritten);
        }

        if (combined != null)
            descriptor.InMemoryPredicate = Expression.Lambda<Func<Contact, bool>>(combined, param);
    }

    /// <summary>
    /// Replaces lambda parameter references in captured unsupported expressions
    /// so they can be combined into a single in-memory predicate.
    /// </summary>
    class ParameterReplacer : ExpressionVisitor
    {
        readonly ParameterExpression newParameter;
        public ParameterReplacer(ParameterExpression newParameter) => this.newParameter = newParameter;

        protected override Expression VisitParameter(ParameterExpression node)
            => node.Type == typeof(Contact) ? newParameter : base.VisitParameter(node);
    }
}
