# Azure Subscription Cleaner

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Build Status](https://jenkins.protacon.cloud/buildStatus/icon?job=www.github.com/azure-subscription-cleaner/master)](https://jenkins.protacon.cloud/blue/organizations/jenkins/www.github.com%2Fazure-subscription-cleaner/activity)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=by-pinja/azure-subscription-cleaner&identifier=204444972)](https://dependabot.com)

This software periodically deletes unlocked Azure Resources. In our case, it's the first saturday of the month.
This is specified with app settings. See `deployment/Prepare-Environment.ps1` and `deployment/azuredeploy.bicep`
for configuration example.

To opt out from this, lock your resouce group. for more information, see
[Opt Out](documentation/OptOut.md)

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

For acceptance testing, see [Acceptance Testing](documentation/AcceptanceTesting.md)

For running this locally, see [Comamnd line usage](documentation/CommandLineUsage.md)

## Deployment

Project `Pinja.AzureSubscriptionCleaner.AzureFunctions` can be deployed to Azure as Azure Function.

Create copy from `developer-settings.example.json` as `developer-settings.json`
with your own values and execute `deployment\Prepare-Environment.ps1`

The function app also needs to be able to delete other resource groups. This can be done by giving
the function app `Contributor` access to the subscription. This is not done as part of the CI progress.

For details, see `deployment\Prepare-Environment.ps1`

## License

[The MIT License (MIT)](LICENSE)
