using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IConversationConfigurationAccessor
{
    ConversationSettings GetSettings();
}
