namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>The direction of one ordering. Always ascending unless the request says otherwise.</summary>
public enum SortDirection
{
    /// <summary>Smallest first.</summary>
    Asc,

    /// <summary>Largest first.</summary>
    Desc,
}
