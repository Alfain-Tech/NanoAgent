using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Conversation.Services;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Serialization;
using FinalAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinalAgent.Tests.Application.Conversation.Services;

public sealed class AgentConversationPipelineTests
{
    private static readonly ReplSessionContext Session = new(
        new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
        "gpt-5-mini",
        ["gpt-5-mini", "gpt-4.1"]);

    [Fact]
    public async Task ProcessAsync_Should_ReturnAssistantMessage_When_ResponseContainsNormalAssistantContent()
    {
        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IConversationConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ConversationSettings("You are helpful.", TimeSpan.FromSeconds(30)));

        Mock<IToolRegistry> toolRegistry = new(MockBehavior.Strict);
        toolRegistry
            .Setup(registry => registry.GetToolDefinitions())
            .Returns([
                CreateToolDefinition("file_read")
            ]);

        Mock<IConversationProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.SendAsync(
                It.Is<ConversationProviderRequest>(request =>
                    request.AvailableTools.Count == 1 &&
                    request.AvailableTools[0].Name == "file_read"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationProviderPayload(
                ProviderKind.OpenAiCompatible,
                """{ "choices": [] }""",
                "resp_123"));

        Mock<IConversationResponseMapper> responseMapper = new(MockBehavior.Strict);
        responseMapper
            .Setup(mapper => mapper.Map(It.IsAny<ConversationProviderPayload>()))
            .Returns(new ConversationResponse("Ready to help.", [], "resp_123"));

        Mock<IToolExecutionPipeline> toolExecutionPipeline = new(MockBehavior.Strict);

        AgentConversationPipeline sut = CreateSut(
            secretStore.Object,
            providerClient.Object,
            responseMapper.Object,
            toolExecutionPipeline.Object,
            toolRegistry.Object,
            configurationAccessor.Object);

        ConversationTurnResult result = await sut.ProcessAsync(
            "Plan the next refactor.",
            Session,
            CancellationToken.None);

        result.Kind.Should().Be(ConversationTurnResultKind.AssistantMessage);
        result.ResponseText.Should().Be("Ready to help.");
        toolExecutionPipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessAsync_Should_HandOffToToolExecutionPipeline_When_ResponseContainsToolCalls()
    {
        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IConversationConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ConversationSettings(null, TimeSpan.FromSeconds(30)));

        Mock<IToolRegistry> toolRegistry = new(MockBehavior.Strict);
        toolRegistry
            .Setup(registry => registry.GetToolDefinitions())
            .Returns([
                CreateToolDefinition("directory_list")
            ]);

        Mock<IConversationProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.SendAsync(
                It.IsAny<ConversationProviderRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationProviderPayload(
                ProviderKind.OpenAiCompatible,
                """{ "choices": [] }""",
                "resp_456"));

        Mock<IConversationResponseMapper> responseMapper = new(MockBehavior.Strict);
        responseMapper
            .Setup(mapper => mapper.Map(It.IsAny<ConversationProviderPayload>()))
            .Returns(new ConversationResponse(
                null,
                [new ConversationToolCall("call_1", "directory_list", "{}")],
                "resp_456"));

        Mock<IToolExecutionPipeline> toolExecutionPipeline = new(MockBehavior.Strict);
        toolExecutionPipeline
            .Setup(pipeline => pipeline.ExecuteAsync(
                It.Is<IReadOnlyList<ConversationToolCall>>(calls => calls.Count == 1 && calls[0].Name == "directory_list"),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolExecutionBatchResult([
                new ToolInvocationResult(
                    "call_1",
                    "directory_list",
                    ToolResultFactory.Success(
                        "Listed directory '.'.",
                        new ToolErrorPayload("ok", "ok"),
                        ToolJsonContext.Default.ToolErrorPayload,
                        new ToolRenderPayload("Directory listing: .", "file: README.md")))
            ]));

        AgentConversationPipeline sut = CreateSut(
            secretStore.Object,
            providerClient.Object,
            responseMapper.Object,
            toolExecutionPipeline.Object,
            toolRegistry.Object,
            configurationAccessor.Object);

        ConversationTurnResult result = await sut.ProcessAsync(
            "Which models can I use?",
            Session,
            CancellationToken.None);

        result.Kind.Should().Be(ConversationTurnResultKind.ToolExecution);
        result.ResponseText.Should().Contain("Directory listing");
        result.ToolExecutionResult.Should().NotBeNull();
        result.ToolExecutionResult!.Results.Should().ContainSingle();
        result.ToolExecutionResult.Results[0].ToolName.Should().Be("directory_list");
    }

    [Fact]
    public async Task ProcessAsync_Should_ThrowConversationPipelineException_When_ApiKeyIsMissing()
    {
        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        AgentConversationPipeline sut = CreateSut(
            secretStore.Object,
            Mock.Of<IConversationProviderClient>(),
            Mock.Of<IConversationResponseMapper>(),
            Mock.Of<IToolExecutionPipeline>(),
            Mock.Of<IToolRegistry>(),
            Mock.Of<IConversationConfigurationAccessor>());

        Func<Task> action = () => sut.ProcessAsync("hello", Session, CancellationToken.None);

        await action.Should().ThrowAsync<ConversationPipelineException>()
            .WithMessage("*API key is missing*");
    }

    [Fact]
    public async Task ProcessAsync_Should_PropagateConversationProviderException_When_ProviderFails()
    {
        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IConversationConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ConversationSettings(null, TimeSpan.FromSeconds(30)));

        Mock<IToolRegistry> toolRegistry = new(MockBehavior.Strict);
        toolRegistry
            .Setup(registry => registry.GetToolDefinitions())
            .Returns([
                CreateToolDefinition("shell_command")
            ]);

        Mock<IConversationProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.SendAsync(
                It.IsAny<ConversationProviderRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConversationProviderException("Provider unavailable."));

        AgentConversationPipeline sut = CreateSut(
            secretStore.Object,
            providerClient.Object,
            Mock.Of<IConversationResponseMapper>(),
            Mock.Of<IToolExecutionPipeline>(),
            toolRegistry.Object,
            configurationAccessor.Object);

        Func<Task> action = () => sut.ProcessAsync("hello", Session, CancellationToken.None);

        await action.Should().ThrowAsync<ConversationProviderException>()
            .WithMessage("Provider unavailable.");
    }

    private static AgentConversationPipeline CreateSut(
        IApiKeySecretStore secretStore,
        IConversationProviderClient providerClient,
        IConversationResponseMapper responseMapper,
        IToolExecutionPipeline toolExecutionPipeline,
        IToolRegistry toolRegistry,
        IConversationConfigurationAccessor configurationAccessor)
    {
        return new AgentConversationPipeline(
            secretStore,
            providerClient,
            responseMapper,
            toolExecutionPipeline,
            toolRegistry,
            configurationAccessor,
            NullLogger<AgentConversationPipeline>.Instance);
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
}
