using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IConfirmationPrompt
{
    Task<bool> PromptAsync(ConfirmationPromptRequest request, CancellationToken cancellationToken);
}
