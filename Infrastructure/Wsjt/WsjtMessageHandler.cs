using System.Net;
using Serilog;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class WsjtMessageHandler : WsjtxUdpServerBaseAsyncMessageHandler
{
    private readonly IWsjtEventSink _eventSink;

    public WsjtMessageHandler(IWsjtEventSink eventSink)
    {
        _eventSink = eventSink;
    }

    public override async Task HandleDecodeMessageAsync(
        WsjtxUdpServer server,
        Decode message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await _eventSink.OnDecodeAsync(new WsjtDecodeEvent
        {
            ClientId = message.Id,
            SessionEndPoint = endPoint,
            IsNew = message.New,
            TimeMilliseconds = message.Time,
            Snr = message.Snr,
            OffsetTimeSeconds = message.OffsetTimeSeconds,
            OffsetFrequencyHz = message.OffsetFrequencyHz,
            Mode = message.Mode,
            Message = message.Message,
            LowConfidence = message.LowConfidence,
            OffAir = message.OffAir
        }, cancellationToken).ConfigureAwait(false);

        await base.HandleDecodeMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleStatusMessageAsync(
        WsjtxUdpServer server,
        Status message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await _eventSink.OnStatusAsync(new WsjtStatusEvent
        {
            ClientId = message.Id,
            SessionEndPoint = endPoint,
            TxMode = message.TXMode,
            Transmitting = message.Transmitting,
            TransmitMessage = message.TXMessage ?? string.Empty,
            DialFrequencyHz = message.DialFrequencyInHz
        }, cancellationToken).ConfigureAwait(false);

        await base.HandleStatusMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleHeartbeatMessageAsync(
        WsjtxUdpServer server,
        Heartbeat message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleHeartbeatMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleClearMessageAsync(
        WsjtxUdpServer server,
        Clear message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleClearMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleClosedMessageAsync(
        WsjtxUdpServer server,
        Close message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleClosedMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleLoggedAdifMessageAsync(
        WsjtxUdpServer server,
        LoggedAdif message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleLoggedAdifMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleQsoLoggedMessageAsync(
        WsjtxUdpServer server,
        QsoLogged message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleQsoLoggedMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleWSPRDecodeMessageAsync(
        WsjtxUdpServer server,
        WSPRDecode message,
        EndPoint endPoint,
        CancellationToken cancellationToken = default)
    {
        await _eventSink.FeedTimeoutDogAsync();
        await PublishSessionActivityAsync(message.Id, endPoint, cancellationToken).ConfigureAwait(false);
        await base.HandleWSPRDecodeMessageAsync(server, message, endPoint, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishSessionActivityAsync(string clientId, EndPoint endPoint, CancellationToken cancellationToken)
    {
        try
        {
            await _eventSink.OnSessionActivityAsync(new WsjtSessionEvent
            {
                ClientId = clientId,
                SessionEndPoint = endPoint
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to publish session activity.");
        }
    }
}
