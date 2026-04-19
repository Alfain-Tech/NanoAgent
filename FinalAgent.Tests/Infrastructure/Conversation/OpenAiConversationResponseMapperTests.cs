using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
using FinalAgent.Domain.Models;
using FinalAgent.Infrastructure.Conversation;
using FluentAssertions;

namespace FinalAgent.Tests.Infrastructure.Conversation;

public sealed class OpenAiConversationResponseMapperTests
{
    [Fact]
    public void Map_Should_ReturnAssistantMessage_When_ResponseContainsContent()
    {
        OpenAiConversationResponseMapper sut = new();

        ConversationResponse response = sut.Map(new ConversationProviderPayload(
            ProviderKind.OpenAi,
            """
            {
              "id": "resp_1",
              "choices": [
                {
                  "message": {
                    "content": "Hello from the provider."
                  }
                }
              ]
            }
            """,
            null));

        response.AssistantMessage.Should().Be("Hello from the provider.");
        response.ToolCalls.Should().BeEmpty();
        response.ResponseId.Should().Be("resp_1");
    }

    [Fact]
    public void Map_Should_ReturnToolCalls_When_ResponseContainsFunctionCalls()
    {
        OpenAiConversationResponseMapper sut = new();

        ConversationResponse response = sut.Map(new ConversationProviderPayload(
            ProviderKind.OpenAiCompatible,
            """
            {
              "choices": [
                {
                  "message": {
                    "tool_calls": [
                      {
                        "id": "call_1",
                        "type": "function",
                        "function": {
                          "name": "use_model",
                          "arguments": "{ \"model\": \"gpt-5-mini\" }"
                        }
                      }
                    ]
                  }
                }
              ]
            }
            """,
            "fallback_id"));

        response.AssistantMessage.Should().BeNull();
        response.ToolCalls.Should().ContainSingle();
        response.ToolCalls[0].Name.Should().Be("use_model");
        response.ResponseId.Should().Be("fallback_id");
    }

    [Fact]
    public void Map_Should_ThrowConversationResponseException_When_ResponseHasNoMessageAndNoToolCalls()
    {
        OpenAiConversationResponseMapper sut = new();

        Action action = () => sut.Map(new ConversationProviderPayload(
            ProviderKind.OpenAi,
            """
            {
              "choices": [
                {
                  "message": {
                    "content": "   "
                  }
                }
              ]
            }
            """,
            null));

        action.Should().Throw<ConversationResponseException>()
            .WithMessage("*neither assistant content nor usable tool calls*");
    }
}
