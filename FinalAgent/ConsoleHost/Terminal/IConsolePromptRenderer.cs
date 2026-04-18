using FinalAgent.Application.Models;

namespace FinalAgent.ConsoleHost.Terminal;

internal interface IConsolePromptRenderer
{
    int WriteInteractiveSelectionPrompt<T>(SelectionPromptRequest<T> request, int selectedIndex);

    void RewriteSelectionOptions<T>(SelectionPromptRequest<T> request, int selectedIndex, int optionsTop);

    void WriteFallbackSelectionPrompt<T>(SelectionPromptRequest<T> request);

    void WriteSecretPrompt(SecretPromptRequest request);

    void WriteStatus(StatusMessageKind kind, string message);

    void WriteTextPrompt(TextPromptRequest request);
}
