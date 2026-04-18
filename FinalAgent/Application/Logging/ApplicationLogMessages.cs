using Microsoft.Extensions.Logging;

namespace FinalAgent.Application.Logging;

internal static partial class ApplicationLogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Application runner started for product '{productName}'.")]
    public static partial void RunnerStarted(ILogger logger, string productName);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Existing onboarding detected for provider '{providerName}'.")]
    public static partial void ExistingOnboardingDetected(ILogger logger, string providerName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Onboarding completed for provider '{providerName}'.")]
    public static partial void OnboardingCompleted(ILogger logger, string providerName);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Stored onboarding data is incomplete. Re-running onboarding.")]
    public static partial void IncompleteOnboardingDetected(ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Application runner completed successfully. Provider: '{providerName}'. Onboarded during this run: {wasOnboarded}.")]
    public static partial void RunnerCompleted(ILogger logger, string providerName, bool wasOnboarded);
}
