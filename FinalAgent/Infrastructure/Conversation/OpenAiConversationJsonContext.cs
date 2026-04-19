using System.Text.Json.Serialization;

namespace FinalAgent.Infrastructure.Conversation;

[JsonSerializable(typeof(OpenAiChatCompletionRequest))]
[JsonSerializable(typeof(OpenAiChatCompletionResponse))]
internal sealed partial class OpenAiConversationJsonContext : JsonSerializerContext
{
}
