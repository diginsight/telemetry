[CmdletBinding()]
param (
    [object] $context
)

Get-Module | Remove-Module
$keys = @('PSBoundParameters','PWD','*Preference') + $PSBoundParameters.Keys 
Get-Variable -Exclude $keys | Remove-Variable -EA 0

$projectBaseName = $($env:PROJECTBASENAME)
if ([string]::IsNullOrEmpty($projectBaseName)) { $projectBaseName = "101.Samples" }

$scriptFolder = $($PSScriptRoot.TrimEnd('\'));
Import-Module "$scriptFolder\Common.ps1"

Set-Location $scriptFolder
$scriptName = $MyInvocation.MyCommand.Name
Start-Transcript -Path "\Logs\$scriptName.log" -Append

    Write-Host "dumping context information - start" # & $context
    Write-Host "$context"  # & $context
    Write-Host "dumping context type" # & $context
    Write-Host $context.GetType()  # & $context
    Write-Host "dumping context object" # & $context
    Write-Host $context  # & $context
    Write-Host "dumping context information - completed" # & $context

Stop-Transcript

