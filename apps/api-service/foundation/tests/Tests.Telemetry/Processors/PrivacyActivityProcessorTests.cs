using System.Diagnostics;
using Aurore.Foundation.Telemetry.Processors;
using Tests.Shared.Privacy;

namespace Tests.Telemetry.Processors;

[Trait("Feature", "Privacy")]
public sealed class PrivacyActivityProcessorTests : IDisposable
{
    private readonly ActivitySource _source = new("privacy-tests");
    private readonly ActivityListener _listener = new()
    {
        ShouldListenTo = _ => true,
        Sample = (ref _) => ActivitySamplingResult.AllData,
    };

    public PrivacyActivityProcessorTests()
    {
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    [Fact(DisplayName = "Personal tags are encrypted when the span ends and other tags are left alone")]
    public void PersonalTagsAreEncrypted()
    {
        // Arrange
        var privacy = PrivacyTestData.Protector();
        var processor = new PrivacyActivityProcessor(privacy);
        using var activity = StartActivity();
        activity.SetTag("client.address", "203.0.113.7");
        activity.SetTag("user_agent.original", "agent/1.0");
        activity.SetTag("server.address", "example.com");
        activity.SetTag("http.response.status_code", 200);

        // Act
        processor.OnEnd(activity);

        // Assert
        var clientAddress = (string)activity.GetTagItem("client.address")!;
        Assert.NotEqual("203.0.113.7", clientAddress);
        Assert.Equal("203.0.113.7", privacy.Unprotect(clientAddress));
        Assert.Equal("agent/1.0", privacy.Unprotect((string)activity.GetTagItem("user_agent.original")!));
        Assert.Equal("example.com", activity.GetTagItem("server.address"));
        Assert.Equal(200, activity.GetTagItem("http.response.status_code"));
    }

    [Fact(DisplayName = "With privacy off no tag is changed")]
    public void TagsAreUntouchedWhenPrivacyIsOff()
    {
        // Arrange
        var processor = new PrivacyActivityProcessor(PrivacyTestData.DisabledProtector());
        using var activity = StartActivity();
        activity.SetTag("client.address", "203.0.113.7");

        // Act
        processor.OnEnd(activity);

        // Assert
        Assert.Equal("203.0.113.7", activity.GetTagItem("client.address"));
    }

    private Activity StartActivity()
    {
        return _source.StartActivity("request")!;
    }
}
