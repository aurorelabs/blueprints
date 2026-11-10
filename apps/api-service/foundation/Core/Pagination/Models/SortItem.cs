using System;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>One ordering of a request: a field and a direction, written "name", "name:asc" or "name:desc".</summary>
public sealed record SortItem(string Field, SortDirection Direction)
{
    /// <summary>
    /// Reads "field" or "field:direction" (asc or desc, any casing). A missing direction means ascending.
    /// </summary>
    public static bool TryParse(string? value, out SortItem item)
    {
        item = default!;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split(':');

        if (parts.Length > 2)
            return false;

        var field = parts[0].Trim();

        if (field.Length == 0)
            return false;

        var direction = SortDirection.Asc;

        if (parts.Length == 2)
        {
            var text = parts[1].Trim();

            if (text.Equals("asc", StringComparison.OrdinalIgnoreCase))
                direction = SortDirection.Asc;
            else if (text.Equals("desc", StringComparison.OrdinalIgnoreCase))
                direction = SortDirection.Desc;
            else
                return false;
        }

        item = new SortItem(field, direction);

        return true;
    }
}
