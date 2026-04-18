using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Services;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinalAgent.Tests.Application.Services;

public sealed class FirstRunOnboardingServiceTests
{
    [Fact]
    public async Task EnsureOnboardedAsync_Should_SkipPrompts_When_ConfigurationAndSecretAlreadyExist()
    {
        AgentProviderProfile existingProfile = new(ProviderKind.OpenAi, null);

        Mock<IUserPrompt> userPrompt = new(MockBehavior.Strict);
        Mock<IOnboardingInputValidator> inputValidator = new(MockBehavior.Strict);
        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingProfile);
        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync("existing-key");
        Mock<IAgentProviderProfileFactory> profileFactory = new(MockBehavior.Strict);

        FirstRunOnboardingService sut = CreateSut(
            userPrompt.Object,
            inputValidator.Object,
            configurationStore.Object,
            secretStore.Object,
            profileFactory.Object);

        OnboardingResult result = await sut.EnsureOnboardedAsync(CancellationToken.None);

        result.Should().Be(new OnboardingResult(existingProfile, false));
        userPrompt.VerifyNoOtherCalls();
        configurationStore.Verify(store => store.SaveAsync(It.IsAny<AgentProviderProfile>(), It.IsAny<CancellationToken>()), Times.Never);
        secretStore.Verify(store => store.SaveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnsureOnboardedAsync_Should_SaveOpenAiConfiguration_When_OpenAiIsSelected()
    {
        AgentProviderProfile openAiProfile = new(ProviderKind.OpenAi, null);

        Mock<IUserPrompt> userPrompt = new(MockBehavior.Strict);
        userPrompt.Setup(prompt => prompt.ShowMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        userPrompt.Setup(prompt => prompt.PromptSelectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        userPrompt.Setup(prompt => prompt.PromptSecretAsync("API key:", It.IsAny<CancellationToken>())).ReturnsAsync("  sk-openai  ");

        Mock<IOnboardingInputValidator> inputValidator = new(MockBehavior.Strict);
        inputValidator.Setup(validator => validator.ValidateApiKey("  sk-openai  ")).Returns(InputValidationResult.Success("sk-openai"));

        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AgentProviderProfile?)null);
        configurationStore.Setup(store => store.SaveAsync(openAiProfile, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        secretStore.Setup(store => store.SaveAsync("sk-openai", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAgentProviderProfileFactory> profileFactory = new(MockBehavior.Strict);
        profileFactory.Setup(factory => factory.CreateOpenAi()).Returns(openAiProfile);

        FirstRunOnboardingService sut = CreateSut(
            userPrompt.Object,
            inputValidator.Object,
            configurationStore.Object,
            secretStore.Object,
            profileFactory.Object);

        OnboardingResult result = await sut.EnsureOnboardedAsync(CancellationToken.None);

        result.Should().Be(new OnboardingResult(openAiProfile, true));
        profileFactory.Verify(factory => factory.CreateOpenAi(), Times.Once);
        userPrompt.Verify(prompt => prompt.PromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        configurationStore.VerifyAll();
        secretStore.VerifyAll();
    }

    [Fact]
    public async Task EnsureOnboardedAsync_Should_RePromptBaseUrl_When_InputIsInvalidForCompatibleProvider()
    {
        AgentProviderProfile compatibleProfile = new(ProviderKind.OpenAiCompatible, "https://compatible.example.com/v1");

        Mock<IUserPrompt> userPrompt = new(MockBehavior.Strict);
        userPrompt.Setup(prompt => prompt.ShowMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        userPrompt.Setup(prompt => prompt.PromptSelectionAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        userPrompt.SetupSequence(prompt => prompt.PromptAsync("Base URL:", It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-a-url")
            .ReturnsAsync("https://compatible.example.com/v1/");
        userPrompt.Setup(prompt => prompt.PromptSecretAsync("API key:", It.IsAny<CancellationToken>())).ReturnsAsync("compatible-key");

        Mock<IOnboardingInputValidator> inputValidator = new(MockBehavior.Strict);
        inputValidator.SetupSequence(validator => validator.ValidateBaseUrl(It.IsAny<string?>()))
            .Returns(InputValidationResult.Failure("Base URL must be an absolute URL."))
            .Returns(InputValidationResult.Success("https://compatible.example.com/v1"));
        inputValidator.Setup(validator => validator.ValidateApiKey("compatible-key"))
            .Returns(InputValidationResult.Success("compatible-key"));

        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AgentProviderProfile?)null);
        configurationStore.Setup(store => store.SaveAsync(compatibleProfile, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore.Setup(store => store.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        secretStore.Setup(store => store.SaveAsync("compatible-key", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAgentProviderProfileFactory> profileFactory = new(MockBehavior.Strict);
        profileFactory
            .Setup(factory => factory.CreateCompatible("https://compatible.example.com/v1"))
            .Returns(compatibleProfile);

        FirstRunOnboardingService sut = CreateSut(
            userPrompt.Object,
            inputValidator.Object,
            configurationStore.Object,
            secretStore.Object,
            profileFactory.Object);

        OnboardingResult result = await sut.EnsureOnboardedAsync(CancellationToken.None);

        result.Should().Be(new OnboardingResult(compatibleProfile, true));
        userPrompt.Verify(prompt => prompt.ShowMessageAsync("Base URL must be an absolute URL.", It.IsAny<CancellationToken>()), Times.Once);
        profileFactory.Verify(factory => factory.CreateCompatible("https://compatible.example.com/v1"), Times.Once);
    }

    private static FirstRunOnboardingService CreateSut(
        IUserPrompt userPrompt,
        IOnboardingInputValidator inputValidator,
        IAgentConfigurationStore configurationStore,
        IApiKeySecretStore secretStore,
        IAgentProviderProfileFactory profileFactory)
    {
        return new FirstRunOnboardingService(
            userPrompt,
            inputValidator,
            configurationStore,
            secretStore,
            profileFactory,
            NullLogger<FirstRunOnboardingService>.Instance);
    }
}
