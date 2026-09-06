param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
& $Dotnet publish "$PSScriptRoot\src\Field\Field.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$PSScriptRoot\dist"
if ($LASTEXITCODE -ne 0) { throw 'Build failed. If FIELD is running from dist, exit it from the tray and retry.' }
