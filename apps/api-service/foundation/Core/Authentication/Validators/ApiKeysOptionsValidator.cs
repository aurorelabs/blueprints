using System;
using System.Linq;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using FluentValidation;

namespace Aurore.Foundation.Core.Authentication.Validators;

/// <summary>Checks the API key settings; production also requires long keys.</summary>
public sealed class ApiKeysOptionsValidator : AbstractValidator<ApiKeysOptions>
{
    /// <summary>The shortest key production accepts.</summary>
    public const int MinimumProductionKeyLength = 32;

    /// <summary>Creates the validator. The flag turns on the stricter production rules.</summary>
    public ApiKeysOptionsValidator(bool isProduction)
    {
        RuleFor(x => x.HeaderName).NotEmpty();

        // Reported with the client name (Clients:billing:Key), not a positional index.
        RuleFor(x => x.Clients).Custom((clients, context) =>
        {
            foreach (var (name, client) in clients)
            {
                if (client.Key.HasValue() is false)
                    context.AddFailure($"Clients:{name}:Key", $"API key client '{name}' has an empty Key.");
                else if (isProduction && client.Key.Length < MinimumProductionKeyLength)
                    context.AddFailure($"Clients:{name}:Key", $"Production API keys must be at least {MinimumProductionKeyLength} characters. Supply them through environment variables or a secret store.");
            }

            var shared = clients
                .Where(x => x.Value.Key.HasValue())
                .GroupBy(x => x.Value.Key, StringComparer.Ordinal)
                .FirstOrDefault(x => x.Count() > 1);

            if (shared is not null)
                context.AddFailure("Clients", $"API key clients {string.Join(", ", shared.Select(x => $"'{x.Key}'"))} share the same Key.");
        });
    }
}
