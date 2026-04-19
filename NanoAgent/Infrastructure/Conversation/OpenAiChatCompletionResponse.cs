using System.Text.Json.Serialization;

namespace NanoAgent.Infrastructure.Conversation;

internal sealed record OpenAiChatCompletionResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChatCompletionChoice>? Choices);

internal sealed record OpenAiChatCompletionChoice(
    [property: JsonPropertyName("message")] OpenAiChatCompletionResponseMessage? Message);

internal sealed record OpenAiChatCompletionResponseMessage(
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("tool_calls")] IReadOnlyList<OpenAiChatCompletionToolCall>? ToolCalls);

internal sealed record OpenAiChatCompletionToolCall(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("function")] OpenAiChatCompletionFunctionCall? Function);

internal sealed record OpenAiChatCompletionFunctionCall(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("arguments")] string? Arguments);
