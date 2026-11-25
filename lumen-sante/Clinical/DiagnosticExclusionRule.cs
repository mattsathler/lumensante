using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Clinical
{
    internal sealed class DiagnosticCondition
    {
        internal DiagnosticCondition(string parameter, double minimum, double maximum)
        { Parameter = parameter; Minimum = minimum; Maximum = maximum; }
        internal string Parameter { get; }
        internal double Minimum { get; }
        internal double Maximum { get; }
    }

    internal sealed class DiagnosticExclusionRule
    {
        internal DiagnosticExclusionRule(DiagnosticCondition[] conditions) { Conditions = conditions; }
        internal IReadOnlyList<DiagnosticCondition> Conditions { get; }
        internal bool Matches(IReadOnlyDictionary<string, double> values) => Conditions.All(condition =>
            values.TryGetValue(condition.Parameter, out double value) && value >= condition.Minimum && value <= condition.Maximum);
    }
}
