using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IConversationConfigurationAccessor
{
    ConversationSettings GetSettings();
}
