param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
python "$project/scripts/check_payload_inputs.py"
if ($LASTEXITCODE -ne 0) { throw 'Payload inputs are missing or changed. Prepare and seal the local video payloads before building.' }
$out = [IO.Path]::GetFullPath($OutputDirectory)
if ($out.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Build outside the public source directory.' }
if (Test-Path -LiteralPath $out) { throw 'Choose a new output directory.' }
New-Item -ItemType Directory -Path $out | Out-Null
$work = Join-Path $out 'build-work'
$release = Join-Path $out 'Zephyr-Chinese-Patcher'
python -m PyInstaller --noconfirm --clean --onefile --name ZephyrPatchEngine --collect-all UnityPy --distpath "$release/tools" --workpath "$work/pyinstaller" --specpath $work "$project/engine/main.py"
if ($LASTEXITCODE -ne 0) { throw 'Engine build failed' }
dotnet publish "$project/app/ZephyrPatcher.csproj" -c Release -r win-x64 --self-contained true -o $release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
$releasePayload = Join-Path $release 'payload'
New-Item -ItemType Directory -Path $releasePayload | Out-Null
foreach ($locale in @('zh-Hans', 'zh-Hant', 'en')) {
 Copy-Item -LiteralPath (Join-Path "$project/payload" $locale) -Destination $releasePayload -Recurse
}
Copy-Item -LiteralPath "$project/licenses" -Destination $release -Recurse
Copy-Item -LiteralPath "$project/THIRD_PARTY_NOTICES.md" -Destination $release
Copy-Item -LiteralPath "$project/LICENSE" -Destination $release
Write-Output "Built: $release"
