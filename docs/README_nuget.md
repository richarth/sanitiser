# Sanitiser

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.Sanitiser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.Sanitiser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.Sanitiser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.Sanitiser)
[![GitHub license](https://img.shields.io/github/license/richarth/sanitiser?color=8AB803)](https://github.com/richarth/sanitiser/blob/main/LICENSE)

When enabled, this package will automatically remove personal data from your Umbraco website on startup.

Out of the box the package will delete member data.

Umbraco versions supported: v13 (on .NET 8), v15/v16 (on .NET 9), and v17/v18 (on .NET 10). A single package version supports them all.

`Umbraco.Community.Sanitiser` is the main package and contains the built-in user and member sanitisers; it
depends on `Umbraco.Community.Sanitiser.Core` (interfaces and orchestration). Optional replacer packages
(`Umbraco.Community.Sanitiser.Faker` and `Umbraco.Community.Sanitiser.AI`) can be added to generate more
lifelike replacement data.
