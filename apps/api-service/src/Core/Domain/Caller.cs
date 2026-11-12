namespace Aurore.Blueprints.ApiService.Core.Domain;

/// <summary>
/// Who is calling, as the features need to know it: the subject of the identity and whether it is an admin. Built by the
/// endpoint from the token, never read from the request body or the query.
/// </summary>
/// <param name="Subject">The subject (sub claim) of the caller, or the name of the API key client.</param>
/// <param name="IsAdmin">True when the caller has the admin role.</param>
public sealed record Caller(string Subject, bool IsAdmin);
