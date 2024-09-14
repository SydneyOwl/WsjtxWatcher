using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.DeviceActions;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Utils.UdpServer;

public class WsjtxMsgHandler : WsjtxUdpServerBaseAsyncMessageHandler
{
    public delegate void MessageReceivedHandler<T>(T message);

    private readonly MainViewModel _model = MainViewModel.GetInstance();

    // 声明一个事件
    public event MessageReceivedHandler<DecodedMsg> OnDecodeMessageReceived;

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
        WriteMessageAsJsonToConsole(message);
        var msg = DecodedMsg.RawDecodedToDecodedMsg(message);
        OnDecodeMessageReceived?.Invoke(msg);
        _model.DecodedMsgList.Add(msg);
        // _model.adapter.NotifyDataSetChanged();
        // do notifications
        if (SettingsVariables.VibrateOnAll) Vibrate.DoVibrate();
        if (SettingsVariables.SendNotificationOnAll)
            Notifications.GetInstance()
                .PopCommonNotification(
                    Application.Context.GetString(ResourceConstant.String.received_msg) + message.Message);
        
        if (!string.IsNullOrEmpty(SettingsVariables.MyCallsign) &&
            message.Message.Contains(SettingsVariables.MyCallsign))
        {
            if (SettingsVariables.VibrateOnCall) Vibrate.DoVibrate();
            if (SettingsVariables.SendNotificationOnCall)
                Notifications.GetInstance()
                    .PopCommonNotification(Application.Context.GetString(ResourceConstant.String.included_in_msg) +
                                           message.Message);
        }
        var wantedDxcc =
            Application.Context.GetSharedPreferences(Application.Context.GetString(ResourceConstant.String.storage_key),
                FileCreationMode.Private).GetStringSet("prefered_dxcc", new List<string>()).ToList();
        if (wantedDxcc.Contains(msg.FromLocationCountryId.ToString()))
        {
            if (SettingsVariables.VibrateOnDxcc) Vibrate.DoVibrate();
            if (SettingsVariables.SendNotificationOnDxcc)
                Notifications.GetInstance()
                    .PopCommonNotification(Application.Context.GetString(ResourceConstant.String.selected_dxcc) +
                                           message.Message);
        }
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
        WriteMessageAsJsonToConsole(message);
        _model.RecvWatchdog.Feed();
        _model.ClientId = message.Id;
        _model.SessionEndPoint = endPoint;
        if (message.TXMode != "FT8") return;
        if (!_model.LastTxStatus && message.Transmitting)
            _model.DecodedMsgList.Add(new DecodedMsg
            {
                Transmitter = "USER_TRANSMIT",
                Message = message.TXMessage
            });
        // _model.adapter.NotifyDataSetChanged();
        _model.LastTxStatus = message.Transmitting;
        _model.CurrentFreq = message.DialFrequencyInHz;
        // 发射中, 如果支持TxMsg则必不为空
        if (message.Transmitting)
        {
            _model.IsTransmitting = true;
            // 低版本的jtdx没有tx message字段，无法显示相关信息！
            _model.TransmittingMessage = string.IsNullOrEmpty(message.TXMessage)
                ? Application.Context.GetString(ResourceConstant.String.txing)
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