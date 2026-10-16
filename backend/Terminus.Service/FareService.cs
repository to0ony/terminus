using System.Text.RegularExpressions;
using Terminus.Common;
using Terminus.Common.Exceptions;
using Terminus.Model.Common;
using Terminus.Model.Common.Filters;
using Terminus.Repository.Common;
using Terminus.Service.Common;
using static Terminus.Service.Validators;

namespace Terminus.Service;

public partial class FareService(
    IFareRepository repository,
    IAgencyRepository agencyRepository,
    IRouteRepository routeRepository) : IFareService
{
    public async Task<IFareModel> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await repository.GetAsync(id, cancellationToken) ?? throw FareNotFound(id);

    public Task<PagedResult<IFareModel>> FindAsync(FareFilter filter, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddPagingErrors(filter.Page, filter.PageSize);
        errors.ThrowIfAny();

        return repository.FindAsync(filter, cancellationToken);
    }

    public async Task<IFareModel> CreateAsync(IFareModel fare, CancellationToken cancellationToken = default)
    {
        fare.Id = Guid.Empty;
        Normalize(fare);
        await ValidateAsync(fare, cancellationToken);

        return await repository.InsertAsync(fare, cancellationToken);
    }

    public async Task<IFareModel> UpdateAsync(Guid id, IFareModel fare, CancellationToken cancellationToken = default)
    {
        fare.Id = id;
        Normalize(fare);
        await ValidateAsync(fare, cancellationToken);

        return await repository.UpdateAsync(fare, cancellationToken) ?? throw FareNotFound(id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            throw FareNotFound(id);
        }
    }

    private static void Normalize(IFareModel fare)
    {
        fare.Code = fare.Code?.Trim() ?? string.Empty;
        fare.CurrencyType = fare.CurrencyType?.Trim().ToUpperInvariant() ?? string.Empty;

        foreach (var rule in fare.Rules)
        {
            rule.OriginId = NullIfBlank(rule.OriginId);
            rule.DestinationId = NullIfBlank(rule.DestinationId);
            rule.ContainsId = NullIfBlank(rule.ContainsId);
        }
    }

    private async Task ValidateAsync(IFareModel fare, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (fare.Code.Length == 0)
        {
            errors.Add(nameof(fare.Code), "Kod je obavezan.");
        }

        if (fare.Price < 0)
        {
            errors.Add(nameof(fare.Price), "Cijena ne smije biti negativna.");
        }

        if (!CurrencyRegex().IsMatch(fare.CurrencyType))
        {
            errors.Add(nameof(fare.CurrencyType), "Valuta mora biti ISO 4217 oznaka od tri slova, npr. EUR.");
        }

        if (!Enum.IsDefined(fare.PaymentMethod))
        {
            errors.Add(nameof(fare.PaymentMethod), "Način plaćanja mora biti 0 (u vozilu) ili 1 (prije ukrcavanja).");
        }

        if (fare.Transfers is { } transfers && !Enum.IsDefined(transfers))
        {
            errors.Add(nameof(fare.Transfers), "Broj presjedanja mora biti 0, 1, 2 ili prazno (neograničeno).");
        }

        if (fare.TransferDuration < 0)
        {
            errors.Add(nameof(fare.TransferDuration), "Trajanje presjedanja ne smije biti negativno.");
        }

        if (fare.AgencyId is Guid agencyId && await agencyRepository.GetAsync(agencyId, cancellationToken) is null)
        {
            errors.Add(nameof(fare.AgencyId), "Prijevoznik ne postoji.");
        }

        for (var i = 0; i < fare.Rules.Count; i++)
        {
            var rule = fare.Rules[i];
            var prefix = $"Rules[{i}]";

            if (rule.RouteId is null && rule.OriginId is null && rule.DestinationId is null && rule.ContainsId is null)
            {
                errors.Add(prefix, "Pravilo mora imati liniju ili barem jednu zonu.");
            }

            if (rule.RouteId is Guid routeId && await routeRepository.GetAsync(routeId, cancellationToken) is null)
            {
                errors.Add($"{prefix}.RouteId", "Linija ne postoji.");
            }
        }

        errors.ThrowIfAny();
    }

    private static NotFoundException FareNotFound(Guid id) => new($"Cijena {id} ne postoji.");

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyRegex();
}
