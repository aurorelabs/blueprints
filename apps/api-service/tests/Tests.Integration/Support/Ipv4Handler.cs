using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Tests.Integration.Support;

/// <summary>
/// An HTTP handler that connects over IPv4 only. "localhost" names resolve to ::1 first, and the ports docker publishes from
/// WSL are not forwarded on IPv6: the connection hangs until the system gives up instead of failing, which would make every
/// call to Keycloak wait about twenty seconds.
/// </summary>
public static class Ipv4Handler
{
    /// <summary>Creates the handler.</summary>
    public static SocketsHttpHandler Create()
    {
        return new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken);
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);

                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();

                    throw;
                }
            },
        };
    }
}
