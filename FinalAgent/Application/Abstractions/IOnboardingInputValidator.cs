using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IOnboardingInputValidator
{
    InputValidationResult ValidateApiKey(string? value);

    InputValidationResult ValidateBaseUrl(string? value);
}
