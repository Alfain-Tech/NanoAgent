using FinalAgent.Infrastructure.Configuration;
using FinalAgent.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FinalAgent.Tests.Infrastructure.Storage;

public sealed class UserDataPathProviderTests
{
    [Fact]
    public void GetLogsDirectoryPath_Should_ReturnStorageDirectoryLogsPath()
    {
        UserDataPathProvider sut = new(Options.Create(new ApplicationOptions
        {
            ProductName = "FinalAgent",
            StorageDirectoryName = "FinalAgent"
        }));

        string logsDirectoryPath = sut.GetLogsDirectoryPath();

        Path.GetFileName(logsDirectoryPath).Should().Be("logs");
        logsDirectoryPath.Should().Contain("FinalAgent");
    }
}
