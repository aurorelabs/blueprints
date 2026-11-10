using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>Creates the SortMap of a type: SortMap.For&lt;Customer&gt;().Allow(x =&gt; x.Name).ById().</summary>
public static class SortMap
{
    /// <summary>The name of the primary key, which is always "Id", and of its ordering in the requests.</summary>
    public const string IdName = "id";

    /// <summary>Starts an empty map for the type being listed.</summary>
    public static SortMap<T> For<T>()
    {
        return new SortMap<T>();
    }
}

/// <summary>One ordering of a plan: what to order by and in which direction.</summary>
internal sealed record SortStep<T>(SortItem Item, SortField<T> Field);

/// <summary>
/// The fields a list may be ordered by, declared once per entity on the feature side: the name a client sends and the
/// property it stands for. A name that is not declared is answered with a 400, so clients can only sort by columns you chose.
/// Every page is ordered: by what the request asks, else by the default of the map, else by id, and the id is always added as
/// the last ordering so rows with equal values never change places between pages.
/// </summary>
/// <typeparam name="T">The type being listed. Its primary key is the property named Id.</typeparam>
public sealed class SortMap<T>
{
    private static readonly Lazy<SortField<T>?> _id = new(BuildIdField);

    private readonly Dictionary<string, SortField<T>> _fields = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SortItem> _default = [];

    internal SortMap()
    {
    }

    /// <summary>The names a client may order by.</summary>
    public IReadOnlyCollection<string> Fields => _fields.Keys;

    /// <summary>
    /// Allows ordering by a property. The client's name for it is the property name in camelCase (x =&gt; x.CreatedAt is
    /// "createdAt"; matching ignores case), or the name given. A path through another object (x =&gt; x.Customer.Name) has no
    /// obvious name, so it needs one. Keep the expression translatable to SQL: plain properties, no method calls of your own.
    /// </summary>
    public SortMap<T> Allow<TKey>(Expression<Func<T, TKey>> key, string? name = null)
    {
        Register(key, name);

        return this;
    }

    /// <summary>
    /// The ordering used when the request sends none, by property (the field is allowed too, if it was not already). Without
    /// a default, or ById, the lists are ordered by id.
    /// </summary>
    public SortMap<T> DefaultTo<TKey>(Expression<Func<T, TKey>> key, SortDirection direction = SortDirection.Asc, string? name = null)
    {
        _default.Add(new SortItem(Register(key, name), direction));

        return this;
    }

    /// <summary>
    /// The ordering used when the request sends none, written like the query ("createdAt:desc"). The name must have been
    /// allowed.
    /// </summary>
    public SortMap<T> DefaultTo(params string[] orderBy)
    {
        foreach (var value in orderBy)
        {
            if (SortItem.TryParse(value, out var item) is false || _fields.ContainsKey(item.Field) is false)
                throw new ArgumentException($"'{value}' is not an allowed ordering.", nameof(orderBy));

            _default.Add(item);
        }

        return this;
    }

    /// <summary>
    /// Allows ordering by id and makes it the default. This is what a list without any default does anyway, so it only says
    /// so out loud.
    /// </summary>
    public SortMap<T> ById(SortDirection direction = SortDirection.Asc)
    {
        _fields[SortMap.IdName] = _id.Value ?? throw new InvalidOperationException($"{typeof(T).Name} has no 'Id' property, so ById cannot be used.");
        _default.Add(new SortItem(SortMap.IdName, direction));

        return this;
    }

    internal IReadOnlyList<SortItem> Default => _default;

    internal bool Allows(string field)
    {
        return _fields.ContainsKey(field);
    }

    /// <summary>
    /// The orderings to apply: the requested items, then the id unless they already include it. A type with no Id can only be
    /// ordered by what the items say.
    /// </summary>
    internal static List<SortStep<T>> Plan(SortMap<T>? map, IReadOnlyList<SortItem> items)
    {
        var steps = items.Select(x => new SortStep<T>(x, map!._fields[x.Field])).ToList();

        if (items.Any(x => string.Equals(x.Field, SortMap.IdName, StringComparison.OrdinalIgnoreCase)))
            return steps;

        if (_id.Value is { } id)
            steps.Add(new SortStep<T>(new SortItem(SortMap.IdName, SortDirection.Asc), id));
        else if (steps.Count == 0)
            throw new InvalidOperationException($"{typeof(T).Name} has no 'Id' property, so it needs a SortMap with a default: DefaultTo(x => x.Something).");

        return steps;
    }

    internal static IQueryable<T> Apply(IQueryable<T> query, IReadOnlyList<SortStep<T>> steps)
    {
        for (var index = 0; index < steps.Count; index++)
            query = steps[index].Field.Order(query, steps[index].Item.Direction, index == 0);

        return query;
    }

    private string Register<TKey>(Expression<Func<T, TKey>> key, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            if (key.Body is not MemberExpression member || member.Expression != key.Parameters[0])
                throw new ArgumentException("An ordering through another object needs an explicit name.", nameof(name));

            name = char.ToLowerInvariant(member.Member.Name[0]) + member.Member.Name[1..];
        }

        _fields[name] = new SortField<T, TKey>(key);

        return name;
    }

    private static SortField<T>? BuildIdField()
    {
        var property = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
            return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        var lambda = Expression.Lambda(Expression.Property(parameter, property), parameter);

        return (SortField<T>)Activator.CreateInstance(typeof(SortField<,>).MakeGenericType(typeof(T), property.PropertyType), lambda)!;
    }
}
