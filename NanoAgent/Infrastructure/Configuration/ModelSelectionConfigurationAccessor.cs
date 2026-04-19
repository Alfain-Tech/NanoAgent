using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using Microsoft.Extensions.Options;

namespace NanoAgent.Infrastructure.Configuration;

internal sealed class ModelSelectionConfigurationAccessor : IModelSelectionConfigurationAccessor
{
    private readonly ApplicationOptions _options;

    public ModelSelectionConfigurationAccessor(IOptions<ApplicationOptions> options)
    {
        _options = options.Value;
    }

    public ModelSelectionSettings GetSettings()
    {
        return new ModelSelectionSettings(
            TimeSpan.FromSeconds(_options.ModelSelection.CacheDurationSeconds));
    }
}
