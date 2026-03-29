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
        var participants = ResolveParticipants(decodeEvent, messageText);
        var transmitterGrid = await ResolveGridAsync(decodeEvent, messageText, participants.Transmitter, cancellationToken).ConfigureAwait(false);
        var fromCountry = string.IsNullOrWhiteSpace(participants.Transmitter)
            ? null
            : await _countryCatalog.FindCountryByCallsignAsync(participants.Transmitter, cancellationToken).ConfigureAwait(false);
        var toCountry = string.IsNullOrWhiteSpace(participants.Receiver)
            ? null
            : await _countryCatalog.FindCountryByCallsignAsync(participants.Receiver, cancellationToken).ConfigureAwait(false);
        var effectiveFrequencyHz = decodeEvent.ReportedFrequencyHz > 0d ? decodeEvent.ReportedFrequencyHz : dialFrequencyHz;

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
            DistanceText = CalculateDistance(settings.MyGrid, transmitterGrid, fromCountry),
            ToCountryEnglish = toCountry?.EnglishName ?? string.Empty,
            ToCountryChinese = toCountry?.ChineseName ?? string.Empty,
            FromCountryEnglish = fromCountry?.EnglishName ?? string.Empty,
            FromCountryChinese = fromCountry?.ChineseName ?? string.Empty,
            ToCountryId = toCountry?.Id ?? 0,
            FromCountryId = fromCountry?.Id ?? 0,
            ContainsMyCallsign = ContainsIgnoreCase(messageText, settings.MyCallsign),
            MatchesSelectedDxcc = fromCountry is not null && settings.PreferredDxccIds.Contains(fromCountry.Id),
            DialFrequencyHz = effectiveFrequencyHz
        };
    }

    private static (string Transmitter, string Receiver) ResolveParticipants(WsjtDecodeEvent decodeEvent, string messageText)
    {
        if (!string.IsNullOrWhiteSpace(decodeEvent.RemoteCallsign))
        {
            return (WsjtxMessageParser.NormalizeCallsign(decodeEvent.RemoteCallsign), string.Empty);
        }

        return WsjtxMessageParser.ParseParticipants(messageText);
    }

    private async Task<string> ResolveGridAsync(
        WsjtDecodeEvent decodeEvent,
        string messageText,
        string transmitter,
        CancellationToken cancellationToken)
    {
        if (MaidenheadLocator.IsValid(decodeEvent.RemoteGrid))
        {
            return decodeEvent.RemoteGrid.Trim().ToUpperInvariant();
        }

        var gridUpdate = WsjtxMessageParser.ExtractGridUpdate(messageText);
        if (gridUpdate is not null)
        {
            return gridUpdate.Value.GridSquare;
        }

        if (string.IsNullOrWhiteSpace(transmitter))
        {
            return string.Empty;
        }

        var cachedGrid = await _gridCacheStore.GetGridAsync(transmitter, cancellationToken).ConfigureAwait(false);
        return cachedGrid?.ToUpperInvariant() ?? string.Empty;
    }

    private static string CalculateDistance(string myGrid, string transmitterGrid, CountryInfo? fromCountry)
    {
        if (!MaidenheadLocator.IsValid(myGrid))
        {
            return string.Empty;
        }

        if (MaidenheadLocator.IsValid(transmitterGrid))
        {
            return MaidenheadLocator.FormatDistance(MaidenheadLocator.GetDistanceKilometers(myGrid, transmitterGrid));
        }

        if (fromCountry is null)
        {
            return string.Empty;
        }

        var myPoint = MaidenheadLocator.ToPoint(myGrid);
        if (myPoint is null)
        {
            return string.Empty;
        }

        var countryPoint = new GeoPoint(fromCountry.Latitude, fromCountry.Longitude);
        return MaidenheadLocator.FormatDistance(MaidenheadLocator.GetDistanceKilometers(countryPoint, myPoint));
    }

    private static bool ContainsIgnoreCase(string source, string target)
    {
        return !string.IsNullOrWhiteSpace(target) && source.Contains(target.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatUtcTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(milliseconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
