using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Conversation.Services;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
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

        Mock<IConversationProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.SendAsync(
                It.IsAny<ConversationProviderRequest>(),
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
                [new ConversationToolCall("call_1", "list_models", "{}")],
                "resp_456"));

        Mock<IToolExecutionPipeline> toolExecutionPipeline = new(MockBehavior.Strict);
        toolExecutionPipeline
            .Setup(pipeline => pipeline.ExecuteAsync(
                It.Is<IReadOnlyList<ConversationToolCall>>(calls => calls.Count == 1 && calls[0].Name == "list_models"),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConversationTurnResult.ToolExecution("Available models:\n- gpt-5-mini (active)"));

        AgentConversationPipeline sut = CreateSut(
            secretStore.Object,
            providerClient.Object,
            responseMapper.Object,
            toolExecutionPipeline.Object,
            configurationAccessor.Object);

        ConversationTurnResult result = await sut.ProcessAsync(
            "Which models can I use?",
            Session,
            CancellationToken.None);

        result.Kind.Should().Be(ConversationTurnResultKind.ToolExecution);
        result.ResponseText.Should().Contain("Available models");
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
        IConversationConfigurationAccessor configurationAccessor)
    {
        return new AgentConversationPipeline(
            secretStore,
            providerClient,
            responseMapper,
            toolExecutionPipeline,
            configurationAccessor,
            NullLogger<AgentConversationPipeline>.Instance);
    }
}
