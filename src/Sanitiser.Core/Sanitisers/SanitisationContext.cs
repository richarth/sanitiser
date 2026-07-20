using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Sanitiser.sanitisers;

/// <summary>
/// Passed to every <see cref="ISanitiser"/> for a sanitisation run. When <see cref="DryRun"/> is true a
/// sanitiser must make no changes and should log — via <see cref="Logger"/> — what it would have done.
/// <see cref="ContentRootPath"/> is the application's content root; directory-based sanitisers use it to
/// refuse to delete anything outside the site. <see cref="CancellationToken"/> is signalled if the host is
/// shutting down; long-running sanitisers should honour it.
/// </summary>
public sealed record SanitisationContext(
    bool DryRun,
    ILogger Logger,
    string ContentRootPath = "",
    CancellationToken CancellationToken = default);
