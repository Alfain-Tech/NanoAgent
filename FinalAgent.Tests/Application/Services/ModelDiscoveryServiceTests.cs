using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Services;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;
using FinalAgent.Domain.Services;
using FinalAgent.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinalAgent.Tests.Application.Services;

public sealed class ModelDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAndSelectAsync_Should_UseConfiguredDefault_When_ItMatchesFetchedModels()
    {
        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConfiguration(
                new AgentProviderProfile(ProviderKind.OpenAi, null),
                "gpt-5"));

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IModelProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.GetAvailableModelsAsync(
                It.IsAny<AgentProviderProfile>(),
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AvailableModel("gpt-5-mini"),
                new AvailableModel("gpt-5"),
                new AvailableModel("gpt-5")
            ]);

        Mock<IModelSelectionConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ModelSelectionSettings(TimeSpan.FromMinutes(5)));

        ModelDiscoveryService sut = CreateSut(
            configurationStore.Object,
            secretStore.Object,
            providerClient.Object,
            new InMemoryModelCache(),
            new ConfiguredOrFirstModelSelectionPolicy(),
            configurationAccessor.Object);

        ModelDiscoveryResult result = await sut.DiscoverAndSelectAsync(CancellationToken.None);

        result.SelectedModelId.Should().Be("gpt-5");
        result.SelectionSource.Should().Be(ModelSelectionSource.ConfiguredDefault);
        result.ConfiguredDefaultStatus.Should().Be(ConfiguredDefaultModelStatus.Matched);
        result.HadDuplicateModelIds.Should().BeTrue();
        result.AvailableModels.Select(model => model.Id).Should().Equal("gpt-5-mini", "gpt-5");
        configurationStore.Verify(store => store.SaveAsync(It.IsAny<AgentConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DiscoverAndSelectAsync_Should_UseFirstReturnedModel_When_ConfiguredDefaultIsNotReturned()
    {
        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConfiguration(
                new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
                "gpt-5"));
        configurationStore
            .Setup(store => store.SaveAsync(
                new AgentConfiguration(
                    new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
                    "gpt-4.1"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IModelProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.GetAvailableModelsAsync(
                It.IsAny<AgentProviderProfile>(),
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AvailableModel("gpt-4.1"),
                new AvailableModel("gpt-4.1-mini")
            ]);

        Mock<IModelSelectionConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ModelSelectionSettings(TimeSpan.FromMinutes(5)));

        ModelDiscoveryService sut = CreateSut(
            configurationStore.Object,
            secretStore.Object,
            providerClient.Object,
            new InMemoryModelCache(),
            new ConfiguredOrFirstModelSelectionPolicy(),
            configurationAccessor.Object);

        ModelDiscoveryResult result = await sut.DiscoverAndSelectAsync(CancellationToken.None);

        result.SelectedModelId.Should().Be("gpt-4.1");
        result.SelectionSource.Should().Be(ModelSelectionSource.FirstReturnedModel);
        result.ConfiguredDefaultStatus.Should().Be(ConfiguredDefaultModelStatus.NotFound);
        result.ConfiguredDefaultModel.Should().Be("gpt-5");
    }

    [Fact]
    public async Task DiscoverAndSelectAsync_Should_UseCachedModels_When_CalledRepeatedly()
    {
        int providerCallCount = 0;

        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConfiguration(
                new AgentProviderProfile(ProviderKind.OpenAi, null),
                null));
        configurationStore
            .Setup(store => store.SaveAsync(
                new AgentConfiguration(
                    new AgentProviderProfile(ProviderKind.OpenAi, null),
                    "gpt-5-mini"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IModelProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.GetAvailableModelsAsync(
                It.IsAny<AgentProviderProfile>(),
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                providerCallCount++;
                return [
                    new AvailableModel("gpt-5-mini"),
                    new AvailableModel("gpt-4.1")
                ];
            });

        Mock<IModelSelectionConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ModelSelectionSettings(TimeSpan.FromMinutes(5)));

        ModelDiscoveryService sut = CreateSut(
            configurationStore.Object,
            secretStore.Object,
            providerClient.Object,
            new InMemoryModelCache(),
            new ConfiguredOrFirstModelSelectionPolicy(),
            configurationAccessor.Object);

        await sut.DiscoverAndSelectAsync(CancellationToken.None);
        await sut.DiscoverAndSelectAsync(CancellationToken.None);

        providerCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DiscoverAndSelectAsync_Should_ThrowModelDiscoveryException_When_ProviderReturnsNoUsableModels()
    {
        Mock<IAgentConfigurationStore> configurationStore = new(MockBehavior.Strict);
        configurationStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentConfiguration(
                new AgentProviderProfile(ProviderKind.OpenAi, null),
                null));

        Mock<IApiKeySecretStore> secretStore = new(MockBehavior.Strict);
        secretStore
            .Setup(store => store.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-key");

        Mock<IModelProviderClient> providerClient = new(MockBehavior.Strict);
        providerClient
            .Setup(client => client.GetAvailableModelsAsync(
                It.IsAny<AgentProviderProfile>(),
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AvailableModel(" "),
                new AvailableModel("")
            ]);

        Mock<IModelSelectionConfigurationAccessor> configurationAccessor = new(MockBehavior.Strict);
        configurationAccessor
            .Setup(accessor => accessor.GetSettings())
            .Returns(new ModelSelectionSettings(TimeSpan.FromMinutes(5)));

        ModelDiscoveryService sut = CreateSut(
            configurationStore.Object,
            secretStore.Object,
            providerClient.Object,
            new InMemoryModelCache(),
            new ConfiguredOrFirstModelSelectionPolicy(),
            configurationAccessor.Object);

        Func<Task> action = () => sut.DiscoverAndSelectAsync(CancellationToken.None);

        await action.Should().ThrowAsync<ModelDiscoveryException>()
            .WithMessage("*no usable models*");
    }

    private static ModelDiscoveryService CreateSut(
        IAgentConfigurationStore configurationStore,
        IApiKeySecretStore secretStore,
        IModelProviderClient providerClient,
        IModelCache modelCache,
        IModelSelectionPolicy selectionPolicy,
        IModelSelectionConfigurationAccessor configurationAccessor)
    {
        return new ModelDiscoveryService(
            configurationStore,
            secretStore,
            providerClient,
            modelCache,
            selectionPolicy,
            configurationAccessor,
            NullLogger<ModelDiscoveryService>.Instance);
    }
}
