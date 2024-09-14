using System.Net;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using WsjtxUtils.WsjtxMessages;
using WsjtxUtils.WsjtxMessages.Messages;

namespace WsjtxWatcher.Utils.UdpServer;

public class UdpAndroidServer
{
    private static UdpAndroidServer _instance;

    private CancellationTokenSource _tokenSource;

    private UdpClient _client; // .....

    private bool _isServerRunning;

    public static UdpAndroidServer GetInstance()
    {
        if (_instance == null) _instance = new UdpAndroidServer();

        return _instance;
    }

    public Task StartAndroidUdpServerAsync(string _ip, string _port)
    {
        try
        {
            _tokenSource?.Cancel();
            _client?.Close();
            _client?.Dispose();
        }
        catch
        {
            // ignored...
        }

        _tokenSource = new CancellationTokenSource();
        if (!IPAddress.TryParse(_ip, out var ip)) throw new Exception($"Failed to parse ip {ip}!");
        if (!int.TryParse(_port, out var port)) throw new Exception($"Failed to parse port {port}!");
        var localEndPoint = new IPEndPoint(ip, port);
        return Task.Run(async () =>
        {
            using (_client = new UdpClient())
            {
                _isServerRunning = true;
                _client.Client.Bind(localEndPoint);
                while (!_tokenSource.Token.IsCancellationRequested)
                    try
                    {
                        var result = await _client.ReceiveAsync().ConfigureAwait(false);
                        Memory<byte> source = new(result.Buffer);
                        var msg = source.DeserializeWsjtxMessage();
                        var messageType = msg?.MessageType;
                        if (messageType.HasValue)
                        {
                            WriteMessageAsJsonToConsole((IWsjtxDirectionOut)msg);
                            switch (messageType.GetValueOrDefault())
                            {
                                case MessageType.Heartbeat:
                                    continue;
                                case MessageType.Status:
                                    continue;
                                case MessageType.Decode:
                                    continue;
                                case MessageType.Clear:
                                    continue;
                                case MessageType.QSOLogged:
                                    continue;
                                case MessageType.Close:
                                    continue;
                                case MessageType.WSPRDecode:
                                    continue;
                                case MessageType.LoggedADIF:
                                    continue;
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        _isServerRunning = false;
                        // Handle if the UdpClient is disposed
                        break;
                    }

                _isServerRunning = false;
            }
        });
    }

    public Task StopAndroidUdpServerAsync()
    {
        return Task.Run(() =>
        {
            _tokenSource.Cancel();
            _client.Close();
            _client.Dispose();
            _isServerRunning = false;
        });
    }

    private void WriteMessageAsJsonToConsole<T>(T message) where T : IWsjtxDirectionOut
    {
        Console.WriteLine(typeof(T) + "~~~~" + JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }));
    }
}