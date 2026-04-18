using System.Text;
using FinalAgent.Application.Abstractions;

namespace FinalAgent.ConsoleHost.Prompts;

internal sealed class ConsoleUserPrompt : IUserPrompt
{
    public Task ShowMessageAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public Task<string> PromptAsync(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.Write($"{prompt} ");

        string? input = Console.ReadLine();
        return Task.FromResult(input ?? string.Empty);
    }

    public Task<string> PromptSecretAsync(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.Write($"{prompt} ");

        if (Console.IsInputRedirected)
        {
            string? redirectedInput = Console.ReadLine();
            return Task.FromResult(redirectedInput ?? string.Empty);
        }

        StringBuilder builder = new();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return Task.FromResult(builder.ToString());
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                }

                continue;
            }

            if (!char.IsControl(keyInfo.KeyChar))
            {
                builder.Append(keyInfo.KeyChar);
            }
        }
    }

    public async Task<int> PromptSelectionAsync(
        string prompt,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (options.Count == 0)
        {
            throw new ArgumentException("At least one option must be provided.", nameof(options));
        }

        Console.WriteLine(prompt);
        for (int index = 0; index < options.Count; index++)
        {
            Console.WriteLine($"{index + 1}. {options[index]}");
        }

        while (true)
        {
            string input = await PromptAsync($"Enter selection [1-{options.Count}]:", cancellationToken);
            if (int.TryParse(input, out int selectedValue) &&
                selectedValue >= 1 &&
                selectedValue <= options.Count)
            {
                return selectedValue - 1;
            }

            Console.WriteLine($"Please enter a number between 1 and {options.Count}.");
        }
    }
}
