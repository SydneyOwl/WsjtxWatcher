using Android.App;
using System.Globalization;
using SQLite;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Persistence;

public sealed class WatcherDatabaseService
{
    private const string CountryCatalogVersion = "cty-2026-03-29-v1";
    private const string CountryCatalogVersionKey = "country_catalog_version";
    private readonly Application _application;
    private readonly IAppInfoService _appInfoService;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private SQLiteAsyncConnection? _connection;
    private bool _isInitialized;

    public WatcherDatabaseService(Application application, IAppInfoService appInfoService)
    {
        _application = application;
        _appInfoService = appInfoService;
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        return _connection!;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            Directory.CreateDirectory(_appInfoService.AppDataDirectory);
            var connectionString = new SQLiteConnectionString(
                Path.Combine(_appInfoService.AppDataDirectory, "wsjtxwatcher.db"),
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache,
                storeDateTimeAsTicks: true);

            _connection = new SQLiteAsyncConnection(connectionString);
            await _connection.EnableWriteAheadLoggingAsync().ConfigureAwait(false);
            await _connection.CreateTableAsync<DbMetadataRecord>().ConfigureAwait(false);
            await _connection.CreateTableAsync<CountryRecord>().ConfigureAwait(false);
            await _connection.CreateTableAsync<CallsignPrefixRecord>().ConfigureAwait(false);
            await _connection.CreateTableAsync<GridCacheRecord>().ConfigureAwait(false);
            await EnsureCountryCatalogAsync().ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private async Task EnsureCountryCatalogAsync()
    {
        var storedVersion = await _connection!.Table<DbMetadataRecord>()
            .Where(record => record.Key == CountryCatalogVersionKey)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        var countryCount = await _connection.Table<CountryRecord>().CountAsync().ConfigureAwait(false);
        var prefixCount = await _connection.Table<CallsignPrefixRecord>().CountAsync().ConfigureAwait(false);
        if (storedVersion?.Value == CountryCatalogVersion && countryCount > 0 && prefixCount > 0)
        {
            return;
        }

        using var stream = _application.Assets!.Open("cty.dat");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync().ConfigureAwait(false);
        var countries = new List<CountryRecord>();
        var prefixes = new List<CallsignPrefixRecord>();
        var blocks = contents.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var nextCountryId = 1;

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

            var englishName = ParseString(parts[0]);
            if (string.IsNullOrWhiteSpace(englishName))
            {
                continue;
            }

            countries.Add(new CountryRecord
            {
                Id = nextCountryId,
                EnglishName = englishName,
                CqZone = ParseInt(parts[1]),
                ItuZone = ParseInt(parts[2]),
                Continent = ParseString(parts[3]),
                Latitude = ParseDouble(parts[4]),
                Longitude = ParseDouble(parts[5]) * -1d,
                GmtOffset = ParseDouble(parts[6]),
                DxccPrefix = ParseString(parts[7])
            });

            foreach (var prefix in ExtractPrefixes(parts[8]))
            {
                prefixes.Add(new CallsignPrefixRecord
                {
                    CountryId = nextCountryId,
                    Callsign = prefix
                });
            }

            nextCountryId += 1;
        }

        await _connection.RunInTransactionAsync(db =>
        {
            db.DropTable<CallsignPrefixRecord>();
            db.DropTable<CountryRecord>();
            db.CreateTable<CountryRecord>();
            db.CreateTable<CallsignPrefixRecord>();
            db.InsertAll(countries);
            db.InsertAll(prefixes);
            db.InsertOrReplace(new DbMetadataRecord
            {
                Key = CountryCatalogVersionKey,
                Value = CountryCatalogVersion
            });
        }).ConfigureAwait(false);
    }

    private static IEnumerable<string> ExtractPrefixes(string rawPrefixes)
    {
        foreach (var rawPrefix in rawPrefixes.Replace("\r", string.Empty).Replace("\n", string.Empty)
                     .Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var normalized = rawPrefix.Trim();
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

            normalized = normalized.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(ParseString(value), out var result) ? result : 0;
    }

    private static double ParseDouble(string value)
    {
        return double.TryParse(ParseString(value), NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0d;
    }

    private static string ParseString(string value)
    {
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
    }
}
