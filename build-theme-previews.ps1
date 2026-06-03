# Build the Quarto site once per Bootswatch theme into docs-themes/<theme>/
# so the available styles can be compared side by side.

[CmdletBinding()]
param(
    [string[]] $Themes,
    [string]   $OutRoot = 'docs-themes',
    [switch]   $SkipExisting
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSCommandPath
Set-Location $repoRoot

$allThemes = @(
    @{ Name = 'cerulean';  Mode = 'light' },
    @{ Name = 'cosmo';     Mode = 'light' },
    @{ Name = 'cyborg';    Mode = 'dark'  },
    @{ Name = 'darkly';    Mode = 'dark'  },
    @{ Name = 'flatly';    Mode = 'light' },
    @{ Name = 'journal';   Mode = 'light' },
    @{ Name = 'litera';    Mode = 'light' },
    @{ Name = 'lumen';     Mode = 'light' },
    @{ Name = 'lux';       Mode = 'light' },
    @{ Name = 'materia';   Mode = 'light' },
    @{ Name = 'minty';     Mode = 'light' },
    @{ Name = 'morph';     Mode = 'light' },
    @{ Name = 'pulse';     Mode = 'light' },
    @{ Name = 'quartz';    Mode = 'dark'  },
    @{ Name = 'sandstone'; Mode = 'light' },
    @{ Name = 'simplex';   Mode = 'light' },
    @{ Name = 'sketchy';   Mode = 'light' },
    @{ Name = 'slate';     Mode = 'dark'  },
    @{ Name = 'solar';     Mode = 'dark'  },
    @{ Name = 'spacelab';  Mode = 'light' },
    @{ Name = 'superhero'; Mode = 'dark'  },
    @{ Name = 'united';    Mode = 'light' },
    @{ Name = 'vapor';     Mode = 'dark'  },
    @{ Name = 'yeti';      Mode = 'light' },
    @{ Name = 'zephyr';    Mode = 'light' }
)

if ($Themes) {
    $themesToBuild = $allThemes | Where-Object { $Themes -contains $_.Name }
} else {
    $themesToBuild = $allThemes
}

New-Item -ItemType Directory -Force -Path $OutRoot | Out-Null

$qmd = '_quarto.yml'
$backup = '_quarto.yml.bak'
Copy-Item $qmd $backup -Force
$originalYaml = Get-Content $qmd -Raw

# Pattern matches the format/html/theme block (light + dark lines) in _quarto.yml.
# It is replaced with simple "theme: <name>" so each render shows the unmodified bootswatch palette.
$themeBlockPattern = '(?ms)(\r?\n\s{4}theme:\r?\n\s{6}light:[^\r\n]*\r?\n\s{6}dark:[^\r\n]*)'

$results = @()

try {
    foreach ($t in $themesToBuild) {
        $name = $t.Name
        $mode = $t.Mode
        $outDir = Join-Path $OutRoot $name

        if ($SkipExisting -and (Test-Path (Join-Path $outDir 'Index.html'))) {
            Write-Host "=== Skipping $name (already built) ===" -ForegroundColor DarkGray
            $results += [pscustomobject]@{ Theme = $name; Mode = $mode; Status = 'skipped' }
            continue
        }

        Write-Host "=== Rendering theme: $name ($mode) ===" -ForegroundColor Cyan

        $newYaml = [regex]::Replace($originalYaml, $themeBlockPattern, "`r`n    theme: $name")
        if ($newYaml -eq $originalYaml) {
            throw "Could not patch theme block in $qmd for $name. Aborting."
        }
        Set-Content -Path $qmd -Value $newYaml -Encoding UTF8

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        & quarto render --output-dir $outDir --quiet
        $sw.Stop()

        if ($LASTEXITCODE -ne 0) {
            Write-Host "FAILED: $name (exit $LASTEXITCODE)" -ForegroundColor Red
            $results += [pscustomobject]@{ Theme = $name; Mode = $mode; Status = 'failed'; Seconds = [int]$sw.Elapsed.TotalSeconds }
        } else {
            Write-Host "OK: $name in $([int]$sw.Elapsed.TotalSeconds)s" -ForegroundColor Green
            $results += [pscustomobject]@{ Theme = $name; Mode = $mode; Status = 'ok'; Seconds = [int]$sw.Elapsed.TotalSeconds }
        }
    }
}
finally {
    Set-Content -Path $qmd -Value $originalYaml -Encoding UTF8
    Remove-Item $backup -ErrorAction SilentlyContinue
    Write-Host "Restored original $qmd" -ForegroundColor Yellow
}

# Generate the comparison index page.
$cards = ($themesToBuild | ForEach-Object {
    $n = $_.Name; $m = $_.Mode
    $badge = if ($m -eq 'dark') { 'background:#222;color:#eee' } else { 'background:#eef;color:#225' }
    @"
        <a class="card $m" href="./$n/Index.html" target="preview">
            <div class="name">$n</div>
            <div class="mode" style="$badge">$m</div>
        </a>
"@
}) -join "`n"

$indexHtml = @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Diginsight Telemetry &mdash; Quarto theme comparison</title>
<style>
  :root { color-scheme: light dark; }
  body { font-family: system-ui, -apple-system, Segoe UI, sans-serif; margin: 0; display: grid; grid-template-columns: 280px 1fr; height: 100vh; }
  aside { background: #fafafa; border-right: 1px solid #ddd; overflow-y: auto; padding: 1rem; }
  aside h1 { font-size: 1.05rem; margin: 0 0 .75rem 0; }
  aside p { font-size: .8rem; color: #555; margin: 0 0 1rem 0; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: .35rem; }
  .card { display: flex; flex-direction: column; align-items: center; gap: .25rem; padding: .55rem .25rem; background: white; border: 1px solid #ddd; border-radius: 6px; text-decoration: none; color: #222; font-size: .8rem; }
  .card:hover { border-color: #888; background: #fff8e8; }
  .card .mode { font-size: .65rem; padding: 1px 6px; border-radius: 10px; text-transform: uppercase; letter-spacing: .03em; }
  main { display: flex; flex-direction: column; }
  .toolbar { padding: .5rem .75rem; border-bottom: 1px solid #ddd; background: #f0f0f0; font-size: .85rem; display: flex; gap: .75rem; align-items: center; }
  .toolbar a { color: #225; }
  iframe { flex: 1; width: 100%; border: 0; }
</style>
</head>
<body>
  <aside>
    <h1>Quarto theme comparison</h1>
    <p>Click any theme to preview in the right pane. Open in a new tab with middle-click.</p>
    <div class="grid">
$cards
    </div>
  </aside>
  <main>
    <div class="toolbar">
      <span>Preview pane &mdash;</span>
      <a href="#" onclick="document.querySelector('iframe').src=document.querySelector('iframe').src;return false;">Reload</a>
      <span id="current"></span>
    </div>
    <iframe name="preview" src="./cosmo/Index.html"></iframe>
  </main>
<script>
  document.querySelectorAll('.card').forEach(a => a.addEventListener('click', e => {
    document.getElementById('current').textContent = a.querySelector('.name').textContent;
  }));
</script>
</body>
</html>
"@

Set-Content -Path (Join-Path $OutRoot 'index.html') -Value $indexHtml -Encoding UTF8

Write-Host ""
Write-Host "=== Build summary ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize
Write-Host ""
Write-Host "Open: $OutRoot/index.html" -ForegroundColor Green
