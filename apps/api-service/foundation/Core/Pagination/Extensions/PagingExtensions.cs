using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Pagination.Errors;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Pagination.Services;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Core.Pagination.Extensions;

/// <summary>
/// Turns a list request into a query: validates the paging, ordering and date parameters, applies them and builds the
/// page. Every problem found comes back together as one 400 with the offending parameters as details.
/// </summary>
public static class PagingExtensions
{
    /// <summary>
    /// Orders, counts and cuts the query into the requested page. This runs the count synchronously, so use it for in-memory
    /// sources; with Entity Framework Core use its asynchronous version, which shares these rules.
    /// </summary>
    public static Result<Page<T>> ToPage<T>(this IQueryable<T> query, PageRequest request, PaginationOptions options, SortMap<T>? sortMap = null)
    {
        var problems = new Problems();
        var (page, pageSize) = ReadWindow(request, options, problems);
        var orderings = ReadOrderings(request, sortMap, problems);

        if (problems.Any)
            return problems.ToError();

        var sorted = SortMap<T>.Apply(query, SortMap<T>.Plan(sortMap, orderings));
        var total = query.Count();
        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Page<T>.Create(items, page, pageSize, total);
    }

    /// <summary>
    /// Orders the query and cuts the page that follows the cursor of the request (the first page when it has none), fetching
    /// one extra row to know whether there is a next page. Nothing is kept between calls: the cursor carries the position.
    /// This runs synchronously, so use it for in-memory sources; with Entity Framework Core use its asynchronous version.
    /// Ordering by a nullable property is not supported, because a comparison with a null never matches: such a request
    /// throws, since that is a mistake of the developer who allowed the field.
    /// </summary>
    public static Result<CursorPage<T>> ToCursorPage<T>(this IQueryable<T> query, CursorRequest request, PaginationOptions options, SortMap<T>? sortMap = null)
    {
        var problems = new Problems();
        var pageSize = ReadPageSize(request.PageSize, options, problems);
        var orderings = ReadOrderings(request, sortMap, problems);

        if (problems.Any)
            return problems.ToError();

        var steps = SortMap<T>.Plan(sortMap, orderings);

        if (steps.Any(x => x.Item.Field.Equals(SortMap.IdName, StringComparison.OrdinalIgnoreCase) is false && Nullable.GetUnderlyingType(x.Field.KeyType) is not null))
            throw new InvalidOperationException("A cursor cannot order by a nullable property: rows with a null never match the comparison. Do not allow it, or use numbered pages.");

        if (steps.Count == 0 || steps[^1].Item.Field.Equals(SortMap.IdName, StringComparison.OrdinalIgnoreCase) is false)
            throw new InvalidOperationException($"{typeof(T).Name} has no 'Id' property, which cursors need to tell equal rows apart.");

        var fingerprint = request.Fingerprint();

        if (string.IsNullOrEmpty(request.Cursor) is false)
        {
            var decoded = CursorCodec.Decode(request.Cursor);

            if (decoded is null || decoded.Values.Length != steps.Count)
                return PaginationErrors.InvalidCursor.WithDetails(CursorDetails());

            if (decoded.Fingerprint != fingerprint)
                return PaginationErrors.CursorMismatch.WithDetails(CursorDetails());

            try
            {
                query = query.Where(Keyset.After(steps, decoded.Values));
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                return PaginationErrors.InvalidCursor.WithDetails(CursorDetails());
            }
        }

        var rows = SortMap<T>.Apply(query, steps).Take(pageSize + 1).ToList();
        var hasMore = rows.Count > pageSize;

        if (hasMore)
            rows.RemoveAt(pageSize);

        var next = hasMore
            ? CursorCodec.Encode(fingerprint, [.. steps.Select(x => x.Field.Read(rows[^1]))])
            : null;

        return new CursorPage<T> { Items = rows, NextCursor = next, PageSize = pageSize };
    }

    /// <summary>
    /// Filters by the creation and update ranges of the request: From is inclusive and To is exclusive. Pass the selector
    /// for each range the request can carry.
    /// </summary>
    public static Result<IQueryable<T>> WhereTimestamps<T>(
        this IQueryable<T> query,
        ITimestampFilter filter,
        Expression<Func<T, DateTimeOffset>>? created = null,
        Expression<Func<T, DateTimeOffset>>? updated = null)
    {
        var problems = new Problems();

        CheckRange(filter.CreatedFrom, filter.CreatedTo, "createdFrom", "createdTo", problems);
        CheckRange(filter.UpdatedFrom, filter.UpdatedTo, "updatedFrom", "updatedTo", problems);

        if (problems.Any)
            return problems.ToError();

        query = Range(query, created, filter.CreatedFrom, filter.CreatedTo, nameof(created));
        query = Range(query, updated, filter.UpdatedFrom, filter.UpdatedTo, nameof(updated));

        return Result<IQueryable<T>>.Success(query);
    }

    internal static (int Page, int PageSize) ReadWindow(PageRequest request, PaginationOptions options, Problems problems)
    {
        var page = request.Page ?? 1;
        var pageSize = ReadPageSize(request.PageSize, options, problems);

        if (page < 1)
            problems.Add("page", "The page must be 1 or greater.");

        if (page >= 1 && pageSize >= 1 && (long)(page - 1) * pageSize > int.MaxValue)
            problems.Add("page", "The page is too far out.");

        return (page, pageSize);
    }

    internal static int ReadPageSize(int? requested, PaginationOptions options, Problems problems)
    {
        var pageSize = requested ?? options.DefaultPageSize;

        if (pageSize < 1 || pageSize > options.MaxPageSize)
            problems.Add("pageSize", $"The page size must be between 1 and {options.MaxPageSize}.");

        return pageSize;
    }

    private static Dictionary<string, string[]> CursorDetails()
    {
        return new Dictionary<string, string[]> { ["cursor"] = ["Send the nextCursor of a previous response unchanged, with the same filters and ordering."] };
    }

    /// <summary>The orderings the request asks for, or the default of the map when it asks for none.</summary>
    internal static List<SortItem> ReadOrderings<T>(SearchRequest request, SortMap<T>? sortMap, Problems problems)
    {
        var items = new List<SortItem>();

        foreach (var value in request.OrderBy ?? [])
        {
            if (SortItem.TryParse(value, out var item) is false)
            {
                problems.Add("orderBy", $"'{value}' is not valid. Use field or field:asc or field:desc.");
            }
            else if (sortMap is null || sortMap.Allows(item.Field) is false)
            {
                var allowed = sortMap is null ? "none" : string.Join(", ", sortMap.Fields.Order());
                problems.Add("orderBy", $"'{item.Field}' cannot be ordered by. Allowed fields: {allowed}.");
            }
            else if (items.Any(x => string.Equals(x.Field, item.Field, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("orderBy", $"'{item.Field}' is ordered by more than once.");
            }
            else
            {
                items.Add(item);
            }
        }

        if (items.Count == 0 && sortMap is not null)
            items.AddRange(sortMap.Default);

        return items;
    }

    private static void CheckRange(DateTimeOffset? from, DateTimeOffset? to, string fromName, string toName, Problems problems)
    {
        if (from is { } start && to is { } end && start >= end)
            problems.Add(toName, $"'{toName}' must be after '{fromName}'.");
    }

    private static IQueryable<T> Range<T>(IQueryable<T> query, Expression<Func<T, DateTimeOffset>>? selector, DateTimeOffset? from, DateTimeOffset? to, string name)
    {
        if (from is null && to is null)
            return query;

        if (selector is null)
            throw new InvalidOperationException($"The request carries a range for '{name}', but no selector was given for it.");

        // The bounds are read from a holder, not inlined as constants, so the provider can parameterize the query.
        Expression? body = null;

        if (from is { } start)
            body = Expression.GreaterThanOrEqual(selector.Body, Bound(start));

        if (to is { } end)
        {
            var before = Expression.LessThan(selector.Body, Bound(end));
            body = body is null ? before : Expression.AndAlso(body, before);
        }

        return query.Where(Expression.Lambda<Func<T, bool>>(body!, selector.Parameters));
    }

    private static MemberExpression Bound(DateTimeOffset value)
    {
        return Expression.Property(Expression.Constant(new Holder(value)), nameof(Holder.Value));
    }

    private sealed record Holder(DateTimeOffset Value);

    /// <summary>Collects the invalid parameters of a request so they are all reported together.</summary>
    internal sealed class Problems
    {
        private readonly Dictionary<string, List<string>> _fields = [];

        internal bool Any => _fields.Count > 0;

        internal void Add(string field, string message)
        {
            if (_fields.TryGetValue(field, out var messages) is false)
                _fields[field] = messages = [];

            messages.Add(message);
        }

        internal ApplicationError ToError()
        {
            return PaginationErrors.InvalidRequest.WithDetails(_fields.ToDictionary(x => x.Key, x => x.Value.ToArray()));
        }
    }
}
