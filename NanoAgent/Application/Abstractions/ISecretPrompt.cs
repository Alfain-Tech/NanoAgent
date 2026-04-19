using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface ISecretPrompt
{
    Task<string> PromptAsync(SecretPromptRequest request, CancellationToken cancellationToken);
}
