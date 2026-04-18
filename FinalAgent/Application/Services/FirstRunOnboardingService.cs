using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Logging;
using FinalAgent.Application.Models;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FinalAgent.Application.Services;

internal sealed class FirstRunOnboardingService : IFirstRunOnboardingService
{
    private static readonly IReadOnlyList<string> ProviderOptions =
    [
        "OpenAI",
        "OpenAI-compatible provider"
    ];

    private readonly IUserPrompt _userPrompt;
    private readonly IOnboardingInputValidator _inputValidator;
    private readonly IAgentConfigurationStore _configurationStore;
    private readonly IApiKeySecretStore _secretStore;
    private readonly IAgentProviderProfileFactory _profileFactory;
    private readonly ILogger<FirstRunOnboardingService> _logger;

    public FirstRunOnboardingService(
        IUserPrompt userPrompt,
        IOnboardingInputValidator inputValidator,
        IAgentConfigurationStore configurationStore,
        IApiKeySecretStore secretStore,
        IAgentProviderProfileFactory profileFactory,
        ILogger<FirstRunOnboardingService> logger)
    {
        _userPrompt = userPrompt;
        _inputValidator = inputValidator;
        _configurationStore = configurationStore;
        _secretStore = secretStore;
        _profileFactory = profileFactory;
        _logger = logger;
    }

    public async Task<OnboardingResult> EnsureOnboardedAsync(CancellationToken cancellationToken)
    {
        AgentProviderProfile? existingConfiguration = await _configurationStore.LoadAsync(cancellationToken);
        string? existingApiKey = await _secretStore.LoadAsync(cancellationToken);

        if (existingConfiguration is not null && !string.IsNullOrWhiteSpace(existingApiKey))
        {
            ApplicationLogMessages.ExistingOnboardingDetected(
                _logger,
                existingConfiguration.ProviderKind.ToDisplayName());

            return new OnboardingResult(existingConfiguration, WasOnboardedDuringCurrentRun: false);
        }

        if (existingConfiguration is not null || !string.IsNullOrWhiteSpace(existingApiKey))
        {
            ApplicationLogMessages.IncompleteOnboardingDetected(_logger);
        }

        await _userPrompt.ShowMessageAsync("Welcome to FinalAgent. Let's configure your provider for first run.", cancellationToken);

        int selectedOption = await _userPrompt.PromptSelectionAsync(
            "Choose the provider you want to use:",
            ProviderOptions,
            cancellationToken);

        AgentProviderProfile profile = selectedOption == 0
            ? _profileFactory.CreateOpenAi()
            : _profileFactory.CreateCompatible(
                await PromptUntilValidAsync(
                    "Base URL",
                    _inputValidator.ValidateBaseUrl,
                    useSecretPrompt: false,
                    cancellationToken));

        string apiKey = await PromptUntilValidAsync(
            "API key",
            _inputValidator.ValidateApiKey,
            useSecretPrompt: true,
            cancellationToken);

        await _configurationStore.SaveAsync(profile, cancellationToken);
        await _secretStore.SaveAsync(apiKey, cancellationToken);

        await _userPrompt.ShowMessageAsync(
            $"Onboarding complete. Provider: {profile.ProviderKind.ToDisplayName()}.",
            cancellationToken);

        ApplicationLogMessages.OnboardingCompleted(_logger, profile.ProviderKind.ToDisplayName());

        return new OnboardingResult(profile, WasOnboardedDuringCurrentRun: true);
    }

    private async Task<string> PromptUntilValidAsync(
        string fieldLabel,
        Func<string?, InputValidationResult> validate,
        bool useSecretPrompt,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string rawValue = useSecretPrompt
                ? await _userPrompt.PromptSecretAsync($"{fieldLabel}:", cancellationToken)
                : await _userPrompt.PromptAsync($"{fieldLabel}:", cancellationToken);

            InputValidationResult validationResult = validate(rawValue);
            if (validationResult.IsValid)
            {
                return validationResult.NormalizedValue!;
            }

            await _userPrompt.ShowMessageAsync(validationResult.ErrorMessage!, cancellationToken);
        }
    }
}
