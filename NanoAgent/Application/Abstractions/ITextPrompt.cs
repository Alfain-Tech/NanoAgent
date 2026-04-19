using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface ITextPrompt
{
    Task<string> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken);
}
