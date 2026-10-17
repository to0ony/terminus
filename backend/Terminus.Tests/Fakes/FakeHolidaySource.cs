using Terminus.Model.Common;
using Terminus.Service.Common;

namespace Terminus.Tests.Fakes;

/// <summary>Praznici u memoriji; <see cref="Unavailable"/> glumi nedostupan API.</summary>
internal sealed class FakeHolidaySource : IPublicHolidaySource
{
    public List<PublicHoliday> Holidays { get; } = [];
    public bool Unavailable { get; set; }

    public Task<IReadOnlyList<HolidayCountry>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult<IReadOnlyList<HolidayCountry>>([new("HR", "Hrvatska"), new("SI", "Slovenija")]);
    }

    public Task<IReadOnlyList<PublicHoliday>> GetHolidaysAsync(string country, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        return Task.FromResult<IReadOnlyList<PublicHoliday>>(Holidays.Where(h => h.Date >= from && h.Date <= to).ToList());
    }

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
        {
            throw new HolidaySourceException("Izvor nije dostupan.");
        }
    }
}
