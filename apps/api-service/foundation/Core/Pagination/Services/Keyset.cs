using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Aurore.Foundation.Core.Pagination.Models;

namespace Aurore.Foundation.Core.Pagination.Services;

/// <summary>
/// Builds the condition "comes after this row" from the orderings and the values of the last row of the previous page:
/// for orderings a, b, id it is (a after A) or (a = A and b after B) or (a = A and b = B and id after ID).
/// </summary>
internal static class Keyset
{
    /// <summary>Throws JsonException when a value does not fit its field.</summary>
    internal static Expression<Func<T, bool>> After<T>(IReadOnlyList<SortStep<T>> steps, JsonElement[] values)
    {
        var row = Expression.Parameter(typeof(T), "x");
        var members = steps.Select(x => x.Field.Member(row)).ToArray();
        var bounds = steps.Select((x, index) => x.Field.Bound(values[index])).ToArray();

        Expression? any = null;

        for (var index = 0; index < steps.Count; index++)
        {
            var term = Compare(members[index], bounds[index], steps[index].Item.Direction is SortDirection.Asc);

            for (var earlier = index - 1; earlier >= 0; earlier--)
                term = Expression.AndAlso(Expression.Equal(members[earlier], bounds[earlier]), term);

            any = any is null ? term : Expression.OrElse(any, term);
        }

        return Expression.Lambda<Func<T, bool>>(any!, row);
    }

    private static Expression Compare(Expression member, Expression bound, bool greater)
    {
        var type = member.Type;

        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
            member = Expression.Convert(member, type);
            bound = Expression.Convert(bound, type);
        }

        // Strings and Guids have no comparison operators; CompareTo is what providers translate for them.
        if (type == typeof(string) || type == typeof(Guid))
        {
            var compare = Expression.Call(member, type.GetMethod(nameof(string.CompareTo), [type])!, bound);

            return greater
                ? Expression.GreaterThan(compare, Expression.Constant(0))
                : Expression.LessThan(compare, Expression.Constant(0));
        }

        return greater ? Expression.GreaterThan(member, bound) : Expression.LessThan(member, bound);
    }
}
