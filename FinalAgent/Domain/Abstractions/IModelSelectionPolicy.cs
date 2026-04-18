using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Abstractions;

public interface IModelSelectionPolicy
{
    ModelSelectionDecision Select(ModelSelectionContext context);
}
