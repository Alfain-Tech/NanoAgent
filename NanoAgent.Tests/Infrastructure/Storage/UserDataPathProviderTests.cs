using NanoAgent.Infrastructure.Storage;
using FluentAssertions;

namespace NanoAgent.Tests.Infrastructure.Storage;

public sealed class UserDataPathProviderTests
{
    [Fact]
    public void GetLogsDirectoryPath_Should_ReturnStorageDirectoryLogsPath()
    {
        UserDataPathProvider sut = new();

        string logsDirectoryPath = sut.GetLogsDirectoryPath();

        Path.GetFileName(logsDirectoryPath).Should().Be("logs");
        logsDirectoryPath.Should().Contain("NanoAgent");
    }

    [Fact]
    public void GetSectionsDirectoryPath_Should_ReturnStorageDirectorySectionsPath()
    {
        UserDataPathProvider sut = new();

        string sectionsDirectoryPath = sut.GetSectionsDirectoryPath();

        Path.GetFileName(sectionsDirectoryPath).Should().Be("sections");
        sectionsDirectoryPath.Should().Contain("NanoAgent");
    }
}
