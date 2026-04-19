namespace NanoAgent.Application.Abstractions;

public interface ITokenEstimator
{
    int Estimate(string text);
}
