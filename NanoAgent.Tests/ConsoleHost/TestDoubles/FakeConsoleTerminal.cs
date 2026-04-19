using System.Text;
using NanoAgent.ConsoleHost.Terminal;

namespace NanoAgent.Tests.ConsoleHost.TestDoubles;

internal sealed class FakeConsoleTerminal : IConsoleTerminal
{
    private readonly Queue<ConsoleKeyInfo> _keyQueue = new();
    private readonly Queue<string?> _lineQueue = new();

    public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;

    public int CursorTop { get; private set; }

    public ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;

    public bool IsInputRedirected { get; set; }

    public bool IsOutputRedirected { get; set; }

    public string Output => _outputBuilder.ToString();

    public int WindowWidth { get; set; } = 120;

    private readonly StringBuilder _outputBuilder = new();

    public void EnqueueKey(ConsoleKeyInfo keyInfo)
    {
        _keyQueue.Enqueue(keyInfo);
    }

    public void EnqueueLine(string? input)
    {
        _lineQueue.Enqueue(input);
    }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (_keyQueue.Count == 0)
        {
            throw new InvalidOperationException("No queued key input is available.");
        }

        return _keyQueue.Dequeue();
    }

    public string? ReadLine()
    {
        if (_lineQueue.Count == 0)
        {
            throw new InvalidOperationException("No queued line input is available.");
        }

        return _lineQueue.Dequeue();
    }

    public void ResetColor()
    {
    }

    public void SetCursorPosition(int left, int top)
    {
        CursorTop = top;
    }

    public void Write(string value)
    {
        _outputBuilder.Append(value);
    }

    public void WriteLine()
    {
        _outputBuilder.AppendLine();
        CursorTop++;
    }

    public void WriteLine(string value)
    {
        _outputBuilder.AppendLine(value);
        CursorTop++;
    }
}
