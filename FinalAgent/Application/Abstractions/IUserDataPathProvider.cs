namespace FinalAgent.Application.Abstractions;

public interface IUserDataPathProvider
{
    string GetConfigurationFilePath();

    string GetSecretFilePath();
}
