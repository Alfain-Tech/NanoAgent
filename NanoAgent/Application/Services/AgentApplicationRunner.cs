using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Logging;
using NanoAgent.Application.Models;
using NanoAgent.Domain.Models;
using NanoAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NanoAgent.Application.Services;

internal sealed class AgentApplicationRunner : IApplicationRunner
{
    private readonly IFirstRunOnboardingService _onboardingService;
    private readonly IModelDiscoveryService _modelDiscoveryService;
    private readonly IReplRuntime _replRuntime;
    private readonly ApplicationOptions _options;
    private readonly ILogger<AgentApplicationRunner> _logger;

    public AgentApplicationRunner(
        IFirstRunOnboardingService onboardingService,
        IModelDiscoveryService modelDiscoveryService,
        IReplRuntime replRuntime,
        IOptions<ApplicationOptions> options,
        ILogger<AgentApplicationRunner> logger)
    {
        _onboardingService = onboardingService;
        _modelDiscoveryService = modelDiscoveryService;
        _replRuntime = replRuntime;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ApplicationLogMessages.RunnerStarted(_logger, _options.ProductName);

        OnboardingResult result = await _onboardingService.EnsureOnboardedAsync(cancellationToken);
        ModelDiscoveryResult modelResult = await _modelDiscoveryService.DiscoverAndSelectAsync(cancellationToken);

        ApplicationLogMessages.ModelDiscoveryCompleted(
            _logger,
            modelResult.SelectedModelId,
            modelResult.SelectionSource.ToString());

        await _replRuntime.RunAsync(
            new ReplSessionContext(
                _options.ProductName,
                result.Profile,
                modelResult.SelectedModelId,
                modelResult.AvailableModels.Select(static model => model.Id).ToArray()),
            cancellationToken);

        ApplicationLogMessages.RunnerCompleted(
            _logger,
            result.Profile.ProviderKind.ToDisplayName(),
            result.WasOnboardedDuringCurrentRun);
    }
}
