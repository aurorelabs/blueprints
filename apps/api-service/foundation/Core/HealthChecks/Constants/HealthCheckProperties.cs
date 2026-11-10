namespace Aurore.Foundation.Core.HealthChecks.Constants;

/// <summary>Names and defaults used by the health check endpoints.</summary>
public static class HealthCheckProperties
{
    /// <summary>Header names.</summary>
    public static class Headers
    {
        /// <summary>Header that marks a request as coming from a health check client.</summary>
        public const string Client = "AUR-HealthCheck-Client";
    }

    /// <summary>Endpoint paths.</summary>
    public static class Endpoints
    {
        /// <summary>The path every health endpoint lives under.</summary>
        public const string Base = "/health";
        /// <summary>Is the process up? Does not look at dependencies.</summary>
        public const string Liveness = "/health/live";
        /// <summary>Can the service take traffic? Checks the dependencies.</summary>
        public const string Readiness = "/health/ready";
        /// <summary>A detailed report of every check.</summary>
        public const string Status = "/health/status";
    }

    /// <summary>Default values.</summary>
    public static class Configuration
    {
        /// <summary>How long a single dependency check may take.</summary>
        public const int DefaultTimeoutSeconds = 5;
        /// <summary>How long health responses are cached.</summary>
        public const int DefaultCacheSeconds = 5;
        /// <summary>The output cache policy name used by the health endpoints.</summary>
        public const string OutputCacheKey = "health-checks-cache";
    }

    /// <summary>Tags that group health checks.</summary>
    public static class Tags
    {
        /// <summary>Checks included in the liveness endpoint.</summary>
        public const string Liveness = "liveness";
        /// <summary>Checks included in the readiness endpoint.</summary>
        public const string Readiness = "readiness";
        /// <summary>A failure makes the service unhealthy.</summary>
        public const string Critical = "critical";
        /// <summary>A failure only degrades the service.</summary>
        public const string NonCritical = "non-critical";
        /// <summary>The check is for a database.</summary>
        public const string TypeDatabase = "type-database";
        /// <summary>The check is for an upstream HTTP service.</summary>
        public const string TypeHttpService = "type-httpservice";
    }
}
