
function GetVersionAttribute([string] $filePath, [string] $versionAttribute) {

    [string[]] $lines = Get-Content -Path $filePath
    
    foreach($line in $lines)
    { 
        $line = $line.Trim()
        if ($line.StartsWith("[assembly: $versionAttribute(""")) 
        {
            $line1 = $line.TrimEnd( """)]");
            $versionString = $line1.TrimStart( "[assembly: $versionAttribute(""");
            $version = [version]$versionString
            return $version
        }
    }
    return $null;
}

function SetVersionAttribute([string] $filePath, [string] $versionAttribute, [string] $version) {

    [string[]] $lines = Get-Content -Path $filePath
    $newLines = New-Object System.Collections.Generic.List[string]

    foreach($line in $lines)
    {
        $line = $line.Trim()
        if ($line.StartsWith("[assembly: $versionAttribute(""")) 
        {
            $line = "[assembly: $versionAttribute(""$version"")]"
        }
        $newLines.Add($line);
    }
    $newLines | out-file $filePath

    return $null;
}

function SetContainerVersion([string] $filePath, [string] $version) {

    [string[]] $lines = Get-Content -Path $filePath
    $newLines = New-Object System.Collections.Generic.List[string]
    $imageRegEx = '.*- image:.*:.*$'

    foreach($line in $lines)
    {
        # $line = $line.Trim()
        
        if ($line -match $imageRegEx) 
        {
            # $lineStart = ($line -split ":", -2)  | select-object -first 1
            $lineStart = $($line -split ':' | select -skiplast 1)  -join ':'
            $line = "$lineStart`:$version"
        }
        $newLines.Add($line);
    }
    $newLines | out-file $filePath

    return $null;
}


function IncrementVersionAttribute([string] $filePath, [string] $versionAttribute) {

    [string[]] $lines = Get-Content -Path $filePath
    $newLines = New-Object System.Collections.Generic.List[string]
    $newVersion = $null;

    foreach($line in $lines)
    {
        $line = $line.Trim()
        if ($line.StartsWith("[assembly: $versionAttribute(""")) 
        {
            $line1 = $line.TrimEnd( """)]");
            $versionString = $line1.TrimStart( "[assembly: $versionAttribute(""");
            $version = [version]$versionString
            $newVersion = "{0}.{1}.{2}.{3}" -f $version.Major, $version.Minor, $version.Build, ($version.Revision + 1)
    
            $line = "[assembly: $versionAttribute(""$newVersion"")]"
        }
        $newLines.Add($line);
    }
    $newLines | out-file $filePath

    return $newVersion;
}

function GetFiles($path, $filter = $null, $include = $null, $exclude = $null, $depth = $null)  {
    Write-Host "GetFiles (path:$path, filter:$filter, include:$include, exclude:$exclude, depth:$depth) START"

    if ([string]::IsNullOrEmpty($path)) { $path = ".." }
    if ($path.IndexOf('|') -ge 0) { $path = $path.Split('|') }
    if ([string]::IsNullOrEmpty($filter)) { $filter = "*.csproj" }
    if ([string]::IsNullOrEmpty($include)) { $include = "*" }
    $location = Get-Location 
    Write-Host "location: $location"

    if ($depth -eq $null) {
        $paths = Get-ChildItem  $path -Recurse -Filter $filter -Include $include -Exclude $exclude 
    } else {
        $paths = Get-ChildItem  $path -Recurse -Depth $depth -Filter $filter # -Include $include -Exclude $exclude 
    }
    
    Write-Host ""
    Write-Host "paths:"
    $paths | ForEach-Object -Process { Write-Host "path: '$($_.FullName)'" }
    
    return $paths;
}

function GetFolders($path, $filter = $null, $include = $null, $exclude = $null, $depth = $null)  {
    Write-Host "GetFolders (path:$path, filter:$filter, include:$include, exclude:$exclude, depth:$depth) START"
    if ([string]::IsNullOrEmpty($path)) { $path = ".." }
    if ($path.IndexOf('|') -ge 0) { $path = $path.Split('|') }
    if ([string]::IsNullOrEmpty($filter)) { $filter = "*" }
    if ([string]::IsNullOrEmpty($include)) { $include = "*" }
    $location = Get-Location 
    Write-Host "location: $location"

    if ($depth -eq $null) {
        $paths = Get-ChildItem  $path -Recurse -Filter $filter -Include $include -Exclude $exclude -Attributes Directory 
    } else {
        $paths = Get-ChildItem  $path -Recurse -Depth $depth -Filter $filter -Attributes Directory #-Include $include -Exclude $exclude 
    }
    
    Write-Host ""
    Write-Host "paths:"
    $paths | ForEach-Object -Process { Write-Host "path: '$($_.FullName)'" }
    
    return $paths;
}


