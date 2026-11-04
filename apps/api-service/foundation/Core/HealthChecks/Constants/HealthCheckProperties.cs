namespace Aurore.Foundation.Core.HealthChecks.Constants;

public static class HealthCheckProperties
{
    public static class Headers
    {
        public const string Client = "AL-HealthCheck-Client";
    }

    public static class Endpoints
    {
        public const string Base = "/health";
        public const string Liveness = "/health/live";
        public const string Readiness = "/health/ready";
        public const string Status = "/health/status";
    }

    public static class Configuration
    {
        public const int DefaultTimeoutSeconds = 5;
        public const int DefaultCacheSeconds = 5;
        public const string OutputCacheKey = "health-checks-cache";
    }

    public static class Tags
    {
        public const string Liveness = "liveness";
        public const string Readiness = "readiness";
        public const string Critical = "critical";
        public const string NonCritical = "non-critical";
        public const string TypeDatabase = "type-database";
        public const string TypeHttpService = "type-httpservice";
    }
}
