function Get-KuduCredentials() {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$ResourceGroup
    )
    Write-Host "Getting credentials from RG $ResourceGroup for APP $AppName"

    $xml = [xml](Get-AzWebAppPublishingProfile -Name $AppName `
            -ResourceGroupName $ResourceGroup `
            -OutputFile $null)

    # Extract connection information from publishing profile
    $username = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userName").value
    $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"MSDeploy`"]/@userPWD").value

    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
    return $base64AuthInfo
}

function Get-Token() {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$ResourceGroup
    )
    $encodedCreds = Get-KuduCredentials -AppName $AppName -ResourceGroup $ResourceGroup

    $jwt = Invoke-RestMethod -Uri "https://$AppName.scm.azurewebsites.net/api/functions/admin/token" `
        -Headers @{Authorization = ("Basic {0}" -f $encodedCreds) } `
        -Method GET

    return $jwt
}

function Get-MasterKey() {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$JwtToken
    )
    $uri = "https://$AppName.azurewebsites.net/admin/host/systemkeys/_master"
    $keys = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $JwtToken) } `
        -Uri $Uri

    return $keys.value
}

function Get-TriggerUri() {
    param(
        [Parameter(Mandatory)][string]$AppName,
        [Parameter(Mandatory)][string]$FunctionName,
        [Parameter(Mandatory)][string]$JwtToken
    )

    $response = Invoke-RestMethod -Method GET -Headers @{Authorization = ("Bearer {0}" -f $JwtToken) } `
        -Uri "https://$AppName.azurewebsites.net/admin/functions/$FunctionName"

    $url = $response.href
    return $url
}
