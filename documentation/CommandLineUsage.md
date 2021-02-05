# Command line tool

This section handles usage of CommandeLine-usage. This is mostly used for
testing and developing this tool.

This project uses [CommandLineParser](https://github.com/commandlineparser/commandline)
to parse command line options.

## Configurations

CommandLine-project reads configuration values from `appsettings.json` and
then overriden from `appsettings.Development.json` if it exists.

`ServicePrincipalConfiguration` is used to connect to Azure. These can be
read and created from Azure AD using Azure Portal (or powershell etc.)

`SlackClientSettings` are used to send messages to slack. This is not mandatory
in Command line tool. If `-c <channel>` parameter is used, settings are
required.

Example of appsettings.Development.json

```json
{
    "ServicePrincipalConfiguration": {
        "ClientId": "e15bf1a8-c8b5-11e9-a32f-2a2ae2dbcce4",
        "ClientSecret": "this-was-very-secret",
        "TenantId": "e15c04c2-c8b5-11e9-a32f-2a2ae2dbcce4"
    },
    "SlackClientSettings": {
        "BearerToken": "token-tokenmock-mocktoken"
    }
}
```

## Usage

For up-to-date usage help:

```cmd
dotnet run --project .\src\Pinja.AzureSubscriptionCleaner.CommandLine -- --help
```

For simulated run:

```cmd
dotnet run --project .\src\Pinja.AzureSubscriptionCleaner.CommandLine -- -s
```

For actual run (this actually deletes stuff!):

```cmd
dotnet run --project .\src\Pinja.AzureSubscriptionCleaner.CommandLine --
```

Actual run with Slack reporting

```cmd
dotnet run --project .\src\Pinja.AzureSubscriptionCleaner.CommandLine -- -c slack-channel
```
