<#
    .SYNOPSIS
    Simple script for building and unit testing the solution.
#>

$ErrorActionPreference = "Stop"
dotnet build .\src\
dotnet test .\src\