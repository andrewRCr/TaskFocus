using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace TaskFocusAPI.IntegrationTests
{
    internal sealed class LoopbackSqlServer : IDisposable
    {
        private const byte PreLogin = 0x12;
        private const byte Login = 0x10;
        private const int HeaderLength = 8;
        private static readonly byte[] PreLoginResponse =
        [
            0x04, 0x01, 0, 15, 0, 0, 1, 0, // TDS response header
            0x01, 0, 6, 0, 1, 0xff, 0x02   // ENCRYPT_NOT_SUP
        ];
        private static readonly byte[] LoginResponse =
        [
            0x04, 0x01, 0, 34, 0, 0, 1, 0,       // TDS response header
            0xad, 10, 0, 1, 0x74, 0, 0, 4, 0,   // LOGINACK, TDS 7.4, empty server name
            16, 0, 0, 0,                        // Server version
            0xfd, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 // DONE
        ];
        private static readonly byte[] FirewallResponse =
        [
            0x04, 0x01, 0, 38, 0, 0, 1, 0,       // TDS response header
            0xaa, 14, 0, 0xa7, 0x9e, 0, 0, 1, 14, // ERROR 40615, state 1, class 14
            0, 0, 0, 0, 0, 0, 0, 0,               // Empty message, server, procedure, and line number
            0xfd, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 // DONE with error
        ];
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

        public LoopbackSqlServer() => _listener.Start();

        public SqlConnection CreateConnection(bool requireEncryption = false)
            => CreateConnection((IPEndPoint)_listener.LocalEndpoint, requireEncryption);

        public static SqlConnection CreateConnection(IPEndPoint endpoint, bool requireEncryption = false)
            => new(new SqlConnectionStringBuilder
            {
                DataSource = $"tcp:{endpoint.Address},{endpoint.Port}",
                UserID = "loopback",
                Password = "loopback",
                Encrypt = requireEncryption ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,
                Pooling = false,
                ConnectTimeout = 5
            }.ConnectionString);

        public async Task InterruptLoginAsync(bool reset, CancellationToken cancellationToken, bool beforeLogin = false)
        {
            using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            var stream = client.GetStream();
            if (beforeLogin)
            {
                await ReadPacketAsync(stream, PreLogin, cancellationToken);
            }
            else
            {
                await ReceiveLoginAsync(stream, cancellationToken);
            }
            if (reset)
            {
                client.Client.Close(0);
            }
            else
            {
                client.Client.Shutdown(SocketShutdown.Send);
            }
        }

        public Task AcceptLoginAsync(CancellationToken cancellationToken)
            => RespondToLoginAsync(LoginResponse, cancellationToken);

        public Task RefuseLoginByFirewallAsync(CancellationToken cancellationToken)
            => RespondToLoginAsync(FirewallResponse, cancellationToken);

        private async Task RespondToLoginAsync(byte[] response, CancellationToken cancellationToken)
        {
            using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            var stream = client.GetStream();
            await ReceiveLoginAsync(stream, cancellationToken);
            await stream.WriteAsync(response, cancellationToken);
        }

        public async Task RejectPreLoginAsync(CancellationToken cancellationToken)
        {
            using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
            var stream = client.GetStream();
            await ReadPacketAsync(stream, PreLogin, cancellationToken);
            await stream.WriteAsync(PreLoginResponse, cancellationToken);
        }

        public void Dispose() => _listener.Stop();

        private static async Task ReceiveLoginAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            await ReadPacketAsync(stream, PreLogin, cancellationToken);
            await stream.WriteAsync(PreLoginResponse, cancellationToken);
            await ReadPacketAsync(stream, Login, cancellationToken);
        }

        private static async Task ReadPacketAsync(NetworkStream stream, byte messageType, CancellationToken cancellationToken)
        {
            var header = new byte[HeaderLength];
            await stream.ReadExactlyAsync(header, cancellationToken);
            Assert.Equal(messageType, header[0]);
            Assert.Equal(1, header[1] & 1); // End of message
            var payload = new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2)) - HeaderLength];
            await stream.ReadExactlyAsync(payload, cancellationToken);
        }
    }
}
