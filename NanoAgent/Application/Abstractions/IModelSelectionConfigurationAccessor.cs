using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IModelSelectionConfigurationAccessor
{
    ModelSelectionSettings GetSettings();
}
