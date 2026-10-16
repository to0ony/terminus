using Terminus.Service.Common;

namespace Terminus.Service.Gtfs.Import;

/// <summary>Skuplja greške i upozorenja uvoza; isti problem iz više redaka spaja u jedan unos.</summary>
internal sealed class ImportReportBuilder
{
    public const int MaxExamples = 5;

    private readonly Issues _errors = new();
    private readonly Issues _warnings = new();

    public List<GtfsSkippedFile> SkippedFiles { get; } = [];

    public bool HasErrors => _errors.Count > 0;

    public void Error(string? file, string message, string? example = null) => _errors.Add(file, message, example);

    public void Warning(string? file, string message, string? example = null) => _warnings.Add(file, message, example);

    public IReadOnlyList<GtfsImportIssue> Errors => _errors.Build();

    public IReadOnlyList<GtfsImportIssue> Warnings => _warnings.Build();

    private sealed class Issues
    {
        private readonly List<(string? File, string Message)> _order = [];
        private readonly Dictionary<(string? File, string Message), (int Count, List<string> Examples)> _items = [];

        public int Count => _order.Count;

        public void Add(string? file, string message, string? example)
        {
            var key = (file, message);
            if (!_items.TryGetValue(key, out var item))
            {
                item = (0, []);
                _order.Add(key);
            }

            if (example is not null && item.Examples.Count < MaxExamples)
            {
                item.Examples.Add(example);
            }

            _items[key] = (item.Count + 1, item.Examples);
        }

        public IReadOnlyList<GtfsImportIssue> Build() =>
            _order.Select(key => new GtfsImportIssue(key.File, key.Message, _items[key].Count, _items[key].Examples)).ToList();
    }
}
