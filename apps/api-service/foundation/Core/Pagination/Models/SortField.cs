using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Linq;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// One allowed field of a SortMap: how to order by it, and what a cursor needs from it (read its value from an item, and
/// compare against a value that came back in a cursor).
/// </summary>
internal abstract class SortField<T>
{
    /// <summary>The type of the property.</summary>
    internal abstract Type KeyType { get; }

    /// <summary>Orders the query by the field. The first ordering uses OrderBy, the following ones ThenBy.</summary>
    internal abstract IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction, bool first);

    /// <summary>Reads the value of the field from an item, ready to go into a cursor.</summary>
    internal abstract JsonElement Read(T item);

    /// <summary>The field's expression, written for the given parameter so several fields can share one lambda.</summary>
    internal abstract Expression Member(ParameterExpression parameter);

    /// <summary>The value that came back in a cursor, as an expression of the field's type. Throws JsonException when it does not fit.</summary>
    internal abstract Expression Bound(JsonElement value);
}

internal sealed class SortField<T, TKey>(Expression<Func<T, TKey>> key) : SortField<T>
{
    private readonly Lazy<Func<T, TKey>> _read = new(key.Compile);

    internal override Type KeyType => typeof(TKey);

    internal override IOrderedQueryable<T> Order(IQueryable<T> query, SortDirection direction, bool first)
    {
        return (first, direction) switch
        {
            (true, SortDirection.Asc) => query.OrderBy(key),
            (true, SortDirection.Desc) => query.OrderByDescending(key),
            (false, SortDirection.Asc) => ((IOrderedQueryable<T>)query).ThenBy(key),
            _ => ((IOrderedQueryable<T>)query).ThenByDescending(key),
        };
    }

    internal override JsonElement Read(T item)
    {
        return JsonSerializer.SerializeToElement(_read.Value(item));
    }

    internal override Expression Member(ParameterExpression parameter)
    {
        return new Replace(key.Parameters[0], parameter).Visit(key.Body);
    }

    internal override Expression Bound(JsonElement value)
    {
        var bound = value.Deserialize<TKey>() ?? throw new JsonException("A cursor value cannot be null.");

        // Read from a holder, not inlined as a constant, so the provider can parameterize the query.
        return Expression.Property(Expression.Constant(new Holder(bound)), nameof(Holder.Value));
    }

    private sealed record Holder(TKey Value);

    private sealed class Replace(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }
}
