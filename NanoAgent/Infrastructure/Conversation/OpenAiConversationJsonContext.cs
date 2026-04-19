using System.Text.Json.Serialization;

namespace NanoAgent.Infrastructure.Conversation;

[JsonSerializable(typeof(OpenAiChatCompletionRequest))]
[JsonSerializable(typeof(OpenAiChatCompletionResponse))]
internal sealed partial class OpenAiConversationJsonContext : JsonSerializerContext
{
}
