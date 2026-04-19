using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IOnboardingInputValidator
{
    InputValidationResult ValidateApiKey(string? value);

    InputValidationResult ValidateBaseUrl(string? value);
}
