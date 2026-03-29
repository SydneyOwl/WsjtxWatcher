using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Services;

public sealed class DecodedMessageFactory
{
    private readonly ICountryCatalog _countryCatalog;
    private readonly IGridCacheStore _gridCacheStore;

    public DecodedMessageFactory(ICountryCatalog countryCatalog, IGridCacheStore gridCacheStore)
    {
        _countryCatalog = countryCatalog;
        _gridCacheStore = gridCacheStore;
    }

    public async Task<DecodedRadioMessage> CreateAsync(
        WsjtDecodeEvent decodeEvent,
        AppSettings settings,
        double dialFrequencyHz,
        CancellationToken cancellationToken = default)
    {
        var messageText = (decodeEvent.Message ?? string.Empty).Trim();
        var participants = ParseParticipants(messageText);
        var transmitterGrid = await ResolveGridAsync(messageText, cancellationToken).ConfigureAwait(false);
        var fromCountry = string.IsNullOrWhiteSpace(participants.Transmitter)
            ? null
            : await _countryCatalog.FindCountryByCallsignAsync(participants.Transmitter, cancellationToken).ConfigureAwait(false);
        var toCountry = string.IsNullOrWhiteSpace(participants.Receiver)
            ? null
            : await _countryCatalog.FindCountryByCallsignAsync(participants.Receiver, cancellationToken).ConfigureAwait(false);

        return new DecodedRadioMessage
        {
            DecodeTimeUtc = FormatUtcTime(decodeEvent.TimeMilliseconds),
            Snr = decodeEvent.Snr,
            OffsetTimeSeconds = decodeEvent.OffsetTimeSeconds,
            OffsetFrequencyHz = decodeEvent.OffsetFrequencyHz,
            Mode = decodeEvent.Mode ?? string.Empty,
            Message = messageText,
            LowConfidence = decodeEvent.LowConfidence,
            OffAir = decodeEvent.OffAir,
            Receiver = participants.Receiver,
            Transmitter = participants.Transmitter,
            TransmitterGrid = transmitterGrid,
            DistanceText = await CalculateDistanceAsync(settings.MyGrid, transmitterGrid, fromCountry).ConfigureAwait(false),
            ToCountryEnglish = toCountry?.EnglishName ?? string.Empty,
            ToCountryChinese = toCountry?.ChineseName ?? string.Empty,
            FromCountryEnglish = fromCountry?.EnglishName ?? string.Empty,
            FromCountryChinese = fromCountry?.ChineseName ?? string.Empty,
            ToCountryId = toCountry?.Id ?? 0,
            FromCountryId = fromCountry?.Id ?? 0,
            ContainsMyCallsign = ContainsIgnoreCase(messageText, settings.MyCallsign),
            MatchesSelectedDxcc = fromCountry is not null && settings.PreferredDxccIds.Contains(fromCountry.Id),
            DialFrequencyHz = dialFrequencyHz
        };
    }

    private async Task<string> ResolveGridAsync(string messageText, CancellationToken cancellationToken)
    {
        var tokens = SplitTokens(messageText);
        if (tokens.Length < 3)
        {
            return string.Empty;
        }

        var candidateCallsign = tokens[^2];
        if (candidateCallsign.Contains("...", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var lastToken = tokens[^1];
        if (MaidenheadLocator.IsValid(lastToken))
        {
            await _gridCacheStore.SaveGridAsync(candidateCallsign, lastToken.ToUpperInvariant(), cancellationToken).ConfigureAwait(false);
            return lastToken.ToUpperInvariant();
        }

        var cachedGrid = await _gridCacheStore.GetGridAsync(candidateCallsign, cancellationToken).ConfigureAwait(false);
        return cachedGrid?.ToUpperInvariant() ?? string.Empty;
    }

    private static Task<string> CalculateDistanceAsync(string myGrid, string transmitterGrid, CountryInfo? fromCountry)
    {
        if (!MaidenheadLocator.IsValid(myGrid))
        {
            return Task.FromResult(string.Empty);
        }

        if (MaidenheadLocator.IsValid(transmitterGrid))
        {
            return Task.FromResult(MaidenheadLocator.FormatDistance(MaidenheadLocator.GetDistanceKilometers(myGrid, transmitterGrid)));
        }

        if (fromCountry is null)
        {
            return Task.FromResult(string.Empty);
        }

        var myPoint = MaidenheadLocator.ToPoint(myGrid);
        if (myPoint is null)
        {
            return Task.FromResult(string.Empty);
        }

        var countryPoint = new GeoPoint(fromCountry.Latitude, fromCountry.Longitude);
        return Task.FromResult(MaidenheadLocator.FormatDistance(MaidenheadLocator.GetDistanceKilometers(countryPoint, myPoint)));
    }

    private static (string Transmitter, string Receiver) ParseParticipants(string messageText)
    {
        var tokens = SplitTokens(messageText);
        if (tokens.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (messageText.StartsWith("CQ", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Length == 1)
            {
                return (string.Empty, string.Empty);
            }

            var transmitter = MaidenheadLocator.IsValid(tokens[^1]) && tokens.Length >= 2 ? tokens[^2] : tokens[^1];
            return (NormalizeCallsign(transmitter), string.Empty);
        }

        if (tokens.Length >= 3)
        {
            return (NormalizeCallsign(tokens[^2]), NormalizeCallsign(tokens[^3]));
        }

        if (tokens.Length == 2)
        {
            return (NormalizeCallsign(tokens[^1]), NormalizeCallsign(tokens[^2]));
        }

        return (string.Empty, string.Empty);
    }

    private static string NormalizeCallsign(string value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool ContainsIgnoreCase(string source, string target)
    {
        return !string.IsNullOrWhiteSpace(target) && source.Contains(target.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string[] SplitTokens(string messageText)
    {
        return messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string FormatUtcTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(milliseconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
