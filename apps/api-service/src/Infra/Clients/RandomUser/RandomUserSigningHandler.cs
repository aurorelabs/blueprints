using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurore.Blueprints.ApiService.Infra.Clients.RandomUser;

// Example of custom upstream authentication (request signing). RandomUser ignores these headers; the key is a demo value.
internal sealed class RandomUserSigningHandler(TimeProvider timeProvider) : DelegatingHandler
{
    private static readonly byte[] _signingKey = Encoding.UTF8.GetBytes("demo-signing-key");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var timestamp = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var payload = $"{request.Method}\n{request.RequestUri?.PathAndQuery}\n{timestamp}";
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(_signingKey, Encoding.UTF8.GetBytes(payload)));

        request.Headers.TryAddWithoutValidation("AL-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("AL-Signature", signature);

        return base.SendAsync(request, cancellationToken);
    }
}
