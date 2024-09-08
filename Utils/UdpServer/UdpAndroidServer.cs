using System.Net;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using WsjtxUtils.WsjtxMessages;
using WsjtxUtils.WsjtxMessages.Messages;

namespace WsjtxWatcher.Utils.UdpServer;

public class UdpAndroidServer
{
    private static UdpAndroidServer instance;

    private UdpClient client; // .....

    private CancellationTokenSource _tokenSource;

    private bool isServerRunning;

    public static UdpAndroidServer getInstance()
    {
        if (instance == null)
        {
            instance = new UdpAndroidServer();
        }

        return instance;
    }

    public Task StartAndroidUdpServerAsync(string ip, string port)
    {
        try
        {
            _tokenSource?.Cancel();
            client?.Close();
            client?.Dispose();
        }
        catch
        {
            // ignored...
        }

        _tokenSource = new CancellationTokenSource();
        if (!IPAddress.TryParse(ip, out var _ip)) throw new Exception($"Failed to parse ip {ip}!");
        if (!int.TryParse(port, out var _port)) throw new Exception($"Failed to parse port {port}!");
        var localEndPoint = new IPEndPoint(_ip, _port);
        return Task.Run(async () =>
        {
            using (client = new UdpClient())
            {
                isServerRunning = true;
                client.Client.Bind(localEndPoint);
                while (!_tokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult result = await client.ReceiveAsync().ConfigureAwait(false);
                        Memory<byte> source = new(result.Buffer);
                        WsjtxMessage? msg = source.DeserializeWsjtxMessage();
                        MessageType? messageType = msg?.MessageType;
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
                        isServerRunning = false;
                        // Handle if the UdpClient is disposed
                        break;
                    }
                }
                isServerRunning = false;
            }
        });
    }

    public Task StopAndroidUdpServerAsync()
    {
        return Task.Run(() =>
        {
            _tokenSource.Cancel();
            client.Close();
            client.Dispose();
            isServerRunning = false;
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