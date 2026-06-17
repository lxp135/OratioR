using OnlyR.Model;

namespace OnlyR.Services.Config;

public interface IConfigRepository
{
    AppConfig Config { get; }

    void Load();

    void Save();
}
