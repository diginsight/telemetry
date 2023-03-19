[CmdletBinding()]
param (
    $connectionString,
    $sourceFolder,
    $azureShare,
    $rootDir,
    $version,
    $filePattern
)
# Get-Module | Remove-Module
# $keys = @('PSBoundParameters','PWD','*Preference') + $PSBoundParameters.Keys 
# Get-Variable -Exclude $keys | Remove-Variable -EA 0
Write-Host "connectionString: $connectionString"
Write-Host "sourceFolder: $sourceFolder"
Write-Host "azureShare: $azureShare"
Write-Host "rootDir: $rootDir"
Write-Host "filePattern: $filePattern" 
 
Get-Module -ListAvailable

#Import-Module Az
Import-Module Az.Storage
$scriptFolder = $($PSScriptRoot.TrimEnd('\'));
Import-Module "$scriptFolder\Common.ps1" 
 
Set-Location $scriptFolder
$scriptName = $MyInvocation.MyCommand.Name
Start-Transcript -Path "\Logs\$scriptName.log" -Append

$agentBuildDirectory = "$($env:AGENT_BUILDDIRECTORY)" 
$buildConfiguration =  "$($env:BUILDCONFIGURATION)" 
if ([string]::IsNullOrEmpty($agentBuildDirectory)) { $agentBuildDirectory = "D:\dev.darioa\97. diginsight\" }
if ([string]::IsNullOrEmpty($connectionString)) { $connectionString = "$($env:CONNECTIONSTRING)" }
if ([string]::IsNullOrEmpty($connectionString)) { $connectionString = "<connectionstring>;" }
if ([string]::IsNullOrEmpty($sourceFolder)) { $sourceFolder = "$($env:SOURCEFOLDER)" }
if ([string]::IsNullOrEmpty($sourceFolder)) { $sourceFolder = "packages" }
if ([string]::IsNullOrEmpty($azureShare))  { $azureShare = "$($env:AZURESHARE)" }
if ([string]::IsNullOrEmpty($azureShare)) { $azureShare = "azureshare" }
if ([string]::IsNullOrEmpty($rootDir)) { $rootDir = "$($env:ROOTDIR)" }
if ([string]::IsNullOrEmpty($rootDir)) { $rootDir = "\_releases\97. Diginsight" }
if ([string]::IsNullOrEmpty($buildConfiguration)) { $buildConfiguration = "Release" }


if ([string]::IsNullOrEmpty($version)) { $version = "$($env:VERSION)" }
if ([string]::IsNullOrEmpty($version)) { $version = "1.0.0.100" }
if ([string]::IsNullOrEmpty($filePattern)) { $filePattern = "$($env:FILEPATTERN)" }
if ([string]::IsNullOrEmpty($filePattern)) { $filePattern = ".*\.$version\.nupkg" }
 
Write-Host "agentBuildDirectory: $agentBuildDirectory"
Write-Host "sourceFolder: $sourceFolder"
Write-Host "connectionString: $connectionString"
Write-Host "azureShare: $azureShare"
Write-Host "rootDir: $rootDir"
Write-Host "filePattern: $filePattern"
Write-Host "buildConfiguration: $buildConfiguration"

$azureDirectory = "$(Get-Date -Format 'yyyyMMdd')_$(Get-Date -Format 'HHmmss')-$Env:BUILD_DEFINITIONNAME-$Env:BUILD_SOURCEBRANCHNAME"

Write-Host "azureDirectory: $azureDirectory"

$path = "$rootDir\$(Get-Date -Format 'yyyy')\$(Get-Date -Format 'MM')\$azureDirectory-v$version" 
Write-Host "path: $path"

#create primary region storage context
$ctx = New-AzStorageContext -ConnectionString $connectionString

#Check for Share Existence
$share = Get-AzStorageShare -Context $ctx -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq $azureShare }
if (!$share.Name) { $share = New-AzStorageShare $azureShare -Context $ctx }

# create a new directory in the share

$parts = $path.Split('\') 
$path = ""
$ex = $null;
foreach ($part in $parts) {
    if ([string]::IsNullOrEmpty($part)) { continue; }
    
    if ([string]::IsNullOrEmpty($path)) { $path = "$part" }
    else { $path = "$path\$part"; }
    
    try {
        $directory = Get-AzStorageFile -Share $share.CloudFileShare -Path $path | where { $_.GetType().Name -eq "CloudFileDirectory" }
        $ex = $null;
        Write-Host "Get-AzStorageFile -Share '$share.CloudFileShare' -Path '$path' => ok"
    } catch {
        $ex = "fault";
        Write-Host "Get-AzStorageFile -Share '$share.CloudFileShare' -Path '$path' => $ex"
    }
    if (![string]::IsNullOrEmpty($ex)) {
        Write-Host "creating folder: $path"
        $directory = New-AzStorageDirectory -Share $share.CloudFileShare -Path $path 
    }
}

$lastdir = Get-AzStorageFile -Share $share.CloudFileShare -ErrorAction SilentlyContinue | where { $_.Name -like "$($path)*" } | select -Last 1
if ($null -ne $lastdir) {
    # make the new dir name progressive
    if ($lastdir.Name -match "$($azureDirectory)_(\d+)") {
        $newfolderindex = ([int]::Parse($Matches[1]) + 1)
        $azureDirectory += "_$($newfolderindex)"
    } 
    else {
        $azureDirectory += "_1"
    }
}

# get all the folders in the AgentDir directory
$sourceFolderFull = "$agentBuildDirectory\$sourceFolder"
Write-Host "sourceFolderFull: $sourceFolderFull"

Write-Host "Creating folders on: $path"
$sourceDirectory = Get-Item -Path "$sourceFolderFull"
$folders = Get-ChildItem -Path "$sourceFolderFull" -Directory -Recurse
##$share = Get-AzStorageShare -Name $AzureShare -Context $ctx
foreach ($folder in $folders) {
    $f = ($folder.FullName).Substring(($sourceDirectory.FullName.Length))
    $newFolderPath = $path + $f
    # create a directory in the share for each folder
    Write-Host "creating folder: $newFolderPath"
    New-AzStorageDirectory -Share $share.CloudFileShare -Path $newFolderPath -ErrorAction SilentlyContinue
    Write-Host "create folder: $newFolderPath"

}

#Get all the files in the AgentDir directory
Write-Host "Copying files to: $path"
$files = Get-ChildItem -Path "$sourceFolderFull" -Recurse -File | Where-Object { $_.Name -match $filePattern }
foreach ($file in $Files) {
    $f = ($file.FullName).Substring(($sourceDirectory.FullName.Length))
    $newFilePath = $path + $f
    #upload the files to the storage
    Write-Host "copying file: '$($file.FullName)' to $newFilePath"
    Set-AzStorageFileContent -Share $share.CloudFileShare -Source $file.FullName -Path $newFilePath -Force
    Write-Host "copyed file: $newFilePath"
}

# Write-Host "$version"
# Write-Host "##vso[task.setvariable variable=version;isOutput=true]$version"

Stop-Transcript

