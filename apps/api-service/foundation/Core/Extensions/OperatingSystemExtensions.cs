using System;

namespace Aurore.Foundation.Core.Extensions;

/// <summary>Runtime environment checks.</summary>
public static class OperatingSystemExtensions
{
    extension(OperatingSystem)
    {
        /// <summary>True when running on Linux inside the Windows Subsystem for Linux.</summary>
        public static bool IsWsl()
        {
            return
                OperatingSystem.IsLinux() &&
                Environment.GetEnvironmentVariable("WSL_DISTRO_NAME") is not null;
        }

        /// <summary>True on Linux outside WSL.</summary>
        public static bool IsNativeLinux()
        {
            return OperatingSystem.IsLinux() && IsWsl() is false;
        }
    }
}
