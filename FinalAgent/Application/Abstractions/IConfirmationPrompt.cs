using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IConfirmationPrompt
{
    Task<bool> PromptAsync(ConfirmationPromptRequest request, CancellationToken cancellationToken);
}
