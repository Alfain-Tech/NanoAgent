using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IModelActivationService
{
    ModelActivationResult Resolve(
        ReplSessionContext session,
        string requestedModel);
}
