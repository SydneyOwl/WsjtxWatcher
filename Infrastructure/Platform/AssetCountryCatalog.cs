using Android.App;
using Android.Content;
using Android.Content.Res;
using Serilog;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AssetCountryCatalog : ICountryCatalog
{
    private readonly Application _application;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly Dictionary<string, CountryInfo> _exactCallsignMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CountryInfo> _prefixMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CountryInfo> _countriesByEnglishName = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<CountryInfo> _countries = Array.Empty<CountryInfo>();
    private bool _isLoaded;

    public AssetCountryCatalog(Application application)
    {
        _application = application;
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded)
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isLoaded)
            {
                return;
            }

            using var stream = _application.Assets!.Open("cty.dat");
            using var reader = new StreamReader(stream);
            var contents = await reader.ReadToEndAsync().ConfigureAwait(false);
            var chineseContext = CreateLocalizedContext("zh-CN");

            var countries = new List<CountryInfo>();
            var blocks = contents.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var nextId = 1;

            foreach (var block in blocks)
            {
                if (!block.Contains(':', StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = block.Split(':');
                if (parts.Length < 9)
                {
                    continue;
                }

                var englishName = parts[0].Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(englishName))
                {
                    continue;
                }

                var country = new CountryInfo
                {
                    Id = nextId++,
                    EnglishName = englishName,
                    ChineseName = ResolveLocalizedDxccName(chineseContext, englishName),
                    CqZone = ParseInt(parts[1]),
                    ItuZone = ParseInt(parts[2]),
                    Continent = ParseString(parts[3]),
                    Latitude = ParseDouble(parts[4]),
                    Longitude = ParseDouble(parts[5]) * -1d,
                    GmtOffset = ParseDouble(parts[6]),
                    DxccPrefix = ParseString(parts[7])
                };

                countries.Add(country);
                _countriesByEnglishName[country.EnglishName] = country;
                RegisterPrefixes(country, parts[8]);
            }

            _countries = countries.OrderBy(country => country.EnglishName).ToList();
            _isLoaded = true;
            Log.Information("Country catalog loaded. Countries={Count}", _countries.Count);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<IReadOnlyList<CountryInfo>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _countries;
    }

    public async Task<IReadOnlyList<CountryInfo>> SearchCountriesAsync(string query, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(query))
        {
            return _countries;
        }

        var normalized = query.Trim();
        return _countries
            .Where(country =>
                country.EnglishName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                country.ChineseName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                country.DxccPrefix.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<CountryInfo?> FindCountryByCallsignAsync(string callsign, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return null;
        }

        var normalized = callsign.Trim().ToUpperInvariant();
        if (_exactCallsignMap.TryGetValue(normalized, out var exactCountry))
        {
            return exactCountry;
        }

        for (var length = normalized.Length; length > 0; length--)
        {
            var prefix = normalized[..length];
            if (_prefixMap.TryGetValue(prefix, out var prefixCountry))
            {
                return prefixCountry;
            }
        }

        return null;
    }

    public async Task<CountryInfo?> FindCountryByEnglishNameAsync(string englishName, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _countriesByEnglishName.TryGetValue(englishName, out var country) ? country : null;
    }

    private Context CreateLocalizedContext(string languageTag)
    {
        var locale = Java.Util.Locale.ForLanguageTag(languageTag) ?? new Java.Util.Locale("zh", "CN");
        var existingConfiguration = _application.Resources?.Configuration;
        var configuration = existingConfiguration is null ? new Configuration() : new Configuration(existingConfiguration);
        configuration.SetLocale(locale);
        configuration.SetLayoutDirection(locale);
        return _application.CreateConfigurationContext(configuration) ?? _application;
    }

    private string ResolveLocalizedDxccName(Context context, string englishName)
    {
        var resourceKey = DxccTranslationKeyNormalizer.Normalize(englishName);
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return string.Empty;
        }

        var resourceId = _application.Resources?.GetIdentifier(resourceKey, "string", _application.PackageName) ?? 0;
        return resourceId > 0 ? context.GetString(resourceId) ?? string.Empty : string.Empty;
    }

    private void RegisterPrefixes(CountryInfo country, string rawPrefixes)
    {
        foreach (var rawPrefix in rawPrefixes.Replace("\r", string.Empty).Replace("\n", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedPrefix = NormalizePrefix(rawPrefix);
            if (string.IsNullOrWhiteSpace(normalizedPrefix))
            {
                continue;
            }

            if (normalizedPrefix.StartsWith('='))
            {
                _exactCallsignMap[normalizedPrefix[1..]] = country;
            }
            else
            {
                _prefixMap[normalizedPrefix] = country;
            }
        }
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = prefix.Trim();
        var parenIndex = normalized.IndexOf('(');
        if (parenIndex >= 0)
        {
            normalized = normalized[..parenIndex];
        }

        var bracketIndex = normalized.IndexOf('[');
        if (bracketIndex >= 0)
        {
            normalized = normalized[..bracketIndex];
        }

        return normalized.Trim().ToUpperInvariant();
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(ParseString(value), out var result) ? result : 0;
    }

    private static double ParseDouble(string value)
    {
        return double.TryParse(ParseString(value), out var result) ? result : 0d;
    }

    private static string ParseString(string value)
    {
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
    }
}
