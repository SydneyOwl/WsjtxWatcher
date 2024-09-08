using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Utils.UdpServer;

public class WsjtxMsgHandler : WsjtxUdpServerBaseAsyncMessageHandler
{
    private readonly Context ctx;

    private readonly MainViewModel model = MainViewModel.GetInstance();
    
    public delegate void MessageReceivedHandler<T>(T message);

    // 声明一个事件
    public event MessageReceivedHandler<Decode> OnDecodeMessageReceived;
    
    public event MessageReceivedHandler<Status> OnStatusMessageReceived;
    
    public WsjtxMsgHandler(Context ctx)
    {
        this.ctx = ctx;
    }

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
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleClearMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleClosedMessageAsync(WsjtxUdpServer server, Close message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleClosedMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleDecodeMessageAsync(WsjtxUdpServer server, Decode message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        OnDecodeMessageReceived?.Invoke(message);
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleDecodeMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleHeartbeatMessageAsync(WsjtxUdpServer server, Heartbeat message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleHeartbeatMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleLoggedAdifMessageAsync(WsjtxUdpServer server, LoggedAdif message,
        EndPoint endPoint, CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleLoggedAdifMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleQsoLoggedMessageAsync(WsjtxUdpServer server, QsoLogged message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleQsoLoggedMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleStatusMessageAsync(WsjtxUdpServer server, Status message, EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        OnStatusMessageReceived?.Invoke(message);
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        if (message.TXMode != "FT8") return;
        model.currentFreq = message.DialFrequencyInHz;
        // 发射中, 如果支持TxMsg则必不为空
        if (message.Transmitting)
        {
            model.IsTransmitting = true;
            // 低版本的jtdx没有tx message字段，无法显示相关信息！
            model.TransmittingMessage = string.IsNullOrEmpty(message.TXMessage)
                ? ctx.GetString(ResourceConstant.String.txing)
                : message.TXMessage;
        }
        else
        {
            model.IsTransmitting = false;
        }

        await base.HandleStatusMessageAsync(server, message, endPoint, cancellationToken);
    }

    public override async Task HandleWSPRDecodeMessageAsync(WsjtxUdpServer server, WSPRDecode message,
        EndPoint endPoint, CancellationToken cancellationToken = default)
    {
        WriteMessageAsJsonToConsole(message);
        model.RecvWatchdog.Feed();
        model.clientId = message.Id;
        model.sessionEndPoint = endPoint;
        await base.HandleWSPRDecodeMessageAsync(server, message, endPoint, cancellationToken);
    }
}