$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$failures = [System.Collections.Generic.List[string]]::new()
$files = @(git ls-files --cached --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Git inventory failed' }
foreach ($required in @('Assets','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','AGENTS.md')) {
    if (!(Test-Path -LiteralPath $required)) { $failures.Add("Missing: $required") }
}
foreach ($file in $files) {
    if ($file -match '^(Library|Temp|Obj|Logs|Builds?|UserSettings|TestResults|\.codex)/') { $failures.Add("Generated/local file: $file") }
    if ($file -match '(^|/)(\.env($|\.)|id_rsa$)|\.(pem|key)$') { $failures.Add("Potential credential file: $file") }
    if ($file.StartsWith('Assets/') -and !$file.EndsWith('.meta') -and !$file.EndsWith('/.gitkeep')) {
        if (!(Test-Path -LiteralPath ($file + '.meta'))) { $failures.Add("Missing meta: $file") }
    }
}
$guids = @{}
foreach ($meta in Get-ChildItem Assets -Filter *.meta -File -Recurse) {
    $asset = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    if (!(Test-Path -LiteralPath $asset)) { $failures.Add("Orphan meta: $($meta.FullName)") }
    $match = [regex]::Match((Get-Content -LiteralPath $meta.FullName -Raw), '(?m)^guid: ([a-f0-9]{32})')
    if (!$match.Success) { $failures.Add("Invalid GUID: $($meta.FullName)"); continue }
    $guid = $match.Groups[1].Value
    if ($guids.ContainsKey($guid)) { $failures.Add("Duplicate GUID: $($meta.FullName)") }
    $guids[$guid] = $meta.FullName
    $relative = $meta.FullName.Substring((Get-Location).Path.Length + 1).Replace('\','/')
    git check-ignore -q --no-index -- $relative
    if ($LASTEXITCODE -eq 0) { $failures.Add("Ignored meta: $relative") }
}
foreach ($dir in Get-ChildItem Assets -Directory -Recurse) {
    if (!(Test-Path -LiteralPath ($dir.FullName + '.meta'))) { $failures.Add("Missing folder meta: $($dir.FullName)") }
}
foreach ($json in @('Packages/manifest.json','Packages/packages-lock.json')) {
    Get-Content $json -Raw | ConvertFrom-Json | Out-Null
}
if ((Get-Content ProjectSettings/EditorSettings.asset -Raw) -notmatch 'm_SerializationMode: 2') { $failures.Add('Force Text disabled') }
if ((Get-Content ProjectSettings/VersionControlSettings.asset -Raw) -notmatch 'm_Mode: Visible Meta Files') { $failures.Add('Visible Meta Files disabled') }
if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }; exit 1 }
Write-Output "Repository integrity passed: $($files.Count) files, $($guids.Count) unique asset GUIDs."
exit 0
