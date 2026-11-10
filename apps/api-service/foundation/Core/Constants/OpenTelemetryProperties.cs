namespace Aurore.Foundation.Core.Constants;

/// <summary>
/// Names of the telemetry sources and attributes used across the foundation, so spans, logs and metrics agree on
/// spelling.
/// </summary>
public static class OpenTelemetryProperties
{
    /// <summary>Names of activity sources.</summary>
    public static class Sources
    {
        /// <summary>The activity source for infrastructure work such as health checks and HTTP dependencies.</summary>
        public const string Infrastructure = "Aurore.Platform.Infrastructure";
    }

    /// <summary>Attribute names.</summary>
    public static class Attributes
    {
        /// <summary>Attribute names defined by the OpenTelemetry semantic conventions.</summary>
        public static class Standard
        {
            /// <summary>Attribute <c>deployment.environment.name</c>.</summary>
            public const string EnvironmentName = "deployment.environment.name";
            /// <summary>Attribute <c>service.instance.id</c>.</summary>
            public const string ServiceInstanceId = "service.instance.id";
            /// <summary>Attribute <c>service.name</c>.</summary>
            public const string ServiceName = "service.name";
            /// <summary>Attribute <c>service.version</c>.</summary>
            public const string ServiceVersion = "service.version";
            /// <summary>Attribute <c>http.request.method</c>.</summary>
            public const string HttpRequestMethod = "http.request.method";
            /// <summary>Attribute <c>server.address</c>.</summary>
            public const string ServerAddress = "server.address";
            /// <summary>Attribute <c>url.path</c>.</summary>
            public const string UrlPath = "url.path";
            /// <summary>Attribute <c>url.query</c>.</summary>
            public const string UrlQuery = "url.query";
            /// <summary>Attribute <c>client.address</c>.</summary>
            public const string ClientAddress = "client.address";
            /// <summary>Attribute <c>network.peer.address</c>.</summary>
            public const string NetworkPeerAddress = "network.peer.address";
            /// <summary>Attribute <c>enduser.id</c>.</summary>
            public const string EndUserId = "enduser.id";
            /// <summary>Attribute <c>user_agent.original</c>.</summary>
            public const string UserAgentOriginal = "user_agent.original";
        }

        /// <summary>Attribute names specific to this platform, prefixed with "aur".</summary>
        public static class Custom
        {
            private const string Prefix = "aur";

            /// <summary>Attribute <c>aur.service.domain</c>.</summary>
            public const string ServiceDomain = $"{Prefix}.service.domain";
            /// <summary>Attribute <c>aur.service.boundary</c>.</summary>
            public const string ServiceBoundary = $"{Prefix}.service.boundary";
            /// <summary>Attribute <c>aur.service.component</c>.</summary>
            public const string ServiceComponent = $"{Prefix}.service.component";
            /// <summary>Attribute <c>aur.service.maintainer</c>.</summary>
            public const string ServiceMaintainer = $"{Prefix}.service.maintainer";
            /// <summary>Attribute <c>aur.correlation.id</c>.</summary>
            public const string CorrelationId = $"{Prefix}.correlation.id";
            /// <summary>Attribute <c>aur.messaging.handler</c>.</summary>
            public const string MessageHandler = $"{Prefix}.messaging.handler";
            /// <summary>Attribute <c>aur.trace.id</c>.</summary>
            public const string TraceId = $"{Prefix}.trace.id";
            /// <summary>Attribute <c>aur.span.id</c>.</summary>
            public const string SpanId = $"{Prefix}.span.id";
            /// <summary>Attribute <c>aur.feature.name</c>: the feature (use case) that ran.</summary>
            public const string FeatureName = $"{Prefix}.feature.name";
            /// <summary>Attribute <c>aur.error.code</c>: the code of the error a feature answered with.</summary>
            public const string ErrorCode = $"{Prefix}.error.code";
        }
    }
}
