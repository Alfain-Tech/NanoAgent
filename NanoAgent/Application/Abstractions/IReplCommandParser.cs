using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IReplCommandParser
{
    ParsedReplCommand Parse(string commandText);
}
