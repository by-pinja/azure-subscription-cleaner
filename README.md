# Azure Subscription Cleaner

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/azure-subscription-cleaner/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Fazure-subscription-cleaner/activity)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=protacon/azure-subscription-cleaner&identifier=204444972)](https://dependabot.com)

This is still WIP

Performs cleaning operations for Azure Subscription. This is used to removed unused items from development subscription.
To opt out from this, lock your resouce group.

## Build

These commands are assumed to be executed from the same directory this file exists.
These commands can also be executed from project directories, but
the directory/project needs to be removed from the command. See `dotnet`
documentation from more information.

This project requires [dotnet core](https://www.microsoft.com/net/download),
see image used in Jenkinsfile for specific requirements.

```cmd
dotnet build .\src\
```

## Testing

```cmd
dotnet test .\src\
```

## Command line tool

This section handles usage of CommandeLine-usage. This is mostly used for
testing and developing this tool.

This project uses [CommandLineParser](https://github.com/commandlineparser/commandline)
to parse command line options.

### Configurations

CommandLine-project reads configuration values from `appsettings.json` and
then overriden from `appsettings.Development.json` if it exists.

`ServicePrincipalConfiguration` is used to connect to Azure. These can be
read and created from Azure AD using Azure Portal (or powershell etc.)

Example of appsettings.Development.json

```json
{
    "ServicePrincipalConfiguration": {
        "ClientId": "e15bf1a8-c8b5-11e9-a32f-2a2ae2dbcce4",
        "ClientSecret": "this-was-very-secret",
        "TenantId": "e15c04c2-c8b5-11e9-a32f-2a2ae2dbcce4"
    }
}
```

### Usage

For up-to-date usage help:

```cmd
dotnet run --project .\src\Protacon.AzureSubscriptionCleaner.CommandLine -- --help
```

For simulated run:

```cmd
dotnet run --project .\src\Protacon.AzureSubscriptionCleaner.CommandLine -- -s
```

For actual run (this actually deletes stuff!):

```cmd
dotnet run --project .\src\Protacon.AzureSubscriptionCleaner.CommandLine --
```

## Deployment

Project `Protacon.AzureSubscriptionCleaner.AzureFunctions` can be deployed to Azure as Azure Function.

Create copy from `developer-settings.example.json` as `developer-settings.json`
with your own values and execute `deployment\Prepare-Environment.ps1`

The function app also needs to be able to delete other resource groups. This can be done by giving
the function app `Contributor` access to the subscription.

For details, see `deployment\Prepare-Environment.ps1`

## License

[The MIT License (MIT)](LICENSE)
