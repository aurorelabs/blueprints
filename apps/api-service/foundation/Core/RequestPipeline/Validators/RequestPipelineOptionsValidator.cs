using System;
using System.Net;
using Aurore.Foundation.Core.RequestPipeline.Models;
using FluentValidation;

namespace Aurore.Foundation.Core.RequestPipeline.Validators;

/// <summary>Checks the request pipeline settings at startup, naming the configuration key of every problem.</summary>
public sealed class RequestPipelineOptionsValidator : AbstractValidator<RequestPipelineOptions>
{
    /// <summary>Creates the validator.</summary>
    public RequestPipelineOptionsValidator()
    {
        RuleFor(x => x.ForwardedHeaders.KnownProxies).Custom((proxies, context) =>
        {
            foreach (var proxy in proxies)
            {
                if (IPAddress.TryParse(proxy, out _) is false)
                {
                    context.AddFailure($"'{proxy}' is not a valid IP address.");
                }
            }
        });

        RuleFor(x => x.ForwardedHeaders.KnownNetworks).Custom((networks, context) =>
        {
            foreach (var network in networks)
            {
                if (IPNetwork.TryParse(network, out _) is false)
                {
                    context.AddFailure($"'{network}' is not a valid network. Use CIDR notation, e.g. '172.18.0.0/16'.");
                }
            }
        });

        RuleFor(x => x.KestrelLimits.MaxConcurrentConnections).GreaterThan(0L);
        RuleFor(x => x.KestrelLimits.MaxRequestHeaderCount).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.RequestHeadersTimeoutSeconds).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.KeepAliveTimeoutSeconds).GreaterThan(0);
        RuleFor(x => x.KestrelLimits.MaxRequestBodySizeBytes).GreaterThan(0L);

        RuleFor(x => x.Idempotency.CacheDurationSeconds).GreaterThan(0);

        RuleFor(x => x.Timeouts.DefaultSeconds).GreaterThan(0);

        // Dictionary entries are reported with the configuration key (RequestPipeline:RateLimiting:Policies:tests:PermitLimit).
        RuleFor(x => x.Timeouts.Policies).Custom((policies, context) =>
        {
            foreach (var (name, seconds) in policies)
            {
                if (seconds <= 0)
                {
                    context.AddFailure($"Timeouts:Policies:{name}", $"'{name}' must be greater than zero, but was {seconds}.");
                }
            }
        });

        RuleFor(x => x.RateLimiting.Policies).Custom((policies, context) =>
        {
            foreach (var (name, policy) in policies)
            {
                AddIfNotPositive(context, $"RateLimiting:Policies:{name}:PermitLimit", policy.PermitLimit);
                AddIfNotPositive(context, $"RateLimiting:Policies:{name}:WindowSeconds", policy.WindowSeconds);
            }
        });

        RuleFor(x => x.OutputCache.Policies).Custom((policies, context) =>
        {
            foreach (var (name, policy) in policies)
            {
                AddIfNotPositive(context, $"OutputCache:Policies:{name}:ExpirationSeconds", policy.ExpirationSeconds);
            }
        });

        RuleFor(x => x.Privacy).Custom((privacy, context) =>
        {
            if (privacy.Enabled is false)
                return;

            ValidateKey(context, "Privacy:Keys:Current", privacy.Keys.Current);

            for (var i = 0; i < privacy.Keys.Previous.Length; i++)
            {
                ValidateKey(context, $"Privacy:Keys:Previous:{i}", privacy.Keys.Previous[i]);
            }
        });

        RuleFor(x => x.CorsPolicies).Custom((policies, context) =>
        {
            foreach (var (name, policy) in policies)
            {
                if (policy.PreflightMaxAgeSeconds < 0)
                {
                    context.AddFailure($"CorsPolicies:{name}:PreflightMaxAgeSeconds", "Must not be negative.");
                }
            }
        });
    }

    private static void ValidateKey(ValidationContext<RequestPipelineOptions> context, string path, string secret)
    {
        var buffer = new byte[32];

        if (Convert.TryFromBase64String(secret, buffer, out var written) is false || written != 32)
        {
            context.AddFailure(path, "Must be 32 random bytes, base64 encoded (set it through an environment variable).");
        }
    }

    private static void AddIfNotPositive(ValidationContext<RequestPipelineOptions> context, string key, int value)
    {
        if (value <= 0)
        {
            context.AddFailure(key, $"Must be greater than zero, but was {value}.");
        }
    }
}
