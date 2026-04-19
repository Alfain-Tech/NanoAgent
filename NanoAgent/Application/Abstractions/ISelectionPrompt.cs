using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface ISelectionPrompt
{
    Task<T> PromptAsync<T>(SelectionPromptRequest<T> request, CancellationToken cancellationToken);
}
