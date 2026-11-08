using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Tests;

internal sealed class TestEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "Tests";

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
