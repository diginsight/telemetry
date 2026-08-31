#requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateSet('ResolveVersion', 'Stage', 'Validate', 'Compare', 'PublishNuGet')]
    [string] $Command,

    [string] $Tag,

    [string] $ManifestPath = (Join-Path $PSScriptRoot 'package-manifest.json'),

    [string] $SourceRoot,

    [string] $StagePath,

    [string] $ReferencePath,

    [string] $NuGetSource = 'https://api.nuget.org/v3/index.json'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-FullPath {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return [System.IO.Path]::GetFullPath($Path)
}

function ConvertTo-NormalizedPackageVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Version,

        [switch] $SourceTag
    )

    $value = $Version.Trim()
    if ($SourceTag) {
        if (-not $value.StartsWith('v', [System.StringComparison]::Ordinal)) {
            throw "Source tag '$Version' must start with a lowercase 'v'."
        }
        $value = $value.Substring(1)
    }

    $match = [regex]::Match(
        $value,
        '^(?<numbers>[0-9]+(?:\.[0-9]+){0,3})(?<suffix>-[0-9A-Za-z](?:[0-9A-Za-z.-]*[0-9A-Za-z])?)?$',
        [System.Text.RegularExpressions.RegexOptions]::CultureInvariant
    )
    if (-not $match.Success) {
        throw "Version '$Version' is not a supported NuGet version. Expected one to four numeric components and an optional prerelease suffix."
    }

    $numberStrings = @($match.Groups['numbers'].Value.Split('.'))
    if ($SourceTag -and $numberStrings.Count -lt 3) {
        throw "Source tag '$Version' must contain at least major, minor, and patch components."
    }

    $numbers = [System.Collections.Generic.List[string]]::new()
    foreach ($numberString in $numberStrings) {
        $number = 0L
        if (-not [long]::TryParse($numberString, [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref] $number)) {
            throw "Version component '$numberString' in '$Version' is outside the supported range."
        }
        $numbers.Add($number.ToString([System.Globalization.CultureInfo]::InvariantCulture))
    }
    while ($numbers.Count -lt 3) {
        $numbers.Add('0')
    }
    if ($numbers.Count -eq 4 -and $numbers[3] -eq '0') {
        $numbers.RemoveAt(3)
    }

    $suffix = $match.Groups['suffix'].Value
    return (($numbers -join '.') + $suffix)
}

function Get-PackageConfiguration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $fullPath = Get-FullPath $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Package inventory '$fullPath' does not exist."
    }

    $configuration = Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json -Depth 20
    foreach ($propertyName in @('schemaVersion', 'repository', 'solution', 'stagingPath', 'packages')) {
        if ($null -eq $configuration.PSObject.Properties[$propertyName]) {
            throw "Package inventory '$fullPath' is missing '$propertyName'."
        }
    }
    if ([int] $configuration.schemaVersion -ne 1) {
        throw "Package inventory '$fullPath' has unsupported schema version '$($configuration.schemaVersion)'."
    }
    if ([string]::IsNullOrWhiteSpace([string] $configuration.repository)) {
        throw "Package inventory '$fullPath' has an empty repository."
    }
    if ([string]::IsNullOrWhiteSpace([string] $configuration.solution) -or [string]::IsNullOrWhiteSpace([string] $configuration.stagingPath)) {
        throw "Package inventory '$fullPath' must specify solution and stagingPath."
    }

    $packages = @($configuration.packages)
    if ($packages.Count -eq 0) {
        throw "Package inventory '$fullPath' does not list any packages."
    }

    $seenIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $normalizedPackages = [System.Collections.Generic.List[object]]::new()
    foreach ($package in $packages) {
        if ($null -eq $package.PSObject.Properties['id'] -or [string]::IsNullOrWhiteSpace([string] $package.id)) {
            throw "Package inventory '$fullPath' contains a package with no id."
        }
        if ($null -eq $package.PSObject.Properties['symbolsRequired']) {
            throw "Package '$($package.id)' must specify symbolsRequired."
        }

        $id = ([string] $package.id).Trim()
        if (-not $seenIds.Add($id)) {
            throw "Package inventory '$fullPath' contains duplicate id '$id'."
        }
        $normalizedPackages.Add([pscustomobject]@{
            Id              = $id
            SymbolsRequired = [bool] $package.symbolsRequired
        })
    }

    return [pscustomobject]@{
        SchemaVersion = 1
        Repository    = [string] $configuration.repository
        Solution      = [string] $configuration.solution
        StagingPath   = [string] $configuration.stagingPath
        Packages      = @($normalizedPackages | Sort-Object -Property Id)
        Path          = $fullPath
    }
}

function Get-PackageArchiveMetadata {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [System.IO.FileInfo] $File
    )

    $fileName = $File.Name
    $role = if ($fileName.EndsWith('.snupkg', [System.StringComparison]::OrdinalIgnoreCase)) {
        'symbols'
    }
    elseif ($fileName.EndsWith('.nupkg', [System.StringComparison]::OrdinalIgnoreCase)) {
        'package'
    }
    else {
        throw "File '$($File.FullName)' is not a NuGet package archive."
    }

    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($File.FullName)
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) })
        if ($nuspecEntries.Count -ne 1) {
            throw "Package '$fileName' must contain exactly one .nuspec file; found $($nuspecEntries.Count)."
        }
        $nuspecEntry = $nuspecEntries[0]
        if ($nuspecEntry.Length -gt 1MB) {
            throw "The .nuspec in '$fileName' is unexpectedly large."
        }

        $stream = $nuspecEntry.Open()
        try {
            $settings = [System.Xml.XmlReaderSettings]::new()
            $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
            $settings.XmlResolver = $null
            $reader = [System.Xml.XmlReader]::Create($stream, $settings)
            try {
                $document = [System.Xml.XmlDocument]::new()
                $document.XmlResolver = $null
                $document.Load($reader)
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    catch {
        throw "Could not inspect package '$($File.FullName)': $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }
    }

    $idNode = $document.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
    $versionNode = $document.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='version']")
    if ($null -eq $idNode -or [string]::IsNullOrWhiteSpace($idNode.InnerText)) {
        throw "Package '$fileName' has no embedded package id."
    }
    if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        throw "Package '$fileName' has no embedded package version."
    }

    $rawVersion = $versionNode.InnerText.Trim()
    return [pscustomobject]@{
        File       = $File
        FileName   = $fileName
        Id         = $idNode.InnerText.Trim()
        RawVersion = $rawVersion
        Version    = ConvertTo-NormalizedPackageVersion -Version $rawVersion
        Role       = $role
        Size       = $File.Length
    }
}

function Get-PackageArchives {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [switch] $Recurse,

        [switch] $ReleaseOutputOnly
    )

    $fullPath = Get-FullPath $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "Package directory '$fullPath' does not exist."
    }

    $files = if ($Recurse) {
        @(Get-ChildItem -LiteralPath $fullPath -Recurse -File)
    }
    else {
        @(Get-ChildItem -LiteralPath $fullPath -File)
    }

    $archives = @($files | Where-Object {
        $_.Name.EndsWith('.nupkg', [System.StringComparison]::OrdinalIgnoreCase) -or
        $_.Name.EndsWith('.snupkg', [System.StringComparison]::OrdinalIgnoreCase)
    })
    if ($ReleaseOutputOnly) {
        $archives = @($archives | Where-Object {
            $_.FullName -match '[\\/]bin[\\/]Release[\\/]'
        })
    }

    return @($archives | Sort-Object -Property FullName)
}

function Assert-PackageInventory {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object[]] $Archives,

        [Parameter(Mandatory)]
        [object] $Configuration,

        [Parameter(Mandatory)]
        [string] $ExpectedVersion
    )

    $expectedById = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($package in $Configuration.Packages) {
        $expectedById.Add($package.Id, $package)
    }

    $seenNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $seenIdentityRoles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($archive in $Archives) {
        if (-not $seenNames.Add($archive.FileName)) {
            throw "Duplicate release filename '$($archive.FileName)'."
        }
        if (-not [string]::Equals($archive.Version, $ExpectedVersion, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Package '$($archive.FileName)' contains version '$($archive.RawVersion)', expected '$ExpectedVersion'."
        }
        if (-not $expectedById.ContainsKey($archive.Id)) {
            throw "Unexpected package id '$($archive.Id)' in '$($archive.FileName)'."
        }
        $identityRole = "$($archive.Id)|$($archive.Role)"
        if (-not $seenIdentityRoles.Add($identityRole)) {
            throw "Duplicate $($archive.Role) archive for package '$($archive.Id)'."
        }
    }

    foreach ($package in $Configuration.Packages) {
        $packageArchives = @($Archives | Where-Object {
            $_.Role -eq 'package' -and [string]::Equals($_.Id, $package.Id, [System.StringComparison]::OrdinalIgnoreCase)
        })
        if ($packageArchives.Count -ne 1) {
            throw "Expected exactly one .nupkg for '$($package.Id)'; found $($packageArchives.Count)."
        }

        $symbolArchives = @($Archives | Where-Object {
            $_.Role -eq 'symbols' -and [string]::Equals($_.Id, $package.Id, [System.StringComparison]::OrdinalIgnoreCase)
        })
        if ($package.SymbolsRequired -and $symbolArchives.Count -ne 1) {
            throw "Expected exactly one .snupkg for '$($package.Id)'; found $($symbolArchives.Count)."
        }
        if (-not $package.SymbolsRequired -and $symbolArchives.Count -gt 1) {
            throw "Expected at most one .snupkg for '$($package.Id)'; found $($symbolArchives.Count)."
        }
    }
}

function Get-Sha256 {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function New-ReleaseManifestObject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object[]] $Archives,

        [Parameter(Mandatory)]
        [object] $Configuration,

        [Parameter(Mandatory)]
        [string] $Tag,

        [Parameter(Mandatory)]
        [string] $Version
    )

    $assetItems = foreach ($archive in ($Archives | Sort-Object -Property FileName)) {
        [ordered]@{
            fileName       = $archive.FileName
            role           = $archive.Role
            packageId      = $archive.Id
            packageVersion = $Version
            sha256         = Get-Sha256 $archive.File.FullName
            size           = [long] $archive.File.Length
        }
    }

    $packageItems = foreach ($package in $Configuration.Packages) {
        [ordered]@{
            id              = $package.Id
            version         = $Version
            symbolsRequired = [bool] $package.SymbolsRequired
        }
    }

    return [ordered]@{
        schemaVersion  = 1
        repository     = $Configuration.Repository
        sourceTag      = $Tag
        packageVersion = $Version
        packages       = @($packageItems)
        assets         = @($assetItems)
    }
}

function Write-ReleaseMetadata {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [object[]] $Archives,

        [Parameter(Mandatory)]
        [object] $Configuration,

        [Parameter(Mandatory)]
        [string] $Tag,

        [Parameter(Mandatory)]
        [string] $Version
    )

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    $releaseManifest = New-ReleaseManifestObject -Archives $Archives -Configuration $Configuration -Tag $Tag -Version $Version
    $json = $releaseManifest | ConvertTo-Json -Depth 10
    [System.IO.File]::WriteAllText((Join-Path $Path 'release-manifest.json'), $json + "`n", $utf8NoBom)

    $checksumLines = foreach ($archive in ($Archives | Sort-Object -Property FileName)) {
        "$(Get-Sha256 $archive.File.FullName)  $($archive.FileName)"
    }
    [System.IO.File]::WriteAllText((Join-Path $Path 'SHA256SUMS'), (($checksumLines -join "`n") + "`n"), $utf8NoBom)
}

function Test-ReleaseSet {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $PackageManifestPath,

        [Parameter(Mandatory)]
        [string] $Tag
    )

    $fullPath = Get-FullPath $Path
    $configuration = Get-PackageConfiguration $PackageManifestPath
    $expectedVersion = ConvertTo-NormalizedPackageVersion -Version $Tag -SourceTag

    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) {
        throw "Release directory '$fullPath' does not exist."
    }
    $directories = @(Get-ChildItem -LiteralPath $fullPath -Directory)
    if ($directories.Count -ne 0) {
        throw "Release directory '$fullPath' must not contain subdirectories."
    }

    $files = @(Get-ChildItem -LiteralPath $fullPath -File)
    $archives = @(Get-PackageArchives -Path $fullPath | ForEach-Object { Get-PackageArchiveMetadata $_ })
    Assert-PackageInventory -Archives $archives -Configuration $configuration -ExpectedVersion $expectedVersion

    $releaseManifestPath = Join-Path $fullPath 'release-manifest.json'
    $checksumsPath = Join-Path $fullPath 'SHA256SUMS'
    foreach ($requiredPath in @($releaseManifestPath, $checksumsPath)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "Release directory '$fullPath' is missing '$([System.IO.Path]::GetFileName($requiredPath))'."
        }
    }

    $expectedNames = @(($archives.FileName + @('SHA256SUMS', 'release-manifest.json')) | Sort-Object)
    $actualNames = @($files.Name | Sort-Object)
    if (($expectedNames -join "`n") -cne ($actualNames -join "`n")) {
        throw "Release file inventory mismatch. Expected [$($expectedNames -join ', ')]; found [$($actualNames -join ', ')]."
    }

    $manifest = Get-Content -LiteralPath $releaseManifestPath -Raw | ConvertFrom-Json -Depth 20
    foreach ($propertyName in @('schemaVersion', 'repository', 'sourceTag', 'packageVersion', 'packages', 'assets')) {
        if ($null -eq $manifest.PSObject.Properties[$propertyName]) {
            throw "Release manifest is missing '$propertyName'."
        }
    }
    if ([int] $manifest.schemaVersion -ne 1) {
        throw "Release manifest has unsupported schema version '$($manifest.schemaVersion)'."
    }
    if (-not [string]::Equals([string] $manifest.repository, $configuration.Repository, [System.StringComparison]::Ordinal)) {
        throw "Release manifest repository '$($manifest.repository)' does not match '$($configuration.Repository)'."
    }
    if (-not [string]::Equals([string] $manifest.sourceTag, $Tag, [System.StringComparison]::Ordinal)) {
        throw "Release manifest tag '$($manifest.sourceTag)' does not match '$Tag'."
    }
    $manifestVersion = ConvertTo-NormalizedPackageVersion -Version ([string] $manifest.packageVersion)
    if (-not [string]::Equals($manifestVersion, $expectedVersion, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Release manifest version '$($manifest.packageVersion)' does not match '$expectedVersion'."
    }

    $expectedPackageRows = @($configuration.Packages | ForEach-Object { "$($_.Id)|$expectedVersion|$($_.SymbolsRequired)" } | Sort-Object)
    $actualPackageRows = @($manifest.packages | ForEach-Object {
        if ($null -eq $_.PSObject.Properties['id'] -or $null -eq $_.PSObject.Properties['version'] -or $null -eq $_.PSObject.Properties['symbolsRequired']) {
            throw 'Release manifest contains an incomplete package record.'
        }
        "$($_.id)|$(ConvertTo-NormalizedPackageVersion -Version ([string] $_.version))|$([bool] $_.symbolsRequired)"
    } | Sort-Object)
    if (($expectedPackageRows -join "`n") -cne ($actualPackageRows -join "`n")) {
        throw 'Release manifest package inventory does not match the tracked package inventory.'
    }

    $expectedAssets = New-ReleaseManifestObject -Archives $archives -Configuration $configuration -Tag $Tag -Version $expectedVersion
    $expectedAssetRows = @($expectedAssets.assets | ForEach-Object {
        "$($_.fileName)|$($_.role)|$($_.packageId)|$($_.packageVersion)|$($_.sha256)|$($_.size)"
    } | Sort-Object)
    $actualAssetRows = @($manifest.assets | ForEach-Object {
        foreach ($propertyName in @('fileName', 'role', 'packageId', 'packageVersion', 'sha256', 'size')) {
            if ($null -eq $_.PSObject.Properties[$propertyName]) {
                throw "Release manifest contains an asset missing '$propertyName'."
            }
        }
        "$($_.fileName)|$($_.role)|$($_.packageId)|$(ConvertTo-NormalizedPackageVersion -Version ([string] $_.packageVersion))|$(([string] $_.sha256).ToLowerInvariant())|$([long] $_.size)"
    } | Sort-Object)
    if (($expectedAssetRows -join "`n") -cne ($actualAssetRows -join "`n")) {
        throw 'Release manifest asset metadata does not match the package bytes.'
    }

    $checksumLines = @(Get-Content -LiteralPath $checksumsPath | Where-Object { $_ -ne '' })
    $checksumRows = [System.Collections.Generic.List[string]]::new()
    $checksumNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in $checksumLines) {
        $match = [regex]::Match($line, '^(?<hash>[0-9a-fA-F]{64})  (?<name>[^\\/]+)$', [System.Text.RegularExpressions.RegexOptions]::CultureInvariant)
        if (-not $match.Success) {
            throw "Invalid SHA256SUMS line '$line'."
        }
        $name = $match.Groups['name'].Value
        if (-not $checksumNames.Add($name)) {
            throw "Duplicate SHA256SUMS entry '$name'."
        }
        $assetPath = Join-Path $fullPath $name
        if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
            throw "SHA256SUMS references missing asset '$name'."
        }
        $actualHash = Get-Sha256 $assetPath
        $declaredHash = $match.Groups['hash'].Value.ToLowerInvariant()
        if ($declaredHash -cne $actualHash) {
            throw "Checksum mismatch for '$name'."
        }
        $checksumRows.Add("$declaredHash  $name")
    }

    $expectedChecksumRows = @($archives | Sort-Object -Property FileName | ForEach-Object { "$(Get-Sha256 $_.File.FullName)  $($_.FileName)" })
    if (($expectedChecksumRows -join "`n") -cne ($checksumRows -join "`n")) {
        throw 'SHA256SUMS inventory or deterministic ordering does not match the package assets.'
    }

    Write-Host "Validated $($configuration.Packages.Count) packages and $($archives.Count) package assets for $Tag ($expectedVersion)."
}

function Invoke-StageRelease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $InputRoot,

        [Parameter(Mandatory)]
        [string] $OutputPath,

        [Parameter(Mandatory)]
        [string] $PackageManifestPath,

        [Parameter(Mandatory)]
        [string] $Tag
    )

    $configuration = Get-PackageConfiguration $PackageManifestPath
    $expectedVersion = ConvertTo-NormalizedPackageVersion -Version $Tag -SourceTag
    $sourceFiles = @(Get-PackageArchives -Path $InputRoot -Recurse -ReleaseOutputOnly)
    if ($sourceFiles.Count -eq 0) {
        throw "No package outputs were found under '$InputRoot/**/bin/Release'."
    }

    $allMetadata = @($sourceFiles | ForEach-Object { Get-PackageArchiveMetadata $_ })
    $currentMetadata = @($allMetadata | Where-Object {
        [string]::Equals($_.Version, $expectedVersion, [System.StringComparison]::OrdinalIgnoreCase)
    })
    Assert-PackageInventory -Archives $currentMetadata -Configuration $configuration -ExpectedVersion $expectedVersion

    $fullOutputPath = Get-FullPath $OutputPath
    if (Test-Path -LiteralPath $fullOutputPath) {
        Remove-Item -LiteralPath $fullOutputPath -Recurse -Force
    }
    $null = New-Item -ItemType Directory -Path $fullOutputPath -Force

    foreach ($archive in $currentMetadata) {
        Copy-Item -LiteralPath $archive.File.FullName -Destination (Join-Path $fullOutputPath $archive.FileName)
    }

    $stagedArchives = @(Get-PackageArchives -Path $fullOutputPath | ForEach-Object { Get-PackageArchiveMetadata $_ })
    Write-ReleaseMetadata -Path $fullOutputPath -Archives $stagedArchives -Configuration $configuration -Tag $Tag -Version $expectedVersion
    Test-ReleaseSet -Path $fullOutputPath -PackageManifestPath $PackageManifestPath -Tag $Tag
}

function Compare-ReleaseSets {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ExpectedPath,

        [Parameter(Mandatory)]
        [string] $ActualPath,

        [Parameter(Mandatory)]
        [string] $PackageManifestPath,

        [Parameter(Mandatory)]
        [string] $Tag
    )

    Test-ReleaseSet -Path $ExpectedPath -PackageManifestPath $PackageManifestPath -Tag $Tag
    Test-ReleaseSet -Path $ActualPath -PackageManifestPath $PackageManifestPath -Tag $Tag

    $expectedFiles = @(Get-ChildItem -LiteralPath (Get-FullPath $ExpectedPath) -File | Sort-Object -Property Name)
    $actualFiles = @(Get-ChildItem -LiteralPath (Get-FullPath $ActualPath) -File | Sort-Object -Property Name)
    if (($expectedFiles.Name -join "`n") -cne ($actualFiles.Name -join "`n")) {
        throw 'Release directories have different file inventories.'
    }

    for ($index = 0; $index -lt $expectedFiles.Count; $index++) {
        if ($expectedFiles[$index].Length -ne $actualFiles[$index].Length) {
            throw "Release asset '$($expectedFiles[$index].Name)' has a size mismatch."
        }
        $expectedHash = Get-Sha256 $expectedFiles[$index].FullName
        $actualHash = Get-Sha256 $actualFiles[$index].FullName
        if ($expectedHash -cne $actualHash) {
            throw "Release asset '$($expectedFiles[$index].Name)' has a SHA-256 mismatch."
        }
    }

    Write-Host "Compared $($expectedFiles.Count) release assets; all bytes match."
}

function Publish-NuGetPackages {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $PackageManifestPath,

        [Parameter(Mandatory)]
        [string] $Tag,

        [Parameter(Mandatory)]
        [string] $Source
    )

    if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) {
        throw 'NUGET_API_KEY is required for NuGet publication.'
    }
    Test-ReleaseSet -Path $Path -PackageManifestPath $PackageManifestPath -Tag $Tag

    $releaseManifest = Get-Content -LiteralPath (Join-Path (Get-FullPath $Path) 'release-manifest.json') -Raw | ConvertFrom-Json -Depth 20
    $packageAssets = @($releaseManifest.assets | Where-Object { $_.role -eq 'package' } | Sort-Object -Property fileName)
    foreach ($asset in $packageAssets) {
        $packagePath = Join-Path (Get-FullPath $Path) ([string] $asset.fileName)
        Write-Host "Publishing $($asset.packageId) $($asset.packageVersion)."
        & dotnet nuget push $packagePath --source $Source --api-key $env:NUGET_API_KEY --skip-duplicate
        if ($LASTEXITCODE -ne 0) {
            throw "NuGet push failed for '$($asset.fileName)' with exit code $LASTEXITCODE."
        }
    }
}

if ($MyInvocation.InvocationName -eq '.') {
    return
}
if ([string]::IsNullOrWhiteSpace($Command)) {
    throw 'Command is required when the script is executed directly.'
}

switch ($Command) {
    'ResolveVersion' {
        if ([string]::IsNullOrWhiteSpace($Tag)) { throw 'Tag is required.' }
        ConvertTo-NormalizedPackageVersion -Version $Tag -SourceTag
    }
    'Stage' {
        if ([string]::IsNullOrWhiteSpace($Tag) -or [string]::IsNullOrWhiteSpace($SourceRoot) -or [string]::IsNullOrWhiteSpace($StagePath)) {
            throw 'Tag, SourceRoot, and StagePath are required for Stage.'
        }
        Invoke-StageRelease -InputRoot $SourceRoot -OutputPath $StagePath -PackageManifestPath $ManifestPath -Tag $Tag
    }
    'Validate' {
        if ([string]::IsNullOrWhiteSpace($Tag) -or [string]::IsNullOrWhiteSpace($StagePath)) {
            throw 'Tag and StagePath are required for Validate.'
        }
        Test-ReleaseSet -Path $StagePath -PackageManifestPath $ManifestPath -Tag $Tag
    }
    'Compare' {
        if ([string]::IsNullOrWhiteSpace($Tag) -or [string]::IsNullOrWhiteSpace($StagePath) -or [string]::IsNullOrWhiteSpace($ReferencePath)) {
            throw 'Tag, StagePath, and ReferencePath are required for Compare.'
        }
        Compare-ReleaseSets -ExpectedPath $ReferencePath -ActualPath $StagePath -PackageManifestPath $ManifestPath -Tag $Tag
    }
    'PublishNuGet' {
        if ([string]::IsNullOrWhiteSpace($Tag) -or [string]::IsNullOrWhiteSpace($StagePath)) {
            throw 'Tag and StagePath are required for PublishNuGet.'
        }
        Publish-NuGetPackages -Path $StagePath -PackageManifestPath $ManifestPath -Tag $Tag -Source $NuGetSource
    }
}
