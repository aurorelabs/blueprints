namespace Aurore.Foundation.Telemetry.Constants;

public static class OpenTelemetryProperties
{
    public static class Sources
    {
        public const string Infrastructure = "Aurore.Platform.Infrastructure";
    }

    public static class Attributes
    {
        public static class Standard
        {
            public const string EnvironmentName = "deployment.environment.name";
            public const string ServiceInstanceId = "service.instance.id";
            public const string ServiceName = "service.name";
            public const string ServiceVersion = "service.version";
            public const string HttpRequestMethod = "http.request.method";
            public const string ServerAddress = "server.address";
            public const string UrlPath = "url.path";
            public const string UrlQuery = "url.query";
            public const string ClientAddress = "client.address";
            public const string UserAgentOriginal = "user_agent.original";
        }

        public static class Custom
        {
            private const string Prefix = "aur";

            public const string ServiceDomain = $"{Prefix}.service.domain";
            public const string ServiceBoundary = $"{Prefix}.service.boundary";
            public const string ServiceComponent = $"{Prefix}.service.component";
            public const string ServiceMaintainer = $"{Prefix}.service.maintainer";
            public const string CorrelationId = $"{Prefix}.correlation.id";
            public const string MessageHandler = $"{Prefix}.messaging.handler";
            public const string TraceId = $"{Prefix}.trace.id";
            public const string SpanId = $"{Prefix}.span.id";
        }
    }
}
