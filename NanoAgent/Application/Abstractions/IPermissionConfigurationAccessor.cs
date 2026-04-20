using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IPermissionConfigurationAccessor
{
    PermissionSettings GetSettings();
}
