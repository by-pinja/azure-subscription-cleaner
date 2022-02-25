<#
    .SYNOPSIS
    Creates environment in Azure from given settings file

    .DESCRIPTION
    Creates and prepares and environment for development and testing.
    SettingsFile (default developer-settings.json) should contain all
    relat

    .PARAMETER SettinsFile
    Settings file that contains environment settings.
    Defaults to 'developer-settings.json'
#>
param(
    [Parameter()][string]$SettingsFile = 'developer-settings.json'
)
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "Reading settings from file $SettingsFile"
$settingsJson = Get-Content -Raw -Path $SettingsFile | ConvertFrom-Json

$tagsHashtable = @{ }
if ($settingsJson.Tags) {
    $settingsJson.Tags.psobject.properties | ForEach-Object { $tagsHashtable[$_.Name] = $_.Value }
}

Write-Host "Creating resource group $($settingsJson.ResourceGroupName) to location $($settingsJson.Location)..."
New-AzResourceGroup -Name $settingsJson.ResourceGroupName -Location $settingsJson.Location -Tag $tagsHashtable -Force

Write-Host 'Creating environment...'
New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'deployment/azuredeploy.bicep' `
    -ResourceGroupName $settingsJson.ResourceGroupName `
    -appName $settingsJson.ResourceGroupName `
    -environment "Development" `
    -slackChannel $settingsJson.SlackChannel `
    -simulate $true `
    -slackBearerToken (ConvertTo-SecureString -String $settingsJson.SlackBearerToken -AsPlainText -Force) `
    -cleanupSchedule '0 0 0 1-7 * SAT'

$createdServicePrincipal = Get-AzADServicePrincipal -DisplayName $settingsJson.ResourceGroupName

$existingRoleAssingment = Get-AzRoleAssignment -ObjectId $createdServicePrincipal.Id -RoleDefinitionName 'Contributor'

if ( -Not ($existingRoleAssingment) ) {
    New-AzRoleAssignment `
        -ObjectId $createdServicePrincipal.Id `
        -RoleDefinitionName 'Contributor'
}

Write-Host 'Publishing...'
.\deployment\Publish.ps1 -ResourceGroup $settingsJson.ResourceGroupName
