using System.Reflection;
using Umbraco.Cms.Core.Manifest;

namespace Umbraco.Community.Sanitiser;

internal class SanitiserManifestFilter : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        Assembly assembly = typeof(SanitiserManifestFilter).Assembly;

        manifests.Add(new PackageManifest
        {
            PackageName = "Umbraco.Community.Sanitiser",
            Version = assembly.GetName()?.Version?.ToString(3) ?? "0.1.0",
            AllowPackageTelemetry = false
        });
    }
}
