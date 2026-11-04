using System;

namespace Aurore.Foundation.Core.Extensions;

public static class OperatingSystemExtensions
{
    extension(OperatingSystem)
    {
        public static bool IsWsl()
        {
            return
                OperatingSystem.IsLinux() &&
                Environment.GetEnvironmentVariable("WSL_DISTRO_NAME") is not null;
        }

        public static bool IsNativeLinux()
        {
            return OperatingSystem.IsLinux() && IsWsl() is false;
        }
    }
}
