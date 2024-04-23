# Sanitiser

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.Sanitiser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.Sanitiser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.Sanitiser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.Sanitiser)
[![GitHub license](https://img.shields.io/github/license/richarth/sanitiser?color=8AB803)](https://github.com/richarth/sanitiser/blob/main/LICENSE)

When enabled, this package will automatically remove personal data from your Umbraco website on startup.

Out of the box the package will delete member data and form submissions.

Umbraco versions supported: v10.8.5+

## Installation

Add the package to your Umbraco website from nuget:

`dotnet add package Umbraco.Community.Sanitiser`

## Configuration

To enable the package, add the following to your `appsettings.json`:

```json
{
  "Sanitiser": {
    "Enabled": true
  }
}
```

### Member Data

To enable the deletion of member data, add the following to your `appsettings.json`:

```json
{
    "Sanitiser": {
        "Enabled": true,
        "MembersSanitiser": {
            "Enable": true
        }
    }
}
```

To exclude members whose email addresses belong to specific domains from deletion, add the following to
your `appsettings.json`:

```json
{
    "Sanitiser": {
        "Enabled": true,
        "MembersSanitiser": {
            "Enable": true,
            "DomainsToExclude": "test.com,example.com"
        }
    }
}
```

### Umbraco Forms Submissions

To enable the deletion of form submissions, add the following to your `appsettings.json`:

```json
{
    "Sanitiser": {
        "Enabled": true,
        "UmbracoFormsSanitiser": {
            "Enable": true
        }
    }
}
```

### Custom database tables

To enable the deletion of data from custom database tables, you can extend the `DatabaseTableSanitiser` class and create
a poco with a table name attribute.

For example, to have a table called `test` automatically emptied on startup, create a poco like this:

```csharp
using NPoco;

[TableName("test")]
public class Test;
```

And extend the `DatabaseTableSanitiser` class like this with your poco class as a type parameter:

```csharp
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.Sanitiser.Models;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class TestTableSanitiser(IScopeProvider scopeProvider) : DatabaseTableSanitiser<Test>(scopeProvider)
{
    public override bool IsEnabled()
    {
        return true;
    }
}
```

> [!WARNING]
> N.B. This package is not intended to be run on production sites, only enable sanitization on a development or staging
environment. Before enabling please ensure you have a backup of your data and a backup of your backup.
>
>If there is a lot of data then the startup of your site may be delayed. Only enable when necessary.

## Customisation

To add your own sanitization logic, implement the `ISanitiser` interface. Your sanitization logic will be run
automatically on startup when the sanitization service and your sanitizer are enabled.

Add your logic to the `Sanitise` method.

You will also need to implement the enabled check in the `IsEnabled` method. You could check for a value in Umbraco,
simply return true or more likely add a setting to `appsettings.json`.

If adding your own setting to the `Sanitiser` section of `appsettings.json`, you can add a new class which extends
the `SanitiserOptions` class to include your new setting. The values from your `appsettings.json` will be automatically
mapped to your new class.

## Acknowledgements

### Logo

The package logo uses the [Sanitiser](https://thenounproject.com/icon/sanitiser-6216442/) (by [Manish Mittal](https://thenounproject.com/creator/butterfingers/)) icon from
the [Noun Project](https://thenounproject.com), licensed
under [CC BY 3.0 US](https://creativecommons.org/licenses/by/3.0/us/).

## License

MIT License
