using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.Core.Services;

public sealed class CloudlogIgnoredCallsignImportService : ICloudlogIgnoredCallsignImportService
{
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(30);

    public async Task<CloudlogIgnoredCallsignImportData> DownloadIgnoredCallsignEntriesAsync(
        string baseUrl,
        string username,
        string password,
        string stationId,
        int lookbackDays,
        CancellationToken cancellationToken = default)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var normalizedUsername = NormalizeRequiredValue(username, "Username");
        var normalizedPassword = NormalizeRequiredValue(password, "Password");
        var normalizedStationId = NormalizeRequiredValue(stationId, "Station ID");
        var normalizedLookbackDays = NormalizeLookbackDays(lookbackDays);

        using var client = CreateCookieHttpClient(ExportTimeout);
        await LoginAsync(client, normalizedBaseUrl, normalizedUsername, normalizedPassword, cancellationToken).ConfigureAwait(false);
        var adifContent = await DownloadAdifAsync(client, normalizedBaseUrl, normalizedStationId, normalizedLookbackDays, cancellationToken)
            .ConfigureAwait(false);

        return ParseImportedEntries(adifContent);
    }

    private static async Task LoginAsync(
        HttpClient client,
        string baseUrl,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsync(
                BuildUrl(baseUrl, "index.php/user/login"),
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["user_name"] = username,
                    ["user_password"] = password
                }),
                cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.SeeOther)
        {
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            if (location.Contains("dashboard", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode && responseText.Contains("dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException("Cloudlog/Wavelog login failed. Check the username and password.");
    }

    private static async Task<string> DownloadAdifAsync(
        HttpClient client,
        string baseUrl,
        string stationId,
        int lookbackDays,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fromDate = DateTime.Today.AddDays(lookbackDays * -1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var payload = new MultipartFormDataContent
        {
            { new StringContent(stationId), "station_profile" },
            { new StringContent(fromDate), "from" },
            { new StringContent(today), "to" }
        };

        using var response = await client.PostAsync(
                BuildUrl(baseUrl, "adif/export_custom"),
                payload,
                cancellationToken)
            .ConfigureAwait(false);
        var adifContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return adifContent;
    }

    private static CloudlogIgnoredCallsignImportData ParseImportedEntries(string adifContent)
    {
        var entries = new List<IgnoredCallsignEntry>();
        var currentCallsign = string.Empty;
        var currentBand = string.Empty;
        var recordCount = 0;
        var index = 0;

        while (index < adifContent.Length)
        {
            var tagStart = adifContent.IndexOf('<', index);
            if (tagStart < 0)
            {
                break;
            }

            var tagEnd = adifContent.IndexOf('>', tagStart + 1);
            if (tagEnd < 0)
            {
                break;
            }

            var descriptor = adifContent[(tagStart + 1)..tagEnd].Trim();
            if (string.IsNullOrWhiteSpace(descriptor))
            {
                index = tagEnd + 1;
                continue;
            }

            if (descriptor.Equals("eoh", StringComparison.OrdinalIgnoreCase))
            {
                currentCallsign = string.Empty;
                currentBand = string.Empty;
                index = tagEnd + 1;
                continue;
            }

            if (descriptor.Equals("eor", StringComparison.OrdinalIgnoreCase))
            {
                recordCount++;
                TryAddEntry(entries, currentCallsign, currentBand);
                currentCallsign = string.Empty;
                currentBand = string.Empty;
                index = tagEnd + 1;
                continue;
            }

            var colonIndex = descriptor.IndexOf(':');
            if (colonIndex < 0)
            {
                index = tagEnd + 1;
                continue;
            }

            var fieldName = descriptor[..colonIndex].Trim();
            var lengthText = descriptor[(colonIndex + 1)..];
            var valueTypeIndex = lengthText.IndexOf(':');
            if (valueTypeIndex >= 0)
            {
                lengthText = lengthText[..valueTypeIndex];
            }

            if (!int.TryParse(lengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueLength) ||
                valueLength < 0)
            {
                index = tagEnd + 1;
                continue;
            }

            var valueStart = tagEnd + 1;
            if (valueStart + valueLength > adifContent.Length)
            {
                break;
            }

            var value = adifContent.Substring(valueStart, valueLength).Trim();
            switch (fieldName.ToUpperInvariant())
            {
                case "CALL":
                    currentCallsign = value;
                    break;
                case "BAND":
                    currentBand = value;
                    break;
            }

            index = valueStart + valueLength;
        }

        if (!string.IsNullOrWhiteSpace(currentCallsign) || !string.IsNullOrWhiteSpace(currentBand))
        {
            recordCount++;
            TryAddEntry(entries, currentCallsign, currentBand);
        }

        return new CloudlogIgnoredCallsignImportData
        {
            RecordCount = recordCount,
            Entries = IgnoredCallsignMatcher.NormalizeEntries(entries)
        };
    }

    private static void TryAddEntry(ICollection<IgnoredCallsignEntry> entries, string callsign, string band)
    {
        if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(band))
        {
            return;
        }

        entries.Add(new IgnoredCallsignEntry
        {
            Callsign = callsign,
            Band = band
        });
    }

    private static HttpClient CreateCookieHttpClient(TimeSpan timeout)
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        return CreateHttpClient(handler, timeout);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler, TimeSpan timeout)
    {
        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WsjtxWatcher", "1.0"));
        return client;
    }

    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var normalized = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Cloudlog/Wavelog URL is required.");
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Cloudlog/Wavelog URL must start with http:// or https://.");
        }

        return normalized;
    }

    private static string NormalizeRequiredValue(string? value, string displayName)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        throw new InvalidOperationException($"{displayName} is required.");
    }

    private static int NormalizeLookbackDays(int lookbackDays)
    {
        return lookbackDays > 0
            ? lookbackDays
            : throw new InvalidOperationException("Lookback days must be greater than zero.");
    }

    private static string BuildUrl(string baseUrl, string relativePath)
    {
        return $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}
