namespace FinalAgent.Application.Abstractions;

public interface IToolRegistry
{
    IReadOnlyList<string> GetRegisteredToolNames();

    bool TryResolve(string toolName, out IAgentTool? tool);
}
