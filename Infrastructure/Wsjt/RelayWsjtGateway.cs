using System.Buffers.Binary;
using System.Formats.Asn1;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using Serilog;
using WsjtxRelay.Proto.V1;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class RelayWsjtGateway : IWsjtGateway
{
    private static readonly TimeSpan[] ReconnectSchedule =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(30)
    ];

    private readonly ISettingsStore _settingsStore;
    private readonly RelayRuntimeState _runtimeState;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private ClientWebSocket? _webSocket;
    private IWsjtEventSink? _eventSink;
    private AppSettings _settings = new();
    private string _desiredSourceName = string.Empty;
    private string _instanceId = Guid.NewGuid().ToString("N");
    private long _outgoingSeq;
    private DateTimeOffset _lastIncomingUtc = DateTimeOffset.UtcNow;
    private int _heartbeatIntervalSec = 10;
    private int _heartbeatTimeoutSec = 30;
    private string _observedFingerprint = string.Empty;

    public RelayWsjtGateway(ISettingsStore settingsStore, RelayRuntimeState runtimeState, IUiDispatcher uiDispatcher)
    {
        _settingsStore = settingsStore;
        _runtimeState = runtimeState;
        _uiDispatcher = uiDispatcher;
    }

    public bool IsRunning => _runTask is { IsCompleted: false };

    public async Task StartAsync(AppSettings settings, IWsjtEventSink eventSink, CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
            _settings = settings.Clone();
            _eventSink = eventSink;
            _desiredSourceName = _settings.RelayPreferredSourceName ?? string.Empty;
            _instanceId = Guid.NewGuid().ToString("N");
            Interlocked.Exchange(ref _outgoingSeq, 0L);
            _runCts = new CancellationTokenSource();

            await UpdateRuntimeAsync(state =>
            {
                state.Reset(relayMode: true);
                state.SetFingerprint(_settings.RelayTrustedFingerprint);
                state.SetConnectionState(connecting: true, connected: false, status: "Connecting to relay server...");
            }).ConfigureAwait(false);

            _runTask = Task.Run(() => RunAsync(_runCts.Token), _runCts.Token);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
            await UpdateRuntimeAsync(state => state.Reset()).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public Task SelectSourceAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        _desiredSourceName = sourceName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_desiredSourceName))
        {
            return Task.CompletedTask;
        }

        return SendEnvelopeIfConnectedAsync(new Envelope
        {
            SelectSourceRequest = new SelectSourceRequest { SourceName = _desiredSourceName }
        }, cancellationToken);
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        CloseWebSocket();
        return Task.CompletedTask;
    }

    private async Task StopCoreAsync()
    {
        var runCts = _runCts;
        var runTask = _runTask;
        _runCts = null;
        _runTask = null;

        if (runCts is null && runTask is null)
        {
            return;
        }

        try
        {
            runCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        CloseWebSocket();

        if (runTask is not null)
        {
            try
            {
                await runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Relay gateway background task stopped with an error.");
            }
        }

        runCts?.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await UpdateRuntimeAsync(state =>
                    state.SetConnectionState(connecting: true, connected: false, status: "Connecting to relay server...")).ConfigureAwait(false);
                await ConnectAndPumpAsync(cancellationToken).ConfigureAwait(false);
                attempt = 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Relay connection loop failed.");
                var delay = ReconnectSchedule[Math.Min(attempt, ReconnectSchedule.Length - 1)];
                attempt += 1;
                await UpdateRuntimeAsync(state =>
                {
                    state.SetConnectionState(connecting: false, connected: false, status: $"Reconnect in {delay.TotalSeconds:0}s");
                    state.LastNotice = exception.Message;
                }).ConfigureAwait(false);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ConnectAndPumpAsync(CancellationToken cancellationToken)
    {
        _observedFingerprint = string.Empty;
        var webSocket = CreateWebSocket();
        _webSocket = webSocket;
        _lastIncomingUtc = DateTimeOffset.UtcNow;

        var serverUri = new Uri($"{_settings.RelayServerUrl.TrimEnd('/')}/v1/watch", UriKind.Absolute);
        await webSocket.ConnectAsync(serverUri, cancellationToken).ConfigureAwait(false);
        await PersistTrustedFingerprintAsync(cancellationToken).ConfigureAwait(false);
        await UpdateRuntimeAsync(state => state.SetFingerprint(_settings.RelayTrustedFingerprint)).ConfigureAwait(false);

        await SendEnvelopeAsync(new Envelope
        {
            ClientHello = new ClientHello
            {
                Role = "watch",
                TenantId = _settings.RelayTenantId ?? string.Empty,
                InstanceId = _instanceId,
                ClientName = "wsjtxwatcher",
                ClientVersion = "0.1.0"
            }
        }, cancellationToken).ConfigureAwait(false);

        var serverHelloEnvelope = await ReceiveEnvelopeAsync(webSocket, cancellationToken).ConfigureAwait(false);
        var serverHello = serverHelloEnvelope.ServerHello
                          ?? throw new InvalidOperationException($"Expected server_hello, received {serverHelloEnvelope.BodyCase}");

        _heartbeatIntervalSec = serverHello.HeartbeatIntervalSec > 0 ? (int)serverHello.HeartbeatIntervalSec : 10;
        _heartbeatTimeoutSec = serverHello.HeartbeatTimeoutSec >= serverHello.HeartbeatIntervalSec && serverHello.HeartbeatTimeoutSec > 0
            ? (int)serverHello.HeartbeatTimeoutSec
            : 30;

        var timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await SendEnvelopeAsync(new Envelope
        {
            AuthRequest = new AuthRequest
            {
                TimestampUnix = timestampUnix,
                Proof = ByteString.CopyFrom(BuildProof(
                    _settings.RelaySharedSecret ?? string.Empty,
                    serverHello.Nonce.ToByteArray(),
                    "watch",
                    _settings.RelayTenantId ?? string.Empty,
                    string.Empty,
                    _instanceId,
                    timestampUnix))
            }
        }, cancellationToken).ConfigureAwait(false);

        var authEnvelope = await ReceiveEnvelopeAsync(webSocket, cancellationToken).ConfigureAwait(false);
        var authResult = authEnvelope.AuthResult
                         ?? throw new InvalidOperationException($"Expected auth_result, received {authEnvelope.BodyCase}");
        if (!authResult.Ok)
        {
            throw new InvalidOperationException($"Relay auth failed: {authResult.ErrorCode} {authResult.Message}".Trim());
        }

        await UpdateRuntimeAsync(state => state.SetConnectionState(connecting: false, connected: true, status: "Relay connected")).ConfigureAwait(false);

        var receiveTask = Task.Run(() => ReceiveLoopAsync(webSocket, cancellationToken), cancellationToken);
        if (!string.IsNullOrWhiteSpace(_desiredSourceName))
        {
            await SendEnvelopeIfConnectedAsync(new Envelope
            {
                SelectSourceRequest = new SelectSourceRequest { SourceName = _desiredSourceName }
            }, cancellationToken).ConfigureAwait(false);
        }

        while (!receiveTask.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(_heartbeatIntervalSec), cancellationToken).ConfigureAwait(false);
            if (DateTimeOffset.UtcNow - _lastIncomingUtc > TimeSpan.FromSeconds(_heartbeatTimeoutSec))
            {
                throw new TimeoutException("Relay heartbeat timed out.");
            }

            await SendEnvelopeIfConnectedAsync(new Envelope
            {
                Ping = new Ping { TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            }, cancellationToken).ConfigureAwait(false);
        }

        await receiveTask.ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var envelope = await ReceiveEnvelopeAsync(webSocket, cancellationToken).ConfigureAwait(false);
            _lastIncomingUtc = DateTimeOffset.UtcNow;
            await FeedWatchdogAsync().ConfigureAwait(false);
            await HandleEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        switch (envelope.BodyCase)
        {
            case Envelope.BodyOneofCase.Ping:
                await SendEnvelopeIfConnectedAsync(new Envelope
                {
                    Pong = new Pong { TimestampUnixMs = envelope.Ping.TimestampUnixMs }
                }, cancellationToken).ConfigureAwait(false);
                break;
            case Envelope.BodyOneofCase.Pong:
                break;
            case Envelope.BodyOneofCase.SourceCatalog:
                await HandleSourceCatalogAsync(envelope.SourceCatalog).ConfigureAwait(false);
                break;
            case Envelope.BodyOneofCase.SelectSourceResult:
                if (!envelope.SelectSourceResult.Ok)
                {
                    await UpdateRuntimeAsync(state => state.LastNotice = envelope.SelectSourceResult.Message).ConfigureAwait(false);
                }
                break;
            case Envelope.BodyOneofCase.SourceSnapshot:
                await HandleSourceSnapshotAsync(envelope.SourceSnapshot, cancellationToken).ConfigureAwait(false);
                break;
            case Envelope.BodyOneofCase.SessionActivity:
                if (_eventSink is not null)
                {
                    await _eventSink.OnSessionActivityAsync(MapSessionActivity(envelope.SessionActivity), cancellationToken).ConfigureAwait(false);
                }
                break;
            case Envelope.BodyOneofCase.Decode:
                if (_eventSink is not null)
                {
                    await _eventSink.OnDecodeAsync(MapDecode(envelope.Decode), cancellationToken).ConfigureAwait(false);
                }
                break;
            case Envelope.BodyOneofCase.Status:
                if (_eventSink is not null)
                {
                    await _eventSink.OnStatusAsync(MapStatus(envelope.Status), cancellationToken).ConfigureAwait(false);
                }
                break;
            case Envelope.BodyOneofCase.QsoLogged:
                if (_eventSink is not null)
                {
                    await _eventSink.OnQsoLoggedAsync(MapQsoLogged(envelope.QsoLogged), cancellationToken).ConfigureAwait(false);
                }
                break;
            case Envelope.BodyOneofCase.SourceState:
                await HandleSourceStateAsync(envelope.SourceState).ConfigureAwait(false);
                break;
            case Envelope.BodyOneofCase.ServerNotice:
                await HandleServerNoticeAsync(envelope.ServerNotice).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleSourceCatalogAsync(SourceCatalog catalog)
    {
        var sources = catalog.Sources.Select(source => new RelaySourceDescriptor
        {
            SourceName = source.SourceName,
            DisplayName = source.DisplayName,
            Online = source.Online,
            LastSeenUnixMs = source.LastSeenUnixMs
        }).ToArray();

        await UpdateRuntimeAsync(state =>
        {
            state.UpdateCatalog(sources, catalog.CurrentSourceName);
            state.SetConnectionState(connecting: false, connected: true, status: "Relay connected");
        }).ConfigureAwait(false);
    }

    private async Task HandleSourceSnapshotAsync(SourceSnapshot snapshot, CancellationToken cancellationToken)
    {
        await UpdateRuntimeAsync(state => state.ApplySnapshot(snapshot.SourceName, snapshot.SourceOnline)).ConfigureAwait(false);
        if (snapshot.LastStatus is not null && _eventSink is not null)
        {
            await _eventSink.OnStatusAsync(MapStatus(snapshot.LastStatus), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleSourceStateAsync(SourceStateEvent sourceState)
    {
        var online = sourceState.State != SourceStateEvent.Types.State.Offline;
        await UpdateRuntimeAsync(state => state.ApplySourceState(sourceState.SourceName, online, sourceState.Message)).ConfigureAwait(false);
    }

    private async Task HandleServerNoticeAsync(ServerNotice notice)
    {
        await UpdateRuntimeAsync(state => state.LastNotice = string.IsNullOrWhiteSpace(notice.Message)
            ? notice.Code
            : notice.Message).ConfigureAwait(false);
    }

    private ClientWebSocket CreateWebSocket()
    {
        var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.Zero;
        webSocket.Options.RemoteCertificateValidationCallback = ValidateServerCertificate;
        return webSocket;
    }

    private bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is null)
        {
            return false;
        }

        var certificate2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        _observedFingerprint = ComputeSpkiFingerprint(certificate2);
        var trustedFingerprint = (_settings.RelayTrustedFingerprint ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trustedFingerprint)
            || string.Equals(trustedFingerprint, _observedFingerprint, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PersistTrustedFingerprintAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_settings.RelayTrustedFingerprint) || string.IsNullOrWhiteSpace(_observedFingerprint))
        {
            return;
        }

        var latestSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        latestSettings.RelayTrustedFingerprint = _observedFingerprint;
        await _settingsStore.SaveAsync(latestSettings, cancellationToken).ConfigureAwait(false);
        _settings.RelayTrustedFingerprint = _observedFingerprint;
    }

    private async Task UpdateRuntimeAsync(Action<RelayRuntimeState> action)
    {
        await _uiDispatcher.InvokeAsync(() => action(_runtimeState)).ConfigureAwait(false);
    }

    private Task FeedWatchdogAsync()
    {
        return _eventSink?.FeedTimeoutDogAsync() ?? Task.CompletedTask;
    }

    private async Task SendEnvelopeIfConnectedAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        if (_webSocket is null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        await SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        var webSocket = _webSocket ?? throw new InvalidOperationException("Relay websocket is not initialized.");
        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("Relay websocket is not open.");
        }

        envelope.ProtoVersion = 1;
        if (envelope.Seq == 0)
        {
            envelope.Seq = unchecked((ulong)Interlocked.Increment(ref _outgoingSeq));
        }

        var payload = envelope.ToByteArray();
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<Envelope> ReceiveEnvelopeAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("Relay websocket closed by server.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        if (stream.Length == 0)
        {
            throw new InvalidOperationException("Received an empty relay frame.");
        }

        return Envelope.Parser.ParseFrom(stream.ToArray());
    }

    private void CloseWebSocket()
    {
        var webSocket = _webSocket;
        _webSocket = null;
        if (webSocket is null)
        {
            return;
        }

        try
        {
            webSocket.Abort();
        }
        catch
        {
        }

        webSocket.Dispose();
    }

    private static string ComputeSpkiFingerprint(X509Certificate2 certificate)
    {
        var spki = ExportSubjectPublicKeyInfo(certificate);
        var hash = SHA256.HashData(spki);
        return Convert.ToHexString(hash);
    }

    private static byte[] ExportSubjectPublicKeyInfo(X509Certificate2 certificate)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        writer.PushSequence();
        writer.WriteObjectIdentifier(certificate.PublicKey.Oid?.Value ?? throw new InvalidOperationException("Certificate public key OID is missing."));
        var parameters = certificate.PublicKey.EncodedParameters.RawData;
        if (parameters.Length > 0)
        {
            writer.WriteEncodedValue(parameters);
        }
        else
        {
            writer.WriteNull();
        }

        writer.PopSequence();
        writer.WriteBitString(certificate.PublicKey.EncodedKeyValue.RawData);
        writer.PopSequence();
        return writer.Encode();
    }

    private static byte[] BuildProof(string sharedSecret, byte[] nonce, string role, string tenantId, string sourceName, string instanceId, long timestampUnix)
    {
        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(sharedSecret));
        hmac.TransformBlock(nonce, 0, nonce.Length, null, 0);
        AppendString(hmac, role);
        AppendString(hmac, tenantId);
        AppendString(hmac, sourceName);
        AppendString(hmac, instanceId);
        var timestampBuffer = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(timestampBuffer, timestampUnix);
        hmac.TransformFinalBlock(timestampBuffer, 0, timestampBuffer.Length);
        return hmac.Hash ?? Array.Empty<byte>();
    }

    private static void AppendString(HashAlgorithm hashAlgorithm, string value)
    {
        var text = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, text.Length);
        hashAlgorithm.TransformBlock(length, 0, length.Length, null, 0);
        if (text.Length > 0)
        {
            hashAlgorithm.TransformBlock(text, 0, text.Length, null, 0);
        }
    }

    private static WsjtSessionEvent MapSessionActivity(SessionActivityEvent relayEvent)
    {
        return new WsjtSessionEvent
        {
            ClientId = relayEvent.ClientId ?? string.Empty,
            SessionEndPoint = null
        };
    }

    private static WsjtDecodeEvent MapDecode(DecodeEvent relayEvent)
    {
        return new WsjtDecodeEvent
        {
            ClientId = relayEvent.ClientId ?? string.Empty,
            SessionEndPoint = null,
            IsNew = relayEvent.IsNew,
            TimeMilliseconds = relayEvent.TimeMilliseconds,
            Snr = relayEvent.Snr,
            OffsetTimeSeconds = relayEvent.OffsetTimeSeconds,
            OffsetFrequencyHz = relayEvent.OffsetFrequencyHz,
            Mode = relayEvent.Mode ?? string.Empty,
            Message = relayEvent.Message ?? string.Empty,
            LowConfidence = relayEvent.LowConfidence,
            OffAir = relayEvent.OffAir,
            RemoteCallsign = relayEvent.RemoteCallsign ?? string.Empty,
            RemoteGrid = relayEvent.RemoteGrid ?? string.Empty,
            DetailText = relayEvent.DetailText ?? string.Empty,
            ReportedFrequencyHz = relayEvent.ReportedFrequencyHz
        };
    }

    private static WsjtStatusEvent MapStatus(StatusEvent relayEvent)
    {
        return new WsjtStatusEvent
        {
            ClientId = relayEvent.ClientId ?? string.Empty,
            SessionEndPoint = null,
            Mode = relayEvent.Mode ?? string.Empty,
            TxMode = relayEvent.TxMode ?? string.Empty,
            Transmitting = relayEvent.Transmitting,
            TransmitMessage = relayEvent.TransmitMessage ?? string.Empty,
            DialFrequencyHz = relayEvent.DialFrequencyHz
        };
    }

    private static WsjtQsoLoggedEvent MapQsoLogged(QsoLoggedEvent relayEvent)
    {
        return new WsjtQsoLoggedEvent
        {
            ClientId = relayEvent.ClientId ?? string.Empty,
            SessionEndPoint = null,
            DxCall = relayEvent.DxCall ?? string.Empty,
            Band = relayEvent.Band ?? string.Empty,
            FrequencyHz = relayEvent.FrequencyHz
        };
    }
}
