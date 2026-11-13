using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Sanitiser.sanitisers;

public abstract class DirectorySanitiser : ISanitiser
{
    public async Task Sanitise(SanitisationContext context)
    {
        // Resolve and validate up front so a misconfigured path is caught in dry run too.
        var directory = ResolveDirectoryWithinSite(GetDirectoryPath(), context.ContentRootPath);

        if (context.DryRun)
        {
            context.Logger.LogInformation("[DRY RUN] Would delete all files and subdirectories in {directory}.", directory);
            return;
        }

        await EmptyDirectory(directory, context.CancellationToken);
    }

    public abstract bool IsEnabled();

    protected abstract string GetDirectoryPath();

    /// <summary>
    /// Resolves the target directory and refuses to proceed unless it is strictly inside the site content
    /// root, so a misconfigured path can never empty an arbitrary or system directory.
    /// </summary>
    private static string ResolveDirectoryWithinSite(string directory, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                "DirectorySanitiser: GetDirectoryPath() returned an empty path.");
        }

        if (string.IsNullOrWhiteSpace(contentRootPath))
        {
            throw new InvalidOperationException("DirectorySanitiser: the content root path is not available.");
        }

        // Note: GetFullPath resolves the path lexically (it does not follow symbolic links), so the reparse-point
        // check below guards against the target, or any folder on the way to it, being a link that points outside
        // the content root.
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentRootPath));
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory, root));

        if (fullPath.Equals(root, StringComparison.Ordinal)
            || !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"DirectorySanitiser: refusing to empty '{fullPath}' because it is not inside the site content root '{root}'.");
        }

        // Check every component below the content root, not just the target: a symlinked parent folder (for
        // example wwwroot/media pointing elsewhere) would otherwise let a lexically-inside path empty a directory
        // outside the site. The root itself and its own ancestors are deliberately not checked, since a deploy may
        // legitimately sit behind a symlink (including the temp directory on macOS).
        var current = root;
        foreach (var segment in Path.GetRelativePath(root, fullPath).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);

            if (Directory.Exists(current) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    $"DirectorySanitiser: refusing to empty '{fullPath}' because '{current}' is a symbolic link, which could resolve outside the site content root.");
            }
        }

        return fullPath;
    }

    private static void RemoveDirectoriesInDirectory(string? directory, CancellationToken cancellationToken)
    {
        if (Directory.Exists(directory))
        {
            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.Delete(subDirectory, true);
            }
        }
    }

    private static void RemoveFilesInDirectory(string? directory, CancellationToken cancellationToken)
    {
        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Delete(file);
            }
        }
    }

    private static Task EmptyDirectory(string? directory, CancellationToken cancellationToken)
    {
        RemoveDirectoriesInDirectory(directory, cancellationToken);
        RemoveFilesInDirectory(directory, cancellationToken);
        return Task.CompletedTask;
    }
}
