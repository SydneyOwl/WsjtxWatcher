using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Utils.UdpServer;

public class WsjtxMsgHandler : WsjtxUdpServerBaseAsyncMessageHandler
{
    public delegate void MessageReceivedHandler<T>(T message);

    private readonly Context _ctx;

    private readonly MainViewModel _model = MainViewModel.GetInstance();

    public WsjtxMsgHandler(Context ctx)
    {
        this._ctx = ctx;
    }

    // 声明一个事件
    public event MessageReceivedHandler<Decode> OnDecodeMessageReceived;

    public event MessageReceivedHandler<Status> OnStatusMessageReceived;

    private void WriteMessageAsJsonToConsole<T>(T message) where T : IWsjtxDirectionOut
    {
        Console.WriteLine(typeof(T) + "~~~~" + JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }));
    }

    public override async Task HandleClearMessageAsync(WsjtxUdpServer server, Clear message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleClearMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleClosedMessageAsync(WsjtxUdpServer server, Close message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleClosedMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleDecodeMessageAsync(WsjtxUdpServer server, Decode message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        OnDecodeMessageReceived?.Invoke(message);
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleDecodeMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleHeartbeatMessageAsync(WsjtxUdpServer server, Heartbeat message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleHeartbeatMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleLoggedAdifMessageAsync(WsjtxUdpServer server, LoggedAdif message,
        EndPoint endPoint, CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleLoggedAdifMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleQsoLoggedMessageAsync(WsjtxUdpServer server, QsoLogged message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleQsoLoggedMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleStatusMessageAsync(WsjtxUdpServer server, Status message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        OnStatusMessageReceived?.Invoke(message);
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        if (message.TXMode != "FT8") return;
        _model.CurrentFreq = message.DialFrequencyInHz;
        // 发射中, 如果支持TxMsg则必不为空
        if (message.Transmitting)
        {
            _model.IsTransmitting = true;
            // 低版本的jtdx没有tx message字段，无法显示相关信息！
            _model.TransmittingMessage = string.IsNullOrEmpty(message.TXMessage)
                ? _ctx.GetString(ResourceConstant.String.txing)
                : message.TXMessage;
        }
        else
        {
            _model.IsTransmitting = false;
        }

        await base.HandleStatusMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleWSPRDecodeMessageAsync(WsjtxUdpServer server, WSPRDecode message,
        EndPoint endPoint, CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        await base.HandleWSPRDecodeMessageAsync(server, message, endPoint, cancellationToken);
    }
}