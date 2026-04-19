using FinalAgent.Application.Models;
using FinalAgent.Domain.Models;

namespace FinalAgent.Tests.Application.Tools;

internal static class TestSessionFactory
{
    public static ReplSessionContext Create()
    {
        return new ReplSessionContext(
            new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
            "gpt-5-mini",
            ["gpt-5-mini", "gpt-4.1"]);
    }
}
