using Aurore.Foundation.Core.RequestPipeline.Models;
using Aurore.Foundation.Core.RequestPipeline.Validators;
using Tests.Shared.Privacy;

namespace Tests.Core.RequestPipeline.Validators;

[Trait("Feature", "Request pipeline validation")]
public sealed class RequestPipelineOptionsValidatorTests
{
    private readonly RequestPipelineOptionsValidator _validator = new();

    [Fact(DisplayName = "The default options are valid")]
    public void DefaultOptionsAreValid()
    {
        // Arrange
        var options = new RequestPipelineOptions();

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Rejects proxies and networks that are not IP addresses or CIDR ranges")]
    public void RejectsInvalidProxiesAndNetworks()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            ForwardedHeaders = new() { KnownProxies = ["nginx"], KnownNetworks = ["172.18.0.0"] },
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "ForwardedHeaders.KnownProxies");
        Assert.Contains(result.Errors, x => x.PropertyName == "ForwardedHeaders.KnownNetworks");
    }

    [Fact(DisplayName = "Accepts an IPv4 proxy and a CIDR network")]
    public void AcceptsValidProxiesAndNetworks()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            ForwardedHeaders = new() { KnownProxies = ["10.0.0.10"], KnownNetworks = ["172.18.0.0/16"] },
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Rejects Kestrel limits and an idempotency duration that are zero")]
    public void RejectsNonPositiveLimits()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            KestrelLimits = new() { MaxConcurrentConnections = 0, MaxRequestHeaderCount = 0, KeepAliveTimeoutSeconds = 0, MaxRequestBodySizeBytes = 0 },
            Idempotency = new() { CacheDurationSeconds = 0 },
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "KestrelLimits.MaxConcurrentConnections");
        Assert.Contains(result.Errors, x => x.PropertyName == "KestrelLimits.MaxRequestHeaderCount");
        Assert.Contains(result.Errors, x => x.PropertyName == "KestrelLimits.KeepAliveTimeoutSeconds");
        Assert.Contains(result.Errors, x => x.PropertyName == "KestrelLimits.MaxRequestBodySizeBytes");
        Assert.Contains(result.Errors, x => x.PropertyName == "Idempotency.CacheDurationSeconds");
    }

    [Fact(DisplayName = "Named timeout, rate limit and output cache policies are checked and reported by their configuration key")]
    public void ChecksNamedPolicies()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            Timeouts = new() { DefaultSeconds = 0, Policies = { ["long-running"] = 0 } },
            RateLimiting = new() { Policies = { ["tests"] = new() { PermitLimit = 0, WindowSeconds = 0 } } },
            OutputCache = new() { Policies = { ["short"] = new() { ExpirationSeconds = 0 } } },
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Timeouts.DefaultSeconds");
        Assert.Contains(result.Errors, x => x.PropertyName == "Timeouts:Policies:long-running");
        Assert.Contains(result.Errors, x => x.PropertyName == "RateLimiting:Policies:tests:PermitLimit");
        Assert.Contains(result.Errors, x => x.PropertyName == "RateLimiting:Policies:tests:WindowSeconds");
        Assert.Contains(result.Errors, x => x.PropertyName == "OutputCache:Policies:short:ExpirationSeconds");
    }

    [Fact(DisplayName = "Rejects a negative CORS preflight max age")]
    public void RejectsNegativePreflightMaxAge()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            CorsPolicies = new Dictionary<string, CorsPolicyOptions> { ["Default"] = new() { PreflightMaxAgeSeconds = -1 } },
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "CorsPolicies:Default:PreflightMaxAgeSeconds");
    }

    [Fact(DisplayName = "Privacy off needs no keys")]
    public void PrivacyOffNeedsNoKeys()
    {
        // Arrange
        var options = new RequestPipelineOptions { Privacy = new() { Enabled = false } };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Privacy on with a valid current key and previous keys is accepted")]
    public void PrivacyOnWithValidKeysIsValid()
    {
        // Arrange
        var options = new RequestPipelineOptions
        {
            Privacy = PrivacyTestData.Enabled(PrivacyTestData.NewKey(), PrivacyTestData.NewKey()),
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Privacy on without a current key is rejected")]
    public void PrivacyOnWithoutKeyIsRejected()
    {
        // Arrange
        var options = new RequestPipelineOptions { Privacy = new() { Enabled = true } };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Privacy:Keys:Current");
    }

    [Theory(DisplayName = "A previous key that is not 32 base64 bytes is rejected, naming the setting")]
    [InlineData("not-base64!")]
    [InlineData("c2hvcnQ=")]
    public void InvalidPreviousKeysAreRejected(string secret)
    {
        // Arrange
        var options = new RequestPipelineOptions { Privacy = PrivacyTestData.Enabled(previousKeys: secret) };

        // Act
        var result = _validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Privacy:Keys:Previous:0");
    }
}
