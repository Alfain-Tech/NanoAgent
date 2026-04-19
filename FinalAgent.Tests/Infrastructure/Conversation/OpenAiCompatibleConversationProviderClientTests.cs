using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using FinalAgent.Application.Models;
using FinalAgent.Domain.Models;
using FinalAgent.Infrastructure.Conversation;
using FluentAssertions;

namespace FinalAgent.Tests.Infrastructure.Conversation;

public sealed class OpenAiCompatibleConversationProviderClientTests
{
    [Fact]
    public async Task SendAsync_Should_PostChatCompletionsToV1Endpoint_When_CompatibleProviderBaseUrlHasNoPath()
    {
        RecordingHandler handler = new("""
            {
              "id": "resp_1",
              "choices": [
                {
                  "message": {
                    "content": "Hello."
                  }
                }
              ]
            }
            """);
        HttpClient httpClient = new(handler);
        OpenAiCompatibleConversationProviderClient sut = new(httpClient);

        ConversationProviderPayload payload = await sut.SendAsync(
            new ConversationProviderRequest(
                new AgentProviderProfile(ProviderKind.OpenAiCompatible, "http://127.0.0.1:1234"),
                "test-key",
                "gpt-4.1",
                "Explain the diff.",
                "You are helpful.",
                [CreateToolDefinition("file_read")]),
            CancellationToken.None);

        handler.RequestUri.Should().Be(new Uri("http://127.0.0.1:1234/v1/chat/completions"));
        handler.RequestMethod.Should().Be(HttpMethod.Post);
        handler.AuthorizationHeader.Should().Be("Bearer test-key");
        handler.RequestBody.Should().Contain("\"model\":\"gpt-4.1\"");
        handler.RequestBody.Should().Contain("\"role\":\"system\"");
        handler.RequestBody.Should().Contain("\"role\":\"user\"");
        handler.RequestBody.Should().Contain("\"tools\"");
        handler.RequestBody.Should().Contain("\"name\":\"file_read\"");
        payload.ResponseId.Should().Be("req_789");
    }

    private static ToolDefinition CreateToolDefinition(string name)
    {
        using JsonDocument schemaDocument = JsonDocument.Parse(
            """{ "type": "object", "properties": {}, "additionalProperties": false }""");

        return new ToolDefinition(
            name,
            $"Description for {name}",
            schemaDocument.RootElement.Clone());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public RecordingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string? AuthorizationHeader { get; private set; }

        public string? RequestBody { get; private set; }

        public HttpMethod? RequestMethod { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestMethod = request.Method;
            AuthorizationHeader = request.Headers.Authorization?.ToString();
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            response.Headers.Add("x-request-id", "req_789");

            return response;
        }
    }
}
