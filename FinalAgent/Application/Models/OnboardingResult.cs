using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed record OnboardingResult(
    AgentProviderProfile Profile,
    bool WasOnboardedDuringCurrentRun);
