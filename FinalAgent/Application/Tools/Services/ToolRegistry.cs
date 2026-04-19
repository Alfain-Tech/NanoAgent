using FinalAgent.Application.Abstractions;

namespace FinalAgent.Application.Tools.Services;

internal sealed class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentTool> _tools;

    public ToolRegistry(IEnumerable<IAgentTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        Dictionary<string, IAgentTool> toolMap = new(StringComparer.Ordinal);

        foreach (IAgentTool tool in tools)
        {
            if (!toolMap.TryAdd(tool.Name, tool))
            {
                throw new InvalidOperationException(
                    $"Duplicate tool registration detected for '{tool.Name}'.");
            }
        }

        _tools = toolMap;
    }

    public IReadOnlyList<string> GetRegisteredToolNames()
    {
        return _tools.Keys
            .OrderBy(static toolName => toolName, StringComparer.Ordinal)
            .ToArray();
    }

    public bool TryResolve(string toolName, out IAgentTool? tool)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        return _tools.TryGetValue(toolName.Trim(), out tool);
    }
}
