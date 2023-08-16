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



    Write-Host ""
    Write-Host "Environment variables"

    $var = (gci env:*).GetEnumerator() | Sort-Object Name
    $out = ""
    Foreach ($v in $var) {
        $out = $out + "`t{0,-28} = {1,-28}`n" -f $v.Name, $v.Value
    }

    Write-Host $out
     
    dir
    # write-output "dump variables on $env:BUILD_ARTIFACTSTAGINGDIRECTORY\test.md"
    # $fileName = "$env:BUILD_ARTIFACTSTAGINGDIRECTORY\test.md"
    # set-content $fileName $out
    # write-output "##vso[task.addattachment type=Distributedtask.Core.Summary;name=Environment Variables;]$fileName"

Stop-Transcript

