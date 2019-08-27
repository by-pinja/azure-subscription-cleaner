##############################################################################
#.SYNOPSIS
# Generates resources and resources groups for testing purposes
#
#.PARAMETER ResourceGroupCount
# Number of resource groups that are generated
#
##############################################################################
param(
    [Parameter(Mandatory = $true)][string]$ResourceGroupCount
)

For ($i = 0; $i -lt $ResourceGroupCount; $i++) {
    New-AzResourceGroup -Name "generated-$i" -Location "North Europe"
}