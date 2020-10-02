<#
    .SYNOPSIS
    Triggers timer in Azure Functions which launches other functions.

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. "$PSScriptRoot/FunctionUtil.ps1"

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$token = Get-Token -AppName $settingsJson.ResourceGroupName -ResourceGroup $settingsJson.ResourceGroupName

$key = Get-MasterKey -AppName $settingsJson.ResourceGroupName -JwtToken $token
$uri = Get-TriggerUri -AppName $settingsJson.ResourceGroupName -FunctionName 'TimerStart' -JwtToken $token

Invoke-RestMethod `
    -Method POST `
    -Uri $uri `
    -ContentType 'application/json;charset=UTF-8' `
    -Body (@{ } | ConvertTo-Json) `
    -Headers @{'x-functions-key' = $key }
