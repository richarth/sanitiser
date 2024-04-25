using System.Reflection;
using Umbraco.Cms.Core.Manifest;

namespace Umbraco.Community.Sanitiser.Forms;

internal class SanitiserFormsManifestFilter : IManifestFilter
{
    public void Filter(List<PackageManifest> manifests)
    {
        Assembly assembly = typeof(SanitiserFormsManifestFilter).Assembly;

        manifests.Add(new PackageManifest
        {
            PackageName = "Umbraco.Community.Sanitiser.Forms",
            Version = assembly.GetName()?.Version?.ToString(3) ?? "0.1.0",
            AllowPackageTelemetry = false
        });
    }
}
