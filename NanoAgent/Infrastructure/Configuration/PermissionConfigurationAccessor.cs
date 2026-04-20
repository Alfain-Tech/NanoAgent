using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using Microsoft.Extensions.Options;

namespace NanoAgent.Infrastructure.Configuration;

internal sealed class PermissionConfigurationAccessor : IPermissionConfigurationAccessor
{
    private static readonly PermissionRule[] BuiltInRules =
    [
        new PermissionRule
        {
            Tools = ["read"],
            Mode = PermissionMode.Allow
        },
        new PermissionRule
        {
            Tools = ["webfetch"],
            Mode = PermissionMode.Allow
        },
        new PermissionRule
        {
            Tools = ["lsp"],
            Mode = PermissionMode.Allow
        },
        new PermissionRule
        {
            Tools = ["bash"],
            Mode = PermissionMode.Ask
        },
        new PermissionRule
        {
            Tools = ["edit"],
            Mode = PermissionMode.Ask
        },
        new PermissionRule
        {
            Tools = ["task"],
            Mode = PermissionMode.Ask
        },
        new PermissionRule
        {
            Tools = ["external_directory"],
            Mode = PermissionMode.Ask
        },
        new PermissionRule
        {
            Tools = ["doom_loop"],
            Mode = PermissionMode.Deny
        },
        new PermissionRule
        {
            Tools = ["read"],
            Mode = PermissionMode.Deny,
            Patterns = [".env", ".env.*", "**/.env", "**/.env.*"]
        }
    ];

    private readonly IOptions<ApplicationOptions> _options;

    public PermissionConfigurationAccessor(IOptions<ApplicationOptions> options)
    {
        _options = options;
    }

    public PermissionSettings GetSettings()
    {
        PermissionSettings configured = _options.Value.Permissions ?? new PermissionSettings();

        PermissionRule[] configuredRules = (configured.Rules ?? [])
            .Where(static rule => rule is not null)
            .Select(NormalizeRule)
            .ToArray();

        PermissionRule[] effectiveRules = BuiltInRules
            .Concat(configuredRules)
            .Select(NormalizeRule)
            .ToArray();

        return new PermissionSettings
        {
            DefaultMode = configured.DefaultMode,
            Rules = effectiveRules
        };
    }

    private static PermissionRule NormalizeRule(PermissionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new PermissionRule
        {
            Mode = rule.Mode,
            Patterns = (rule.Patterns ?? [])
                .Where(static pattern => !string.IsNullOrWhiteSpace(pattern))
                .Select(static pattern => pattern.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Tools = (rule.Tools ?? [])
                .Where(static tool => !string.IsNullOrWhiteSpace(tool))
                .Select(static tool => tool.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }
}
