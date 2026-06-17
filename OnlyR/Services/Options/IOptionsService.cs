namespace OnlyR.Services.Options;

public interface IOptionsService
{
    Options Options { get; }

    void Save();
}