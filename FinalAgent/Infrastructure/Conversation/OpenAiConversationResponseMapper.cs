using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;

namespace FinalAgent.Infrastructure.Conversation;

internal sealed class OpenAiConversationResponseMapper : IConversationResponseMapper
{
    public ConversationResponse Map(ConversationProviderPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        OpenAiChatCompletionResponse? response = JsonSerializer.Deserialize(
            payload.RawContent,
            OpenAiConversationJsonContext.Default.OpenAiChatCompletionResponse);

        OpenAiChatCompletionChoice? firstChoice = response?.Choices?.FirstOrDefault();
        OpenAiChatCompletionResponseMessage? message = firstChoice?.Message;

        if (message is null)
        {
            throw new ConversationResponseException(
                "The provider response did not contain a chat completion message.");
        }

        List<ConversationToolCall> toolCalls = [];

        if (message.ToolCalls is not null)
        {
            foreach (OpenAiChatCompletionToolCall toolCall in message.ToolCalls)
            {
                if (!string.Equals(toolCall.Type, "function", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(toolCall.Id) ||
                    string.IsNullOrWhiteSpace(toolCall.Function?.Name) ||
                    string.IsNullOrWhiteSpace(toolCall.Function.Arguments))
                {
                    throw new ConversationResponseException(
                        "The provider returned an incomplete tool call payload.");
                }

                toolCalls.Add(new ConversationToolCall(
                    toolCall.Id.Trim(),
                    toolCall.Function.Name.Trim(),
                    toolCall.Function.Arguments));
            }
        }

        string? assistantMessage = string.IsNullOrWhiteSpace(message.Content)
            ? null
            : message.Content.Trim();

        if (toolCalls.Count == 0 && assistantMessage is null)
        {
            throw new ConversationResponseException(
                "The provider returned neither assistant content nor usable tool calls.");
        }

        return new ConversationResponse(
            assistantMessage,
            toolCalls,
            response?.Id ?? payload.ResponseId);
    }
}
