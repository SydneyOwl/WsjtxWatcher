using Android.App;
using Android.Content;
using Android.Content.Res;
using System.Collections.Concurrent;
using SQLite;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;
using WsjtxWatcher.Infrastructure.Persistence;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class SqliteCountryCatalog : ICountryCatalog
{
    private const int MaxCacheEntries = 2048;
    private readonly Application _application;
    private readonly ConcurrentDictionary<string, CountryInfo> _callsignCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _missingCallsignCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CountryInfo> _englishNameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _chineseNameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly WatcherDatabaseService _databaseService;
    private readonly Context _chineseContext;
    private IReadOnlyList<CountryInfo>? _allCountriesCache;

    public SqliteCountryCatalog(Application application, WatcherDatabaseService databaseService)
    {
        _application = application;
        _databaseService = databaseService;
        _chineseContext = CreateLocalizedContext("zh-CN");
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        await _databaseService.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
    }

    // DXCC list
    public async Task<IReadOnlyList<CountryInfo>> GetAllCountriesAsync(CancellationToken cancellationToken = default)
    {
        var cachedCountries = Volatile.Read(ref _allCountriesCache);
        if (cachedCountries is not null)
        {
            return cachedCountries;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var countries = await connection.Table<CountryRecord>()
            .OrderBy(country => country.EnglishName)
            .ToListAsync()
            .ConfigureAwait(false);

        var mappedCountries = countries.Select(MapCountry).ToList();
        foreach (var country in mappedCountries)
        {
            CacheValue(_englishNameCache, country.EnglishName, country);
        }

        Volatile.Write(ref _allCountriesCache, mappedCountries);
        return mappedCountries;
    }

    // DXCC list
    public async Task<IReadOnlyList<CountryInfo>> SearchCountriesAsync(string query, CancellationToken cancellationToken = default)
    {
        var countries = await GetAllCountriesAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(query))
        {
            return countries;
        }

        var normalized = query.Trim();
        return countries
            .Where(country =>
                country.EnglishName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                country.ChineseName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                country.DxccPrefix.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<CountryInfo?> FindCountryByCallsignAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            return null;
        }

        var normalized = callsign.Trim().ToUpperInvariant();
        if (_callsignCache.TryGetValue(normalized, out var cachedCountry))
        {
            return cachedCountry;
        }

        if (_missingCallsignCache.ContainsKey(normalized))
        {
            return null;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var prefixes = BuildMatchCandidates(normalized);
        var placeholders = string.Join(",", Enumerable.Repeat("?", prefixes.Count));
        var results = await connection.QueryAsync<CountryRecord>(
                $"""
                 SELECT
                     b.id AS id,
                     b.english_name AS english_name,
                     b.dxcc_prefix AS dxcc_prefix,
                     b.cq_zone AS cq_zone,
                     b.itu_zone AS itu_zone,
                     b.continent AS continent,
                     b.latitude AS latitude,
                     b.longitude AS longitude,
                     b.gmt_offset AS gmt_offset
                 FROM callsigns AS a
                 LEFT JOIN countries AS b ON a.country_id = b.id
                 WHERE a.callsign IN ({placeholders})
                 ORDER BY LENGTH(a.callsign) DESC
                 LIMIT 1
                 """,
                prefixes.Cast<object>().ToArray())
            .ConfigureAwait(false);

        var record = results.FirstOrDefault();
        if (record is null)
        {
            CacheMissingCallsign(normalized);
            return null;
        }

        var country = MapCountry(record);
        CacheValue(_callsignCache, normalized, country);
        CacheValue(_englishNameCache, country.EnglishName, country);
        _missingCallsignCache.TryRemove(normalized, out _);
        return country;
    }

    public async Task<CountryInfo?> FindCountryByEnglishNameAsync(string englishName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(englishName))
        {
            return null;
        }

        var normalizedEnglishName = englishName.Trim();
        if (_englishNameCache.TryGetValue(normalizedEnglishName, out var cachedCountry))
        {
            return cachedCountry;
        }

        var connection = await _databaseService.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var record = await connection.Table<CountryRecord>()
            .Where(country => country.EnglishName == normalizedEnglishName)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (record is null)
        {
            return null;
        }

        var country = MapCountry(record);
        CacheValue(_englishNameCache, country.EnglishName, country);
        return country;
    }

    private static IReadOnlyList<string> BuildMatchCandidates(string callsign)
    {
        var result = new List<string>(callsign.Length + 1)
        {
            "=" + callsign
        };

        for (var length = callsign.Length; length > 0; length--)
        {
            result.Add(callsign[..length]);
        }

        return result;
    }

    private CountryInfo MapCountry(CountryRecord record)
    {
        return new CountryInfo
        {
            Id = record.Id,
            EnglishName = record.EnglishName,
            ChineseName = ResolveChineseName(record.EnglishName),
            CqZone = record.CqZone,
            ItuZone = record.ItuZone,
            Continent = record.Continent,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            GmtOffset = record.GmtOffset,
            DxccPrefix = record.DxccPrefix
        };
    }

    private string ResolveChineseName(string englishName)
    {
        return _chineseNameCache.GetOrAdd(englishName, static (name, state) =>
        {
            var (application, chineseContext) = state;
            var resourceKey = DxccTranslationKeyNormalizer.Normalize(name);
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return string.Empty;
            }

            var resourceId = application.Resources?.GetIdentifier(resourceKey, "string", application.PackageName) ?? 0;
            return resourceId > 0 ? chineseContext.GetString(resourceId) ?? string.Empty : string.Empty;
        }, (_application, _chineseContext));
    }

    private static void CacheValue<TValue>(ConcurrentDictionary<string, TValue> cache, string key, TValue value) where TValue : class
    {
        if (cache.Count >= MaxCacheEntries)
        {
            cache.Clear();
        }

        cache[key] = value;
    }

    private void CacheMissingCallsign(string callsign)
    {
        if (_missingCallsignCache.Count >= MaxCacheEntries)
        {
            _missingCallsignCache.Clear();
        }

        _missingCallsignCache[callsign] = 0;
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
}
