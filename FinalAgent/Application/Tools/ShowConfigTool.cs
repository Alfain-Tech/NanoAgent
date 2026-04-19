using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Tools;

internal sealed class ShowConfigTool : IAgentTool
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1/";

    private readonly IUserDataPathProvider _userDataPathProvider;

    public ShowConfigTool(IUserDataPathProvider userDataPathProvider)
    {
        _userDataPathProvider = userDataPathProvider;
    }

    public string Name => AgentToolNames.ShowConfig;

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        string baseUrl = context.Session.ProviderProfile.ProviderKind == ProviderKind.OpenAi
            ? OpenAiBaseUrl
            : context.Session.ProviderProfile.BaseUrl ?? "(not configured)";

        string message =
            "Current configuration:" + Environment.NewLine +
            $"Provider: {context.Session.ProviderName}" + Environment.NewLine +
            $"Base URL: {baseUrl}" + Environment.NewLine +
            $"Configuration file: {_userDataPathProvider.GetConfigurationFilePath()}" + Environment.NewLine +
            $"Active model: {context.Session.ActiveModelId}";

        return Task.FromResult(ToolResult.Success(message));
    }
}
