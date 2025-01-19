using RedactorApi.Analyzer.Models;

namespace RedactorApi.Analyzer.Replacer;

public interface IReplacer
{
    List<Analysis> FilterReplacements(float threshold, IEnumerable<Analysis> replacements);
}
