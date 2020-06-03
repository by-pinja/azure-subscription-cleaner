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

Write-Host "Creating resource group $($settingsJson.ResourceGroupName) to location $($settingsJson.Location)..."
New-AzResourceGroup -Name $settingsJson.ResourceGroupName -Location $settingsJson.Location -Force

Write-Host 'Creating environment...'
New-AzResourceGroupDeployment `
    -Name 'test-deployment' `
    -TemplateFile 'Deployment/azuredeploy.json' `
    -ResourceGroupName $settingsJson.ResourceGroupName `
    -appName $settingsJson.ResourceGroupName `
    -environment "Development" `
    -slackBearerToken (ConvertTo-SecureString -String $settingsJson.SlackBearerToken -AsPlainText -Force)

$createdServicePrincipal = Get-AzADServicePrincipal -DisplayName $settingsJson.ResourceGroupName

$existingRoleAssingment = Get-AzRoleAssignment -ObjectId $createdServicePrincipal.Id -RoleDefinitionName 'Contributor'

if ( -Not ($existingRoleAssingment) ) {
    New-AzRoleAssignment `
        -ObjectId $createdServicePrincipal.Id `
        -RoleDefinitionName 'Contributor'
}

Write-Host 'Publishing...'
.\Deployment\Publish.ps1 -ResourceGroup $settingsJson.ResourceGroupName