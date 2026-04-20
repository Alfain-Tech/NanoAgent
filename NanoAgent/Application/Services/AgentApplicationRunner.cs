using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Logging;
using NanoAgent.Application.Models;
using NanoAgent.Domain.Models;
using NanoAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NanoAgent.Application.Services;

internal sealed class AgentApplicationRunner : IApplicationRunner
{
    private readonly IFirstRunOnboardingService _onboardingService;
    private readonly IModelDiscoveryService _modelDiscoveryService;
    private readonly IReplSectionService _replSectionService;
    private readonly IReplRuntime _replRuntime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentApplicationRunner> _logger;

    public AgentApplicationRunner(
        IFirstRunOnboardingService onboardingService,
        IModelDiscoveryService modelDiscoveryService,
        IReplSectionService replSectionService,
        IReplRuntime replRuntime,
        IConfiguration configuration,
        ILogger<AgentApplicationRunner> logger)
    {
        _onboardingService = onboardingService;
        _modelDiscoveryService = modelDiscoveryService;
        _replSectionService = replSectionService;
        _replRuntime = replRuntime;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ApplicationLogMessages.RunnerStarted(_logger, ApplicationIdentity.ProductName);

        OnboardingResult result = await _onboardingService.EnsureOnboardedAsync(cancellationToken);
        ReplSessionContext session;
        string? requestedSectionId = NormalizeRequestedSectionId(_configuration["section"]);

        if (requestedSectionId is null)
        {
            ModelDiscoveryResult modelResult = await _modelDiscoveryService.DiscoverAndSelectAsync(cancellationToken);

            ApplicationLogMessages.ModelDiscoveryCompleted(
                _logger,
                modelResult.SelectedModelId,
                modelResult.SelectionSource.ToString());

            session = await _replSectionService.CreateNewAsync(
                ApplicationIdentity.ProductName,
                result.Profile,
                modelResult.SelectedModelId,
                modelResult.AvailableModels.Select(static model => model.Id).ToArray(),
                cancellationToken);
        }
        else
        {
            session = await _replSectionService.ResumeAsync(
                ApplicationIdentity.ProductName,
                requestedSectionId,
                cancellationToken);
        }

        await _replRuntime.RunAsync(
            session,
            cancellationToken);

        ApplicationLogMessages.RunnerCompleted(
            _logger,
            session.ProviderProfile.ProviderKind.ToDisplayName(),
            result.WasOnboardedDuringCurrentRun);
    }

    private static string? NormalizeRequestedSectionId(string? sectionId)
    {
        return string.IsNullOrWhiteSpace(sectionId)
            ? null
            : sectionId.Trim();
    }
}
