using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.WebApi.Infrastructure;

/// <summary>
/// Državni praznici s <see href="https://openholidaysapi.org">OpenHolidays API-ja</see>
/// (otvoreni podaci, ODbL, bez ključa). Odgovori se čuvaju jedan dan po državi i
/// godini, a u sezoni se praznici ionako zapisuju u bazu.
/// </summary>
public sealed class OpenHolidaysSource(HttpClient httpClient, IMemoryCache cache) : IPublicHolidaySource
{
    public const string BaseAddress = "https://openholidaysapi.org/";

    private static readonly TimeSpan CacheFor = TimeSpan.FromDays(1);

    /// <summary>Hrvatski naziv ako postoji, inače engleski, inače prvi.</summary>
    private static readonly string[] Languages = ["HR", "EN"];

    public async Task<IReadOnlyList<HolidayCountry>> GetCountriesAsync(CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync("holidays:countries", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var countries = await GetAsync<List<CountryDto>>("Countries", cancellationToken);
            return (IReadOnlyList<HolidayCountry>)countries
                .Select(c => new HolidayCountry(c.IsoCode, Name(c.Name) ?? c.IsoCode))
                .OrderBy(c => c.Name, StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true))
                .ToList();
        }) ?? [];

    public async Task<IReadOnlyList<PublicHoliday>> GetHolidaysAsync(string country, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var result = new List<PublicHoliday>();
        for (var year = from.Year; year <= to.Year; year++)
        {
            result.AddRange(await GetYearAsync(country, year, cancellationToken));
        }

        return result.Where(h => h.Date >= from && h.Date <= to).OrderBy(h => h.Date).ToList();
    }

    private async Task<IReadOnlyList<PublicHoliday>> GetYearAsync(string country, int year, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync($"holidays:{country}:{year}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            var holidays = await GetAsync<List<HolidayDto>>(
                $"PublicHolidays?countryIsoCode={Uri.EscapeDataString(country)}&validFrom={year}-01-01&validTo={year}-12-31",
                cancellationToken);

            // Regionalni praznici (npr. samo u jednoj pokrajini) se ne primjenjuju sami.
            return (IReadOnlyList<PublicHoliday>)holidays
                .Where(h => h.Nationwide)
                .SelectMany(h => Days(h.StartDate, h.EndDate).Select(d => new PublicHoliday(d, Name(h.Name) ?? "")))
                .ToList();
        }) ?? [];

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new HolidaySourceException("OpenHolidays API je vratio prazan odgovor.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
            && !cancellationToken.IsCancellationRequested)
        {
            throw new HolidaySourceException("OpenHolidays API nije dostupan.", ex);
        }
    }

    private static IEnumerable<DateOnly> Days(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            yield return day;
        }
    }

    private static string? Name(IReadOnlyList<LocalizedText>? names) =>
        names is null or { Count: 0 }
            ? null
            : Languages.Select(l => names.FirstOrDefault(n => string.Equals(n.Language, l, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(n => n is not null)?.Text ?? names[0].Text;

    private sealed record LocalizedText(
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("text")] string Text);

    private sealed record CountryDto(
        [property: JsonPropertyName("isoCode")] string IsoCode,
        [property: JsonPropertyName("name")] List<LocalizedText>? Name);

    private sealed record HolidayDto(
        [property: JsonPropertyName("startDate")] DateOnly StartDate,
        [property: JsonPropertyName("endDate")] DateOnly EndDate,
        [property: JsonPropertyName("nationwide")] bool Nationwide,
        [property: JsonPropertyName("name")] List<LocalizedText>? Name);
}
