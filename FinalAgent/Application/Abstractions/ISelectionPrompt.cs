using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface ISelectionPrompt
{
    Task<T> PromptAsync<T>(SelectionPromptRequest<T> request, CancellationToken cancellationToken);
}
