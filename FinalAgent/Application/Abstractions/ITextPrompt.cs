using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface ITextPrompt
{
    Task<string> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken);
}
