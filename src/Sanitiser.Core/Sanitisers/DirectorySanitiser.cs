namespace Umbraco.Community.Sanitiser.sanitisers;

public abstract class DirectorySanitiser : ISanitiser
{
    public async Task Sanitise()
    {
        await EmptyDirectory(GetDirectoryPath());
    }

    public abstract bool IsEnabled();

    protected abstract string GetDirectoryPath();

    private static Task RemoveDirectoriesInDirectory(string? directory)
    {
        if (Directory.Exists(directory))
        {
            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                Directory.Delete(subDirectory, true);
            }
        }

        return Task.CompletedTask;
    }

    private static Task RemoveFilesInDirectory(string? directory)
    {
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task EmptyDirectory(string? directory)
    {
        await RemoveDirectoriesInDirectory(directory);
        await RemoveFilesInDirectory(directory);
    }
}
