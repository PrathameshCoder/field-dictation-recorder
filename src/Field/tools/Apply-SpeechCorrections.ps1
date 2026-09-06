$ErrorActionPreference = 'Stop'
$folder = Join-Path $env:LOCALAPPDATA 'Field'
$path = Join-Path $folder 'dictionary.json'
$existing = [System.Collections.Generic.List[object]]::new()
if (Test-Path -LiteralPath $path) { foreach ($item in (Get-Content -Raw -LiteralPath $path | ConvertFrom-Json)) { $existing.Add($item) } }
$recommendations = @(Get-Content -Raw "$PSScriptRoot\..\Design\RecommendedDictionary.json" | ConvertFrom-Json)
$added = 0
foreach ($entry in $recommendations) {
 $source = if ($entry.Heard) { $entry.Heard } else { $entry.Word }
 $matches = @($existing | Where-Object { $candidate = if ($_.Heard) { $_.Heard } else { $_.Word }; $candidate -eq $source })
 if ($matches.Count -eq 0) { $existing.Add($entry); $added++ }
}
if ($added -gt 0) {
 New-Item -ItemType Directory -Force -Path $folder | Out-Null
 if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $folder ('dictionary-backup-' + [guid]::NewGuid().ToString('N') + '.json')) }
 $temporary = $path + '.tmp'
 ConvertTo-Json -InputObject $existing.ToArray() -Depth 10 | Set-Content -LiteralPath $temporary -Encoding utf8
 Move-Item -LiteralPath $temporary -Destination $path -Force
}
Write-Output "Added $added scoped dictionary entries; preserved existing entries."
