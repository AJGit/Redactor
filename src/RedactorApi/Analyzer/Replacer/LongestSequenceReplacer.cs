using RedactorApi.Analyzer.Models;

namespace RedactorApi.Analyzer.Replacer;

public class LongestSequenceReplacer(ILogger<LongestSequenceReplacer> logger) : IReplacer
{
    private readonly ILogger<LongestSequenceReplacer> _logger = logger;

    public List<Analysis> FilterReplacements(float threshold, IEnumerable<Analysis> replacements)
    {
        // Sort replacements by score descending, then by start position ascending
        var sortedReplacements = replacements
            .Where(r => Math.Round(r.Score, 2) >= Math.Round(threshold, 2))
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Start);
        // Filter overlapping replacements
        var filteredReplacements = new List<Analysis>();
        foreach (var replacement in sortedReplacements)
        {
            var overlap = filteredReplacements.Any(accepted => replacement.Start < accepted.End && replacement.End > accepted.Start);
            if (!overlap)
            {
                filteredReplacements.Add(replacement);
            }
        }
        // Sort the filtered replacements by start position
        return filteredReplacements.OrderBy(r => r.Start).ToList();
    }
}