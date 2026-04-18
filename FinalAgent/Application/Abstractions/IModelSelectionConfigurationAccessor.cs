using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IModelSelectionConfigurationAccessor
{
    ModelSelectionSettings GetSettings();
}
