#requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '..' 'Publish-Packages.ps1')

$script:Passed = 0
$script:Failed = 0

function Invoke-Test {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [scriptblock] $Body
    )

    try {
        & $Body
        $script:Passed++
        Write-Host "PASS: $Name"
    }
    catch {
        $script:Failed++
        Write-Host "FAIL: $Name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [object] $Expected,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [object] $Actual
    )

    if ([string] $Expected -cne [string] $Actual) {
        throw "Expected '$Expected', got '$Actual'."
    }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory)]
        [scriptblock] $Body,

        [string] $MessageLike = '*'
    )

    try {
        & $Body
    }
    catch {
        if ($_.Exception.Message -notlike $MessageLike) {
            throw "Expected error like '$MessageLike', got '$($_.Exception.Message)'."
        }
        return
    }
    throw 'Expected an exception, but the operation succeeded.'
}

function New-TestArchive {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Id,

        [Parameter(Mandatory)]
        [string] $Version
    )

    $directory = Split-Path -Parent $Path
    $null = New-Item -ItemType Directory -Path $directory -Force
    $fileStream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            $entry = $archive.CreateEntry("$Id.nuspec")
            $entryStream = $entry.Open()
            try {
                $writer = [System.IO.StreamWriter]::new($entryStream, [System.Text.UTF8Encoding]::new($false), 1024, $true)
                try {
                    $writer.Write("<?xml version=`"1.0`"?><package><metadata><id>$Id</id><version>$Version</version><authors>test</authors><description>test</description></metadata></package>")
                }
                finally {
                    $writer.Dispose()
                }
            }
            finally {
                $entryStream.Dispose()
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $fileStream.Dispose()
    }
}

function New-TestFixture {
    param(
        [Parameter(Mandatory)]
        [string] $Root,

        [switch] $MissingSecondPackage,

        [switch] $UnexpectedPackage
    )

    $manifest = [ordered]@{
        schemaVersion = 1
        repository    = 'example/repository'
        solution      = 'src/Test.slnx'
        stagingPath   = 'artifacts/release'
        packages      = @(
            [ordered]@{ id = 'Test.One'; symbolsRequired = $true },
            [ordered]@{ id = 'Test.Two'; symbolsRequired = $true }
        )
    }
    $manifestPath = Join-Path $Root 'package-manifest.json'
    $null = New-Item -ItemType Directory -Path $Root -Force
    [System.IO.File]::WriteAllText($manifestPath, (($manifest | ConvertTo-Json -Depth 10) + "`n"), [System.Text.UTF8Encoding]::new($false))

    $outputPath = Join-Path $Root 'src' 'Project' 'bin' 'Release'
    foreach ($id in @('Test.One', 'Test.Two')) {
        if ($MissingSecondPackage -and $id -eq 'Test.Two') {
            continue
        }
        New-TestArchive -Path (Join-Path $outputPath "$id.3.8.0.1.nupkg") -Id $id -Version '3.8.0.1'
        New-TestArchive -Path (Join-Path $outputPath "$id.3.8.0.1.snupkg") -Id $id -Version '3.8.0.1'
    }
    if ($UnexpectedPackage) {
        New-TestArchive -Path (Join-Path $outputPath 'Test.Unexpected.3.8.0.1.nupkg') -Id 'Test.Unexpected' -Version '3.8.0.1'
        New-TestArchive -Path (Join-Path $outputPath 'Test.Unexpected.3.8.0.1.snupkg') -Id 'Test.Unexpected' -Version '3.8.0.1'
    }

    return [pscustomobject]@{
        ManifestPath = $manifestPath
        SourceRoot   = Join-Path $Root 'src'
        StagePath    = Join-Path $Root 'stage'
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "diginsight-release-tests-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $tempRoot -Force
try {
    Invoke-Test 'four-part zero tag normalizes to three parts' {
        Assert-Equal '3.8.0' (ConvertTo-NormalizedPackageVersion -Version 'v3.8.0.0' -SourceTag)
    }

    Invoke-Test 'nonzero fourth tag component is preserved' {
        Assert-Equal '3.8.0.1' (ConvertTo-NormalizedPackageVersion -Version 'v3.8.0.1' -SourceTag)
    }

    Invoke-Test 'valid synthetic nupkg and nuspec set stages and validates' {
        $fixture = New-TestFixture -Root (Join-Path $tempRoot 'valid')
        Invoke-StageRelease -InputRoot $fixture.SourceRoot -OutputPath $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
        Test-ReleaseSet -Path $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
    }

    Invoke-Test 'missing package is rejected' {
        $fixture = New-TestFixture -Root (Join-Path $tempRoot 'missing') -MissingSecondPackage
        Assert-Throws -MessageLike "*Test.Two*found 0*" -Body {
            Invoke-StageRelease -InputRoot $fixture.SourceRoot -OutputPath $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
        }
    }

    Invoke-Test 'unexpected package is rejected' {
        $fixture = New-TestFixture -Root (Join-Path $tempRoot 'unexpected') -UnexpectedPackage
        Assert-Throws -MessageLike "*Unexpected package id 'Test.Unexpected'*" -Body {
            Invoke-StageRelease -InputRoot $fixture.SourceRoot -OutputPath $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
        }
    }

    Invoke-Test 'checksum mismatch is rejected' {
        $fixture = New-TestFixture -Root (Join-Path $tempRoot 'checksum')
        Invoke-StageRelease -InputRoot $fixture.SourceRoot -OutputPath $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
        $checksumsPath = Join-Path $fixture.StagePath 'SHA256SUMS'
        $lines = @(Get-Content -LiteralPath $checksumsPath)
        $replacement = if ($lines[0][0] -eq '0') { '1' } else { '0' }
        $lines[0] = $replacement + $lines[0].Substring(1)
        [System.IO.File]::WriteAllText($checksumsPath, (($lines -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
        Assert-Throws -MessageLike '*Checksum mismatch*' -Body {
            Test-ReleaseSet -Path $fixture.StagePath -PackageManifestPath $fixture.ManifestPath -Tag 'v3.8.0.1'
        }
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Tests passed: $script:Passed; failed: $script:Failed."
if ($script:Failed -ne 0) {
    exit 1
}
