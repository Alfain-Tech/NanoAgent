using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Repl.Services;
using NanoAgent.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace NanoAgent.Tests.Application.Repl.Services;

public sealed class ReplRuntimeTests
{
    private static readonly ReplSessionContext Session = new(
        "NanoAgent",
        new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
        "gpt-oss-20b",
        ["gpt-oss-20b", "qwen/qwen3-coder-30b"]);

    [Fact]
    public async Task RunAsync_Should_DispatchSlashCommand_When_InputStartsWithSlash()
    {
        QueueReplInputReader inputReader = new("/help", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand helpCommand = new("/help", "help", string.Empty, []);
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/help"))
            .Returns(helpCommand);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .SetupSequence(dispatcher => dispatcher.DispatchAsync(
                It.IsAny<ParsedReplCommand>(),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Continue("Available commands"))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(helpCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
        conversationPipeline.VerifyNoOtherCalls();
        outputWriter.HeaderMessages.Should().ContainSingle()
            .Which.Should().Be("NanoAgent|gpt-oss-20b");
        outputWriter.InfoMessages.Should().Contain("Available commands");
    }

    [Fact]
    public async Task RunAsync_Should_DispatchSlashCommand_When_FirstRedirectedLineContainsUtf8Bom()
    {
        QueueReplInputReader inputReader = new("\uFEFF/help", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand helpCommand = new("/help", "help", string.Empty, []);
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/help"))
            .Returns(helpCommand);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .SetupSequence(dispatcher => dispatcher.DispatchAsync(
                It.IsAny<ParsedReplCommand>(),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Continue("Available commands"))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(helpCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
        conversationPipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_Should_DispatchSlashCommand_When_FirstRedirectedLineContainsUtf8BomMojibakePrefix()
    {
        QueueReplInputReader inputReader = new("\u00EF\u00BB\u00BF/help", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand helpCommand = new("/help", "help", string.Empty, []);
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/help"))
            .Returns(helpCommand);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .SetupSequence(dispatcher => dispatcher.DispatchAsync(
                It.IsAny<ParsedReplCommand>(),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Continue("Available commands"))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(helpCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
        conversationPipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_Should_IgnoreWhitespaceOnlyInput_When_LineContainsNoContent()
    {
        QueueReplInputReader inputReader = new("   ", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
        conversationPipeline.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_Should_SendNonCommandInputToConversationPipeline_When_LineDoesNotStartWithSlash()
    {
        QueueReplInputReader inputReader = new("help me plan this change", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);
        conversationPipeline
            .Setup(pipeline => pipeline.ProcessAsync(
                "help me plan this change",
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationTurnResult("Response ready"));

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        conversationPipeline.Verify(pipeline => pipeline.ProcessAsync(
            "help me plan this change",
            Session,
            It.IsAny<CancellationToken>()), Times.Once);
        outputWriter.Responses.Should().ContainSingle().Which.Should().Be("Response ready");
    }

    [Fact]
    public async Task RunAsync_Should_ContinueAfterCommandError_When_DispatcherThrows()
    {
        QueueReplInputReader inputReader = new("/help", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand helpCommand = new("/help", "help", string.Empty, []);
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/help"))
            .Returns(helpCommand);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .SetupSequence(dispatcher => dispatcher.DispatchAsync(
                It.IsAny<ParsedReplCommand>(),
                Session,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        outputWriter.ErrorMessages.Should().ContainSingle(message =>
            message.Contains("command failed unexpectedly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_Should_ContinueAfterConversationError_When_PipelineThrows()
    {
        QueueReplInputReader inputReader = new("hello", "/exit");
        RecordingReplOutputWriter outputWriter = new();
        ParsedReplCommand exitCommand = new("/exit", "exit", string.Empty, []);

        Mock<IReplCommandParser> commandParser = new(MockBehavior.Strict);
        commandParser
            .Setup(parser => parser.Parse("/exit"))
            .Returns(exitCommand);

        Mock<IReplCommandDispatcher> commandDispatcher = new(MockBehavior.Strict);
        commandDispatcher
            .Setup(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReplCommandResult.Exit());

        Mock<IConversationPipeline> conversationPipeline = new(MockBehavior.Strict);
        conversationPipeline
            .Setup(pipeline => pipeline.ProcessAsync("hello", Session, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        ReplRuntime sut = CreateSut(
            inputReader,
            outputWriter,
            commandParser.Object,
            commandDispatcher.Object,
            conversationPipeline.Object);

        await sut.RunAsync(Session, CancellationToken.None);

        outputWriter.ErrorMessages.Should().ContainSingle(message =>
            message.Contains("conversation pipeline failed unexpectedly", StringComparison.OrdinalIgnoreCase));
        commandDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(exitCommand, Session, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ReplRuntime CreateSut(
        IReplInputReader inputReader,
        IReplOutputWriter outputWriter,
        IReplCommandParser commandParser,
        IReplCommandDispatcher commandDispatcher,
        IConversationPipeline conversationPipeline)
    {
        return new ReplRuntime(
            inputReader,
            outputWriter,
            commandParser,
            commandDispatcher,
            conversationPipeline,
            NullLogger<ReplRuntime>.Instance);
    }

    private sealed class QueueReplInputReader : IReplInputReader
    {
        private readonly Queue<string?> _inputs;

        public QueueReplInputReader(params string?[] inputs)
        {
            _inputs = new Queue<string?>(inputs);
        }

        public Task<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_inputs.Count == 0 ? null : _inputs.Dequeue());
        }
    }

    private sealed class RecordingReplOutputWriter : IReplOutputWriter
    {
        public List<string> ErrorMessages { get; } = [];

        public List<string> HeaderMessages { get; } = [];

        public List<string> InfoMessages { get; } = [];

        public List<string> Responses { get; } = [];

        public List<string> WarningMessages { get; } = [];

        public Task WriteErrorAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ErrorMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task WriteShellHeaderAsync(
            string applicationName,
            string modelName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HeaderMessages.Add($"{applicationName}|{modelName}");
            return Task.CompletedTask;
        }

        public Task WriteInfoAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InfoMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task WriteWarningAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WarningMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task WriteResponseAsync(string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Responses.Add(message);
            return Task.CompletedTask;
        }
    }
}
