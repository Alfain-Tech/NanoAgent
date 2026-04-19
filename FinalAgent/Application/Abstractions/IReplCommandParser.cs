using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IReplCommandParser
{
    ParsedReplCommand Parse(string commandText);
}
