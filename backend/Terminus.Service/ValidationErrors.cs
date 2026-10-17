using Terminus.Common.Exceptions;

namespace Terminus.Service;

/// <summary>Skuplja greške po svojstvu pa ih baca odjednom kao ValidationException.</summary>
internal sealed class ValidationErrors
{
    public const int MaxPageSize = 100;

    private readonly Dictionary<string, List<string>> _errors = new();

    public void AddPagingErrors(int page, int pageSize)
    {
        if (page < 1)
        {
            Add("Page", "Broj stranice mora biti 1 ili veći.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            Add("PageSize", $"Veličina stranice mora biti između 1 i {MaxPageSize}.");
        }
    }

    public void Add(string property, string message)
    {
        if (!_errors.TryGetValue(property, out var messages))
        {
            messages = [];
            _errors[property] = messages;
        }

        messages.Add(message);
    }

    public bool Contains(string property) => _errors.ContainsKey(property);

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
        {
            throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
