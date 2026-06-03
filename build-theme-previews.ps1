# Build the Quarto site ONCE into docs-themes/site and expose a comparison page
# that previews every Bootswatch theme against that single, shared render.
#
# Why: a full render is ~16 MB (mostly images + content that is identical across
# themes). The only per-theme difference is the compiled bootstrap.min.css, which
# the runtime switcher in header-includes.html already loads from a CDN. Rendering
# the site once per theme therefore duplicated ~16 MB x 25 themes (~390 MB) of
# byte-for-byte identical content. This script keeps a single base render and lets
# the comparison page deep-link each palette via ?theme=<name>.

[CmdletBinding()]
param(
    [string]   $OutRoot = 'docs-themes',
    # Skip the base render entirely and reuse an already-rendered site (e.g. in CI,
    # where `quarto render` has already produced the parent site).
    [switch]   $SkipRender,
    # URL the preview iframe/cards point at, relative to <OutRoot>/index.html.
    # Default targets the local base render at <OutRoot>/site. In CI, where this
    # page is written inside the already-rendered docs/ site, pass '../Index.html'
    # to reuse the main render and avoid a duplicate copy.
    [string]   $SiteHref = './site/Index.html'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSCommandPath
Set-Location $repoRoot

# name + mode only; all visual data (colors, fonts) lives in header-includes.html.
$themes = @(
    @{ Name = 'cerulean';  Mode = 'light' },
    @{ Name = 'cosmo';     Mode = 'light' },
    @{ Name = 'flatly';    Mode = 'light' },
    @{ Name = 'journal';   Mode = 'light' },
    @{ Name = 'litera';    Mode = 'light' },
    @{ Name = 'lumen';     Mode = 'light' },
    @{ Name = 'lux';       Mode = 'light' },
    @{ Name = 'materia';   Mode = 'light' },
    @{ Name = 'minty';     Mode = 'light' },
    @{ Name = 'morph';     Mode = 'light' },
    @{ Name = 'pulse';     Mode = 'light' },
    @{ Name = 'sandstone'; Mode = 'light' },
    @{ Name = 'simplex';   Mode = 'light' },
    @{ Name = 'sketchy';   Mode = 'light' },
    @{ Name = 'spacelab';  Mode = 'light' },
    @{ Name = 'united';    Mode = 'light' },
    @{ Name = 'yeti';      Mode = 'light' },
    @{ Name = 'zephyr';    Mode = 'light' },
    @{ Name = 'cyborg';    Mode = 'dark'  },
    @{ Name = 'darkly';    Mode = 'dark'  },
    @{ Name = 'quartz';    Mode = 'dark'  },
    @{ Name = 'slate';     Mode = 'dark'  },
    @{ Name = 'solar';     Mode = 'dark'  },
    @{ Name = 'superhero'; Mode = 'dark'  },
    @{ Name = 'vapor';     Mode = 'dark'  }
)

New-Item -ItemType Directory -Force -Path $OutRoot | Out-Null
$siteDir = Join-Path $OutRoot 'site'

# Single base render (shared by every theme). Skipped when reusing an existing
# render (e.g. in CI, via -SkipRender + -SiteHref '../Index.html').
if ($SkipRender) {
    Write-Host "=== Skipping render (reusing existing site at '$SiteHref') ===" -ForegroundColor DarkGray
} else {
    Write-Host "=== Rendering base site once -> $siteDir ===" -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & quarto render --output-dir $siteDir --quiet
    $sw.Stop()
    if ($LASTEXITCODE -ne 0) {
        throw "Base render failed (exit $LASTEXITCODE)."
    }
    Write-Host "OK: base site in $([int]$sw.Elapsed.TotalSeconds)s" -ForegroundColor Green
}

# Generate the comparison index page. Each card points the preview iframe at the
# single base render, deep-linking the theme via ?theme=<name> (handled at runtime
# by header-includes.html, which swaps the Bootswatch CSS from the CDN).
$cards = ($themes | ForEach-Object {
    $n = $_.Name; $m = $_.Mode
    $badge = if ($m -eq 'dark') { 'background:#222;color:#eee' } else { 'background:#eef;color:#225' }
    @"
        <a class="card $m" href="$SiteHref?theme=$n" target="preview" data-name="$n">
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
    <p>One shared render, previewed under every Bootswatch palette. Click a theme to load it in the right pane; middle-click to open in a new tab.</p>
    <div class="grid">
$cards
    </div>
  </aside>
  <main>
    <div class="toolbar">
      <span>Preview pane &mdash;</span>
      <a href="#" onclick="document.querySelector('iframe').src=document.querySelector('iframe').src;return false;">Reload</a>
      <span id="current">cosmo</span>
    </div>
    <iframe name="preview" src="$SiteHref?theme=cosmo"></iframe>
  </main>
<script>
  document.querySelectorAll('.card').forEach(a => a.addEventListener('click', () => {
    document.getElementById('current').textContent = a.dataset.name;
  }));
</script>
</body>
</html>
"@

Set-Content -Path (Join-Path $OutRoot 'index.html') -Value $indexHtml -Encoding UTF8

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
if (-not $SkipRender) { Write-Host "Single base render: $siteDir" -ForegroundColor Green }
Write-Host "Preview source: $SiteHref" -ForegroundColor Green
Write-Host "Open: $OutRoot/index.html" -ForegroundColor Green
