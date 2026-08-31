# Package release tooling

The existing [v3 workflow](../.github/workflows/v3.yml) is the only tag-release workflow. A matching tag such as `v3.8.0.1` builds version `3.8.0.1` once, validates the 11 packages listed in [package-manifest.json](package-manifest.json), publishes and verifies a GitHub Release, and only then publishes the same `.nupkg` bytes to NuGet.org.

## Dry run

Run the **Diginsight v3+ NuGet Packages** workflow manually and supply a source tag. Manual runs always stop after locked restore, build, staging, validation, and diagnostic artifact upload. They never create a GitHub Release or publish to NuGet.org.

For local validation from the repository root:

```powershell
pwsh ./eng/tests/Publish-Packages.Tests.ps1
$tag = 'v3.8.0.1'
$version = pwsh ./eng/Publish-Packages.ps1 -Command ResolveVersion -Tag $tag
# Restore/build exactly as the workflow does, then:
pwsh ./eng/Publish-Packages.ps1 -Command Stage -Tag $tag -SourceRoot ./src -StagePath "./artifacts/release/$tag"
pwsh ./eng/Publish-Packages.ps1 -Command Validate -Tag $tag -StagePath "./artifacts/release/$tag"
```

`SHA256SUMS` and `release-manifest.json` are deterministic and are generated from embedded `.nuspec` identities and versions, not filenames.

## Reruns and recovery

A rerun inspects every existing release asset before uploading anything. Matching assets are retained, missing assets are uploaded, and any same-name byte mismatch or unexpected asset fails the run. The final release is downloaded into a fresh directory and compared byte-for-byte with the staged workflow artifact before the NuGet job is authorized.

If GitHub Release verification succeeds but NuGet publication fails, rerun the same workflow for the same tag. Never replace assets or rebuild a published version manually; NuGet pushes use `--skip-duplicate` so an interrupted package set can be completed safely.

Changing the package surface requires an intentional update to [package-manifest.json](package-manifest.json). Every listed package currently requires one `.nupkg` and one `.snupkg`.
