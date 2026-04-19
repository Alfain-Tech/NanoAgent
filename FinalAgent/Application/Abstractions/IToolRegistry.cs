using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IToolRegistry
{
    IReadOnlyList<ToolDefinition> GetToolDefinitions();

    IReadOnlyList<string> GetRegisteredToolNames();

    bool TryResolve(string toolName, out ToolRegistration? tool);
}
