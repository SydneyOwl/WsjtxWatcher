using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.Contracts;

public interface ICountryCatalog
{
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CountryInfo>> GetAllCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CountryInfo>> SearchCountriesAsync(string query, CancellationToken cancellationToken = default);
    Task<CountryInfo?> FindCountryByCallsignAsync(string callsign, CancellationToken cancellationToken = default);
    Task<CountryInfo?> FindCountryByEnglishNameAsync(string englishName, CancellationToken cancellationToken = default);
}
