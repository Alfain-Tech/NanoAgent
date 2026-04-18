using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface ISecretPrompt
{
    Task<string> PromptAsync(SecretPromptRequest request, CancellationToken cancellationToken);
}
