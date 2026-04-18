namespace FinalAgent.Application.Abstractions;

public interface IGreetingSink
{
    ValueTask WriteAsync(string message, CancellationToken cancellationToken);
}
