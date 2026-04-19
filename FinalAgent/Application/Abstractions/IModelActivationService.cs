using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IModelActivationService
{
    ModelActivationResult Resolve(
        ReplSessionContext session,
        string requestedModel);
}
