using NanoAgent.Domain.Models;

namespace NanoAgent.Domain.Abstractions;

public interface IModelSelectionPolicy
{
    ModelSelectionDecision Select(ModelSelectionContext context);
}
