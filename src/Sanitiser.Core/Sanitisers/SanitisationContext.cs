using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Sanitiser.sanitisers;

/// <summary>
/// Passed to every <see cref="ISanitiser"/> for a sanitisation run. When <see cref="DryRun"/> is true a
/// sanitiser must make no changes and should log — via <see cref="Logger"/> — what it would have done.
/// </summary>
public sealed record SanitisationContext(bool DryRun, ILogger Logger);
