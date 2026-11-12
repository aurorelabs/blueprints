using System;
using System.Security.Claims;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;

namespace Aurore.Blueprints.ApiService.App.Api.Resources;

internal static class CallerExtensions
{
    /// <summary>
    /// Who is calling, from the token (or the API key client). The features never take this from the body or the query: a client
    /// cannot say it is somebody else.
    /// </summary>
    public static Caller ToCaller(this ClaimsPrincipal user)
    {
        var subject = user.GetUserId()
            ?? throw new InvalidOperationException("The caller has no subject. The endpoint must require authorization.");

        return new Caller(subject, user.IsInRole("admin"));
    }
}
