namespace FinalAgent.Application.Abstractions;

public interface IUserPrompt
{
    Task ShowMessageAsync(string message, CancellationToken cancellationToken);

    Task<string> PromptAsync(string prompt, CancellationToken cancellationToken);

    Task<string> PromptSecretAsync(string prompt, CancellationToken cancellationToken);

    Task<int> PromptSelectionAsync(
        string prompt,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken);
}
