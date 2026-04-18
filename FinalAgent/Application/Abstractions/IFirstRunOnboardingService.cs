using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IFirstRunOnboardingService
{
    Task<OnboardingResult> EnsureOnboardedAsync(CancellationToken cancellationToken);
}
