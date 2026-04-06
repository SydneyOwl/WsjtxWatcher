using Android.App;
using System.Buffers.Binary;
using System.Formats.Asn1;
using System.Globalization;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Google.Protobuf;
using WsjtxRelay.Proto.V1;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Infrastructure.Wsjt;

public sealed class RelayConnectionProbe : IRelayConnectionProbe
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(12);
    private readonly Application _application;

    public RelayConnectionProbe(Application application)
    {
        _application = application;
    }

    public async Task<RelayConnectionTestResult> TestWatchConnectionAsync(
        RelayConnectionProbeOptions options,
        CancellationToken cancellationToken = default)
    {
        var normalizedError = Normalize(options);
        if (normalizedError is not null)
        {
            return normalizedError;
        }

        var state = new ProbeState((options.TrustedFingerprint ?? string.Empty).Trim());
        using var webSocket = CreateWebSocket(state);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProbeTimeout);
        var probeToken = timeoutCts.Token;
        long outgoingSeq = 0;
        var instanceId = Guid.NewGuid().ToString("N");

        try
        {
            var serverUri = new Uri($"{options.ServerUrl.TrimEnd('/')}/v1/watch", UriKind.Absolute);
            await webSocket.ConnectAsync(serverUri, probeToken).ConfigureAwait(false);

            await SendEnvelopeAsync(webSocket, new Envelope
            {
                ClientHello = new ClientHello
                {
                    Role = "watch",
                    TenantId = options.TenantId,
                    InstanceId = instanceId,
                    ClientName = "wsjtxwatcher-test",
                    ClientVersion = "0.1.0"
                }
            }, NextSeq(ref outgoingSeq), probeToken).ConfigureAwait(false);

            var serverHelloEnvelope = await ReceiveEnvelopeAsync(webSocket, probeToken).ConfigureAwait(false);
            var serverHello = serverHelloEnvelope.ServerHello
                              ?? throw new InvalidOperationException(GetFormattedString(
                                  Resource.String.relay_expected_message,
                                  "server_hello",
                                  serverHelloEnvelope.BodyCase));

            var timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await SendEnvelopeAsync(webSocket, new Envelope
            {
                AuthRequest = new AuthRequest
                {
                    TimestampUnix = timestampUnix,
                    Proof = ByteString.CopyFrom(BuildProof(
                        options.SharedSecret,
                        serverHello.Nonce.ToByteArray(),
                        "watch",
                        options.TenantId,
                        string.Empty,
                        instanceId,
                        timestampUnix))
                }
            }, NextSeq(ref outgoingSeq), probeToken).ConfigureAwait(false);

            var authEnvelope = await ReceiveEnvelopeAsync(webSocket, probeToken).ConfigureAwait(false);
            var authResult = authEnvelope.AuthResult
                             ?? throw new InvalidOperationException(GetFormattedString(
                                 Resource.String.relay_expected_message,
                                 "auth_result",
                                 authEnvelope.BodyCase));
            if (!authResult.Ok)
            {
                var message = string.IsNullOrWhiteSpace(authResult.Message)
                    ? authResult.ErrorCode
                    : authResult.Message;
                return new RelayConnectionTestResult(
                    false,
                    GetFormattedString(Resource.String.relay_auth_failed, message),
                    state.ObservedFingerprint);
            }

            return new RelayConnectionTestResult(
                true,
                GetString(Resource.String.relay_probe_success),
                state.ObservedFingerprint);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RelayConnectionTestResult(
                false,
                GetString(Resource.String.relay_probe_timeout),
                state.ObservedFingerprint);
        }
        catch (Exception exception)
        {
            if (!string.IsNullOrWhiteSpace(state.ValidationError))
            {
                return new RelayConnectionTestResult(false, state.ValidationError, state.ObservedFingerprint);
            }

            return new RelayConnectionTestResult(false, exception.Message, state.ObservedFingerprint);
        }
    }

    private RelayConnectionTestResult? Normalize(RelayConnectionProbeOptions options)
    {
        var serverUrl = (options.ServerUrl ?? string.Empty).Trim().TrimEnd('/');
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverUri))
        {
            return new RelayConnectionTestResult(false, GetString(Resource.String.relay_probe_invalid_url));
        }

        if (!string.Equals(serverUri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
        {
            return new RelayConnectionTestResult(false, GetString(Resource.String.relay_probe_invalid_scheme));
        }

        if (string.IsNullOrWhiteSpace(options.SharedSecret))
        {
            return new RelayConnectionTestResult(false, GetString(Resource.String.relay_probe_missing_secret));
        }

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            return new RelayConnectionTestResult(false, GetString(Resource.String.relay_probe_missing_tenant));
        }

        return null;
    }

    private ClientWebSocket CreateWebSocket(ProbeState state)
    {
        var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.Zero;
        webSocket.Options.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
            ValidateServerCertificate(certificate, state);
        return webSocket;
    }

    private bool ValidateServerCertificate(X509Certificate? certificate, ProbeState state)
    {
        if (certificate is null)
        {
            state.ValidationError = GetString(Resource.String.relay_probe_no_certificate);
            return false;
        }

        var certificate2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        state.ObservedFingerprint = ComputeSpkiFingerprint(certificate2);
        if (string.IsNullOrWhiteSpace(state.TrustedFingerprint)
            || string.Equals(state.TrustedFingerprint, state.ObservedFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        state.ValidationError = GetString(Resource.String.relay_probe_cert_mismatch);
        return false;
    }

    private async Task SendEnvelopeAsync(
        ClientWebSocket webSocket,
        Envelope envelope,
        ulong seq,
        CancellationToken cancellationToken)
    {
        envelope.ProtoVersion = 1;
        if (envelope.Seq == 0)
        {
            envelope.Seq = seq;
        }

        var payload = envelope.ToByteArray();
        await webSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Envelope> ReceiveEnvelopeAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException(GetString(Resource.String.relay_ws_closed));
            }

            stream.Write(buffer, 0, result.Count);
            if (stream.Length > RelayProtocolLimits.MaxFrameBytes)
            {
                throw new InvalidOperationException(GetString(Resource.String.relay_frame_too_large));
            }
            if (result.EndOfMessage)
            {
                break;
            }
        }

        if (stream.Length == 0)
        {
            throw new InvalidOperationException(GetString(Resource.String.relay_empty_frame));
        }

        return Envelope.Parser.ParseFrom(stream.ToArray());
    }

    private string ComputeSpkiFingerprint(X509Certificate2 certificate)
    {
        var spki = ExportSubjectPublicKeyInfo(certificate);
        var hash = SHA256.HashData(spki);
        return Convert.ToHexString(hash);
    }

    private byte[] ExportSubjectPublicKeyInfo(X509Certificate2 certificate)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        writer.PushSequence();
        writer.WriteObjectIdentifier(certificate.PublicKey.Oid?.Value ?? throw new InvalidOperationException(GetString(Resource.String.relay_certificate_oid_missing)));
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

    private static byte[] BuildProof(
        string sharedSecret,
        byte[] nonce,
        string role,
        string tenantId,
        string sourceName,
        string instanceId,
        long timestampUnix)
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

    private string GetString(int resourceId)
    {
        return _application.GetString(resourceId) ?? string.Empty;
    }

    private string GetFormattedString(int resourceId, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString(resourceId), args);
    }

    private static ulong NextSeq(ref long outgoingSeq)
    {
        return unchecked((ulong)Interlocked.Increment(ref outgoingSeq));
    }

    private sealed class ProbeState(string trustedFingerprint)
    {
        public string TrustedFingerprint { get; } = trustedFingerprint;

        public string ObservedFingerprint { get; set; } = string.Empty;

        public string ValidationError { get; set; } = string.Empty;
    }
}
