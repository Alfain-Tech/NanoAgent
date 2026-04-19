using NanoAgent.Application.Abstractions;

namespace NanoAgent.Application.Services;

internal sealed class HeuristicTokenEstimator : ITokenEstimator
{
    public int Estimate(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        string trimmed = text.Trim();
        int byCharacters = (int)Math.Ceiling(trimmed.Length / 4.0);
        int byWords = trimmed
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return Math.Max(1, Math.Max(byCharacters, byWords));
    }
}
